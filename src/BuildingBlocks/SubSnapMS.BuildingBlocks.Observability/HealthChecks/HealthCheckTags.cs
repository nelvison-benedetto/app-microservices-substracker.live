using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Observability.HealthChecks;

/// <summary>
/// Tag per classificare gli health check.
/// Live  = il processo è vivo? Se fallisce, il container va riavviato.
/// Ready = può ricevere traffico? (DB, RabbitMQ raggiungibili). Se fallisce, va escluso dal traffico.
/// </summary>
public static class HealthCheckTags
{
    public const string Live = "live";
    public const string Ready = "ready";
}
