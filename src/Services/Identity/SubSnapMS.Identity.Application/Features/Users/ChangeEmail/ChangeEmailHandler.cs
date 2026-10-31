using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Application.Abstractions.Persistence;
using SubSnapMS.Identity.Application.Abstractions.Security;
using SubSnapMS.Identity.Application.Common.Security;
using SubSnapMS.Identity.Core.Users;
using SubSnapMS.Identity.Core.Users.Exceptions;

namespace SubSnapMS.Identity.Application.Features.Users.ChangeEmail;

internal sealed class ChangeEmailHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : ICommandHandler<ChangeEmailCommand>
{
    public async Task Handle(ChangeEmailCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken)
                   ?? throw new UserNotFoundException();

        CurrentPasswordGuard.Ensure(passwordHasher, user, command.CurrentPassword);

        var newEmail = Email.Create(command.NewEmail);
        if (newEmail == user.Email)
            return;

        if (await users.EmailExistsAsync(newEmail, cancellationToken))
            throw new EmailAlreadyRegisteredException();

        user.ChangeEmail(newEmail, timeProvider.GetUtcNow());
    }
}