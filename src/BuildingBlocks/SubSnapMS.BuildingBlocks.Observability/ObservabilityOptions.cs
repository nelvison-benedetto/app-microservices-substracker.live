using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Observability;

/// <summary>
/// Impostazioni di osservabilità. Si possono sovrascrivere dalla sezione "Observability"
/// della configurazione (appsettings / variabili d'ambiente Observability__*).
/// </summary>
public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    /// <summary>
    /// Prefisso comune di ActivitySource e Meter della piattaforma.
    /// Un servizio che crea new ActivitySource("SubSnapMS.Messaging") viene tracciato in automatico.
    /// </summary>
    public const string SourcePrefix = "SubSnapMS";

    /// <summary>Nome con cui il servizio appare in trace, metriche e log (es. "api-gateway").</summary>
    public string ServiceName { get; set; } = "unknown-service";

    /// <summary>Versione del servizio. Default: InformationalVersion dell'assembly d'avvio.</summary>
    public string ServiceVersion { get; set; } = ResolveEntryAssemblyVersion();

    /// <summary>Header di risposta che riporta il TraceId al client.</summary>
    public string CorrelationHeaderName { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// Percentuale di trace registrate (1.0 = tutte, 0.2 = 20%).
    /// Se la richiesta arriva già con una trace (traceparent), si rispetta la decisione del chiamante.
    /// </summary>
    public double TraceSamplingRatio { get; set; } = 1.0;

    private static string ResolveEntryAssemblyVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        return assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? assembly?.GetName().Version?.ToString()
               ?? "1.0.0";
    }
}
