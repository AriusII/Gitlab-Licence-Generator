using GitlabLicenseGenerator.Core.Licensing;
using Xunit;

namespace GitlabLicenseGenerator.Core.Tests.Licensing;

public sealed class LicenseFactoryTests
{
    [Fact]
    public void Create_DefaultOptions_ProducesValidMaximalUltimateLicense()
    {
        var license = LicenseFactory.Create(new LicenseGenerationOptions());

        Assert.True(LicenseValidator.IsValid(license));
        Assert.True(license.CloudLicensingEnabled);
        Assert.True(license.OfflineCloudLicensingEnabled);
        Assert.Equal("ultimate", license.Restrictions!.Plan);
        Assert.Equal(int.MaxValue, license.Restrictions.ActiveUserCount);
        Assert.False(license.Restrictions.Trial);
        Assert.True(license.Restrictions.ReconciliationCompleted);
    }

    [Fact]
    public void Create_DefaultOptions_InjectsEveryCatalogFeature()
    {
        var license = LicenseFactory.Create(new LicenseGenerationOptions());

        foreach (var feature in LicenseFeatureCatalog.UltimateFeatures)
            Assert.True(license.Restrictions!.FeatureUserCounts.ContainsKey(feature));
    }

    [Fact]
    public void Create_IncludeAddOnsTrue_PopulatesEveryAddOnProduct()
    {
        var license = LicenseFactory.Create(new LicenseGenerationOptions { IncludeAddOns = true, UserCount = 42 });

        foreach (var addOn in LicenseAddOnCatalog.AllAddOns)
        {
            Assert.True(license.Restrictions!.AddOnProducts.ContainsKey(addOn));
            Assert.Equal(42, license.Restrictions.AddOnProducts[addOn][0].Quantity);
        }
    }

    [Fact]
    public void Create_IncludeAddOnsFalse_LeavesAddOnProductsEmpty()
    {
        var license = LicenseFactory.Create(new LicenseGenerationOptions { IncludeAddOns = false });

        Assert.Empty(license.Restrictions!.AddOnProducts);
    }

    [Fact]
    public void Create_ExpireYear_SetsExpiresAtAndBlockChangesAtToAprilFirst()
    {
        var license = LicenseFactory.Create(new LicenseGenerationOptions { ExpireYear = 2100 });

        Assert.Equal(new DateOnly(2100, 4, 1), license.ExpiresAt);
        Assert.Equal(new DateOnly(2100, 4, 1), license.BlockChangesAt);
    }
}