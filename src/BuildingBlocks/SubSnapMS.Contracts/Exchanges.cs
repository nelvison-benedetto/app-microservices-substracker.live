namespace SubSnapMS.Contracts;

//nomi degli exchange di RabbitMQ, cioè i 'punti di smistamento', su cui i services pubblicano gli events.
//Produttore ──► EXCHANGE ──(binding: regola)──► CODA ──► Consumer.  
//Exchange riceve i mexs e decide in quali code copiarli, Coda è dove aspettano i mexs di essere letti, Binding è la regola che collega una coda ad uno exchange, Routing Key è l'etichetta del mex usata dai binding (il nome dell'evento, e.g.subscription.created.v1)

//un Exchange per ogni service produttore
public static class Exchanges
{
    public const string Identity = "identity.events";
    public const string Subscriptions = "subscriptions.events";
    public const string Media = "media.events";
    public const string UserDeletion = "user-deletion.events";  //x la risposta dell Saga di cancellazione account
}