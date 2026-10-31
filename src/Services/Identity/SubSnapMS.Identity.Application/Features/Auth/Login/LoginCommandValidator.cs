using FluentValidation;
using SubSnapMS.Identity.Application.Common.Validation;

namespace SubSnapMS.Identity.Application.Features.Auth.Login;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email).ValidEmail();
        RuleFor(c => c.Password).ExistingPassword();
    }
}