using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SubSnapMS.Notification.API.Configuration;
using SubSnapMS.Notification.API.Persistence.Documents;

namespace SubSnapMS.Notification.API.Persistence;

public sealed class RecipientStore(NotificationDb db, IOptions<NotificationOptions> options)
{
    private static FilterDefinitionBuilder<RecipientDocument> Filter => Builders<RecipientDocument>.Filter;
    private static UpdateDefinitionBuilder<RecipientDocument> Update => Builders<RecipientDocument>.Update;

    //Da UserRegistered e UserEmailChanged. Vince l'email con la data più recente, qualunque sia l'ordine di arrivo.
    //Se il documento esiste con una data più recente (o è un tombstone) il filtro non lo trova, l'upsert prova a inserirlo e la chiave _id lo rifiuta: l'evento vecchio viene ignorato.
    public async Task UpsertEmailAsync(Guid userId, string email, DateTimeOffset changedOnUtc, CancellationToken cancellationToken)
    {
        var changedOn = changedOnUtc.UtcDateTime;

        var filter = Filter.Eq(d => d.UserId, userId)
                     & Filter.Eq(d => d.Deleted, false)
                     & Filter.Lt(d => d.EmailUpdatedAtUtc, changedOn);

        var update = Update
            .Set(d => d.Email, email)
            .Set(d => d.EmailUpdatedAtUtc, changedOn);

        try
        {
            await db.Recipients.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.IsDuplicateKey())
        {
            // evento più vecchio di quello già applicato, o utente cancellato
        }
    }

    //Email corrente dell'utente; null se sconosciuto o cancellato
    public Task<string?> GetEmailAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Recipients
            .Find(Filter.Eq(d => d.UserId, userId) & Filter.Eq(d => d.Deleted, false))
            .Project(d => d.Email)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var recipients = await db.Recipients
            .Find(Filter.In(d => d.UserId, userIds) & Filter.Eq(d => d.Deleted, false) & Filter.Ne(d => d.Email, null))
            .ToListAsync(cancellationToken);

        return recipients.ToDictionary(d => d.UserId, d => d.Email!);
    }

    public Task<bool> IsDeletedAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Recipients
            .Find(Filter.Eq(d => d.UserId, userId) & Filter.Eq(d => d.Deleted, true))
            .AnyAsync(cancellationToken);

    //Sostituisce i dati con un tombstone senza email, eliminato dal TTL dopo DeletedTombstoneRetention
    public Task MarkDeletedAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.Recipients.ReplaceOneAsync(
            Filter.Eq(d => d.UserId, userId),
            new RecipientDocument
            {
                UserId = userId,
                Email = null,
                EmailUpdatedAtUtc = now.UtcDateTime,
                Deleted = true,
                ExpireAtUtc = (now + options.Value.DeletedTombstoneRetention).UtcDateTime
            },
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
}