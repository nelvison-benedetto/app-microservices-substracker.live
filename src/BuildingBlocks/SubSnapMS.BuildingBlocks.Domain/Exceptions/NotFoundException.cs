namespace SubSnapMS.BuildingBlocks.Domain.Exceptions;

public class NotFoundException(string code, string message) : DomainException(code, message);