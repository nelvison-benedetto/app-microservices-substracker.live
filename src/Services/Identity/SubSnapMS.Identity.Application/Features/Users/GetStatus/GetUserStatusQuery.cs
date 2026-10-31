using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Features.Users.GetStatus;

/// <summary>Usata dal servizio gRPC UserLookup (chiamate interne da Media).</summary>
public sealed record GetUserStatusQuery(UserId UserId) : IQuery<UserStatusResult>;