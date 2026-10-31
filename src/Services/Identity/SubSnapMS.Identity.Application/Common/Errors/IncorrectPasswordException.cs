using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Identity.Application.Common.Errors;

/// <summary>
/// Password attuale errata su un endpoint GIÀ autenticato (cambio email/password, cancellazione).
/// 400 e non 401: un 401 farebbe partire il refresh automatico del frontend.
/// </summary>
public sealed class IncorrectPasswordException()
    : DomainException("auth.incorrect_password", "The current password is incorrect.");