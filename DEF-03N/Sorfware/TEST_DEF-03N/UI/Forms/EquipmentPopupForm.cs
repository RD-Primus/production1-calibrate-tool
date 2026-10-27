using System.Drawing;
using System.Windows.Forms;

namespace TEST_DEF_03N
{
    internal sealed class EquipmentPopupForm : Form
    {
        private static readonly Color BackgroundColor = Color.FromArgb(7, 23, 35);
        private static readonly Color HeaderColor = Color.FromArgb(12, 29, 46);
        private static readonly Color CardColor = Color.FromArgb(20, 40, 55);
        private static readonly Color AccentColor = Color.FromArgb(73, 218, 242);
        private static readonly Color MutedColor = Color.FromArgb(166, 185, 199);
        private static readonly Color DangerColor = Color.FromArgb(192, 0, 0);
        private static readonly Color SaveColor = Color.FromArgb(255, 217, 0);
        private static readonly Color TabInactiveColor = Color.FromArgb(30, 55, 75);
        private static readonly Color TabActiveColor = Color.FromArgb(73, 218, 242);

        private readonly List<(TextBox Name, TextBox Series, TextBox EquipmentId, TextBox ExpireDate)> _equipmentEditors = new();
        private TextBox _txtFirmware = null!;
        private TextBox _txtRevision = null!;

        private Panel _tabPanelEquipment = null!;
        private Panel _tabPanelFirmware = null!;
        private Panel _tabPanelRevision = null!;

        private Button _btnTabEquipment = null!;
        private Button _btnTabFirmware = null!;
        private Button _btnTabRevision = null!;

        private bool _isDragging;
        private Point _dragStartCursor;
        private Point _dragStartForm;

        internal List<EquipmentRecord> EquipmentRecords { get; private set; }
        internal string FirmwareVersion { get; private set; }
        internal string Revision { get; private set; }

        internal EquipmentPopupForm(
            IEnumerable<EquipmentRecord> equipmentRecords,
            string firmwareVersion,
            string revision)
        {
            EquipmentRecords = equipmentRecords.Select(item => item.Clone()).ToList();
            if (EquipmentRecords.Count != 2)
                EquipmentRecords = EquipmentRecord.CreateDefaults();

            FirmwareVersion = firmwareVersion ?? "";
            Revision = revision ?? "";

            InitializeEquipmentPopup();
        }

        private void InitializeEquipmentPopup()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = BackgroundColor;
            ClientSize = new Size(740, 470);
            Font = new Font("Segoe UI", 9.5F);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "รายละเอียด";

            // ── Top Header ──────────────────────────────────────────────────────────
            var header = new Panel
            {
                BackColor = HeaderColor,
                Dock = DockStyle.Top,
                Height = 56
            };

            var title = new Label
            {
                Bounds = new Rectangle(50, 0, 640, 56),
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = AccentColor,
                Text = "รายละเอียด",
                TextAlign = ContentAlignment.MiddleCenter
            };

            var closeButton = new Button
            {
                BackColor = DangerColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(690, 0),
                Size = new Size(50, 56),
                Text = "X",
                UseVisualStyleBackColor = false
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            foreach (Control dragControl in new Control[] { header, title })
            {
                dragControl.MouseDown += EquipmentHeader_MouseDown;
                dragControl.MouseMove += EquipmentHeader_MouseMove;
                dragControl.MouseUp += EquipmentHeader_MouseUp;
            }

            header.Controls.Add(title);
            header.Controls.Add(closeButton);
            Controls.Add(header);

            // ── Top Accent Line ─────────────────────────────────────────────────────
            Controls.Add(new Panel
            {
                BackColor = AccentColor,
                Location = new Point(28, 68),
                Size = new Size(684, 2)
            });

            // ── Tab Navigation Buttons ──────────────────────────────────────────────
            var tabNavPanel = new Panel
            {
                Location = new Point(28, 80),
                Size = new Size(684, 38)
            };

            int tabWidth = (684 - 8) / 3;
            _btnTabEquipment = CreateTabButton("อุปกรณ์ที่ใช้ทดสอบ", 0, tabWidth);
            _btnTabFirmware = CreateTabButton("Firmware", tabWidth + 4, tabWidth);
            _btnTabRevision = CreateTabButton("Revision", (tabWidth + 4) * 2, tabWidth);

            _btnTabEquipment.Click += (_, _) => SwitchTab(0);
            _btnTabFirmware.Click += (_, _) => SwitchTab(1);
            _btnTabRevision.Click += (_, _) => SwitchTab(2);

            tabNavPanel.Controls.Add(_btnTabEquipment);
            tabNavPanel.Controls.Add(_btnTabFirmware);
            tabNavPanel.Controls.Add(_btnTabRevision);
            Controls.Add(tabNavPanel);

            // ── Tab 1: Equipment Panel ──────────────────────────────────────────────
            _tabPanelEquipment = new Panel
            {
                Location = new Point(28, 126),
                Size = new Size(684, 268),
                BackColor = Color.Transparent
            };
            BuildEquipmentTabContent(_tabPanelEquipment);
            Controls.Add(_tabPanelEquipment);

            // ── Tab 2: Firmware Panel ───────────────────────────────────────────────
            _tabPanelFirmware = new Panel
            {
                Location = new Point(28, 126),
                Size = new Size(684, 268),
                BackColor = Color.Transparent,
                Visible = false
            };
            BuildFirmwareTabContent(_tabPanelFirmware);
            Controls.Add(_tabPanelFirmware);

            // ── Tab 3: Revision Panel ───────────────────────────────────────────────
            _tabPanelRevision = new Panel
            {
                Location = new Point(28, 126),
                Size = new Size(684, 268),
                BackColor = Color.Transparent,
                Visible = false
            };
            BuildRevisionTabContent(_tabPanelRevision);
            Controls.Add(_tabPanelRevision);

            // ── OK / Save Button ────────────────────────────────────────────────────
            var saveButton = new Button
            {
                BackColor = SaveColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.Black,
                Location = new Point(28, 408),
                Size = new Size(684, 44),
                Text = "OK",
                UseVisualStyleBackColor = false
            };
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.Click += SaveButton_Click;
            Controls.Add(saveButton);

            SwitchTab(0);
        }

        private Button CreateTabButton(string text, int left, int width) => new()
        {
            Bounds = new Rectangle(left, 0, width, 38),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = TabInactiveColor,
            Text = text,
            UseVisualStyleBackColor = false,
            FlatAppearance = { BorderSize = 0 }
        };

        private void SwitchTab(int tabIndex)
        {
            _tabPanelEquipment.Visible = (tabIndex == 0);
            _tabPanelFirmware.Visible = (tabIndex == 1);
            _tabPanelRevision.Visible = (tabIndex == 2);

            SetTabStyle(_btnTabEquipment, tabIndex == 0);
            SetTabStyle(_btnTabFirmware, tabIndex == 1);
            SetTabStyle(_btnTabRevision, tabIndex == 2);
        }

        private static void SetTabStyle(Button btn, bool isActive)
        {
            btn.BackColor = isActive ? TabActiveColor : TabInactiveColor;
            btn.ForeColor = isActive ? Color.Black : Color.White;
        }

        private void BuildEquipmentTabContent(Panel panel)
        {
            panel.Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(4, 6),
                Text = "ข้อมูลอุปกรณ์เพิ่มเติม"
            });

            string[] sectionTitles =
            {
                "1. อุปกรณ์จำลองค่าความต้านทาน",
                "2. อุปกรณ์เก็บผล Relay Output"
            };

            for (int index = 0; index < EquipmentRecords.Count; index++)
            {
                int top = 38 + (index * 110);
                EquipmentRecord equipment = EquipmentRecords[index];

                panel.Controls.Add(new Label
                {
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                    ForeColor = AccentColor,
                    Location = new Point(4, top),
                    Text = sectionTitles[index]
                });

                var card = new Panel
                {
                    BackColor = CardColor,
                    Location = new Point(0, top + 26),
                    Size = new Size(684, 52)
                };

                TextBox nameEditor = CreateEditor(equipment.Name, new Rectangle(58, 11, 140, 30));
                TextBox seriesEditor = CreateEditor(equipment.Series, new Rectangle(256, 11, 90, 30));
                TextBox idEditor = CreateEditor(equipment.EquipmentId, new Rectangle(450, 11, 100, 30));
                TextBox expEditor = CreateEditor(equipment.ExpireDate, new Rectangle(592, 11, 80, 30));

                card.Controls.Add(CreateFieldLabel("Name:", new Rectangle(10, 11, 46, 30)));
                card.Controls.Add(nameEditor);
                card.Controls.Add(CreateFieldLabel("series:", new Rectangle(206, 11, 48, 30)));
                card.Controls.Add(seriesEditor);
                card.Controls.Add(CreateFieldLabel("equiment_id:", new Rectangle(354, 11, 94, 30)));
                card.Controls.Add(idEditor);
                card.Controls.Add(CreateFieldLabel("Exp:", new Rectangle(558, 11, 32, 30)));
                card.Controls.Add(expEditor);

                panel.Controls.Add(card);
                _equipmentEditors.Add((nameEditor, seriesEditor, idEditor, expEditor));
            }
        }

        private void BuildFirmwareTabContent(Panel panel)
        {
            panel.Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(4, 6),
                Text = "firmware ที่ใช้งาน"
            });

            var card = new Panel
            {
                BackColor = CardColor,
                Location = new Point(0, 42),
                Size = new Size(684, 60)
            };

            _txtFirmware = CreateEditor(FirmwareVersion, new Rectangle(60, 15, 608, 30));
            card.Controls.Add(CreateFieldLabel("FW:", new Rectangle(16, 15, 40, 30)));
            card.Controls.Add(_txtFirmware);

            panel.Controls.Add(card);
        }

        private void BuildRevisionTabContent(Panel panel)
        {
            panel.Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(4, 6),
                Text = "การเปลี่ยนแปลงแบบร่าง (Revision)"
            });

            var card = new Panel
            {
                BackColor = CardColor,
                Location = new Point(0, 42),
                Size = new Size(684, 60)
            };

            _txtRevision = CreateEditor(Revision, new Rectangle(84, 15, 584, 30));
            card.Controls.Add(CreateFieldLabel("Revision:", new Rectangle(16, 15, 64, 30)));
            card.Controls.Add(_txtRevision);

            panel.Controls.Add(card);
        }

        private static Label CreateFieldLabel(string text, Rectangle bounds) => new()
        {
            Bounds = bounds,
            ForeColor = MutedColor,
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        private static TextBox CreateEditor(string value, Rectangle bounds) => new()
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Bounds = bounds,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.Black,
            Text = value
        };

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            for (int index = 0; index < _equipmentEditors.Count; index++)
            {
                var editor = _equipmentEditors[index];
                if (string.IsNullOrWhiteSpace(editor.Name.Text)
                    || string.IsNullOrWhiteSpace(editor.Series.Text)
                    || string.IsNullOrWhiteSpace(editor.EquipmentId.Text)
                    || string.IsNullOrWhiteSpace(editor.ExpireDate.Text))
                {
                    SwitchTab(0);
                    MessageBox.Show(this,
                        $"กรุณากรอกข้อมูลอุปกรณ์รายการที่ {index + 1} ให้ครบทุกช่อง (Name, series, equiment_id, Exp)",
                        "ข้อมูลไม่ครบ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (_txtFirmware != null && string.IsNullOrWhiteSpace(_txtFirmware.Text))
            {
                SwitchTab(1);
                MessageBox.Show(this,
                    "กรุณากรอกเวอร์ชัน Firmware",
                    "ข้อมูลไม่ครบ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtFirmware.Focus();
                return;
            }

            EquipmentRecords = _equipmentEditors.Select(editor => new EquipmentRecord
            {
                Name = editor.Name.Text.Trim(),
                Series = editor.Series.Text.Trim(),
                EquipmentId = editor.EquipmentId.Text.Trim(),
                ExpireDate = editor.ExpireDate.Text.Trim()
            }).ToList();

            FirmwareVersion = _txtFirmware?.Text.Trim() ?? "";
            Revision = _txtRevision?.Text.Trim() ?? "";

            DialogResult = DialogResult.OK;
            Close();
        }

        private void EquipmentHeader_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _isDragging = true;
            _dragStartCursor = Cursor.Position;
            _dragStartForm = Location;
        }

        private void EquipmentHeader_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isDragging || e.Button != MouseButtons.Left) return;
            Point cursor = Cursor.Position;
            Location = new Point(
                _dragStartForm.X + cursor.X - _dragStartCursor.X,
                _dragStartForm.Y + cursor.Y - _dragStartCursor.Y);
        }

        private void EquipmentHeader_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) _isDragging = false;
        }
    }
}
