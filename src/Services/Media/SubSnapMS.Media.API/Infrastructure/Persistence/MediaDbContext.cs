using Microsoft.EntityFrameworkCore;
using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Messaging.Persistence;
using SubSnapMS.BuildingBlocks.Persistence;
using SubSnapMS.Media.Core.MediaFiles;

namespace SubSnapMS.Media.API.Infrastructure.Persistence;

/// <summary>Database di Media (media_db): solo metadati, i file stanno su R2.</summary>
public sealed class MediaDbContext(
    DbContextOptions<MediaDbContext> options,
    IDomainEventDispatcher domainEventDispatcher) : ServiceDbContext(options, domainEventDispatcher)
{
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MediaFileConfiguration());
        modelBuilder.ApplyOutboxConfiguration().ApplyInboxConfiguration();
    }
}