namespace CashTracker.Core.Models
{
    public sealed class UpdateSettings
    {
        public string RepoOwner { get; set; } = string.Empty;
        public string RepoName { get; set; } = string.Empty;
        public string ManifestUrl { get; set; } = string.Empty;
        public int AutoCheckDelaySeconds { get; set; } = 30;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ResolveManifestUrl());

        public string ResolveManifestUrl()
        {
            if (!string.IsNullOrWhiteSpace(ManifestUrl))
                return ManifestUrl.Trim();

            if (string.IsNullOrWhiteSpace(RepoOwner) || string.IsNullOrWhiteSpace(RepoName))
                return string.Empty;

            return $"https://github.com/{RepoOwner.Trim()}/{RepoName.Trim()}/releases/latest/download/update-manifest.json";
        }
    }
}
