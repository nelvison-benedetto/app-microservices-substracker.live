using Microsoft.EntityFrameworkCore;
using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Media.API.Infrastructure.Persistence;
using SubSnapMS.Media.Core.MediaFiles.Exceptions;

namespace SubSnapMS.Media.API.Features.DeleteMedia;

//Rimuove i metadati; il file su R2 lo cancella il consumer di media.deleted.v1 dopo il commit
internal sealed class DeleteMediaHandler(MediaDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<DeleteMediaCommand>
{
    public async Task Handle(DeleteMediaCommand command, CancellationToken cancellationToken)
    {
        var file = await dbContext.MediaFiles.FirstOrDefaultAsync(f => f.Id == command.MediaFileId, cancellationToken)
                   ?? throw new MediaFileNotFoundException();

        file.EnsureOwnedBy(command.UserId);

        file.Delete(timeProvider.GetUtcNow());
        dbContext.MediaFiles.Remove(file);
    }
}