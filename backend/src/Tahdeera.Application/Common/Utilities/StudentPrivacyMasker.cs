namespace Tahdeera.Application.Common.Utilities;

public static class StudentPrivacyMasker
{
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

        var firstName = parts[0];
        var secondInitial = parts[1].Length > 0 ? parts[1][..1] : string.Empty;
        return $"{firstName} {secondInitial}***";
    }
}
