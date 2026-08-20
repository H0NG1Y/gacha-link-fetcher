using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GachaLinkFetcher.Models;
using GachaLinkFetcher.Services;
using GachaLinkFetcher.Storage;

namespace GachaLinkFetcher.UI
{
    internal sealed class MainForm : Form
    {
        private static readonly GameDefinition[] Games =
        {
            new GameDefinition(GameKind.WutheringWaves, "鸣潮", "唤取记录", null, null, "Wuthering Waves Game", "Wuthering Waves\\Wuthering Waves Game", "Program Files\\Wuthering Waves\\Wuthering Waves Game", "Program Files\\Epic Games\\WutheringWavesj3oFh", "SteamLibrary\\steamapps\\common\\Wuthering Waves"),
            new GameDefinition(GameKind.GenshinImpact, "原神", "祈愿记录", "YuanShen_Data", "hk4e", "Genshin Impact Game", "Genshin Impact\\Genshin Impact Game", "Program Files\\Genshin Impact\\Genshin Impact Game", "Program Files\\HoYoverse\\Genshin Impact Game", "Program Files\\miHoYo Launcher\\games\\Genshin Impact Game", "Program Files\\HoYoPlay\\games\\Genshin Impact Game"),
            new GameDefinition(GameKind.HonkaiStarRail, "崩坏：星穹铁道", "跃迁记录", "StarRail_Data", "hkrpg", "Star Rail", "Honkai Star Rail\\Star Rail", "Program Files\\HoYoverse\\Star Rail", "Program Files\\miHoYo Launcher\\games\\Star Rail", "Program Files\\HoYoPlay\\games\\Star Rail"),
            new GameDefinition(GameKind.ZenlessZoneZero, "绝区零", "调频记录", "ZenlessZoneZero_Data", "nap", "Zenless Zone Zero Game", "Zenless Zone Zero Game", "Zenless Zone Zero\\ZenlessZoneZero Game", "Program Files\\HoYoverse\\ZenlessZoneZero Game", "Program Files\\miHoYo Launcher\\games\\ZenlessZoneZero Game", "Program Files\\HoYoPlay\\games\\ZenlessZoneZero Game")
        };
        private readonly LinkDiscoveryService discovery = new LinkDiscoveryService();
        private readonly GameApiClient api = new GameApiClient(); private readonly LocalStore store = new LocalStore();
        private readonly ExportService exporter = new ExportService(); private readonly AnalyticsService analytics = new AnalyticsService();
        private readonly ComboBox gameBox = new ComboBox(); private readonly ComboBox poolBox = new ComboBox(); private readonly TextBox folderBox = new TextBox(); private readonly TextBox urlBox = new TextBox();
        private readonly Label instruction = new Label(); private readonly Label status = new Label(); private readonly Label summary = new Label();
        private Button copyButton; private Button syncButton; private Button findButton;
        private readonly DataGridView grid = new DataGridView(); private string selectedFolder = string.Empty, foundUrl = string.Empty; private bool updatingPool;
        private string lastScanSummary = "尚未执行扫描。请先选择游戏并点击“自动获取链接”。";

        public MainForm()
        {
            Text = "抽卡链接获取工具"; ClientSize = new Size(900, 730); MinimumSize = new Size(900, 730); StartPosition = FormStartPosition.CenterScreen; Font = new Font("Microsoft YaHei UI", 9F);
            var title = new Label { Text = "抽卡链接获取工具", Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold), Location = new Point(24, 18), AutoSize = true };
            var privacy = new Label { Text = "本地读取 · 手动同步时才请求官方记录接口 · 数据仅保存本机", ForeColor = Color.FromArgb(45, 125, 70), Location = new Point(27, 54), AutoSize = true };
            var gameLabel = new Label { Text = "游戏：", Location = new Point(27, 87), AutoSize = true };
            gameBox.DropDownStyle = ComboBoxStyle.DropDownList; gameBox.Location = new Point(75, 83); gameBox.Size = new Size(210, 27); foreach (var game in Games) gameBox.Items.Add(game.Name); gameBox.SelectedIndex = 0; gameBox.SelectedIndexChanged += (_, __) => { UpdateInstruction(); UpdatePoolFilter(); };
            var poolLabel = new Label { Text = "卡池：", Location = new Point(315, 87), AutoSize = true };
            poolBox.DropDownStyle = ComboBoxStyle.DropDownList; poolBox.Location = new Point(363, 83); poolBox.Size = new Size(240, 27); poolBox.SelectedIndexChanged += (_, __) => { if (!updatingPool) RefreshRecords(); };
            instruction.Location = new Point(27, 120); instruction.Size = new Size(650, 42);
            var folderLabel = new Label { Text = "游戏目录：", Location = new Point(27, 173), AutoSize = true }; folderBox.Location = new Point(91, 169); folderBox.Size = new Size(640, 27); folderBox.ReadOnly = true; folderBox.Text = "将自动检查常见安装目录";
            var browse = MakeButton("手动选择…", 744, 168, 120, (_, __) => ChooseFolder()); findButton = MakeButton("自动获取链接", 27, 211, 118, (_, __) => FindUrl()); copyButton = MakeButton("复制链接", 157, 211, 100, (_, __) => CopyUrl()); copyButton.Enabled = false;
            syncButton = MakeButton("同步记录", 270, 211, 100, (_, __) => Sync()); var diagnostics = MakeButton("复制诊断信息", 383, 211, 118, (_, __) => CopyDiagnostics());
            urlBox.Location = new Point(27, 255); urlBox.Size = new Size(837, 44); urlBox.Multiline = true; urlBox.ReadOnly = true; urlBox.ScrollBars = ScrollBars.Vertical;
            status.Location = new Point(27, 307); status.Size = new Size(837, 36); status.AutoEllipsis = true; status.Text = "准备就绪：先在游戏里打开一次抽卡记录。";
            var dataBox = new GroupBox { Text = "本地数据（自动备份保留最近 20 份）", Location = new Point(22, 347), Size = new Size(846, 85) };
            dataBox.Controls.AddRange(new Control[] { MakeButton("刷新列表", 16, 27, 92, (_, __) => RefreshRecords()), MakeButton("数据分析", 119, 27, 92, (_, __) => ShowAnalysis()), MakeButton("导出 CSV", 222, 27, 92, (_, __) => Export("csv")), MakeButton("导出 Excel", 325, 27, 100, (_, __) => Export("excel")), MakeButton("导出 JSON", 436, 27, 100, (_, __) => Export("json")), MakeButton("导出 UIGF", 547, 27, 100, (_, __) => Export("uigf")), MakeButton("立即备份", 658, 27, 82, (_, __) => Backup()), MakeButton("恢复备份", 748, 27, 82, (_, __) => Restore()) });
            summary.Location = new Point(27, 440); summary.Size = new Size(837, 38); summary.Text = "同步后将在此显示基础统计。";
            grid.Location = new Point(27, 484); grid.Size = new Size(837, 215); grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            Controls.AddRange(new Control[] { title, privacy, gameLabel, gameBox, poolLabel, poolBox, instruction, folderLabel, folderBox, browse, findButton, copyButton, syncButton, diagnostics, urlBox, status, dataBox, summary, grid }); UpdateInstruction(); UpdatePoolFilter();
        }
        private Button MakeButton(string text, int x, int y, int width, EventHandler click) { var button = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 29) }; button.Click += click; return button; }
        private GameDefinition SelectedGame { get { return Games[gameBox.SelectedIndex]; } }
        private GachaPoolDefinition SelectedPool { get { return poolBox.SelectedItem as GachaPoolDefinition; } }
        private void UpdateInstruction() { var game = SelectedGame; instruction.Text = "1. 打开《" + game.Name + "》并进入任意卡池\r\n2. 打开「" + game.RecordName + "」，等待页面加载完成\r\n3. 点击“自动获取链接”；确认后可点击“同步记录”下载官方记录数据"; }
        private void ChooseFolder() { using (var dialog = new FolderBrowserDialog { Description = "选择游戏实际安装目录（含游戏 EXE 或 *_Data 文件夹）" }) { if (dialog.ShowDialog() != DialogResult.OK) return; selectedFolder = dialog.SelectedPath; folderBox.Text = selectedFolder; status.Text = "已选择目录。点击“自动获取链接”开始读取。"; } }
        private void FindUrl()
        {
            var result = discovery.Find(SelectedGame, selectedFolder); lastScanSummary = "抽卡链接获取工具 - 诊断信息\r\n游戏：" + SelectedGame.Name + "\r\n扫描模式：" + (string.IsNullOrWhiteSpace(selectedFolder) ? "自动检测" : "手动目录") + "\r\n已检查目录数：" + result.RootCount + "\r\n候选日志/缓存文件数：" + result.FileCount + "\r\n匹配结果：" + (string.IsNullOrWhiteSpace(result.Url) ? "未找到链接" : "已找到链接") + "\r\n说明：诊断信息不包含抽卡链接、账号或文件路径。";
            if (string.IsNullOrWhiteSpace(result.Url)) { foundUrl = ""; copyButton.Enabled = false; urlBox.Text = ""; status.Text = "未找到链接。请先在游戏里打开抽卡记录；仍失败时请选择实际游戏安装目录后重试。"; return; }
            foundUrl = result.Url; urlBox.Text = foundUrl; copyButton.Enabled = true; status.Text = "已从《" + SelectedGame.Name + "》本地缓存/日志获取链接。链接只保留在内存中，不会写入数据文件。";
        }
        private void CopyUrl() { if (string.IsNullOrWhiteSpace(foundUrl)) return; Clipboard.SetText(foundUrl); status.Text = "链接已复制到剪贴板。请只粘贴至你信任的统计工具。"; }
        private void CopyDiagnostics() { Clipboard.SetText(lastScanSummary); status.Text = "已复制不含链接、账号或文件路径的诊断信息。"; }
        private void Sync()
        {
            if (string.IsNullOrWhiteSpace(foundUrl)) { MessageBox.Show("请先获取链接。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (MessageBox.Show("将使用当前链接中的临时凭证向该游戏官方记录接口请求数据。\r\n不会上传本地日志，链接不会保存到本地。是否继续？", "确认同步", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            syncButton.Enabled = false; findButton.Enabled = false; status.Text = "正在请求官方记录接口，请稍候…"; var game = SelectedGame;
            Task.Factory.StartNew(() => api.Download(game.Kind, foundUrl)).ContinueWith(task => BeginInvoke(new Action(() =>
            {
                syncButton.Enabled = true; findButton.Enabled = true;
                if (task.IsFaulted) { var message = task.Exception.GetBaseException().Message; status.Text = "同步失败：" + message; MessageBox.Show(message, "同步失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var added = store.Merge(task.Result.Records); status.Text = task.Result.Message + " 新增并保存 " + added + " 条；已有记录已自动去重。"; UpdatePoolFilter();
            })));
        }
        private List<GachaRecord> AllGameRecords() { return store.Load().Records.Where(item => item.Game == SelectedGame.Kind.ToString()).OrderByDescending(item => item.Time).ToList(); }
        private List<GachaRecord> CurrentRecords()
        {
            var records = AllGameRecords(); var pool = SelectedPool;
            return pool == null || string.IsNullOrWhiteSpace(pool.Code) ? records : records.Where(item => GachaPoolCatalog.CanonicalCode(SelectedGame.Kind, item.GachaType) == pool.Code).ToList();
        }
        private void UpdatePoolFilter()
        {
            var selectedCode = SelectedPool == null ? string.Empty : GachaPoolCatalog.CanonicalCode(SelectedGame.Kind, SelectedPool.Code); updatingPool = true; poolBox.Items.Clear();
            poolBox.Items.Add(new GachaPoolDefinition(string.Empty, "全部卡池"));
            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pool in GachaPoolCatalog.ForGame(SelectedGame.Kind)) { poolBox.Items.Add(pool); known.Add(pool.Code); }
            foreach (var code in AllGameRecords().Select(item => GachaPoolCatalog.CanonicalCode(SelectedGame.Kind, item.GachaType)).Where(code => !string.IsNullOrWhiteSpace(code) && !known.Contains(code)).Distinct().OrderBy(code => code))
                poolBox.Items.Add(new GachaPoolDefinition(code, GachaPoolCatalog.NameFor(SelectedGame.Kind, code)));
            var selectedIndex = 0;
            for (var index = 0; index < poolBox.Items.Count; index++) { var pool = poolBox.Items[index] as GachaPoolDefinition; if (pool != null && pool.Code == selectedCode) { selectedIndex = index; break; } }
            poolBox.SelectedIndex = selectedIndex; updatingPool = false; RefreshRecords();
        }
        private void RefreshRecords() { var records = CurrentRecords(); grid.DataSource = records.Select(item => new { 卡池 = GachaPoolCatalog.NameFor(SelectedGame.Kind, item.GachaType), 名称 = item.Name, 类型 = item.ItemType, 星级 = item.RankType, 时间 = item.Time, UID = item.Uid }).ToList(); summary.Text = analytics.BuildSummary(records); }
        private void ShowAnalysis() { MessageBox.Show(analytics.BuildSummary(CurrentRecords()), "《" + SelectedGame.Name + "》" + (SelectedPool == null ? "" : SelectedPool.Name) + "数据分析", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        private void Export(string format)
        {
            var records = CurrentRecords(); if (records.Count == 0) { MessageBox.Show("当前筛选条件下没有可导出的本地记录。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var poolName = SelectedPool == null || string.IsNullOrWhiteSpace(SelectedPool.Code) ? "全部卡池" : SelectedPool.Name;
            var filters = format == "csv" ? "CSV 文件|*.csv" : format == "excel" ? "Excel XML 文件|*.xml" : "JSON 文件|*.json"; using (var dialog = new SaveFileDialog { Filter = filters, FileName = SelectedGame.Name + "-" + poolName + "-抽卡记录-" + DateTime.Now.ToString("yyyyMMdd") })
            { if (dialog.ShowDialog() != DialogResult.OK) return; try { if (format == "csv") exporter.ExportCsv(records, dialog.FileName); else if (format == "excel") exporter.ExportExcelXml(records, dialog.FileName); else if (format == "uigf") exporter.ExportUigf(records, dialog.FileName); else exporter.ExportJson(records, dialog.FileName); status.Text = "已导出 " + records.Count + " 条记录：" + dialog.FileName; } catch (Exception ex) { MessageBox.Show(ex.Message, "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); } }
        }
        private void Backup() { var path = store.CreateBackup(); status.Text = "已创建本地备份：" + path; }
        private void Restore() { using (var dialog = new OpenFileDialog { Filter = "JSON 备份文件|*.json", InitialDirectory = store.BackupDirectory }) { if (dialog.ShowDialog() != DialogResult.OK) return; try { store.Restore(dialog.FileName); status.Text = "已恢复备份。"; UpdatePoolFilter(); } catch (Exception ex) { MessageBox.Show(ex.Message, "恢复失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); } } }
    }
}
