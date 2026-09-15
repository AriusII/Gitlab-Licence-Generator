namespace GitlabLicenseGenerator.Core.Crypto;

/// <summary>Thrown when an RSA key is missing, malformed, or otherwise unusable for encryption/decryption.</summary>
public sealed class LicenseKeyException(string message, Exception innerException)
    : LicenseEncryptionException(message, innerException);