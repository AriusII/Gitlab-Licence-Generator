namespace GitlabLicenseGenerator.Core.Licensing;

/// <summary>Thrown when a built license fails <see cref="LicenseValidator" /> checks.</summary>
public sealed class LicenseValidationException(IReadOnlyList<string> errors)
    : Exception("License is invalid: " + string.Join("; ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}