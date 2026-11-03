namespace SubSnapMS.Contracts.Media;

public static class MediaPurposes
{
    public const string Avatar = "avatar";
}
//Scopo di un file caricato. Determina anche il prefisso della chiave su R2 ("avatars/..."), stringhe e non enum (per lo stesso motivo di BillingCycles)