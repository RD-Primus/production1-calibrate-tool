using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO.Ports;
using System.Windows.Forms;

namespace PH_08_Test
{
    public class FormSerialSettingsPopup : Form
    {
        private readonly string _targetName;

        // UI Controls
        private Button btnClose = null!;
        private CustomComboBox cmbDataBits = null!;
        private CustomComboBox cmbStopBits = null!;
        private CustomComboBox cmbParity = null!;
        private Button btnReset = null!;
        private Button btnCancel = null!;
        private CustomRoundedButton btnOk = null!;

        // Output Properties
        public int SelectedDataBits { get; private set; } = 8;
        public StopBits SelectedStopBits { get; private set; } = StopBits.One;
        public Parity SelectedParity { get; private set; } = Parity.None;

        protected override CreateParams CreateParams => FormDropShadowHelper.ApplyCreateParams(base.CreateParams);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            FormDropShadowHelper.ApplyDropShadow(this);
        }

        public FormSerialSettingsPopup(string targetName, int currentDataBits, StopBits currentStopBits, Parity currentParity)
        {
            _targetName = targetName;
            SelectedDataBits = currentDataBits;
            SelectedStopBits = currentStopBits;
            SelectedParity = currentParity;

            InitializeComponentCustom();
            LoadInitialValues();
        }

        private void InitializeComponentCustom()
        {
            this.Text = $"ตั้งค่าเพิ่มเติม · {_targetName}";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(360, 275);
            this.BackColor = Color.White;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false;

            // Form Dragging support
            this.MouseDown += Form_MouseDown;

            // 1. Close Button (✕) Top-Right
            btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(248, 250, 252),
                Size = new Size(32, 28),
                Location = new Point(this.Width - 42, 8),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 226, 226);
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(239, 68, 68);
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(239, 68, 68);
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(100, 116, 139);
            btnClose.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            this.Controls.Add(btnClose);

            // 2. Data bits (Row 1 Left)
            Label lblDataBits = new Label
            {
                Text = "Data bits",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(24, 58),
                Size = new Size(140, 20),
                BackColor = Color.Transparent
            };
            this.Controls.Add(lblDataBits);

            cmbDataBits = new CustomComboBox
            {
                Location = new Point(24, 80),
                Size = new Size(145, 34),
                BorderRadius = 6,
                BorderColor = Color.FromArgb(203, 213, 225),
                BorderFocusColor = Color.FromArgb(27, 94, 32),
                ItemHoverColor = Color.FromArgb(232, 245, 233),
                ForeColor = Color.FromArgb(30, 41, 59),
                ItemHoverForeColor = Color.FromArgb(20, 20, 20)
            };
            cmbDataBits.Items.AddRange(new object[] { "7", "8" });
            this.Controls.Add(cmbDataBits);

            // 3. Stop bits (Row 1 Right)
            Label lblStopBits = new Label
            {
                Text = "Stop bits",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(190, 58),
                Size = new Size(140, 20),
                BackColor = Color.Transparent
            };
            this.Controls.Add(lblStopBits);

            cmbStopBits = new CustomComboBox
            {
                Location = new Point(190, 80),
                Size = new Size(145, 34),
                BorderRadius = 6,
                BorderColor = Color.FromArgb(203, 213, 225),
                BorderFocusColor = Color.FromArgb(27, 94, 32),
                ItemHoverColor = Color.FromArgb(232, 245, 233),
                ForeColor = Color.FromArgb(30, 41, 59),
                ItemHoverForeColor = Color.FromArgb(20, 20, 20)
            };
            cmbStopBits.Items.AddRange(new object[] { "1", "2" });
            this.Controls.Add(cmbStopBits);

            // 4. Parity (Row 2 Full Width)
            Label lblParity = new Label
            {
                Text = "Parity",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(24, 126),
                Size = new Size(310, 20),
                BackColor = Color.Transparent
            };
            this.Controls.Add(lblParity);

            cmbParity = new CustomComboBox
            {
                Location = new Point(24, 148),
                Size = new Size(311, 34),
                BorderRadius = 6,
                BorderColor = Color.FromArgb(203, 213, 225),
                BorderFocusColor = Color.FromArgb(27, 94, 32),
                ItemHoverColor = Color.FromArgb(232, 245, 233),
                ForeColor = Color.FromArgb(30, 41, 59),
                ItemHoverForeColor = Color.FromArgb(20, 20, 20)
            };
            cmbParity.Items.AddRange(new object[] { "None", "Even", "Odd" });
            this.Controls.Add(cmbParity);

            // 5. Footer Buttons
            // [ คืนค่าเดิม ]
            btnReset = new Button
            {
                Text = "คืนค่าเดิม",
                Font = new Font("Leelawadee UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.Transparent,
                Location = new Point(20, 215),
                Size = new Size(85, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            btnReset.Click += (s, e) =>
            {
                cmbDataBits.SelectedItem = "8";
                cmbStopBits.SelectedItem = "1";
                cmbParity.SelectedItem = "None";
            };
            this.Controls.Add(btnReset);

            // [ ยกเลิก ]
            btnCancel = new Button
            {
                Text = "ยกเลิก",
                Font = new Font("Leelawadee UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                BackColor = Color.White,
                Location = new Point(160, 215),
                Size = new Size(80, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 250, 252);
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            this.Controls.Add(btnCancel);

            // [ ตกลง ]
            btnOk = new CustomRoundedButton
            {
                Text = "ตกลง",
                Font = new Font("Leelawadee UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(27, 94, 32),
                BorderColor = Color.FromArgb(27, 94, 32),
                BorderRadius = 6,
                BorderThickness = 1,
                Location = new Point(250, 215),
                Size = new Size(85, 36),
                Cursor = Cursors.Hand
            };
            btnOk.Click += (s, e) =>
            {
                SelectedDataBits = int.TryParse(cmbDataBits.SelectedItem?.ToString(), out int d) ? d : 8;

                SelectedStopBits = cmbStopBits.SelectedItem?.ToString() switch
                {
                    "2" => StopBits.Two,
                    _ => StopBits.One
                };

                SelectedParity = cmbParity.SelectedItem?.ToString() switch
                {
                    "Even" => Parity.Even,
                    "Odd" => Parity.Odd,
                    _ => Parity.None
                };

                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnOk);
        }

        private void LoadInitialValues()
        {
            cmbDataBits.SelectedItem = SelectedDataBits.ToString();
            cmbStopBits.SelectedItem = SelectedStopBits == StopBits.Two ? "2" : "1";
            cmbParity.SelectedItem = SelectedParity.ToString();

            if (cmbDataBits.SelectedIndex < 0) cmbDataBits.SelectedItem = "8";
            if (cmbStopBits.SelectedIndex < 0) cmbStopBits.SelectedItem = "1";
            if (cmbParity.SelectedIndex < 0) cmbParity.SelectedItem = "None";
        }

        public void SetPositionBelow(Control anchorControl)
        {
            Point screenPt = anchorControl.PointToScreen(Point.Empty);
            int targetX = screenPt.X + anchorControl.Width - this.Width;
            int targetY = screenPt.Y + anchorControl.Height + 6;

            Rectangle workingArea = Screen.FromControl(anchorControl).WorkingArea;
            if (targetX < workingArea.Left + 10) targetX = screenPt.X;
            if (targetX + this.Width > workingArea.Right) targetX = workingArea.Right - this.Width - 10;
            if (targetY + this.Height > workingArea.Bottom) targetY = screenPt.Y - this.Height - 6;

            this.Location = new Point(targetX, targetY);
        }

        private void Form_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y <= 44)
            {
                this.Capture = false;
                Message msg = Message.Create(this.Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
                this.DefWndProc(ref msg);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Form Background & Elevated Border with Header
            using (var bgBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(bgBrush, this.ClientRectangle);
            }
            FormDropShadowHelper.DrawElevatedBorder(this, g, borderWidth: 1.5f, headerHeight: 44);

            // Header Title: ตั้งค่าเพิ่มเติม · {Target}
            using (var titleFont = new Font("Leelawadee UI", 11F, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(27, 94, 32)))
            {
                g.DrawString($"ตั้งค่าเพิ่มเติม · {_targetName}", titleFont, titleBrush, 16, 11);
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
