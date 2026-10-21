using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TEST_DEF_03N
{
    [ToolboxItem(true)]
    [DefaultEvent("TextChanged")]
    public class CustomTextBox : UserControl
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        private readonly TextBox _tb = new TextBox();
        private bool _hover;
        private string _placeholder = "";

        [Category("Modern")] public int BorderRadius { get; set; } = 10;
        [Category("Modern")] public Color BorderColor { get; set; } = Color.FromArgb(200, 200, 200);
        [Category("Modern")] public Color BorderFocusColor { get; set; } = Color.FromArgb(0, 120, 215);
        [Category("Modern")] public int BorderSize { get; set; } = 1;
        [Category("Modern")] public int InnerPadding { get; set; } = 12;

        public CustomTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            _tb.BorderStyle = BorderStyle.None;
            _tb.Font = new Font("Segoe UI", 10f);
            _tb.BackColor = Color.White;
            _tb.ForeColor = Color.FromArgb(40, 40, 40);

            base.BackColor = Color.White;
            base.ForeColor = Color.FromArgb(40, 40, 40);
            base.Font = _tb.Font;

            Controls.Add(_tb);
            Size = new Size(200, 36);

            _tb.TextChanged += (s, e) => OnTextChanged(e);
            _tb.KeyDown += (s, e) => OnKeyDown(e);
            _tb.KeyPress += (s, e) => OnKeyPress(e);
            _tb.Enter += (s, e) => Invalidate();
            _tb.Leave += (s, e) => Invalidate();
            _tb.MouseEnter += (s, e) => SetHover(true);
            _tb.MouseLeave += (s, e) => UpdateHover();
            _tb.HandleCreated += (s, e) => ApplyPlaceholder();
        }

        // ---------- Properties ----------
        [Browsable(true), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text { get { return _tb.Text; } set { _tb.Text = value; } }

        [Category("Modern"), DefaultValue("")]
        public string PlaceholderText
        {
            get { return _placeholder; }
            set { _placeholder = value ?? ""; ApplyPlaceholder(); }
        }

        [Category("Modern"), DefaultValue(false)]
        public bool Multiline
        {
            get { return _tb.Multiline; }
            set { _tb.Multiline = value; _tb.ScrollBars = value ? ScrollBars.Vertical : ScrollBars.None; LayoutInner(); }
        }

        [Category("Modern"), DefaultValue(ScrollBars.None)]
        public ScrollBars ScrollBars
        {
            get { return _tb.ScrollBars; }
            set { _tb.ScrollBars = value; }
        }

        public void AppendText(string text)
        {
            _tb.AppendText(text);
        }

        public void Clear()
        {
            _tb.Clear();
        }

        [Category("Modern"), DefaultValue(false)]
        public bool ReadOnly { get { return _tb.ReadOnly; } set { _tb.ReadOnly = value; } }

        [Category("Modern"), DefaultValue(false)]
        public bool UseSystemPasswordChar { get { return _tb.UseSystemPasswordChar; } set { _tb.UseSystemPasswordChar = value; } }

        [Category("Modern"), DefaultValue('\0')]
        public char PasswordChar { get { return _tb.PasswordChar; } set { _tb.PasswordChar = value; } }

        [Category("Modern"), DefaultValue(32767)]
        public int MaxLength { get { return _tb.MaxLength; } set { _tb.MaxLength = value; } }

        [Category("Modern"), DefaultValue(HorizontalAlignment.Left)]
        public HorizontalAlignment TextAlign { get { return _tb.TextAlign; } set { _tb.TextAlign = value; } }

        public override Color BackColor
        {
            get { return base.BackColor; }
            set { base.BackColor = value; _tb.BackColor = value; Invalidate(); }
        }

        public override Color ForeColor
        {
            get { return base.ForeColor; }
            set { base.ForeColor = value; _tb.ForeColor = value; }
        }

        public override Font Font
        {
            get { return base.Font; }
            set { base.Font = value; _tb.Font = value; LayoutInner(); }
        }

        public void SelectAll() { _tb.SelectAll(); }
        public new bool Focus() { return _tb.Focus(); }

        // ---------- Layout ----------
        private void LayoutInner()
        {
            int pad = InnerPadding;
            if (_tb.Multiline)
            {
                _tb.SetBounds(pad, 8, Math.Max(1, Width - pad * 2), Math.Max(1, Height - 16));
            }
            else
            {
                _tb.Width = Math.Max(1, Width - pad * 2);
                _tb.Location = new Point(pad, Math.Max(0, (Height - _tb.Height) / 2));
            }
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); LayoutInner(); }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); LayoutInner(); }
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _tb.Enabled = Enabled;
            Invalidate();
        }

        private void ApplyPlaceholder()
        {
            if (_tb.IsHandleCreated)
                SendMessage(_tb.Handle, EM_SETCUEBANNER, (IntPtr)1, _placeholder);
        }

        // ---------- Hover ----------
        private void SetHover(bool value) { if (_hover != value) { _hover = value; Invalidate(); } }
        private void UpdateHover() { SetHover(ClientRectangle.Contains(PointToClient(Cursor.Position))); }
        protected override void OnMouseEnter(EventArgs e) { SetHover(true); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { UpdateHover(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e) { _tb.Focus(); base.OnMouseDown(e); }
        protected override void OnGotFocus(EventArgs e) { _tb.Focus(); base.OnGotFocus(e); }

        // ---------- Paint ----------
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : SystemColors.Control);

            float half = BorderSize / 2f;
            var rect = new RectangleF(half, half, Width - BorderSize - 1, Height - BorderSize - 1);
            bool active = _tb.Focused || _hover;

            using (var path = RoundedRect(rect, BorderRadius))
            using (var bg = new SolidBrush(BackColor))
            using (var pen = new Pen(active ? BorderFocusColor : BorderColor, BorderSize))
            {
                g.FillPath(bg, path);
                if (BorderSize > 0) g.DrawPath(pen, path);
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
