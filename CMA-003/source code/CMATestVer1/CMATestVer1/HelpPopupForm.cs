using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CMATestVer1
{
    internal sealed class HelpPopupForm : Form
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUNDED = 2;

        private static readonly Color PrimaryGreen = Color.FromArgb(34, 91, 52);
        private static readonly Color HeaderDarkGreen = Color.FromArgb(26, 74, 42);
        private static readonly Color SurfaceGreen = Color.FromArgb(232, 240, 216);
        private static readonly Color CardBg = Color.FromArgb(244, 248, 236);
        private static readonly Color InactiveTabBg = Color.FromArgb(184, 207, 172);
        private static readonly Color InactiveTabHover = Color.FromArgb(168, 194, 155);
        private static readonly Color DangerColor = Color.FromArgb(181, 57, 34);
        private static readonly Color DangerHover = Color.FromArgb(215, 60, 40);
        private static readonly Color NavBtnBg = Color.FromArgb(244, 248, 236);
        private static readonly Color NavBtnHover = Color.FromArgb(220, 235, 208);
        private static readonly Color NavBtnDisabled = Color.FromArgb(210, 215, 205);
        private static readonly Color TextDarkGreen = Color.FromArgb(41, 67, 32);

        private const int TotalWidth = 1180;
        private const int TotalHeight = 660;

        private enum HelpMode
        {
            NoServer = 0,
            WithServer = 1
        }

        private HelpMode _currentMode = HelpMode.NoServer;
        private int _currentPageIndex = 0;

        private readonly Dictionary<int, Image> _noServerSlides = new();
        private readonly Dictionary<int, Image> _withServerSlides = new();

        private bool _isDragging;
        private Point _dragStartCursor;
        private Point _dragStartForm;

        // UI Controls
        private Button _btnTabNoServer = null!;
        private Button _btnTabWithServer = null!;
        private Label _lblPageIndicator = null!;
        private Button _btnPrev = null!;
        private Button _btnNext = null!;
        private PictureBox _picSlide = null!;
        private Panel _slideContainer = null!;

        protected override CreateParams CreateParams
        {
            get
            {
                const int CsDropShadow = 0x00020000;
                CreateParams parameters = base.CreateParams;
                parameters.ClassStyle |= CsDropShadow;
                return parameters;
            }
        }

        internal HelpPopupForm()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = SurfaceGreen;
            ClientSize = new Size(TotalWidth, TotalHeight);
            Font = new Font("Tahoma", 10F, FontStyle.Regular, GraphicsUnit.Point);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "ขั้นตอนการใช้งานเบื้องต้น";
            KeyPreview = true;
            DoubleBuffered = true;

            LoadSlideImages();
            BuildUi();
            UpdateDisplay();
        }

        private void BuildUi()
        {
            // 1. Header Bar (Dragable)
            var header = BuildHeader();
            Controls.Add(header);

            // 2. Sub-Header Toolbar (Tabs + Navigation)
            var toolbar = BuildToolbar();
            Controls.Add(toolbar);

            // 3. Slide Content Area
            _slideContainer = new Panel
            {
                Location = new Point(12, 106),
                Size = new Size(TotalWidth - 24, TotalHeight - 118),
                BackColor = CardBg
            };
            ApplyRoundedRegion(_slideContainer, 8);

            _picSlide = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = CardBg
            };

            _slideContainer.Controls.Add(_picSlide);
            Controls.Add(_slideContainer);

            // Keyboard navigation
            KeyDown += Form_KeyDown;
        }

        private Panel BuildHeader()
        {
            var header = new Panel
            {
                BackColor = HeaderDarkGreen,
                Dock = DockStyle.Top,
                Height = 48,
                Cursor = Cursors.SizeAll
            };

            var title = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Tahoma", 12.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "📖  ขั้นตอนการใช้งานเบื้องต้น (User Guide)",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                Cursor = Cursors.SizeAll
            };

            var closeButton = new Button
            {
                BackColor = DangerColor,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Tahoma", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Size = new Size(48, 48),
                Text = "✕",
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.MouseEnter += (_, _) => closeButton.BackColor = DangerHover;
            closeButton.MouseLeave += (_, _) => closeButton.BackColor = DangerColor;
            closeButton.Click += (_, _) =>
            {
                DialogResult = DialogResult.OK;
                Close();
            };

            header.MouseDown += Header_MouseDown;
            header.MouseMove += Header_MouseMove;
            header.MouseUp += Header_MouseUp;
            title.MouseDown += Header_MouseDown;
            title.MouseMove += Header_MouseMove;
            title.MouseUp += Header_MouseUp;

            header.Controls.Add(title);
            header.Controls.Add(closeButton);
            closeButton.BringToFront();
            return header;
        }

        private Panel BuildToolbar()
        {
            var toolbar = new Panel
            {
                Location = new Point(12, 54),
                Size = new Size(TotalWidth - 24, 46),
                BackColor = SurfaceGreen
            };

            // Mode Tab 1: กรณีไม่ต้องการส่งไปที่ Server
            _btnTabNoServer = new Button
            {
                Location = new Point(0, 2),
                Size = new Size(285, 40),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Tahoma", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Text = "📤  กรณีไม่ต้องการส่งไปที่ Server",
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            _btnTabNoServer.FlatAppearance.BorderSize = 0;
            _btnTabNoServer.Click += (_, _) => SwitchMode(HelpMode.NoServer);
            ApplyRoundedRegion(_btnTabNoServer, 6);

            // Mode Tab 2: กรณีต้องการส่งไปที่ Server
            _btnTabWithServer = new Button
            {
                Location = new Point(295, 2),
                Size = new Size(285, 40),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Tahoma", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Text = "🌐  กรณีต้องการส่งไปที่ Server",
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            _btnTabWithServer.FlatAppearance.BorderSize = 0;
            _btnTabWithServer.Click += (_, _) => SwitchMode(HelpMode.WithServer);
            ApplyRoundedRegion(_btnTabWithServer, 6);

            // Navigation Area (Right side)
            int navRight = toolbar.Width;

            // Next Button
            _btnNext = new Button
            {
                Location = new Point(navRight - 44, 2),
                Size = new Size(44, 40),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Tahoma", 12F, FontStyle.Bold),
                ForeColor = TextDarkGreen,
                BackColor = NavBtnBg,
                Text = "▶",
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            _btnNext.FlatAppearance.BorderSize = 0;
            _btnNext.MouseEnter += (s, _) => { if (((Button)s!).Enabled) ((Button)s!).BackColor = NavBtnHover; };
            _btnNext.MouseLeave += (s, _) => { if (((Button)s!).Enabled) ((Button)s!).BackColor = NavBtnBg; };
            _btnNext.Click += (_, _) => NextPage();
            ApplyRoundedRegion(_btnNext, 6);

            // Prev Button
            _btnPrev = new Button
            {
                Location = new Point(navRight - 94, 2),
                Size = new Size(44, 40),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Tahoma", 12F, FontStyle.Bold),
                ForeColor = TextDarkGreen,
                BackColor = NavBtnBg,
                Text = "◀",
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            _btnPrev.FlatAppearance.BorderSize = 0;
            _btnPrev.MouseEnter += (s, _) => { if (((Button)s!).Enabled) ((Button)s!).BackColor = NavBtnHover; };
            _btnPrev.MouseLeave += (s, _) => { if (((Button)s!).Enabled) ((Button)s!).BackColor = NavBtnBg; };
            _btnPrev.Click += (_, _) => PrevPage();
            ApplyRoundedRegion(_btnPrev, 6);

            // Page Indicator Badge
            _lblPageIndicator = new Label
            {
                Location = new Point(navRight - 190, 2),
                Size = new Size(88, 40),
                Font = new Font("Tahoma", 12F, FontStyle.Bold),
                ForeColor = TextDarkGreen,
                BackColor = CardBg,
                TextAlign = ContentAlignment.MiddleCenter
            };
            ApplyRoundedRegion(_lblPageIndicator, 6);

            toolbar.Controls.Add(_btnTabNoServer);
            toolbar.Controls.Add(_btnTabWithServer);
            toolbar.Controls.Add(_lblPageIndicator);
            toolbar.Controls.Add(_btnPrev);
            toolbar.Controls.Add(_btnNext);

            return toolbar;
        }

        private void LoadSlideImages()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] searchPaths = new[]
            {
                Path.Combine(baseDir, "Resources", "Help"),
                Path.Combine(baseDir, "..", "..", "..", "Resources", "Help"),
                Path.Combine(baseDir, "..", "..", "..", "..", "Resources", "Help"),
                @"c:\Primus\do by mine\project\CMATestVer1\CMATestVer1\Resources\Help"
            };

            string helpDir = string.Empty;
            foreach (var path in searchPaths)
            {
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "slide_1.png")))
                {
                    helpDir = Path.GetFullPath(path);
                    break;
                }
            }

            if (string.IsNullOrEmpty(helpDir))
                return;

            try
            {
                // Mode NoServer: Slide 1 (1/2), Slide 2 (2/2)
                LoadSlideSafe(_noServerSlides, 0, Path.Combine(helpDir, "slide_1.png"));
                LoadSlideSafe(_noServerSlides, 1, Path.Combine(helpDir, "slide_2.png"));

                // Mode WithServer: Slide 3 (1/3), Slide 4 (2/3), Slide 5 (3/3)
                LoadSlideSafe(_withServerSlides, 0, Path.Combine(helpDir, "slide_3.png"));
                LoadSlideSafe(_withServerSlides, 1, Path.Combine(helpDir, "slide_4.png"));
                LoadSlideSafe(_withServerSlides, 2, Path.Combine(helpDir, "slide_5.png"));
            }
            catch
            {
                // Ignore load errors gracefully
            }
        }

        private static void LoadSlideSafe(Dictionary<int, Image> dict, int index, string filePath)
        {
            if (File.Exists(filePath))
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var original = Image.FromStream(fs);
                dict[index] = new Bitmap(original);
            }
        }

        private void SwitchMode(HelpMode mode)
        {
            if (_currentMode == mode)
                return;

            _currentMode = mode;
            _currentPageIndex = 0;
            UpdateDisplay();
        }

        private void PrevPage()
        {
            if (_currentPageIndex > 0)
            {
                _currentPageIndex--;
                UpdateDisplay();
            }
        }

        private void NextPage()
        {
            int maxPages = GetTotalPages();
            if (_currentPageIndex < maxPages - 1)
            {
                _currentPageIndex++;
                UpdateDisplay();
            }
        }

        private int GetTotalPages()
        {
            return _currentMode == HelpMode.NoServer ? 2 : 3;
        }

        private void UpdateDisplay()
        {
            int totalPages = GetTotalPages();

            // Tab Buttons appearance
            if (_currentMode == HelpMode.NoServer)
            {
                _btnTabNoServer.BackColor = PrimaryGreen;
                _btnTabNoServer.ForeColor = Color.White;
                _btnTabWithServer.BackColor = InactiveTabBg;
                _btnTabWithServer.ForeColor = TextDarkGreen;
            }
            else
            {
                _btnTabNoServer.BackColor = InactiveTabBg;
                _btnTabNoServer.ForeColor = TextDarkGreen;
                _btnTabWithServer.BackColor = PrimaryGreen;
                _btnTabWithServer.ForeColor = Color.White;
            }

            // Page Indicator
            _lblPageIndicator.Text = $"{_currentPageIndex + 1} / {totalPages}";

            // Navigation Buttons state
            _btnPrev.Enabled = _currentPageIndex > 0;
            _btnPrev.BackColor = _btnPrev.Enabled ? NavBtnBg : NavBtnDisabled;
            _btnPrev.ForeColor = _btnPrev.Enabled ? TextDarkGreen : Color.Gray;

            _btnNext.Enabled = _currentPageIndex < totalPages - 1;
            _btnNext.BackColor = _btnNext.Enabled ? NavBtnBg : NavBtnDisabled;
            _btnNext.ForeColor = _btnNext.Enabled ? TextDarkGreen : Color.Gray;

            // Slide Image
            var currentDict = _currentMode == HelpMode.NoServer ? _noServerSlides : _withServerSlides;
            if (currentDict.TryGetValue(_currentPageIndex, out var img))
            {
                _picSlide.Image = img;
            }
            else
            {
                _picSlide.Image = null;
            }
        }

        private void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.OK;
                Close();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Left)
            {
                PrevPage();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right)
            {
                NextPage();
                e.Handled = true;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int cornerPreference = DWMWCP_ROUNDED;
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

                var margins = new MARGINS { cxLeftWidth = 1, cxRightWidth = 1, cyTopHeight = 1, cyBottomHeight = 1 };
                DwmExtendFrameIntoClientArea(Handle, ref margins);
            }
            catch
            {
                // Fallback for older Windows environments
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyRoundedRegion(this, 8);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundedRegion(this, 8);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Outer dark accent border for sharp elevation
            var borderBounds = new Rectangle(1, 1, Width - 3, Height - 3);
            using var borderPath = CreateRoundedPath(borderBounds, 8);
            using var borderPen = new Pen(Color.FromArgb(20, 55, 30), 3F);
            e.Graphics.DrawPath(borderPen, borderPath);

            // Subtle inner glow
            var innerBounds = new Rectangle(3, 3, Width - 7, Height - 7);
            using var innerPath = CreateRoundedPath(innerBounds, 7);
            using var innerPen = new Pen(Color.FromArgb(70, Color.White), 1.5F);
            e.Graphics.DrawPath(innerPen, innerPath);
        }

        private static void ApplyRoundedRegion(Control control, int cornerRadius)
        {
            if (control.Width <= 0 || control.Height <= 0)
                return;

            int diameter = Math.Min(cornerRadius * 2, Math.Min(control.Width, control.Height));
            if (diameter <= 0)
                return;

            int right = control.Width - 1;
            int bottom = control.Height - 1;

            using var path = new GraphicsPath();
            path.AddArc(0, 0, diameter, diameter, 180, 90);
            path.AddArc(right - diameter, 0, diameter, diameter, 270, 90);
            path.AddArc(right - diameter, bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(0, bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            Region? previousRegion = control.Region;
            control.Region = new Region(path);
            previousRegion?.Dispose();
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int cornerRadius)
        {
            int diameter = Math.Min(cornerRadius * 2, Math.Min(bounds.Width, bounds.Height));
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void Header_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            _isDragging = true;
            _dragStartCursor = Cursor.Position;
            _dragStartForm = Location;
        }

        private void Header_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isDragging)
                return;

            Point currentCursor = Cursor.Position;
            Location = new Point(
                _dragStartForm.X + (currentCursor.X - _dragStartCursor.X),
                _dragStartForm.Y + (currentCursor.Y - _dragStartCursor.Y));
        }

        private void Header_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _isDragging = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var img in _noServerSlides.Values)
                    img.Dispose();
                _noServerSlides.Clear();

                foreach (var img in _withServerSlides.Values)
                    img.Dispose();
                _withServerSlides.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
