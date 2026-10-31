using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Features.Users.GetProfile;

public sealed record GetUserProfileQuery(UserId UserId) : IQuery<UserProfileResult>;