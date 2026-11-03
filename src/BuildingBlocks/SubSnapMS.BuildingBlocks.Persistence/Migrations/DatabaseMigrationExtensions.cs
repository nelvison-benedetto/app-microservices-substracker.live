using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SubSnapMS.BuildingBlocks.Persistence.Migrations;

//Strategia di migrazione della piattaforma:
//- PRODUZIONE: un container dedicato avviato con "--migrate" applica le migrations e termina, PRIMA che partano le repliche del servizio (che non migrano mai, per evitare corse tra repliche);
//- SVILUPPO: "Database:MigrateOnStartup": true → il servizio migra all'avvio e poi parte normalmente.


//deploy → container "subscriptions --migrate" (applica, termina con codice 0) → solo se è andato bene: partono le repliche di subscriptions
public static class DatabaseMigrationExtensions
{
    public const string MigrateArgument = "--migrate";
    public const string MigrateOnStartupKey = "Database:MigrateOnStartup";

    /// <summary>
    /// Applica le migrations se richiesto. Restituisce true se il processo è stato avviato SOLO per migrare
    /// (argomento --migrate): in quel caso Program.cs deve terminare invece di avviare il servizio.
    /// </summary>
    public static async Task<bool> ApplyMigrationsAsync<TContext>(this IHost host, string[] args)
        where TContext : DbContext
    {
        var migrateOnly = args.Contains(MigrateArgument, StringComparer.OrdinalIgnoreCase);

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var migrateOnStartup = bool.TryParse(configuration[MigrateOnStartupKey], out var enabled) && enabled;

        if (!migrateOnly && !migrateOnStartup)
            return false;

        await using var scope = host.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("SubSnapMS.BuildingBlocks.Persistence.Migrations");
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("{DbContext}: database is up to date", typeof(TContext).Name);
        }
        else
        {
            logger.LogInformation("{DbContext}: applying {Count} migrations: {Migrations}",
                typeof(TContext).Name, pending.Count, string.Join(", ", pending));

            await dbContext.Database.MigrateAsync();

            logger.LogInformation("{DbContext}: migrations applied", typeof(TContext).Name);
        }

        return migrateOnly;
    }
}