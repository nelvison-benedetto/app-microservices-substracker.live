using FluentValidation;

namespace SubSnapMS.Identity.Application.Features.Auth.RefreshSession;

internal sealed class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator()
    {
        RuleFor(c => c.RefreshToken)
            .NotEmpty().WithErrorCode("refresh_token.required").WithMessage("Refresh token is required.")
            .MaximumLength(256).WithErrorCode("refresh_token.too_long").WithMessage("Refresh token is too long.");
    }
}