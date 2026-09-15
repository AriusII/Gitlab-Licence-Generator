using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;

namespace GitlabLicenseGenerator.Core.Crypto;

/// <summary>
///     Reproduces GitLab's license encryption envelope bit-for-bit: an AES-128-CBC payload whose key
///     is wrapped with the raw RSA PKCS#1 v1.5 type-1 "sign-style" primitive (OpenSSL's
///     private_encrypt/public_decrypt), so licenses generated here are accepted by real GitLab EE
///     instances and licenses produced by GitLab's own license-generation tooling can be read back here.
///     .NET's built-in RSA.Encrypt/Decrypt only supports OAEP/PKCS1-type-2 in the
///     public-encrypt/private-decrypt direction, so the raw type-1 primitive is
///     delegated to BouncyCastle's Pkcs1Encoding, whose padding type and
///     encode/decode direction are selected together by (a) which kind of key is
///     passed to Init (private vs public) and (b) the forEncryption flag — this
///     exact pairing was verified against the BouncyCastle source before writing
///     this class.
/// </summary>
public sealed class BouncyCastleLicenseEncryptor : ILicenseEncryptor
{
    private const int AesKeySizeBytes = 16;
    private const int AesIvSizeBytes = 16;

    public string Encrypt(string plaintextJson, RSA privateKey)
    {
        ArgumentNullException.ThrowIfNull(plaintextJson);
        ArgumentNullException.ThrowIfNull(privateKey);

        var aesKey = RandomNumberGenerator.GetBytes(AesKeySizeBytes);
        var aesIv = RandomNumberGenerator.GetBytes(AesIvSizeBytes);
        var ciphertext = Aes128Cbc.Encrypt(aesKey, aesIv, Encoding.UTF8.GetBytes(plaintextJson));

        var keyParams = RsaKeyConversion.ToBouncyCastlePrivateKey(privateKey);
        var rsaCodec = new Pkcs1Encoding(new RsaEngine());
        rsaCodec.Init(true, keyParams);

        byte[] encryptedKey;
        try
        {
            encryptedKey = rsaCodec.ProcessBlock(aesKey, 0, aesKey.Length);
        }
        catch (Exception ex) when (ex is InvalidCipherTextException or DataLengthException)
        {
            throw new LicenseKeyException("Failed to encrypt the AES key with the provided private key.", ex);
        }

        var envelope = new LicenseEnvelope(
            Convert.ToBase64String(ciphertext),
            Convert.ToBase64String(encryptedKey),
            Convert.ToBase64String(aesIv));

        var envelopeJson = JsonSerializer.SerializeToUtf8Bytes(envelope);
        return Convert.ToBase64String(envelopeJson);
    }

    public string Decrypt(string licenseFileContents, RSA publicKey)
    {
        ArgumentNullException.ThrowIfNull(licenseFileContents);
        ArgumentNullException.ThrowIfNull(publicKey);

        var envelope = ParseEnvelope(licenseFileContents);

        var encryptedData = Convert.FromBase64String(envelope.Data);
        var aesKey = RecoverAesKey(envelope.Key, publicKey);
        var aesIv = Convert.FromBase64String(envelope.Iv);

        try
        {
            var plaintext = Aes128Cbc.Decrypt(aesKey, aesIv, encryptedData);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (CryptographicException ex)
        {
            throw new LicenseDecryptionException("Data could not be decrypted.", ex);
        }
    }

    public string Reencrypt(string licenseFileContents, string newPlaintextJson, RSA publicKey)
    {
        ArgumentNullException.ThrowIfNull(licenseFileContents);
        ArgumentNullException.ThrowIfNull(newPlaintextJson);
        ArgumentNullException.ThrowIfNull(publicKey);

        var envelope = ParseEnvelope(licenseFileContents);
        var aesKey = RecoverAesKey(envelope.Key, publicKey);
        var aesIv = Convert.FromBase64String(envelope.Iv);

        var ciphertext = Aes128Cbc.Encrypt(aesKey, aesIv, Encoding.UTF8.GetBytes(newPlaintextJson));

        var newEnvelope = new LicenseEnvelope(Convert.ToBase64String(ciphertext), envelope.Key, envelope.Iv);
        var newEnvelopeJson = JsonSerializer.SerializeToUtf8Bytes(newEnvelope);
        return Convert.ToBase64String(newEnvelopeJson);
    }

    private static LicenseEnvelope ParseEnvelope(string licenseFileContents)
    {
        byte[] envelopeJson;
        try
        {
            envelopeJson = Convert.FromBase64String(licenseFileContents.Trim());
        }
        catch (FormatException ex)
        {
            throw new LicenseDecryptionException("Encryption data is invalid base64.", ex);
        }

        LicenseEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<LicenseEnvelope>(envelopeJson);
        }
        catch (JsonException ex)
        {
            throw new LicenseDecryptionException("Encryption data is invalid JSON.", ex);
        }

        if (envelope is null
            || string.IsNullOrEmpty(envelope.Data)
            || string.IsNullOrEmpty(envelope.Key)
            || string.IsNullOrEmpty(envelope.Iv))
            throw new LicenseDecryptionException("Required field missing from encryption data.");

        return envelope;
    }

    private static byte[] RecoverAesKey(string encryptedKeyBase64, RSA publicKey)
    {
        var encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

        var keyParams = RsaKeyConversion.ToBouncyCastlePublicKey(publicKey);
        var rsaCodec = new Pkcs1Encoding(new RsaEngine());
        rsaCodec.Init(false, keyParams);

        byte[] aesKey;
        try
        {
            aesKey = rsaCodec.ProcessBlock(encryptedKey, 0, encryptedKey.Length);
        }
        catch (Exception ex) when (ex is InvalidCipherTextException or DataLengthException)
        {
            throw new LicenseDecryptionException("AES encryption key could not be decrypted.", ex);
        }

        return aesKey.Length != AesKeySizeBytes
            ? throw new LicenseDecryptionException("AES encryption key is invalid.")
            : aesKey;
    }

    private sealed record LicenseEnvelope(
        [property: JsonPropertyName("data")] string Data,
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("iv")] string Iv);
}