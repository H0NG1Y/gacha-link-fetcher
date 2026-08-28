using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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
            new GameDefinition(GameKind.GenshinImpact, "原神", "祈愿记录", "YuanShen_Data", "hk4e", "Genshin Impact Game", "Genshin Impact\\Genshin Impact Game", "Program Files\\Genshin Impact\\Genshin Impact Game", "Program Files\\HoYoverse\\Genshin Impact Game", "Program Files\\miHoYo Launcher\\games\\Genshin Impact Game", "Program Files\\HoYoPlay\\games\\Genshin Impact Game"),
            new GameDefinition(GameKind.HonkaiStarRail, "崩坏：星穹铁道", "跃迁记录", "StarRail_Data", "hkrpg", "Star Rail", "Honkai Star Rail\\Star Rail", "Program Files\\HoYoverse\\Star Rail", "Program Files\\miHoYo Launcher\\games\\Star Rail", "Program Files\\HoYoPlay\\games\\Star Rail"),
            new GameDefinition(GameKind.ZenlessZoneZero, "绝区零", "调频记录", "ZenlessZoneZero_Data", "nap", "Zenless Zone Zero Game", "Zenless Zone Zero Game", "Zenless Zone Zero\\ZenlessZoneZero Game", "Program Files\\HoYoverse\\ZenlessZoneZero Game", "Program Files\\miHoYo Launcher\\games\\ZenlessZoneZero Game", "Program Files\\HoYoPlay\\games\\ZenlessZoneZero Game"),
            new GameDefinition(GameKind.WutheringWaves, "鸣潮", "唤取记录", null, null, "Wuthering Waves Game", "Wuthering Waves\\Wuthering Waves Game", "Program Files\\Wuthering Waves\\Wuthering Waves Game", "Program Files\\Epic Games\\WutheringWavesj3oFh", "SteamLibrary\\steamapps\\common\\Wuthering Waves")
        };

        private static readonly Color[] GameColors =
        {
            Color.FromArgb(66, 133, 244),
            Color.FromArgb(123, 97, 255),
            Color.FromArgb(249, 171, 0),
            Color.FromArgb(0, 150, 136)
        };

        private static readonly string[] GameShortNames = { "原", "崩", "绝", "鸣" };
        private static readonly string[] GameCompactNames = { "原神", "崩铁", "绝区零", "鸣潮" };

        private readonly LinkDiscoveryService discovery = new LinkDiscoveryService();
        private readonly GameApiClient api = new GameApiClient();
        private readonly LocalStore store = new LocalStore();
        private readonly ExportService exporter = new ExportService();
        private readonly AnalyticsService analytics = new AnalyticsService();
        private readonly UpdateService updateService = new UpdateService();

        private readonly ModernComboBox poolBox = new ModernComboBox();
        private readonly TextBox folderBox = new TextBox();
        private readonly TextBox urlBox = new TextBox();
        private readonly Label instruction = new Label();
        private readonly Label status = new Label();
        private readonly TextBox summary = new TextBox();
        private readonly Label statusDot = new Label();
        private readonly Label heroTitle = new Label();
        private readonly Label heroSubtitle = new Label();
        private readonly GameBadge gameBadge = new GameBadge();
        private readonly DataGridView grid = new DataGridView();
        private readonly List<GameNavButton> gameButtons = new List<GameNavButton>();

        private ModernButton copyButton;
        private ModernButton syncButton;
        private ModernButton findButton;
        private ModernButton aboutButton;
        private ModernButton updateButton;
        private TableLayoutPanel workspace;
        private TableLayoutPanel sidebarLayout;
        private TableLayoutPanel contentLayout;
        private TableLayoutPanel actionLayout;
        private TableLayoutPanel recordsLayout;
        private ModernCard sidebarHelp;
        private Panel heroPoolHost;
        private Label topSubtitle;
        private ModernCard privacyChip;
        private Panel topBar;
        private Panel windowControlHost;
        private WindowControlButton maximizeButton;
        private UpdateInfo availableUpdate;
        private bool updateCheckInProgress;
        private bool coldStartNotificationShown;

        private int selectedGameIndex;
        private string selectedFolder = string.Empty;
        private string foundUrl = string.Empty;
        private bool updatingPool;
        private string lastScanSummary = "尚未执行扫描。请先选择游戏并点击“自动获取链接”。";

        public MainForm()
        {
            Text = "抽卡链接获取工具";
            ClientSize = new Size(1180, 820);
            MinimumSize = new Size(880, 680);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = UiColors.Canvas;
            AutoScaleMode = AutoScaleMode.Dpi;
            KeyPreview = true;
            BuildInterface();
            Resize += delegate { ApplyResponsiveLayout(); UpdateMaximizeButton(); UpdateWindowShape(); };
            Shown += async delegate { await CheckForUpdatesAsync(true, false); };
            SelectGame(0);
            ApplyResponsiveLayout();
        }

        private void BuildInterface()
        {
            SuspendLayout();
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = UiColors.Canvas, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, Padding = Padding.Empty };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.Controls.Add(BuildTopBar(), 0, 0);
            workspace = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = UiColors.Canvas, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 218F));
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            workspace.Controls.Add(BuildSidebar(), 0, 0);
            workspace.Controls.Add(BuildContent(), 1, 0);
            root.Controls.Add(workspace, 0, 1);
            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildTopBar()
        {
            topBar = new Panel { Dock = DockStyle.Fill, BackColor = UiColors.Surface, Margin = Padding.Empty };
            var bar = topBar;
            var logo = new AppLogo { Location = new Point(22, 17) };
            var title = new Label { Text = "抽卡链接获取工具", Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold), ForeColor = UiColors.Text, Location = new Point(78, 12), Size = new Size(260, 30) };
            topSubtitle = new Label { Text = "获取链接 · 同步记录 · 导出分析 · 本地备份", ForeColor = UiColors.TextMuted, Location = new Point(80, 43), AutoSize = true };
            privacyChip = new ModernCard { Radius = 16, BackColor = Color.FromArgb(230, 244, 234), Size = new Size(278, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(880, 20) };
            privacyChip.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "●  数据仅保存在本机", TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold), ForeColor = UiColors.Success });
            windowControlHost = new Panel { Dock = DockStyle.Right, Width = 140, BackColor = UiColors.Surface };
            var minimizeButton = new WindowControlButton(WindowButtonKind.Minimize) { Location = new Point(4, 19) };
            maximizeButton = new WindowControlButton(WindowButtonKind.Maximize) { Location = new Point(48, 19) };
            var closeButton = new WindowControlButton(WindowButtonKind.Close) { Location = new Point(92, 19) };
            minimizeButton.Click += delegate { WindowState = FormWindowState.Minimized; };
            maximizeButton.Click += delegate { ToggleMaximize(); };
            closeButton.Click += delegate { Close(); };
            windowControlHost.Controls.AddRange(new Control[] { minimizeButton, maximizeButton, closeButton });
            bar.Resize += delegate { PositionTopBarControls(); };
            bar.MouseDown += BeginWindowDrag;
            title.MouseDown += BeginWindowDrag;
            topSubtitle.MouseDown += BeginWindowDrag;
            logo.MouseDown += BeginWindowDrag;
            bar.DoubleClick += delegate { ToggleMaximize(); };
            title.DoubleClick += delegate { ToggleMaximize(); };
            topSubtitle.DoubleClick += delegate { ToggleMaximize(); };
            logo.DoubleClick += delegate { ToggleMaximize(); };
            bar.Controls.AddRange(new Control[] { logo, title, topSubtitle, privacyChip, windowControlHost, new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = UiColors.Border } });
            return bar;
        }

        private Panel BuildSidebar()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiColors.Canvas, Padding = new Padding(12, 18, 12, 16), Margin = Padding.Empty };
            sidebarLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = UiColors.Canvas, Margin = Padding.Empty, Padding = Padding.Empty };
            sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 216F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 118F));
            var caption = new Label { Text = "选择游戏", Dock = DockStyle.Top, Height = 34, Padding = new Padding(10, 0, 0, 0), Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold), ForeColor = UiColors.TextMuted };
            var navigation = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 216, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = UiColors.Canvas, Margin = Padding.Empty, Padding = Padding.Empty };
            for (var index = 0; index < Games.Length; index++)
            {
                var gameIndex = index;
                var button = new GameNavButton { FullText = Games[index].Name, ShortText = GameCompactNames[index], Text = Games[index].Name, AccentColor = GameColors[index], Width = 190, Margin = new Padding(0, 0, 0, 6) };
                button.Click += delegate { SelectGame(gameIndex); };
                gameButtons.Add(button);
                navigation.Controls.Add(button);
            }
            var sidebarActions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = UiColors.Canvas, Margin = Padding.Empty, Padding = new Padding(0, 2, 0, 2) };
            updateButton = MakeButton("发现新版本", 190, ModernButtonStyle.Primary, delegate { ShowUpdateDialog(); });
            updateButton.Margin = new Padding(0, 0, 0, 6);
            updateButton.Visible = false;
            aboutButton = MakeButton("关于与更新", 190, ModernButtonStyle.Text, delegate { ShowAbout(); });
            aboutButton.Margin = Padding.Empty;
            sidebarActions.Controls.Add(updateButton);
            sidebarActions.Controls.Add(aboutButton);

            sidebarHelp = new ModernCard { Dock = DockStyle.Fill, Height = 118, Radius = 16, BackColor = Color.FromArgb(235, 241, 250), Padding = new Padding(15), Margin = Padding.Empty };
            var helpTitle = new Label { Text = "使用提示", Dock = DockStyle.Top, Height = 26, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold), ForeColor = UiColors.Text };
            var helpText = new Label { Text = "先在游戏中打开抽卡记录页，等待页面加载完成后再获取链接。", Dock = DockStyle.Fill, ForeColor = UiColors.TextMuted };
            sidebarHelp.Controls.Add(helpText);
            sidebarHelp.Controls.Add(helpTitle);
            sidebarLayout.Controls.Add(caption, 0, 0);
            sidebarLayout.Controls.Add(navigation, 0, 1);
            sidebarLayout.Controls.Add(sidebarActions, 0, 3);
            sidebarLayout.Controls.Add(sidebarHelp, 0, 4);
            panel.Controls.Add(sidebarLayout);
            return panel;
        }

        private Control BuildContent()
        {
            var host = new Panel { Dock = DockStyle.Fill, BackColor = UiColors.Canvas, Padding = new Padding(20, 18, 22, 20), Margin = Padding.Empty };
            contentLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = UiColors.Canvas, Margin = Padding.Empty, Padding = Padding.Empty };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 126F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 270F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            contentLayout.Controls.Add(BuildHeroCard(), 0, 0);
            contentLayout.Controls.Add(BuildActionCard(), 0, 1);
            contentLayout.Controls.Add(BuildRecordsCard(), 0, 2);
            host.Controls.Add(contentLayout);
            return host;
        }

        private Control BuildHeroCard()
        {
            var card = new ModernCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 14), Padding = new Padding(18, 12, 18, 12) };
            var badgeHost = new Panel { Dock = DockStyle.Left, Width = 84, BackColor = UiColors.Surface };
            gameBadge.Location = new Point(6, 16);
            badgeHost.Controls.Add(gameBadge);
            heroPoolHost = new Panel { Dock = DockStyle.Right, Width = 258, BackColor = UiColors.Surface, Padding = new Padding(18, 12, 2, 10) };
            var poolLabel = new Label { Text = "查看卡池", Dock = DockStyle.Top, Height = 25, ForeColor = UiColors.TextMuted, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold) };
            poolBox.Dock = DockStyle.Top;
            poolBox.SelectedIndexChanged += delegate { if (!updatingPool) RefreshRecords(); };
            heroPoolHost.Controls.Add(poolBox);
            heroPoolHost.Controls.Add(poolLabel);
            var textHost = new Panel { Dock = DockStyle.Fill, BackColor = UiColors.Surface, Padding = new Padding(0, 7, 10, 4) };
            heroTitle.Dock = DockStyle.Top;
            heroTitle.Height = 32;
            heroTitle.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold);
            heroTitle.ForeColor = UiColors.Text;
            heroSubtitle.Dock = DockStyle.Top;
            heroSubtitle.Height = 24;
            heroSubtitle.ForeColor = UiColors.TextMuted;
            instruction.Dock = DockStyle.Fill;
            instruction.ForeColor = UiColors.TextMuted;
            instruction.AutoEllipsis = true;
            textHost.Controls.Add(instruction);
            textHost.Controls.Add(heroSubtitle);
            textHost.Controls.Add(heroTitle);
            card.Controls.Add(textHost);
            card.Controls.Add(heroPoolHost);
            card.Controls.Add(badgeHost);
            return card;
        }

        private Control BuildActionCard()
        {
            var card = new ModernCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 14), Padding = new Padding(20, 16, 20, 16) };
            actionLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = UiColors.Surface, Margin = Padding.Empty, Padding = Padding.Empty };
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var header = new Panel { Dock = DockStyle.Fill, BackColor = UiColors.Surface };
            header.Controls.Add(new Label { Text = "获取与同步", Dock = DockStyle.Left, Width = 180, Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold), ForeColor = UiColors.Text });
            var diagnostics = MakeButton("复制诊断信息", 126, ModernButtonStyle.Text, delegate { CopyDiagnostics(); });
            diagnostics.Dock = DockStyle.Right;
            diagnostics.Height = 30;
            header.Controls.Add(diagnostics);
            actionLayout.Controls.Add(header, 0, 0);
            var folderRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty, Padding = new Padding(0, 3, 0, 3), BackColor = UiColors.Surface };
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122F));
            folderRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var folderLabel = new Label
            {
                Text = "游戏目录",
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = new Padding(0, 0, 0, 4),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiColors.TextMuted,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            };
            folderRow.Controls.Add(folderLabel, 0, 0);
            StyleTextBox(folderBox, false);
            folderBox.Text = "自动检查常见安装目录";
            var folderField = WrapTextBox(folderBox, false);
            folderField.Dock = DockStyle.Fill;
            folderField.Margin = new Padding(0, 0, 10, 0);
            folderRow.Controls.Add(folderField, 1, 0);
            var browse = MakeButton("手动选择…", 112, ModernButtonStyle.Outline, delegate { ChooseFolder(); });
            browse.Dock = DockStyle.Fill;
            browse.Margin = Padding.Empty;
            folderRow.Controls.Add(browse, 2, 0);
            actionLayout.Controls.Add(folderRow, 0, 1);
            var actionRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = UiColors.Surface, Margin = Padding.Empty, Padding = new Padding(0, 5, 0, 5) };
            findButton = MakeButton("自动获取链接", 142, ModernButtonStyle.Primary, delegate { FindUrl(); });
            copyButton = MakeButton("复制链接", 104, ModernButtonStyle.Tonal, delegate { CopyUrl(); });
            copyButton.Enabled = false;
            syncButton = MakeButton("同步全部记录", 126, ModernButtonStyle.Outline, delegate { Sync(); });
            actionRow.Controls.AddRange(new Control[] { findButton, copyButton, syncButton });
            actionLayout.Controls.Add(actionRow, 0, 2);
            var linkHost = new Panel { Dock = DockStyle.Fill, BackColor = UiColors.Surface, Padding = new Padding(0, 1, 0, 4) };
            linkHost.Controls.Add(new Label { Text = "获取到的链接", Dock = DockStyle.Top, Height = 20, ForeColor = UiColors.TextMuted });
            StyleTextBox(urlBox, true);
            var urlField = WrapTextBox(urlBox, true);
            urlField.Dock = DockStyle.Fill;
            linkHost.Controls.Add(urlField);
            urlField.BringToFront();
            actionLayout.Controls.Add(linkHost, 0, 3);
            var statusCard = new ModernCard { Dock = DockStyle.Fill, Radius = 10, BackColor = UiColors.SurfaceMuted, Margin = new Padding(0, 2, 0, 0), Padding = new Padding(10, 0, 10, 0) };
            statusDot.Text = "●";
            statusDot.Dock = DockStyle.Left;
            statusDot.Width = 22;
            statusDot.TextAlign = ContentAlignment.MiddleCenter;
            statusDot.ForeColor = UiColors.Primary;
            status.Dock = DockStyle.Fill;
            status.TextAlign = ContentAlignment.MiddleLeft;
            status.AutoEllipsis = true;
            status.ForeColor = UiColors.TextMuted;
            statusCard.Controls.Add(status);
            statusCard.Controls.Add(statusDot);
            actionLayout.Controls.Add(statusCard, 0, 4);
            card.Controls.Add(actionLayout);
            return card;
        }

        private Control BuildRecordsCard()
        {
            var card = new ModernCard { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(18, 14, 18, 16) };
            recordsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = UiColors.Surface, Margin = Padding.Empty, Padding = Padding.Empty };
            recordsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            recordsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
            recordsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
            recordsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var header = new Panel { Dock = DockStyle.Fill, BackColor = UiColors.Surface };
            header.Controls.Add(new Label { Text = "本地抽卡记录", Dock = DockStyle.Top, Height = 28, Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold), ForeColor = UiColors.Text });
            summary.Dock = DockStyle.Fill;
            summary.ForeColor = UiColors.TextMuted;
            summary.BackColor = UiColors.Surface;
            summary.BorderStyle = BorderStyle.None;
            summary.ReadOnly = true;
            summary.Multiline = true;
            summary.WordWrap = true;
            summary.ScrollBars = ScrollBars.Vertical;
            summary.TabStop = false;
            summary.Text = "同步后将在此显示基础统计。";
            header.Controls.Add(summary);
            summary.BringToFront();
            recordsLayout.Controls.Add(header, 0, 0);
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, AutoScroll = true, BackColor = UiColors.Surface, Margin = Padding.Empty, Padding = new Padding(0, 5, 0, 5) };
            toolbar.Controls.AddRange(new Control[]
            {
                MakeButton("刷新", 72, ModernButtonStyle.Text, delegate { RefreshRecords(); }),
                MakeButton("数据分析", 90, ModernButtonStyle.Tonal, delegate { ShowAnalysis(); }),
                MakeButton("导出 CSV", 90, ModernButtonStyle.Outline, delegate { Export("csv"); }),
                MakeButton("导出 Excel", 98, ModernButtonStyle.Outline, delegate { Export("excel"); }),
                MakeButton("导出 JSON", 92, ModernButtonStyle.Outline, delegate { Export("json"); }),
                MakeButton("导出 UIGF", 92, ModernButtonStyle.Outline, delegate { Export("uigf"); }),
                MakeButton("立即备份", 90, ModernButtonStyle.Text, delegate { Backup(); }),
                MakeButton("恢复备份", 90, ModernButtonStyle.Text, delegate { Restore(); })
            });
            recordsLayout.Controls.Add(toolbar, 0, 1);
            ConfigureGrid();
            recordsLayout.Controls.Add(grid, 0, 2);
            card.Controls.Add(recordsLayout);
            return card;
        }

        private ModernButton MakeButton(string text, int width, ModernButtonStyle style, EventHandler click)
        {
            var button = new ModernButton { Text = text, Width = width, Height = 38, ButtonStyle = style, Margin = new Padding(0, 0, 10, 0) };
            button.Click += click;
            return button;
        }

        private static void StyleTextBox(TextBox textBox, bool multiline)
        {
            textBox.ReadOnly = true;
            textBox.Multiline = multiline;
            textBox.BorderStyle = BorderStyle.None;
            textBox.BackColor = Color.FromArgb(250, 251, 255);
            textBox.ForeColor = UiColors.Text;
            textBox.Font = new Font("Microsoft YaHei UI", 9F);
            if (multiline) textBox.ScrollBars = ScrollBars.Vertical;
        }

        private static ModernCard WrapTextBox(TextBox textBox, bool multiline)
        {
            var field = new ModernCard
            {
                Radius = 10,
                BackColor = Color.FromArgb(250, 251, 255),
                Padding = multiline ? new Padding(11, 6, 7, 5) : new Padding(11, 8, 9, 6)
            };
            textBox.Dock = DockStyle.Fill;
            field.Controls.Add(textBox);
            return field;
        }

        private void ConfigureGrid()
        {
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.RowHeadersVisible = false;
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = UiColors.Surface;
            grid.GridColor = Color.FromArgb(232, 234, 237);
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeight = 38;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 253);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = UiColors.TextMuted;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 250, 253);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.DefaultCellStyle.BackColor = UiColors.Surface;
            grid.DefaultCellStyle.ForeColor = UiColors.Text;
            grid.DefaultCellStyle.SelectionBackColor = UiColors.PrimaryContainer;
            grid.DefaultCellStyle.SelectionForeColor = UiColors.Text;
            grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            grid.RowTemplate.Height = 36;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 253, 255);
        }

        private void ApplyResponsiveLayout()
        {
            if (workspace == null) return;
            var compact = ClientSize.Width < 1040;
            workspace.ColumnStyles[0].Width = compact ? 132F : 218F;
            sidebarHelp.Visible = !compact;
            sidebarLayout.RowStyles[4].Height = compact ? 0F : 118F;
            heroPoolHost.Width = compact ? 208 : 258;
            topSubtitle.Visible = ClientSize.Width >= 980;
            privacyChip.Width = compact ? 218 : 278;
            PositionTopBarControls();
            var shortHeight = ClientSize.Height < 760;
            contentLayout.RowStyles[0].Height = shortHeight ? 120F : 126F;
            contentLayout.RowStyles[1].Height = shortHeight ? 254F : 270F;
            actionLayout.RowStyles[0].Height = shortHeight ? 30F : 32F;
            actionLayout.RowStyles[1].Height = shortHeight ? 40F : 44F;
            actionLayout.RowStyles[2].Height = shortHeight ? 44F : 48F;
            actionLayout.RowStyles[3].Height = shortHeight ? 58F : 68F;
            recordsLayout.RowStyles[0].Height = shortHeight ? 58F : 78F;
            foreach (var button in gameButtons)
            {
                button.Compact = compact;
                button.Width = compact ? 108 : 190;
            }
            if (aboutButton != null) aboutButton.Width = compact ? 108 : 190;
            if (updateButton != null) updateButton.Width = compact ? 108 : 190;
        }

        internal void HandleSecondaryLaunch()
        {
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Show();
            ShowInTaskbar = true;
            BringToFront();
            Activate();
            SetForegroundWindow(Handle);
            var ignored = CheckForUpdatesAsync(false, false);
        }

        private async Task CheckForUpdatesAsync(bool coldStart, bool userInitiated)
        {
            if (updateCheckInProgress)
            {
                if (userInitiated) MessageBox.Show("正在检查更新，请稍候。", "软件更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            updateCheckInProgress = true;
            try
            {
                var result = await updateService.CheckAsync();
                if (!result.IsUpdateAvailable)
                {
                    availableUpdate = null;
                    updateButton.Visible = false;
                    if (userInitiated) MessageBox.Show(result.Message, "软件更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                availableUpdate = result.Release;
                updateButton.Text = "更新到 v" + availableUpdate.VersionText;
                updateButton.Visible = true;
                if (coldStart && !coldStartNotificationShown)
                {
                    coldStartNotificationShown = true;
                    var answer = MessageBox.Show("发现新版本 v" + availableUpdate.VersionText + "。\r\n\r\n是否现在查看并下载更新？", "发现软件更新", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (answer == DialogResult.Yes) ShowUpdateDialog();
                }
                else if (userInitiated)
                {
                    ShowUpdateDialog();
                }
            }
            catch (Exception exception)
            {
                if (userInitiated) MessageBox.Show("无法检查更新：" + exception.Message, "检查更新失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                updateCheckInProgress = false;
            }
        }

        private void ShowAbout()
        {
            using (var about = new AboutForm(store.DataDirectory, delegate { BeginInvoke(new Action(async delegate { await CheckForUpdatesAsync(false, true); })); }))
                about.ShowDialog(this);
        }

        private void ShowUpdateDialog()
        {
            if (availableUpdate == null) return;
            using (var update = new UpdateForm(availableUpdate)) update.ShowDialog(this);
        }

        private GameDefinition SelectedGame { get { return Games[selectedGameIndex]; } }
        private GachaPoolDefinition SelectedPool { get { return poolBox.SelectedItem as GachaPoolDefinition; } }

        private void SelectGame(int index)
        {
            if (index < 0 || index >= Games.Length) return;
            selectedGameIndex = index;
            for (var buttonIndex = 0; buttonIndex < gameButtons.Count; buttonIndex++) gameButtons[buttonIndex].Selected = buttonIndex == index;
            gameBadge.BadgeText = GameShortNames[index];
            gameBadge.AccentColor = GameColors[index];
            gameBadge.Invalidate();
            heroTitle.Text = Games[index].Name;
            heroSubtitle.Text = Games[index].RecordName + " · 链接获取与本地记录管理";
            selectedFolder = string.Empty;
            folderBox.Text = "自动检查常见安装目录";
            foundUrl = string.Empty;
            urlBox.Clear();
            copyButton.Enabled = false;
            lastScanSummary = "尚未执行扫描。请先选择游戏并点击“自动获取链接”。";
            UpdateInstruction();
            UpdatePoolFilter();
            SetStatus("准备就绪：请先在游戏里打开一次抽卡记录。", UiColors.Primary);
        }

        private void UpdateInstruction()
        {
            instruction.Text = "打开《" + SelectedGame.Name + "》的“" + SelectedGame.RecordName + "”并等待加载完成，然后点击下方“自动获取链接”。";
        }

        private void SetStatus(string text, Color color)
        {
            status.Text = text;
            statusDot.ForeColor = color;
        }

        private void ChooseFolder()
        {
            using (var dialog = new FolderBrowserDialog { Description = "选择游戏实际安装目录（含游戏 EXE 或 *_Data 文件夹）" })
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                selectedFolder = dialog.SelectedPath;
                folderBox.Text = selectedFolder;
                SetStatus("已选择目录。点击“自动获取链接”开始读取。", UiColors.Primary);
            }
        }

        private void FindUrl()
        {
            SetStatus("正在扫描游戏日志与缓存…", UiColors.Primary);
            var result = discovery.Find(SelectedGame, selectedFolder);
            lastScanSummary = "抽卡链接获取工具 - 诊断信息\r\n游戏：" + SelectedGame.Name + "\r\n扫描模式：" + (string.IsNullOrWhiteSpace(selectedFolder) ? "自动检测" : "手动目录") + "\r\n已检查目录数：" + result.RootCount + "\r\n候选日志/缓存文件数：" + result.FileCount + "\r\n匹配结果：" + (string.IsNullOrWhiteSpace(result.Url) ? "未找到链接" : "已找到链接") + "\r\n说明：诊断信息不包含抽卡链接、账号或文件路径。";
            if (string.IsNullOrWhiteSpace(result.Url))
            {
                foundUrl = string.Empty;
                copyButton.Enabled = false;
                urlBox.Clear();
                SetStatus("未找到链接。请打开游戏内记录页；仍失败时可手动选择游戏目录后重试。", UiColors.Warning);
                return;
            }
            foundUrl = result.Url;
            urlBox.Text = foundUrl;
            copyButton.Enabled = true;
            SetStatus("已从《" + SelectedGame.Name + "》本地缓存或日志获取链接；链接不会写入数据文件。", UiColors.Success);
        }

        private void CopyUrl()
        {
            if (string.IsNullOrWhiteSpace(foundUrl)) return;
            Clipboard.SetText(foundUrl);
            SetStatus("链接已复制。请只粘贴到你信任的统计工具。", UiColors.Success);
        }

        private void CopyDiagnostics()
        {
            Clipboard.SetText(lastScanSummary);
            SetStatus("已复制不含链接、账号或文件路径的诊断信息。", UiColors.Success);
        }

        private void Sync()
        {
            if (string.IsNullOrWhiteSpace(foundUrl))
            {
                MessageBox.Show("请先获取链接。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("将使用当前链接中的临时凭证向该游戏官方记录接口请求数据。\r\n不会上传本地日志，链接不会保存到本地。是否继续？", "确认同步", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            syncButton.Enabled = false;
            findButton.Enabled = false;
            SetStatus("正在请求官方记录接口，请稍候…", UiColors.Primary);
            var game = SelectedGame;
            Task.Factory.StartNew(() => api.Download(game.Kind, foundUrl)).ContinueWith(task => BeginInvoke(new Action(delegate
            {
                syncButton.Enabled = true;
                findButton.Enabled = true;
                if (task.IsFaulted)
                {
                    var message = task.Exception.GetBaseException().Message;
                    SetStatus("同步失败：" + message, UiColors.Error);
                    MessageBox.Show(message, "同步失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var added = store.Merge(task.Result.Records);
                SetStatus(task.Result.Message + " 新增并保存 " + added + " 条；已有记录已自动去重。", UiColors.Success);
                UpdatePoolFilter();
            })));
        }

        private List<GachaRecord> AllGameRecords()
        {
            return store.Load().Records.Where(item => item.Game == SelectedGame.Kind.ToString()).OrderByDescending(item => item.Time).ToList();
        }

        private List<GachaRecord> CurrentRecords()
        {
            var records = AllGameRecords();
            var pool = SelectedPool;
            return pool == null || string.IsNullOrWhiteSpace(pool.Code) ? records : records.Where(item => GachaPoolCatalog.CanonicalCode(SelectedGame.Kind, item.GachaType) == pool.Code).ToList();
        }

        private void UpdatePoolFilter()
        {
            var selectedCode = SelectedPool == null ? string.Empty : GachaPoolCatalog.CanonicalCode(SelectedGame.Kind, SelectedPool.Code);
            updatingPool = true;
            poolBox.Items.Clear();
            poolBox.Items.Add(new GachaPoolDefinition(string.Empty, "全部卡池"));
            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pool in GachaPoolCatalog.ForGame(SelectedGame.Kind))
            {
                poolBox.Items.Add(pool);
                known.Add(pool.Code);
            }
            foreach (var code in AllGameRecords().Select(item => GachaPoolCatalog.CanonicalCode(SelectedGame.Kind, item.GachaType)).Where(code => !string.IsNullOrWhiteSpace(code) && !known.Contains(code)).Distinct().OrderBy(code => code))
                poolBox.Items.Add(new GachaPoolDefinition(code, GachaPoolCatalog.NameFor(SelectedGame.Kind, code)));
            var selectedIndex = 0;
            for (var index = 0; index < poolBox.Items.Count; index++)
            {
                var pool = poolBox.Items[index] as GachaPoolDefinition;
                if (pool != null && pool.Code == selectedCode) { selectedIndex = index; break; }
            }
            poolBox.SelectedIndex = selectedIndex;
            updatingPool = false;
            RefreshRecords();
        }

        private void RefreshRecords()
        {
            var records = CurrentRecords();
            grid.DataSource = records.Select(item => new { 卡池 = GachaPoolCatalog.NameFor(SelectedGame.Kind, item.GachaType), 名称 = item.Name, 类型 = item.ItemType, 星级 = item.RankType, 时间 = item.Time, UID = item.Uid }).ToList();
            summary.Text = analytics.BuildSummary(records);
        }

        private void ShowAnalysis()
        {
            MessageBox.Show(analytics.BuildSummary(CurrentRecords()), "《" + SelectedGame.Name + "》" + (SelectedPool == null ? string.Empty : SelectedPool.Name) + "数据分析", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Export(string format)
        {
            var records = CurrentRecords();
            if (records.Count == 0)
            {
                MessageBox.Show("当前筛选条件下没有可导出的本地记录。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var poolName = SelectedPool == null || string.IsNullOrWhiteSpace(SelectedPool.Code) ? "全部卡池" : SelectedPool.Name;
            var filters = format == "csv" ? "CSV 文件|*.csv" : format == "excel" ? "Excel XML 文件|*.xml" : "JSON 文件|*.json";
            using (var dialog = new SaveFileDialog { Filter = filters, FileName = SelectedGame.Name + "-" + poolName + "-抽卡记录-" + DateTime.Now.ToString("yyyyMMdd") })
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                try
                {
                    if (format == "csv") exporter.ExportCsv(records, dialog.FileName);
                    else if (format == "excel") exporter.ExportExcelXml(records, dialog.FileName);
                    else if (format == "uigf") exporter.ExportUigf(records, dialog.FileName);
                    else exporter.ExportJson(records, dialog.FileName);
                    SetStatus("已导出 " + records.Count + " 条记录：" + dialog.FileName, UiColors.Success);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void Backup()
        {
            var path = store.CreateBackup();
            SetStatus("已创建本地备份：" + path, UiColors.Success);
        }

        private void Restore()
        {
            using (var dialog = new OpenFileDialog { Filter = "JSON 备份文件|*.json", InitialDirectory = store.BackupDirectory })
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                try
                {
                    store.Restore(dialog.FileName);
                    SetStatus("已恢复备份。", UiColors.Success);
                    UpdatePoolFilter();
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "恢复失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void PositionTopBarControls()
        {
            if (privacyChip == null || topBar == null || windowControlHost == null) return;
            privacyChip.Left = Math.Max(360, topBar.ClientSize.Width - windowControlHost.Width - privacyChip.Width - 12);
        }

        private void BeginWindowDrag(object sender, MouseEventArgs eventArgs)
        {
            if (eventArgs.Button != MouseButtons.Left) return;
            if (WindowState == FormWindowState.Maximized)
            {
                var cursor = Cursor.Position;
                WindowState = FormWindowState.Normal;
                Location = new Point(cursor.X - Width / 2, Math.Max(0, cursor.Y - 24));
            }
            ReleaseCapture();
            SendMessage(Handle, 0x00A1, new IntPtr(2), IntPtr.Zero);
        }

        private void ToggleMaximize()
        {
            if (WindowState == FormWindowState.Maximized)
                WindowState = FormWindowState.Normal;
            else
            {
                MaximizedBounds = Screen.FromControl(this).WorkingArea;
                WindowState = FormWindowState.Maximized;
            }
            UpdateMaximizeButton();
        }

        private void UpdateMaximizeButton()
        {
            if (maximizeButton != null) maximizeButton.RestoreGlyph = WindowState == FormWindowState.Maximized;
        }

        protected override void OnHandleCreated(EventArgs eventArgs)
        {
            base.OnHandleCreated(eventArgs);
            try
            {
                const int cornerPreferenceAttribute = 33;
                var rounded = 2;
                DwmSetWindowAttribute(Handle, cornerPreferenceAttribute, ref rounded, sizeof(int));
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
            UpdateWindowShape();
        }

        private void UpdateWindowShape()
        {
            if (!IsHandleCreated || Width <= 0 || Height <= 0) return;
            var oldRegion = Region;
            if (WindowState == FormWindowState.Maximized)
                Region = null;
            else
            {
                using (var path = ModernCard.RoundedPath(new Rectangle(0, 0, Width, Height), 12))
                    Region = new Region(path);
            }
            if (oldRegion != null) oldRegion.Dispose();
        }

        protected override void WndProc(ref Message message)
        {
            const int wmNcHitTest = 0x0084;
            if (message.Msg == wmNcHitTest && WindowState == FormWindowState.Normal)
            {
                base.WndProc(ref message);
                var value = message.LParam.ToInt64();
                var screenPoint = new Point(unchecked((short)(value & 0xFFFF)), unchecked((short)((value >> 16) & 0xFFFF)));
                var clientPoint = PointToClient(screenPoint);
                const int grip = 7;
                var left = clientPoint.X <= grip;
                var right = clientPoint.X >= ClientSize.Width - grip;
                var top = clientPoint.Y <= grip;
                var bottom = clientPoint.Y >= ClientSize.Height - grip;
                if (left && top) message.Result = new IntPtr(13);
                else if (right && top) message.Result = new IntPtr(14);
                else if (left && bottom) message.Result = new IntPtr(16);
                else if (right && bottom) message.Result = new IntPtr(17);
                else if (left) message.Result = new IntPtr(10);
                else if (right) message.Result = new IntPtr(11);
                else if (top) message.Result = new IntPtr(12);
                else if (bottom) message.Result = new IntPtr(15);
                return;
            }
            base.WndProc(ref message);
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr wordParameter, IntPtr longParameter);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr windowHandle);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute, ref int attributeValue, int attributeSize);
    }
}
