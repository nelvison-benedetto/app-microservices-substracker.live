namespace SubSnapMS.BuildingBlocks.Domain.Exceptions;

//credenziali o token non validi (→ 401)
public class UnauthorizedException(string code, string message) : DomainException(code, message);