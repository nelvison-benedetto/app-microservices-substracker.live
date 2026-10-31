namespace SubSnapMS.Media.Core.MediaFiles;

/// <summary>Salvato come stringa ("Pending" / "Uploaded"): leggibile nel database.</summary>
public enum MediaFileStatus
{
    /// <summary>URL di upload emesso, file non ancora confermato. Invisibile all'utente e agli altri servizi.</summary>
    Pending = 1,

    /// <summary>File verificato sullo storage e pubblicato sul CDN.</summary>
    Uploaded = 2
}