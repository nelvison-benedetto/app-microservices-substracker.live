using Microsoft.EntityFrameworkCore;
using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Messaging.Persistence;
using SubSnapMS.BuildingBlocks.Persistence;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Infrastructure.Persistence;

/// <summary>
/// Database di Subscriptions (subscriptions_db). È anche l'IUnitOfWork della richiesta:
/// a ogni SaveChangesAsync esegue i domain event (→ Outbox), salva in una transazione, traduce gli errori
/// (version cambiata nel frattempo → ConcurrencyConflictException → 409).
/// </summary>
public sealed class SubscriptionsDbContext(
    DbContextOptions<SubscriptionsDbContext> options,
    IDomainEventDispatcher domainEventDispatcher) : ServiceDbContext(options, domainEventDispatcher)
{
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<SubscriptionHistoryEntry> SubscriptionHistory => Set<SubscriptionHistoryEntry>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SubscriptionsDbContext).Assembly);

        // Tabelle outbox_messages e inbox_messages (BuildingBlocks.Messaging)
        modelBuilder.ApplyOutboxConfiguration().ApplyInboxConfiguration();
    }
}