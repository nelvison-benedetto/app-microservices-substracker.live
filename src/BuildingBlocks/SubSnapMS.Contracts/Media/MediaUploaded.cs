using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Media;

/// <summary>
/// Upload confermato (il file è già su R2). Esempio di consumer: un servizio che genera
/// miniature, oppure Identity se un giorno tenesse l'URL dell'avatar nel profilo.
/// </summary>
[IntegrationEvent(Exchanges.Media, "media.uploaded.v1")]
public sealed record MediaUploaded : IntegrationEvent
{
    public required Guid MediaId { get; init; }

    public required Guid UserId { get; init; }

    /// <summary>Chiave dell'oggetto su R2, es. "avatars/{guid}-foto.png".</summary>
    public required string ObjectKey { get; init; }

    /// <summary>URL pubblico sul CDN.</summary>
    public required string PublicUrl { get; init; }

    public required string ContentType { get; init; }

    public required long SizeBytes { get; init; }

    /// <summary>Scopo del file, es. "avatars". Stringa libera per lo stesso motivo di BillingCycles.</summary>
    public required string Purpose { get; init; }

    public required DateTimeOffset UploadedOnUtc { get; init; }
}
