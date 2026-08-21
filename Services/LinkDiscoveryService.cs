using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GachaLinkFetcher.Models;

namespace GachaLinkFetcher.Services
{
    internal sealed class LinkDiscoveryResult
    {
        public string Url; public string SourceFile; public int RootCount; public int FileCount;
    }

    internal sealed class LinkDiscoveryService
    {
        private static readonly Regex WutheringPattern = new Regex(@"https://aki-gm-resources(?:-oversea)?\.aki-game\.(?:net|com)/aki/gacha/index\.html#/record[^\""\s]*", RegexOptions.Compiled);
        private static readonly Regex HttpPattern = new Regex(@"https://[^\0\""\s<>]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public LinkDiscoveryResult Find(GameDefinition game, string selectedFolder)
        {
            var candidates = new List<Tuple<FileInfo, string>>(); var rootCount = 0; var fileCount = 0;
            var scannedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in RootsFor(game, selectedFolder).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                rootCount++;
                foreach (var file in LinkFiles(game, root))
                {
                    if (!scannedFiles.Add(file.FullName)) continue;
                    fileCount++;
                    var url = ExtractUrl(game, file.FullName);
                    if (!string.IsNullOrWhiteSpace(url)) candidates.Add(Tuple.Create(file, url));
                }
            }
            foreach (var file in UserLogFiles(game))
            {
                if (!scannedFiles.Add(file.FullName)) continue;
                fileCount++;
                var url = ExtractUrl(game, file.FullName);
                if (!string.IsNullOrWhiteSpace(url)) candidates.Add(Tuple.Create(file, url));
            }
            var latest = candidates.OrderByDescending(item => item.Item1.LastWriteTimeUtc).FirstOrDefault();
            var resultUrl = latest == null ? null : latest.Item2;
            if (game.Kind == GameKind.HonkaiStarRail) resultUrl = PortableStarRailUrl(resultUrl);
            return new LinkDiscoveryResult { Url = resultUrl, SourceFile = latest == null ? null : latest.Item1.FullName, RootCount = rootCount, FileCount = fileCount };
        }

        private static IEnumerable<string> RootsFor(GameDefinition game, string selectedFolder)
        {
            if (!string.IsNullOrWhiteSpace(selectedFolder))
            {
                yield return selectedFolder;
                foreach (var child in Directories(selectedFolder)) yield return child;
                yield break;
            }
            foreach (var drive in DriveInfo.GetDrives().Where(item => item.IsReady))
            {
                foreach (var suffix in game.Roots) yield return Path.Combine(drive.RootDirectory.FullName, suffix);
                if (game.Kind == GameKind.WutheringWaves)
                    foreach (var root in WeGameRoots(drive.RootDirectory.FullName)) yield return root;
            }
        }
        private static IEnumerable<string> WeGameRoots(string driveRoot)
        {
            foreach (var library in new[] { Path.Combine(driveRoot, "WeGameApps", "rail_apps"), Path.Combine(driveRoot, "WeGameApps", "apps") })
            foreach (var app in Directories(library))
            {
                var name = Path.GetFileName(app);
                if (string.Equals(name, "WeGame", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "WeGameInstaller", StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var root in Descendants(app, 2)) yield return root;
            }
        }
        private static IEnumerable<string> Descendants(string root, int depth)
        {
            yield return root; if (depth == 0) yield break;
            foreach (var child in Directories(root)) foreach (var item in Descendants(child, depth - 1)) yield return item;
        }
        private static string[] Directories(string path)
        {
            try { return Directory.Exists(path) ? Directory.GetDirectories(path) : new string[0]; }
            catch (IOException) { return new string[0]; } catch (UnauthorizedAccessException) { return new string[0]; }
        }
        private static IEnumerable<FileInfo> LinkFiles(GameDefinition game, string root)
        {
            if (game.Kind == GameKind.WutheringWaves)
            {
                var files = new[] { Path.Combine(root, "Client", "Saved", "Logs", "Client.log"), Path.Combine(root, "Client", "Binaries", "Win64", "ThirdParty", "KrPcSdk_Global", "KRSDKRes", "KRSDKWebView", "debug.log") };
                return files.Where(File.Exists).Select(path => new FileInfo(path));
            }
            if (game.Kind == GameKind.GenshinImpact)
                return CacheFiles(Path.Combine(root, "YuanShen_Data", "webCaches"), 4).Concat(CacheFiles(Path.Combine(root, "GenshinImpact_Data", "webCaches"), 4));
            return CacheFiles(Path.Combine(root, game.DataFolder, "webCaches"), 4);
        }
        private static IEnumerable<FileInfo> CacheFiles(string root, int depth)
        {
            if (!Directory.Exists(root)) yield break;
            string[] files; try { files = Directory.GetFiles(root, "data_*"); } catch (IOException) { yield break; } catch (UnauthorizedAccessException) { yield break; }
            foreach (var file in files) yield return new FileInfo(file);
            if (depth == 0) yield break;
            foreach (var child in Directories(root)) foreach (var file in CacheFiles(child, depth - 1)) yield return file;
        }
        private static IEnumerable<FileInfo> UserLogFiles(GameDefinition game)
        {
            if (game.Kind == GameKind.WutheringWaves) yield break;
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(userProfile)) yield break;
            var localLow = Path.Combine(userProfile, "AppData", "LocalLow");
            string[] gameFolders;
            if (game.Kind == GameKind.GenshinImpact)
                gameFolders = new[] { "miHoYo\\原神", "miHoYo\\Genshin Impact", "Cognosphere\\Genshin Impact", "HoYoverse\\Genshin Impact" };
            else if (game.Kind == GameKind.HonkaiStarRail)
                gameFolders = new[] { "miHoYo\\崩坏：星穹铁道", "miHoYo\\Star Rail", "Cognosphere\\Star Rail", "HoYoverse\\Star Rail" };
            else
                gameFolders = new[] { "miHoYo\\绝区零", "miHoYo\\ZenlessZoneZero", "Cognosphere\\ZenlessZoneZero", "HoYoverse\\ZenlessZoneZero" };
            foreach (var gameFolder in gameFolders)
            {
                var root = Path.Combine(localLow, gameFolder);
                foreach (var name in new[] { "Player.log", "Player-prev.log", "output_log.txt" })
                {
                    var path = Path.Combine(root, name);
                    if (File.Exists(path)) yield return new FileInfo(path);
                }
            }
        }
        private static string ExtractUrl(GameDefinition game, string path)
        {
            byte[] bytes;
            try { using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) using (var data = new MemoryStream()) { stream.CopyTo(data); bytes = data.ToArray(); } }
            catch (IOException) { return null; }
            var direct = FindMatch(game, bytes); if (direct != null || game.Kind != GameKind.WutheringWaves) return direct;
            var decoded = new byte[bytes.Length]; for (var index = 0; index < bytes.Length; index++) decoded[index] = (byte)(bytes[index] ^ ((bytes[index] & 1) == 1 ? 0xA5 : 0xEF));
            return FindMatch(game, decoded);
        }
        private static string FindMatch(GameDefinition game, byte[] bytes)
        {
            var text = Encoding.UTF8.GetString(bytes);
            if (game.Kind == GameKind.WutheringWaves) { var matches = WutheringPattern.Matches(text); return matches.Count == 0 ? null : matches[matches.Count - 1].Value; }
            var candidates = HttpPattern.Matches(text).Cast<Match>().Select(match => match.Value).ToList();
            try { var decoded = Uri.UnescapeDataString(text); if (!string.Equals(decoded, text, StringComparison.Ordinal)) candidates.AddRange(HttpPattern.Matches(decoded).Cast<Match>().Select(match => match.Value)); } catch (UriFormatException) { }
            var urls = candidates.Select(url => url.Replace("&amp;", "&")).Where(url => url.IndexOf("authkey=", StringComparison.OrdinalIgnoreCase) >= 0 && url.IndexOf("gacha", StringComparison.OrdinalIgnoreCase) >= 0 && url.IndexOf(game.Marker, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            return urls.Count == 0 ? null : urls[urls.Count - 1];
        }

        private static string PortableStarRailUrl(string sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl)) return sourceUrl;
            Uri uri;
            if (!Uri.TryCreate(sourceUrl.TrimEnd(',', ';'), UriKind.Absolute, out uri)) return sourceUrl;
            var query = ParseQuery(uri.Query);
            string authkey;
            if (!query.TryGetValue("authkey", out authkey) || string.IsNullOrWhiteSpace(authkey)) return sourceUrl;

            string gameBiz;
            query.TryGetValue("game_biz", out gameBiz);
            var overseas = (!string.IsNullOrWhiteSpace(gameBiz) && gameBiz.EndsWith("_global", StringComparison.OrdinalIgnoreCase))
                || uri.Host.EndsWith("hoyoverse.com", StringComparison.OrdinalIgnoreCase);
            var endpoint = overseas
                ? "https://public-operation-hkrpg-sg.hoyoverse.com/common/gacha_record/api/getGachaLog"
                : "https://api-takumi.mihoyo.com/common/gacha_record/api/getGachaLog";

            var requiredKeys = new[] { "authkey_ver", "sign_type", "authkey", "game_biz", "lang" };
            var values = new List<string>();
            foreach (var key in requiredKeys)
            {
                string value;
                if (query.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
                    values.Add(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value));
            }
            return endpoint + "?" + string.Join("&", values);
        }

        private static Dictionary<string, string> ParseQuery(string value)
        {
            var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(value)) return output;
            foreach (var part in value.TrimStart('?').Split('&'))
            {
                var pair = part.Split(new[] { '=' }, 2);
                if (pair.Length != 2) continue;
                try { output[Uri.UnescapeDataString(pair[0])] = Uri.UnescapeDataString(pair[1]); }
                catch (UriFormatException) { }
            }
            return output;
        }
    }
}
