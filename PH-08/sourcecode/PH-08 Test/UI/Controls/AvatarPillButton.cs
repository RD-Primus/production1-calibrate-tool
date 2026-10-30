using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace PH_08_Test
{
    [ToolboxItem(true)]
    public class AvatarPillButton : Control
    {
        private bool _isHovered = false;
        private bool _isPressed = false;
        private bool _isLoggedIn = false;
        private string _operatorName = string.Empty;
        private int _rightAnchorX = 1188;

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                _isLoggedIn = value;
                UpdateSizeAndPosition();
                Invalidate();
            }
        }

        public string OperatorName
        {
            get => _operatorName;
            set
            {
                _operatorName = value ?? string.Empty;
                UpdateSizeAndPosition();
                Invalidate();
            }
        }

        public string LoggedOutText { get; set; } = "Login";

        public int RightAnchorX
        {
            get => _rightAnchorX;
            set
            {
                _rightAnchorX = value;
                UpdateSizeAndPosition();
            }
        }

        public Color PillBackColor { get; set; } = Color.White;
        public Color PillHoverColor { get; set; } = Color.FromArgb(248, 250, 252);
        public Color PillBorderColor { get; set; } = Color.FromArgb(226, 232, 240);
        public Color AvatarBgColor { get; set; } = Color.FromArgb(209, 237, 217);
        public Color PrimaryTextColor { get; set; } = Color.FromArgb(27, 94, 32);

        public AvatarPillButton()
        {
            this.SetStyle(ControlStyles.UserPaint |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.SupportsTransparentBackColor, true);

            this.BackColor = Color.Transparent;
            this.Font = new Font("Leelawadee UI", 9.5F, FontStyle.Bold);
            this.Cursor = Cursors.Hand;
            this.Size = new Size(110, 38);
        }

        public void SetState(bool loggedIn, string name)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SetState(loggedIn, name)));
                return;
            }

            _isLoggedIn = loggedIn;
            _operatorName = name?.Trim() ?? string.Empty;
            UpdateSizeAndPosition();
            Invalidate();
        }

        [DefaultValue(false)]
        public bool AutoSizeWidth { get; set; } = false;

        private void UpdateSizeAndPosition()
        {
            if (!AutoSizeWidth)
            {
                // ไม่ปรับเปลี่ยน Width เอง เพื่อให้ขนาดและตำแหน่งอิงตามหน้า Designer 100%
                return;
            }

            string displayText = _isLoggedIn && !string.IsNullOrWhiteSpace(_operatorName)
                ? $"{_operatorName} · Logout"
                : LoggedOutText;

            int textWidth = TextRenderer.MeasureText(displayText, this.Font).Width;
            int newWidth = 42 + textWidth + 18;
            if (newWidth < 100) newWidth = 100;

            this.Width = newWidth;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _isPressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            // 1. Draw Pill Capsule Background
            using (var path = GetCapsulePath(rect))
            {
                Color currentBg = _isPressed
                    ? Color.FromArgb(235, 243, 237)
                    : (_isHovered ? PillHoverColor : PillBackColor);

                using (var bgBrush = new SolidBrush(currentBg))
                {
                    g.FillPath(bgBrush, path);
                }

                using (var borderPen = new Pen(PillBorderColor, 1.2f))
                {
                    borderPen.Alignment = PenAlignment.Inset;
                    g.DrawPath(borderPen, path);
                }
            }

            // 2. Draw Avatar Circle (Left)
            int avatarSize = 28;
            int avatarX = 5;
            int avatarY = (this.Height - avatarSize) / 2;
            Rectangle avatarRect = new Rectangle(avatarX, avatarY, avatarSize, avatarSize);

            using (var avatarPath = new GraphicsPath())
            {
                avatarPath.AddEllipse(avatarRect);
                using (var avatarBrush = new SolidBrush(AvatarBgColor))
                {
                    g.FillPath(avatarBrush, avatarPath);
                }

                // Clip person silhouette to the avatar circle
                var oldClip = g.Clip;
                g.SetClip(avatarPath, CombineMode.Intersect);

                float cx = avatarX + avatarSize / 2f;
                float cy = avatarY + avatarSize / 2f;

                using (var iconBrush = new SolidBrush(PrimaryTextColor))
                {
                    // Head (solid circle)
                    g.FillEllipse(iconBrush, cx - 4.5f, cy - 6.5f, 9f, 9f);

                    // Torso (solid curve clipped inside circle)
                    g.FillEllipse(iconBrush, cx - 8.5f, cy + 3.5f, 17f, 16f);
                }

                g.Clip = oldClip;
            }

            // 3. Draw Label Text
            string text = _isLoggedIn && !string.IsNullOrWhiteSpace(_operatorName)
                ? $"{_operatorName} · Logout"
                : LoggedOutText;

            Rectangle textRect = new Rectangle(38, 0, this.Width - 44, this.Height);
            TextRenderer.DrawText(g, text, this.Font, textRect, PrimaryTextColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
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
