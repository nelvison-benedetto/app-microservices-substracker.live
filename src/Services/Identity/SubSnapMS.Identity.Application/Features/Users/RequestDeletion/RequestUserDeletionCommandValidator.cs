using FluentValidation;
using SubSnapMS.Identity.Application.Common.Validation;

namespace SubSnapMS.Identity.Application.Features.Users.RequestDeletion;

internal sealed class RequestUserDeletionCommandValidator : AbstractValidator<RequestUserDeletionCommand>
{
    public RequestUserDeletionCommandValidator()
    {
        RuleFor(c => c.CurrentPassword).ExistingPassword();
    }
}