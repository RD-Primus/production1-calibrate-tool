using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CMATestVer1
{
    internal sealed class DescriptionForm : Form
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
        private static readonly Color DangerColor = Color.FromArgb(181, 57, 34);
        private static readonly Color DangerHover = Color.FromArgb(215, 60, 40);
        private static readonly Color TextDarkGreen = Color.FromArgb(41, 67, 32);

        private readonly CheckBox _chkAuto = new CheckBox();
        private readonly CheckBox _chkTemperature = new CheckBox();
        private readonly ComboBox _cmbTemperature = new ComboBox();
        private readonly TextBox _txtOther = new TextBox();
        private readonly Dictionary<string, CheckBox> _reasonChecks = new Dictionary<string, CheckBox>();
        private Point _dragStart;

        internal bool AutoEnabled => _chkAuto.Checked;
        internal string TemperaturePoint => _cmbTemperature.Text.Trim();
        internal string OtherText => _txtOther.Text.Trim();
        internal IReadOnlyCollection<string> SelectedReasons => _reasonChecks
            .Where(pair => pair.Value.Checked)
            .Select(pair => pair.Key)
            .ToArray();

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
            catch { }
        }

        internal DescriptionForm(
            bool autoEnabled,
            IEnumerable<string> selectedReasons,
            string temperaturePoint,
            string otherText,
            bool requireReason)
        {
            Text = "แจ้งหมายเหตุ";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 590);
            BackColor = SurfaceGreen;
            Font = new Font("Tahoma", 10.5F);
            ShowInTaskbar = false;

            Panel header = BuildHeader();
            Controls.Add(header);

            var body = new Panel
            {
                Location = new Point(18, 62),
                Size = new Size(484, 510),
                BackColor = CardBg
            };
            ApplyRoundedRegion(body, 8);
            Controls.Add(body);

            _chkAuto.Text = "แจ้งหมายเหตุ Auto";
            _chkAuto.AutoSize = true;
            _chkAuto.Font = new Font("Tahoma", 10.5F, FontStyle.Bold);
            _chkAuto.ForeColor = Color.FromArgb(30, 75, 140);
            _chkAuto.Location = new Point(16, 14);
            _chkAuto.Checked = autoEnabled;
            _chkAuto.CheckedChanged += (_, _) => UpdateManualControlsEnabled();
            body.Controls.Add(_chkAuto);

            var instruction = new Label
            {
                Text = "กรุณาเลือกจุดที่ทำให้ทดสอบไม่ผ่าน/ไม่ทำงาน",
                ForeColor = DangerColor,
                Font = new Font("Tahoma", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(16, 48)
            };
            body.Controls.Add(instruction);

            _chkTemperature.Text = "ทดสอบอุณหภูมิที่จุด";
            _chkTemperature.AutoSize = true;
            _chkTemperature.Font = new Font("Tahoma", 10F);
            _chkTemperature.ForeColor = TextDarkGreen;
            _chkTemperature.Location = new Point(16, 82);
            body.Controls.Add(_chkTemperature);

            _cmbTemperature.Location = new Point(190, 78);
            _cmbTemperature.Size = new Size(270, 30);
            _cmbTemperature.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbTemperature.IntegralHeight = false;
            _cmbTemperature.DropDownHeight = 220;
            _cmbTemperature.Font = new Font("Tahoma", 10F);
            _cmbTemperature.Items.AddRange(new object[]
            {
                "200 Ohm",
                "2000 Ohm",
                "8000 Ohm",
                "20 °C",
                "30 °C",
                "40 °C",
                "50 °C"
            });

            int savedTemperatureIndex = _cmbTemperature.Items.Cast<string>()
                .Select((value, index) => new { value, index })
                .Where(item => string.Equals(
                    item.value,
                    temperaturePoint,
                    StringComparison.OrdinalIgnoreCase))
                .Select(item => item.index)
                .DefaultIfEmpty(-1)
                .First();
            _cmbTemperature.SelectedIndex = savedTemperatureIndex;
            body.Controls.Add(_cmbTemperature);

            AddReason(body, "WL", "WL", 16, 128);
            AddReason(body, "HP", "HP", 170, 128);
            AddReason(body, "Alarm1", "Alarm1", 16, 170);
            AddReason(body, "Alarm2", "Alarm2", 170, 170);
            AddReason(body, "CoolFan", "CoolFan", 16, 212);
            AddReason(body, "HotFan", "HotFan", 170, 212);
            AddReason(body, "Compressor", "Compressor", 310, 212);
            AddReason(body, "Register", "เขียน register (พารามิเตอร์) ไม่สำเร็จ", 16, 256);

            var otherLabel = new Label
            {
                Text = "อื่นๆ",
                AutoSize = true,
                Font = new Font("Tahoma", 10F, FontStyle.Bold),
                ForeColor = TextDarkGreen,
                Location = new Point(16, 298)
            };
            body.Controls.Add(otherLabel);

            _txtOther.Location = new Point(16, 326);
            _txtOther.Size = new Size(448, 30);
            _txtOther.Text = otherText;
            _txtOther.Font = new Font("Tahoma", 10F);
            body.Controls.Add(_txtOther);

            var save = new Button
            {
                Text = "✔  บันทึก (Save)",
                BackColor = PrimaryGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Tahoma", 11.5F, FontStyle.Bold),
                Location = new Point(16, 444),
                Size = new Size(448, 46),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            save.FlatAppearance.BorderSize = 0;
            ApplyRoundedRegion(save, 6);
            save.Click += (_, _) =>
            {
                bool hasReason = _chkTemperature.Checked
                    || _reasonChecks.Values.Any(check => check.Checked)
                    || !string.IsNullOrWhiteSpace(_txtOther.Text);

                if (requireReason && !_chkAuto.Checked && !hasReason)
                {
                    MessageBox.Show(this,
                        "กรุณาเลือกหรือระบุสาเหตุอย่างน้อย 1 รายการ",
                        "แจ้งหมายเหตุ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            };
            body.Controls.Add(save);

            HashSet<string> selected = new HashSet<string>(selectedReasons, StringComparer.OrdinalIgnoreCase);
            foreach (var pair in _reasonChecks)
                pair.Value.Checked = selected.Contains(pair.Key);
            _chkTemperature.Checked = selected.Contains("Temperature");

            UpdateManualControlsEnabled();
        }

        protected override void OnShown(System.EventArgs e)
        {
            base.OnShown(e);
            ApplyTahomaFont(this);
            ApplyRoundedRegion(this, 5);
            ApplyRoundedButtons(this);
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundedRegion(this, 5);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle borderBounds = new Rectangle(1, 1, Width - 3, Height - 3);
            using (GraphicsPath borderPath = CreateRoundedPath(borderBounds, 6))
            using (var borderPen = new Pen(Color.FromArgb(20, 55, 30), 2.5F))
                e.Graphics.DrawPath(borderPen, borderPath);
        }

        private static void ApplyTahomaFont(Control control)
        {
            Font currentFont = control.Font;
            control.Font = new Font(
                "Tahoma",
                currentFont.Size,
                currentFont.Style,
                currentFont.Unit,
                currentFont.GdiCharSet,
                currentFont.GdiVerticalFont);

            foreach (Control child in control.Controls)
                ApplyTahomaFont(child);
        }

        private static void ApplyRoundedButtons(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is Button button)
                {
                    ApplyRoundedRegion(button, 5);
                    button.SizeChanged += (_, _) => ApplyRoundedRegion(button, 5);
                }

                if (child.HasChildren)
                    ApplyRoundedButtons(child);
            }
        }

        private static void ApplyRoundedRegion(Control control, int cornerRadius)
        {
            if (control.Width <= 0 || control.Height <= 0)
                return;

            int diameter = System.Math.Min(
                cornerRadius * 2,
                System.Math.Min(control.Width, control.Height));

            if (diameter <= 0)
                return;

            int right = control.Width - 1;
            int bottom = control.Height - 1;

            using (var path = new GraphicsPath())
            {
                path.AddArc(0, 0, diameter, diameter, 180, 90);
                path.AddArc(right - diameter, 0, diameter, diameter, 270, 90);
                path.AddArc(right - diameter, bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(0, bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();

                Region? previousRegion = control.Region;
                control.Region = new Region(path);
                previousRegion?.Dispose();
            }
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int cornerRadius)
        {
            int diameter = System.Math.Min(
                cornerRadius * 2,
                System.Math.Min(bounds.Width, bounds.Height));

            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        internal IEnumerable<string> GetAllSelectedReasons()
        {
            if (_chkTemperature.Checked)
            {
                string detail = TemperaturePoint;
                yield return string.IsNullOrWhiteSpace(detail)
                    ? "Temperature"
                    : $"Temperature ({detail})";
            }

            foreach (string reason in SelectedReasons)
                yield return reason;
        }

        private Panel BuildHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = HeaderDarkGreen,
                Cursor = Cursors.SizeAll
            };

            var title = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Tahoma", 12.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "📝  แจ้งหมายเหตุ (Test Remarks)",
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
                DialogResult = DialogResult.Cancel;
                Close();
            };

            header.MouseDown += HeaderMouseDown;
            header.MouseMove += HeaderMouseMove;
            title.MouseDown += HeaderMouseDown;
            title.MouseMove += HeaderMouseMove;

            header.Controls.Add(title);
            header.Controls.Add(closeButton);
            closeButton.BringToFront();
            return header;
        }

        private void AddReason(Control parent, string key, string text, int x, int y)
        {
            var check = new CheckBox
            {
                Text = text,
                AutoSize = true,
                Location = new Point(x, y)
            };
            _reasonChecks[key] = check;
            parent.Controls.Add(check);
        }

        private void UpdateManualControlsEnabled()
        {
            bool enabled = !_chkAuto.Checked;
            _chkTemperature.Enabled = enabled;
            _cmbTemperature.Enabled = enabled;
            _txtOther.Enabled = enabled;
            foreach (CheckBox check in _reasonChecks.Values)
                check.Enabled = enabled;
        }

        private void HeaderMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _dragStart = e.Location;
        }

        private void HeaderMouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            Point screenPoint = ((Control)sender!).PointToScreen(e.Location);
            Location = new Point(screenPoint.X - _dragStart.X, screenPoint.Y - _dragStart.Y);
        }
    }
}
