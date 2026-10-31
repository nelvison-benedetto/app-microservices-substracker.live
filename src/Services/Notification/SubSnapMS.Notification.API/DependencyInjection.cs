using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using SubSnapMS.BuildingBlocks.Observability.HealthChecks;
using SubSnapMS.Contracts;
using SubSnapMS.Notification.API.Configuration;
using SubSnapMS.Notification.API.Delivery;
using SubSnapMS.Notification.API.Email;
using SubSnapMS.Notification.API.Persistence;
using SubSnapMS.Notification.API.Renewals;

namespace SubSnapMS.Notification.API;

public static class DependencyInjection
{
    private const string ConnectionStringName = "NotificationDb";

    public static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        // ── Opzioni ───────────────────────────────────────────────────────────────
        services.AddOptions<NotificationOptions>()
            .Bind(configuration.GetSection(NotificationOptions.SectionName))
            .Validate(o => o.Reminders.DaysBefore is >= 0 and <= 30, "Notification:Reminders:DaysBefore must be between 0 and 30.")
            .Validate(o => o.Reminders.SendFromHour is >= 0 and <= 23 && o.Reminders.SendUntilHour is >= 1 and <= 24
                           && o.Reminders.SendFromHour < o.Reminders.SendUntilHour,
                "Notification:Reminders: SendFromHour must be lower than SendUntilHour (0–24).")
            .Validate(o => o.Reminders.CheckInterval >= TimeSpan.FromMinutes(1), "Notification:Reminders:CheckInterval must be at least 1 minute.")
            .Validate(o => o.Reminders.BatchSize > 0, "Notification:Reminders:BatchSize must be positive.")
            .Validate(o => TimeZoneInfo.TryFindSystemTimeZoneById(o.Reminders.TimeZone, out _), "Notification:Reminders:TimeZone is not a valid time zone.")
            .Validate(o => o.Delivery.MaxAttempts > 0, "Notification:Delivery:MaxAttempts must be positive.")
            .ValidateOnStart();

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.FromAddress), "Email:FromAddress is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Smtp.Host), "Email:Smtp:Host is required.")
            .Validate(o => Uri.TryCreate(o.AppBaseUrl, UriKind.Absolute, out _), "Email:AppBaseUrl must be an absolute URL.")
            .ValidateOnStart();

        // ── MongoDB ───────────────────────────────────────────────────────────────
        // Password inclusa nella connection string: in produzione da ConnectionStrings__NotificationDb
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing.");

        var mongoUrl = new MongoUrl(connectionString);
        if (string.IsNullOrWhiteSpace(mongoUrl.DatabaseName))
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' must include the database name (…/notification_db).");

        MongoSerialization.Register();

        services.AddSingleton<IMongoClient>(_ =>
        {
            var settings = MongoClientSettings.FromUrl(mongoUrl);
            settings.ApplicationName = ServiceNames.Notification;
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);   // default 30 s: troppo per health check e retry
            return new MongoClient(settings);
        });
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName));
        services.AddSingleton<NotificationDb>();
        services.AddSingleton<RecipientStore>();
        services.AddSingleton<SubscriptionStore>();
        services.AddSingleton<NotificationLog>();
        services.AddHostedService<MongoIndexInitializer>();

        services.AddHealthChecks()
            .AddCheck<MongoHealthCheck>("mongo", tags: [HealthCheckTags.Ready]);

        // ── Email ─────────────────────────────────────────────────────────────────
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<EmailTemplates>();
        services.AddSingleton<NotificationSender>();

        // ── Promemoria di rinnovo ─────────────────────────────────────────────────
        services.AddSingleton<LocalCalendar>();
        services.AddHostedService<RenewalReminderService>();

        return services;
    }
}