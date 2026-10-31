using FluentValidation;
using SubSnapMS.Identity.Application.Common.Validation;

namespace SubSnapMS.Identity.Application.Features.Users.Register;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.Email).ValidEmail();
        RuleFor(c => c.Password).StrongPassword();
    }
}