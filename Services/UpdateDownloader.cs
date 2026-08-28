using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GachaLinkFetcher.Models;

namespace GachaLinkFetcher.Services
{
    internal sealed class UpdateDownloader
    {
        private const int DownloadTimeoutMilliseconds = 30000;

        public string UpdateDirectory
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GachaLinkFetcher", "updates"); }
        }

        public async Task<string> DownloadAsync(UpdateInfo info, DownloadRoute route, IProgress<int> progress)
        {
            if (info == null) throw new ArgumentNullException("info");
            if (route == null) throw new ArgumentNullException("route");
            if (!info.CanDownload) throw new InvalidOperationException("此版本缺少安装程序或 SHA-256 校验文件。");

            var expectedHash = await DownloadTrustedChecksumAsync(info);
            var downloadUrl = info.InstallerAssetUrl;
            if (route.Kind != DownloadRouteKind.Direct)
                downloadUrl = NodeCatalogService.BuildAcceleratedUrl(route.Value, info.InstallerAssetUrl);

            Directory.CreateDirectory(UpdateDirectory);
            var finalPath = Path.Combine(UpdateDirectory, info.InstallerFileName);
            var temporaryPath = finalPath + ".part";
            DeleteIfExists(temporaryPath);

            try
            {
                using (var client = UpdateService.CreateWebClient(DownloadTimeoutMilliseconds))
                {
                    var lastActivityUtc = DateTime.UtcNow;
                    client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs eventArgs)
                    {
                        lastActivityUtc = DateTime.UtcNow;
                        if (progress != null) progress.Report(eventArgs.ProgressPercentage);
                    };
                    var downloadTask = client.DownloadFileTaskAsync(new Uri(downloadUrl), temporaryPath);
                    while (!downloadTask.IsCompleted)
                    {
                        var completedTask = await Task.WhenAny(downloadTask, Task.Delay(1000));
                        if (completedTask == downloadTask) break;
                        if ((DateTime.UtcNow - lastActivityUtc).TotalMilliseconds < DownloadTimeoutMilliseconds) continue;

                        client.CancelAsync();
                        try { await downloadTask; }
                        catch (Exception) { }
                        throw new WebException("安装程序下载连续 30 秒没有收到数据。", WebExceptionStatus.Timeout);
                    }
                    await downloadTask;
                }

                var actualHash = ComputeSha256(temporaryPath);
                if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("安装程序 SHA-256 校验失败，文件已删除。\r\n期望：" + expectedHash + "\r\n实际：" + actualHash);

                DeleteIfExists(finalPath);
                File.Move(temporaryPath, finalPath);
                return finalPath;
            }
            catch
            {
                DeleteIfExists(temporaryPath);
                throw;
            }
        }

        private static async Task<string> DownloadTrustedChecksumAsync(UpdateInfo info)
        {
            var checksumText = await UpdateService.DownloadStringWithTimeoutAsync(new Uri(info.ChecksumAssetUrl), UpdateService.MetadataTimeoutMilliseconds);

            var filePattern = "(?im)^([a-f0-9]{64})\\s+\\*?" + Regex.Escape(info.InstallerFileName) + "\\s*$";
            var match = Regex.Match(checksumText, filePattern, RegexOptions.IgnoreCase);
            if (!match.Success && info.ChecksumAssetUrl.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase))
                match = Regex.Match(checksumText, "(?i)\\b([a-f0-9]{64})\\b");
            if (!match.Success) throw new InvalidDataException("GitHub 校验文件中没有安装程序对应的 SHA-256。");
            return match.Groups[1].Value.ToUpperInvariant();
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void DeleteIfExists(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
        }
    }
}
