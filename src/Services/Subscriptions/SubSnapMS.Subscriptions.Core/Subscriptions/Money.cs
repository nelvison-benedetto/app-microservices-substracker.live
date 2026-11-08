using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Subscriptions.Core.Subscriptions;

//Importo + valuta. Un decimal da solo non dice in che valuta è: tenerli insieme evita di sommare EUR e USD per errore.
public sealed record Money
{
    public const string DefaultCurrency = "EUR";
    public const decimal MaxAmount = 1_000_000m;
    public const int Decimals = 2;

    private Money() { }   //x EF Core

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; private set; }

    //Codice ISO 4217 in maiuscolo (e.g. "EUR")
    public string Currency { get; private set; } = default!;

    public static Money Create(decimal amount, string currency)
    {
        if (amount <= 0)
            throw new DomainException("money.amount_not_positive", "Amount must be greater than zero.");

        if (amount > MaxAmount)
            throw new DomainException("money.amount_too_large", $"Amount cannot exceed {MaxAmount}.");

        if (decimal.Round(amount, Decimals) != amount)
            throw new DomainException("money.amount_precision", $"Amount can have at most {Decimals} decimal places.");

        var normalizedCurrency = currency?.Trim().ToUpperInvariant();
        if (normalizedCurrency is not { Length: 3 } || !normalizedCurrency.All(char.IsAsciiLetterUpper))
            throw new DomainException("money.currency_invalid", "Currency must be a 3-letter ISO 4217 code (e.g. EUR).");

        return new Money(amount, normalizedCurrency);
    }

    //equivalente mensile nella stessa valuta, arrotondato a 2 decimali.
    //Settimanale × 52 / 12 · mensile × 1 · trimestrale / 3 · semestrale / 6 · annuale / 12. Può risultare 0.00 per importi minimi su cicli lunghi: è un valore derivato, non un prezzo.
    public Money ToMonthly(BillingCycle billingCycle)
    {
        var monthly = billingCycle switch
        {
            BillingCycle.Weekly => Amount * 52m / 12m,
            BillingCycle.Monthly => Amount,
            BillingCycle.Quarterly => Amount / 3m,
            BillingCycle.Biannual => Amount / 6m,
            BillingCycle.Yearly => Amount / 12m,
            _ => throw new ArgumentOutOfRangeException(nameof(billingCycle), billingCycle, "Unknown billing cycle.")
        };
        return new Money(decimal.Round(monthly, Decimals, MidpointRounding.AwayFromZero), Currency);
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}