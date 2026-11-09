using MongoDB.Driver;

namespace SubSnapMS.Analytics.API.Persistence;

internal static class MongoExtensions
{
    //Upsert rifiutato dalla chiave univoca: il documento esiste già ed è più nuovo (o è un tombstone)
    public static bool IsDuplicateKey(this MongoWriteException exception) =>
        exception.WriteError?.Category == ServerErrorCategory.DuplicateKey;

    //Date senza ora salvate come mezzanotte UTC
    public static DateTime ToBsonDate(this DateOnly date) => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    public static DateOnly ToDateOnly(this DateTime value) => DateOnly.FromDateTime(value);
}