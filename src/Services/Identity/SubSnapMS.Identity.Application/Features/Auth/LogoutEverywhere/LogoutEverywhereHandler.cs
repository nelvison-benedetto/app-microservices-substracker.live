using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Application.Abstractions.Persistence;
using SubSnapMS.Identity.Core.Users.Exceptions;

namespace SubSnapMS.Identity.Application.Features.Auth.LogoutEverywhere;

internal sealed class LogoutEverywhereHandler(IUserRepository users, TimeProvider timeProvider)
    : ICommandHandler<LogoutEverywhereCommand>
{
    public async Task Handle(LogoutEverywhereCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken)
                   ?? throw new UserNotFoundException();

        user.SignOutEverywhere(timeProvider.GetUtcNow());
    }
}