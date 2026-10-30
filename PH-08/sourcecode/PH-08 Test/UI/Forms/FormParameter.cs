using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PH_08_Test
{
    public class FormParameter : Form
    {
        private readonly Form1 _mainForm;

        // Title Bar Controls
        private Button btnClose = null!;

        // Input Controls
        private Label lblDeviceAdr = null!;
        private TextBox txtDeviceAdr = null!;

        private Label lblModuleType = null!;
        private TextBox txtModuleType = null!;

        private Label lblRunFlag = null!;
        private TextBox txtRunFlag = null!;

        protected override CreateParams CreateParams => FormDropShadowHelper.ApplyCreateParams(base.CreateParams);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            FormDropShadowHelper.ApplyDropShadow(this);
        }

        public FormParameter(Form1 mainForm)
        {
            _mainForm = mainForm;
            InitializeComponentCustom();
            LoadCurrentValues();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "parameter";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(360, 275);
            this.BackColor = Color.White;
            this.DoubleBuffered = true;

            // Form Dragging
            this.MouseDown += Form_MouseDown;

            // --- Top-Right Close Button (Matching FormLEDTest & FormSwitchIDTest) ---
            btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(248, 250, 252),
                Size = new Size(32, 30),
                Location = new Point(320, 5),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 226, 226);
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(239, 68, 68);
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(239, 68, 68);
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(100, 116, 139);
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            // --- Form Inputs (3 Rows) ---
            int startY = 60;
            int rowSpacing = 44;
            int labelX = 24;
            int inputX = 160;
            int inputW = 165;
            int inputH = 34;

            // 1. Device Adr (Reg 1001)
            lblDeviceAdr = new Label
            {
                Text = "Device Adr:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85), // Slate 700
                Location = new Point(labelX, startY),
                Size = new Size(125, inputH),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };
            this.Controls.Add(lblDeviceAdr);

            txtDeviceAdr = CreateStyledTextBox(new Point(inputX, startY), new Size(inputW, inputH));
            txtDeviceAdr.KeyDown += TxtDeviceAdr_KeyDown;
            txtDeviceAdr.TextChanged += (s, e) => _mainForm.ParamDeviceAdr = txtDeviceAdr.Text.Trim();
            this.Controls.Add(txtDeviceAdr);

            // 2. Module Type (Reg 1002)
            lblModuleType = new Label
            {
                Text = "Module Type:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(labelX, startY + rowSpacing),
                Size = new Size(125, inputH),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };
            this.Controls.Add(lblModuleType);

            txtModuleType = CreateStyledTextBox(new Point(inputX, startY + rowSpacing), new Size(inputW, inputH));
            txtModuleType.KeyDown += TxtModuleType_KeyDown;
            txtModuleType.TextChanged += (s, e) => _mainForm.ParamModuleType = txtModuleType.Text.Trim();
            this.Controls.Add(txtModuleType);

            // 3. RunFlag (Reg 1)
            lblRunFlag = new Label
            {
                Text = "RunFlag:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(labelX, startY + rowSpacing * 2),
                Size = new Size(125, inputH),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };
            this.Controls.Add(lblRunFlag);

            txtRunFlag = CreateStyledTextBox(new Point(inputX, startY + rowSpacing * 2), new Size(inputW, inputH));
            txtRunFlag.KeyDown += TxtRunFlag_KeyDown;
            txtRunFlag.TextChanged += (s, e) => _mainForm.ParamRunFlag = txtRunFlag.Text.Trim();
            this.Controls.Add(txtRunFlag);

            // Footnote hint
            var lblHint = new Label
            {
                Text = "💡 กดปุ่ม Enter เพื่อส่งคำสั่งเขียนค่าไปยัง PH-08",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184), // Slate 400
                Location = new Point(24, 235),
                Size = new Size(312, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            this.Controls.Add(lblHint);
        }

        private TextBox CreateStyledTextBox(Point location, Size size)
        {
            var tb = new TextBox
            {
                Location = new Point(location.X, location.Y + 2),
                Size = new Size(size.Width, size.Height),
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            tb.Enter += (s, e) => { tb.BackColor = Color.White; };
            tb.Leave += (s, e) => { tb.BackColor = Color.FromArgb(248, 250, 252); };
            return tb;
        }

        public void LoadCurrentValues()
        {
            txtDeviceAdr.Text = _mainForm.ParamDeviceAdr;
            txtModuleType.Text = _mainForm.ParamModuleType;
            txtRunFlag.Text = _mainForm.ParamRunFlag;
        }

        public void UpdateLiveValues(string? deviceAdr, string? moduleType, string? runFlag)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            this.BeginInvoke(new Action(() =>
            {
                if (deviceAdr != null && !txtDeviceAdr.Focused) txtDeviceAdr.Text = deviceAdr;
                if (moduleType != null && !txtModuleType.Focused) txtModuleType.Text = moduleType;
                if (runFlag != null && !txtRunFlag.Focused) txtRunFlag.Text = runFlag;
            }));
        }

        private async void TxtDeviceAdr_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                this.ActiveControl = null;
                await _mainForm.WritePH08RegisterAsync(1001, txtDeviceAdr.Text.Trim(), txtDeviceAdr);
            }
        }

        private async void TxtModuleType_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                this.ActiveControl = null;
                await _mainForm.WritePH08RegisterAsync(1002, txtModuleType.Text.Trim(), txtModuleType);
            }
        }

        private async void TxtRunFlag_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                this.ActiveControl = null;
                await _mainForm.WritePH08RegisterAsync(1, txtRunFlag.Text.Trim(), txtRunFlag);
            }
        }

        private void Form_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y <= 40)
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
            FormDropShadowHelper.DrawElevatedBorder(this, g, borderWidth: 1.5f, headerHeight: 40);

            // 2. Title Bar Text ("parameter")
            using (var titleFont = new Font("Segoe UI", 10.5F, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("parameter", titleFont, titleBrush, 18, 10);
            }
        }
    }
}
