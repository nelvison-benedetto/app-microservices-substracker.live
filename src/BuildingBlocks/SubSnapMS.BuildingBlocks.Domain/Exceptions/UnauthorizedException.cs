using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Domain.Exceptions;

/// <summary>Credenziali o token non validi (→ 401).</summary>
public class UnauthorizedException(string code, string message) : DomainException(code, message);