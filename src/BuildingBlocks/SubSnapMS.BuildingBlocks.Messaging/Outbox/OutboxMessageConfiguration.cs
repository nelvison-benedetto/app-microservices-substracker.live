using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    // Nomi di colonna espliciti: l'OutboxProcessor li usa in SQL puro
    internal const string TableName = "outbox_messages";
    internal const string ProcessedOnUtcColumn = "processed_on_utc";
    internal const string AttemptsColumn = "attempts";
    internal const string OccurredOnUtcColumn = "occurred_on_utc";

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(m => m.EventName).HasColumnName("event_name").HasMaxLength(200).IsRequired();
        builder.Property(m => m.Exchange).HasColumnName("exchange").HasMaxLength(200).IsRequired();
        builder.Property(m => m.Payload).HasColumnName("payload").IsRequired();
        builder.Property(m => m.TraceParent).HasColumnName("trace_parent").HasMaxLength(100);
        builder.Property(m => m.TraceState).HasColumnName("trace_state").HasMaxLength(512);
        builder.Property(m => m.OccurredOnUtc).HasColumnName(OccurredOnUtcColumn).IsRequired();
        builder.Property(m => m.ProcessedOnUtc).HasColumnName(ProcessedOnUtcColumn);
        builder.Property(m => m.Attempts).HasColumnName(AttemptsColumn).IsRequired();
        builder.Property(m => m.LastError).HasColumnName("last_error").HasMaxLength(2000);

        // Indice parziale: copre solo i messaggi ancora da pubblicare, quindi resta piccolo
        builder.HasIndex(m => m.OccurredOnUtc)
            .HasDatabaseName("ix_outbox_messages_pending")
            .HasFilter($"{ProcessedOnUtcColumn} IS NULL");
    }
}