using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Media.Core.MediaFiles.Exceptions;

//Inesistente O di un altro utente: stessa risposta (404), non si rivela l'esistenza
public sealed class MediaFileNotFoundException()
    : NotFoundException("media.not_found", "Media file not found.");