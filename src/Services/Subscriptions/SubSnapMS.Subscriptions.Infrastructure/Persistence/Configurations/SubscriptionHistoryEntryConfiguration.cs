using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubSnapMS.Subscriptions.Core.Subscriptions;
using SubSnapMS.Subscriptions.Infrastructure.Persistence.Converters;

namespace SubSnapMS.Subscriptions.Infrastructure.Persistence.Configurations;

internal sealed class SubscriptionHistoryEntryConfiguration : IEntityTypeConfiguration<SubscriptionHistoryEntry>
{
    /// <summary>FK "shadow": il dominio non ha HistoryEntry.SubscriptionId (si passa sempre dall'aggregate).</summary>
    public const string SubscriptionIdProperty = "SubscriptionId";

    public void Configure(EntityTypeBuilder<SubscriptionHistoryEntry> builder)
    {
        builder.ToTable("subscription_history");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(SubscriptionsValueConverters.HistoryEntryIdConverter)
            .ValueGeneratedNever();   // generato nel dominio → inserito come nuovo quando compare nella collezione

        builder.Property<SubscriptionId>(SubscriptionIdProperty)
            .HasColumnName("subscription_id")
            .HasConversion(SubscriptionsValueConverters.SubscriptionIdConverter)
            .IsRequired();

        builder.Property(e => e.Action)
            .HasColumnName("action")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at").IsRequired();

        // Campi cambiati → una colonna jsonb: [{"field":"amount","oldValue":"9.99","newValue":"12.99"}, ...]
        builder.OwnsMany(e => e.Changes, changes =>
        {
            changes.ToJson("changes");
            changes.Property(c => c.Field).HasJsonPropertyName("field");
            changes.Property(c => c.OldValue).HasJsonPropertyName("oldValue");
            changes.Property(c => c.NewValue).HasJsonPropertyName("newValue");
        });

        builder.Navigation(e => e.Changes)
            .HasField("_changes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Dettaglio: storico di un abbonamento, dal più recente
        builder.HasIndex(SubscriptionIdProperty, nameof(SubscriptionHistoryEntry.OccurredAtUtc))
            .HasDatabaseName("ix_subscription_history_subscription_id_occurred_at");
    }
}