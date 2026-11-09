namespace SubSnapMS.Graph.API.Neo4j;

//Vincoli e indici (sintassi Neo4j 5, idempotente con IF NOT EXISTS).
//I vincoli di unicità creano anche l'indice e rendono MERGE sicuro con scritture concorrenti.
public static class GraphSchema
{
    public static readonly IReadOnlyList<string> Statements =
    [
        "CREATE CONSTRAINT user_id IF NOT EXISTS FOR (u:User) REQUIRE u.id IS UNIQUE",
        "CREATE CONSTRAINT subscription_id IF NOT EXISTS FOR (s:Subscription) REQUIRE s.id IS UNIQUE",
        "CREATE CONSTRAINT service_key IF NOT EXISTS FOR (s:Service) REQUIRE s.key IS UNIQUE",
        "CREATE CONSTRAINT deleted_user_id IF NOT EXISTS FOR (d:DeletedUser) REQUIRE d.id IS UNIQUE",
        "CREATE INDEX subscription_user_id IF NOT EXISTS FOR (s:Subscription) ON (s.userId)",
        "CREATE INDEX subscription_deleted IF NOT EXISTS FOR (s:Subscription) ON (s.deleted)"
    ];
}