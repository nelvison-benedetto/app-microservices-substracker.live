namespace SubSnapMS.BuildingBlocks.Messaging.Configuration;

/*e.g. il service (ms) si collega a raabitmq con 
amqp://subsnap:password@rabbitmq:5672/subsnapms
       │       │        │        │    └ virtual host (è come un server isolato dentro server rabbitmq, di solito 1 per ogni ur platform)
       │       │        │        └ porta AMQP
       │       │        └ host
       │       └ password
       └ utente
oppure anche "Messaging": { "RabbitMq": { "Host": "rabbitmq", "Username": "subsnap" } }
*/

public sealed class RabbitMqOptions
{
    public const string SectionName = "Messaging:RabbitMq";

    //Alternativa ai campi singoli: amqp://user:password@host:5672/vhost.
    //Se valorizzato, ha la precedenza su Host, Port, Username, Password e VirtualHost.
    public Uri? Uri { get; set; }

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";


    
    public ushort PrefetchCount { get; set; } = 16;  //quando un consumer legga da una queue, rabbitmq non gli manda 1 mex alla volta aspettando la res, invece gli manda mex in anticipo fino ad un max (here 16).


    //se un handler lancia un'exception, il mex non va subito eliminato, ma invece il MessageDispatcher ritenta max X volte (here 3) con attese crescenti (backoff esponenziale). se proprio KO allora il mex va in DLQ.
    public int MaxRetryAttempts { get; set; } = 3;  //3 tentativi, dopo il primo di base.
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);  //1sec è la prima attesa, poi backoff esponenziale (base, base×2, base×4)
}


