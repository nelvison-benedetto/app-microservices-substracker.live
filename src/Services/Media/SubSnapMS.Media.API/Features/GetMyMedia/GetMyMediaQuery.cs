using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Media.API.Common;
using SubSnapMS.Media.Core.Common;

namespace SubSnapMS.Media.API.Features.GetMyMedia;

public sealed record GetMyMediaQuery(UserId UserId, string? Purpose) : IQuery<IReadOnlyList<MediaFileResponse>>;  //Purpose è il filtro opzionale e.g. 'avatar'