using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GachaLinkFetcher.Services;

namespace GachaLinkFetcher.UI
{
    internal sealed class AboutForm : Form
    {
        private readonly Action checkForUpdates;

        public AboutForm(string dataDirectory, Action checkAction)
        {
            checkForUpdates = checkAction;
            Text = "关于抽卡链接获取工具";
            ClientSize = new Size(650, 560);
            MinimumSize = new Size(570, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = UiColors.Canvas;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BuildInterface(dataDirectory);
            Resize += delegate { UpdateWindowShape(); };
        }

        private void BuildInterface(string dataDirectory)
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = UiColors.Canvas, Margin = Padding.Empty, Padding = Padding.Empty };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));

            var titleBar = new Panel { Dock = DockStyle.Fill, BackColor = UiColors.Surface, Margin = Padding.Empty };
            var logo = new AppLogo { Location = new Point(18, 11) };
            var title = new Label { Text = "关于", Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold), ForeColor = UiColors.Text, Location = new Point(72, 17), AutoSize = true };
            var close = new WindowControlButton(WindowButtonKind.Close) { Dock = DockStyle.Right, Width = 48 };
            close.Click += delegate { Close(); };
            titleBar.Controls.AddRange(new Control[] { logo, title, close, new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = UiColors.Border } });
            titleBar.MouseDown += BeginWindowDrag;
            title.MouseDown += BeginWindowDrag;
            logo.MouseDown += BeginWindowDrag;
            root.Controls.Add(titleBar, 0, 0);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22, 20, 22, 8), BackColor = UiColors.Canvas };
            var card = new ModernCard { Dock = DockStyle.Fill, Padding = new Padding(24), BackColor = UiColors.Surface };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, BackColor = UiColors.Surface, Margin = Padding.Empty, Padding = Padding.Empty };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            layout.Controls.Add(new Label { Text = "抽卡链接获取工具", Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold), ForeColor = UiColors.Text, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            layout.Controls.Add(new Label { Text = "版本 v" + AppInfo.VersionText + " · 64 位 Windows", Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold), ForeColor = UiColors.Primary, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
            layout.Controls.Add(new Label { Text = "支持原神、崩坏：星穹铁道、绝区零和鸣潮", Dock = DockStyle.Fill, ForeColor = UiColors.TextMuted, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
            layout.Controls.Add(BuildPathRow("安装目录", AppInfo.InstallDirectory), 0, 3);
            layout.Controls.Add(BuildPathRow("数据目录", dataDirectory), 0, 4);

            var projectLink = new LinkLabel { Text = AppInfo.RepositoryUrl, Dock = DockStyle.Fill, LinkColor = UiColors.Primary, ActiveLinkColor = UiColors.PrimaryHover, VisitedLinkColor = UiColors.Primary, TextAlign = ContentAlignment.MiddleLeft };
            projectLink.LinkClicked += delegate
            {
                try { Process.Start(new ProcessStartInfo(AppInfo.RepositoryUrl) { UseShellExecute = true }); }
                catch (Exception exception) { MessageBox.Show(exception.Message, "无法打开项目页面", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            layout.Controls.Add(projectLink, 0, 5);
            layout.Controls.Add(new Label { Text = "程序只在你主动确认后请求游戏官方记录接口；抽卡记录、设置与备份均保存在本机用户数据目录。", Dock = DockStyle.Fill, ForeColor = UiColors.TextMuted, AutoEllipsis = true }, 0, 6);
            card.Controls.Add(layout);
            body.Controls.Add(card);
            root.Controls.Add(body, 0, 1);

            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = UiColors.Canvas, Padding = new Padding(22, 10, 22, 14) };
            var closeButton = MakeButton("关闭", 96, ModernButtonStyle.Outline);
            closeButton.Click += delegate { Close(); };
            var updateButton = MakeButton("检查更新", 116, ModernButtonStyle.Primary);
            updateButton.Click += delegate
            {
                Close();
                if (checkForUpdates != null) checkForUpdates();
            };
            footer.Controls.AddRange(new Control[] { closeButton, updateButton });
            root.Controls.Add(footer, 0, 2);
            Controls.Add(root);
        }

        private static Control BuildPathRow(string label, string value)
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiColors.Surface, Margin = Padding.Empty, Padding = new Padding(0, 5, 0, 5) };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold), ForeColor = UiColors.TextMuted, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            var text = new TextBox { Text = value, Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(250, 251, 255), ForeColor = UiColors.Text, Margin = new Padding(0, 8, 0, 0) };
            var field = new ModernCard { Dock = DockStyle.Fill, Radius = 9, BackColor = Color.FromArgb(250, 251, 255), Padding = new Padding(10, 6, 8, 4) };
            field.Controls.Add(text);
            row.Controls.Add(field, 1, 0);
            return row;
        }

        private static ModernButton MakeButton(string text, int width, ModernButtonStyle style)
        {
            return new ModernButton { Text = text, Width = width, Height = 40, ButtonStyle = style, Margin = new Padding(10, 0, 0, 0) };
        }

        protected override void OnShown(EventArgs eventArgs)
        {
            base.OnShown(eventArgs);
            UpdateWindowShape();
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

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr wordParameter, IntPtr longParameter);
    }
}
