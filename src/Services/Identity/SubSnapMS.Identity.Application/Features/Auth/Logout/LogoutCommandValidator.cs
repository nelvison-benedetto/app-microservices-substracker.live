using FluentValidation;

namespace SubSnapMS.Identity.Application.Features.Auth.Logout;

internal sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(c => c.RefreshToken)
            .NotEmpty().WithErrorCode("refresh_token.required").WithMessage("Refresh token is required.")
            .MaximumLength(256).WithErrorCode("refresh_token.too_long").WithMessage("Refresh token is too long.");
    }
}