using SubSnapMS.Search.API.Elasticsearch;

namespace SubSnapMS.Search.API.Indexing;

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

/// <summary>Scritture sugli indici, tutte idempotenti.</summary>
public sealed class SubscriptionIndexer(
    ElasticHttpClient elastic,
    SearchIndexState state,
    TimeProvider timeProvider,
    ILogger<SubscriptionIndexer> logger)
{
    /// <summary>
    /// Versione dei tombstone: più alta di qualsiasi Version reale, quindi nessun evento successivo li sovrascrive.
    /// Elasticsearch accetta versioni esterne fino a circa 9,2 × 10^18.
    /// </summary>
    public const long TombstoneVersion = 9_000_000_000_000_000_000;

    public async Task UpsertAsync(SubscriptionSnapshot snapshot, CancellationToken cancellationToken)
    {
        await state.WaitUntilReadyAsync(cancellationToken);

        var document = new SubscriptionSearchDocument(
            snapshot.SubscriptionId,
            snapshot.UserId,
            Deleted: false,
            snapshot.Name,
            string.IsNullOrWhiteSpace(snapshot.Category) ? null : snapshot.Category.Trim(),
            snapshot.Amount,
            MonthlyCost.Calculate(snapshot.Amount, snapshot.BillingCycle),
            snapshot.Currency,
            snapshot.BillingCycle,
            snapshot.StartDate,
            snapshot.EndDate,
            timeProvider.GetUtcNow());

        var applied = await elastic.IndexWithExternalVersionAsync(
            SearchIndices.Subscriptions, snapshot.SubscriptionId.ToString(), document, snapshot.Version, cancellationToken);

        if (!applied)
            logger.LogDebug("Subscription {SubscriptionId} v{Version}: older or duplicate, ignored", snapshot.SubscriptionId, snapshot.Version);
    }

    public async Task MarkDeletedAsync(Guid subscriptionId, Guid userId, CancellationToken cancellationToken)
    {
        await state.WaitUntilReadyAsync(cancellationToken);

        // false = già tombstone (evento duplicato): va bene
        await elastic.IndexWithExternalVersionAsync(
            SearchIndices.Subscriptions,
            subscriptionId.ToString(),
            new SubscriptionTombstoneDocument(subscriptionId, userId, Deleted: true, timeProvider.GetUtcNow()),
            TombstoneVersion,
            cancellationToken);
    }

    /// <summary>Prima il tombstone dell'utente (blocca gli eventi in ritardo), poi tutti i suoi documenti.</summary>
    public async Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await state.WaitUntilReadyAsync(cancellationToken);

        await elastic.IndexAsync(
            SearchIndices.DeletedUsers, userId.ToString(), new DeletedUserDocument(userId, timeProvider.GetUtcNow()), cancellationToken);

        var deleted = await elastic.DeleteByQueryAsync(
            SearchIndices.Subscriptions, EsQuery.Term("userId", userId.ToString()), cancellationToken);

        logger.LogInformation("User deletion {UserId}: {Count} search documents removed", userId, deleted);
    }

    public async Task<bool> IsUserDeletedAsync(Guid userId, CancellationToken cancellationToken)
    {
        await state.WaitUntilReadyAsync(cancellationToken);
        return await elastic.ExistsAsync($"{SearchIndices.DeletedUsers}/_doc/{userId}", cancellationToken);
    }
}