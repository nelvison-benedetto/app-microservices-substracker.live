namespace SubSnapMS.Identity.Infrastructure.Background;

/// <summary>Sezione "Identity:UserDeletionMonitor".</summary>
public sealed class UserDeletionMonitorOptions
{
    public const string SectionName = "Identity:UserDeletionMonitor";

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Una saga ancora in corso dopo questo tempo è considerata bloccata.</summary>
    public TimeSpan StuckAfter { get; set; } = TimeSpan.FromHours(24);
}