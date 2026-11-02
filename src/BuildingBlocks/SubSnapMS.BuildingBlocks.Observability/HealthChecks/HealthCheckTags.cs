using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Observability.HealthChecks;

// Tag per classificare gli health check.
// Live  = il processo è vivo? Se fallisce, il container va riavviato
// Ready = può ricevere traffico? (DB, RabbitMQ raggiungibili). Se fallisce, va escluso dal traffico
public static class HealthCheckTags
{
    public const string Live = "live";  //non controlla i dbs se sono ready!
    public const string Ready = "ready";
}
