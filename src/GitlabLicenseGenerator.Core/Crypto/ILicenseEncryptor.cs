using System.Security.Cryptography;

namespace GitlabLicenseGenerator.Core.Crypto;

/// <summary>Encrypts and decrypts the RSA/AES envelope GitLab EE license files use.</summary>
public interface ILicenseEncryptor
{
    /// <summary>Encrypts plaintext JSON into the base64 envelope GitLab EE expects. Requires a private key.</summary>
    string Encrypt(string plaintextJson, RSA privateKey);

    /// <summary>Decrypts a license file's contents back to plaintext JSON. Only a public key is required.</summary>
    string Decrypt(string licenseFileContents, RSA publicKey);

    /// <summary>
    ///     Re-encrypts replacement plaintext JSON into an existing license envelope, reusing that
    ///     envelope's RSA-wrapped AES key and IV verbatim — only the ciphertext ("data") changes.
    ///     Only the public key is required: the AES key is recovered from the envelope with the same
    ///     public-key "verify-style" RSA primitive <see cref="Decrypt" /> uses, so a license can be
    ///     edited without ever holding the private key.
    /// </summary>
    string Reencrypt(string licenseFileContents, string newPlaintextJson, RSA publicKey);
}