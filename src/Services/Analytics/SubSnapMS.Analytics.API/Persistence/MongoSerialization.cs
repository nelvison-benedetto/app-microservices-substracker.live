using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace SubSnapMS.Analytics.API.Persistence;

/// <summary>Guid standard, camelCase, campi sconosciuti ignorati. Da eseguire una volta all'avvio.</summary>
internal static class MongoSerialization
{
    private static int _registered;

    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
            return;

        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        ConventionRegistry.Register(
            "SubSnapMS.Analytics",
            new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String)
            },
            type => type.Namespace?.StartsWith("SubSnapMS.Analytics", StringComparison.Ordinal) == true);
    }
}