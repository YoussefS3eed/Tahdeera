namespace Tahdeera.Application.Common.Utilities;

public static class StudentPrivacyMasker
{
    private static readonly HashSet<string> CompoundPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "عبد", "سيف", "نور", "تقى", "شمس", "عز", "بهاء", "ضياء", "حسام", "أبو", "ابو"
    };

    public static string MaskEgyptianName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "***";

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            var single = parts[0];
            return single.Length <= 2 ? single + "***" : single[..2] + "***";
        }

        // Check if first name is a compound given name (e.g. "عبد الرحمن محمد", "سيف الدين خالد")
        if (parts.Length >= 3 && CompoundPrefixes.Contains(parts[0]))
        {
            var compoundFirst = $"{parts[0]} {parts[1]}";
            var thirdInitial = parts[2].Length > 0 ? parts[2][..1] : string.Empty;
            return $"{compoundFirst} {thirdInitial}***";
        }

        var firstName = parts[0];
        var secondInitial = parts[1].Length > 0 ? parts[1][..1] : string.Empty;
        return $"{firstName} {secondInitial}***";
    }
}
