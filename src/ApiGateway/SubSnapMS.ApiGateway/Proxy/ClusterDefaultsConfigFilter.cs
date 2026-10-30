using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace SubSnapMS.ApiGateway.Proxy;

/// <summary>
/// Completa ogni cluster letto da "ReverseProxy:Clusters" con i default della piattaforma.
/// Vale solo per i valori NON impostati: se un cluster li specifica in configurazione, vincono quelli.
/// </summary>
internal sealed class ClusterDefaultsConfigFilter : IProxyConfigFilter
{
    private const string ConsecutiveFailuresThresholdKey = "ConsecutiveFailuresHealthPolicy.Threshold";

    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel)
    {
        var healthCheck = cluster.HealthCheck ?? new HealthCheckConfig();

        // Attivo: YARP chiama /health/ready di ogni destinazione; dopo 3 fallimenti consecutivi la esclude
        var active = healthCheck.Active ?? new ActiveHealthCheckConfig();
        active = active with
        {
            Enabled = active.Enabled ?? true,
            Interval = active.Interval ?? TimeSpan.FromSeconds(15),
            Timeout = active.Timeout ?? TimeSpan.FromSeconds(5),
            Policy = active.Policy ?? "ConsecutiveFailures",
            Path = active.Path ?? "/health/ready"
        };

        // Passivo: osserva il traffico reale; troppi errori di trasporto → destinazione esclusa per 30s
        var passive = healthCheck.Passive ?? new PassiveHealthCheckConfig();
        passive = passive with
        {
            Enabled = passive.Enabled ?? true,
            Policy = passive.Policy ?? "TransportFailureRate",
            ReactivationPeriod = passive.ReactivationPeriod ?? TimeSpan.FromSeconds(30)
        };

        // Timeout: se il servizio non invia dati per 30s la richiesta termina con 504
        var httpRequest = cluster.HttpRequest ?? new ForwarderRequestConfig();
        httpRequest = httpRequest with
        {
            ActivityTimeout = httpRequest.ActivityTimeout ?? TimeSpan.FromSeconds(30)
        };

        var metadata = cluster.Metadata is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(cluster.Metadata);
        metadata.TryAdd(ConsecutiveFailuresThresholdKey, "3");

        return ValueTask.FromResult(cluster with
        {
            HealthCheck = healthCheck with { Active = active, Passive = passive },
            HttpRequest = httpRequest,
            Metadata = metadata
        });
    }

    public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, ClusterConfig? cluster, CancellationToken cancel) =>
        ValueTask.FromResult(route);
}