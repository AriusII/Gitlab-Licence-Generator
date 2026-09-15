namespace GitlabLicenseGenerator.Core.Licensing;

/// <summary>
///     Configuration for a generated GitLab EE license — bound from the "License" section of
///     appsettings.json, overridable via License__* environment variables (no command-line arguments
///     are supported; this tool is settings-driven only). Every default already grants the maximum
///     permissions a self-signed offline license can carry, so running the tool with no configuration
///     at all produces a fully-featured Ultimate license.
/// </summary>
public sealed class LicenseGenerationOptions
{
    public string Name { get; set; } = "Tim Cook";

    public string Company { get; set; } = "Apple Computer, Inc.";

    public string Email { get; set; } = "tcook@apple.com";

    /// <summary>"ultimate" (default, grants every feature in <see cref="LicenseFeatureCatalog" />), "premium", or "starter".</summary>
    public string Plan { get; set; } = "ultimate";

    public int UserCount { get; set; } = int.MaxValue;

    /// <summary>The license's expiry (and block-changes) year; month/day are always April 1st.</summary>
    public int ExpireYear { get; set; } = 2500;

    /// <summary>
    ///     Grants every GitLab Duo / seat-based add-on (restrictions.add_on_products) — see
    ///     <see cref="LicenseAddOnCatalog" />.
    /// </summary>
    public bool IncludeAddOns { get; set; } = true;

    public string PublicKeyPath { get; set; } = "keys/public.key";

    public string PrivateKeyPath { get; set; } = "keys/private.key";

    /// <summary>
    ///     RSA keys are generated automatically on first run when missing at the configured paths; set true to force a
    ///     fresh pair even if one already exists.
    /// </summary>
    public bool RegenerateKeys { get; set; }

    /// <summary>
    ///     Deliberately not named "license/" — that collides with the repository's own LICENSE file on case-insensitive
    ///     filesystems (Windows, default macOS). Every generation run also copies the public key that signed this
    ///     license into this same output directory — the only key material GitLab itself ever needs (see
    ///     <see cref="PublicKeyPath" />). The private key is never copied here; it stays under <see cref="PrivateKeyPath" />.
    /// </summary>
    public string OutputPath { get; set; } = "output/result.gitlab-license";

    /// <summary>
    ///     Optional plaintext (pre-encryption) copy of the license JSON, useful for inspection/debugging. Set to
    ///     null/empty to skip writing it.
    /// </summary>
    public string? PlainLicensePath { get; set; } = "output/license.json";
}