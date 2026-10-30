using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Inbox;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        // Chiave composta: lo stesso messaggio può essere processato da più handler diversi
        builder.HasKey(m => new { m.MessageId, m.HandlerName });

        builder.Property(m => m.MessageId).HasColumnName("message_id").ValueGeneratedNever();
        builder.Property(m => m.HandlerName).HasColumnName("handler_name").HasMaxLength(300).IsRequired();
        builder.Property(m => m.ProcessedOnUtc).HasColumnName("processed_on_utc").IsRequired();

        builder.HasIndex(m => m.ProcessedOnUtc).HasDatabaseName("ix_inbox_messages_processed_on_utc");
    }
}