using System.Security.Cryptography;
using System.Text.Json;
using GitlabLicenseGenerator.Core.Crypto;
using Xunit;

namespace GitlabLicenseGenerator.Core.Tests.Crypto;

/// <summary>
///     Covers ILicenseEncryptor.Reencrypt — the "modify without the private key"
///     trick: the RSA-wrapped AES key/iv must stay byte-identical while only the
///     ciphertext changes, using only the public key.
/// </summary>
public sealed class ReencryptTests
{
    [Fact]
    public void Reencrypt_WithPublicKeyOnly_ProducesNewPlaintext_ButSameWrappedKeyAndIv()
    {
        using var rsa = RSA.Create(2048);
        using var publicOnly = RSA.Create();
        publicOnly.ImportRSAPublicKey(rsa.ExportRSAPublicKey(), out _);

        var encryptor = new BouncyCastleLicenseEncryptor();
        const string originalPlaintext = """{"licensee":{"Name":"Original"}}""";
        const string editedPlaintext = """{"licensee":{"Name":"Edited"}}""";

        var originalEnvelope = encryptor.Encrypt(originalPlaintext, rsa);
        var newEnvelope = encryptor.Reencrypt(originalEnvelope, editedPlaintext, publicOnly);

        var originalFields = ParseEnvelope(originalEnvelope);
        var newFields = ParseEnvelope(newEnvelope);

        Assert.Equal(originalFields.GetProperty("key").GetString(), newFields.GetProperty("key").GetString());
        Assert.Equal(originalFields.GetProperty("iv").GetString(), newFields.GetProperty("iv").GetString());
        Assert.NotEqual(originalFields.GetProperty("data").GetString(), newFields.GetProperty("data").GetString());

        var recovered = encryptor.Decrypt(newEnvelope, publicOnly);
        Assert.Equal(editedPlaintext, recovered);
    }

    [Fact]
    public void Reencrypt_NeverRequiresThePrivateKey()
    {
        using var rsa = RSA.Create(2048);
        using var publicOnly = RSA.Create();
        publicOnly.ImportRSAPublicKey(rsa.ExportRSAPublicKey(), out _);

        var encryptor = new BouncyCastleLicenseEncryptor();
        var envelope = encryptor.Encrypt("""{"a":1}""", rsa);

        // Compiles and succeeds with a public-only RSA instance — no private key material involved.
        var reencrypted = encryptor.Reencrypt(envelope, """{"a":2}""", publicOnly);

        Assert.Equal("""{"a":2}""", encryptor.Decrypt(reencrypted, publicOnly));
    }

    [Fact]
    public void Reencrypt_MalformedEnvelope_ThrowsLicenseDecryptionException()
    {
        using var rsa = RSA.Create(2048);
        var encryptor = new BouncyCastleLicenseEncryptor();

        Assert.Throws<LicenseDecryptionException>(() => encryptor.Reencrypt("not-valid-base64!!!", "{}", rsa));
    }

    private static JsonElement ParseEnvelope(string envelopeBase64)
    {
        var envelopeJson = Convert.FromBase64String(envelopeBase64);
        using var document = JsonDocument.Parse(envelopeJson);
        return document.RootElement.Clone();
    }
}