using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Features.Auth.LogoutEverywhere;

public sealed record LogoutEverywhereCommand(UserId UserId) : ICommand;