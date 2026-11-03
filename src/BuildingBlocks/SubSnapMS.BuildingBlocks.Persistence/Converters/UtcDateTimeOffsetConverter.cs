using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SubSnapMS.BuildingBlocks.Persistence.Converters;

//Normalizza ogni DateTimeOffset a offset zero, in scrittura e in lettura.
// risolve il problema che e.g. postgresql salva le date in timestamptz(cioè NON in UTC ma with timezone)
//PostgreSQL (timestamptz) ora salva solo istanti UTC e Npgsql rifiuta offset diversi da zero: così un valore con offset locale non fa fallire il salvataggio e le letture sono sempre coerenti.

public sealed class UtcDateTimeOffsetConverter() : ValueConverter<DateTimeOffset, DateTimeOffset>(  //il convertitore non cambia il type, cambia solo il valore
    value => value.ToUniversalTime(),  //in scrittura, normalizza a UTC
    value => value.ToUniversalTime());  //in lettura, normalizza a UTC

//e.g.2026-11-03T12:52:00+01:00  →  2026-11-03T11:52:00+00:00

