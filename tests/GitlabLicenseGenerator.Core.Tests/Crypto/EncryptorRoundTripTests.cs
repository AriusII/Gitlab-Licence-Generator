using System.Security.Cryptography;
using System.Text;
using GitlabLicenseGenerator.Core.Crypto;
using Xunit;

namespace GitlabLicenseGenerator.Core.Tests.Crypto;

public sealed class EncryptorRoundTripTests
{
    [Fact]
    public void Encrypt_ThenDecrypt_WithEphemeralKeyPair_RecoversOriginalPlaintext()
    {
        using var rsa = RSA.Create(2048);
        var encryptor = new BouncyCastleLicenseEncryptor();
        const string plaintext = """{"hello":"world","n":42}""";

        var envelope = encryptor.Encrypt(plaintext, rsa);
        var recovered = encryptor.Decrypt(envelope, rsa);

        Assert.Equal(plaintext, recovered);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertextEachTime_DueToRandomKeyAndIv()
    {
        using var rsa = RSA.Create(2048);
        var encryptor = new BouncyCastleLicenseEncryptor();

        var first = encryptor.Encrypt("same plaintext", rsa);
        var second = encryptor.Encrypt("same plaintext", rsa);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Decrypt_InvalidBase64_ThrowsLicenseDecryptionException()
    {
        using var rsa = RSA.Create(2048);
        var encryptor = new BouncyCastleLicenseEncryptor();

        Assert.Throws<LicenseDecryptionException>(() => encryptor.Decrypt("not-valid-base64!!!", rsa));
    }

    [Fact]
    public void Decrypt_ValidBase64ButNotJson_ThrowsLicenseDecryptionException()
    {
        using var rsa = RSA.Create(2048);
        var encryptor = new BouncyCastleLicenseEncryptor();
        var notJson = Convert.ToBase64String(Encoding.UTF8.GetBytes("this is not json"));

        Assert.Throws<LicenseDecryptionException>(() => encryptor.Decrypt(notJson, rsa));
    }

    [Fact]
    public void Decrypt_MissingRequiredField_ThrowsLicenseDecryptionException()
    {
        using var rsa = RSA.Create(2048);
        var encryptor = new BouncyCastleLicenseEncryptor();
        var incompleteEnvelope = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"data":"x","key":"y"}"""));

        Assert.Throws<LicenseDecryptionException>(() => encryptor.Decrypt(incompleteEnvelope, rsa));
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_ThrowsLicenseDecryptionException()
    {
        using var rsa = RSA.Create(2048);
        var encryptor = new BouncyCastleLicenseEncryptor();
        var envelope = encryptor.Encrypt("some plaintext to tamper with", rsa);

        var envelopeBytes = Convert.FromBase64String(envelope);
        // Flip a byte roughly in the middle of the JSON envelope, likely inside the "data" field.
        envelopeBytes[envelopeBytes.Length / 2] ^= 0xFF;
        var tampered = Convert.ToBase64String(envelopeBytes);

        // Tampering may corrupt the JSON, the base64 field, or the padded plaintext — any of these
        // should surface as a LicenseDecryptionException, never an unhandled exception.
        Assert.Throws<LicenseDecryptionException>(() => encryptor.Decrypt(tampered, rsa));
    }

    [Fact]
    public void Encrypt_WithPublicKeyOnly_ThrowsLicenseKeyException()
    {
        using var fullKeyPair = RSA.Create(2048);
        using var publicOnly = RSA.Create();
        publicOnly.ImportRSAPublicKey(fullKeyPair.ExportRSAPublicKey(), out _);

        var encryptor = new BouncyCastleLicenseEncryptor();

        Assert.Throws<LicenseKeyException>(() => encryptor.Encrypt("plaintext", publicOnly));
    }
}