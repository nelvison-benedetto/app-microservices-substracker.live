using Microsoft.Extensions.Hosting;  //BackgroundService (x il background, all'avvio chaiama StartAsync (che lancia ExecuteAsync) cosi l'app si avvia e allo spegnimento StopAsync)
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;          //IChannel, ExchangeType, BasicQos…
using RabbitMQ.Client.Events;   //AsyncEventingBasicConsumer, BasicDeliverEventArgs
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Configuration;
using SubSnapMS.BuildingBlocks.Messaging.Consuming;
using SubSnapMS.BuildingBlocks.Messaging.Diagnostics;
using SubSnapMS.BuildingBlocks.Messaging.Serialization;
using SubSnapMS.Contracts.Abstractions;
using System.Diagnostics;
using System.Text.Json;         //JsonSerializer, JsonException

namespace SubSnapMS.BuildingBlocks.Messaging.RabbitMq;

//Dichiara la topologia del servizio (coda quorum + DLQ + binding) e consuma i messaggi.  ack = processato · nack senza requeue = va in DLQ · nessuna risposta (shutdown) = RabbitMQ lo riconsegna.

internal sealed class RabbitMqConsumerService(
    RabbitMqConnection connection,  //connessione singleton
    ConsumerRegistry registry,
    MessageDispatcher dispatcher,  //esegue gli handler (Inbox + retry)
    MessagingSettings settings,
    IOptions<RabbitMqOptions> options,  //PrefetchCount
    ILogger<RabbitMqConsumerService> logger) : BackgroundService
{
    private static readonly TimeSpan StartupRetryDelay = TimeSpan.FromSeconds(5);  //attesa tra un tentativo falloto e il successivo
    private IChannel? _channel;  //channel su cui si consuma

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)  //come ricordi, con CancellationToken il token stoppingToken viene annullato se l'utente se ne va.
    {
        //RabbitMQ potrebbe non essere ancora pronto all'avvio: riprova finché non si collega
        while (!stoppingToken.IsCancellationRequested)  //ciclo continua finche l'app non viene spenta 
        {
            try
            {
                await StartConsumingAsync(stoppingToken);  //prova a dichiarare la topologia e a iscriversi alla coda.
                break;  //se ci riesce, stop cycle di tenativi di connessione.
            }
            catch (OperationCanceledException) when   (stoppingToken.IsCancellationRequested)  //se l'utente sta uscendo, allora non è un errore no logs.
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not start the RabbitMQ consumer. Retrying in {Delay}", StartupRetryDelay);
                await DisposeChannelAsync();  //chiude il channel, intanto il possimo ciclo ne userà uno nuovo
                await Task.Delay(StartupRetryDelay, stoppingToken);  //aspetta, con il token, 5sec
            }
        }

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);  //attesa infinita, ExecuteAsync non deve terminare xk seno BackgroundService lo interpreta come chiusura dell'app!!. questo termina solo se il token viene annullato.
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            //shutdown normale
        }
    }

    private async Task StartConsumingAsync(CancellationToken stoppingToken)
    {
        var rabbitConnection = await connection.GetConnectionAsync(stoppingToken);  //channel  //connection
        var channel = await rabbitConnection.CreateChannelAsync(cancellationToken: stoppingToken);  //channel dedicato al consumo (uno tuo, diverso da quello usato dal publisher rabbitmq)
        _channel = channel;

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: options.Value.PrefetchCount, global: false, cancellationToken: stoppingToken);  //imposta Quality of Service: quanti mexs il broker può consegnare senza ancora aver ricevuto l'ack. nessun limite in byte, max 16 mexs 'in volo' verso il consumer, regola applicata solo per ogni consumer registrato su questo channel 

        await channel.ExchangeDeclareAsync(settings.DeadLetterExchange, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: stoppingToken);  //Dead letter: ogni mex viene inviato a tutte le queues legate ignorando la routing key (i mexs scartati cmnq conservano la loro routing key) e con un fanout arrivano tutti alla DLQ senza binding per ciascun event. exchange sopravvive al riavvio di rabbitmq, e non viene cancellato quando na ha piu queues legate.

        await channel.QueueDeclareAsync(
            queue: settings.DeadLetterQueue,  // "subscriptions.queue.dlq"
            durable: true,   //queue e i suo mexs sopravvivono al riavvio
            exclusive: false,  //non è legata a questa sola connessione
            autoDelete: false,  //non sparisce quando non ha un consumer
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },  //queue di tipo quorom, come la principale
            cancellationToken: stoppingToken);

        //DLQ i mexs al suo interna restano li finche qualcuno non li guarda/sposta/cancella

        await channel.QueueBindAsync(settings.DeadLetterQueue, settings.DeadLetterExchange, routingKey: string.Empty, cancellationToken: stoppingToken);  //lega la DLQ alla DLX, la routing key va bene xk fanout la ignora

        // Coda principale (🔥🔥quorum = replicata e durevole); i messaggi rifiutati vanno nel DLX!
        await channel.QueueDeclareAsync(
            queue: settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                ["x-dead-letter-exchange"] = settings.DeadLetterExchange  //x-dead-letter-exchange dice al broker dove mandare i mexs rifiutati con nacck + requeue: false, è proprio questo a collegare la queue main alla DLX
            },
            cancellationToken: stoppingToken);

        //Un binding per ogni evento a cui il servizio è iscritto.
        //L'exchange viene dichiarato anche qui: così l'ordine di avvio tra producer e consumer non conta.
        foreach (var registration in registry.Registrations)
        {
            await channel.ExchangeDeclareAsync(registration.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);
            await channel.QueueBindAsync(settings.QueueName, registration.Exchange, routingKey: registration.EventName, cancellationToken: stoppingToken);
        }
        /*per ogni event registrato con AddConsumer, e.g.
        registration.Exchange  = "subscriptions.events"
        registration.EventName = "subscription.created.v1"

        dichiara l'exchange del producer (tipo Topic, durable), e crea binding ("i messaggi che arrivano su subscriptions.events con routing key subscription.created.v1 vanno in analytics.queue"), la routing key E' IL NOME ESATTO DELL'EVENTO (senza *, #) quindi la queue riceve solo gli events richiesti
        */

        var consumer = new AsyncEventingBasicConsumer(channel);  //crea il consumer del client rabbitmq nella versione async
        consumer.ReceivedAsync += (_, delivery) => OnMessageAsync(channel, delivery, stoppingToken);  //si iscrive all'event ReceivedAsync (che è sollevato ad ogni mex consegnato) VEDI METHOD HERE SOTTO

        await channel.BasicConsumeAsync(queue: settings.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);  //AVVIA IL CONSUMO, da questo momento il broker inizia a consegnare i mex, fino al prefetch

        logger.LogInformation(
            "Consuming {Queue} for events: {Events}",
            settings.QueueName,
            string.Join(", ", registry.Registrations.Select(r => r.EventName)));
    }


    private async Task OnMessageAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        var startTimestamp = Stopwatch.GetTimestamp();  //prende timestamp
        var properties = delivery.BasicProperties; 
        var eventName = properties.Type ?? MessageHeaders.GetString(properties.Headers, MessageHeaders.EventName);  //ricava il nome dell'event, prova priam properties.Type poi properties.Headers poi MessageHeaders.EventName

        if (eventName is null || !registry.TryGet(eventName, out var registration))
        {
            // Non dovrebbe succedere (la coda riceve solo gli eventi legati), ma non deve bloccare la coda
            logger.LogWarning("Received message {MessageId} with unknown event '{EventName}': discarded", properties.MessageId, eventName);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, CancellationToken.None);  //BasicAck il mex viene confermato e poi scartato, multiple=false per non confermare anche tutti i precedenti mexs che magari non sono ancora stati confermati
            MessagingDiagnostics.RecordProcessed(eventName ?? "unknown", MessagingDiagnostics.OutcomeDiscarded, startTimestamp);  //metrica con esito discarded
            return;
        }  //se il nome manca o non è nel registro, il servizio non sa cosa farse. cmnq non dovrebbe mai succedere.


        //Il body è valido solo durante questo handler: va deserializzato subito
        IntegrationEvent? integrationEvent;  //deserializza subito, fuori dal ty{} cosi puoi usarla anche dopo
        try
        {
            integrationEvent = (IntegrationEvent?)JsonSerializer.Deserialize(delivery.Body.Span, registration.EventType, MessagingJson.Options);
            //delivery.Body.Span , il System.Text.Json legge direttamente l'utf-8 senza passare da una str.
            //registration.EventType è il tipo concreto da creare
            //MessagingJson.Options cioè le stesse opzioni del publisher (camelcase, ect)
        }
        catch (JsonException exception)
        {
            //Payload illeggibile (o campo "required" mancante): riprovare non serve, va direttamente in DLQ
            logger.LogError(exception, "Cannot deserialize {EventName} {MessageId}: moved to {Dlq}", eventName, properties.MessageId, settings.DeadLetterQueue);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);  //BasicNack con requeue: false: il broker non rimette il messaggio in coda e, grazie a x-dead-letter-exchange, lo manda alla DLX e quindi nella DLQ
            MessagingDiagnostics.RecordProcessed(eventName, MessagingDiagnostics.OutcomeDeadLettered, startTimestamp);
            return;
        }

        if (integrationEvent is null)
        {
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
            MessagingDiagnostics.RecordProcessed(eventName, MessagingDiagnostics.OutcomeDeadLettered, startTimestamp);
            return;
        }
        //se il body contiene letterlmente null: allora json valido (quindo non lancia exception) ma return null

        var messageId = Guid.TryParse(properties.MessageId, out var parsedId) ? parsedId : integrationEvent.EventId;  //prima scelta: proprietà AMQP MessageId, che il publisher imposta all’id dell’evento, altrimento l' EventId viene letto dal payload
        //id usato per Inbox, log e tracing.  

        var parent = MessagingDiagnostics.ParseParent(
            MessageHeaders.GetString(properties.Headers, MessageHeaders.TraceParent),
            MessageHeaders.GetString(properties.Headers, MessageHeaders.TraceState));
        //legge traceparent e tracestate dagli header (convertendo i byte[] in stringhe con GetString) e li trasforma in un ActivityContext: trace id, span id del publisher e flag di campionamento. se gli header mancano o non sono validi, allora ParseParent return contesto vuoto e lo span che segue inizierà una traccia nuova

        using var activity = MessagingDiagnostics.ActivitySource.StartActivity($"{eventName} process", ActivityKind.Consumer, parent);  //using var, lo span si chiude da solo all'uscita dal method
        //apre lo span di elaborazione. ActivityKind.Consumer è il lato che riceve un mex. 'parent' quindi lo span diventa figlio di quello publish, nella stessa traccia della req http originale

        activity?.SetTag("messaging.system", "rabbitmq");  //system di messaging
        activity?.SetTag("messaging.operation.type", "process");  //tipo di operazione
        activity?.SetTag("messaging.destination.name", settings.QueueName);  //queue
        activity?.SetTag("messaging.message.id", messageId.ToString());  //id del mex
        //attributi standart delle convenzioni semantiche di lib OpenTelemetry


        var context = new MessageContext(messageId, eventName, properties.AppId, delivery.Redelivered, Attempt: 1);  //MessageContext passato agli handler insieme all'event

        try
        {
            await dispatcher.DispatchAsync(registration, integrationEvent, context, stoppingToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, CancellationToken.None);
            MessagingDiagnostics.RecordProcessed(eventName, MessagingDiagnostics.OutcomeSuccess, startTimestamp);
            //percorso normale. Il dispatcher, per ogni handler registrato per l’evento, crea uno scope DI, passa dall’Inbox e lo esegue con fino a 3 retry (backoff 1/2/4 s). Se ritorna senza eccezioni, tutti gli handler hanno finito. se tutto ok finora, allora BrockerAck il broker cancella il mex, e metrica success
            //l'ACK viene dopo l'elaborazine, se il processo crolla tra la fine degli handler e l'ack, allora il mex verrà riconsegnato (cmnq riconsegna max 1 volta, e l'Inbox impedisce che si ripeta)
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)  //se l'app si spegne durante l'elaborazione. il mex resta “consegnato ma non confermato” e verrà rimesso in coda quando app è accessa
        {
            //Shutdown durante l'elaborazione: nessun ack, RabbitMQ lo riconsegnerà alla prossima istanza
            logger.LogInformation("Shutdown while processing {EventName} {MessageId}: it will be redelivered", eventName, messageId);
        }
        catch (Exception exception) 
        {
            MessagingDiagnostics.RecordError(activity, exception);  //RecordError marca lo span come errore e ci allega l’eccezione, così in Jaeger lo vedi in rosso con lo stack trace
            logger.LogError(exception, "{EventName} {MessageId} failed after all retries: moved to {Dlq}", eventName, messageId, settings.DeadLetterQueue);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);  //mex va nella DLQ (se era requeue=true il mex tornerebbe in testa alla queue, no è proprio fallito questo mex!)
            MessagingDiagnostics.RecordProcessed(eventName, MessagingDiagnostics.OutcomeDeadLettered, startTimestamp);  //metrica dead_lettered
        }
    }


    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);   //annulla ExecuteAsync e gli handler in corso
        await DisposeChannelAsync();               //i mexs non confermati tornano in coda
    }  //override di StopAsync, chiamato auto dall'host allo spegnimento.


    private async Task DisposeChannelAsync()
    {
        if (_channel is null) return;
        try { await _channel.DisposeAsync(); }
        catch (Exception exception) { logger.LogDebug(exception, "Error while disposing the consumer channel"); }
        _channel = null;
    }


}
