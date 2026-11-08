namespace SubSnapMS.Identity.Infrastructure.Background;

//Sezione "Identity:UserDeletionMonitor"
public sealed class UserDeletionMonitorOptions
{
    public const string SectionName = "Identity:UserDeletionMonitor";

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);

    //Una saga ancora in corso dopo questo tempo è considerata bloccata
    public TimeSpan StuckAfter { get; set; } = TimeSpan.FromHours(24);
}