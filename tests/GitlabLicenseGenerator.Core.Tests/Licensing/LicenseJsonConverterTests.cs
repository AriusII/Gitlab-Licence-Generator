using System.Text.Json;
using GitlabLicenseGenerator.Core.Licensing;
using Xunit;

namespace GitlabLicenseGenerator.Core.Tests.Licensing;

public sealed class LicenseJsonConverterTests
{
    private static License MinimalLicense()
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
    public void Write_StartsAt_IsSerializedAsIssuedAt()
    {
        var license = MinimalLicense();

        var root = SerializeToElement(license);

        Assert.False(root.TryGetProperty("starts_at", out _));
        Assert.Equal("1976-04-01", root.GetProperty("issued_at").GetString());
    }

    [Fact]
    public void Write_LastSyncedAt_IsGatedOnNextSyncAtPresence_NotItsOwnPresence()
    {
        var withoutNextSync = MinimalLicense();
        withoutNextSync.LastSyncedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        withoutNextSync.NextSyncAt = null;

        var root = SerializeToElement(withoutNextSync);

        Assert.False(root.TryGetProperty("last_synced_at", out _),
            "last_synced_at must not be emitted when next_sync_at is absent, even though last_synced_at itself is set.");
        Assert.False(root.TryGetProperty("next_sync_at", out _));
    }

    [Fact]
    public void Write_LastSyncedAtAndNextSyncAt_BothPresent_BothEmitted()
    {
        var license = MinimalLicense();
        license.NextSyncAt = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));
        license.LastSyncedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var root = SerializeToElement(license);

        Assert.True(root.TryGetProperty("next_sync_at", out _));
        Assert.True(root.TryGetProperty("last_synced_at", out _));
    }

    [Fact]
    public void Write_NextSyncAtPresent_LastSyncedAtAbsent_OnlyNextSyncAtEmitted()
    {
        var license = MinimalLicense();
        license.NextSyncAt = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        license.LastSyncedAt = null;

        var root = SerializeToElement(license);

        Assert.True(root.TryGetProperty("next_sync_at", out _));
        Assert.False(root.TryGetProperty("last_synced_at", out _));
    }

    [Fact]
    public void Write_ExpiresAt_Absent_WhenNull()
    {
        var license = MinimalLicense();
        license.ExpiresAt = null;

        var root = SerializeToElement(license);

        Assert.False(root.TryGetProperty("expires_at", out _));
    }

    [Fact]
    public void Write_ContractOveragesAllowed_DefaultsTrue()
    {
        var license = MinimalLicense();

        var root = SerializeToElement(license);

        Assert.True(root.GetProperty("contract_overages_allowed").GetBoolean());
    }

    [Fact]
    public void Write_Restrictions_OnlyEmittedWhenPresent()
    {
        var withoutRestrictions = MinimalLicense();
        var root = SerializeToElement(withoutRestrictions);
        Assert.False(root.TryGetProperty("restrictions", out _));

        var withRestrictions = MinimalLicense();
        withRestrictions.Restrictions = new LicenseRestrictions { Plan = "ultimate", ActiveUserCount = 100 };
        withRestrictions.Restrictions.FeatureUserCounts["geo"] = 100;
        var rootWithRestrictions = SerializeToElement(withRestrictions);

        var restrictions = rootWithRestrictions.GetProperty("restrictions");
        Assert.Equal("ultimate", restrictions.GetProperty("plan").GetString());
        Assert.Equal(100, restrictions.GetProperty("active_user_count").GetInt32());
        Assert.Equal(100, restrictions.GetProperty("geo").GetInt32());
    }

    [Fact]
    public void RoundTrip_WriteThenRead_PreservesAllFields()
    {
        var license = MinimalLicense();
        license.NotifyAdminsAt = new DateOnly(2500, 3, 1);
        license.BlockChangesAt = new DateOnly(2500, 4, 1);
        license.OfflineCloudLicensingEnabled = true;
        license.Restrictions = new LicenseRestrictions { Plan = "premium", ActiveUserCount = 50 };
        license.Restrictions.FeatureUserCounts["feature_a"] = 50;

        var json = LicenseCodec.ToJson(license);
        var roundTripped = JsonSerializer.Deserialize<License>(json, LicenseJsonOptions.Default)!;

        Assert.Equal(license.Licensee, roundTripped.Licensee);
        Assert.Equal(license.StartsAt, roundTripped.StartsAt);
        Assert.Equal(license.ExpiresAt, roundTripped.ExpiresAt);
        Assert.Equal(license.NotifyAdminsAt, roundTripped.NotifyAdminsAt);
        Assert.Equal(license.BlockChangesAt, roundTripped.BlockChangesAt);
        Assert.Equal(license.OfflineCloudLicensingEnabled, roundTripped.OfflineCloudLicensingEnabled);
        Assert.Equal(license.Restrictions.Plan, roundTripped.Restrictions!.Plan);
        Assert.Equal(license.Restrictions.ActiveUserCount, roundTripped.Restrictions.ActiveUserCount);
        Assert.Equal(license.Restrictions.FeatureUserCounts, roundTripped.Restrictions.FeatureUserCounts);
    }

    [Fact]
    public void Write_Restrictions_IncludesTrialAndReconciliationCompleted()
    {
        var license = MinimalLicense();
        license.Restrictions = new LicenseRestrictions { Plan = "ultimate", ActiveUserCount = 100 };

        var restrictions = SerializeToElement(license).GetProperty("restrictions");

        Assert.False(restrictions.GetProperty("trial").GetBoolean());
        Assert.True(restrictions.GetProperty("reconciliation_completed").GetBoolean());
    }

    [Fact]
    public void Write_AddOnProducts_OnlyEmittedWhenPresent_WithFullPurchaseShape()
    {
        var license = MinimalLicense();
        license.Restrictions = new LicenseRestrictions { Plan = "ultimate", ActiveUserCount = 100 };
        var withoutAddOns = SerializeToElement(license);
        Assert.False(withoutAddOns.GetProperty("restrictions").TryGetProperty("add_on_products", out _));

        license.Restrictions.AddOnProducts["duo_enterprise"] =
        [
            new LicenseAddOnPurchase
            {
                Quantity = 100,
                StartedOn = new DateOnly(2020, 1, 1),
                ExpiresOn = new DateOnly(2500, 4, 1),
                PurchaseXid = "abc-123",
                Trial = false
            }
        ];

        var restrictions = SerializeToElement(license).GetProperty("restrictions");
        var purchase = restrictions.GetProperty("add_on_products").GetProperty("duo_enterprise")[0];

        Assert.Equal(100, purchase.GetProperty("quantity").GetInt32());
        Assert.Equal("2020-01-01", purchase.GetProperty("started_on").GetString());
        Assert.Equal("2500-04-01", purchase.GetProperty("expires_on").GetString());
        Assert.Equal("abc-123", purchase.GetProperty("purchase_xid").GetString());
        Assert.False(purchase.GetProperty("trial").GetBoolean());
    }

    [Fact]
    public void RoundTrip_AddOnProductsAndLegacyAddOns_PreservedThroughReadWrite()
    {
        var license = MinimalLicense();
        license.Restrictions = new LicenseRestrictions { Plan = "ultimate", ActiveUserCount = 100 };
        license.Restrictions.AddOns["GitLab_Geo"] = 1;
        license.Restrictions.AddOnProducts["duo_pro"] =
        [
            new LicenseAddOnPurchase
                { Quantity = 50, StartedOn = new DateOnly(2020, 1, 1), ExpiresOn = new DateOnly(2099, 1, 1) }
        ];

        var json = LicenseCodec.ToJson(license);
        var roundTripped = JsonSerializer.Deserialize<License>(json, LicenseJsonOptions.Default)!;

        Assert.Equal(1, roundTripped.Restrictions!.AddOns["GitLab_Geo"]);
        var duoPro = roundTripped.Restrictions.AddOnProducts["duo_pro"];
        Assert.Single(duoPro);
        Assert.Equal(50, duoPro[0].Quantity);
        Assert.Equal(new DateOnly(2020, 1, 1), duoPro[0].StartedOn);
        Assert.Equal(new DateOnly(2099, 1, 1), duoPro[0].ExpiresOn);
    }

    [Fact]
    public void Read_MalformedDateString_IsSilentlyDroppedNotThrown()
    {
        const string json = """
                            {
                              "version": 1,
                              "licensee": {"Name": "Ada"},
                              "issued_at": "not-a-date",
                              "cloud_licensing_enabled": true
                            }
                            """;

        var license = JsonSerializer.Deserialize<License>(json, LicenseJsonOptions.Default)!;

        Assert.Null(license.StartsAt);
    }

    [Fact]
    public void Read_UnsupportedVersion_Throws()
    {
        const string json = """{"version": 2, "licensee": {}}""";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<License>(json, LicenseJsonOptions.Default));
    }

    private static JsonElement SerializeToElement(License license)
    {
        var json = LicenseCodec.ToJson(license);
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}