namespace SubSnapMS.Search.API.Elasticsearch;

//"Indici pronti". Nessuna scrittura o ricerca prima: un PUT su un alias inesistente creerebbe un indice con mapping automatico (sbagliato).
public sealed class SearchIndexState
{
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsReady => _ready.Task.IsCompleted;

    public void MarkReady() => _ready.TrySetResult();

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken) => _ready.Task.WaitAsync(cancellationToken);
}