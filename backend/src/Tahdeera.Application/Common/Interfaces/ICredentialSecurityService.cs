namespace Tahdeera.Application.Common.Interfaces;

public interface ICredentialSecurityService
{
    /// <summary>
    /// Computes the cryptographic SHA-256 binary hash of a raw credential token.
    /// Used for storing physical NFC and digital QR tokens securely in the database.
    /// </summary>
    /// <param name="token">The raw unhashed token string.</param>
    /// <returns>A 32-byte binary array representing the SHA-256 digest.</returns>
    byte[] HashToken(string token);

    /// <summary>
    /// Generates a globally unique, human-readable student code following the STU-YYYY-XXXXX format.
    /// Uses a 5-digit numeric suffix (0-9).
    /// </summary>
    /// <param name="year">The four-digit calendar year.</param>
    /// <returns>A formatted student code string (e.g. STU-2026-84920).</returns>
    string GenerateStudentCode(int year);

    /// <summary>
    /// Generates a high-entropy digital QR code payload token.
    /// Formatted as "tahdeera://qr/{hex32}" containing 16 cryptographically random bytes.
    /// </summary>
    /// <returns>The standardized QR payload URI.</returns>
    string GenerateQrPayload();

    /// <summary>
    /// Introduces an artificial timing jitter between 35ms and 65ms.
    /// Executed during failed anti-enumeration student lookups to equalize timing distributions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteDummyVerificationDelayAsync(CancellationToken cancellationToken = default);
}
