using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

internal static class Program
{
    [STAThread] private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
internal enum GameKind { WutheringWaves, GenshinImpact, HonkaiStarRail, ZenlessZoneZero }
internal sealed class Game
{
    public GameKind Kind; public string Name; public string Record; public string DataFolder; public string Marker; public string[] Roots;
    public Game(GameKind kind, string name, string record, string dataFolder, string marker, params string[] roots)
    { Kind = kind; Name = name; Record = record; DataFolder = dataFolder; Marker = marker; Roots = roots; }
}
internal sealed class Link
{
    public Game Game; public FileInfo File; public string Url;
    public Link(Game game, FileInfo file, string url) { Game = game; File = file; Url = url; }
}
internal sealed class MainForm : Form
{
    private static readonly Regex WutheringPattern = new Regex(@"https://aki-gm-resources(?:-oversea)?\.aki-game\.(?:net|com)/aki/gacha/index\.html#/record[^""\s]*", RegexOptions.Compiled);
    private static readonly Regex HttpPattern = new Regex(@"https://[^\0""\s<>]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Game[] Games =
    {
        new Game(GameKind.WutheringWaves, "鸣潮", "唤取记录", null, null, "Wuthering Waves Game", "Wuthering Waves\\Wuthering Waves Game", "Program Files\\Wuthering Waves\\Wuthering Waves Game", "Program Files\\Epic Games\\WutheringWavesj3oFh", "SteamLibrary\\steamapps\\common\\Wuthering Waves"),
        new Game(GameKind.GenshinImpact, "原神", "祈愿记录", "YuanShen_Data", "hk4e", "Genshin Impact Game", "Genshin Impact\\Genshin Impact Game", "Program Files\\Genshin Impact\\Genshin Impact Game", "Program Files\\HoYoverse\\Genshin Impact Game", "Program Files\\miHoYo Launcher\\games\\Genshin Impact Game", "Program Files\\HoYoPlay\\games\\Genshin Impact Game"),
        new Game(GameKind.HonkaiStarRail, "崩坏：星穹铁道", "跃迁记录", "StarRail_Data", "hkrpg", "Star Rail", "Honkai Star Rail\\Star Rail", "Program Files\\HoYoverse\\Star Rail", "Program Files\\miHoYo Launcher\\games\\Star Rail", "Program Files\\HoYoPlay\\games\\Star Rail"),
        new Game(GameKind.ZenlessZoneZero, "绝区零", "调频记录", "ZenlessZoneZero_Data", "nap", "ZenlessZoneZero Game", "Zenless Zone Zero Game", "Zenless Zone Zero\\ZenlessZoneZero Game", "Program Files\\HoYoverse\\ZenlessZoneZero Game", "Program Files\\miHoYo Launcher\\games\\ZenlessZoneZero Game", "Program Files\\HoYoPlay\\games\\ZenlessZoneZero Game")
    };
    private readonly ComboBox gameBox = new ComboBox();
    private readonly TextBox folderBox = new TextBox();
    private readonly TextBox urlBox = new TextBox();
    private readonly Label instruction = new Label();
    private readonly Label status = new Label();
    private readonly Button copyButton = new Button();
    private readonly Button diagnosticsButton = new Button();
    private string selectedFolder = string.Empty, foundUrl = string.Empty;
    private string lastScanSummary = "尚未执行扫描。请先选择游戏并点击“自动获取链接”。";

    public MainForm()
    {
        Text = "抽卡链接获取工具"; ClientSize = new Size(710, 470); MinimumSize = MaximumSize = new Size(710, 470); StartPosition = FormStartPosition.CenterScreen; Font = new Font("Microsoft YaHei UI", 9F);
        var title = new Label { Text = "抽卡链接获取工具", Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold), Location = new Point(24, 22), AutoSize = true };
        var privacy = new Label { Text = "本地读取 · 不联网 · 不保存链接", ForeColor = Color.FromArgb(45, 125, 70), Location = new Point(27, 59), AutoSize = true };
        var gameLabel = new Label { Text = "游戏：", Location = new Point(27, 91), AutoSize = true };
        gameBox.DropDownStyle = ComboBoxStyle.DropDownList; gameBox.Location = new Point(75, 87); gameBox.Size = new Size(210, 27);
        foreach (var game in Games) gameBox.Items.Add(game.Name);
        gameBox.SelectedIndex = 0; gameBox.SelectedIndexChanged += (_, __) => UpdateInstruction();
        instruction.Location = new Point(27, 129); instruction.Size = new Size(650, 48);
        var folderLabel = new Label { Text = "游戏目录：", Location = new Point(27, 191), AutoSize = true };
        folderBox.Location = new Point(91, 187); folderBox.Size = new Size(480, 27); folderBox.ReadOnly = true; folderBox.Text = "将自动检查常见安装目录";
        var browse = new Button { Text = "手动选择…", Location = new Point(582, 186), Size = new Size(100, 29) }; browse.Click += (_, __) => ChooseFolder();
        var find = new Button { Text = "自动获取链接", Location = new Point(27, 236), Size = new Size(118, 34) }; find.Click += (_, __) => FindUrl();
        copyButton.Text = "复制链接"; copyButton.Location = new Point(157, 236); copyButton.Size = new Size(100, 34); copyButton.Enabled = false; copyButton.Click += (_, __) => CopyUrl();
        diagnosticsButton.Text = "复制诊断信息"; diagnosticsButton.Location = new Point(270, 236); diagnosticsButton.Size = new Size(118, 34); diagnosticsButton.Click += (_, __) => CopyDiagnostics();
        status.Location = new Point(27, 293); status.Size = new Size(650, 42); status.AutoEllipsis = true; status.Text = "准备就绪：请先在游戏里打开一次抽卡记录。";
        urlBox.Location = new Point(27, 341); urlBox.Size = new Size(655, 56); urlBox.Multiline = true; urlBox.ReadOnly = true; urlBox.ScrollBars = ScrollBars.Vertical;
        var warning = new Label { Text = "安全提醒：链接含临时查询凭证。仅粘贴到你信任的统计工具，切勿公开发送。", Location = new Point(27, 411), Size = new Size(650, 26), ForeColor = Color.FromArgb(145, 83, 0) };
        Controls.AddRange(new Control[] { title, privacy, gameLabel, gameBox, instruction, folderLabel, folderBox, browse, find, copyButton, diagnosticsButton, status, urlBox, warning });
        UpdateInstruction();
    }
    private Game SelectedGame { get { return Games[gameBox.SelectedIndex]; } }
    private void UpdateInstruction()
    {
        var game = SelectedGame;
        instruction.Text = "1. 打开《" + game.Name + "》并进入任意卡池\r\n2. 打开「" + game.Record + "」，等待页面加载完成\r\n3. 回到这里，点击「自动获取链接」";
    }
    private void ChooseFolder()
    {
        using (var dialog = new FolderBrowserDialog { Description = "选择游戏实际安装目录（含游戏 EXE 或 *_Data 文件夹）" })
        {
            if (dialog.ShowDialog() != DialogResult.OK) return;
            selectedFolder = dialog.SelectedPath; folderBox.Text = selectedFolder; status.Text = "已选择目录。点击「自动获取链接」开始读取。";
        }
    }
    private void FindUrl()
    {
        var links = new List<Link>(); var games = new[] { SelectedGame }; var rootCount = 0; var fileCount = 0;
        foreach (var game in games)
        foreach (var root in RootsFor(game).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            rootCount++;
            foreach (var file in LinkFiles(game, root))
            {
                fileCount++;
                var url = ExtractUrl(game, file.FullName);
                if (!string.IsNullOrWhiteSpace(url)) links.Add(new Link(game, file, url));
            }
        }
        lastScanSummary = "抽卡链接获取工具 - 诊断信息\r\n游戏：" + SelectedGame.Name
            + "\r\n扫描模式：" + (string.IsNullOrWhiteSpace(selectedFolder) ? "自动检测" : "手动目录")
            + "\r\n已检查目录数：" + rootCount + "\r\n候选日志/缓存文件数：" + fileCount
            + "\r\n匹配结果：" + (links.Count == 0 ? "未找到链接" : "已找到链接")
            + "\r\n说明：诊断信息不包含抽卡链接、账号或文件路径。";
        if (links.Count == 0)
        {
            foundUrl = ""; copyButton.Enabled = false; urlBox.Text = ""; status.Text = "未找到链接。请先在游戏里打开抽卡记录；仍失败时请选择实际游戏安装目录后重试。"; return;
        }
        var newest = links.OrderByDescending(item => item.File.LastWriteTimeUtc).First();
        foundUrl = newest.Url; urlBox.Text = foundUrl; copyButton.Enabled = true; status.Text = "已从《" + newest.Game.Name + "》本地缓存/日志获取链接：" + newest.File.FullName;
    }
    private void CopyUrl()
    {
        if (string.IsNullOrWhiteSpace(foundUrl)) return;
        Clipboard.SetText(foundUrl); status.Text = "链接已复制到剪贴板。请只粘贴至你信任的统计工具。";
    }
    private void CopyDiagnostics()
    {
        Clipboard.SetText(lastScanSummary); status.Text = "已复制不含链接、账号或文件路径的诊断信息。";
    }
    private IEnumerable<string> RootsFor(Game game)
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
        yield return root;
        if (depth == 0) yield break;
        foreach (var child in Directories(root))
        foreach (var item in Descendants(child, depth - 1)) yield return item;
    }
    private static string[] Directories(string path)
    {
        try { return Directory.Exists(path) ? Directory.GetDirectories(path) : new string[0]; }
        catch (IOException) { return new string[0]; }
        catch (UnauthorizedAccessException) { return new string[0]; }
    }
    private static IEnumerable<FileInfo> LinkFiles(Game game, string root)
    {
        if (game.Kind == GameKind.WutheringWaves)
        {
            var files = new[] { Path.Combine(root, "Client", "Saved", "Logs", "Client.log"), Path.Combine(root, "Client", "Binaries", "Win64", "ThirdParty", "KrPcSdk_Global", "KRSDKRes", "KRSDKWebView", "debug.log") };
            return files.Where(File.Exists).Select(path => new FileInfo(path));
        }
        if (game.Kind == GameKind.GenshinImpact)
            return CacheFiles(Path.Combine(root, "YuanShen_Data", "webCaches"), 4)
                .Concat(CacheFiles(Path.Combine(root, "GenshinImpact_Data", "webCaches"), 4));
        return CacheFiles(Path.Combine(root, game.DataFolder, "webCaches"), 4);
    }
    private static IEnumerable<FileInfo> CacheFiles(string root, int depth)
    {
        if (!Directory.Exists(root)) yield break;
        string[] files;
        try { files = Directory.GetFiles(root, "data_*"); }
        catch (IOException) { yield break; }
        catch (UnauthorizedAccessException) { yield break; }
        foreach (var file in files) yield return new FileInfo(file);
        if (depth == 0) yield break;
        foreach (var child in Directories(root))
        foreach (var file in CacheFiles(child, depth - 1)) yield return file;
    }
    private static string ExtractUrl(Game game, string path)
    {
        byte[] bytes;
        try
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var data = new MemoryStream()) { stream.CopyTo(data); bytes = data.ToArray(); }
        }
        catch (IOException) { return null; }
        var direct = FindMatch(game, bytes);
        if (direct != null || game.Kind != GameKind.WutheringWaves) return direct;
        var decoded = new byte[bytes.Length];
        for (var index = 0; index < bytes.Length; index++) decoded[index] = (byte)(bytes[index] ^ ((bytes[index] & 1) == 1 ? 0xA5 : 0xEF));
        return FindMatch(game, decoded);
    }
    private static string FindMatch(Game game, byte[] bytes)
    {
        var text = Encoding.UTF8.GetString(bytes);
        if (game.Kind == GameKind.WutheringWaves)
        {
            var matches = WutheringPattern.Matches(text);
            return matches.Count == 0 ? null : matches[matches.Count - 1].Value;
        }
        var candidates = HttpPattern.Matches(text).Cast<Match>().Select(match => match.Value).ToList();
        try
        {
            var decodedText = Uri.UnescapeDataString(text);
            if (!string.Equals(decodedText, text, StringComparison.Ordinal))
                candidates.AddRange(HttpPattern.Matches(decodedText).Cast<Match>().Select(match => match.Value));
        }
        catch (UriFormatException) { }
        var urls = candidates.Select(url => url.Replace("&amp;", "&"))
            .Where(url => url.IndexOf("authkey=", StringComparison.OrdinalIgnoreCase) >= 0 && url.IndexOf("gacha", StringComparison.OrdinalIgnoreCase) >= 0 && url.IndexOf(game.Marker, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        return urls.Count == 0 ? null : urls[urls.Count - 1];
    }
}
