using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Media.Core.MediaFiles.Exceptions;

/// <summary>Inesistente O di un altro utente: stessa risposta (404), non si rivela l'esistenza.</summary>
public sealed class MediaFileNotFoundException()
    : NotFoundException("media.not_found", "Media file not found.");