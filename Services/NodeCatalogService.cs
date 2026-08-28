using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GachaLinkFetcher.Services
{
    internal sealed class NodeCatalogService
    {
        private const int NodeCatalogTimeoutMilliseconds = 12000;
        private static readonly Regex ScriptPattern = new Regex("<script[^>]+src=[\\\"']([^\\\"']+)[\\\"']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex NodePattern = new Regex("\\{label:[\\\"'](?:contribute|search)[\\\"'],value:[\\\"']([^\\\"']+)[\\\"']\\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public async Task<List<string>> FetchNodesAsync()
        {
            var html = await UpdateService.DownloadStringWithTimeoutAsync(new Uri(AppInfo.NodeCatalogUrl), NodeCatalogTimeoutMilliseconds);

            var pageUri = new Uri(AppInfo.NodeCatalogUrl);
            var scriptUris = ScriptPattern.Matches(html).Cast<Match>()
                .Select(match => new Uri(pageUri, match.Groups[1].Value))
                .Where(uri => uri.Scheme == Uri.UriSchemeHttps && string.Equals(uri.Host, pageUri.Host, StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToList();

            var scripts = await Task.WhenAll(scriptUris.Select(TryDownloadScriptAsync));
            var nodes = new List<string>();
            foreach (var script in scripts.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                foreach (Match match in NodePattern.Matches(script))
                {
                    var node = match.Groups[1].Value.Trim();
                    Uri nodeUri;
                    if (!Uri.TryCreate("https://" + node + "/", UriKind.Absolute, out nodeUri)) continue;
                    if (string.IsNullOrWhiteSpace(nodeUri.Host) || nodes.Contains(node, StringComparer.OrdinalIgnoreCase)) continue;
                    nodes.Add(node);
                }
            }

            if (nodes.Count < 2)
                throw new InvalidOperationException("未能从实时节点页面解析到多个可用节点。");
            return nodes;
        }

        private static async Task<string> TryDownloadScriptAsync(Uri scriptUri)
        {
            try
            {
                return await UpdateService.DownloadStringWithTimeoutAsync(scriptUri, NodeCatalogTimeoutMilliseconds);
            }
            catch (WebException)
            {
                return string.Empty;
            }
        }

        public async Task<string> SelectFastestAsync(IEnumerable<string> nodes, IProgress<string> progress)
        {
            var candidates = nodes.Where(node => !string.IsNullOrWhiteSpace(node)).Distinct(StringComparer.OrdinalIgnoreCase).Take(24).ToList();
            if (candidates.Count < 2) throw new InvalidOperationException("自动选择至少需要两个实时节点。");
            if (progress != null) progress.Report("正在测试 " + candidates.Count + " 个实时节点…");

            var tests = candidates.Select(TestNodeAsync).ToArray();
            var results = await Task.WhenAll(tests);
            var fastest = results.Where(result => result.Success).OrderBy(result => result.ElapsedMilliseconds).FirstOrDefault();
            if (fastest == null) throw new InvalidOperationException("实时节点均未通过连通性测试，请改用 GitHub 直连或自定义地址。");
            if (progress != null) progress.Report("已选择 " + fastest.Node + "（" + fastest.ElapsedMilliseconds + " ms）");
            return fastest.Node;
        }

        internal static string BuildAcceleratedUrl(string prefix, string originalUrl)
        {
            if (string.IsNullOrWhiteSpace(prefix)) throw new InvalidOperationException("加速地址不能为空。");
            Uri original;
            if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out original) || original.Scheme != Uri.UriSchemeHttps || !string.Equals(original.Host, "github.com", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("只允许加速受信任的 GitHub HTTPS 下载地址。");

            var normalized = prefix.Trim();
            if (!normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) normalized = "https://" + normalized;
            if (normalized.IndexOf("{url}", StringComparison.OrdinalIgnoreCase) >= 0)
                normalized = Regex.Replace(normalized, "\\{url\\}", original.AbsoluteUri, RegexOptions.IgnoreCase);
            else
                normalized = normalized.TrimEnd('/') + "/" + original.AbsoluteUri;

            Uri accelerated;
            if (!Uri.TryCreate(normalized, UriKind.Absolute, out accelerated) || accelerated.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("加速地址必须是有效的 HTTPS 地址。");
            return accelerated.AbsoluteUri;
        }

        private static Task<NodeTestResult> TestNodeAsync(string node)
        {
            return Task.Run(delegate
            {
                var result = new NodeTestResult { Node = node };
                var timer = Stopwatch.StartNew();
                try
                {
                    var testUrl = BuildAcceleratedUrl(node, "https://github.com/H0NG1Y/gacha-link-fetcher/raw/main/README.md");
                    var request = (HttpWebRequest)WebRequest.Create(testUrl);
                    request.Method = "GET";
                    request.UserAgent = "GachaLinkFetcher/" + AppInfo.VersionText;
                    request.Timeout = 4500;
                    request.ReadWriteTimeout = 4500;
                    request.AllowAutoRedirect = true;
                    request.AddRange(0, 0);
                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    {
                        if (stream != null) stream.ReadByte();
                        result.Success = (int)response.StatusCode >= 200 && (int)response.StatusCode < 400;
                    }
                }
                catch (Exception)
                {
                    result.Success = false;
                }
                timer.Stop();
                result.ElapsedMilliseconds = timer.ElapsedMilliseconds;
                return result;
            });
        }

        private sealed class NodeTestResult
        {
            public string Node { get; set; }
            public bool Success { get; set; }
            public long ElapsedMilliseconds { get; set; }
        }
    }
}
