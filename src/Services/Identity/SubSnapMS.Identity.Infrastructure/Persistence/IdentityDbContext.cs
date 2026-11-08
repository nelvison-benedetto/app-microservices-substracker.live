using Microsoft.EntityFrameworkCore;
using Npgsql;
using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Messaging.Persistence;
using SubSnapMS.BuildingBlocks.Persistence;
using SubSnapMS.Identity.Core.Deletions;
using SubSnapMS.Identity.Core.Users;
using SubSnapMS.Identity.Core.Users.Exceptions;
using SubSnapMS.Identity.Infrastructure.Persistence.Configurations;

namespace SubSnapMS.Identity.Infrastructure.Persistence;

//Database di Identity (identity_db). È anche l'IUnitOfWork della richiesta: a ogni SaveChangesAsync esegue i domain event, salva tutto in una transazione, traduce gli errori.
public sealed class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options,
    IDomainEventDispatcher domainEventDispatcher) : ServiceDbContext(options, domainEventDispatcher)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserDeletion> UserDeletions => Set<UserDeletion>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // Tabelle outbox_messages e inbox_messages (BuildingBlocks.Messaging)
        modelBuilder.ApplyOutboxConfiguration().ApplyInboxConfiguration();
    }

    //Due registrazioni (o cambi email) simultanei con la stessa email superano entrambe il controllo applicativo: l'indice univoco ferma la seconda, che riceve lo stesso errore di dominio (409).
    protected override Exception? TranslateSaveException(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: UserConfiguration.EmailUniqueIndex
        }
            ? new EmailAlreadyRegisteredException()
            : null;
}