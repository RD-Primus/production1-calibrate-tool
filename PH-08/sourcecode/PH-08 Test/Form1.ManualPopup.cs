using System;
using System.Drawing;
using System.Windows.Forms;

namespace PH_08_Test
{
    public partial class Form1
    {
        // ─────────────────────────────────────────────────────────────
        // Manual Control Popup Components (ย้ายออกจาก Form1 ให้หน้าจอหลักโล่ง)
        // ─────────────────────────────────────────────────────────────
        private CustomRoundedPanel customRoundedPanel7 = null!;
        private Label label31 = null!;
        private PictureBox setting = null!;
        private CustomRoundedButton btn_TestCommunication = null!;
        private CustomRoundedButton btn_TestLED = null!;
        private CustomRoundedButton btn_TestSwitchID = null!;

        private Form? _manualPopupForm;
        private ToolTip? _manualGearToolTip;

        /// <summary>
        /// สร้างและกำหนดค่าปุ่ม/แผงควบคุม Manual ทั้งหมดให้อยู่ในโค้ด Popup โดยตรง (ไม่ปนเปื้อนใน Form1.Designer.cs)
        /// </summary>
        private void InitManualPopupControls()
        {
            // 1. Label "Manual"
            label31 = new Label
            {
                AutoSize = true,
                Font = new Font("Leelawadee UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0),
                ForeColor = Color.FromArgb(27, 94, 32),
                Location = new Point(7, 7),
                Name = "label31",
                Size = new Size(83, 28),
                Text = "Manual"
            };

            // 2. PictureBox "setting" (สำหรับเปิด FormParameter)
            setting = new PictureBox
            {
                Location = new Point(248, 13),
                Name = "setting",
                Size = new Size(25, 25),
                TabStop = false
            };

            // 3. Button "Test Communication"
            btn_TestCommunication = new CustomRoundedButton
            {
                BackColor = Color.FromArgb(224, 224, 224),
                BorderColor = Color.Silver,
                BorderRadius = 5,
                BorderThickness = 2,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(13, 44),
                Name = "btn_TestCommunication",
                Size = new Size(260, 32),
                Text = "Test Communication",
                UseVisualStyleBackColor = false
            };
            btn_TestCommunication.FlatAppearance.BorderSize = 0;

            // 4. Button "Test LED"
            btn_TestLED = new CustomRoundedButton
            {
                BackColor = Color.FromArgb(224, 224, 224),
                BorderColor = Color.Silver,
                BorderRadius = 5,
                BorderThickness = 2,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(13, 84),
                Name = "btn_TestLED",
                Size = new Size(126, 32),
                Text = "Test LED",
                UseVisualStyleBackColor = false
            };
            btn_TestLED.FlatAppearance.BorderSize = 0;

            // 5. Button "Test SW ID"
            btn_TestSwitchID = new CustomRoundedButton
            {
                BackColor = Color.FromArgb(224, 224, 224),
                BorderColor = Color.Silver,
                BorderRadius = 5,
                BorderThickness = 2,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(147, 84),
                Name = "btn_TestSwitchID",
                Size = new Size(126, 32),
                Text = "Test SW ID",
                UseVisualStyleBackColor = false
            };
            btn_TestSwitchID.FlatAppearance.BorderSize = 0;

            // 6. CustomRoundedPanel7 (แผงคลุมปุ่ม Manual ทั้งหมด)
            customRoundedPanel7 = new CustomRoundedPanel
            {
                BackColor = Color.White,
                BorderColor = Color.White,
                BorderRadius = 5,
                BorderThickness = 2,
                Location = new Point(10, 44),
                Name = "customRoundedPanel7",
                Size = new Size(287, 125)
            };
            customRoundedPanel7.Controls.Add(label31);
            customRoundedPanel7.Controls.Add(setting);
            customRoundedPanel7.Controls.Add(btn_TestCommunication);
            customRoundedPanel7.Controls.Add(btn_TestLED);
            customRoundedPanel7.Controls.Add(btn_TestSwitchID);
        }

        /// <summary>
        /// เตรียมการแสดงผล Manual Control Popup และตั้งค่าปุ่มฟันเฟือง
        /// </summary>
        private void InitManualPopup()
        {
            if (btn_manualGear != null)
            {
                _manualGearToolTip = new ToolTip();
                _manualGearToolTip.SetToolTip(btn_manualGear, "Manual Control (เปิด/ปิด หน้าต่างควบคุมแบบแมนนวล)");
                btn_manualGear.Click += (s, e) => ToggleManualPopup();
            }
        }

        /// <summary>
        /// เปิดหรือปิดหน้าต่าง Popup ของ Manual
        /// </summary>
        public void ToggleManualPopup()
        {
            EnsureManualPopupCreated();

            if (_manualPopupForm == null) return;

            if (_manualPopupForm.Visible)
            {
                _manualPopupForm.Hide();
                return;
            }

            PositionManualPopup();
            _manualPopupForm.Show(this);
            _manualPopupForm.BringToFront();
        }

        private void PositionManualPopup()
        {
            if (_manualPopupForm == null || btn_manualGear == null) return;

            Point btnScreen = btn_manualGear.PointToScreen(Point.Empty);
            Rectangle screenArea = Screen.FromControl(btn_manualGear).WorkingArea;

            // จัดตำแหน่งให้อยู่ด้านล่างเยื้องตามแนวปุ่มฟันเฟือง
            int x = btnScreen.X;
            int y = btnScreen.Y + btn_manualGear.Height + 4;

            // หากตกขอบจอด้านล่าง ให้ดีดขึ้นไปอยู่ด้านบนแทน
            if (y + _manualPopupForm.Height > screenArea.Bottom)
            {
                y = btnScreen.Y - _manualPopupForm.Height - 4;
            }

            // จัดตำแหน่งไม่ให้หลุดขอบซ้าย/ขวา/บน
            if (x + _manualPopupForm.Width > screenArea.Right)
                x = screenArea.Right - _manualPopupForm.Width - 10;
            if (x < screenArea.Left)
                x = screenArea.Left + 10;
            if (y < screenArea.Top)
                y = screenArea.Top + 10;

            _manualPopupForm.Location = new Point(x, y);
        }

        private class ManualFloatingPopupForm : Form
        {
            protected override CreateParams CreateParams => FormDropShadowHelper.ApplyCreateParams(base.CreateParams);

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                FormDropShadowHelper.ApplyDropShadow(this);
            }
        }

        private void EnsureManualPopupCreated()
        {
            if (_manualPopupForm != null && !_manualPopupForm.IsDisposed)
                return;

            int panelW = customRoundedPanel7?.Width ?? 287;
            int panelH = customRoundedPanel7?.Height ?? 125;

            _manualPopupForm = new ManualFloatingPopupForm
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.FromArgb(248, 250, 252),
                ShowInTaskbar = false,
                KeyPreview = true,
                ClientSize = new Size(panelW + 20, panelH + 54)
            };

            // วาดเส้นขอบ Slate-400 ชัดเจนมีมิติ ไม่กลืนกับพื้นหลังหลัก
            _manualPopupForm.Paint += (s, e) =>
            {
                using var borderPen = new Pen(FormDropShadowHelper.BorderColor, 1.5f);
                e.Graphics.DrawRectangle(borderPen, 0, 0, _manualPopupForm.Width - 1, _manualPopupForm.Height - 1);
            };

            // ──────────────────────────────────────────
            // แถบ Header สีเขียว Forest Green (ลากย้ายได้)
            // ──────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(27, 94, 32),
                Cursor = Cursors.SizeAll
            };

            var lblTitle = new Label
            {
                Text = "🔧 Manual Control",
                Font = new Font("Leelawadee UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(12, 8),
                Cursor = Cursors.SizeAll
            };
            pnlHeader.Controls.Add(lblTitle);

            // ปุ่มปิด [✕]
            var btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(26, 26),
                Location = new Point(_manualPopupForm.ClientSize.Width - 30, 5),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 38, 38);
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(185, 28, 28);
            btnClose.Click += (s, e) => _manualPopupForm.Hide();
            pnlHeader.Controls.Add(btnClose);

            // ระบบคลิกลากย้าย Popup
            bool isDragging = false;
            Point dragCursor = Point.Empty;
            Point dragForm = Point.Empty;

            void OnMouseDown(object? s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = true;
                    dragCursor = Cursor.Position;
                    dragForm = _manualPopupForm.Location;
                }
            }

            void OnMouseMove(object? s, MouseEventArgs e)
            {
                if (isDragging)
                {
                    Point diff = Point.Subtract(Cursor.Position, new Size(dragCursor));
                    _manualPopupForm.Location = Point.Add(dragForm, new Size(diff));
                }
            }

            void OnMouseUp(object? s, MouseEventArgs e)
            {
                isDragging = false;
            }

            pnlHeader.MouseDown += OnMouseDown;
            pnlHeader.MouseMove += OnMouseMove;
            pnlHeader.MouseUp += OnMouseUp;
            lblTitle.MouseDown += OnMouseDown;
            lblTitle.MouseMove += OnMouseMove;
            lblTitle.MouseUp += OnMouseUp;

            _manualPopupForm.Controls.Add(pnlHeader);

            // ติดตั้ง customRoundedPanel7 ใน Popup
            if (customRoundedPanel7 != null)
            {
                if (customRoundedPanel7.Parent != null)
                {
                    customRoundedPanel7.Parent.Controls.Remove(customRoundedPanel7);
                }
                customRoundedPanel7.Location = new Point(10, 44);
                customRoundedPanel7.Visible = true;
                _manualPopupForm.Controls.Add(customRoundedPanel7);
            }

            // ซ่อนแทนการ Dispose เมื่อผู้ใช้กดปิด
            _manualPopupForm.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    _manualPopupForm.Hide();
                }
            };

            // กดปุ่ม Escape เพื่อปิดหน้าต่างได้
            _manualPopupForm.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    _manualPopupForm.Hide();
                }
            };
        }
    }
}
