using System.Text.RegularExpressions;

namespace GitlabLicenseGenerator.Core.Crypto;

/// <summary>
///     An optional PEM-like "-----BEGIN X LICENSE-----"/"-----END X LICENSE-----" wrapper around a
///     .gitlab-license file. Not used on the default generation path (GitLab's own generator exports
///     without a boundary — the real .gitlab-license file is a raw base64 blob), kept for
///     import/modify fidelity.
/// </summary>
public static partial class LicenseBoundary
{
    // Both patterns use exactly one capture group deliberately: .NET's Regex.Split includes captured
    // groups in its result array, which is what lets the plain `.last`/`.first` calls below land on
    // the correct segment after the split.
    [GeneratedRegex(@"(\A|\r?\n)-*BEGIN .+? LICENSE-*\r?\n")]
    private static partial Regex BoundaryStartRegex();

    [GeneratedRegex(@"\r?\n-*END .+? LICENSE-*(\r?\n|\z)")]
    private static partial Regex BoundaryEndRegex();

    public static string AddBoundary(string data, string productName)
    {
        data = RemoveBoundary(data);
        var upperName = productName.ToUpperInvariant();

        return string.Join('\n',
            Pad($"BEGIN {upperName} LICENSE", 60),
            data.Trim(),
            Pad($"END {upperName} LICENSE", 60));
    }

    public static string RemoveBoundary(string data)
    {
        var afterStart = BoundaryStartRegex().Split(data);
        var afterBoundary = afterStart[^1];
        var beforeEnd = BoundaryEndRegex().Split(afterBoundary);
        return beforeEnd[0];
    }

    private static string Pad(string message, int width)
    {
        var totalPadding = Math.Max(width - message.Length, 0);
        var half = totalPadding / 2.0;
        return new string('-', (int)Math.Ceiling(half)) + message + new string('-', (int)Math.Floor(half));
    }
}