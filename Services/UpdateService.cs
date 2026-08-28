using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using GachaLinkFetcher.Models;

namespace GachaLinkFetcher.Services
{
    internal sealed class UpdateService
    {
        internal const int MetadataTimeoutMilliseconds = 15000;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public async Task<UpdateCheckResult> CheckAsync()
        {
            var json = await DownloadStringWithTimeoutAsync(new Uri(AppInfo.LatestReleaseApiUrl), MetadataTimeoutMilliseconds);

            var release = ParseRelease(json);
            return new UpdateCheckResult
            {
                IsUpdateAvailable = release.Version > AppInfo.Version,
                Release = release,
                Message = release.Version > AppInfo.Version
                    ? "发现新版本 v" + release.VersionText
                    : "当前已是最新版本 v" + AppInfo.VersionText
            };
        }

        internal UpdateInfo ParseRelease(string json)
        {
            var root = serializer.DeserializeObject(json) as IDictionary<string, object>;
            if (root == null) throw new InvalidOperationException("GitHub Release 响应格式无效。");

            var tagName = Text(root, "tag_name");
            var versionText = tagName.Trim().TrimStart('v', 'V');
            Version version;
            if (!Version.TryParse(NormalizeVersion(versionText), out version))
                throw new InvalidOperationException("无法识别最新版本号：" + tagName);

            var info = new UpdateInfo
            {
                Version = version,
                VersionText = versionText,
                TagName = tagName,
                ReleaseName = Text(root, "name"),
                ReleasePageUrl = Text(root, "html_url"),
                ReleaseNotes = Text(root, "body")
            };

            var assets = root.ContainsKey("assets") ? root["assets"] as object[] : null;
            if (assets == null) return info;

            var expectedInstaller = "GachaLinkFetcher-Setup-v" + versionText + ".exe";
            var assetList = assets.OfType<IDictionary<string, object>>().ToList();
            var installer = assetList.FirstOrDefault(item => string.Equals(Text(item, "name"), expectedInstaller, StringComparison.OrdinalIgnoreCase));
            if (installer == null)
                installer = assetList.FirstOrDefault(item => Text(item, "name").IndexOf("GachaLinkFetcher-Setup", StringComparison.OrdinalIgnoreCase) >= 0 && Text(item, "name").EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

            if (installer != null)
            {
                info.InstallerFileName = Text(installer, "name");
                info.InstallerAssetUrl = RequireGitHubDownloadUrl(Text(installer, "browser_download_url"));
                var checksumName = info.InstallerFileName + ".sha256";
                var checksum = assetList.FirstOrDefault(item => string.Equals(Text(item, "name"), checksumName, StringComparison.OrdinalIgnoreCase));
                if (checksum == null)
                    checksum = assetList.FirstOrDefault(item => string.Equals(Text(item, "name"), "checksums.txt", StringComparison.OrdinalIgnoreCase));
                if (checksum != null) info.ChecksumAssetUrl = RequireGitHubDownloadUrl(Text(checksum, "browser_download_url"));
            }

            return info;
        }

        internal static WebClient CreateGitHubClient()
        {
            return CreateWebClient(MetadataTimeoutMilliseconds);
        }

        internal static WebClient CreateWebClient(int timeoutMilliseconds)
        {
            var client = new TimeoutWebClient(timeoutMilliseconds) { Encoding = Encoding.UTF8 };
            client.Headers[HttpRequestHeader.UserAgent] = "GachaLinkFetcher/" + AppInfo.VersionText;
            client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
            return client;
        }

        internal static async Task<string> DownloadStringWithTimeoutAsync(Uri uri, int timeoutMilliseconds)
        {
            using (var client = CreateWebClient(timeoutMilliseconds))
            {
                var downloadTask = client.DownloadStringTaskAsync(uri);
                var completedTask = await Task.WhenAny(downloadTask, Task.Delay(timeoutMilliseconds));
                if (completedTask == downloadTask) return await downloadTask;

                client.CancelAsync();
                try { await downloadTask; }
                catch (Exception) { }
                throw new WebException("网络连接在 " + (timeoutMilliseconds / 1000) + " 秒内没有响应。", WebExceptionStatus.Timeout);
            }
        }

        private static string RequireGitHubDownloadUrl(string value)
        {
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Release 附件地址不是受信任的 GitHub HTTPS 地址。");
            return uri.AbsoluteUri;
        }

        private static string NormalizeVersion(string value)
        {
            var parts = value.Split('.');
            if (parts.Length == 1) return value + ".0.0";
            if (parts.Length == 2) return value + ".0";
            return value;
        }

        private static string Text(IDictionary<string, object> source, string key)
        {
            object value;
            return source != null && source.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : string.Empty;
        }
    }
}
