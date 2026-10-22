namespace Tahdeera.Domain.Common;

public static class ReceiptNumberGenerator
{
    public static string Generate(int yearMonth, Guid workspaceId, int sequence)
    {
        var prefix = workspaceId.ToString("N")[..6].ToUpperInvariant();
        return $"REC-{yearMonth:D6}-{prefix}-{sequence:D5}";
    }
}
