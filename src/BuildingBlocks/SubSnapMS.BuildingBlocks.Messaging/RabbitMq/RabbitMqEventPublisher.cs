using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Configuration;
using SubSnapMS.BuildingBlocks.Messaging.Diagnostics;
using SubSnapMS.BuildingBlocks.Messaging.Serialization;
using SubSnapMS.Contracts.Abstractions;
using System.Diagnostics;
using System.Text.Json;

namespace SubSnapMS.BuildingBlocks.Messaging.RabbitMq;


internal sealed class RabbitMqEventPublisher(  //unico componente che pubblica davvero on rabbitmq
    RabbitMqConnection connection,
    MessagingSettings settings,
    ILogger<RabbitMqEventPublisher> logger) : IEventPublisher, IMessagePublisher, IAsyncDisposable  //3 interfaces. IEventPublisher (x pubblica x per i servizi), IMessagePublisher (interna, x Outbox) e IAsyncDisposable (per chiudere il channel allo spegnimento)
{
    //un channel non è thread-safe: le publish passano una alla volta
    private readonly SemaphoreSlim _lock = new(1, 1);  //lock, max 1 posto.
    private readonly HashSet<string> _declaredExchanges = new(StringComparer.Ordinal);  //insieme dei nomi di exchange già dichiarati sul channel corrente, è una cache: exchange si dichiara la prima volta che serve poi non piu. StringComparer.Ordinal confronta le stringhe carattere per carattere (case sensitive)
    private IChannel? _channel;  //channel di pubblicazione 


    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);  //se event è null allora eccezzione

        var eventType = integrationEvent.GetType();   //usa il tipo reale a runtime, non quello dichiarato
        var metadata = EventMetadata.For(eventType);  //legge dalla cache di EventMetadata, il nome (subscription.created.v1) e l’exchange (subscriptions.events) dichiarati nell’attributo [IntegrationEvent]. Se l’attributo manca, For lancia un’eccezione.
        var current = Activity.Current;  //lo span attivo in questo momento

        //costruzione pacchetto standart
        var message = new OutgoingMessage(
            integrationEvent.EventId,  //id del messaggio = id dell'evento
            metadata.Exchange,   //"subscriptions.events"
            metadata.Name,  //"subscription.created.v1"
            JsonSerializer.SerializeToUtf8Bytes(integrationEvent, eventType, MessagingJson.Options),  //serializza direttamente in utf-8 (senza passare da str)
            integrationEvent.OccurredOnUtc,  //timestamp
            current?.Id,  //con il formato W3C (il default di .NET), l’Id di un’Activity è già una stringa traceparent (00-<trace-id>-<span-id>-<flag>).
            current?.TraceStateString);  //il tracestate, se presente

        return PublishRawAsync(message, cancellationToken);  //see below
    }


    public async Task PublishRawAsync(OutgoingMessage message, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);   //lock 

        try
        {
            var channel = await GetChannelAsync(cancellationToken);  //ur channel di pubblicazione (quello attivo, altrimenti uno nuovo). see below
            await EnsureExchangeAsync(channel, message.Exchange, cancellationToken);  //ur see below. exchange di destinazione deve esistere (in AMQP pubblicare su un exchange inesistente è errore che chiude il channel!)

            var parent = MessagingDiagnostics.ParseParent(message.TraceParent, message.TraceState);  //urs, ricostruisce il contesto del genitore dal mex
            using var activity = MessagingDiagnostics.ActivitySource.StartActivity(
                $"{message.EventName} publish", ActivityKind.Producer, parent);  //apre lo span subscription.created.v1 publish. ActivityKind.Producer è il lato che invia il mex, using var si chiude alla fine del blocco try{}, può essere null se nessuno ascolta l’ActivitySource o se la traccia non è campionata.
            
            //setta le convenzioni semantiche di OpenTelemetry
            activity?.SetTag("messaging.system", "rabbitmq");  //system di messaging
            activity?.SetTag("messaging.operation.type", "publish");  //tipo di operazione
            activity?.SetTag("messaging.destination.name", message.Exchange);  //queue
            activity?.SetTag("messaging.rabbitmq.destination.routing_key", message.EventName);  
            activity?.SetTag("messaging.message.id", message.MessageId.ToString());

            //il consumer continuerà questa trace
            var headers = new Dictionary<string, object?>  //crea dictionary
            {
                [MessageHeaders.EventName] = message.EventName,  // "x-event-name"
                [MessageHeaders.SourceService] = settings.ServiceName  //"x-source-service"
            };

            var traceParent = activity?.Id ?? message.TraceParent;
            var traceState = activity?.TraceStateString ?? message.TraceState;
            //se lo span di publish esiste, mette negli header il suo Id, quindi il consumer diventerà figlio della pubblicazione, e la traccia avrà la catena completa. se invece lo span di publish non esiste, allora si ripiega su message.xxx

            if (traceParent is not null) headers[MessageHeaders.TraceParent] = traceParent;
            if (traceState is not null) headers[MessageHeaders.TraceState] = traceState;
            //aggiunge agli headers gli headers di tracing (solo se c'è un valore)

            var properties = new BasicProperties
            {
                MessageId = message.MessageId.ToString(),  //id del mex come str (le proprietà di AMQP sono testuali). intanto il consumer lo rilegge con Guid.Tryparse per l'Inbox
                Type = message.EventName,
                AppId = settings.ServiceName,  //applicazione mittente, visibile nella UI e passata agli handler in MessageContext
                ContentType = "application/json",  //formato del body
                DeliveryMode = DeliveryModes.Persistent,  //sopravvive a un riavvio del broker
                Timestamp = new AmqpTimestamp(message.OccurredOnUtc.ToUnixTimeSeconds()),  //timestamp AMQP
                Headers = headers  //dictionary costruito da te
            };

            try
            {
                //con i publisher confirms attivi, questa await termina solo quando RabbitMQ ha preso in carico il messaggio; se lo rifiuta, lancia un'eccezione. mandatory: false → un evento senza consumer iscritti viene scartato senza errore.
                await channel.BasicPublishAsync(  //🔥🔥MANDA IL MEX!!
                    exchange: message.Exchange,  //"subscriptions.events"
                    routingKey: message.EventName,  //"subscription.created.v1"
                    mandatory: false,  //se nessuna coda è legata a quella routing key (nessun servizio è iscritto a quell’evento), il broker scarta il messaggio in silenzio e conferma comunque
                    basicProperties: properties,
                    body: message.Body,
                    cancellationToken: cancellationToken);
            }
            catch (Exception exception)
            {
                MessagingDiagnostics.RecordError(activity, exception);
                throw;
            }

            MessagingDiagnostics.PublishedMessages.Add(1, new KeyValuePair<string, object?>("messaging.event.name", message.EventName));
            logger.LogDebug("Published {EventName} {MessageId} to {Exchange}", message.EventName, message.MessageId, message.Exchange);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)  //x qualsiasi errore che non sia una cancellazione
        {
            //un errore a livello di channel lo chiude: alla prossima publish se ne apre uno nuovo
            await ResetChannelAsync();  //ur see below
            throw;  //è senza argomento. here rilascia la stessa exception con lo stack trace originale.
        }
        finally
        {
            _lock.Release();  //di nuovo 1 posto libero! 
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true }) return _channel;  //è true se _channel non è null e al sua proprietà IsOpen è true
        await ResetChannelAsync();  //ur. per sicurezza chiude il channel vecchio e svuota la cache degli exchange
        var rabbitConnection = await connection.GetConnectionAsync(cancellationToken);  //la connessione singleton. se rabbitmq non è raggiungibile l'eccezione nasce qui e risale al chiamante
        _channel = await rabbitConnection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);  //apre di nuovo il channel con le opzioni del client rabbitmq7. 
            //publisherConfirmationsEnabled: true: mette il channel in modalità confirm. Il broker risponderà con un ack o un nack a ogni messaggio pubblicato.
            //publisherConfirmationTrackingEnabled: true: il client tiene traccia di ogni pubblicazione e fa terminare il BasicPublishAsync solo quando arriva la risposta corrispondente, lanciando un’eccezione in caso di nack. Senza questa opzione le conferme arriverebbero, ma il codice dovrebbe gestirle a mano.
        return _channel;
    }


    private async Task EnsureExchangeAsync(IChannel channel, string exchange, CancellationToken cancellationToken)
    {
        if (_declaredExchanges.Contains(exchange)) return;
        //se l'exchange è stato gia dichiarato su questo channel, esci subito.
        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        //dichiara l'exchange CON GLI STESSI IDENTICI PARAMETRI USATI DAL CONSUMER, è fondamentale che coincidano (altrimenti il broker chiuderebbe il channel)
        _declaredExchanges.Add(exchange);
    }


    private async Task ResetChannelAsync()  //chiude il channel vecchio e svuota la cache degli exchange
    {
        _declaredExchanges.Clear();  //svuota sempre la cache degli exchange (anche se non c'è un channel da chiudere)
        if (_channel is null) return;
        try { await _channel.DisposeAsync(); }
        catch (Exception exception) { logger.LogDebug(exception, "Error while disposing the publisher channel"); }
        _channel = null;
    }


    public async ValueTask DisposeAsync()
    {
        await ResetChannelAsync();
        _lock.Dispose();
    }


}