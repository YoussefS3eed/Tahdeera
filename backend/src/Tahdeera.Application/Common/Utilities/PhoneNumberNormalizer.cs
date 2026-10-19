using System.Text.RegularExpressions;

namespace Tahdeera.Application.Common.Utilities;

public static partial class PhoneNumberNormalizer
{
    private static readonly Regex EgyptianPhoneRegex = new(@"^(?:\+201|01)[0-2,5]\d{8}$", RegexOptions.Compiled);

    public static string? NormalizeEgyptian(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var trimmed = input.Trim();

        if (!EgyptianPhoneRegex.IsMatch(trimmed))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("01", StringComparison.Ordinal))
        {
            return "+20" + trimmed[1..];
        }

        return trimmed;
    }

    public static bool IsValidEgyptian(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return EgyptianPhoneRegex.IsMatch(input.Trim());
    }
}
