using System.Text.Json.Serialization;

namespace CashTracker.CustomerUpdater;

internal sealed class CustomerUpdaterOptions
{
    public bool CheckOnly { get; init; }
    public bool Silent { get; init; }
    public string StatusFilePath { get; init; } = string.Empty;
}

internal sealed record CustomerUpdaterStatus(int Percent, string Message);

internal sealed record CustomerUpdaterResult(
    bool Success,
    string Message,
    string LatestVersion,
    string InstalledExePath,
    string ShortcutPath);

internal sealed record LatestPackageInfo(
    string LatestVersion,
    string PackageUrl,
    string PackageFileName,
    string Sha256,
    string ReleasePageUrl,
    string ReleaseNotes);

internal class SignedUpdateManifestPayload
{
    public string LatestVersion { get; set; } = string.Empty;
    public string MinSupportedVersion { get; set; } = string.Empty;
    public string PackageUrl { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
}

internal sealed class SignedUpdateManifest : SignedUpdateManifestPayload
{
    public string Signature { get; set; } = string.Empty;
}

internal sealed class GitHubReleaseResponse
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("assets")]
    public List<GitHubReleaseAsset> Assets { get; set; } = new();
}

internal sealed class GitHubReleaseAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = string.Empty;
}
