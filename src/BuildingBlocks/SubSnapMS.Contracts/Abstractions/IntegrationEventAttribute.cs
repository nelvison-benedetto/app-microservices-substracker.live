namespace SubSnapMS.Contracts.Abstractions;

//Identità stabile di un evento sul broker, indipendente dal nome della classe C#. e.g. [IntegrationEvent("subscriptions.events", "subscription.created.v1")]

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]  //AttributeTargets.Class si puo mettere solo su classi (e.g. questa), un tipo che eredita da un event non eredita l'attribute, un evento ha solo 1 identità.
public sealed class IntegrationEventAttribute(string exchange, string name) : Attribute   //tutti gli attributi ereditano da System.Attribute, e e.g. questo lo puoi usaare chaiamando [IntegrationEventAttribute(..)] o anche [IntegrationEvent(..)] 
{
    public string Exchange { get; } = exchange;  //exchange del servizio che pubblica: "<servizio>.events", xk ogni service ha il proprio exchange rabbitmq

    public string Name { get; } = name;  //nome versionato dell'evento, ed è anche la Routing Key
}
//un attributo è un etichetta, non cambia il comportamento del code ma lo descrive, cosi altro code lo puo leggere a runtime (e.g.[HttpGet])
//“questo tipo, sul broker, si pubblica sull’exchange identity.events e si chiama user.registered.v1”.


