using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Media.API.Common;
using SubSnapMS.Media.Core.Common;

namespace SubSnapMS.Media.API.Features.GetMyMedia;

/// <param name="Purpose">Filtro opzionale (es. "avatar").</param>
public sealed record GetMyMediaQuery(UserId UserId, string? Purpose) : IQuery<IReadOnlyList<MediaFileResponse>>;