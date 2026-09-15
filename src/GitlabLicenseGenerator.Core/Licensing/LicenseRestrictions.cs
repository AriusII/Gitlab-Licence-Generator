namespace GitlabLicenseGenerator.Core.Licensing;

/// <summary>
///     The license's `restrictions` hash. The generator always populates <see cref="Plan" /> and
///     <see cref="ActiveUserCount" />, one integer entry per injected feature name (see
///     <see cref="LicenseFeatureCatalog" />), and — for a maximal-permissions license — a seat-based
///     <see cref="AddOnProducts" /> entry per GitLab Duo/add-on (see <see cref="LicenseAddOnCatalog" />).
/// </summary>
public sealed class LicenseRestrictions
{
    public required string Plan { get; set; }

    public required int ActiveUserCount { get; set; }

    public Dictionary<string, int> FeatureUserCounts { get; init; } = new(StringComparer.Ordinal);

    /// <summary>When true, GitLab stamps a trial end date on the instance — leave false for a permanent license.</summary>
    public bool Trial { get; set; }

    /// <summary>
    ///     Skips GitLab's true-up seat validation on license creation (see License#check_trueup). Default true since this
    ///     generator never populates trueup_* fields.
    /// </summary>
    public bool ReconciliationCompleted { get; set; } = true;

    /// <summary>
    ///     Legacy pre-"plan" add-on codes (restricted_attr(:add_ons, {})) — e.g. GitLab_Auditor_User,
    ///     GitLab_FileLocks, GitLab_Geo. Vestigial for modern GitLab (their features already ship via
    ///     the plan itself) but kept as a compatibility shim for any code path still reading them directly.
    /// </summary>
    public Dictionary<string, int> AddOns { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    ///     The modern, seat-based add-on mechanism (GitLab Duo Pro/Enterprise, etc. — see
    ///     <see cref="LicenseAddOnCatalog" />). Only provisioned by GitLab when this is an "offline cloud
    ///     license" (License.CloudLicensingEnabled and License.OfflineCloudLicensingEnabled both true).
    /// </summary>
    public Dictionary<string, List<LicenseAddOnPurchase>> AddOnProducts { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    ///     Mirrors GitLab's own restrictions-count check (used by License.Restricted) — Plan and
    ///     ActiveUserCount alone already guarantee this is always &gt;= 1.
    /// </summary>
    public int Count => 2 + FeatureUserCounts.Count;
}