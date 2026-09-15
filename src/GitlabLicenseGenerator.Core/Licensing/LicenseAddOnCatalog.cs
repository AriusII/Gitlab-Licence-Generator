namespace GitlabLicenseGenerator.Core.Licensing;

/// <summary>
///     Add-on names GitLab EE reads from restrictions.add_on_products (the "name_in_license" of each
///     ee/app/services/gitlab_subscriptions/add_on_purchases/self_managed/license_add_ons/*.rb class).
///     This is the modern, seat-based mechanism for GitLab Duo and other paid add-ons — distinct from
///     and superseding the legacy restrictions.add_ons hash. It only takes effect on an "offline cloud
///     license" (License.CloudLicensingEnabled and License.OfflineCloudLicensingEnabled both true,
///     which this generator always sets) — see GitlabSubscriptions::UploadLicenseService#update_add_on_purchases.
/// </summary>
public static class LicenseAddOnCatalog
{
    /// <summary>
    ///     duo_pro is GitLab Duo Pro (internal add-on record name "code_suggestions" — "duo_pro" is the license-facing
    ///     key).
    /// </summary>
    public static readonly IReadOnlyList<string> AllAddOns =
    [
        "duo_pro",
        "duo_enterprise",
        "duo_amazon_q",
        "duo_core",
        "self_hosted_dap",
        "gitlab_credits",
        "secrets_manager",
        "flex_offline"
    ];
}