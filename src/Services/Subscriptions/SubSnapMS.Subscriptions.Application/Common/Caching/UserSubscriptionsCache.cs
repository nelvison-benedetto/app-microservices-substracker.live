using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using SubSnapMS.Subscriptions.Application.Common.Results;
using SubSnapMS.Subscriptions.Application.Options;
using SubSnapMS.Subscriptions.Core.Common;

namespace SubSnapMS.Subscriptions.Application.Common.Caching;

//Cache della lista abbonamenti per utente. HybridCache: memoria (L1) + Redis (L2), con protezione dallo "stampede" (richieste simultanee per la stessa chiave → una sola query al database).
//Chiavi con prefisso "subscriptions:": Redis è condiviso, ogni servizio usa solo le proprie chiavi.
public sealed class UserSubscriptionsCache(HybridCache cache, IOptions<SubscriptionCachingOptions> options)
{
    public ValueTask<IReadOnlyList<SubscriptionResult>> GetOrLoadAsync(
        UserId userId,
        Func<CancellationToken, ValueTask<IReadOnlyList<SubscriptionResult>>> load,
        CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(KeyFor(userId), load, EntryOptions(), cancellationToken: cancellationToken);

    public ValueTask InvalidateAsync(UserId userId, CancellationToken cancellationToken) =>
        cache.RemoveAsync(KeyFor(userId), cancellationToken);

    private static string KeyFor(UserId userId) => $"subscriptions:user:{userId.Value}:list";

    private HybridCacheEntryOptions EntryOptions()
    {
        var settings = options.Value;

        return settings.LocalCacheExpiration > TimeSpan.Zero
            ? new HybridCacheEntryOptions
            {
                Expiration = settings.UserSubscriptionsExpiration,
                LocalCacheExpiration = settings.LocalCacheExpiration
            }
            : new HybridCacheEntryOptions
            {
                Expiration = settings.UserSubscriptionsExpiration,
                Flags = HybridCacheEntryFlags.DisableLocalCache   //solo Redis: coerente tra repliche
            };
    }
}