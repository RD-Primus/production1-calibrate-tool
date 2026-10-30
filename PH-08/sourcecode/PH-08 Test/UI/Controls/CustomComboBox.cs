using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace PH_08_Test
{
    [ToolboxItem(true)]
    public class CustomComboBox : ComboBox
    {
        private const int WM_PAINT = 0x000F;
        private const int WM_PRINTCLIENT = 0x0318;
        private bool _hover;

        [Category("Modern")] public int BorderRadius { get; set; } = 10;
        [Category("Modern")] public int BorderSize { get; set; } = 1;
        [Category("Modern")] public Color BorderColor { get; set; } = Color.FromArgb(200, 200, 200);
        [Category("Modern")] public Color BorderFocusColor { get; set; } = Color.FromArgb(0, 120, 215);
        [Category("Modern")] public Color ArrowColor { get; set; } = Color.FromArgb(90, 90, 90);
        [Category("Modern")] public Color ItemHoverColor { get; set; } = Color.FromArgb(0, 120, 215);
        [Category("Modern")] public Color ItemHoverForeColor { get; set; } = Color.Empty;

        [Category("Modern")] public Color DisabledBackColor { get; set; } = Color.FromArgb(235, 238, 240);
        [Category("Modern")] public Color DisabledBorderColor { get; set; } = Color.FromArgb(215, 219, 223);
        [Category("Modern")] public Color DisabledForeColor { get; set; } = Color.FromArgb(120, 130, 140);
        [Category("Modern")] public Color DisabledArrowColor { get; set; } = Color.FromArgb(160, 170, 180);

        public CustomComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            Font = new Font("Segoe UI", 10f);
            ItemHeight = 28;
            BackColor = Color.White;
            ForeColor = Color.FromArgb(40, 40, 40);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if ((m.Msg == WM_PAINT || m.Msg == WM_PRINTCLIENT) && IsHandleCreated)
            {
                using (var g = Graphics.FromHwnd(Handle))
                    DrawControl(g);
            }
        }

        private void DrawControl(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // เคลียร์พื้นหลังด้วยสีของ parent เพื่อให้มุมมนดูเนียน
            g.Clear(Parent != null ? Parent.BackColor : SystemColors.Control);

            float half = BorderSize / 2f;
            var rect = new RectangleF(half, half, Width - BorderSize - 1, Height - BorderSize - 1);
            bool active = Enabled && (Focused || DroppedDown || _hover);

            Color effectiveBg = Enabled ? BackColor : DisabledBackColor;
            Color effectiveBorder = Enabled ? (active ? BorderFocusColor : BorderColor) : DisabledBorderColor;
            Color effectiveText = Enabled ? ForeColor : DisabledForeColor;
            Color effectiveArrow = Enabled ? ArrowColor : DisabledArrowColor;

            using (var path = RoundedRect(rect, BorderRadius))
            using (var bg = new SolidBrush(effectiveBg))
            using (var pen = new Pen(effectiveBorder, BorderSize))
            {
                g.FillPath(bg, path);
                if (BorderSize > 0) g.DrawPath(pen, path);
            }

            // ข้อความ
            var textRect = new Rectangle(12, 0, Width - 40, Height);
            TextRenderer.DrawText(g, SelectedIndex >= 0 ? GetItemText(SelectedItem) : "",
                Font, textRect, effectiveText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // ลูกศร chevron
            int cx = Width - 20, cy = Height / 2;
            using (var arrowPen = new Pen(effectiveArrow, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(arrowPen, cx - 4, cy - 2, cx, cy + 2);
                g.DrawLine(arrowPen, cx, cy + 2, cx + 4, cy - 2);
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            Color bgColor = selected ? ItemHoverColor : BackColor;
            using (var bg = new SolidBrush(bgColor))
                e.Graphics.FillRectangle(bg, e.Bounds);

            Color textColor;
            if (selected)
            {
                if (!ItemHoverForeColor.IsEmpty)
                {
                    textColor = ItemHoverForeColor;
                }
                else
                {
                    // If background is light (e.g. green tint, gray, white), use ForeColor (black); otherwise white
                    double luminance = (bgColor.R * 0.299 + bgColor.G * 0.587 + bgColor.B * 0.114);
                    textColor = luminance > 150 ? ForeColor : Color.White;
                }
            }
            else
            {
                textColor = ForeColor;
            }

            TextRenderer.DrawText(e.Graphics, GetItemText(Items[e.Index]), Font,
                new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height),
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        private static GraphicsPath RoundedRect(RectangleF r, int radius)
        {
            float d = Math.Max(1, radius * 2);
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void OnMouseEnter(EventArgs e) { if (!Enabled) return; _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnDropDownClosed(EventArgs e) { Invalidate(); base.OnDropDownClosed(e); }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (!Enabled) _hover = false;
            Invalidate();
        }
    }
}