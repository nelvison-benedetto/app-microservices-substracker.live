using FluentValidation;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Application.Common.Validation;

//Regole comuni a creazione e modifica. Il dominio rivalida comunque (difesa in profondità)
internal static class SubscriptionValidationRules
{
    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithErrorCode("subscription.name_required").WithMessage("Name is required.")
            .MaximumLength(SubscriptionDetails.NameMaxLength).WithErrorCode("subscription.name_too_long")
                .WithMessage($"Name cannot exceed {SubscriptionDetails.NameMaxLength} characters.");

    public static IRuleBuilderOptions<T, string?> ValidCategory<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .MaximumLength(SubscriptionDetails.CategoryMaxLength).WithErrorCode("subscription.category_too_long")
                .WithMessage($"Category cannot exceed {SubscriptionDetails.CategoryMaxLength} characters.");

    public static IRuleBuilderOptions<T, decimal> ValidAmount<T>(this IRuleBuilder<T, decimal> rule) =>
        rule
            .GreaterThan(0).WithErrorCode("subscription.amount_not_positive").WithMessage("Amount must be greater than zero.")
            .LessThanOrEqualTo(Money.MaxAmount).WithErrorCode("subscription.amount_too_large")
                .WithMessage($"Amount cannot exceed {Money.MaxAmount}.")
            .PrecisionScale(12, 2, ignoreTrailingZeros: true).WithErrorCode("subscription.amount_precision")
                .WithMessage("Amount can have at most 2 decimal places.");

    //Opzionale: se assente si usa la valuta di default
    public static IRuleBuilderOptions<T, string?> ValidCurrency<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Matches("^[A-Za-z]{3}$").When((_, currency) => currency is not null)
                .WithErrorCode("subscription.currency_invalid").WithMessage("Currency must be a 3-letter ISO 4217 code (e.g. EUR).");

    public static IRuleBuilderOptions<T, string> ValidBillingCycle<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithErrorCode("subscription.billing_cycle_required").WithMessage("Billing cycle is required.")
            .Must(BillingCycleExtensions.IsValidCode).WithErrorCode("subscription.billing_cycle_invalid")
                .WithMessage($"Billing cycle must be one of: {string.Join(", ", BillingCycleExtensions.AllCodes)}.");

    public static IRuleBuilderOptions<T, DateOnly> ValidStartDate<T>(this IRuleBuilder<T, DateOnly> rule) =>
        rule
            .NotEqual(default(DateOnly)).WithErrorCode("subscription.start_date_required").WithMessage("Start date is required.");

}