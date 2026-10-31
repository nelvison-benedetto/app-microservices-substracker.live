using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Media.API.Common;
using SubSnapMS.Media.Core.Common;
using SubSnapMS.Media.Core.MediaFiles;

namespace SubSnapMS.Media.API.Features.ConfirmUpload;

public sealed record ConfirmUploadCommand(UserId UserId, MediaFileId MediaFileId) : ICommand<MediaFileResponse>;