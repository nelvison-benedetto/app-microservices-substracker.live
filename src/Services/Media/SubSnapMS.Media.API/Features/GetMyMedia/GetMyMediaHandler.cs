using Microsoft.EntityFrameworkCore;
using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Media.API.Common;
using SubSnapMS.Media.API.Infrastructure.Persistence;
using SubSnapMS.Media.API.Infrastructure.Storage;
using SubSnapMS.Media.Core.MediaFiles;

namespace SubSnapMS.Media.API.Features.GetMyMedia;

//Solo file confermati, dal più recente. Le richieste Pending non sono visibili
internal sealed class GetMyMediaHandler(MediaDbContext dbContext, IObjectStorage storage)
    : IQueryHandler<GetMyMediaQuery, IReadOnlyList<MediaFileResponse>>
{
    public async Task<IReadOnlyList<MediaFileResponse>> Handle(GetMyMediaQuery query, CancellationToken cancellationToken)
    {
        var files = dbContext.MediaFiles
            .AsNoTracking()
            .Where(f => f.UserId == query.UserId && f.Status == MediaFileStatus.Uploaded);

        if (query.Purpose is not null)
        {
            var purpose = MediaPurposeExtensions.Parse(query.Purpose);
            files = files.Where(f => f.Purpose == purpose);
        }

        var result = await files
            .OrderByDescending(f => f.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        return result.Select(f => MediaFileResponse.From(f, storage)).ToList();
    }
}