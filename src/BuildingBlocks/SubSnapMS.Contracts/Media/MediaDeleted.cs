using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.Contracts.Media;


[IntegrationEvent(Exchanges.Media, "media.deleted.v1")]
public sealed record MediaDeleted : IntegrationEvent
{
    public required Guid MediaId { get; init; }

    public required Guid UserId { get; init; }

    public required string ObjectKey { get; init; }

    public required string Purpose { get; init; }

    public required DateTimeOffset DeletedOnUtc { get; init; }

}