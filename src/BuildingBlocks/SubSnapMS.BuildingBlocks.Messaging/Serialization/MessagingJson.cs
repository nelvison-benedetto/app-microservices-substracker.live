using System.Text.Json;

namespace SubSnapMS.BuildingBlocks.Messaging.Serialization;

/// <summary>Serializzazione degli eventi: camelCase, uguale per tutti i servizi.</summary>
internal static class MessagingJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}