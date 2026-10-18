using System.Security.Cryptography;
using System.Text;
using Tahdeera.Application.Common.Interfaces;

namespace Tahdeera.Infrastructure.Services;

public class CredentialSecurityService : ICredentialSecurityService
{
    private const string StudentCodeAlphabet = "0123456789"; // 10 numeric digits

    public byte[] HashToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }

    public string GenerateStudentCode(int year)
    {
        if (year < 2000 || year > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 2000 and 2100.");
        }

        string suffix = RandomNumberGenerator.GetString(StudentCodeAlphabet, 5);
        return $"STU-{year}-{suffix}";
    }

    public string GenerateQrPayload()
    {
        Span<byte> randomBytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(randomBytes);
        return $"tahdeera://qr/{Convert.ToHexString(randomBytes).ToLowerInvariant()}";
    }

    public async Task ExecuteDummyVerificationDelayAsync(CancellationToken cancellationToken = default)
    {
        int delayMs = RandomNumberGenerator.GetInt32(35, 66); // Generates 35 to 65 inclusive
        await Task.Delay(delayMs, cancellationToken);
    }
}
