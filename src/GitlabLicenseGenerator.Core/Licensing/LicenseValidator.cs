namespace GitlabLicenseGenerator.Core.Licensing;

/// <summary>
///     Validates the invariants GitLab's license format requires. Some checks GitLab performs at
///     runtime (e.g. "expires_at must be a Date if present") are unreachable here by construction,
///     since <see cref="License" /> uses typed DateOnly?/DateTimeOffset?/LicenseRestrictions fields
///     instead of untyped attributes — only the genuinely semantic rules are enforced below.
/// </summary>
public static class LicenseValidator
{
    public static IReadOnlyList<string> Validate(License license)
    {
        var errors = new List<string>();

        if (license.Licensee.Count == 0) errors.Add("licensee must be present");

        if (license.StartsAt is null) errors.Add("starts_at must be a date");

        if (license is { WillExpire: false, GlTeamLicense: false, JhTeamLicense: false })
            errors.Add("expires_at must be present unless this is a GitLab/JiHu team license");

        if (license is { CloudLicensingEnabled: false, OfflineCloudLicensingEnabled: true })
            errors.Add("offline_cloud_licensing_enabled cannot be true unless cloud_licensing_enabled is also true");

        return errors;
    }

    public static bool IsValid(License license)
    {
        return Validate(license).Count == 0;
    }

    public static void ValidateOrThrow(License license)
    {
        var errors = Validate(license);
        if (errors.Count > 0) throw new LicenseValidationException(errors);
    }
}