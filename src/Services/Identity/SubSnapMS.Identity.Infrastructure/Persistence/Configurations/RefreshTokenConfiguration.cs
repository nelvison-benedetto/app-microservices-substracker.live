using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubSnapMS.Identity.Core.RefreshTokens;
using SubSnapMS.Identity.Core.Users;
using SubSnapMS.Identity.Infrastructure.Persistence.Converters;

namespace SubSnapMS.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <summary>FK "shadow": il dominio non ha RefreshToken.UserId (si accede sempre tramite User).</summary>
    public const string UserIdProperty = "UserId";

    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasConversion(IdentityValueConverters.RefreshTokenIdConverter)
            .ValueGeneratedNever();

        builder.Property<UserId>(UserIdProperty)
            .HasColumnName("user_id")
            .HasConversion(IdentityValueConverters.UserIdConverter)
            .IsRequired();

        // Lookup del refresh per uguaglianza sull'hash SHA-256
        builder.Property(t => t.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(RefreshToken.TokenHashMaxLength)
            .IsRequired();

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_refresh_tokens_token_hash");

        builder.Property(t => t.FamilyId).HasColumnName("family_id").IsRequired();
        builder.Property(t => t.CreatedAtUtc).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.ExpiresAtUtc).HasColumnName("expires_at").IsRequired();
        builder.Property(t => t.RevokedAtUtc).HasColumnName("revoked_at");

        builder.Property(t => t.RevocationReason)
            .HasColumnName("revocation_reason")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(t => t.ReplacedByTokenId)
            .HasColumnName("replaced_by_token_id")
            .HasConversion(IdentityValueConverters.RefreshTokenIdConverter);
    }
}