using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace PH_08_Test
{
    [ToolboxItem(true)]
    public class ConnectionStatusBadge : Control
    {
        private bool _isConnected = false;

        // Colors for Connected state (Soft mint pastel + forest green)
        public Color ConnectedBackColor { get; set; } = Color.FromArgb(209, 237, 217);
        public Color ConnectedBorderColor { get; set; } = Color.FromArgb(180, 225, 195);
        public Color ConnectedDotColor { get; set; } = Color.FromArgb(27, 94, 32);
        public Color ConnectedTextColor { get; set; } = Color.FromArgb(27, 94, 32);
        public string ConnectedText { get; set; } = "เชื่อมต่อแล้ว";

        // Colors for Disconnected state (Soft rose pastel + vibrant red)
        public Color DisconnectedBackColor { get; set; } = Color.FromArgb(254, 226, 226);
        public Color DisconnectedBorderColor { get; set; } = Color.FromArgb(254, 202, 202);
        public Color DisconnectedDotColor { get; set; } = Color.FromArgb(220, 38, 38);
        public Color DisconnectedTextColor { get; set; } = Color.FromArgb(185, 28, 28);
        public string DisconnectedText { get; set; } = "ยังไม่เชื่อมต่อ";

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    Invalidate();
                }
            }
        }

        public ConnectionStatusBadge()
        {
            this.SetStyle(ControlStyles.UserPaint |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.SupportsTransparentBackColor, true);

            this.BackColor = Color.Transparent;
            this.Font = new Font("Leelawadee UI", 9.5F, FontStyle.Bold);
            this.Size = new Size(136, 38);
        }

        public void SetConnected(bool connected)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SetConnected(connected)));
                return;
            }

            _isConnected = connected;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            Color currentBg = _isConnected ? ConnectedBackColor : DisconnectedBackColor;
            Color currentBorder = _isConnected ? ConnectedBorderColor : DisconnectedBorderColor;
            Color currentDot = _isConnected ? ConnectedDotColor : DisconnectedDotColor;
            Color currentText = _isConnected ? ConnectedTextColor : DisconnectedTextColor;
            string text = _isConnected ? ConnectedText : DisconnectedText;

            // 1. Draw Capsule Background & Border
            using (var path = GetCapsulePath(rect))
            {
                using (var bgBrush = new SolidBrush(currentBg))
                {
                    g.FillPath(bgBrush, path);
                }

                using (var borderPen = new Pen(currentBorder, 1.2f))
                {
                    borderPen.Alignment = PenAlignment.Inset;
                    g.DrawPath(borderPen, path);
                }
            }

            // 2. Draw Solid Status Dot
            int dotDiameter = 8;
            int dotX = 14;
            int dotY = (this.Height - dotDiameter) / 2;

            using (var dotBrush = new SolidBrush(currentDot))
            {
                g.FillEllipse(dotBrush, dotX, dotY, dotDiameter, dotDiameter);
            }

            // 3. Draw Status Text
            int textX = dotX + dotDiameter + 8;
            Rectangle textRect = new Rectangle(textX, 0, this.Width - textX - 8, this.Height);

            TextRenderer.DrawText(g, text, this.Font, textRect, currentText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private static GraphicsPath GetCapsulePath(Rectangle rect)
        {
            GraphicsPath path = new GraphicsPath();
            float radius = rect.Height / 2f;
            float diameter = radius * 2f;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 90, 180);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 180);
            path.CloseFigure();
            return path;
        }
    }
}
