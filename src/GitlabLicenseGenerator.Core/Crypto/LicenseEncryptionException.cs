namespace GitlabLicenseGenerator.Core.Crypto;

/// <summary>Base type for every <see cref="ILicenseEncryptor" /> error.</summary>
public abstract class LicenseEncryptionException : Exception
{
    protected LicenseEncryptionException(string message) : base(message)
    {
    }

    protected LicenseEncryptionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}