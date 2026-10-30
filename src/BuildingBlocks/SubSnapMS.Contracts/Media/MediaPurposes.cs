using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Media;

/// <summary>
/// Scopo di un file caricato. Determina anche il prefisso della chiave su R2 ("avatars/...").
/// Stringhe e non enum, per lo stesso motivo di BillingCycles.
/// </summary>
public static class MediaPurposes
{
    public const string Avatar = "avatar";
}