using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace TEST_DEF_03N
{
    public class CustomCheckBox : CheckBox
    {
        private Color _disabledForeColor = Color.FromArgb(140, 155, 175);
        private Color _disabledBorderColor = Color.FromArgb(50, 62, 78);
        private Color _disabledBoxColor = Color.FromArgb(20, 26, 36);
        private Color _checkedColor = Color.FromArgb(0, 120, 215);
        private Color _borderColor = Color.FromArgb(70, 85, 105);
        private Color _boxBackColor = Color.FromArgb(26, 36, 50); // กล่องสีเข้มทึบ เรียบหรู
        private int _boxSize = 16;
        private int _boxBorderRadius = 3;
        private int _boxTextGap = 8;
        private bool _isHovered = false;

        public CustomCheckBox()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        // ปิดเส้นประ Focus Cues ของ Windows เพื่อให้การแสดงผลสมูท 100% ไม่มีเส้นขีด
        protected override bool ShowFocusCues => false;

        [Category("Appearance")]
        [Description("สีตัวอักษรเมื่อปิดการใช้งาน (Locked / Disabled)")]
        public Color DisabledForeColor
        {
            get => _disabledForeColor;
            set { _disabledForeColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("สีขอบกล่องเมื่อปิดการใช้งาน (Locked / Disabled)")]
        public Color DisabledBorderColor
        {
            get => _disabledBorderColor;
            set { _disabledBorderColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("สีพื้นหลังกล่องเมื่อปิดการใช้งาน (Locked / Disabled)")]
        public Color DisabledBoxColor
        {
            get => _disabledBoxColor;
            set { _disabledBoxColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("สีขอบกล่องเมื่อเปิดใช้งานปกติ")]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("สีพื้นหลังกล่องทึบเมื่อยังไม่ติ๊กเลือก")]
        public Color BoxBackColor
        {
            get => _boxBackColor;
            set { _boxBackColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("สีพื้นหลังกล่องเมื่อติ๊กถูก (Checked)")]
        public Color CheckedColor
        {
            get => _checkedColor;
            set { _checkedColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("ขนาดของกล่องติ๊กถูก (พิกเซล)")]
        public int BoxSize
        {
            get => _boxSize;
            set { _boxSize = Math.Max(12, value); Invalidate(); }
        }

        [Category("Appearance")]
        [Description("ความโค้งมนของกล่องติ๊กถูก")]
        public int BoxBorderRadius
        {
            get => _boxBorderRadius;
            set { _boxBorderRadius = Math.Max(0, value); Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (Enabled)
            {
                _isHovered = true;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_isHovered)
            {
                _isHovered = false;
                Invalidate();
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            Invalidate();
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size textSize = TextRenderer.MeasureText(
                string.IsNullOrEmpty(Text) ? " " : Text,
                Font,
                proposedSize,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            int width = Padding.Left + _boxSize + _boxTextGap + textSize.Width + Padding.Right + 4;
            int height = Math.Max(_boxSize, textSize.Height) + Padding.Vertical + 4;
            return new Size(width, height);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // ป้องกันการกะพริบซ้ำซ้อนของการวาดพื้นหลัง
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // 1. เคลียร์พื้นหลังให้เนียนสนิทไปกับ Parent (เช่น MyRoundedPanel)
            Color bg = Parent?.BackColor ?? (BackColor != Color.Transparent ? BackColor : Color.FromArgb(16, 22, 30));
            using (var bgBrush = new SolidBrush(bg))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // 2. คำนวณตำแหน่งกล่องสี่เหลี่ยม
            int boxY = Math.Max(0, (Height - _boxSize) / 2);
            int boxX = Padding.Left + 1;
            Rectangle boxRect = new Rectangle(boxX, boxY, _boxSize - 1, _boxSize - 1);

            Color currentBoxBg;
            Color currentBoxBorder;
            Color currentCheckColor;
            Color currentTextColor;

            if (!Enabled)
            {
                currentBoxBg = _disabledBoxColor;
                currentBoxBorder = _disabledBorderColor;
                currentCheckColor = _disabledForeColor;
                currentTextColor = _disabledForeColor;
            }
            else
            {
                if (Checked)
                {
                    currentBoxBg = _checkedColor;
                    currentBoxBorder = _isHovered ? Color.FromArgb(40, 160, 255) : _checkedColor;
                    currentCheckColor = Color.White;
                }
                else
                {
                    currentBoxBg = _boxBackColor;
                    currentBoxBorder = _isHovered ? _checkedColor : _borderColor;
                    currentCheckColor = Color.Transparent;
                }
                currentTextColor = ForeColor;
            }

            // 3. วาดกล่อง CheckBox ขอบมน (ใช้ Pen Center 1px มาตรฐาน ป้องกัน GDI+ Artifacts)
            using (GraphicsPath path = GetRoundedPath(boxRect, _boxBorderRadius))
            {
                using (var brush = new SolidBrush(currentBoxBg))
                {
                    g.FillPath(brush, path);
                }

                using (var pen = new Pen(currentBoxBorder, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 4. วาดเครื่องหมายติ๊กถูก (Vector Checkmark ลายเส้นนุ่มนวล)
            if (Checked)
            {
                using (var checkPen = new Pen(currentCheckColor, 2f))
                {
                    checkPen.StartCap = LineCap.Round;
                    checkPen.EndCap = LineCap.Round;
                    checkPen.LineJoin = LineJoin.Round;

                    float bx = boxRect.X;
                    float by = boxRect.Y;
                    float bw = boxRect.Width;
                    float bh = boxRect.Height;

                    PointF p1 = new PointF(bx + bw * 0.22f, by + bh * 0.50f);
                    PointF p2 = new PointF(bx + bw * 0.44f, by + bh * 0.72f);
                    PointF p3 = new PointF(bx + bw * 0.80f, by + bh * 0.28f);

                    g.DrawLines(checkPen, new[] { p1, p2, p3 });
                }
            }

            // 5. วาดข้อความ Text
            int textX = boxRect.Right + _boxTextGap + 1;
            Rectangle textRect = new Rectangle(textX, 0, Math.Max(0, Width - textX), Height);
            TextFormatFlags flags = TextFormatFlags.Left |
                                    TextFormatFlags.VerticalCenter |
                                    TextFormatFlags.SingleLine |
                                    TextFormatFlags.NoPadding;

            TextRenderer.DrawText(g, Text, Font, textRect, currentTextColor, flags);
        }

        private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diameter = radius * 2;
            if (diameter > rect.Height) diameter = rect.Height;
            if (diameter > rect.Width) diameter = rect.Width;

            Rectangle arc = new Rectangle(rect.X, rect.Y, diameter, diameter);

            path.StartFigure();
            // Top-left
            path.AddArc(arc, 180, 90);
            // Top-right
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            // Bottom-right
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            // Bottom-left
            arc.X = rect.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
