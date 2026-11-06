using Microsoft.EntityFrameworkCore;
using SubSnapMS.BuildingBlocks.Messaging.Inbox;
using SubSnapMS.BuildingBlocks.Messaging.Outbox;

namespace SubSnapMS.BuildingBlocks.Messaging.Persistence;

//🔥🔥punto di ingresso pubblico con un service (ms) aggiunge al poprio db le tabelle 'outbox_massages' e 'inbox_messages'.
//Da chiamare in OnModelCreating del DbContext di ogni servizio che usa Outbox e/o Inbox

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyOutboxConfiguration(this ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    //extension method on ModelBuilder(cioè l'obj con cui EF costruisce il modello in OnModelCreating)

    public static ModelBuilder ApplyInboxConfiguration(this ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
}

