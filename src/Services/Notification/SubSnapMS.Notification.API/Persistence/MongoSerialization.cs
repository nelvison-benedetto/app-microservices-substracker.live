using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace SubSnapMS.Notification.API.Persistence;

/// <summary>
/// Configurazione globale del driver, da eseguire una volta prima di usare le collezioni:
/// Guid in formato standard (UUID binario sub-type 4), nomi dei campi in camelCase,
/// enum come stringhe, campi sconosciuti ignorati (documenti scritti da versioni più nuove).
/// </summary>
internal static class MongoSerialization
{
    private static int _registered;

    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
            return;

        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        ConventionRegistry.Register(
            "SubSnapMS.Notification",
            new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String)
            },
            type => type.Namespace?.StartsWith("SubSnapMS.Notification", StringComparison.Ordinal) == true);
    }
}