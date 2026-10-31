using FluentValidation;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Common.Validation;

/// <summary>Regole riusate da più validator. La password segue le stesse regole del form Zod del frontend.</summary>
internal static class IdentityValidationRules
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithErrorCode("email.required").WithMessage("Email is required.")
            .MaximumLength(Email.MaxLength).WithErrorCode("email.too_long").WithMessage($"Email cannot exceed {Email.MaxLength} characters.")
            .EmailAddress().WithErrorCode("email.invalid").WithMessage("Email format is invalid.");

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithErrorCode("password.required").WithMessage("Password is required.")
            .MinimumLength(PasswordMinLength).WithErrorCode("password.too_short").WithMessage($"Password must be at least {PasswordMinLength} characters.")
            .MaximumLength(PasswordMaxLength).WithErrorCode("password.too_long").WithMessage($"Password cannot exceed {PasswordMaxLength} characters.")
            .Matches("[A-Z]").WithErrorCode("password.uppercase_required").WithMessage("Password must contain an uppercase letter.")
            .Matches("[0-9]").WithErrorCode("password.digit_required").WithMessage("Password must contain a number.")
            .Matches("[^a-zA-Z0-9]").WithErrorCode("password.special_required").WithMessage("Password must contain a special character.");

    /// <summary>Password esistente (login, re-autenticazione): nessuna regola di complessità, solo limiti.</summary>
    public static IRuleBuilderOptions<T, string> ExistingPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithErrorCode("password.required").WithMessage("Password is required.")
            .MaximumLength(PasswordMaxLength).WithErrorCode("password.too_long").WithMessage($"Password cannot exceed {PasswordMaxLength} characters.");
}