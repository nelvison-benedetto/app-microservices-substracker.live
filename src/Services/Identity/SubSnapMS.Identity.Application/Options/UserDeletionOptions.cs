using SubSnapMS.Contracts;

namespace SubSnapMS.Identity.Application.Options;

//Sezione "Identity:UserDeletion"
public sealed class UserDeletionOptions
{
    public const string SectionName = "Identity:UserDeletion";

    //Servizi che POSSIEDONO dati dell'utente: la saga attende un UserDataDeleted da ognuno
    public static readonly string[] DefaultExpectedServices = [ServiceNames.Subscriptions, ServiceNames.Media];

    public List<string> ExpectedServices { get; set; } = [];
}