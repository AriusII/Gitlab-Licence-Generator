namespace GitlabLicenseGenerator.Core.Crypto;

/// <summary>Thrown when a license envelope cannot be decrypted — wrong key, or corrupted/tampered ciphertext.</summary>
public sealed class LicenseDecryptionException : LicenseEncryptionException
{
    public LicenseDecryptionException(string message) : base(message)
    {
    }

    public LicenseDecryptionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}