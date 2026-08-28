using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
    internal sealed class UpdateForm : Form
    {
        private readonly UpdateInfo update;
        private readonly SettingsStore settingsStore = new SettingsStore();
        private readonly NodeCatalogService nodeCatalog = new NodeCatalogService();
        private readonly UpdateDownloader downloader = new UpdateDownloader();
        private readonly ModernComboBox routeBox = new ModernComboBox();
        private readonly ModernComboBox nodeBox = new ModernComboBox();
        private readonly TextBox customUrlBox = new TextBox();
        private readonly Label nodeStatus = new Label();
        private readonly Label downloadStatus = new Label();
        private readonly ModernProgressBar progressBar = new ModernProgressBar();
        private readonly TableLayoutPanel optionLayout = new TableLayoutPanel();
        private readonly ModernButton downloadButton = new ModernButton();
        private readonly ModernButton refreshNodesButton = new ModernButton();
        private AppSettings settings;
        private List<string> liveNodes = new List<string>();
        private bool rebuildingRoutes;
        private bool downloading;

        public UpdateForm(UpdateInfo updateInfo)
        {
            if (updateInfo == null) throw new ArgumentNullException("updateInfo");
            update = updateInfo;
            settings = settingsStore.Load();
            Text = "更新到 v" + update.VersionText;
            ClientSize = new Size(760, 650);
            MinimumSize = new Size(680, 610);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = UiColors.Canvas;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BuildInterface();
            Resize += delegate { UpdateWindowShape(); };
            Shown += async delegate { await RefreshNodesAsync(); };
            FormClosing += delegate(object sender, FormClosingEventArgs eventArgs)
            {
                if (!downloading) return;
                eventArgs.Cancel = true;
                MessageBox.Show("安装程序仍在下载和校验，请完成后再关闭窗口。", "正在更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
        }

        private void BuildInterface()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = UiColors.Canvas, Margin = Padding.Empty, Padding = Padding.Empty };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            root.Controls.Add(BuildTitleBar(), 0, 0);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22, 18, 22, 8), BackColor = UiColors.Canvas };
            var bodyLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = UiColors.Canvas, Margin = Padding.Empty, Padding = Padding.Empty };
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 154F));
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            bodyLayout.Controls.Add(BuildReleaseCard(), 0, 0);
            bodyLayout.Controls.Add(BuildOptionsCard(), 0, 1);
            bodyLayout.Controls.Add(BuildProgressCard(), 0, 2);
            body.Controls.Add(bodyLayout);
            root.Controls.Add(body, 0, 1);
            root.Controls.Add(BuildFooter(), 0, 2);
            Controls.Add(root);
        }

        private Control BuildTitleBar()
        {
            var titleBar = new Panel { Dock = DockStyle.Fill, BackColor = UiColors.Surface, Margin = Padding.Empty };
            var title = new Label { Text = "软件更新", Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold), ForeColor = UiColors.Text, Location = new Point(22, 17), AutoSize = true };
            var close = new WindowControlButton(WindowButtonKind.Close) { Dock = DockStyle.Right, Width = 48 };
            close.Click += delegate { Close(); };
            titleBar.Controls.AddRange(new Control[] { title, close, new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = UiColors.Border } });
            titleBar.MouseDown += BeginWindowDrag;
            title.MouseDown += BeginWindowDrag;
            return titleBar;
        }

        private Control BuildReleaseCard()
        {
            var card = new ModernCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 14), Padding = new Padding(20, 14, 20, 14), BackColor = UiColors.Surface };
            var title = new Label { Text = "发现新版本 v" + update.VersionText, Dock = DockStyle.Top, Height = 34, Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold), ForeColor = UiColors.Text };
            var releaseName = new Label { Text = string.IsNullOrWhiteSpace(update.ReleaseName) ? update.TagName : update.ReleaseName, Dock = DockStyle.Top, Height = 26, ForeColor = UiColors.Primary, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold) };
            var notes = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, TabStop = false, BorderStyle = BorderStyle.None, BackColor = UiColors.Surface, ForeColor = UiColors.TextMuted, ScrollBars = ScrollBars.Vertical, Text = string.IsNullOrWhiteSpace(update.ReleaseNotes) ? "请查看 GitHub Release 获取版本说明。" : update.ReleaseNotes };
            card.Controls.Add(notes);
            card.Controls.Add(releaseName);
            card.Controls.Add(title);
            return card;
        }

        private Control BuildOptionsCard()
        {
            var card = new ModernCard { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(20, 14, 20, 12), BackColor = UiColors.Surface };
            optionLayout.Dock = DockStyle.Fill;
            optionLayout.ColumnCount = 1;
            optionLayout.RowCount = 6;
            optionLayout.BackColor = UiColors.Surface;
            optionLayout.Margin = Padding.Empty;
            optionLayout.Padding = Padding.Empty;
            optionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            optionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            optionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            optionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            optionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            optionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            optionLayout.Controls.Add(new Label { Text = "下载方式", Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold), ForeColor = UiColors.Text }, 0, 0);

            routeBox.Dock = DockStyle.Fill;
            routeBox.AccessibleName = "更新下载方式";
            routeBox.SelectedIndexChanged += delegate { UpdateOptionRows(); };
            optionLayout.Controls.Add(routeBox, 0, 1);

            var nodeHeader = new Panel { Dock = DockStyle.Fill, BackColor = UiColors.Surface };
            nodeStatus.Dock = DockStyle.Fill;
            nodeStatus.ForeColor = UiColors.TextMuted;
            nodeStatus.TextAlign = ContentAlignment.MiddleLeft;
            nodeStatus.AutoEllipsis = true;
            refreshNodesButton.Text = "刷新实时节点";
            refreshNodesButton.ButtonStyle = ModernButtonStyle.Text;
            refreshNodesButton.Dock = DockStyle.Right;
            refreshNodesButton.Width = 126;
            refreshNodesButton.Click += async delegate { await RefreshNodesAsync(); };
            nodeHeader.Controls.Add(nodeStatus);
            nodeHeader.Controls.Add(refreshNodesButton);
            optionLayout.Controls.Add(nodeHeader, 0, 2);

            nodeBox.Dock = DockStyle.Fill;
            nodeBox.AccessibleName = "手动加速节点";
            optionLayout.Controls.Add(nodeBox, 0, 3);

            var customHost = new ModernCard { Dock = DockStyle.Fill, Radius = 10, BackColor = Color.FromArgb(250, 251, 255), Padding = new Padding(11, 8, 9, 6), Margin = new Padding(0, 4, 0, 4) };
            customUrlBox.Dock = DockStyle.Fill;
            customUrlBox.BorderStyle = BorderStyle.None;
            customUrlBox.BackColor = Color.FromArgb(250, 251, 255);
            customUrlBox.ForeColor = UiColors.Text;
            customUrlBox.Text = settings.CustomAccelerationUrl ?? string.Empty;
            customHost.Controls.Add(customUrlBox);
            optionLayout.Controls.Add(customHost, 0, 4);

            optionLayout.Controls.Add(new Label
            {
                Text = "节点列表每次从 github.akams.cn 实时获取。",
                Dock = DockStyle.Fill,
                ForeColor = UiColors.TextMuted,
                AutoEllipsis = true
            }, 0, 5);
            card.Controls.Add(optionLayout);
            RebuildRoutes(false);
            return card;
        }

        private Control BuildProgressCard()
        {
            var card = new ModernCard { Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 0), Padding = new Padding(14, 9, 14, 9), Radius = 12, BackColor = UiColors.Surface };
            downloadStatus.Text = "准备下载。";
            downloadStatus.Dock = DockStyle.Top;
            downloadStatus.Height = 28;
            downloadStatus.ForeColor = UiColors.TextMuted;
            progressBar.Dock = DockStyle.Top;
            progressBar.Value = 0;
            card.Controls.Add(progressBar);
            card.Controls.Add(downloadStatus);
            return card;
        }

        private Control BuildFooter()
        {
            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = UiColors.Canvas, Padding = new Padding(22, 12, 22, 16) };
            var close = new ModernButton { Text = "稍后", Width = 96, Height = 40, ButtonStyle = ModernButtonStyle.Outline, Margin = new Padding(10, 0, 0, 0) };
            close.Click += delegate { Close(); };
            downloadButton.Text = update.CanDownload ? "下载并安装" : "缺少安装包";
            downloadButton.Width = 138;
            downloadButton.Height = 40;
            downloadButton.ButtonStyle = ModernButtonStyle.Primary;
            downloadButton.Enabled = update.CanDownload;
            downloadButton.Margin = new Padding(10, 0, 0, 0);
            downloadButton.Click += async delegate { await DownloadAndInstallAsync(); };
            var releasePage = new ModernButton { Text = "查看发布页", Width = 116, Height = 40, ButtonStyle = ModernButtonStyle.Text, Margin = new Padding(10, 0, 0, 0) };
            releasePage.Click += delegate
            {
                try { Process.Start(new ProcessStartInfo(update.ReleasePageUrl) { UseShellExecute = true }); }
                catch (Exception exception) { MessageBox.Show(exception.Message, "无法打开发布页", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            footer.Controls.AddRange(new Control[] { close, downloadButton, releasePage });
            return footer;
        }

        private async Task RefreshNodesAsync()
        {
            refreshNodesButton.Enabled = false;
            nodeStatus.Text = "正在实时获取加速节点…";
            try
            {
                var nodes = await nodeCatalog.FetchNodesAsync();
                if (IsDisposed) return;
                liveNodes = nodes;
                PopulateNodeBox();
                RebuildRoutes(true);
                nodeStatus.Text = "已实时获取 " + liveNodes.Count + " 个节点；自动模式会测试前 24 个。";
            }
            catch (Exception exception)
            {
                if (IsDisposed) return;
                liveNodes.Clear();
                PopulateNodeBox();
                RebuildRoutes(false);
                nodeStatus.Text = "实时节点不可用，仅保留 GitHub 直连和自定义地址：" + exception.Message;
            }
            finally
            {
                if (!IsDisposed) refreshNodesButton.Enabled = true;
            }
        }

        private void PopulateNodeBox()
        {
            nodeBox.Items.Clear();
            var selectedIndex = -1;
            for (var index = 0; index < liveNodes.Count; index++)
            {
                nodeBox.Items.Add(liveNodes[index]);
                if (string.Equals(liveNodes[index], settings.SelectedNode, StringComparison.OrdinalIgnoreCase)) selectedIndex = index;
            }
            if (nodeBox.Items.Count > 0) nodeBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }

        private void RebuildRoutes(bool includeLiveNodes)
        {
            rebuildingRoutes = true;
            try
            {
                DownloadRouteKind preferred;
                if (!Enum.TryParse(settings.DownloadRoute, true, out preferred)) preferred = DownloadRouteKind.Direct;
                var choices = new List<RouteOption> { new RouteOption(DownloadRouteKind.Direct, "GitHub 直连（默认）") };
                if (includeLiveNodes && liveNodes.Count >= 2)
                {
                    choices.Add(new RouteOption(DownloadRouteKind.Auto, "自动选择实时加速节点"));
                    choices.Add(new RouteOption(DownloadRouteKind.Manual, "手动选择实时加速节点"));
                }
                choices.Add(new RouteOption(DownloadRouteKind.Custom, "自定义加速地址"));

                routeBox.Items.Clear();
                var selected = 0;
                for (var index = 0; index < choices.Count; index++)
                {
                    routeBox.Items.Add(choices[index]);
                    if (choices[index].Kind == preferred) selected = index;
                }
                routeBox.SelectedIndex = selected;
            }
            finally
            {
                rebuildingRoutes = false;
                UpdateOptionRows();
            }
        }

        private void UpdateOptionRows()
        {
            if (rebuildingRoutes || optionLayout.RowStyles.Count < 5) return;
            var option = routeBox.SelectedItem as RouteOption;
            var manual = option != null && option.Kind == DownloadRouteKind.Manual;
            var custom = option != null && option.Kind == DownloadRouteKind.Custom;
            optionLayout.RowStyles[3].Height = manual ? 48F : 0F;
            optionLayout.RowStyles[4].Height = custom ? 52F : 0F;
            nodeBox.Visible = manual;
            customUrlBox.Parent.Visible = custom;
        }

        private async Task DownloadAndInstallAsync()
        {
            var option = routeBox.SelectedItem as RouteOption;
            if (option == null) return;
            downloadButton.Enabled = false;
            refreshNodesButton.Enabled = false;
            downloading = true;
            progressBar.Value = 0;
            try
            {
                var route = new DownloadRoute { Kind = option.Kind, Value = string.Empty };
                if (option.Kind == DownloadRouteKind.Auto)
                {
                    route.Value = await nodeCatalog.SelectFastestAsync(liveNodes, new Progress<string>(text => downloadStatus.Text = text));
                    settings.SelectedNode = route.Value;
                }
                else if (option.Kind == DownloadRouteKind.Manual)
                {
                    route.Value = Convert.ToString(nodeBox.SelectedItem);
                    if (string.IsNullOrWhiteSpace(route.Value)) throw new InvalidOperationException("请选择一个实时节点。");
                    settings.SelectedNode = route.Value;
                }
                else if (option.Kind == DownloadRouteKind.Custom)
                {
                    route.Value = customUrlBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(route.Value)) throw new InvalidOperationException("请输入自定义加速地址。");
                    NodeCatalogService.BuildAcceleratedUrl(route.Value, update.InstallerAssetUrl);
                    settings.CustomAccelerationUrl = route.Value;
                }

                settings.DownloadRoute = option.Kind.ToString();
                settingsStore.Save(settings);
                downloadStatus.Text = "正在下载并验证安装程序…";
                var installerPath = await downloader.DownloadAsync(update, route, new Progress<int>(value =>
                {
                    progressBar.Value = value;
                    downloadStatus.Text = "正在下载安装程序… " + value + "%";
                }));
                downloadStatus.Text = "SHA-256 校验通过，安装程序已就绪。";
                progressBar.Value = 100;

                var start = MessageBox.Show("安装程序已下载并通过 SHA-256 校验。\r\n\r\n现在关闭本程序并启动安装吗？", "准备安装 v" + update.VersionText, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (start != DialogResult.Yes) return;
                Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true, Verb = "runas" });
                Application.Exit();
            }
            catch (Win32Exception exception)
            {
                downloadStatus.Text = "安装未启动。";
                MessageBox.Show(exception.NativeErrorCode == 1223 ? "已取消管理员授权，安装程序未启动。" : exception.Message, "无法启动安装", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception exception)
            {
                downloadStatus.Text = "更新失败：" + exception.Message;
                MessageBox.Show(exception.Message, "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                downloading = false;
                if (!IsDisposed)
                {
                    downloadButton.Enabled = update.CanDownload;
                    refreshNodesButton.Enabled = true;
                }
            }
        }

        protected override void OnShown(EventArgs eventArgs)
        {
            base.OnShown(eventArgs);
            UpdateWindowShape();
            ActiveControl = routeBox;
        }

        private void UpdateWindowShape()
        {
            if (Width <= 0 || Height <= 0) return;
            using (var path = ModernCard.RoundedPath(new Rectangle(0, 0, Width, Height), 12))
            {
                var old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        private void BeginWindowDrag(object sender, MouseEventArgs eventArgs)
        {
            if (eventArgs.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, 0x00A1, new IntPtr(2), IntPtr.Zero);
        }

        private sealed class RouteOption
        {
            public DownloadRouteKind Kind { get; private set; }
            private readonly string text;

            public RouteOption(DownloadRouteKind kind, string displayText)
            {
                Kind = kind;
                text = displayText;
            }

            public override string ToString()
            {
                return text;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr wordParameter, IntPtr longParameter);
    }
}
