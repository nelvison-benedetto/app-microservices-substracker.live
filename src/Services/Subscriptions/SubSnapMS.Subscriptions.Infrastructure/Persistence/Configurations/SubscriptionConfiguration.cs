using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubSnapMS.Subscriptions.Core.Subscriptions;
using SubSnapMS.Subscriptions.Infrastructure.Persistence.Converters;

namespace SubSnapMS.Subscriptions.Infrastructure.Persistence.Configurations;

internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasConversion(SubscriptionsValueConverters.SubscriptionIdConverter)
            .ValueGeneratedNever();   //xk l'id lo crea il dominio e.g.SubscriptionId.New() ! il db non deve generarlo.

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .HasConversion(SubscriptionsValueConverters.UserIdConverter)
            .IsRequired();

        //Version gestita dal dominio (+1 a ogni modifica) e usata come concurrency token:
        //🔥🔥UPDATE ... WHERE id = @id AND version = @versioneLetta se return 0 rows ALLORA QUALCUNO HA MODIFICATO LA ROW NEL FRATTEMPO! quindi error 409 e il client riprova
        builder.Property(s => s.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAtUtc).HasColumnName("updated_at").IsRequired();

        builder.Ignore(s => s.MonthlyCost);  //questo viene calcolato dal dominio, EF la esclude proprio no column nella tab

        //Value Object annidati → colonne della stessa tabella (complex types: niente join)
        builder.ComplexProperty(s => s.Details, details =>
        {  //nel dominio Subscription contiene un Value Object SubscriptionDetails, che ha sua volta contiene altri Value Object 

            details.Property(d => d.Name)
                .HasColumnName("name")
                .HasMaxLength(SubscriptionDetails.NameMaxLength)
                .IsRequired();

            details.Property(d => d.Category)
                .HasColumnName("category")
                .HasMaxLength(SubscriptionDetails.CategoryMaxLength);

            details.Property(d => d.BillingCycle)
                .HasColumnName("billing_cycle")
                .HasConversion(SubscriptionsValueConverters.BillingCycleConverter)
                .HasMaxLength(16)
                .IsRequired();

            details.ComplexProperty(d => d.Price, price =>
            {
                price.Property(p => p.Amount)
                    .HasColumnName("amount")
                    .HasPrecision(12, Money.Decimals)
                    .IsRequired();

                price.Property(p => p.Currency)
                    .HasColumnName("currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            details.ComplexProperty(d => d.Period, period =>
            {
                period.Property(p => p.StartDate).HasColumnName("start_date").IsRequired();
                period.Property(p => p.EndDate).HasColumnName("end_date");
            });
        });


        //Lista dell'utente, già ordinata dal più recente
        builder.HasIndex(s => new { s.UserId, s.CreatedAtUtc })
            .HasDatabaseName("ix_subscriptions_user_id_created_at")
            .IsDescending(false, true);

        //Storico: figlio append-only. Mai caricato con l'aggregate; le voci nuove vengono inserite.
        //Cascade NEL DATABASE: cancellare un abbonamento (anche con ExecuteDelete) elimina lo storico.

        //relazione 1:N
        builder.HasMany(s => s.HistoryEntries)
            .WithOne()
            .HasForeignKey(SubscriptionHistoryEntryConfiguration.SubscriptionIdProperty)  //🔥🔥SHADOW KEY
            .OnDelete(DeleteBehavior.Cascade);


        builder.Navigation(s => s.HistoryEntries)  //
            .HasField("_historyEntries")  //dice a EF quale campo privato contiene davvero i dati
            .UsePropertyAccessMode(PropertyAccessMode.Field);  //EF legge e scrive direttamente il campo
        //EF legge e scrivo il campo privato _changes, mente il resto del code vede solo una lista in sola lettura
    }
}