using System.Globalization;
using System.Text;

namespace SubSnapMS.Graph.API.Projection;

//Chiave del nodo Service: stesso servizio scritto in modi diversi → stesso nodo.
//"Netflix " / "NETFLIX" → "netflix" · "Caffè Pass" → "caffe pass" · "Disney+" → "disney".
//Limite noto: "Disney+" e "Disney Plus" restano diversi (servirebbe un catalogo di alias).
public static class ServiceKey
{
    public static string From(string name)
    {
        var decomposed = name.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingSpace = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;   // accenti rimossi

            if (char.IsLetterOrDigit(character))
            {
                if (pendingSpace && builder.Length > 0)
                    builder.Append(' ');

                builder.Append(char.ToLowerInvariant(character));
                pendingSpace = false;
            }
            else
            {
                pendingSpace = true;   // punteggiatura e spazi multipli → un solo spazio
            }
        }

        // Nome fatto solo di simboli: chiave non vuota comunque
        return builder.Length > 0 ? builder.ToString() : name.Trim().ToLowerInvariant();
    }
}