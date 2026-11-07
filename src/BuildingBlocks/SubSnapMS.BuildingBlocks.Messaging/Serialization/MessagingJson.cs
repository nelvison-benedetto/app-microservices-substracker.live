using System.Text.Json;

namespace SubSnapMS.BuildingBlocks.Messaging.Serialization;

//serializzazione degli eventi: camelCase, uguale per tutti i servizi

internal static class MessagingJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);  //
}