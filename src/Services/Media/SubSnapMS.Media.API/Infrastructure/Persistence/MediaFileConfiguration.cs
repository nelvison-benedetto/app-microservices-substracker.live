using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubSnapMS.Media.Core.Common;
using SubSnapMS.Media.Core.MediaFiles;

namespace SubSnapMS.Media.API.Infrastructure.Persistence;

internal sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("media_files");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new MediaFileId(value))
            .ValueGeneratedNever();

        builder.Property(f => f.UserId)
            .HasColumnName("user_id")
            .HasConversion(id => id.Value, value => new UserId(value))
            .IsRequired();

        builder.Property(f => f.Purpose)
            .HasColumnName("purpose")
            .HasConversion(purpose => purpose.ToCode(), code => MediaPurposeExtensions.Parse(code))
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(f => f.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(f => f.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        builder.Property(f => f.SizeBytes).HasColumnName("size_bytes").IsRequired();

        builder.Property(f => f.ObjectKey).HasColumnName("object_key").HasMaxLength(512).IsRequired();
        builder.HasIndex(f => f.ObjectKey).IsUnique().HasDatabaseName("ux_media_files_object_key");

        builder.Property(f => f.CreatedAtUtc).HasColumnName("created_at").IsRequired();
        builder.Property(f => f.UploadExpiresAtUtc).HasColumnName("upload_expires_at").IsRequired();
        builder.Property(f => f.UploadedAtUtc).HasColumnName("uploaded_at");

        // "I miei file di tipo X" e "richieste Pending scadute" (pulizia)
        builder.HasIndex(f => new { f.UserId, f.Purpose, f.Status }).HasDatabaseName("ix_media_files_user_purpose_status");
        builder.HasIndex(f => new { f.Status, f.UploadExpiresAtUtc }).HasDatabaseName("ix_media_files_status_upload_expires_at");

        // Conferme concorrenti dello stesso file → la seconda fallisce (409)
        builder.Property<uint>("Version").IsRowVersion();
    }
}