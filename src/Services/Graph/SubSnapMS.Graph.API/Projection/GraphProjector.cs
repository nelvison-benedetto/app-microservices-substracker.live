using System.Globalization;
using Neo4j.Driver;
using SubSnapMS.Graph.API.Neo4j;

namespace SubSnapMS.Graph.API.Projection;

/// <summary>Stato completo di un abbonamento, preso da SubscriptionCreated/Updated.</summary>
public sealed record SubscriptionSnapshot(
    Guid SubscriptionId,
    Guid UserId,
    long Version,
    string Name,
    string? Category,
    decimal Amount,
    string Currency,
    string BillingCycle,
    DateOnly StartDate,
    DateOnly? EndDate);

/// <summary>Scritture sul grafo, tutte idempotenti (ogni transazione può essere ripetuta dal driver).</summary>
public sealed class GraphProjector(GraphClient graph, GraphSchemaState state, ILogger<GraphProjector> logger)
{
    /// <summary>Versione dei tombstone: nessun evento reale la supera.</summary>
    public const long TombstoneVersion = long.MaxValue;

    /// <summary>
    /// 1. MERGE del nodo Subscription; prosegue solo se non è un tombstone e la versione è più nuova
    ///    (altrimenti zero righe → le clausole successive non fanno nulla → applied = 0);
    /// 2. proprietà aggiornate, relazione User-HAS;
    /// 3. relazione OF spostata sul Service corretto (il nome può essere cambiato).
    /// </summary>
    private const string UpsertSubscription = """
        MERGE (s:Subscription {id: $id})
        WITH s
        WHERE coalesce(s.deleted, false) = false AND coalesce(s.version, -1) < $version
        SET s.userId = $userId,
            s.version = $version,
            s.name = $name,
            s.category = $category,
            s.serviceKey = $serviceKey,
            s.monthlyAmount = $monthlyAmount,
            s.currency = $currency,
            s.billingCycle = $billingCycle,
            s.startDate = date($startDate),
            s.endDate = CASE WHEN $endDate IS NULL THEN null ELSE date($endDate) END,
            s.updatedAt = datetime()
        MERGE (u:User {id: $userId})
        MERGE (u)-[:HAS]->(s)
        WITH s
        OPTIONAL MATCH (s)-[old:OF]->(:Service)
        DELETE old
        WITH DISTINCT s
        MERGE (svc:Service {key: $serviceKey})
          ON CREATE SET svc.name = $name
        MERGE (s)-[:OF]->(svc)
        RETURN count(s) AS applied
        """;

    /// <summary>Tombstone: niente dati, nessuna relazione, versione massima.</summary>
    private const string MarkSubscriptionDeleted = """
        MERGE (s:Subscription {id: $id})
        SET s.deleted = true,
            s.version = $version,
            s.userId = $userId,
            s.deletedAt = datetime()
        REMOVE s.name, s.category, s.serviceKey, s.monthlyAmount, s.currency,
               s.billingCycle, s.startDate, s.endDate, s.updatedAt
        WITH s
        OPTIONAL MATCH (s)-[r]-()
        DELETE r
        """;

    private const string IsUserDeleted = """
        OPTIONAL MATCH (d:DeletedUser {id: $userId})
        RETURN d IS NOT NULL AS deleted
        """;

    public async Task UpsertAsync(SubscriptionSnapshot snapshot, CancellationToken cancellationToken)
    {
        await state.WaitUntilReadyAsync(cancellationToken);

        var records = await graph.WriteAsync("upsert-subscription", UpsertSubscription, new
        {
            id = snapshot.SubscriptionId.ToString(),
            userId = snapshot.UserId.ToString(),
            version = snapshot.Version,
            name = snapshot.Name,
            category = string.IsNullOrWhiteSpace(snapshot.Category) ? null : snapshot.Category.Trim(),
            serviceKey = ServiceKey.From(snapshot.Name),
            monthlyAmount = (double)MonthlyCost.Calculate(snapshot.Amount, snapshot.BillingCycle),   // Neo4j non ha decimal
            currency = snapshot.Currency,
            billingCycle = snapshot.BillingCycle,
            startDate = FormatDate(snapshot.StartDate),
            endDate = snapshot.EndDate is { } end ? FormatDate(end) : null
        }, cancellationToken);

        if (records.Count == 0 || records[0]["applied"].As<long>() == 0)
            logger.LogDebug("Subscription {SubscriptionId} v{Version}: older, duplicate or deleted, ignored", snapshot.SubscriptionId, snapshot.Version);
    }

    public async Task MarkDeletedAsync(Guid subscriptionId, Guid userId, CancellationToken cancellationToken)
    {
        await state.WaitUntilReadyAsync(cancellationToken);

        await graph.WriteAsync("delete-subscription", MarkSubscriptionDeleted, new
        {
            id = subscriptionId.ToString(),
            userId = userId.ToString(),
            version = TombstoneVersion
        }, cancellationToken);
    }

    /// <summary>In una transazione: tombstone dell'utente, poi tutti i suoi abbonamenti (anche tombstone) e il nodo User.</summary>
    public async Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await state.WaitUntilReadyAsync(cancellationToken);
        var parameters = new { userId = userId.ToString() };

        await graph.WriteAsync("delete-user", async transaction =>
        {
            await (await transaction.RunAsync("MERGE (d:DeletedUser {id: $userId}) SET d.deletedAt = datetime()", parameters)).ConsumeAsync();
            await (await transaction.RunAsync("MATCH (s:Subscription {userId: $userId}) DETACH DELETE s", parameters)).ConsumeAsync();
            await (await transaction.RunAsync("MATCH (u:User {id: $userId}) DETACH DELETE u", parameters)).ConsumeAsync();
        });

        logger.LogInformation("User deletion {UserId}: graph data removed", userId);
    }

    public async Task<bool> IsUserDeletedAsync(Guid userId, CancellationToken cancellationToken)
    {
        await state.WaitUntilReadyAsync(cancellationToken);

        var records = await graph.ReadAsync("is-user-deleted", IsUserDeleted, new { userId = userId.ToString() }, cancellationToken);
        return records[0]["deleted"].As<bool>();
    }

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}