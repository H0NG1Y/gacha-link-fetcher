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
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private static readonly Regex UrlPattern = new Regex(
        @"https://aki-gm-resources(?:-oversea)?\.aki-game\.(?:net|com)/aki/gacha/index\.html#/record[^\""\s]*",
        RegexOptions.Compiled);

    private readonly TextBox locationBox = new TextBox();
    private readonly TextBox urlBox = new TextBox();
    private readonly Label status = new Label();
    private readonly Button copyButton = new Button();
    private string selectedFolder = string.Empty;
    private string foundUrl = string.Empty;

    public MainForm()
    {
        Text = "鸣潮唤取链接获取器";
        ClientSize = new Size(710, 420);
        MinimumSize = new Size(710, 420);
        MaximumSize = new Size(710, 420);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);

        var title = new Label
        {
            Text = "鸣潮唤取链接获取器",
            Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
            Location = new Point(24, 22),
            AutoSize = true
        };
        var privacy = new Label
        {
            Text = "本地读取 · 不联网 · 不保存链接",
            ForeColor = Color.FromArgb(45, 125, 70),
            Location = new Point(27, 59),
            AutoSize = true
        };
        var steps = new Label
        {
            Text = "1. 打开《鸣潮》并进入任意卡池\r\n2. 点击左下角「唤取记录」，等待页面加载完成\r\n3. 回到这里，点击「自动获取链接」",
            Location = new Point(27, 91),
            Size = new Size(650, 58)
        };
        var locationLabel = new Label { Text = "游戏目录：", Location = new Point(27, 162), AutoSize = true };
        locationBox.Location = new Point(91, 158);
        locationBox.Size = new Size(480, 27);
        locationBox.ReadOnly = true;
        locationBox.Text = "将自动检查常见安装目录";
        var browse = new Button { Text = "手动选择…", Location = new Point(582, 157), Size = new Size(100, 29) };
        browse.Click += (_, __) => ChooseFolder();

        var find = new Button { Text = "自动获取链接", Location = new Point(27, 207), Size = new Size(118, 34) };
        find.Click += (_, __) => FindUrl();
        copyButton.Text = "复制链接";
        copyButton.Location = new Point(157, 207);
        copyButton.Size = new Size(100, 34);
        copyButton.Enabled = false;
        copyButton.Click += (_, __) => CopyUrl();

        status.Location = new Point(27, 264);
        status.Size = new Size(650, 38);
        status.Text = "准备就绪：请先在游戏里打开一次「唤取记录」。";
        status.AutoEllipsis = true;
        urlBox.Location = new Point(27, 309);
        urlBox.Size = new Size(655, 52);
        urlBox.Multiline = true;
        urlBox.ReadOnly = true;
        urlBox.ScrollBars = ScrollBars.Vertical;
        var warning = new Label
        {
            Text = "安全提醒：链接含临时查询凭证。仅粘贴到你信任的统计工具，切勿公开发送。",
            Location = new Point(27, 376),
            Size = new Size(650, 26),
            ForeColor = Color.FromArgb(145, 83, 0)
        };

        Controls.AddRange(new Control[] { title, privacy, steps, locationLabel, locationBox, browse, find, copyButton, status, urlBox, warning });
    }

    private void ChooseFolder()
    {
        using (var dialog = new FolderBrowserDialog { Description = "选择包含 Client 文件夹的《鸣潮》游戏目录" })
        {
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                selectedFolder = dialog.SelectedPath;
                locationBox.Text = selectedFolder;
                status.Text = "已选择目录。点击「自动获取链接」开始读取。";
            }
        }
    }

    private void FindUrl()
    {
        var found = new List<Tuple<FileInfo, string>>();
        foreach (var root in CandidateRoots())
        {
            foreach (var log in LogFilesForRoot(root))
            {
                var url = ExtractUrl(log.FullName);
                if (!string.IsNullOrWhiteSpace(url))
                    found.Add(Tuple.Create(log, url));
            }
        }

        if (found.Count == 0)
        {
            foundUrl = string.Empty;
            copyButton.Enabled = false;
            urlBox.Text = string.Empty;
            status.Text = "未找到链接。请先在游戏里打开「唤取记录」；若仍失败，请手动选择包含 Client 文件夹的游戏目录。";
            return;
        }

        var newest = found.OrderByDescending(item => item.Item1.LastWriteTimeUtc).First();
        foundUrl = newest.Item2;
        urlBox.Text = foundUrl;
        copyButton.Enabled = true;
        status.Text = "已从本地日志获取链接：" + newest.Item1.FullName;
    }

    private void CopyUrl()
    {
        if (string.IsNullOrWhiteSpace(foundUrl)) return;
        Clipboard.SetText(foundUrl);
        status.Text = "链接已复制到剪贴板。请只粘贴至你信任的统计工具。";
    }

    private IEnumerable<string> CandidateRoots()
    {
        if (!string.IsNullOrWhiteSpace(selectedFolder))
        {
            yield return selectedFolder;
            yield return Path.Combine(selectedFolder, "Wuthering Waves Game");
            yield break;
        }

        foreach (var drive in DriveInfo.GetDrives().Where(item => item.IsReady))
        {
            var root = drive.RootDirectory.FullName;
            yield return Path.Combine(root, "Wuthering Waves Game");
            yield return Path.Combine(root, "Wuthering Waves", "Wuthering Waves Game");
            yield return Path.Combine(root, "Program Files", "Wuthering Waves", "Wuthering Waves Game");
            yield return Path.Combine(root, "Program Files", "Epic Games", "WutheringWavesj3oFh");
            yield return Path.Combine(root, "SteamLibrary", "steamapps", "common", "Wuthering Waves");
            yield return Path.Combine(root, "SteamLibrary", "steamapps", "common", "Wuthering Waves", "Wuthering Waves Game");
            yield return Path.Combine(root, "Steam", "steamapps", "common", "Wuthering Waves");
            yield return Path.Combine(root, "Steam", "steamapps", "common", "Wuthering Waves", "Wuthering Waves Game");
        }
    }

    private static IEnumerable<FileInfo> LogFilesForRoot(string root)
    {
        var names = new[]
        {
            Path.Combine(root, "Client", "Saved", "Logs", "Client.log"),
            Path.Combine(root, "Client", "Binaries", "Win64", "ThirdParty", "KrPcSdk_Global", "KRSDKRes", "KRSDKWebView", "debug.log")
        };
        return names.Where(File.Exists).Select(path => new FileInfo(path));
    }

    private static string ExtractUrl(string path)
    {
        byte[] bytes;
        try
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var data = new MemoryStream())
            {
                stream.CopyTo(data);
                bytes = data.ToArray();
            }
        }
        catch (IOException)
        {
            return null;
        }

        var direct = FindLastMatch(bytes);
        if (direct != null) return direct;
        var decoded = new byte[bytes.Length];
        for (var index = 0; index < bytes.Length; index++)
            decoded[index] = (byte)(bytes[index] ^ ((bytes[index] & 1) == 1 ? 0xA5 : 0xEF));
        return FindLastMatch(decoded);
    }

    private static string FindLastMatch(byte[] bytes)
    {
        var text = Encoding.UTF8.GetString(bytes);
        var matches = UrlPattern.Matches(text);
        return matches.Count == 0 ? null : matches[matches.Count - 1].Value;
    }
}
