namespace Tahdeera.Application.Common.Utilities;

/// <summary>
/// Zero-dependency Arabic number-to-words converter ("Tafqeet") specialized for Egyptian currency amounts.
/// Supports amounts up to 999,999.99 with piastre precision.
/// </summary>
public static class Tafqeet
{
    private static readonly string[] Ones =
    [
        "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة"
    ];

    private static readonly string[] Teens =
    [
        "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر",
        "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر"
    ];

    private static readonly string[] Tens =
    [
        "", "عشرة", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون"
    ];

    private static readonly string[] Hundreds =
    [
        "", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة",
        "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة"
    ];

    public static string Convert(decimal amount, string currency = "EGP")
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative for receipt Tafqeet conversion.");
        }

        if (amount == 0m)
        {
            return "فقط صفر جنيهاً مصرياً لا غير";
        }

        long integerPart = (long)Math.Truncate(amount);
        int piastres = (int)Math.Round((amount - integerPart) * 100m, MidpointRounding.AwayFromZero);

        // Adjust if rounding caused piastres to reach 100
        if (piastres == 100)
        {
            integerPart += 1;
            piastres = 0;
        }

        var parts = new List<string>();

        if (integerPart > 0)
        {
            string integerWords = ConvertNumberToWords(integerPart);
            string integerDenomination = GetPoundsDenomination(integerPart);
            parts.Add($"{integerWords} {integerDenomination}".Trim());
        }

        if (piastres > 0)
        {
            string piastreWords = ConvertNumberToWords(piastres);
            string piastreDenomination = GetPiastresDenomination(piastres);
            parts.Add($"{piastreWords} {piastreDenomination}".Trim());
        }

        string result = string.Join(" و", parts);
        return $"فقط {result} لا غير";
    }

    private static string ConvertNumberToWords(long number)
    {
        if (number == 0)
        {
            return "صفر";
        }

        if (number > 999_999)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "Amounts greater than 999,999 are not supported.");
        }

        var segments = new List<string>();

        long thousands = number / 1000;
        long remainder = number % 1000;

        if (thousands > 0)
        {
            if (thousands == 1)
            {
                segments.Add("ألف");
            }
            else if (thousands == 2)
            {
                segments.Add("ألفان");
            }
            else if (thousands >= 3 && thousands <= 10)
            {
                segments.Add($"{ConvertBelowThousand((int)thousands)} آلاف");
            }
            else
            {
                segments.Add($"{ConvertBelowThousand((int)thousands)} ألفاً");
            }
        }

        if (remainder > 0)
        {
            segments.Add(ConvertBelowThousand((int)remainder));
        }

        return string.Join(" و", segments);
    }

    private static string ConvertBelowThousand(int number)
    {
        var parts = new List<string>();

        int hundreds = number / 100;
        int rem = number % 100;

        if (hundreds > 0)
        {
            parts.Add(Hundreds[hundreds]);
        }

        if (rem > 0)
        {
            if (rem < 10)
            {
                parts.Add(Ones[rem]);
            }
            else if (rem < 20)
            {
                parts.Add(Teens[rem - 10]);
            }
            else
            {
                int tens = rem / 10;
                int ones = rem % 10;

                if (ones > 0)
                {
                    parts.Add($"{Ones[ones]} و{Tens[tens]}");
                }
                else
                {
                    parts.Add(Tens[tens]);
                }
            }
        }

        return string.Join(" و", parts);
    }

    private static string GetPoundsDenomination(long amount)
    {
        if (amount == 1)
        {
            return "جنيه مصري واحد";
        }

        if (amount == 2)
        {
            return "جنيهان مصريان";
        }

        long lastTwoDigits = amount % 100;
        if (lastTwoDigits >= 3 && lastTwoDigits <= 10)
        {
            return "جنيهات مصرية";
        }

        return "جنيهاً مصرياً";
    }

    private static string GetPiastresDenomination(int amount)
    {
        if (amount == 1)
        {
            return "قرش واحد";
        }

        if (amount == 2)
        {
            return "قرشان";
        }

        if (amount >= 3 && amount <= 10)
        {
            return "قروش";
        }

        return "قرشاً";
    }
}
