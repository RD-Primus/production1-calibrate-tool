using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TEST_DEF_03N
{
    [ToolboxItem(true)]
    public class CustomComboBox : ComboBox
    {
        private const int WM_PAINT = 0x000F;
        private bool _hover;

        [Category("Modern")] public int BorderRadius { get; set; } = 10;
        [Category("Modern")] public int BorderSize { get; set; } = 1;
        [Category("Modern")] public Color BorderColor { get; set; } = Color.FromArgb(200, 200, 200);
        [Category("Modern")] public Color BorderFocusColor { get; set; } = Color.FromArgb(0, 120, 215);
        [Category("Modern")] public Color ArrowColor { get; set; } = Color.FromArgb(90, 90, 90);
        [Category("Modern")] public Color ItemHoverColor { get; set; } = Color.FromArgb(0, 120, 215);

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
            if (m.Msg == WM_PAINT && IsHandleCreated)
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
            bool active = Focused || DroppedDown || _hover;

            using (var path = RoundedRect(rect, BorderRadius))
            using (var bg = new SolidBrush(BackColor))
            using (var pen = new Pen(active ? BorderFocusColor : BorderColor, BorderSize))
            {
                g.FillPath(bg, path);
                if (BorderSize > 0) g.DrawPath(pen, path);
            }

            // ข้อความ
            var textRect = new Rectangle(12, 0, Width - 40, Height);
            TextRenderer.DrawText(g, SelectedIndex >= 0 ? GetItemText(SelectedItem) : "",
                Font, textRect, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // ลูกศร chevron
            int cx = Width - 20, cy = Height / 2;
            using (var arrowPen = new Pen(ArrowColor, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(arrowPen, cx - 4, cy - 2, cx, cy + 2);
                g.DrawLine(arrowPen, cx, cy + 2, cx + 4, cy - 2);
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using (var bg = new SolidBrush(selected ? ItemHoverColor : BackColor))
                e.Graphics.FillRectangle(bg, e.Bounds);

            TextRenderer.DrawText(e.Graphics, GetItemText(Items[e.Index]), Font,
                new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height),
                selected ? Color.White : ForeColor,
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

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnDropDownClosed(EventArgs e) { Invalidate(); base.OnDropDownClosed(e); }
    }
}
