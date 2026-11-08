using System.Text.RegularExpressions;
using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Identity.Core.Users;

//Email normalizzata (trim + lowercase) e validata. Se esiste un'istanza, è valida
public sealed partial record Email
{
    public const int MaxLength = 254;   // limite pratico RFC 5321

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Email Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("email.required", "Email is required.");

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
            throw new DomainException("email.too_long", $"Email cannot exceed {MaxLength} characters.");

        if (!EmailPattern().IsMatch(normalized))
            throw new DomainException("email.invalid", "Email format is invalid.");

        return new Email(normalized);
    }

    public override string ToString() => Value;

    //Validazione volutamente semplice: la vera verifica di un'email è l'invio di un messaggio
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}