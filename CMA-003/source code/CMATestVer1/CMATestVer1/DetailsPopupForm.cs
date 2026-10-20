using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CMATestVer1
{
    internal sealed class DetailsPopupForm : Form
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
        private static readonly Color TextColor = Color.FromArgb(41, 67, 32);

        private readonly Panel _contentPanel = new Panel();
        private readonly List<Button> _tabButtons = new List<Button>();
        private readonly List<Panel> _pages = new List<Panel>();
        private readonly List<(TextBox Name, TextBox Series, TextBox EquipmentId, TextBox Expiration)> _equipmentEditors = new();
        private readonly TextBox _mainFirmwareEditor = CreateEditor("", "กรอก Firmware (Air Control)");
        private readonly TextBox _displayFirmwareEditor = CreateEditor("", "กรอก Firmware (Display)");
        private readonly TextBox _revisionEditor = CreateEditor("", "กรอก Revision");
        private bool _isDragging;
        private Point _dragStartCursor;
        private Point _dragStartForm;

        internal List<EquipmentRecord> EquipmentRecords { get; private set; }
        internal string MainFirmware { get; private set; }
        internal string DisplayFirmware { get; private set; }
        internal string Revision { get; private set; }

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

        internal DetailsPopupForm(
            IEnumerable<EquipmentRecord> equipmentRecords,
            string mainFirmware,
            string displayFirmware,
            string revision)
        {
            EquipmentRecords = equipmentRecords.Select(item => item.Clone()).ToList();
            if (EquipmentRecords.Count != 4)
                EquipmentRecords = EquipmentRecord.CreateDefaults();

            MainFirmware = mainFirmware;
            DisplayFirmware = displayFirmware;
            Revision = revision;

            InitializeDetailsPopup();
        }

        protected override void OnShown(System.EventArgs e)
        {
            base.OnShown(e);
            ApplyTahomaFont(this);
            ApplyRoundedRegion(this, 8);
            ApplyRoundedButtons(this);
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundedRegion(this, 8);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle borderBounds = new Rectangle(1, 1, Width - 3, Height - 3);
            using (GraphicsPath borderPath = CreateRoundedPath(borderBounds, 8))
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
                    ApplyRoundedRegion(button, 6);
                    button.SizeChanged += (_, _) => ApplyRoundedRegion(button, 6);
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

        private void InitializeDetailsPopup()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = SurfaceGreen;
            ClientSize = new Size(840, 565);
            Font = new Font("Tahoma", 10F, FontStyle.Regular, GraphicsUnit.Point);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "รายละเอียด";

            Controls.Add(BuildHeader());
            Controls.Add(BuildTabBar());

            _contentPanel.Location = new Point(18, 108);
            _contentPanel.Size = new Size(804, 388);
            _contentPanel.BackColor = CardBg;
            ApplyRoundedRegion(_contentPanel, 8);
            Controls.Add(_contentPanel);

            _pages.Add(BuildEquipmentPage());
            _pages.Add(BuildFirmwarePage());
            _pages.Add(BuildRevisionPage());
            foreach (Panel page in _pages)
                _contentPanel.Controls.Add(page);

            var saveButton = new Button
            {
                BackColor = PrimaryGreen,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Tahoma", 11.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(18, 508),
                Size = new Size(804, 44),
                Text = "✔  บันทึก (Save)",
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            saveButton.FlatAppearance.BorderSize = 0;
            ApplyRoundedRegion(saveButton, 6);
            saveButton.Click += SaveButton_Click;
            Controls.Add(saveButton);

            ShowPage(0);
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
                Text = "⚙  รายละเอียด (Equipment & Firmware Details)",
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

        private Panel BuildTabBar()
        {
            var tabBar = new Panel
            {
                BackColor = SurfaceGreen,
                Location = new Point(18, 56),
                Size = new Size(804, 42)
            };

            string[] captions = { "อุปกรณ์ที่ใช้ทดสอบ", "Firmware", "Revision" };
            int tabGap = 8;
            int totalTabs = captions.Length;
            int tabWidth = (tabBar.Width - ((totalTabs - 1) * tabGap)) / totalTabs;

            for (int index = 0; index < captions.Length; index++)
            {
                int pageIndex = index;
                var button = new Button
                {
                    BackColor = InactiveTabBg,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Tahoma", 10F, FontStyle.Bold),
                    ForeColor = TextColor,
                    Location = new Point(index * (tabWidth + tabGap), 2),
                    Size = new Size(tabWidth, 38),
                    Text = captions[index],
                    Cursor = Cursors.Hand,
                    UseVisualStyleBackColor = false
                };
                button.FlatAppearance.BorderSize = 0;
                ApplyRoundedRegion(button, 6);
                button.Click += (_, _) => ShowPage(pageIndex);
                _tabButtons.Add(button);
                tabBar.Controls.Add(button);
            }

            return tabBar;
        }

        private Panel BuildEquipmentPage()
        {
            var page = CreatePage();
            page.Controls.Add(CreateSectionLabel("ข้อมูลอุปกรณ์เพิ่มเติม", 8));

            string[] sectionTitles =
            {
                "1. อุปกรณ์จำลองค่าความต้านทาน",
                "2. อุปกรณ์สั่งการทำงาน WL/HP",
                "3. อุปกรณ์เก็บผล Relay Output",
                "4. อุปกรณ์ตัดต่อไฟ Relay"
            };

            for (int index = 0; index < EquipmentRecords.Count; index++)
                AddEquipmentRow(page, index, sectionTitles[index], 42 + (index * 82));

            return page;
        }

        private void AddEquipmentRow(Panel page, int index, string title, int top)
        {
            EquipmentRecord item = EquipmentRecords[index];
            page.Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(8, top),
                Text = title
            });

            var row = new TableLayoutPanel
            {
                ColumnCount = 8,
                Location = new Point(8, top + 27),
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                RowCount = 1,
                Size = new Size(776, 30)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 176F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 106F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            TextBox name = CreateEditor(item.Name, "ชื่ออุปกรณ์");
            TextBox series = CreateEditor(item.Series, "Series");
            TextBox equipmentId = CreateEditor(item.EquipmentId, "Equipment ID");
            TextBox expiration = CreateEditor(item.Expiration, "Exp. Date");

            row.Controls.Add(CreateFieldLabel("Name:"), 0, 0);
            row.Controls.Add(name, 1, 0);
            row.Controls.Add(CreateFieldLabel("Series:"), 2, 0);
            row.Controls.Add(series, 3, 0);
            row.Controls.Add(CreateFieldLabel("Equipment ID:"), 4, 0);
            row.Controls.Add(equipmentId, 5, 0);
            row.Controls.Add(CreateFieldLabel("Exp:"), 6, 0);
            row.Controls.Add(expiration, 7, 0);
            page.Controls.Add(row);

            _equipmentEditors.Add((name, series, equipmentId, expiration));
        }

        private Panel BuildFirmwarePage()
        {
            var page = CreatePage();
            page.Controls.Add(CreateSectionLabel("Firmware ที่ใช้งาน", 12));

            _mainFirmwareEditor.Text = MainFirmware;
            _displayFirmwareEditor.Text = DisplayFirmware;
            AddWideEditorRow(page, "Air Control:", _mainFirmwareEditor, 82);
            AddWideEditorRow(page, "Display:", _displayFirmwareEditor, 142);
            return page;
        }

        private Panel BuildRevisionPage()
        {
            var page = CreatePage();
            page.Controls.Add(CreateSectionLabel("การเปลี่ยนแปลงแบบร่าง (Revision)", 12));

            _revisionEditor.Text = Revision;
            AddWideEditorRow(page, "Revision:", _revisionEditor, 92);
            return page;
        }

        private static Panel CreatePage()
        {
            return new Panel
            {
                BackColor = CardBg,
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(8)
            };
        }

        private static Label CreateSectionLabel(string text, int top)
        {
            return new Label
            {
                AutoSize = true,
                Font = new Font("Tahoma", 10.5F, FontStyle.Bold),
                ForeColor = PrimaryGreen,
                Location = new Point(8, top),
                Text = text
            };
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Tahoma", 9.5F),
                ForeColor = TextColor,
                Margin = Padding.Empty,
                Padding = new Padding(0, 0, 6, 0),
                Text = text,
                TextAlign = ContentAlignment.MiddleRight
            };
        }

        private static TextBox CreateEditor(string value, string placeholder = "")
        {
            return new TextBox
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Font = new Font("Tahoma", 9.5F),
                Margin = new Padding(2, 1, 7, 1),
                PlaceholderText = placeholder,
                Text = value
            };
        }

        private static void AddWideEditorRow(Panel page, string label, TextBox editor, int top)
        {
            page.Controls.Add(new Label
            {
                AutoSize = false,
                Font = new Font("Tahoma", 10F, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(30, top),
                Size = new Size(120, 30),
                Text = label,
                TextAlign = ContentAlignment.MiddleLeft
            });
            editor.Location = new Point(160, top);
            editor.Size = new Size(590, 30);
            editor.Dock = DockStyle.None;
            page.Controls.Add(editor);
        }

        private void ShowPage(int selectedIndex)
        {
            for (int index = 0; index < _pages.Count; index++)
            {
                bool selected = index == selectedIndex;
                _pages[index].Visible = selected;
                if (selected)
                    _pages[index].BringToFront();

                _tabButtons[index].BackColor = selected ? PrimaryGreen : InactiveTabBg;
                _tabButtons[index].ForeColor = selected ? Color.White : TextColor;
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            for (int index = 0; index < _equipmentEditors.Count; index++)
            {
                var editor = _equipmentEditors[index];
                if (string.IsNullOrWhiteSpace(editor.Name.Text)
                    || string.IsNullOrWhiteSpace(editor.Series.Text)
                    || string.IsNullOrWhiteSpace(editor.EquipmentId.Text)
                    || string.IsNullOrWhiteSpace(editor.Expiration.Text))
                {
                    ShowPage(0);
                    MessageBox.Show(this,
                        $"กรุณากรอกข้อมูลอุปกรณ์รายการที่ {index + 1} ให้ครบ (หากไม่มี Exp. ให้ใส่ -)",
                        "ข้อมูลไม่ครบ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(_mainFirmwareEditor.Text)
                || string.IsNullOrWhiteSpace(_displayFirmwareEditor.Text))
            {
                ShowPage(1);
                MessageBox.Show(this, "กรุณากรอก Firmware ให้ครบ", "ข้อมูลไม่ครบ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_revisionEditor.Text))
            {
                ShowPage(2);
                MessageBox.Show(this, "กรุณากรอก Revision", "ข้อมูลไม่ครบ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EquipmentRecords = _equipmentEditors.Select(editor => new EquipmentRecord
            {
                Name = editor.Name.Text.Trim(),
                Series = editor.Series.Text.Trim(),
                EquipmentId = editor.EquipmentId.Text.Trim(),
                Expiration = editor.Expiration.Text.Trim()
            }).ToList();
            MainFirmware = _mainFirmwareEditor.Text.Trim();
            DisplayFirmware = _displayFirmwareEditor.Text.Trim();
            Revision = _revisionEditor.Text.Trim();

            DialogResult = DialogResult.OK;
            Close();
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
            if (!_isDragging || e.Button != MouseButtons.Left)
                return;

            Point currentCursor = Cursor.Position;
            Location = new Point(
                _dragStartForm.X + currentCursor.X - _dragStartCursor.X,
                _dragStartForm.Y + currentCursor.Y - _dragStartCursor.Y);
        }

        private void Header_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _isDragging = false;
        }
    }
}
