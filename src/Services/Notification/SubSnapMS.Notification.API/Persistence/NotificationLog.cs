using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SubSnapMS.Notification.API.Configuration;
using SubSnapMS.Notification.API.Persistence.Documents;

namespace SubSnapMS.Notification.API.Persistence;

public enum ClaimResult
{
    //Puoi inviare
    Claimed,

    //Già inviata: non inviare
    AlreadySent,

    //Un altro tentativo sta inviando proprio ora: riprova più tardi
    InProgress,

    //Tentativi esauriti (o notifica rimossa dalla cancellazione account): non inviare
    GaveUp
}

//Registro invii. Prima di inviare si "prenota" la chiave: l'_id univoco garantisce che due consumer o due repliche del job non inviino la stessa notifica.
public sealed class NotificationLog(NotificationDb db, IOptions<NotificationOptions> options)
{
    private const int MaxErrorLength = 500;

    private static FilterDefinitionBuilder<NotificationLogDocument> Filter => Builders<NotificationLogDocument>.Filter;
    private static UpdateDefinitionBuilder<NotificationLogDocument> Update => Builders<NotificationLogDocument>.Update;

    public async Task<ClaimResult> TryClaimAsync(string key, Guid userId, string type, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var delivery = options.Value.Delivery;

        // 1. Prima volta: inserimento
        try
        {
            await db.Notifications.InsertOneAsync(
                new NotificationLogDocument
                {
                    Key = key,
                    UserId = userId,
                    Type = type,
                    Status = NotificationStatus.Sending,
                    Attempts = 1,
                    CreatedAtUtc = nowUtc,
                    ClaimedAtUtc = nowUtc
                },
                cancellationToken: cancellationToken);

            return ClaimResult.Claimed;
        }
        catch (MongoWriteException exception) when (exception.IsDuplicateKey())
        {
            // esiste già: vediamo se si può riprendere
        }

        // 2. Ripresa: tentativo precedente fallito, o rimasto "Sending" oltre il lease (crash)
        var staleClaim = nowUtc - delivery.ClaimLease;
        var reclaimable = Filter.Eq(d => d.Key, key)
                          & Filter.Lt(d => d.Attempts, delivery.MaxAttempts)
                          & (Filter.Eq(d => d.Status, NotificationStatus.Failed)
                             | (Filter.Eq(d => d.Status, NotificationStatus.Sending) & Filter.Lt(d => d.ClaimedAtUtc, staleClaim)));

        var result = await db.Notifications.UpdateOneAsync(
            reclaimable,
            Update
                .Set(d => d.Status, NotificationStatus.Sending)
                .Set(d => d.ClaimedAtUtc, nowUtc)
                .Inc(d => d.Attempts, 1),
            cancellationToken: cancellationToken);

        if (result.ModifiedCount == 1)
            return ClaimResult.Claimed;

        // 3. Non ripresa: perché?
        var existing = await db.Notifications.Find(Filter.Eq(d => d.Key, key)).FirstOrDefaultAsync(cancellationToken);

        return existing switch
        {
            { Status: NotificationStatus.Sent } => ClaimResult.AlreadySent,
            { Status: NotificationStatus.Sending } when existing.ClaimedAtUtc >= staleClaim => ClaimResult.InProgress,
            _ => ClaimResult.GaveUp
        };
    }

    public Task MarkSentAsync(string key, DateTime nowUtc, CancellationToken cancellationToken) =>
        db.Notifications.UpdateOneAsync(
            Filter.Eq(d => d.Key, key),
            Update
                .Set(d => d.Status, NotificationStatus.Sent)
                .Set(d => d.SentAtUtc, nowUtc)
                .Set(d => d.LastError, null),
            cancellationToken: cancellationToken);

    public Task MarkFailedAsync(string key, string error, CancellationToken cancellationToken) =>
        db.Notifications.UpdateOneAsync(
            Filter.Eq(d => d.Key, key),
            Update
                .Set(d => d.Status, NotificationStatus.Failed)
                .Set(d => d.LastError, error.Length > MaxErrorLength ? error[..MaxErrorLength] : error),
            cancellationToken: cancellationToken);

    public Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Notifications.DeleteManyAsync(Filter.Eq(d => d.UserId, userId), cancellationToken);
}