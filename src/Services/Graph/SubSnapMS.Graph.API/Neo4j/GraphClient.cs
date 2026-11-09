using System.Diagnostics;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using SubSnapMS.Graph.API.Configuration;

namespace SubSnapMS.Graph.API.Neo4j;

//Esegue Cypher in transazioni GESTITE: il driver ripete da solo la transazione sugli errori transitori (es. cambio di leader in un cluster). Ogni chiamata è uno span "neo4j &lt;operazione&gt;".
//Il lavoro passato può essere eseguito più volte: deve essere idempotente.
public sealed class GraphClient(IDriver driver, IOptions<Neo4jOptions> options)
{
    private string Database => options.Value.Database;

    public async Task<IReadOnlyList<IRecord>> ReadAsync(string operation, string cypher, object parameters, CancellationToken cancellationToken)
    {
        using var activity = StartActivity(operation);
        await using var session = driver.AsyncSession(o => o.WithDatabase(Database).WithDefaultAccessMode(AccessMode.Read));

        return await session.ExecuteReadAsync(async transaction =>
        {
            var cursor = await transaction.RunAsync(cypher, parameters);
            return (IReadOnlyList<IRecord>)await cursor.ToListAsync(cancellationToken);
        });
    }

    public async Task<IReadOnlyList<IRecord>> WriteAsync(string operation, string cypher, object parameters, CancellationToken cancellationToken)
    {
        using var activity = StartActivity(operation);
        await using var session = driver.AsyncSession(o => o.WithDatabase(Database));

        return await session.ExecuteWriteAsync(async transaction =>
        {
            var cursor = await transaction.RunAsync(cypher, parameters);
            return (IReadOnlyList<IRecord>)await cursor.ToListAsync(cancellationToken);
        });
    }

    //Più istruzioni nella STESSA transazione (tutto o niente)
    public async Task WriteAsync(string operation, Func<IAsyncQueryRunner, Task> work)
    {
        using var activity = StartActivity(operation);
        await using var session = driver.AsyncSession(o => o.WithDatabase(Database));

        await session.ExecuteWriteAsync(async transaction =>
        {
            await work(transaction);
            return true;
        });
    }

    public Task VerifyConnectivityAsync() => driver.VerifyConnectivityAsync();

    private Activity? StartActivity(string operation)
    {
        var activity = GraphTelemetry.ActivitySource.StartActivity($"neo4j {operation}", ActivityKind.Client);
        activity?.SetTag("db.system", "neo4j");
        activity?.SetTag("db.namespace", Database);
        activity?.SetTag("db.operation.name", operation);
        return activity;
    }
}