using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Abstractions;

/// <summary>
/// Identità stabile di un evento sul broker, indipendente dal nome della classe C#.
/// Esempio: [IntegrationEvent("subscriptions.events", "subscription.created.v1")]
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class IntegrationEventAttribute(string exchange, string name) : Attribute
{
    /// <summary>Exchange del servizio che pubblica: "&lt;servizio&gt;.events".</summary>
    public string Exchange { get; } = exchange;

    /// <summary>Nome versionato dell'evento. È anche la routing key: "&lt;entità&gt;.&lt;azione&gt;.v&lt;N&gt;".</summary>
    public string Name { get; } = name;
}
