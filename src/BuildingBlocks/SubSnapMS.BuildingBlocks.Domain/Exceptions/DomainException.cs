using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Domain.Exceptions;

/// <summary>
/// Una regola di business è stata violata (→ 400).
/// Code: identificativo stabile e leggibile da una macchina (es. "email.invalid"),
/// che il frontend può usare per tradurre il messaggio con next-intl.
/// Le sottoclassi indicano categorie con uno status code diverso.
/// </summary>
public class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}