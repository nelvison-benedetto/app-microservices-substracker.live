using SubSnapMS.Subscriptions.Core.Common;

namespace SubSnapMS.Subscriptions.Application.Common.Caching;

//Marcatore dei command che modificano la lista abbonamenti di un utente: dopo il commit, UserSubscriptionsCacheInvalidationBehavior ne invalida la cache.
public interface IInvalidatesUserSubscriptions
{
    UserId UserId { get; }
}