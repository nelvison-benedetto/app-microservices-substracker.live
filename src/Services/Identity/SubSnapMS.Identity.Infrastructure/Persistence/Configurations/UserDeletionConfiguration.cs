using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubSnapMS.Identity.Core.Deletions;
using SubSnapMS.Identity.Infrastructure.Persistence.Converters;

namespace SubSnapMS.Identity.Infrastructure.Persistence.Configurations;

//Stato della saga di cancellazione. Resta dopo la rimozione dell'utente come audit: contiene solo l'id e le date, nessun dato personale.
internal sealed class UserDeletionConfiguration : IEntityTypeConfiguration<UserDeletion>
{
    public void Configure(EntityTypeBuilder<UserDeletion> builder)
    {
        builder.ToTable("user_deletions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("user_id")
            .HasConversion(IdentityValueConverters.UserIdConverter)
            .ValueGeneratedNever();

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(d => d.RequestedAtUtc).HasColumnName("requested_at").IsRequired();
        builder.Property(d => d.CompletedAtUtc).HasColumnName("completed_at");

        // Liste di stringhe → colonne text[] di Postgres (primitive collections di EF Core)
        builder.PrimitiveCollection(d => d.ExpectedServices)
            .HasColumnName("expected_services")
            .HasField("_expectedServices")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.PrimitiveCollection(d => d.ConfirmedServices)
            .HasColumnName("confirmed_services")
            .HasField("_confirmedServices")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        //Il monitor cerca le saghe in corso più vecchie di una soglia
        builder.HasIndex(d => new { d.Status, d.RequestedAtUtc })
            .HasDatabaseName("ix_user_deletions_status_requested_at");

        builder.Property<uint>("Version").IsRowVersion();
    }
}