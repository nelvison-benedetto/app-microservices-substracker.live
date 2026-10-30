using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Diagnostics;

/// <summary>
/// Span di publish e consume. Il nome inizia con "SubSnapMS." quindi Observability
/// lo registra in automatico (AddSource("SubSnapMS.*")).
/// </summary>
internal static class MessagingDiagnostics
{
    public const string ActivitySourceName = "SubSnapMS.Messaging";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>Ricostruisce il contesto della trace da un traceparent W3C (default se assente o non valido).</summary>
    public static ActivityContext ParseParent(string? traceParent, string? traceState) =>
        traceParent is not null && ActivityContext.TryParse(traceParent, traceState, out var context)
            ? context
            : default;

    public static void RecordError(Activity? activity, Exception exception)
    {
        if (activity is null) return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddException(exception);
    }
}
