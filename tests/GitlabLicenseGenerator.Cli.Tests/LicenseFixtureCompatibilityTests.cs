using System.Security.Cryptography;
using System.Text.Json;
using GitlabLicenseGenerator.Core.Crypto;
using GitlabLicenseGenerator.Core.Licensing;
using Xunit;

namespace GitlabLicenseGenerator.Cli.Tests;

/// <summary>
///     Validates the decrypt/import path against a real, known-good GitLab-accepted
///     license artifact (generated with the repository's own committed keys/private.key
///     against keys/public.key). If this test is green, GitLab EE will accept licenses
///     produced/read the same way this code does.
/// </summary>
public sealed class LicenseFixtureCompatibilityTests
{
    private static string FixturePath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
    }

    [Fact]
    public void Decrypt_KnownGoodLicense_MatchesKnownPlaintext()
    {
        var licenseFileContents = File.ReadAllText(FixturePath("reference.gitlab-license"));
        var publicKeyPem = File.ReadAllText(FixturePath("committed-public.key"));
        var expectedJson = File.ReadAllText(FixturePath("reference.license.json"));

        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(publicKeyPem);

        var encryptor = new BouncyCastleLicenseEncryptor();
        var decryptedJson = encryptor.Decrypt(licenseFileContents, publicKey);

        using var actual = JsonDocument.Parse(decryptedJson);
        using var expected = JsonDocument.Parse(expectedJson);

        AssertJsonEquivalent(expected.RootElement, actual.RootElement);
    }

    [Fact]
    public void Import_KnownGoodLicense_ProducesExpectedLicenseModel()
    {
        var licenseFileContents = File.ReadAllText(FixturePath("reference.gitlab-license"));
        var publicKeyPem = File.ReadAllText(FixturePath("committed-public.key"));

        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(publicKeyPem);

        var license = LicenseCodec.Import(licenseFileContents, publicKey);

        Assert.Equal(1, license.Version);
        Assert.Equal("Tim Cook", license.Licensee["Name"]);
        Assert.Equal("Apple Computer, Inc.", license.Licensee["Company"]);
        Assert.Equal("tcook@apple.com", license.Licensee["Email"]);
        Assert.Equal(new DateOnly(1976, 4, 1), license.StartsAt);
        Assert.Equal(new DateOnly(2500, 4, 1), license.ExpiresAt);
        Assert.Equal(new DateOnly(2500, 4, 1), license.BlockChangesAt);
        Assert.True(license.CloudLicensingEnabled);
        Assert.True(license.OfflineCloudLicensingEnabled);
        Assert.NotNull(license.Restrictions);
        Assert.Equal("ultimate", license.Restrictions!.Plan);
        Assert.Equal(2147483647, license.Restrictions.ActiveUserCount);
        Assert.True(license.Restrictions.FeatureUserCounts.Count > 0);
        Assert.Equal(2147483647, license.Restrictions.FeatureUserCounts["admin_audit_log"]);
    }

    private static void AssertJsonEquivalent(JsonElement expected, JsonElement actual)
    {
        Assert.Equal(expected.ValueKind, actual.ValueKind);

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedProps = expected.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
                var actualProps = actual.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
                Assert.Equal(expectedProps.Keys.OrderBy(k => k), actualProps.Keys.OrderBy(k => k));
                foreach (var key in expectedProps.Keys) AssertJsonEquivalent(expectedProps[key], actualProps[key]);

                break;
            case JsonValueKind.Array:
                var expectedItems = expected.EnumerateArray().ToArray();
                var actualItems = actual.EnumerateArray().ToArray();
                Assert.Equal(expectedItems.Length, actualItems.Length);
                for (var i = 0; i < expectedItems.Length; i++) AssertJsonEquivalent(expectedItems[i], actualItems[i]);

                break;
            default:
                Assert.Equal(expected.GetRawText(), actual.GetRawText());
                break;
        }
    }
}