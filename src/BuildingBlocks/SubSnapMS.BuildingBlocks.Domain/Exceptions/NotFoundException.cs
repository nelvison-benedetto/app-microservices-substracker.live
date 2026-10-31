using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Domain.Exceptions;

/// <summary>La risorsa richiesta non esiste (o non appartiene all'utente) (→ 404).</summary>
public class NotFoundException(string code, string message) : DomainException(code, message);