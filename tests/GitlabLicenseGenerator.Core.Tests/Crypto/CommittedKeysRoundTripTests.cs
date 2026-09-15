using System.Security.Cryptography;
using GitlabLicenseGenerator.Core.Crypto;
using Xunit;

namespace GitlabLicenseGenerator.Core.Tests.Crypto;

/// <summary>
///     Fast, always-on smoke test exercising the actual repo-committed
///     keys/private.key + keys/public.key (not ephemeral ones), catching any
///     PEM-parsing quirk specific to those exact files.
/// </summary>
public sealed class CommittedKeysRoundTripTests
{
    private static string FixturePath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
    }

    [Fact]
    public void Encrypt_WithCommittedPrivateKey_DecryptsWithCommittedPublicKey()
    {
        using var privateKey = RSA.Create();
        privateKey.ImportFromPem(File.ReadAllText(FixturePath("committed-private.key")));
        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(File.ReadAllText(FixturePath("committed-public.key")));

        var encryptor = new BouncyCastleLicenseEncryptor();
        const string plaintext = """{"licensee":{"Name":"Test"}}""";

        var envelope = encryptor.Encrypt(plaintext, privateKey);
        var recovered = encryptor.Decrypt(envelope, publicKey);

        Assert.Equal(plaintext, recovered);
    }
}