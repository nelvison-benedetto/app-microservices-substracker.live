using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Domain.Exceptions;

/// <summary>Identità valida, ma l'operazione non è permessa (es. account non attivo) (→ 403).</summary>
public class ForbiddenException(string code, string message) : DomainException(code, message);