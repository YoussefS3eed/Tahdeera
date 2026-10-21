using System.Text.RegularExpressions;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Domain.ValueObjects;

public sealed record E164PhoneNumber
{
    private static readonly Regex EgyptianPhoneRegex = new(
        @"^(\+201|01|201)[0125][0-9]{8}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Value { get; }

    private E164PhoneNumber(string value)
    {
        Value = value;
    }

    public static E164PhoneNumber Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new DomainValidationException("INVALID_PHONE_NUMBER", "Recipient phone number is missing or empty.");

        var cleaned = input.Trim().Replace(" ", "").Replace("-", "");

        if (!EgyptianPhoneRegex.IsMatch(cleaned))
            throw new DomainValidationException("INVALID_PHONE_NUMBER", "Recipient phone number does not match Egyptian mobile format (+201...).");

        string normalized;
        if (cleaned.StartsWith("+201"))
        {
            normalized = cleaned;
        }
        else if (cleaned.StartsWith("201"))
        {
            normalized = "+" + cleaned;
        }
        else if (cleaned.StartsWith("01"))
        {
            normalized = "+20" + cleaned.Substring(1);
        }
        else
        {
            throw new DomainValidationException("INVALID_PHONE_NUMBER", "Recipient phone number does not match Egyptian mobile format (+201...).");
        }

        return new E164PhoneNumber(normalized);
    }

    public override string ToString() => Value;

    public static implicit operator string(E164PhoneNumber phone) => phone.Value;
}
