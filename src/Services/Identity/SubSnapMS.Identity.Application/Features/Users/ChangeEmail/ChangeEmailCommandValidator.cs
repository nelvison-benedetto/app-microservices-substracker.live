using FluentValidation;
using SubSnapMS.Identity.Application.Common.Validation;

namespace SubSnapMS.Identity.Application.Features.Users.ChangeEmail;

internal sealed class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
{
    public ChangeEmailCommandValidator()
    {
        RuleFor(c => c.NewEmail).ValidEmail();
        RuleFor(c => c.CurrentPassword).ExistingPassword();
    }
}