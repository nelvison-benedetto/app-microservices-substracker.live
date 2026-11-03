using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.Contracts.Identity;


[IntegrationEvent(Exchanges.Identity, "user.email-changed.v1")]
public sealed record UserEmailChanged : IntegrationEvent
{
    public required Guid UserId { get; init; }

    /// <summary>Nuova email normalizzata (lowercase).</summary>
    public required string NewEmail { get; init; }

    public required DateTimeOffset ChangedOnUtc { get; init; }
}