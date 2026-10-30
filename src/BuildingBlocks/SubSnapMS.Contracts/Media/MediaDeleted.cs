using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Media;

/// <summary>Un file è stato eliminato (dall'utente o dalla saga di cancellazione account).</summary>
[IntegrationEvent(Exchanges.Media, "media.deleted.v1")]
public sealed record MediaDeleted : IntegrationEvent
{
    public required Guid MediaId { get; init; }

    public required Guid UserId { get; init; }

    public required string ObjectKey { get; init; }

    /// <summary>Uno dei valori di <see cref="MediaPurposes"/>.</summary>
    public required string Purpose { get; init; }

    public required DateTimeOffset DeletedOnUtc { get; init; }
}