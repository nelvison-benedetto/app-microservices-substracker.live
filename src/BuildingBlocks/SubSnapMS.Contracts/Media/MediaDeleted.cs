using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Media;

[IntegrationEvent(Exchanges.Media, "media.deleted.v1")]
public sealed record MediaDeleted : IntegrationEvent
{
    public required Guid MediaId { get; init; }

    public required Guid UserId { get; init; }

    public required string ObjectKey { get; init; }

    public required DateTimeOffset DeletedOnUtc { get; init; }
}
