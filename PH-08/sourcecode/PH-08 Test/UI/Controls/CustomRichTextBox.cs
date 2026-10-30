using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PH_08_Test
{
    [ToolboxItem(true)]
    [DefaultEvent("TextChanged")]
    public class CustomRichTextBox : UserControl
    {
        private readonly RichTextBox _rtb = new RichTextBox();
        private bool _hover;

        [Category("Modern")] public int BorderRadius { get; set; } = 2;
        [Category("Modern")] public Color BorderColor { get; set; } = Color.FromArgb(200, 200, 200);
        [Category("Modern")] public Color BorderFocusColor { get; set; } = Color.FromArgb(0, 120, 215);
        [Category("Modern")] public int BorderSize { get; set; } = 1;
        [Category("Modern")] public int InnerPadding { get; set; } = 10;

        public CustomRichTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            _rtb.BorderStyle = BorderStyle.None;
            _rtb.Font = new Font("Leelawadee UI", 9.75f);
            _rtb.BackColor = Color.White;
            _rtb.ForeColor = Color.FromArgb(40, 40, 40);
            _rtb.ScrollBars = RichTextBoxScrollBars.Vertical;
            _rtb.ReadOnly = true;

            base.BackColor = Color.White;
            base.ForeColor = Color.FromArgb(40, 40, 40);
            base.Font = _rtb.Font;

            Controls.Add(_rtb);
            Size = new Size(200, 100);

            _rtb.TextChanged += (s, e) => OnTextChanged(e);
            _rtb.Enter += (s, e) => Invalidate();
            _rtb.Leave += (s, e) => Invalidate();
            _rtb.MouseEnter += (s, e) => SetHover(true);
            _rtb.MouseLeave += (s, e) => UpdateHover();
        }

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string Text
        {
            get => _rtb.Text;
            set => _rtb.Text = value ?? string.Empty;
        }

        [Category("Modern"), DefaultValue(true)]
        public bool Multiline { get; set; } = true;

        [Category("Modern"), DefaultValue(ScrollBars.Vertical)]
        public ScrollBars ScrollBars
        {
            get => _rtb.ScrollBars == RichTextBoxScrollBars.Vertical ? ScrollBars.Vertical : ScrollBars.None;
            set => _rtb.ScrollBars = (value == ScrollBars.Vertical || value == ScrollBars.Both)
                ? RichTextBoxScrollBars.Vertical
                : RichTextBoxScrollBars.None;
        }

        [Category("Modern"), DefaultValue(true)]
        public bool ReadOnly
        {
            get => _rtb.ReadOnly;
            set => _rtb.ReadOnly = value;
        }

        public void AppendText(string text)
        {
            AppendText(text, ForeColor);
        }

        public void AppendText(string text, Color color)
        {
            if (_rtb.InvokeRequired)
            {
                _rtb.BeginInvoke(new Action(() => AppendText(text, color)));
                return;
            }

            _rtb.SelectionStart = _rtb.TextLength;
            _rtb.SelectionLength = 0;
            _rtb.SelectionColor = color;
            _rtb.AppendText(text);
            _rtb.SelectionColor = _rtb.ForeColor;
            _rtb.SelectionStart = _rtb.TextLength;
            _rtb.ScrollToCaret();
        }

        public void Clear()
        {
            if (_rtb.InvokeRequired)
            {
                _rtb.BeginInvoke(new Action(Clear));
                return;
            }
            _rtb.Clear();
        }

        public override Color BackColor
        {
            get => base.BackColor;
            set
            {
                base.BackColor = value;
                _rtb.BackColor = value;
                Invalidate();
            }
        }

        public override Color ForeColor
        {
            get => base.ForeColor;
            set
            {
                base.ForeColor = value;
                _rtb.ForeColor = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override Font Font
        {
            get => base.Font;
            set
            {
                base.Font = value;
                if (value != null)
                {
                    _rtb.Font = value;
                }
                LayoutInner();
            }
        }

        private void LayoutInner()
        {
            int pad = InnerPadding;
            _rtb.SetBounds(pad, pad, Math.Max(1, Width - pad * 2), Math.Max(1, Height - pad * 2));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInner();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            LayoutInner();
        }

        private void SetHover(bool value)
        {
            if (_hover != value)
            {
                _hover = value;
                Invalidate();
            }
        }

        private void UpdateHover()
        {
            SetHover(ClientRectangle.Contains(PointToClient(Cursor.Position)));
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            SetHover(true);
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            UpdateHover();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _rtb.Focus();
            base.OnMouseDown(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            _rtb.Focus();
            base.OnGotFocus(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : SystemColors.Control);

            float half = BorderSize / 2f;
            var rect = new RectangleF(half, half, Width - BorderSize - 1, Height - BorderSize - 1);
            bool active = _rtb.Focused || _hover;

            using (var path = RoundedRect(rect, BorderRadius))
            using (var bg = new SolidBrush(BackColor))
            using (var pen = new Pen(active ? BorderFocusColor : BorderColor, BorderSize))
            {
                g.FillPath(bg, path);
                if (BorderSize > 0)
                {
                    g.DrawPath(pen, path);
                }
            }
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
    }
}
