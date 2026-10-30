using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PH_08_Test
{
    public class FormLEDTest : Form
    {
        private readonly Form1 _mainForm;

        // 6 LED Error States (true = Has Problem / Checked)
        private bool _pwrHasProblem = false;
        private bool _statusHasProblem = false;
        private bool _localRxHasProblem = false;
        private bool _localTxHasProblem = false;
        private bool _remoteRxHasProblem = false;
        private bool _remoteTxHasProblem = false;

        // Interactive Card Click Rectangles
        private Rectangle _rectPwr;
        private Rectangle _rectStatus;
        private Rectangle _rectLocalRx;
        private Rectangle _rectLocalTx;
        private Rectangle _rectRemoteRx;
        private Rectangle _rectRemoteTx;

        // UI Controls
        private Button btnClose = null!;
        private Button btnReset = null!;
        private Button btnSave = null!;

        // Test Results
        public bool TestPassed { get; private set; } = false;
        public int ProblemCount => GetProblemCount();
        public string ProblemSummary { get; private set; } = "";

        protected override CreateParams CreateParams => FormDropShadowHelper.ApplyCreateParams(base.CreateParams);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            FormDropShadowHelper.ApplyDropShadow(this);
        }

        public FormLEDTest(Form1 mainForm)
        {
            _mainForm = mainForm;
            InitializeComponentCustom();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "LED Test";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(440, 706);
            this.BackColor = Color.White;
            this.DoubleBuffered = true;

            // Form Dragging
            this.MouseDown += Form_MouseDown;
            this.MouseClick += Form_MouseClick;

            // Close Button (Top-Right in Title Bar)
            btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(248, 250, 252),
                Size = new Size(32, 30),
                Location = new Point(400, 5),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 226, 226);
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(239, 68, 68);
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(239, 68, 68);
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(100, 116, 139);
            btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnClose);

            // Bottom Reset Button (Left)
            btnReset = new Button
            {
                Text = "🔄 Reset",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                BackColor = Color.FromArgb(254, 240, 138), // Soft Yellow #FEF08A
                Size = new Size(110, 44),
                Location = new Point(24, 638),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.FlatAppearance.MouseOverBackColor = Color.FromArgb(253, 224, 71);
            btnReset.Click += (s, e) => ResetAllCheckboxes();
            this.Controls.Add(btnReset);

            // Bottom Save Test Button (Right)
            btnSave = new Button
            {
                Text = "💾  Save",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(34, 197, 94), // Emerald Green #22C55E
                Size = new Size(272, 44),
                Location = new Point(144, 638),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 163, 74);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            // Calculate bounding rectangles (Spaced comfortably)
            int cardW = 190;
            int cardH = 122;

            _rectPwr = new Rectangle(24, 170, cardW, cardH);
            _rectStatus = new Rectangle(226, 170, cardW, cardH);

            _rectLocalRx = new Rectangle(24, 330, cardW, cardH);
            _rectLocalTx = new Rectangle(226, 330, cardW, cardH);

            _rectRemoteRx = new Rectangle(24, 490, cardW, cardH);
            _rectRemoteTx = new Rectangle(226, 490, cardW, cardH);
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

        private void Form_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            Point pt = e.Location;

            if (_rectPwr.Contains(pt))
            {
                _pwrHasProblem = !_pwrHasProblem;
                this.Invalidate();
            }
            else if (_rectStatus.Contains(pt))
            {
                _statusHasProblem = !_statusHasProblem;
                this.Invalidate();
            }
            else if (_rectLocalRx.Contains(pt))
            {
                _localRxHasProblem = !_localRxHasProblem;
                this.Invalidate();
            }
            else if (_rectLocalTx.Contains(pt))
            {
                _localTxHasProblem = !_localTxHasProblem;
                this.Invalidate();
            }
            else if (_rectRemoteRx.Contains(pt))
            {
                _remoteRxHasProblem = !_remoteRxHasProblem;
                this.Invalidate();
            }
            else if (_rectRemoteTx.Contains(pt))
            {
                _remoteTxHasProblem = !_remoteTxHasProblem;
                this.Invalidate();
            }
        }

        private int GetProblemCount()
        {
            int count = 0;
            if (_pwrHasProblem) count++;
            if (_statusHasProblem) count++;
            if (_localRxHasProblem) count++;
            if (_localTxHasProblem) count++;
            if (_remoteRxHasProblem) count++;
            if (_remoteTxHasProblem) count++;
            return count;
        }

        #region Custom GDI+ Painting

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

            // Title Bar Text
            using (var titleFont = new Font("Segoe UI", 10.5F, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("LED Test", titleFont, titleBrush, 24, 10);
            }

            // 3. Main Header ("LED Test" & Status Pill)
            using (var headerFont = new Font("Segoe UI", 18F, FontStyle.Bold))
            using (var headerBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("LED Test", headerFont, headerBrush, 24, 54);
            }

            // Status Pill (Top Right)
            int problemCount = GetProblemCount();
            DrawStatusPill(g, 300, 54, 116, 26, problemCount);

            // 4. Instruction Banner (Soft Cream Yellow - generous width for clean 2 lines)
            DrawInstructionBanner(g, 24, 96, 392, 58);

            // 5. Section 1: System LEDs (PWR & Status)
            DrawLedCard(g, _rectPwr, "PWR", Color.FromArgb(239, 68, 68), _pwrHasProblem);
            DrawLedCard(g, _rectStatus, "Status", Color.FromArgb(34, 197, 94), _statusHasProblem);

            // 6. Section 2: Local
            DrawSectionHeader(g, 24, 308, 392, "Local");
            DrawLedCard(g, _rectLocalRx, "RX", Color.FromArgb(34, 197, 94), _localRxHasProblem);
            DrawLedCard(g, _rectLocalTx, "TX", Color.FromArgb(249, 115, 22), _localTxHasProblem);

            // 7. Section 3: Remote
            DrawSectionHeader(g, 24, 468, 392, "Remote");
            DrawLedCard(g, _rectRemoteRx, "RX", Color.FromArgb(34, 197, 94), _remoteRxHasProblem);
            DrawLedCard(g, _rectRemoteTx, "TX", Color.FromArgb(249, 115, 22), _remoteTxHasProblem);
        }

        private void DrawStatusPill(Graphics g, int x, int y, int w, int h, int problemCount)
        {
            var pillRect = new Rectangle(x, y, w, h);
            bool hasProblem = problemCount > 0;

            Color bgColor = hasProblem ? Color.FromArgb(254, 242, 242) : Color.FromArgb(240, 253, 244);
            Color borderColor = hasProblem ? Color.FromArgb(239, 68, 68) : Color.FromArgb(34, 197, 94);
            Color textColor = hasProblem ? Color.FromArgb(220, 38, 38) : Color.FromArgb(22, 163, 74);
            string pillText = hasProblem ? $"มีปัญหา {problemCount} จุด" : "ปกติทุกจุด";

            using (var path = GetRoundedRectanglePath(pillRect, h / 2))
            {
                using (var brush = new SolidBrush(bgColor)) g.FillPath(brush, path);
                using (var pen = new Pen(borderColor, 1.2f)) g.DrawPath(pen, path);
            }

            using (var font = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (var brush = new SolidBrush(textColor))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(pillText, font, brush, pillRect, sf);
            }
        }

        private void DrawInstructionBanner(Graphics g, int x, int y, int w, int h)
        {
            var cardRect = new Rectangle(x, y, w, h);
            using (var path = GetRoundedRectanglePath(cardRect, 8))
            {
                // Cream Yellow Fill
                using (var brush = new SolidBrush(Color.FromArgb(254, 252, 232))) g.FillPath(brush, path);
                using (var pen = new Pen(Color.FromArgb(82, 82, 91), 1f)) g.DrawPath(pen, path);
            }

            // Info Icon (Left)
            float iconCx = x + 24;
            float iconCy = y + (h / 2f);
            DrawInfoIcon(g, iconCx, iconCy, 9.5f, Color.FromArgb(15, 23, 42));

            // Instruction Text (Exact 2 lines)
            using (var font = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
            {
                g.DrawString("ดูไฟ LED แล้วติ๊ก \"มีปัญหา\" เฉพาะตัวที่ผิดปกติ", font, brush, new RectangleF(x + 44, y + 8, w - 50, 20), sf);
                g.DrawString("ตัวที่ปกติไม่ต้องติ๊ก", font, brush, new RectangleF(x + 44, y + 28, w - 50, 20), sf);
            }
        }

        private void DrawSectionHeader(Graphics g, int x, int y, int w, string title)
        {
            using (var font = new Font("Segoe UI", 10.5F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString(title, font, brush, x, y - 2);
                
                // Measure title width to start divider line right after
                SizeF sz = g.MeasureString(title, font);
                int lineStartX = (int)(x + sz.Width + 8);

                using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
                {
                    g.DrawLine(pen, lineStartX, y + 10, x + w, y + 10);
                }
            }
        }

        private void DrawLedCard(Graphics g, Rectangle rect, string title, Color ledColor, bool hasProblem)
        {
            using (var path = GetRoundedRectanglePath(rect, 8))
            {
                // Background
                using (var brush = new SolidBrush(Color.White)) g.FillPath(brush, path);

                // Border: Red if problem, Gray if normal
                Color borderColor = hasProblem ? Color.FromArgb(239, 68, 68) : Color.FromArgb(226, 232, 240);
                float borderWidth = hasProblem ? 1.5f : 1f;
                using (var pen = new Pen(borderColor, borderWidth)) g.DrawPath(pen, path);
            }

            // 1. 3D LED Bulb
            float ledCx = rect.X + (rect.Width / 2f);
            float ledCy = rect.Y + 28;
            float ledRadius = 15f;
            DrawLedBulb(g, ledCx, ledCy, ledRadius, ledColor);

            // 2. Title (e.g. "PWR", "Status", "RX", "TX")
            using (var titleFont = new Font("Segoe UI", 12F, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(title, titleFont, titleBrush, new RectangleF(rect.X, rect.Y + 48, rect.Width, 22), sf);
            }

            // 3. Toggle Button (Bottom)
            int btnW = 160;
            int btnH = 32;
            int btnX = rect.X + ((rect.Width - btnW) / 2);
            int btnY = rect.Y + 78;

            var btnRect = new Rectangle(btnX, btnY, btnW, btnH);
            using (var btnPath = GetRoundedRectanglePath(btnRect, 6))
            {
                if (hasProblem)
                {
                    // Red border button
                    using (var brush = new SolidBrush(Color.White)) g.FillPath(brush, btnPath);
                    using (var pen = new Pen(Color.FromArgb(239, 68, 68), 1.5f)) g.DrawPath(pen, btnPath);

                    // Red Checked Box
                    int boxSize = 16;
                    int boxX = btnX + 22;
                    int boxY = btnY + ((btnH - boxSize) / 2);
                    var checkRect = new Rectangle(boxX, boxY, boxSize, boxSize);
                    using (var checkPath = GetRoundedRectanglePath(checkRect, 3))
                    {
                        using (var brush = new SolidBrush(Color.FromArgb(239, 68, 68))) g.FillPath(brush, checkPath);
                    }
                    DrawCheckmarkOnly(g, boxX + 8, boxY + 8, 4.5f, Color.White);

                    // Text "มีปัญหา"
                    using (var font = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString("มีปัญหา", font, brush, new RectangleF(boxX + 24, btnY, btnW - 48, btnH), sf);
                    }
                }
                else
                {
                    // Unchecked normal button
                    using (var brush = new SolidBrush(Color.White)) g.FillPath(brush, btnPath);
                    using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1f)) g.DrawPath(pen, btnPath);

                    // Empty Gray Box
                    int boxSize = 16;
                    int boxX = btnX + 16;
                    int boxY = btnY + ((btnH - boxSize) / 2);
                    var checkRect = new Rectangle(boxX, boxY, boxSize, boxSize);
                    using (var checkPath = GetRoundedRectanglePath(checkRect, 3))
                    {
                        using (var pen = new Pen(Color.FromArgb(148, 163, 184), 1.2f)) g.DrawPath(pen, checkPath);
                    }

                    // Text "ติ๊กถ้ามีปัญหา"
                    using (var font = new Font("Segoe UI", 9F, FontStyle.Regular))
                    using (var brush = new SolidBrush(Color.FromArgb(51, 65, 85)))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString("ติ๊กถ้ามีปัญหา", font, brush, new RectangleF(boxX + 22, btnY, btnW - 40, btnH), sf);
                    }
                }
            }
        }

        private void DrawLedBulb(Graphics g, float cx, float cy, float radius, Color baseColor)
        {
            // Outer Ring / Base
            using (var pen = new Pen(Color.FromArgb(30, 41, 59), 2f))
            {
                g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2);
            }

            // Fill Base Solid Color
            using (var brush = new SolidBrush(baseColor))
            {
                g.FillEllipse(brush, cx - radius + 1, cy - radius + 1, (radius - 1) * 2, (radius - 1) * 2);
            }

            // 3D Glass / Highlight Gloss
            float hlRadius = radius * 0.55f;
            float hlCx = cx - (radius * 0.25f);
            float hlCy = cy - (radius * 0.25f);
            using (var hlBrush = new SolidBrush(Color.FromArgb(130, 255, 255, 255)))
            {
                g.FillEllipse(hlBrush, hlCx - hlRadius, hlCy - hlRadius, hlRadius * 2, hlRadius * 2);
            }
        }

        private void DrawInfoIcon(Graphics g, float cx, float cy, float radius, Color color)
        {
            using (var pen = new Pen(color, 1.6f))
            {
                g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2);
            }

            // 'i' Dot
            using (var brush = new SolidBrush(color))
            {
                g.FillEllipse(brush, cx - 1.2f, cy - (radius * 0.55f), 2.4f, 2.4f);
            }

            // 'i' Body Line
            using (var pen = new Pen(color, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(pen, cx, cy - (radius * 0.15f), cx, cy + (radius * 0.45f));
            }
        }

        private void DrawCheckmarkOnly(Graphics g, float cx, float cy, float size, Color color)
        {
            using (var pen = new Pen(color, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                PointF p1 = new PointF(cx - (size * 0.7f), cy);
                PointF p2 = new PointF(cx - (size * 0.15f), cy + (size * 0.65f));
                PointF p3 = new PointF(cx + (size * 0.8f), cy - (size * 0.65f));

                g.DrawLines(pen, new PointF[] { p1, p2, p3 });
            }
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        #endregion

        #region Test Execution Logic

        private void ResetAllCheckboxes()
        {
            _pwrHasProblem = false;
            _statusHasProblem = false;
            _localRxHasProblem = false;
            _localTxHasProblem = false;
            _remoteRxHasProblem = false;
            _remoteTxHasProblem = false;
            this.Invalidate();
            _mainForm.LogDebug("[LED Test] รีเซ็ตการเลือกไฟ LED ทั้งหมดเรียบร้อยแล้ว");
        }

        private void ShowSummaryMessageBox()
        {
            int count = GetProblemCount();
            if (count == 0)
            {
                _mainForm.LogDebug("[LED Test] สรุปผล: ไฟ LED ปกติทุกดวง (PASS 100%)");
                MessageBox.Show("🎉 ผลการตรวจสอบไฟ LED: ผ่านสมบูรณ์ (PASS 100%)\n\n" +
                                "✔ ไฟ PWR: ปกติ\n" +
                                "✔ ไฟ Status: ปกติ\n" +
                                "✔ ไฟ Local RX / TX: ปกติ\n" +
                                "✔ ไฟ Remote RX / TX: ปกติ\n\n" +
                                "ระบบได้บันทึกผลการตรวจสอบเรียบร้อยแล้ว",
                                "สรุปผลการทดสอบไฟ LED", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var issues = new System.Collections.Generic.List<string>();
                if (_pwrHasProblem) issues.Add("• ไฟ PWR: มีปัญหา");
                if (_statusHasProblem) issues.Add("• ไฟ Status: มีปัญหา");
                if (_localRxHasProblem) issues.Add("• ไฟ Local RX: มีปัญหา");
                if (_localTxHasProblem) issues.Add("• ไฟ Local TX: มีปัญหา");
                if (_remoteRxHasProblem) issues.Add("• ไฟ Remote RX: มีปัญหา");
                if (_remoteTxHasProblem) issues.Add("• ไฟ Remote TX: มีปัญหา");

                string problemDetails = string.Join("\n", issues);
                _mainForm.LogDebug($"[LED Test] สรุปผล: พบไฟ LED มีปัญหา {count} จุด ({string.Join(", ", issues)})");
                MessageBox.Show($"⚠️ ผลการตรวจสอบไฟ LED: พบปัญหา {count} จุด\n\n" +
                                $"{problemDetails}\n\n" +
                                "ระบบได้บันทึกผลการตรวจสอบเรียบร้อยแล้ว",
                                "สรุปผลการทดสอบไฟ LED", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            int count = GetProblemCount();
            TestPassed = (count == 0);

            var issues = new System.Collections.Generic.List<string>();
            if (_pwrHasProblem) issues.Add("PWR");
            if (_statusHasProblem) issues.Add("Status");
            if (_localRxHasProblem) issues.Add("Local RX");
            if (_localTxHasProblem) issues.Add("Local TX");
            if (_remoteRxHasProblem) issues.Add("Remote RX");
            if (_remoteTxHasProblem) issues.Add("Remote TX");

            ProblemSummary = issues.Count > 0 ? string.Join(", ", issues) : "ปกติทุกดวง";

            if (TestPassed)
            {
                _mainForm.LogDebug("✔ [LED Test] สรุปผล: ไฟ LED ปกติทุกดวง (PASS)");
            }
            else
            {
                _mainForm.LogDebug($"✖ [LED Test] สรุปผล: พบไฟ LED มีปัญหา {count} จุด ({ProblemSummary})");
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        #endregion
    }
}
