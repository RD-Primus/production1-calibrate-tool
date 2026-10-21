using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TEST_DEF_03N
{
    public enum TimelineStepStatus
    {
        Pending,
        Running,
        Pass,
        Fail
    }

    public enum TimelineViewMode
    {
        Overview,
        Temperature
    }

    public class TimelineStepItem
    {
        public string Title { get; set; } = "";
        public TimelineStepStatus Status { get; set; } = TimelineStepStatus.Pending;

        public TimelineStepItem() { }

        public TimelineStepItem(string title, TimelineStepStatus status = TimelineStepStatus.Pending)
        {
            Title = title;
            Status = status;
        }
    }

    public class StepTimelineControl : Control
    {
        #region Private Fields

        private TimelineViewMode _viewMode = TimelineViewMode.Overview;
        private readonly List<TimelineStepItem> _overviewSteps = new List<TimelineStepItem>();
        private readonly List<TimelineStepItem> _tempSteps = new List<TimelineStepItem>();

        private string _tempTitle = "Temp Test";
        private string _resultText = "---------";
        private Color _resultTextColor = Color.Black;
        private Color _resultCardBg = Color.White;

        // Visual properties matching Image 2 & dark theme
        private int _nodeDiameter = 34;
        private int _lineWidth = 3;
        private Color _lineColor = Color.FromArgb(46, 64, 88);
        private Color _passColor = Color.FromArgb(78, 190, 126);
        private Color _failColor = Color.FromArgb(239, 68, 68);
        private Color _runningBorderColor = Color.FromArgb(56, 189, 248);
        private Color _runningFillColor = Color.FromArgb(14, 116, 144);
        private Color _pendingBorderColor = Color.FromArgb(65, 82, 105);
        private Color _pendingTextColor = Color.FromArgb(156, 172, 193);

        private Font _titleFont = new Font("Segoe UI", 16f, FontStyle.Bold);
        private Font _stepFont = new Font("Leelawadee UI", 12f, FontStyle.Bold);
        private Font _resultFont = new Font("Segoe UI Semibold", 14f, FontStyle.Bold);

        private bool _isQAMode = false;

        #endregion

        #region Properties

        [Category("Timeline Appearance")]
        [Description("โหมดการแสดงผลปัจจุบัน (Overview 4 สเต็ปหลัก หรือ Temperature ทดสอบอุณหภูมิ)")]
        public TimelineViewMode ViewMode
        {
            get => _viewMode;
            set { _viewMode = value; Invalidate(); }
        }

        [Category("Timeline Appearance")]
        [Description("ขนาดเส้นผ่านศูนย์กลางของวงกลมสถานะ (px)")]
        public int NodeDiameter
        {
            get => _nodeDiameter;
            set { _nodeDiameter = Math.Max(16, value); Invalidate(); }
        }

        [Category("Timeline Appearance")]
        [Description("ความหนาของเส้นเชื่อมต่อแนวตั้ง (px)")]
        public int LineWidth
        {
            get => _lineWidth;
            set { _lineWidth = Math.Max(1, value); Invalidate(); }
        }

        [Category("Timeline Appearance")]
        [Description("สีของเส้นเชื่อมต่อแนวตั้ง")]
        public Color LineColor
        {
            get => _lineColor;
            set { _lineColor = value; Invalidate(); }
        }

        [Category("Timeline Appearance")]
        [Description("สีวงกลมสถานะผ่าน (Pass) สไตล์ภาพที่ 2")]
        public Color PassColor
        {
            get => _passColor;
            set { _passColor = value; Invalidate(); }
        }

        [Category("Timeline Appearance")]
        [Description("สีวงกลมสถานะไม่ผ่าน (Fail)")]
        public Color FailColor
        {
            get => _failColor;
            set { _failColor = value; Invalidate(); }
        }

        [Category("Timeline Appearance")]
        [Description("ฟอนต์สำหรับข้อความแต่ละขั้นตอน")]
        public Font StepFont
        {
            get => _stepFont;
            set { _stepFont = value; Invalidate(); }
        }

        [Category("Timeline Appearance")]
        [Description("ฟอนต์สำหรับหัวข้อ")]
        public Font TitleFont
        {
            get => _titleFont;
            set { _titleFont = value; Invalidate(); }
        }

        [Category("Timeline Appearance")]
        [Description("ข้อความผลการทดสอบ (PASS / FAIL / ---------)")]
        public string ResultText
        {
            get => _resultText;
            set { _resultText = value; Invalidate(); }
        }

        [Category("Timeline Appearance")]
        [Description("สีตัวอักษรของผลการทดสอบ")]
        public Color ResultTextColor
        {
            get => _resultTextColor;
            set { _resultTextColor = value; Invalidate(); }
        }

        [Browsable(false)]
        public bool IsQAMode => _isQAMode;

        #endregion

        public StepTimelineControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);

            DoubleBuffered = true;
            BackColor = Color.FromArgb(24, 32, 43);
            ForeColor = Color.White;
            Size = new Size(292, 405);

            InitializeDefaultSteps();
        }

        private void InitializeDefaultSteps()
        {
            _overviewSteps.Clear();
            _overviewSteps.Add(new TimelineStepItem("Calibration", TimelineStepStatus.Pending));
            _overviewSteps.Add(new TimelineStepItem("ทดสอบอุณหภูมิ", TimelineStepStatus.Pending));
            _overviewSteps.Add(new TimelineStepItem("ทดสอบ Relay", TimelineStepStatus.Pending));
            _overviewSteps.Add(new TimelineStepItem("ทดสอบปุ่มกด", TimelineStepStatus.Pending));

            _tempSteps.Clear();
            _tempSteps.Add(new TimelineStepItem("- 40 °C", TimelineStepStatus.Pending));
            _tempSteps.Add(new TimelineStepItem("0 °C", TimelineStepStatus.Pending));
            _tempSteps.Add(new TimelineStepItem("130 °C", TimelineStepStatus.Pending));
        }

        #region Public Control Methods

        /// <summary>
        /// สลับกลับมาหน้าภาพรวม 4 ขั้นตอนหลัก
        /// </summary>
        public void ShowOverviewMode()
        {
            _viewMode = TimelineViewMode.Overview;
            Invalidate();
        }

        /// <summary>
        /// สลับไปหน้าทดสอบอุณหภูมิ (รองรับทั้ง QA 9 จุด และ QC 3 จุด)
        /// </summary>
        public void ShowTempMode(bool isQA, string testType, IEnumerable<string>? customPoints = null)
        {
            _isQAMode = isQA;
            _viewMode = TimelineViewMode.Temperature;
            _tempTitle = isQA ? "Temp Test" : "Temp Test";
            _resultText = "---------";
            _resultTextColor = Color.Black;

            _tempSteps.Clear();
            if (customPoints != null)
            {
                foreach (var pt in customPoints)
                {
                    _tempSteps.Add(new TimelineStepItem(pt, TimelineStepStatus.Pending));
                }
            }
            else if (isQA)
            {
                // QA Mode: 9 จุดมาตรฐาน
                string[] qaPoints = { "- 40 °C", "- 20 °C", "0 °C", "20 °C", "40 °C", "60 °C", "80 °C", "100 °C", "130 °C" };
                foreach (var pt in qaPoints)
                {
                    _tempSteps.Add(new TimelineStepItem(pt, TimelineStepStatus.Pending));
                }
            }
            else
            {
                // QC Mode 3 จุดเริ่มต้น
                _tempSteps.Add(new TimelineStepItem("- 40 °C", TimelineStepStatus.Pending));
                _tempSteps.Add(new TimelineStepItem("40 °C", TimelineStepStatus.Pending));
                _tempSteps.Add(new TimelineStepItem("130 °C", TimelineStepStatus.Pending));
            }

            Invalidate();
        }

        /// <summary>
        /// ปรับสถานะของ 4 สเต็ปหลัก (Calibration = 0, Temp = 1, Relay = 2, Button = 3)
        /// </summary>
        public void SetOverviewStepStatus(int stepIndex, TimelineStepStatus status)
        {
            if (stepIndex >= 0 && stepIndex < _overviewSteps.Count)
            {
                _overviewSteps[stepIndex].Status = status;
                Invalidate();
            }
        }

        /// <summary>
        /// แปลงค่า Color เป็น TimelineStepStatus เพื่อความเข้ากันได้กับโค้ดเดิม
        /// </summary>
        public void SetOverviewStepStatus(int stepIndex, Color lightColor)
        {
            TimelineStepStatus status = ColorToStatus(lightColor);
            SetOverviewStepStatus(stepIndex, status);
        }

        /// <summary>
        /// ปรับสถานะของจุดทดสอบอุณหภูมิ
        /// </summary>
        public void SetTempStepStatus(int pointIndex, TimelineStepStatus status)
        {
            if (pointIndex >= 0 && pointIndex < _tempSteps.Count)
            {
                _tempSteps[pointIndex].Status = status;
                Invalidate();
            }
        }

        /// <summary>
        /// แปลงค่า Color เป็น TimelineStepStatus สำหรับจุดอุณหภูมิ
        /// </summary>
        public void SetTempStepStatus(int pointIndex, Color lightColor)
        {
            TimelineStepStatus status = ColorToStatus(lightColor);
            SetTempStepStatus(pointIndex, status);
        }

        /// <summary>
        /// ตั้งค่าผลลัพธ์ในกรอบ Test Result
        /// </summary>
        public void SetResult(string resultText, Color textColor)
        {
            _resultText = resultText;
            _resultTextColor = textColor;
            Invalidate();
        }

        /// <summary>
        /// รีเซ็ตไฟ 4 สเต็ปหลักเป็น Pending
        /// </summary>
        public void ResetOverview()
        {
            foreach (var step in _overviewSteps)
            {
                step.Status = TimelineStepStatus.Pending;
            }
            Invalidate();
        }

        /// <summary>
        /// รีเซ็ตไฟจุดอุณหภูมิเป็น Pending
        /// </summary>
        public void ResetTempPoints()
        {
            foreach (var step in _tempSteps)
            {
                step.Status = TimelineStepStatus.Pending;
            }
            _resultText = "---------";
            _resultTextColor = Color.Black;
            Invalidate();
        }

        private static TimelineStepStatus ColorToStatus(Color color)
        {
            if (color == Color.LightGray || color == Color.Gray)
                return TimelineStepStatus.Pending;
            if (color == Color.Yellow || color == Color.Gold || color == Color.Orange)
                return TimelineStepStatus.Running;
            if (color == Color.LimeGreen || color == Color.Green || color == Color.Lime)
                return TimelineStepStatus.Pass;
            if (color == Color.Red || color == Color.DarkRed)
                return TimelineStepStatus.Fail;

            return TimelineStepStatus.Pending;
        }

        #endregion

        #region Painting (GDI+ High Quality)

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Background fill
            using (var bgBrush = new SolidBrush(BackColor))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            if (_viewMode == TimelineViewMode.Overview)
            {
                RenderOverview(g);
            }
            else
            {
                RenderTemperature(g);
            }
        }

        /// <summary>
        /// วาดหน้า 4 สเต็ปหลัก (Calibration ➔ ทดสอบอุณหภูมิ ➔ ทดสอบ Relay ➔ ทดสอบปุ่มกด)
        /// สไตล์ตามภาพที่ 2 เป๊ะๆ
        /// </summary>
        private void RenderOverview(Graphics g)
        {
            int count = _overviewSteps.Count;
            if (count == 0) return;

            int nodeX = 40;
            int textX = nodeX + _nodeDiameter / 2 + 18;
            int topPadding = 38;
            int bottomPadding = 38;
            int availableHeight = Height - topPadding - bottomPadding;
            float stepDistance = count > 1 ? (float)availableHeight / (count - 1) : 0;

            // 1. วาดเส้นเชื่อมต่อแนวตั้ง (Connecting Line)
            float firstCy = topPadding;
            float lastCy = topPadding + (count - 1) * stepDistance;
            using (var linePen = new Pen(_lineColor, _lineWidth))
            {
                linePen.StartCap = LineCap.Round;
                linePen.EndCap = LineCap.Round;
                g.DrawLine(linePen, nodeX, firstCy, nodeX, lastCy);
            }

            // 2. วาดโหนดและข้อความแต่ละขั้นตอน
            for (int i = 0; i < count; i++)
            {
                float cy = topPadding + i * stepDistance;
                var item = _overviewSteps[i];

                DrawNode(g, nodeX, cy, _nodeDiameter / 2f, item.Status);

                // ข้อความกำกับ
                Color textColor = (item.Status == TimelineStepStatus.Pending) ? _pendingTextColor : Color.White;
                using (var textBrush = new SolidBrush(textColor))
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near })
                {
                    RectangleF textRect = new RectangleF(textX, cy - 22, Width - textX - 8, 44);
                    g.DrawString(item.Title, _stepFont, textBrush, textRect, sf);
                }
            }
        }

        /// <summary>
        /// วาดหน้าทดสอบอุณหภูมิ (รองรับทั้ง QA 9 จุด และ QC 3 จุด)
        /// </summary>
        private void RenderTemperature(Graphics g)
        {
            // 1. หัวข้อด้านบน (เช่น "Temp Test")
            using (var titleBrush = new SolidBrush(Color.White))
            using (var titleSf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                RectangleF titleRect = new RectangleF(0, 10, Width, 36);
                g.DrawString(_tempTitle, _titleFont, titleBrush, titleRect, titleSf);
            }

            // คำนวณพื้นที่สำหรับ Result Box ด้านล่าง
            int resultBoxHeight = 90;
            int contentTop = 55;
            int contentBottom = Height - resultBoxHeight;

            int count = _tempSteps.Count;

            if (_isQAMode && count >= 9)
            {
                // โหมด QA: มี 9 จุด จัดเป็น 2 คอลัมน์แบบ Timeline เชื่อมเส้นอย่างสวยงาม (สไตล์ภาพ 1 + ภาพ 2)
                // คอลัมน์ซ้าย: จุด 0..4 (5 จุด: -40, -20, 0, 20, 40)
                // คอลัมน์ขวา: จุด 5..8 (4 จุด: 60, 80, 100, 130)
                int col1NodeX = 35;
                int col1TextX = col1NodeX + 20;
                int col2NodeX = Width / 2 + 15;
                int col2TextX = col2NodeX + 20;

                int col1Count = 5;
                int col2Count = 4;
                float qaNodeRadius = 13f; // เส้นผ่านศูนย์กลาง 26px

                float col1StepDist = (contentBottom - contentTop) / (float)col1Count;

                // วาดเส้นแนวตั้งคอลัมน์ซ้าย
                float c1FirstCy = contentTop + col1StepDist * 0.5f;
                float c1LastCy = contentTop + (col1Count - 0.5f) * col1StepDist;
                using (var linePen = new Pen(_lineColor, 2.5f))
                {
                    g.DrawLine(linePen, col1NodeX, c1FirstCy, col1NodeX, c1LastCy);
                }

                // วาดเส้นแนวตั้งคอลัมน์ขวา
                float c2FirstCy = contentTop + col1StepDist * 0.5f;
                float c2LastCy = contentTop + (col2Count - 0.5f) * col1StepDist;
                using (var linePen = new Pen(_lineColor, 2.5f))
                {
                    g.DrawLine(linePen, col2NodeX, c2FirstCy, col2NodeX, c2LastCy);
                }

                using (var fontQA = new Font("Segoe UI Semibold", 11f, FontStyle.Bold))
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near })
                {
                    // คอลัมน์ซ้าย (จุด 0..4)
                    for (int i = 0; i < col1Count && i < count; i++)
                    {
                        float cy = contentTop + (i + 0.5f) * col1StepDist;
                        var item = _tempSteps[i];
                        DrawNode(g, col1NodeX, cy, qaNodeRadius, item.Status);

                        Color textColor = (item.Status == TimelineStepStatus.Pending) ? _pendingTextColor : Color.White;
                        using (var brush = new SolidBrush(textColor))
                        {
                            g.DrawString(item.Title, fontQA, brush, new RectangleF(col1TextX, cy - 14, 85, 28), sf);
                        }
                    }

                    // คอลัมน์ขวา (จุด 5..8)
                    for (int i = 0; i < col2Count && (i + col1Count) < count; i++)
                    {
                        float cy = contentTop + (i + 0.5f) * col1StepDist;
                        var item = _tempSteps[i + col1Count];
                        DrawNode(g, col2NodeX, cy, qaNodeRadius, item.Status);

                        Color textColor = (item.Status == TimelineStepStatus.Pending) ? _pendingTextColor : Color.White;
                        using (var brush = new SolidBrush(textColor))
                        {
                            g.DrawString(item.Title, fontQA, brush, new RectangleF(col2TextX, cy - 14, 85, 28), sf);
                        }
                    }
                }
            }
            else
            {
                // โหมด QC (3 จุด): จัดเป็น 1 แถวแนวตั้งตรงกลางสวยงาม
                int nodeX = 65;
                int textX = nodeX + _nodeDiameter / 2 + 18;
                float nodeRadius = _nodeDiameter / 2f;

                float stepDist = (contentBottom - contentTop) / (float)Math.Max(1, count);
                float firstCy = contentTop + stepDist * 0.5f;
                float lastCy = contentTop + (count - 0.5f) * stepDist;

                using (var linePen = new Pen(_lineColor, _lineWidth))
                {
                    g.DrawLine(linePen, nodeX, firstCy, nodeX, lastCy);
                }

                for (int i = 0; i < count; i++)
                {
                    float cy = contentTop + (i + 0.5f) * stepDist;
                    var item = _tempSteps[i];

                    DrawNode(g, nodeX, cy, nodeRadius, item.Status);

                    Color textColor = (item.Status == TimelineStepStatus.Pending) ? _pendingTextColor : Color.White;
                    using (var textBrush = new SolidBrush(textColor))
                    using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near })
                    {
                        RectangleF textRect = new RectangleF(textX, cy - 18, Width - textX - 10, 36);
                        g.DrawString(item.Title, _stepFont, textBrush, textRect, sf);
                    }
                }
            }

            // 3. วาด Result Box ด้านล่าง (Test Result: [ --------- / PASS / FAIL ])
            RenderResultBox(g, contentBottom);
        }

        private void RenderResultBox(Graphics g, int topY)
        {
            // ข้อความ "Test Result:"
            using (var lblBrush = new SolidBrush(Color.FromArgb(85, 223, 243)))
            {
                using (var lblFont = new Font("Segoe UI", 9.5f, FontStyle.Bold | FontStyle.Italic))
                {
                    g.DrawString("Test Result:", lblFont, lblBrush, 22, topY + 4);
                }
            }

            // กรอบการ์ดสีขาวแสดงผล
            int cardW = 145;
            int cardH = 42;
            int cardX = (Width - cardW) / 2;
            int cardY = topY + 28;

            Rectangle cardRect = new Rectangle(cardX, cardY, cardW, cardH);
            using (var cardBrush = new SolidBrush(_resultCardBg))
            {
                using (var cardPath = GetRoundedPath(cardRect, 8))
                {
                    g.FillPath(cardBrush, cardPath);
                    using (var borderPen = new Pen(Color.FromArgb(200, 210, 225), 1))
                    {
                        g.DrawPath(borderPen, cardPath);
                    }
                }
            }

            // ตัวอักษรแสดงผล PASS / FAIL
            using (var resBrush = new SolidBrush(_resultTextColor))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(_resultText, _resultFont, resBrush, cardRect, sf);
            }
        }

        /// <summary>
        /// วาดวงกลมโหนดตามสถานะ (Pending = วงกลมกลวง, Pass = วงกลมเขียว+ติ๊กถูก, Fail = วงกลมแดง+กากบาท)
        /// ตามภาพที่ 2 แบบเป๊ะๆ
        /// </summary>
        private void DrawNode(Graphics g, float cx, float cy, float r, TimelineStepStatus status)
        {
            switch (status)
            {
                case TimelineStepStatus.Pending:
                    // วงกลมเส้นขอบสีเทา กลวงตรงกลาง สีพื้นหลังกลืนกับพาเนล
                    using (var fillBrush = new SolidBrush(BackColor))
                    {
                        g.FillEllipse(fillBrush, cx - r, cy - r, r * 2, r * 2);
                    }
                    using (var borderPen = new Pen(_pendingBorderColor, Math.Max(2f, r * 0.16f)))
                    {
                        g.DrawEllipse(borderPen, cx - r, cy - r, r * 2, r * 2);
                    }
                    break;

                case TimelineStepStatus.Running:
                    // วงกลมสีฟ้ากำลังทดสอบ
                    using (var glowBrush = new SolidBrush(Color.FromArgb(50, 56, 189, 248)))
                    {
                        g.FillEllipse(glowBrush, cx - r - 3, cy - r - 3, (r + 3) * 2, (r + 3) * 2);
                    }
                    using (var fillBrush = new SolidBrush(_runningFillColor))
                    {
                        g.FillEllipse(fillBrush, cx - r, cy - r, r * 2, r * 2);
                    }
                    using (var borderPen = new Pen(_runningBorderColor, Math.Max(2.5f, r * 0.18f)))
                    {
                        g.DrawEllipse(borderPen, cx - r, cy - r, r * 2, r * 2);
                    }
                    // จุดไฟสีขาวตรงกลาง
                    float dotR = r * 0.35f;
                    using (var dotBrush = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(dotBrush, cx - dotR, cy - dotR, dotR * 2, dotR * 2);
                    }
                    break;

                case TimelineStepStatus.Pass:
                    // วงกลมสีเขียวทึบ + เวกเตอร์เครื่องหมายถูกสีขาว (ตรงตามภาพ 2)
                    using (var passBrush = new SolidBrush(_passColor))
                    {
                        g.FillEllipse(passBrush, cx - r, cy - r, r * 2, r * 2);
                    }
                    // วาดเครื่องหมายถูก (Checkmark)
                    float penW = Math.Max(2.2f, r * 0.22f);
                    using (var checkPen = new Pen(Color.White, penW))
                    {
                        checkPen.StartCap = LineCap.Round;
                        checkPen.EndCap = LineCap.Round;
                        checkPen.LineJoin = LineJoin.Round;

                        PointF p1 = new PointF(cx - r * 0.38f, cy + r * 0.04f);
                        PointF p2 = new PointF(cx - r * 0.08f, cy + r * 0.36f);
                        PointF p3 = new PointF(cx + r * 0.42f, cy - r * 0.32f);
                        g.DrawLines(checkPen, new[] { p1, p2, p3 });
                    }
                    break;

                case TimelineStepStatus.Fail:
                    // วงกลมสีแดงทึบ + เวกเตอร์กากบาทสีขาว
                    using (var failBrush = new SolidBrush(_failColor))
                    {
                        g.FillEllipse(failBrush, cx - r, cy - r, r * 2, r * 2);
                    }
                    // วาดกากบาท (Cross)
                    float crossW = Math.Max(2.2f, r * 0.22f);
                    using (var crossPen = new Pen(Color.White, crossW))
                    {
                        crossPen.StartCap = LineCap.Round;
                        crossPen.EndCap = LineCap.Round;

                        float d = r * 0.32f;
                        g.DrawLine(crossPen, cx - d, cy - d, cx + d, cy + d);
                        g.DrawLine(crossPen, cx - d, cy + d, cx + d, cy - d);
                    }
                    break;
            }
        }

        private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;

            RectangleF arc = new RectangleF(rect.X, rect.Y, diameter, diameter);

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        #endregion
    }
}
