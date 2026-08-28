using System;

namespace GachaLinkFetcher.Models
{
    internal sealed class UpdateInfo
    {
        public Version Version { get; set; }
        public string VersionText { get; set; }
        public string TagName { get; set; }
        public string ReleaseName { get; set; }
        public string ReleasePageUrl { get; set; }
        public string ReleaseNotes { get; set; }
        public string InstallerFileName { get; set; }
        public string InstallerAssetUrl { get; set; }
        public string ChecksumAssetUrl { get; set; }

        public UpdateInfo()
        {
            Version = new Version(0, 0, 0);
            VersionText = "0.0.0";
            TagName = string.Empty;
            ReleaseName = string.Empty;
            ReleasePageUrl = string.Empty;
            ReleaseNotes = string.Empty;
            InstallerFileName = string.Empty;
            InstallerAssetUrl = string.Empty;
            ChecksumAssetUrl = string.Empty;
        }

        public bool CanDownload
        {
            get { return !string.IsNullOrWhiteSpace(InstallerAssetUrl) && !string.IsNullOrWhiteSpace(ChecksumAssetUrl); }
        }
    }

    internal sealed class UpdateCheckResult
    {
        public bool IsUpdateAvailable { get; set; }
        public UpdateInfo Release { get; set; }
        public string Message { get; set; }

        public UpdateCheckResult()
        {
            Release = new UpdateInfo();
            Message = string.Empty;
        }
    }
}
