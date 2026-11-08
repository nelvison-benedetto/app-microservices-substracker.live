using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Features.Users.GetStatus;

//Usata dal servizio gRPC UserLookup (chiamate interne da Media)
public sealed record GetUserStatusQuery(UserId UserId) : IQuery<UserStatusResult>;