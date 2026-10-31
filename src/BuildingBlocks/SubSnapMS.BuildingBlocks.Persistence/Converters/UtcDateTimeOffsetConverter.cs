using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SubSnapMS.BuildingBlocks.Persistence.Converters;

/// <summary>
/// Normalizza ogni DateTimeOffset a offset zero, in scrittura e in lettura.
/// PostgreSQL (timestamptz) salva solo istanti UTC e Npgsql rifiuta offset diversi da zero:
/// così un valore con offset locale non fa fallire il salvataggio e le letture sono sempre coerenti.
/// </summary>
public sealed class UtcDateTimeOffsetConverter() : ValueConverter<DateTimeOffset, DateTimeOffset>(
    value => value.ToUniversalTime(),
    value => value.ToUniversalTime());