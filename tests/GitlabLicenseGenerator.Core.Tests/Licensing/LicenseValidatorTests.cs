using GitlabLicenseGenerator.Core.Licensing;
using Xunit;

namespace GitlabLicenseGenerator.Core.Tests.Licensing;

public sealed class LicenseValidatorTests
{
    private static License ValidLicense()
    {
        return new License
        {
            Licensee = new Dictionary<string, string>
                { ["Name"] = "Ada", ["Company"] = "Acme", ["Email"] = "ada@acme.test" },
            StartsAt = new DateOnly(1976, 4, 1),
            ExpiresAt = new DateOnly(2500, 4, 1),
            CloudLicensingEnabled = true
        };
    }

    [Fact]
    public void Validate_WellFormedLicense_HasNoErrors()
    {
        Assert.True(LicenseValidator.IsValid(ValidLicense()));
    }

    [Fact]
    public void Validate_EmptyLicensee_IsInvalid()
    {
        var license = ValidLicense();
        license.Licensee = new Dictionary<string, string>();

        Assert.False(LicenseValidator.IsValid(license));
    }

    [Fact]
    public void Validate_MissingStartsAt_IsInvalid()
    {
        var license = ValidLicense();
        license.StartsAt = null;

        Assert.False(LicenseValidator.IsValid(license));
    }

    [Fact]
    public void Validate_MissingExpiresAt_WithoutTeamLicense_IsInvalid()
    {
        var license = ValidLicense();
        license.ExpiresAt = null;

        Assert.False(LicenseValidator.IsValid(license));
    }

    [Fact]
    public void Validate_MissingExpiresAt_WithGitLabTeamLicense_IsValid()
    {
        var license = ValidLicense();
        license.ExpiresAt = null;
        license.Licensee["Company"] = "GitLab Inc.";
        license.Licensee["Email"] = "someone@gitlab.com";

        Assert.True(LicenseValidator.IsValid(license));
    }

    [Fact]
    public void Validate_MissingExpiresAt_WithJiHuTeamLicense_IsValid()
    {
        var license = ValidLicense();
        license.ExpiresAt = null;
        license.Licensee["Company"] = "GitLab JiHu";
        license.Licensee["Email"] = "someone@jihulab.com";

        Assert.True(LicenseValidator.IsValid(license));
    }

    [Fact]
    public void Validate_OfflineCloudLicensingWithoutCloudLicensing_IsInvalid()
    {
        var license = ValidLicense();
        license.CloudLicensingEnabled = false;
        license.OfflineCloudLicensingEnabled = true;

        Assert.False(LicenseValidator.IsValid(license));
    }

    [Fact]
    public void ValidateOrThrow_InvalidLicense_ThrowsWithErrors()
    {
        var license = ValidLicense();
        license.Licensee = new Dictionary<string, string>();

        var exception = Assert.Throws<LicenseValidationException>(() => LicenseValidator.ValidateOrThrow(license));
        Assert.NotEmpty(exception.Errors);
    }
}