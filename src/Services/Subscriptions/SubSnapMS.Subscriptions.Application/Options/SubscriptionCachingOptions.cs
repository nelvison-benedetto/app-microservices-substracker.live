namespace SubSnapMS.Subscriptions.Application.Options;

//Sezione "Subscriptions:Caching"
public sealed class SubscriptionCachingOptions
{
    public const string SectionName = "Subscriptions:Caching";

    //Durata massima della lista in cache (Redis). Di norma viene invalidata prima, a ogni modifica
    public TimeSpan UserSubscriptionsExpiration { get; set; } = TimeSpan.FromMinutes(10);

    //Durata della copia in memoria di ogni replica (L1). L'invalidazione pulisce L1 solo sulla replica che ha gestito la modifica: con più repliche le altre possono servire dati vecchi fino a questa durata.
    //00:00:00 = niente L1, solo Redis (consigliato con più repliche).
    public TimeSpan LocalCacheExpiration { get; set; } = TimeSpan.FromMinutes(1);
}

