using System.Security.Cryptography;
using System.Text.Json;
using GitlabLicenseGenerator.Core.Crypto;

namespace GitlabLicenseGenerator.Core.Licensing;

/// <summary>
///     Combines validation, JSON serialization and encryption/decryption into the
///     .gitlab-license file format.
/// </summary>
public static class LicenseCodec
{
    private static readonly ILicenseEncryptor DefaultEncryptor = new BouncyCastleLicenseEncryptor();

    /// <summary>Validates, serializes and encrypts a license — produces the exact bytes written to a .gitlab-license file.</summary>
    public static string Export(License license, RSA privateKey, ILicenseEncryptor? encryptor = null)
    {
        LicenseValidator.ValidateOrThrow(license);
        var json = ToJson(license);
        return (encryptor ?? DefaultEncryptor).Encrypt(json, privateKey);
    }

    /// <summary>Decrypts and parses a .gitlab-license file's contents. Only the public key is required.</summary>
    public static License Import(string licenseFileContents, RSA publicKey, ILicenseEncryptor? encryptor = null)
    {
        var boundaryStripped = LicenseBoundary.RemoveBoundary(licenseFileContents);
        var json = (encryptor ?? DefaultEncryptor).Decrypt(boundaryStripped, publicKey);
        return JsonSerializer.Deserialize<License>(json, LicenseJsonOptions.Default)
               ?? throw new LicenseDecryptionException("License data is invalid JSON.");
    }

    public static string ToJson(License license, bool indented = false)
    {
        return JsonSerializer.Serialize(license, indented ? LicenseJsonOptions.Indented : LicenseJsonOptions.Default);
    }
}