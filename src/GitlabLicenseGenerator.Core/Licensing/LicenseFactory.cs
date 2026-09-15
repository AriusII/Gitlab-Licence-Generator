namespace GitlabLicenseGenerator.Core.Licensing;

/// <summary>Builds a maximal-permissions GitLab EE license from <see cref="LicenseGenerationOptions" />. Pure — no I/O.</summary>
public static class LicenseFactory
{
    /// <summary>Required by GitLab's license format; the actual value is irrelevant as long as it predates every real license.</summary>
    private static readonly DateOnly StartsAt = new(1976, 4, 1);

    /// <summary>A wide, arbitrary start date for add-on purchase windows so they are always active.</summary>
    private static readonly DateOnly AddOnWindowStart = new(2020, 1, 1);

    public static License Create(LicenseGenerationOptions options)
    {
        var expiresAt = new DateOnly(options.ExpireYear, 4, 1);

        var restrictions = new LicenseRestrictions
        {
            Plan = options.Plan,
            ActiveUserCount = options.UserCount,
            Trial = false,
            ReconciliationCompleted = true
        };

        foreach (var feature in LicenseFeatureCatalog.UltimateFeatures)
            restrictions.FeatureUserCounts[feature] = options.UserCount;

        // Legacy pre-"plan" add-on codes some older GitLab code paths still check directly.
        restrictions.AddOns["GitLab_Auditor_User"] = 1;
        restrictions.AddOns["GitLab_FileLocks"] = 1;
        restrictions.AddOns["GitLab_Geo"] = 1;

        if (!options.IncludeAddOns)
            return new License
            {
                Licensee = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Name"] = options.Name,
                    ["Company"] = options.Company,
                    ["Email"] = options.Email
                },

                StartsAt = StartsAt,
                ExpiresAt = expiresAt,

                // Prevents a GitLab crash computing notification_start_date = block_changes_at - N days.
                BlockChangesAt = expiresAt,

                // Both required for GitLab to treat this as an "offline cloud license", the only license
                // subtype that provisions restrictions.add_on_products (GitLab Duo, etc.) on upload.
                CloudLicensingEnabled = true,
                OfflineCloudLicensingEnabled = true,

                Restrictions = restrictions
            };
        foreach (var addOn in LicenseAddOnCatalog.AllAddOns)
            restrictions.AddOnProducts[addOn] =
            [
                new LicenseAddOnPurchase
                {
                    Quantity = options.UserCount,
                    StartedOn = AddOnWindowStart,
                    ExpiresOn = expiresAt,
                    Trial = false
                }
            ];

        return new License
        {
            Licensee = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Name"] = options.Name,
                ["Company"] = options.Company,
                ["Email"] = options.Email
            },

            StartsAt = StartsAt,
            ExpiresAt = expiresAt,

            // Prevents a GitLab crash computing notification_start_date = block_changes_at - N days.
            BlockChangesAt = expiresAt,

            // Both required for GitLab to treat this as an "offline cloud license", the only license
            // subtype that provisions restrictions.add_on_products (GitLab Duo, etc.) on upload.
            CloudLicensingEnabled = true,
            OfflineCloudLicensingEnabled = true,

            Restrictions = restrictions
        };
    }
}