using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Domain.Exceptions;

/// <summary>L'operazione è in conflitto con lo stato attuale (es. email già registrata) (→ 409).</summary>
public class ConflictException(string code, string message) : DomainException(code, message);