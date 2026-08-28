using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GachaLinkFetcher.UI
{
    internal static class UiColors
    {
        public static readonly Color Canvas = Color.FromArgb(246, 248, 252);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceMuted = Color.FromArgb(241, 243, 244);
        public static readonly Color Border = Color.FromArgb(218, 220, 224);
        public static readonly Color Text = Color.FromArgb(31, 31, 31);
        public static readonly Color TextMuted = Color.FromArgb(95, 99, 104);
        public static readonly Color Primary = Color.FromArgb(11, 87, 208);
        public static readonly Color PrimaryHover = Color.FromArgb(8, 75, 184);
        public static readonly Color PrimaryContainer = Color.FromArgb(211, 227, 253);
        public static readonly Color Success = Color.FromArgb(24, 128, 56);
        public static readonly Color Warning = Color.FromArgb(180, 95, 6);
        public static readonly Color Error = Color.FromArgb(179, 38, 30);
    }

    internal enum ModernButtonStyle
    {
        Primary,
        Tonal,
        Outline,
        Text
    }

    internal enum WindowButtonKind
    {
        Minimize,
        Maximize,
        Close
    }

    internal sealed class ModernCard : Panel
    {
        public int Radius { get; set; }

        public ModernCard()
        {
            Radius = 18;
            BackColor = UiColors.Surface;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnResize(EventArgs eventArgs)
        {
            base.OnResize(eventArgs);
            if (Width <= 0 || Height <= 0) return;
            using (var path = RoundedPath(new Rectangle(0, 0, Width, Height), Radius))
            {
                var oldRegion = Region;
                Region = new Region(path);
                if (oldRegion != null) oldRegion.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            base.OnPaint(eventArgs);
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(UiColors.Border))
            using (var path = RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
                eventArgs.Graphics.DrawPath(pen, path);
        }

        internal static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            var diameter = Math.Max(2, radius * 2);
            var arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class ModernButton : Button
    {
        private bool hovered;
        private ModernButtonStyle buttonStyle;

        public ModernButtonStyle ButtonStyle
        {
            get { return buttonStyle; }
            set { buttonStyle = value; Invalidate(); }
        }

        public ModernButton()
        {
            ButtonStyle = ModernButtonStyle.Outline;
            Cursor = Cursors.Default;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            Height = 38;
            TabStop = true;
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs eventArgs) { hovered = true; Invalidate(); base.OnMouseEnter(eventArgs); }
        protected override void OnMouseLeave(EventArgs eventArgs) { hovered = false; Invalidate(); base.OnMouseLeave(eventArgs); }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.Clear(Parent == null ? UiColors.Surface : Parent.BackColor);
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            var fill = UiColors.Surface;
            var border = UiColors.Border;
            var text = UiColors.Primary;

            if (!Enabled)
            {
                fill = UiColors.SurfaceMuted;
                border = UiColors.SurfaceMuted;
                text = Color.FromArgb(154, 160, 166);
            }
            else if (ButtonStyle == ModernButtonStyle.Primary)
            {
                fill = hovered ? UiColors.PrimaryHover : UiColors.Primary;
                border = fill;
                text = Color.White;
            }
            else if (ButtonStyle == ModernButtonStyle.Tonal)
            {
                fill = hovered ? Color.FromArgb(194, 218, 255) : UiColors.PrimaryContainer;
                border = fill;
                text = Color.FromArgb(4, 57, 128);
            }
            else if (ButtonStyle == ModernButtonStyle.Text)
            {
                fill = hovered ? Color.FromArgb(235, 241, 250) : UiColors.Surface;
                border = fill;
            }
            else if (hovered)
            {
                fill = Color.FromArgb(247, 249, 252);
                border = Color.FromArgb(174, 179, 185);
            }

            using (var path = ModernCard.RoundedPath(bounds, 10))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border))
            {
                eventArgs.Graphics.FillPath(brush, path);
                if (ButtonStyle == ModernButtonStyle.Outline) eventArgs.Graphics.DrawPath(pen, path);
            }

            TextRenderer.DrawText(eventArgs.Graphics, Text, Font, bounds, text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (Focused && ShowFocusCues)
            {
                var focus = Rectangle.Inflate(bounds, -4, -4);
                ControlPaint.DrawFocusRectangle(eventArgs.Graphics, focus, text, fill);
            }
        }
    }

    internal sealed class ModernProgressBar : Control
    {
        private int value;

        public int Value
        {
            get { return value; }
            set { this.value = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        public ModernProgressBar()
        {
            Height = 8;
            BackColor = UiColors.SurfaceMuted;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.Clear(Parent == null ? UiColors.Surface : Parent.BackColor);
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var backgroundPath = ModernCard.RoundedPath(bounds, Math.Max(2, Height / 2)))
            using (var backgroundBrush = new SolidBrush(UiColors.SurfaceMuted))
                eventArgs.Graphics.FillPath(backgroundBrush, backgroundPath);
            if (value <= 0) return;
            var fillWidth = Math.Max(Height, (int)Math.Round(bounds.Width * value / 100D));
            using (var progressPath = ModernCard.RoundedPath(new Rectangle(bounds.X, bounds.Y, Math.Min(bounds.Width, fillWidth), bounds.Height), Math.Max(2, Height / 2)))
            using (var progressBrush = new SolidBrush(UiColors.Primary))
                eventArgs.Graphics.FillPath(progressBrush, progressPath);
        }
    }

    internal sealed class GameNavButton : Button
    {
        private bool selected;
        private bool compact;
        private bool hovered;

        public string FullText { get; set; }
        public string ShortText { get; set; }
        public Color AccentColor { get; set; }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; Invalidate(); }
        }

        public bool Compact
        {
            get { return compact; }
            set { compact = value; Text = value ? ShortText : FullText; TextAlign = value ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft; Padding = value ? Padding.Empty : new Padding(22, 0, 8, 0); Invalidate(); }
        }

        public GameNavButton()
        {
            AccentColor = UiColors.Primary;
            Cursor = Cursors.Default;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            Height = 46;
            TextAlign = ContentAlignment.MiddleLeft;
            Padding = new Padding(22, 0, 8, 0);
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs eventArgs) { hovered = true; Invalidate(); base.OnMouseEnter(eventArgs); }
        protected override void OnMouseLeave(EventArgs eventArgs) { hovered = false; Invalidate(); base.OnMouseLeave(eventArgs); }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.Clear(Parent == null ? UiColors.Canvas : Parent.BackColor);
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(4, 2, Width - 9, Height - 5);
            var fill = selected ? UiColors.PrimaryContainer : hovered ? Color.FromArgb(235, 241, 250) : UiColors.Canvas;
            using (var path = ModernCard.RoundedPath(bounds, 14))
            using (var brush = new SolidBrush(fill))
                eventArgs.Graphics.FillPath(brush, path);

            if (selected)
            {
                using (var brush = new SolidBrush(AccentColor))
                    eventArgs.Graphics.FillRectangle(brush, 4, 13, 4, Height - 26);
            }

            var textBounds = compact ? bounds : new Rectangle(bounds.X + 18, bounds.Y, bounds.Width - 22, bounds.Height);
            TextRenderer.DrawText(eventArgs.Graphics, Text, Font, textBounds, selected ? Color.FromArgb(4, 57, 128) : UiColors.Text, (compact ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left) | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    internal sealed class GameBadge : Control
    {
        public Color AccentColor { get; set; }
        public string BadgeText { get; set; }

        public GameBadge()
        {
            AccentColor = UiColors.Primary;
            BadgeText = "原";
            Size = new Size(64, 64);
            Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.Clear(Parent == null ? UiColors.Surface : Parent.BackColor);
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(1, 1, Width - 3, Height - 3);
            using (var path = ModernCard.RoundedPath(bounds, 18))
            using (var brush = new SolidBrush(AccentColor))
                eventArgs.Graphics.FillPath(brush, path);
            TextRenderer.DrawText(eventArgs.Graphics, BadgeText, Font, bounds, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    internal sealed class ModernComboBox : Control
    {
        internal sealed class ItemCollection
        {
            private readonly ModernComboBox owner;
            private readonly List<object> values = new List<object>();

            internal ItemCollection(ModernComboBox ownerControl) { owner = ownerControl; }
            public int Count { get { return values.Count; } }
            public object this[int index] { get { return values[index]; } }
            public int Add(object value) { values.Add(value); owner.Invalidate(); return values.Count - 1; }
            public void Clear() { values.Clear(); owner.SelectedIndex = -1; owner.Invalidate(); }
        }

        private readonly ItemCollection items;
        private int selectedIndex = -1;
        private bool hovered;
        private ModernDropDownForm openMenu;

        public event EventHandler SelectedIndexChanged;
        public ItemCollection Items { get { return items; } }
        public object SelectedItem { get { return selectedIndex < 0 || selectedIndex >= items.Count ? null : items[selectedIndex]; } }

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                if (value < -1 || value >= items.Count) throw new ArgumentOutOfRangeException("value");
                if (selectedIndex == value) return;
                selectedIndex = value;
                Invalidate();
                var handler = SelectedIndexChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        public ModernComboBox()
        {
            items = new ItemCollection(this);
            Height = 38;
            Font = new Font("Microsoft YaHei UI", 9.5F);
            ForeColor = UiColors.Text;
            BackColor = UiColors.Surface;
            Cursor = Cursors.Default;
            TabStop = true;
            AccessibleRole = AccessibleRole.ComboBox;
            AccessibleName = "查看卡池";
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        }

        protected override void OnMouseEnter(EventArgs eventArgs) { hovered = true; Invalidate(); base.OnMouseEnter(eventArgs); }
        protected override void OnMouseLeave(EventArgs eventArgs) { hovered = false; Invalidate(); base.OnMouseLeave(eventArgs); }

        protected override void OnMouseDown(MouseEventArgs eventArgs)
        {
            base.OnMouseDown(eventArgs);
            if (eventArgs.Button != MouseButtons.Left) return;
            Focus();
            ShowDropDown();
        }

        protected override void OnKeyDown(KeyEventArgs eventArgs)
        {
            if (eventArgs.KeyCode == Keys.Enter || eventArgs.KeyCode == Keys.Space || (eventArgs.Alt && eventArgs.KeyCode == Keys.Down))
            {
                ShowDropDown();
                eventArgs.Handled = true;
                return;
            }
            if (eventArgs.KeyCode == Keys.Down && selectedIndex < items.Count - 1)
            {
                SelectedIndex++;
                eventArgs.Handled = true;
                return;
            }
            if (eventArgs.KeyCode == Keys.Up && selectedIndex > 0)
            {
                SelectedIndex--;
                eventArgs.Handled = true;
                return;
            }
            base.OnKeyDown(eventArgs);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.Clear(Parent == null ? UiColors.Surface : Parent.BackColor);
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            var fill = hovered || Focused ? Color.FromArgb(247, 250, 255) : Color.FromArgb(250, 251, 255);
            var border = Focused ? UiColors.Primary : UiColors.Border;
            using (var path = ModernCard.RoundedPath(bounds, 10))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border, Focused ? 1.4F : 1F))
            {
                eventArgs.Graphics.FillPath(brush, path);
                eventArgs.Graphics.DrawPath(pen, path);
            }

            var text = SelectedItem == null ? "请选择" : SelectedItem.ToString();
            var textBounds = new Rectangle(12, 0, Math.Max(0, Width - 46), Height);
            TextRenderer.DrawText(eventArgs.Graphics, text, Font, textBounds, SelectedItem == null ? UiColors.TextMuted : ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            var centerX = Width - 20;
            var centerY = Height / 2;
            using (var pen = new Pen(UiColors.TextMuted, 1.6F))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                if (openMenu == null)
                {
                    eventArgs.Graphics.DrawLine(pen, centerX - 4, centerY - 2, centerX, centerY + 2);
                    eventArgs.Graphics.DrawLine(pen, centerX, centerY + 2, centerX + 4, centerY - 2);
                }
                else
                {
                    eventArgs.Graphics.DrawLine(pen, centerX - 4, centerY + 2, centerX, centerY - 2);
                    eventArgs.Graphics.DrawLine(pen, centerX, centerY - 2, centerX + 4, centerY + 2);
                }
            }
        }

        private void ShowDropDown()
        {
            if (items.Count == 0) return;
            if (openMenu != null && !openMenu.IsDisposed) openMenu.Close();
            var menu = new ModernDropDownForm(items, selectedIndex, Width, delegate(int index) { SelectedIndex = index; });
            menu.FormClosed += delegate
            {
                if (ReferenceEquals(openMenu, menu)) openMenu = null;
                Invalidate();
            };
            openMenu = menu;

            var location = PointToScreen(new Point(0, Height + 5));
            var workingArea = Screen.FromControl(this).WorkingArea;
            if (location.Y + menu.Height > workingArea.Bottom)
                location.Y = PointToScreen(Point.Empty).Y - menu.Height - 5;
            location.X = Math.Max(workingArea.Left + 4, Math.Min(location.X, workingArea.Right - menu.Width - 4));
            menu.Location = location;
            var owner = FindForm();
            if (owner == null) menu.Show(); else menu.Show(owner);
            menu.Activate();
            Invalidate();
        }

        private sealed class ModernDropDownForm : Form
        {
            internal ModernDropDownForm(ItemCollection items, int selectedIndex, int width, Action<int> chooseItem)
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                BackColor = UiColors.Surface;
                Font = new Font("Microsoft YaHei UI", 9.5F);
                AutoScaleMode = AutoScaleMode.Dpi;
                Size = new Size(width, items.Count * 34 + 12);
                Padding = Padding.Empty;
                var list = new DropDownListSurface(items, selectedIndex, chooseItem) { Dock = DockStyle.Fill };
                list.ItemChosen += delegate { Close(); };
                Controls.Add(list);
                Deactivate += delegate { Close(); };
                Shown += delegate { list.Focus(); };
            }

            protected override void OnResize(EventArgs eventArgs)
            {
                base.OnResize(eventArgs);
                if (Width <= 0 || Height <= 0) return;
                using (var path = ModernCard.RoundedPath(new Rectangle(0, 0, Width, Height), 12))
                {
                    var oldRegion = Region;
                    Region = new Region(path);
                    if (oldRegion != null) oldRegion.Dispose();
                }
            }
        }

        private sealed class DropDownListSurface : Control
        {
            private readonly ItemCollection items;
            private readonly Action<int> chooseItem;
            private int selectedIndex;
            private int hoverIndex = -1;

            internal event EventHandler ItemChosen;

            internal DropDownListSurface(ItemCollection values, int currentIndex, Action<int> choose)
            {
                items = values;
                selectedIndex = currentIndex;
                chooseItem = choose;
                BackColor = UiColors.Surface;
                Cursor = Cursors.Default;
                TabStop = true;
                AccessibleRole = AccessibleRole.List;
                AccessibleName = "卡池列表";
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            }

            protected override void OnMouseMove(MouseEventArgs eventArgs)
            {
                var next = HitTest(eventArgs.Location);
                if (next != hoverIndex) { hoverIndex = next; Invalidate(); }
                base.OnMouseMove(eventArgs);
            }

            protected override void OnMouseLeave(EventArgs eventArgs)
            {
                hoverIndex = -1;
                Invalidate();
                base.OnMouseLeave(eventArgs);
            }

            protected override void OnMouseDown(MouseEventArgs eventArgs)
            {
                base.OnMouseDown(eventArgs);
                if (eventArgs.Button != MouseButtons.Left) return;
                var index = HitTest(eventArgs.Location);
                if (index >= 0) Choose(index);
            }

            protected override void OnKeyDown(KeyEventArgs eventArgs)
            {
                if (eventArgs.KeyCode == Keys.Escape)
                {
                    var form = FindForm();
                    if (form != null) form.Close();
                    eventArgs.Handled = true;
                    return;
                }
                if (eventArgs.KeyCode == Keys.Down && selectedIndex < items.Count - 1) { selectedIndex++; Invalidate(); eventArgs.Handled = true; return; }
                if (eventArgs.KeyCode == Keys.Up && selectedIndex > 0) { selectedIndex--; Invalidate(); eventArgs.Handled = true; return; }
                if (eventArgs.KeyCode == Keys.Enter || eventArgs.KeyCode == Keys.Space) { if (selectedIndex >= 0) Choose(selectedIndex); eventArgs.Handled = true; return; }
                base.OnKeyDown(eventArgs);
            }

            protected override void OnPaint(PaintEventArgs eventArgs)
            {
                eventArgs.Graphics.Clear(UiColors.Surface);
                eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var outer = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var borderPath = ModernCard.RoundedPath(outer, 12))
                using (var pen = new Pen(UiColors.Border))
                    eventArgs.Graphics.DrawPath(pen, borderPath);

                for (var index = 0; index < items.Count; index++)
                {
                    var row = new Rectangle(6, 6 + index * 34, Width - 12, 30);
                    if (index == selectedIndex || index == hoverIndex)
                    {
                        var fill = index == selectedIndex ? UiColors.PrimaryContainer : Color.FromArgb(241, 245, 251);
                        using (var path = ModernCard.RoundedPath(row, 8))
                        using (var brush = new SolidBrush(fill))
                            eventArgs.Graphics.FillPath(brush, path);
                    }
                    var textBounds = new Rectangle(row.X + 10, row.Y, row.Width - 38, row.Height);
                    var textColor = index == selectedIndex ? Color.FromArgb(4, 57, 128) : UiColors.Text;
                    var textFont = index == selectedIndex ? new Font(Font, FontStyle.Bold) : Font;
                    TextRenderer.DrawText(eventArgs.Graphics, items[index] == null ? string.Empty : items[index].ToString(), textFont, textBounds, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                    if (!ReferenceEquals(textFont, Font)) textFont.Dispose();
                    if (index == selectedIndex)
                    {
                        var checkBounds = new Rectangle(row.Right - 28, row.Y, 20, row.Height);
                        using (var checkFont = new Font(Font, FontStyle.Bold))
                            TextRenderer.DrawText(eventArgs.Graphics, "✓", checkFont, checkBounds, UiColors.Primary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    }
                }
            }

            private int HitTest(Point point)
            {
                if (point.Y < 6) return -1;
                var index = (point.Y - 6) / 34;
                if (index < 0 || index >= items.Count) return -1;
                var row = new Rectangle(6, 6 + index * 34, Width - 12, 30);
                return row.Contains(point) ? index : -1;
            }

            private void Choose(int index)
            {
                selectedIndex = index;
                chooseItem(index);
                var handler = ItemChosen;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }
    }

    internal sealed class WindowControlButton : Button
    {
        private bool hovered;
        private bool restoreGlyph;

        public WindowButtonKind ButtonKind { get; set; }
        public bool RestoreGlyph
        {
            get { return restoreGlyph; }
            set { restoreGlyph = value; Invalidate(); }
        }

        public WindowControlButton(WindowButtonKind kind)
        {
            ButtonKind = kind;
            Size = new Size(42, 38);
            Cursor = Cursors.Default;
            TabStop = false;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            AccessibleName = kind == WindowButtonKind.Minimize ? "最小化" : kind == WindowButtonKind.Maximize ? "最大化" : "关闭";
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs eventArgs) { hovered = true; Invalidate(); base.OnMouseEnter(eventArgs); }
        protected override void OnMouseLeave(EventArgs eventArgs) { hovered = false; Invalidate(); base.OnMouseLeave(eventArgs); }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.Clear(Parent == null ? UiColors.Surface : Parent.BackColor);
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(2, 2, Width - 5, Height - 5);
            if (hovered)
            {
                var fill = ButtonKind == WindowButtonKind.Close ? Color.FromArgb(220, 53, 69) : Color.FromArgb(235, 241, 250);
                using (var path = ModernCard.RoundedPath(bounds, 9))
                using (var brush = new SolidBrush(fill))
                    eventArgs.Graphics.FillPath(brush, path);
            }

            var color = hovered && ButtonKind == WindowButtonKind.Close ? Color.White : UiColors.TextMuted;
            using (var pen = new Pen(color, 1.5F))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                var centerX = Width / 2;
                var centerY = Height / 2;
                if (ButtonKind == WindowButtonKind.Minimize)
                    eventArgs.Graphics.DrawLine(pen, centerX - 5, centerY + 4, centerX + 5, centerY + 4);
                else if (ButtonKind == WindowButtonKind.Close)
                {
                    eventArgs.Graphics.DrawLine(pen, centerX - 5, centerY - 5, centerX + 5, centerY + 5);
                    eventArgs.Graphics.DrawLine(pen, centerX + 5, centerY - 5, centerX - 5, centerY + 5);
                }
                else if (!RestoreGlyph)
                    eventArgs.Graphics.DrawRectangle(pen, centerX - 5, centerY - 5, 10, 10);
                else
                {
                    eventArgs.Graphics.DrawRectangle(pen, centerX - 3, centerY - 5, 9, 9);
                    eventArgs.Graphics.DrawRectangle(pen, centerX - 6, centerY - 2, 9, 9);
                }
            }
        }
    }

    internal sealed class AppLogo : Control
    {
        public AppLogo()
        {
            Size = new Size(42, 42);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.Clear(Parent == null ? UiColors.Surface : Parent.BackColor);
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(1, 1, Width - 3, Height - 3);
            using (var path = ModernCard.RoundedPath(bounds, 12))
            using (var brush = new SolidBrush(UiColors.Primary))
                eventArgs.Graphics.FillPath(brush, path);
            using (var pen = new Pen(Color.White, 3F))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                eventArgs.Graphics.DrawArc(pen, 9, 13, 17, 14, 135, 210);
                eventArgs.Graphics.DrawArc(pen, 17, 15, 17, 14, -45, 210);
            }
        }
    }
}
