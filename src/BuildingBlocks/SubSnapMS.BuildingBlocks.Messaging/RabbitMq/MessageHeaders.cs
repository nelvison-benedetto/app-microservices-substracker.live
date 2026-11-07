using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.RabbitMq;


/*
 
 */

internal static class MessageHeaders
{

    public const string TraceParent = "traceparent";
    public const string TraceState = "tracestate";
    //questi 2 sono dello standart W3C Trace Context, gli stessi che viaggiano nelle req http, ma qui sto usando gli events e mi servono cmnq. portano il TRACING DISTRIBUITO attraverso rabbitmq. 

    public const string EventName = "x-event-name";  //il nome dell’evento (quello di [IntegrationEvent]), da cui il consumer sceglie il tipo C# e gli handler nel ConsumerRegistry
    public const string SourceService = "x-source-service";  //quale servizio ha pubblicato il messaggio: utile nei log, nei tag del tracing e quando guardi un messaggio finito in DLQ


    //gli header stringa arrivano dal broker come byte[] UTF-8
    //Quando pubblichi, metti negli header str  headers[MessageHeaders.TraceParent] = "00-4bf9…-01";  , pero usando il protoccolo AMQP le stringhe degli header non hanno info about la codifica...quindi quando il mex arrivera al consumer, aspnetcore non sa se quelli erano byte di testo.
    public static string? GetString(IDictionary<string, object?>? headers, string key)
    {
        if (headers is null || !headers.TryGetValue(key, out var value) || value is null)  
            return null;
        //3 casi: header is null || il dizionario c'è ma la key no || la key c'è ma ha valore null

        return value switch  //switch, che controlla il tipo reale dell'obj
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),  //caso normale di un mex ricevuto dal broker, allora byte decodificati come utf-8 (stessa usata dal client in pubblicazione)
            string text => text,  //stringa cosi com'è
            _ => value.ToString()  //altro, la sua rappresentazione testuale
        };
    }


}