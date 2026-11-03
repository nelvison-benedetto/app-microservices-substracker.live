using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Observability;

//impostazioni di osservabilità. Si possono sovrascrivere dalla sezione "Observability" della configurazione (appsettings / variabili d'ambiente Observability__*).
public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";  

    public const string SourcePrefix = "SubSnapMS";  

    /// <summary>Nome con cui il servizio appare in trace, metriche e log (es. "api-gateway").</summary>
    public string ServiceName { get; set; } = "unknown-service";  //default, il nome con cui il servizio appare in trace, metriche e log (es. "api-gateway"). verra sempre sivrascritto questo default.

    public string ServiceVersion { get; set; } = ResolveEntryAssemblyVersion();  //versione del servizio. dei default prende il InformationalVersion dell'assembly d'avvio.

    public string CorrelationHeaderName { get; set; } = "X-Correlation-Id";  //nome dell'header usato dal CorrelationIdMiddleware. com standart comunemente si chiama proprio 'X-Correlation-Id'

    public double TraceSamplingRatio { get; set; } = 1.0;  //% di trace registrate (1.0 = tutte, 0.2 = 20%). Se la richiesta arriva già con una trace (traceparent) (quindi this req non è root), allora si rispetta la decisione del chiamante (quindi si mantiene quella del padre).

    public List<string> AdditionalSources { get; set; } = [];  //ActivitySource esterne da tracciare, oltre a quelle "SubSnapMS.*", e.g. "Npgsql" (query Postgres), "MongoDB.Driver", "Elastic.Clients.Elasticsearch.ElasticsearchClient"

    public List<string> AdditionalMeters { get; set; } = [];  //meter esterni da raccogliere, oltre a quelli "SubSnapMS.*". Esempio: "Npgsql".

    private static string ResolveEntryAssemblyVersion()
    {
        var assembly = Assembly.GetEntryAssembly();  //l'assembly d'avvio (es. l'api-gateway) e non quello della libreria di building blocks (SubSnapMS.BuildingBlocks.Observability.dll)
        return assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion  //prendo l'InformationalVersion se presente (la versione da 'umani' testuale e.g. 1.4.0-beta)
               ?? assembly?.GetName().Version?.ToString()  //se non c'è l'InformationalVersion, prendo la Version da AssemblyName
               ?? "1.0.0";  //ultimate fallback 
    }
}
