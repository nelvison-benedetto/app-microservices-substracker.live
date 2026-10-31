namespace SubSnapMS.Graph.API.Neo4j;

/// <summary>"Schema pronto": nessuna scrittura prima dei vincoli (MERGE senza vincolo può creare duplicati).</summary>
public sealed class GraphSchemaState
{
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsReady => _ready.Task.IsCompleted;

    public void MarkReady() => _ready.TrySetResult();

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken) => _ready.Task.WaitAsync(cancellationToken);
}