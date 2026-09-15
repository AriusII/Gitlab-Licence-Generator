namespace GitlabLicenseGenerator.Core.Licensing;

/// <summary>
///     One purchase-entry for a seat-based add-on inside restrictions.add_on_products (see
///     ee/app/services/gitlab_subscriptions/add_on_purchases/self_managed/license_add_ons/*.rb).
///     An add-on can have several entries (e.g. renewed terms); GitLab sums the quantity of every
///     entry whose window is currently active (started_on &lt;= today &lt; expires_on).
/// </summary>
public sealed class LicenseAddOnPurchase
{
    public required int Quantity { get; set; }

    public required DateOnly StartedOn { get; set; }

    public required DateOnly ExpiresOn { get; set; }

    public string? PurchaseXid { get; set; }

    public bool Trial { get; set; }
}