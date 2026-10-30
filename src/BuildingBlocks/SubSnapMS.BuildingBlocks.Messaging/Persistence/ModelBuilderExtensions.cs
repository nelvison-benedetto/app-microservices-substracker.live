using Microsoft.EntityFrameworkCore;
using SubSnapMS.BuildingBlocks.Messaging.Inbox;
using SubSnapMS.BuildingBlocks.Messaging.Outbox;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Persistence;

/// <summary>Da chiamare in OnModelCreating del DbContext di ogni servizio che usa Outbox e/o Inbox.</summary>
public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyOutboxConfiguration(this ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

    public static ModelBuilder ApplyInboxConfiguration(this ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
}
