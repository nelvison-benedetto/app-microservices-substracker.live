using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Application.Abstractions.Persistence;
using SubSnapMS.Identity.Application.Abstractions.Security;
using SubSnapMS.Identity.Core.Users;
using SubSnapMS.Identity.Core.Users.Exceptions;

namespace SubSnapMS.Identity.Application.Features.Users.Register;

//Crea l'utente. UserRegisteredDomainEvent → (al salvataggio) UserRegistered nell'Outbox → RabbitMQ.
//La registrazione non apre una sessione: il frontend fa poi il login (come nel monolite).
internal sealed class RegisterUserHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : ICommandHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);

        // Controllo "amichevole"; la garanzia vera è l'indice univoco (Infrastructure → stessa eccezione)
        if (await users.EmailExistsAsync(email, cancellationToken))
            throw new EmailAlreadyRegisteredException();

        var user = User.Register(email, PasswordHash.Create(passwordHasher.Hash(command.Password)), timeProvider.GetUtcNow());
        users.Add(user);

        return new RegisterUserResult(user.Id.Value, user.Email.Value);
    }
}