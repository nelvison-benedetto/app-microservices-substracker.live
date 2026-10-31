using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Media.Core.Common;
using SubSnapMS.Media.Core.MediaFiles;

namespace SubSnapMS.Media.API.Features.DeleteMedia;

public sealed record DeleteMediaCommand(UserId UserId, MediaFileId MediaFileId) : ICommand;