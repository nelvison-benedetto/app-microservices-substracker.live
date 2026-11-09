namespace SubSnapMS.Search.API.Elasticsearch;

//Indici e loro definizione. Il codice usa sempre gli ALIAS; l'indice fisico ha la versione nel nome. Per cambiare il mapping: nuova versione (v2), creazione, spostamento dell'alias, --republish-events.
public static class SearchIndices
{
    public const string Subscriptions = "subscriptions";
    public const string SubscriptionsV1 = "subscriptions-v1";

    public const string DeletedUsers = "deleted-users";
    public const string DeletedUsersV1 = "deleted-users-v1";

    //Analizzatori:
    // - folded: minuscole + accenti rimossi ("Caffè" = "caffe");
    // - autocomplete: come folded + edge n-gram in indicizzazione ("netflix" → n, ne, net, …);
    // - lowercase_folded (normalizer): per keyword usate in filtri e ordinamenti, senza distinzione di maiuscole.
    public static string SubscriptionsDefinition(int replicas) => $$"""
        {
          "settings": {
            "number_of_shards": 1,
            "number_of_replicas": {{replicas}},
            "analysis": {
              "filter": {
                "autocomplete_edge_ngram": { "type": "edge_ngram", "min_gram": 1, "max_gram": 20 }
              },
              "analyzer": {
                "folded": {
                  "type": "custom",
                  "tokenizer": "standard",
                  "filter": [ "lowercase", "asciifolding" ]
                },
                "autocomplete": {
                  "type": "custom",
                  "tokenizer": "standard",
                  "filter": [ "lowercase", "asciifolding", "autocomplete_edge_ngram" ]
                }
              },
              "normalizer": {
                "lowercase_folded": { "type": "custom", "filter": [ "lowercase", "asciifolding" ] }
              }
            }
          },
          "mappings": {
            "dynamic": "strict",
            "properties": {
              "subscriptionId": { "type": "keyword" },
              "userId":         { "type": "keyword" },
              "deleted":        { "type": "boolean" },
              "name": {
                "type": "text",
                "analyzer": "folded",
                "fields": {
                  "autocomplete": { "type": "text", "analyzer": "autocomplete", "search_analyzer": "folded" },
                  "sort":         { "type": "keyword", "normalizer": "lowercase_folded" }
                }
              },
              "category": {
                "type": "text",
                "analyzer": "folded",
                "fields": {
                  "keyword": { "type": "keyword", "normalizer": "lowercase_folded" },
                  "raw":     { "type": "keyword" }
                }
              },
              "amount":        { "type": "scaled_float", "scaling_factor": 100 },
              "monthlyAmount": { "type": "scaled_float", "scaling_factor": 100 },
              "currency":      { "type": "keyword" },
              "billingCycle":  { "type": "keyword" },
              "startDate":     { "type": "date", "format": "yyyy-MM-dd" },
              "endDate":       { "type": "date", "format": "yyyy-MM-dd" },
              "updatedAt":     { "type": "date" }
            }
          },
          "aliases": { "{{Subscriptions}}": {} }
        }
        """;

    public static string DeletedUsersDefinition(int replicas) => $$"""
        {
          "settings": { "number_of_shards": 1, "number_of_replicas": {{replicas}} },
          "mappings": {
            "dynamic": "strict",
            "properties": {
              "userId":    { "type": "keyword" },
              "deletedAt": { "type": "date" }
            }
          },
          "aliases": { "{{DeletedUsers}}": {} }
        }
        """;
}