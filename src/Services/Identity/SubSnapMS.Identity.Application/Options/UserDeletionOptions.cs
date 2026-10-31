using SubSnapMS.Contracts;

namespace SubSnapMS.Identity.Application.Options;

/// <summary>Sezione "Identity:UserDeletion".</summary>
public sealed class UserDeletionOptions
{
    public const string SectionName = "Identity:UserDeletion";

    /// <summary>Servizi che POSSIEDONO dati dell'utente: la saga attende un UserDataDeleted da ognuno.</summary>
    public static readonly string[] DefaultExpectedServices = [ServiceNames.Subscriptions, ServiceNames.Media];

    public List<string> ExpectedServices { get; set; } = [];
}