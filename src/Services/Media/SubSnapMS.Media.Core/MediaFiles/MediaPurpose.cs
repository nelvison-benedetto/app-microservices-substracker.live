using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Media.Core.MediaFiles;

//A cosa serve un file. Determina tipi ammessi, dimensione massima e cartella sullo storage
public enum MediaPurpose
{
    Avatar = 1
}

//Conversione enum ↔ codice neutro. I codici coincidono con Contracts.MediaPurposes e con l'API.
//Il parsing ignora maiuscole/minuscole ("Avatar" = "avatar").
public static class MediaPurposeExtensions
{
    private static readonly Dictionary<string, MediaPurpose> ByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["avatar"] = MediaPurpose.Avatar
    };

    public static IReadOnlyList<string> AllCodes { get; } = ["avatar"];

    public static string ToCode(this MediaPurpose purpose) => purpose switch
    {
        MediaPurpose.Avatar => "avatar",
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Unknown media purpose.")
    };

    //Prima parte della chiave sullo storage (es. "avatars/...")
    public static string ToStorageFolder(this MediaPurpose purpose) => purpose switch
    {
        MediaPurpose.Avatar => "avatars",
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Unknown media purpose.")
    };

    public static bool IsValidCode(string? code) =>
        code is not null && ByCode.ContainsKey(code.Trim());

    public static MediaPurpose Parse(string? code) =>
        code is not null && ByCode.TryGetValue(code.Trim(), out var purpose)
            ? purpose
            : throw new DomainException(
                "media.purpose_invalid",
                $"Purpose must be one of: {string.Join(", ", AllCodes)}.");
}