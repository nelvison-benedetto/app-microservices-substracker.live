namespace SubSnapMS.Media.Core.MediaFiles;

//Salvato come stringa ("Pending" / "Uploaded"): leggibile nel database
public enum MediaFileStatus
{
    //URL di upload emesso, file non ancora confermato. Invisibile all'utente e agli altri servizi
    Pending = 1,

    //File verificato sullo storage e pubblicato sul CDN
    Uploaded = 2
}