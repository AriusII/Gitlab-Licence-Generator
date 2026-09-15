using System.Text.Json;

namespace GitlabLicenseGenerator.Core.Licensing;

public static class LicenseJsonOptions
{
    public static JsonSerializerOptions Default { get; } = Create(false);

    public static JsonSerializerOptions Indented { get; } = Create(true);

    private static JsonSerializerOptions Create(bool indented)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented };
        options.Converters.Add(new LicenseJsonConverter());
        return options;
    }
}