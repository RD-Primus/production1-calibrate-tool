using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace CMATestVer1
{
    public class LedBulbControl : Control
    {
        private bool _isOn = false;
        private Color _onColor = Color.FromArgb(255, 180, 0);

        public bool IsOn
        {
            get => _isOn;
            set { _isOn = value; Invalidate(); }
        }

        public Color OnColor
        {
            get => _onColor;
            set { _onColor = value; Invalidate(); }
        }

        public LedBulbControl()
        {
            Size = new Size(100, 160);

            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                BackColor = Color.Transparent;
            }
            else
            {
                BackColor = Color.White;
            }

            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (LicenseManager.UsageMode != LicenseUsageMode.Runtime)
            {
                e.Graphics.Clear(Color.WhiteSmoke);
            }
            DrawBulb(e.Graphics);
        }

        private void DrawBulb(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cx = Width / 2;
            int cy = Height / 2 - 10;
            int r = (int)(Math.Min(Width, Height) * 0.34f);

            Rectangle bulbRect = new Rectangle(cx - r, cy - r - 8, r * 2, r * 2);
            int bulbCenterY = bulbRect.Y + r;

            // --- Aura ---
            if (_isOn)
            {
                for (int i = 4; i >= 1; i--)
                {
                    int ar = r + i * 11;
                    using var ab = new SolidBrush(Color.FromArgb(i * 14, _onColor));
                    g.FillEllipse(ab, cx - ar, bulbCenterY - ar, ar * 2, ar * 2);
                }
            }

            // --- Ray lines ---
            if (_isOn)
            {
                using var rayPen = new Pen(Color.FromArgb(210, _onColor), 2.8f);
                rayPen.StartCap = LineCap.Round;
                rayPen.EndCap = LineCap.Round;
                double[] angles = { 90, 55, 25, 0, -25, 125, 155, 180, 205 };
                foreach (double angle in angles)
                {
                    double rad = angle * Math.PI / 180.0;
                    int x1 = (int)(cx + Math.Cos(rad) * (r + 6));
                    int y1 = (int)(bulbCenterY - Math.Sin(rad) * (r + 6));
                    int x2 = (int)(cx + Math.Cos(rad) * (r + 18));
                    int y2 = (int)(bulbCenterY - Math.Sin(rad) * (r + 18));
                    g.DrawLine(rayPen, x1, y1, x2, y2);
                }
            }

            // --- ตัวหลอด Radial Gradient ---
            Color edgeColor = _isOn ? _onColor : Color.FromArgb(65, 65, 65);
            Color darkColor = _isOn ? ControlPaint.Dark(_onColor, 0.45f) : Color.FromArgb(28, 28, 28);
            Color centerColor = _isOn ? Color.White : Color.FromArgb(190, 190, 190);

            using (var path = new GraphicsPath())
            {
                path.AddEllipse(bulbRect);
                using var pgb = new PathGradientBrush(path);
                pgb.CenterColor = centerColor;
                pgb.SurroundColors = new[] { darkColor };
                pgb.CenterPoint = new PointF(cx - r * 0.22f, bulbRect.Y + r * 0.28f);
                g.FillEllipse(new SolidBrush(edgeColor), bulbRect);
                g.FillEllipse(pgb, bulbRect);
            }

            // --- ขอบหลอด ---
            Color borderColor = _isOn ? ControlPaint.Dark(_onColor, 0.25f) : Color.FromArgb(55, 55, 55);
            using var borderPen = new Pen(borderColor, 2f);
            g.DrawEllipse(borderPen, bulbRect);

            // --- Shine ใหญ่ ---
            int shineW = (int)(r * 0.55f);
            int shineH = (int)(r * 0.38f);
            int shineX = cx - (int)(r * 0.52f);
            int shineY = bulbRect.Y + (int)(r * 0.18f);
            using var sb1 = new SolidBrush(Color.FromArgb(_isOn ? 140 : 18, 255, 255, 255));
            g.FillEllipse(sb1, shineX, shineY, shineW, shineH);

            // --- Shine เล็ก ---
            using var sb2 = new SolidBrush(Color.FromArgb(_isOn ? 90 : 10, 255, 255, 255));
            g.FillEllipse(sb2, shineX + 4, shineY - 8, shineW / 2, shineH / 2);

            // --- ไส้หลอด (Filament) ---
            int fy = bulbCenterY + (int)(r * 0.15f);
            Color filColor = _isOn ? Color.FromArgb(255, 215, 80) : Color.FromArgb(75, 75, 75);
            using var filPen = new Pen(filColor, 1.8f);
            filPen.StartCap = LineCap.Round;
            filPen.EndCap = LineCap.Round;
            g.DrawArc(filPen, cx - 22, fy - 28, 20, 20, 0, 180);
            g.DrawArc(filPen, cx + 2, fy - 28, 20, 20, 0, 180);
            g.DrawLine(filPen, cx - 12, fy - 8, cx - 12, fy + 8);
            g.DrawLine(filPen, cx + 12, fy - 8, cx + 12, fy + 8);
            g.DrawLine(filPen, cx - 12, fy + 8, cx + 12, fy + 8);
            using var stemBrush = new SolidBrush(filColor);
            g.FillRectangle(stemBrush, cx - 3, fy + 8, 6, 12);

            // --- คอหลอด ---
            int neckTop = bulbRect.Bottom - 10;
            int neckBot = neckTop + 14;
            using var neckBrush = new SolidBrush(_isOn
                ? ControlPaint.Dark(_onColor, 0.5f)
                : Color.FromArgb(40, 40, 40));
            FillRoundedRect(g, neckBrush, cx - 20, neckTop, 40, 14, 3);

            // --- วงแหวนโลหะ ---
            int ringY = neckBot;
            using var ringBrush = new LinearGradientBrush(
                new Point(cx - r / 2, ringY),
                new Point(cx + r / 2, ringY),
                Color.FromArgb(140, 175, 200),
                Color.FromArgb(70, 100, 125));
            FillRoundedRect(g, ringBrush, cx - r / 2, ringY, r, 8, 3);

            // --- ขั้วเกลียว 4 ชั้น ---
            int[] widths = { r - 2, r - 8, r - 14, r - 20 };
            for (int i = 0; i < 4; i++)
            {
                int tw = widths[i];
                int tx = cx - tw / 2;
                int ty = ringY + 8 + i * 9;
                using var tb = new LinearGradientBrush(
                    new Point(tx, ty), new Point(tx + tw, ty),
                    Color.FromArgb(125, 158, 180),
                    Color.FromArgb(55, 80, 100));
                FillRoundedRect(g, tb, tx, ty, tw, 7, 2);
                using var hl = new Pen(Color.FromArgb(70, 210, 230, 245), 0.8f);
                g.DrawLine(hl, tx + 3, ty + 2, tx + tw - 3, ty + 2);
            }

            // --- ปลายขั้ว ---
            int tipW = widths[3] - 10;
            int tipY = ringY + 8 + 4 * 9;
            if (tipW > 0)
            {
                using var tipBrush = new LinearGradientBrush(
                    new Point(cx - tipW / 2, tipY),
                    new Point(cx + tipW / 2, tipY),
                    Color.FromArgb(160, 190, 210),
                    Color.FromArgb(80, 105, 125));
                FillRoundedRect(g, tipBrush, cx - tipW / 2, tipY, tipW, 5, 2);
            }
        }

        private void FillRoundedRect(Graphics g, Brush brush, int x, int y, int w, int h, int radius)
        {
            if (w <= 0 || h <= 0) return;
            radius = Math.Min(radius, Math.Min(w, h) / 2);
            using var path = new GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + w - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + w - radius * 2, y + h - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + h - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }
}