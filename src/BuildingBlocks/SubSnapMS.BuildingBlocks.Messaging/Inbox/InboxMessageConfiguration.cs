using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SubSnapMS.BuildingBlocks.Messaging.Inbox;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>  //
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");  //nome della tabella!

        builder.HasKey(m => new { m.MessageId, m.HandlerName });  //primary key (come faccio sui db) composta

        builder.Property(m => m.MessageId).HasColumnName("message_id").ValueGeneratedNever();  //ValueGeneratedNever() (x aspnetcore considera gia generata se gli passi un uuid (e quindi ne crerebbe un'altra sua), mentre noi vogliamo che il valore provenga dal mex!)

        builder.Property(m => m.HandlerName).HasColumnName("handler_name").HasMaxLength(300).IsRequired();

        builder.Property(m => m.ProcessedOnUtc).HasColumnName("processed_on_utc").IsRequired();

        builder.HasIndex(m => m.ProcessedOnUtc).HasDatabaseName("ix_inbox_messages_processed_on_utc");  //index non univoco sulla data, per la pulizia periodica.
    }
}