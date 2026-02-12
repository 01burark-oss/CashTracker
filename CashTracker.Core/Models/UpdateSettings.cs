namespace CashTracker.Core.Models
{
    public sealed class UpdateSettings
    {
        public string RepoOwner { get; set; } = string.Empty;
        public string RepoName { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string ChecksumAssetName { get; set; } = string.Empty;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(RepoOwner) &&
            !string.IsNullOrWhiteSpace(RepoName);
    }
}
