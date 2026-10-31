using FluentValidation;
using SubSnapMS.Subscriptions.Application.Common.Validation;

namespace SubSnapMS.Subscriptions.Application.Features.Subscriptions.Create;

internal sealed class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(c => c.Name).ValidName();
        RuleFor(c => c.Category).ValidCategory();
        RuleFor(c => c.Amount).ValidAmount();
        RuleFor(c => c.Currency).ValidCurrency();
        RuleFor(c => c.BillingCycle).ValidBillingCycle();
        RuleFor(c => c.StartDate).ValidStartDate();

        RuleFor(c => c.EndDate)
            .GreaterThanOrEqualTo(c => c.StartDate).When(c => c.EndDate.HasValue)
            .WithErrorCode("subscription.end_before_start").WithMessage("End date cannot be before the start date.");
    }
}