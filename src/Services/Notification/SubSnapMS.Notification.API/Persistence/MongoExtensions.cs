using MongoDB.Driver;

namespace SubSnapMS.Notification.API.Persistence;

internal static class MongoExtensions
{
    //>Upsert rifiutato dalla chiave univoca: il documento esiste già ed è più nuovo (o è un tombstone)
    public static bool IsDuplicateKey(this MongoWriteException exception) =>
        exception.WriteError?.Category == ServerErrorCategory.DuplicateKey;

    //Le date senza ora si salvano come mezzanotte UTC: confronti e ordinamenti corretti su Mongo
    public static DateTime ToBsonDate(this DateOnly date) => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    public static DateOnly ToDateOnly(this DateTime value) => DateOnly.FromDateTime(value);
}