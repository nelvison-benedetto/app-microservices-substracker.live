namespace SubSnapMS.Graph.API.Neo4j;

/// <summary>Applica GraphSchema all'avvio, in background con retry finché Neo4j non risponde.</summary>
internal sealed class SchemaInitializer(
    GraphClient graph,
    GraphSchemaState state,
    ILogger<SchemaInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    // Ogni istruzione di schema nella propria transazione (Neo4j non le mescola con altre)
                    foreach (var statement in GraphSchema.Statements)
                        await graph.WriteAsync("schema", statement, new { }, stoppingToken);

                    state.MarkReady();
                    logger.LogInformation("Neo4j schema ready");
                    return;
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    var delay = TimeSpan.FromSeconds(Math.Min(60, 2 * attempt));
                    logger.LogWarning(exception, "Neo4j schema initialization failed (attempt {Attempt}). Retrying in {Delay}", attempt, delay);
                    await Task.Delay(delay, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown durante l'attesa
        }
    }
}