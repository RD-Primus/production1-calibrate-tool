using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TEST_DEF_03N
{
    internal sealed class DescriptionForm : Form
    {
        private static readonly Color BackgroundColor = Color.FromArgb(7, 23, 35);
        private static readonly Color HeaderColor = Color.FromArgb(12, 29, 46);
        private static readonly Color CardColor = Color.FromArgb(20, 40, 55);
        private static readonly Color AccentColor = Color.FromArgb(73, 218, 242);
        private static readonly Color MutedColor = Color.FromArgb(166, 185, 199);
        private static readonly Color DangerColor = Color.FromArgb(182, 56, 33);
        private static readonly Color WarningColor = Color.FromArgb(255, 120, 120);
        private static readonly Color InputBackColor = Color.FromArgb(12, 29, 46);

        private static readonly Dictionary<string, string[]> SensorTempMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "PTC 2K", new[] { "-40°C", "40°C", "130°C" } },
            { "NTC 10K (01)", new[] { "-40°C", "100°C", "120°C" } },
            { "NTC 2K", new[] { "-20°C", "40°C", "120°C" } },
            { "NTC 10K (03)", new[] { "-40°C", "60°C", "120°C" } }
        };

        private readonly CheckBox _chkAuto = new CheckBox();
        private readonly CheckBox _chkTemperature = new CheckBox();
        private readonly ComboBox _cmbSensorType = new ComboBox();
        private readonly ComboBox _cmbTemperature = new ComboBox();
        private readonly Label _lblSensorType = new Label();
        private readonly Label _lblTemperature = new Label();
        private readonly TextBox _txtOther = new TextBox();
        private readonly Dictionary<string, CheckBox> _reasonChecks = new Dictionary<string, CheckBox>();
        private Point _dragStart;

        internal bool AutoEnabled => _chkAuto.Checked;
        internal string SensorType => _cmbSensorType.Text.Trim();
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

        internal DescriptionForm(
            bool autoEnabled,
            IEnumerable<string> selectedReasons,
            string savedSensorType,
            string savedTemperaturePoint,
            string otherText,
            bool requireReason)
        {
            Text = "แจ้งหมายเหตุ";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(600, 440);
            BackColor = BackgroundColor;
            Font = new Font("Segoe UI", 9.5F);
            ForeColor = Color.White;
            ShowInTaskbar = false;

            Panel header = BuildHeader();
            Controls.Add(header);

            var body = new Panel
            {
                Location = new Point(25, 58),
                Size = new Size(550, 360),
                BackColor = BackgroundColor
            };
            Controls.Add(body);

            // 1. Checkbox แจ้งหมายเหตุ Auto
            _chkAuto.Text = "แจ้งหมายเหตุ Auto";
            _chkAuto.AutoSize = true;
            _chkAuto.ForeColor = AccentColor;
            _chkAuto.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            _chkAuto.Location = new Point(0, 0);
            _chkAuto.Checked = autoEnabled;
            _chkAuto.CheckedChanged += (_, _) => UpdateManualControlsEnabled();
            body.Controls.Add(_chkAuto);

            // 2. คำแนะนำสีแดง
            var instruction = new Label
            {
                Text = "กรุณาเลือกจุดที่ทำให้ทดสอบไม่ผ่าน/ไม่ทำงาน",
                ForeColor = WarningColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(0, 26)
            };
            body.Controls.Add(instruction);

            // 3. แถวทดสอบอุณหภูมิที่จุด + 2 ComboBoxes พร้อมป้ายกำกับด้านบนที่มีระยะห่างชัดเจน
            _chkTemperature.Text = "ทดสอบอุณหภูมิที่จุด";
            _chkTemperature.AutoSize = true;
            _chkTemperature.ForeColor = Color.White;
            _chkTemperature.Location = new Point(0, 76);
            _chkTemperature.CheckedChanged += (_, _) => UpdateTemperatureInputsEnabled();
            body.Controls.Add(_chkTemperature);

            // Label & ComboBox 1: ชนิดเซนเซอร์ (Sensor Type)
            _lblSensorType.Text = "ชนิดเซนเซอร์";
            _lblSensorType.ForeColor = MutedColor;
            _lblSensorType.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            _lblSensorType.AutoSize = true;
            _lblSensorType.Location = new Point(195, 52);
            body.Controls.Add(_lblSensorType);

            _cmbSensorType.Location = new Point(195, 74);
            _cmbSensorType.Size = new Size(165, 28);
            _cmbSensorType.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbSensorType.BackColor = InputBackColor;
            _cmbSensorType.ForeColor = Color.White;
            _cmbSensorType.FlatStyle = FlatStyle.Flat;
            _cmbSensorType.Items.AddRange(SensorTempMap.Keys.Cast<object>().ToArray());
            _cmbSensorType.SelectedIndexChanged += (_, _) => OnSensorTypeChanged();
            body.Controls.Add(_cmbSensorType);

            // Label & ComboBox 2: จุดอุณหภูมิ (Temperature Point)
            _lblTemperature.Text = "จุดอุณหภูมิ";
            _lblTemperature.ForeColor = MutedColor;
            _lblTemperature.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            _lblTemperature.AutoSize = true;
            _lblTemperature.Location = new Point(375, 52);
            body.Controls.Add(_lblTemperature);

            _cmbTemperature.Location = new Point(375, 74);
            _cmbTemperature.Size = new Size(175, 28);
            _cmbTemperature.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbTemperature.BackColor = InputBackColor;
            _cmbTemperature.ForeColor = Color.White;
            _cmbTemperature.FlatStyle = FlatStyle.Flat;
            body.Controls.Add(_cmbTemperature);

            // Set initial sensor type and temp
            int initialSensorIndex = _cmbSensorType.Items.Cast<string>()
                .Select((value, index) => new { value, index })
                .Where(item => string.Equals(item.value, savedSensorType, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.index)
                .DefaultIfEmpty(0)
                .First();
            if (_cmbSensorType.Items.Count > 0)
            {
                _cmbSensorType.SelectedIndex = initialSensorIndex;
                PopulateTemperatureOptions(savedTemperaturePoint);
            }

            // 4. Checkbox รายการข้อบกพร่องอื่นๆ
            AddReason(body, "Output Relay", "Output Relay", 0, 118);
            AddReason(body, "Alarm Relay", "Alarm Relay", 0, 150);
            AddReason(body, "Register", "เขียน register (พารามิเตอร์) ไม่สำเร็จ", 0, 182);

            // 5. อื่นๆ
            var otherLabel = new Label
            {
                Text = "อื่นๆ",
                ForeColor = MutedColor,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 218)
            };
            body.Controls.Add(otherLabel);

            _txtOther.Location = new Point(0, 242);
            _txtOther.Size = new Size(550, 28);
            _txtOther.BackColor = InputBackColor;
            _txtOther.ForeColor = Color.White;
            _txtOther.BorderStyle = BorderStyle.FixedSingle;
            _txtOther.Text = otherText;
            body.Controls.Add(_txtOther);

            // 6. ปุ่ม OK
            var okButton = new Button
            {
                Text = "OK",
                BackColor = DangerColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Location = new Point(0, 290),
                Size = new Size(550, 44),
                Cursor = Cursors.Hand
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.Click += (_, _) =>
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
            ApplyRoundedRegion(okButton, 5);
            body.Controls.Add(okButton);

            // Sync checked items
            HashSet<string> selected = new HashSet<string>(selectedReasons, StringComparer.OrdinalIgnoreCase);
            foreach (var pair in _reasonChecks)
                pair.Value.Checked = selected.Contains(pair.Key);
            _chkTemperature.Checked = selected.Any(s => s.StartsWith("Temperature", StringComparison.OrdinalIgnoreCase));

            UpdateManualControlsEnabled();
        }

        private void OnSensorTypeChanged()
        {
            PopulateTemperatureOptions(null);
        }

        private void PopulateTemperatureOptions(string? selectTemp)
        {
            _cmbTemperature.Items.Clear();
            string selectedType = _cmbSensorType.Text.Trim();
            if (SensorTempMap.TryGetValue(selectedType, out var temps))
            {
                _cmbTemperature.Items.AddRange(temps.Cast<object>().ToArray());
            }

            if (_cmbTemperature.Items.Count > 0)
            {
                int targetIndex = 0;
                if (!string.IsNullOrWhiteSpace(selectTemp))
                {
                    int found = _cmbTemperature.Items.Cast<string>()
                        .Select((val, idx) => new { val, idx })
                        .Where(item => string.Equals(item.val, selectTemp, StringComparison.OrdinalIgnoreCase))
                        .Select(item => item.idx)
                        .DefaultIfEmpty(-1)
                        .First();
                    if (found >= 0) targetIndex = found;
                }
                _cmbTemperature.SelectedIndex = targetIndex;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyRoundedRegion(this, 6);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundedRegion(this, 6);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle borderBounds = new Rectangle(1, 1, Width - 3, Height - 3);
            using var borderPen = new Pen(Color.FromArgb(60, AccentColor), 1.5F);
            using var borderPath = CreateRoundedPath(borderBounds, 5);
            e.Graphics.DrawPath(borderPen, borderPath);
        }

        private static void ApplyRoundedRegion(Control control, int cornerRadius)
        {
            if (control.Width <= 0 || control.Height <= 0) return;
            int diameter = Math.Min(cornerRadius * 2, Math.Min(control.Width, control.Height));
            if (diameter <= 0) return;

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

        internal IEnumerable<string> GetAllSelectedReasons()
        {
            if (_chkTemperature.Checked)
            {
                string sensor = SensorType;
                string temp = TemperaturePoint;
                if (!string.IsNullOrWhiteSpace(sensor) && !string.IsNullOrWhiteSpace(temp))
                    yield return $"Temperature ({sensor} {temp})";
                else if (!string.IsNullOrWhiteSpace(sensor))
                    yield return $"Temperature ({sensor})";
                else if (!string.IsNullOrWhiteSpace(temp))
                    yield return $"Temperature ({temp})";
                else
                    yield return "Temperature";
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
                BackColor = HeaderColor,
                Cursor = Cursors.SizeAll
            };

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = HeaderColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                ColumnCount = 3,
                RowCount = 1,
                Cursor = Cursors.SizeAll
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45F));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var leftBalance = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = HeaderColor,
                Margin = Padding.Empty,
                Cursor = Cursors.SizeAll
            };

            var title = new Label
            {
                Text = "แจ้งหมายเหตุ",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Cursor = Cursors.SizeAll
            };

            var close = new Button
            {
                Text = "X",
                ForeColor = Color.White,
                BackColor = DangerColor,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            close.FlatAppearance.BorderSize = 0;
            close.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            headerLayout.Controls.Add(leftBalance, 0, 0);
            headerLayout.Controls.Add(title, 1, 0);
            headerLayout.Controls.Add(close, 2, 0);
            header.Controls.Add(headerLayout);

            header.MouseDown += HeaderMouseDown;
            header.MouseMove += HeaderMouseMove;
            headerLayout.MouseDown += HeaderMouseDown;
            headerLayout.MouseMove += HeaderMouseMove;
            leftBalance.MouseDown += HeaderMouseDown;
            leftBalance.MouseMove += HeaderMouseMove;
            title.MouseDown += HeaderMouseDown;
            title.MouseMove += HeaderMouseDown;

            return header;
        }

        private void AddReason(Control parent, string key, string text, int x, int y)
        {
            var check = new CheckBox
            {
                Text = text,
                AutoSize = true,
                ForeColor = Color.White,
                Location = new Point(x, y)
            };
            _reasonChecks[key] = check;
            parent.Controls.Add(check);
        }

        private void UpdateManualControlsEnabled()
        {
            bool manualEnabled = !_chkAuto.Checked;
            _chkTemperature.Enabled = manualEnabled;
            _txtOther.Enabled = manualEnabled;
            foreach (CheckBox check in _reasonChecks.Values)
                check.Enabled = manualEnabled;

            UpdateTemperatureInputsEnabled();
        }

        private void UpdateTemperatureInputsEnabled()
        {
            bool tempEnabled = !_chkAuto.Checked && _chkTemperature.Checked;
            _lblSensorType.Enabled = tempEnabled;
            _cmbSensorType.Enabled = tempEnabled;
            _lblTemperature.Enabled = tempEnabled;
            _cmbTemperature.Enabled = tempEnabled;
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
