using FluentValidation;
using SubSnapMS.Identity.Application.Common.Validation;

namespace SubSnapMS.Identity.Application.Features.Users.ChangePassword;

internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(c => c.CurrentPassword).ExistingPassword();

        RuleFor(c => c.NewPassword)
            .StrongPassword()
            .NotEqual(c => c.CurrentPassword)
                .WithErrorCode("password.same_as_current")
                .WithMessage("The new password must be different from the current one.");
    }
}