using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SubSnapMS.BuildingBlocks.Messaging.Configuration;

namespace SubSnapMS.BuildingBlocks.Messaging.RabbitMq;

//UNA connessione TCP del processo (raccomandazione RabbitMQ!!) verso rabbitmq, condivisa da publisher  consumer e healthcheck, chius allo spegnimento dell'app.
//consigliato da rabbitmq, POCHE connessioni ma lunghe e con piu channels leggeri dentro.

internal sealed class RabbitMqConnection(
    IOptions<RabbitMqOptions> options,
    MessagingSettings settings,
    ILogger<RabbitMqConnection> logger) : IAsyncDisposable
{

    private readonly SemaphoreSlim _lock = new(1, 1);  //valore iniziale 1, max 1, quindi funziona come un lock
    private IConnection? _connection;  //la connessione

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        //Con l'automatic recovery l'oggetto connessione resta lo stesso anche dopo una caduta di rete: RabbitMQ.Client la ricollega da solo (insieme a exchange, code, binding e consumer).

        if (_connection is not null) return _connection;  //controllo veloce senza lock

        await _lock.WaitAsync(cancellationToken);  //await. prende il lock, se il posto è libero allora lo occupa e prosegue subito, altrimenti aspetta che si liberi
        try
        {
            if (_connection is not null) return _connection;  //ricontrollo, dentro il lock

            var rabbit = options.Value;  
            var factory = new ConnectionFactory  //crea la factory del client rabbitmq con un object initializer
            {
                ClientProvidedName = $"{settings.ServiceName}@{Environment.MachineName}", //Environment.MachineName è il nome della macchina, che in Docker è l’hostname del container
                AutomaticRecoveryEnabled = true,  //se la connessione cade dopo essere stata aperta, il client si ricollega da solo e riapre i channels
                TopologyRecoveryEnabled = true,  //dopo il ricollegamento ridichiara exchange, code e binding e riattiva i consumer che erano stati registrati su questa connessione
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),  //attesa tra un tentativo di ricollegamento e il successivo
                RequestedConnectionTimeout = TimeSpan.FromSeconds(10),  //tempo max per aprire la connessione
                ConsumerDispatchConcurrency = 1   //🔥🔥il client passa al codice del consumer un messaggio alla volta per channel, così l’ordine di elaborazione segue l’ordine della coda
            };

            if (rabbit.Uri is not null)  //se nella configurazione c'è uri completo tipo System.Uri, e.g. amqp://user:pass@rabbitmq:5672/vhost allora lo passa alla factory (che si scompone da solo i pezzi host, porta, utente, ...)
            {
                factory.Uri = rabbit.Uri;
            }
            else
            {
                factory.HostName = rabbit.Host;
                factory.Port = rabbit.Port;
                factory.UserName = rabbit.Username;
                factory.Password = rabbit.Password;
                factory.VirtualHost = rabbit.VirtualHost;  //virtual host è uno spazio isolato dentro rabbitmq, pensalo come un db interno in cui salva queue/exchange/permessi suoi di rabbitmq.
            }

            _connection = await factory.CreateConnectionAsync(cancellationToken); //apre socket tcp verso host e porta, fai handshake AMQP e negozia params come heartbeat e dimensione max dei frame, auth con user & psw, apre virtual host
            logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}{VirtualHost}", factory.HostName, factory.Port, factory.VirtualHost);
            
            return _connection;
        }
        finally
        {
            _lock.Release();  //libera il posto
        }
    }

    public async ValueTask DisposeAsync()  
    {
        if (_connection is not null)  //se la connessione esiste, la chiuse in modo ordinato
            await _connection.DisposeAsync();
        _lock.Dispose();
    }
}