using System.Net;

namespace SubSnapMS.Search.API.Elasticsearch;

//Risposta di errore di Elasticsearch. Nel consumer → retry e poi DLQ; nell'API → 500
public sealed class ElasticsearchException(HttpStatusCode statusCode, string responseBody)
    : Exception($"Elasticsearch returned {(int)statusCode} {statusCode}: {responseBody}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}