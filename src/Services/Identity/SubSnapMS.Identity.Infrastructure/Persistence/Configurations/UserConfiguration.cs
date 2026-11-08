using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubSnapMS.Identity.Core.Users;
using SubSnapMS.Identity.Infrastructure.Persistence.Converters;

namespace SubSnapMS.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public const string EmailUniqueIndex = "ux_users_email";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasConversion(IdentityValueConverters.UserIdConverter)
            .ValueGeneratedNever();   // generato nel dominio

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasConversion(IdentityValueConverters.EmailConverter)
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName(EmailUniqueIndex);

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasConversion(IdentityValueConverters.PasswordHashConverter)
            .HasMaxLength(PasswordHash.MaxLength)
            .IsRequired();

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(u => u.CreatedAtUtc).HasColumnName("created_at").IsRequired();
        builder.Property(u => u.UpdatedAtUtc).HasColumnName("updated_at").IsRequired();
        builder.Property(u => u.LastLoginAtUtc).HasColumnName("last_login_at");
        builder.Property(u => u.DeletionRequestedAtUtc).HasColumnName("deletion_requested_at");

        //Optimistic concurrency: xmin è una colonna di SISTEMA di Postgres, aggiornata a ogni modifica.
        //Nessuna colonna da creare o gestire; un salvataggio concorrente → ConcurrencyConflictException (409).
        builder.Property<uint>("Version").IsRowVersion();

        //Sessioni: figli dell'aggregate, eliminati con l'utente e quando escono dalla collezione (orphan delete)
        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(RefreshTokenConfiguration.UserIdProperty)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.RefreshTokens)
            .HasField("_refreshTokens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}