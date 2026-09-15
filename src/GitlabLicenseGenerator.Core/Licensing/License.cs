namespace GitlabLicenseGenerator.Core.Licensing;

/// <summary>
///     GitLab EE's in-memory license model. Field names, JSON shape (see <see cref="LicenseJsonConverter" />)
///     and validation rules (see <see cref="LicenseValidator" />) must stay byte-for-byte faithful to
///     GitLab's own license format so licenses generated here are accepted by real GitLab EE instances.
/// </summary>
public sealed class License
{
    public int Version { get; init; } = 1;

    public Dictionary<string, string> Licensee { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Serialized as "issued_at" — the legacy JSON key name GitLab uses for starts_at.</summary>
    public DateOnly? StartsAt { get; set; }

    public DateOnly? ExpiresAt { get; set; }
    public DateOnly? NotifyAdminsAt { get; set; }
    public DateOnly? NotifyUsersAt { get; set; }
    public DateOnly? BlockChangesAt { get; set; }

    public DateTimeOffset? NextSyncAt { get; set; }

    /// <summary>
    ///     Only serialized when <see cref="NextSyncAt" /> is present — NOT gated on its own presence. This is a
    ///     GitLab license JSON serialization quirk, not a general rule.
    /// </summary>
    public DateTimeOffset? LastSyncedAt { get; set; }

    public DateTimeOffset? ActivatedAt { get; set; }

    public bool CloudLicensingEnabled { get; set; }
    public bool OfflineCloudLicensingEnabled { get; set; }
    public bool AutoRenewEnabled { get; set; }
    public bool SeatReconciliationEnabled { get; set; }
    public bool OperationalMetricsEnabled { get; set; }

    /// <summary>GitLab default: true unless the license JSON explicitly sets it false.</summary>
    public bool ContractOveragesAllowed { get; set; } = true;

    public bool GeneratedFromCustomersDot { get; set; }
    public bool GeneratedFromCancellation { get; set; }
    public bool TemporaryExtension { get; set; }

    public LicenseRestrictions? Restrictions { get; set; }

    public bool WillExpire => ExpiresAt is not null;
    public bool WillNotifyAdmins => NotifyAdminsAt is not null;
    public bool WillNotifyUsers => NotifyUsersAt is not null;
    public bool WillBlockChanges => BlockChangesAt is not null;
    public bool WillSync => NextSyncAt is not null;
    public bool Activated => ActivatedAt is not null;

    public bool Expired => WillExpire && DateOnly.FromDateTime(DateTime.Today) >= ExpiresAt!.Value;
    public bool NotifyAdmins => WillNotifyAdmins && DateOnly.FromDateTime(DateTime.Today) >= NotifyAdminsAt!.Value;
    public bool NotifyUsers => WillNotifyUsers && DateOnly.FromDateTime(DateTime.Today) >= NotifyUsersAt!.Value;
    public bool BlockChanges => WillBlockChanges && DateOnly.FromDateTime(DateTime.Today) >= BlockChangesAt!.Value;

    public bool Restricted => Restrictions is not null && Restrictions.Count >= 1;

    /// <summary>Never use "GitLab Inc." as a sample company — this waives the expires_at requirement.</summary>
    public bool GlTeamLicense =>
        LicenseeValue("Company").Contains("gitlab", StringComparison.OrdinalIgnoreCase)
        && LicenseeValue("Email").EndsWith("@gitlab.com", StringComparison.Ordinal);

    public bool JhTeamLicense =>
        LicenseeValue("Company").Contains("gitlab", StringComparison.OrdinalIgnoreCase)
        && LicenseeValue("Email").EndsWith("@jihulab.com", StringComparison.Ordinal);

    private string LicenseeValue(string key)
    {
        return Licensee.TryGetValue(key, out var value) ? value : string.Empty;
    }
}