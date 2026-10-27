using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace TEST_DEF_03N
{
    internal sealed class HelpPopupForm : Form
    {
        private static readonly Color BackgroundColor = Color.FromArgb(7, 23, 35);
        private static readonly Color HeaderColor = Color.FromArgb(12, 29, 46);
        private static readonly Color CardBg = Color.FromArgb(20, 40, 55);
        private static readonly Color AccentColor = Color.FromArgb(73, 218, 242);
        private static readonly Color TextColor = Color.White;
        private static readonly Color SubTextColor = Color.FromArgb(180, 205, 225);
        private static readonly Color ExampleBoxBg = Color.FromArgb(12, 29, 46);
        private static readonly Color ExampleTextColor = Color.FromArgb(166, 185, 199);
        private static readonly Color DangerColor = Color.FromArgb(182, 56, 33);
        private static readonly Color NavBtnBg = Color.FromArgb(20, 40, 55);
        private static readonly Color NavBtnHover = Color.FromArgb(28, 55, 75);

        private const int TotalFormWidth = 1080;
        private const int TotalFormHeight = 680;
        private const int ExampleBoxWidth = 330;

        private int _currentTab = 0; // 0 = วิธีการใช้งาน, 1 = ตัวอย่างการกรอกข้อมูล
        private int _currentWorkflowSlide = 0; // 0 = Step 1&2, 1 = Step 3&4, 2 = Step 5&6

        // UI Controls
        private Button _btnTabWorkflow = null!;
        private Button _btnTabDataExample = null!;
        private Button _btnPrev = null!;
        private Button _btnNext = null!;
        private Label _lblPageIndicator = null!;

        private Panel _contentPanel = null!;
        private Panel _panelWorkflow = null!;
        private Panel _panelDataExample = null!;

        private PictureBox _picBoxWorkflow = null!;
        private Label _lblStepCard1Title = null!;
        private Label _lblStepCard1Desc = null!;
        private Label _lblStepCard2Title = null!;
        private Label _lblStepCard2Desc = null!;

        private readonly Image?[] _workflowImages = new Image?[3];

        private bool _isDragging;
        private Point _dragStartCursor;
        private Point _dragStartForm;

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
            BackColor = BackgroundColor;
            ClientSize = new Size(TotalFormWidth, TotalFormHeight);
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "ความช่วยเหลือ (Help)";
            KeyPreview = true;

            LoadWorkflowImages();

            Controls.Add(BuildHeader());
            Controls.Add(BuildToolbar());
            Controls.Add(BuildContentPanel());

            KeyDown += Form_KeyDown;
            SwitchTab(0);
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
            Rectangle borderBounds = new Rectangle(1, 1, Width - 3, Height - 3);
            using var borderPen = new Pen(Color.FromArgb(60, AccentColor), 1.5F);
            using var borderPath = CreateRoundedPath(borderBounds, 7);
            e.Graphics.DrawPath(borderPen, borderPath);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int i = 0; i < _workflowImages.Length; i++)
                {
                    _workflowImages[i]?.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void LoadWorkflowImages()
        {
            string[] imageFiles = new[]
            {
                "help_step1_2.png",
                "help_step3_4.png",
                "help_step5_6.png"
            };

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectResDir = @"c:\Primus\do by mine\DEF-03N\TEST_DEF-03N\TEST_DEF-03N\Resources";

            for (int i = 0; i < imageFiles.Length; i++)
            {
                string filename = imageFiles[i];
                string[] candidatePaths = new[]
                {
                    Path.Combine(baseDir, "Resources", filename),
                    Path.Combine(baseDir, filename),
                    Path.Combine(projectResDir, filename),
                    Path.Combine(baseDir, "Resources", "help_workflow.png"),
                    Path.Combine(projectResDir, "help_workflow.png")
                };

                foreach (var path in candidatePaths)
                {
                    if (File.Exists(path))
                    {
                        try
                        {
                            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            _workflowImages[i] = Image.FromStream(fs);
                            break;
                        }
                        catch { }
                    }
                }
            }
        }

        private Panel BuildHeader()
        {
            var header = new Panel
            {
                BackColor = HeaderColor,
                Dock = DockStyle.Top,
                Height = 52,
                Cursor = Cursors.SizeAll
            };

            var title = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "ความช่วยเหลือการใช้งาน (HELP)",
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.SizeAll
            };

            var closeButton = new Button
            {
                BackColor = DangerColor,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Size = new Size(52, 52),
                Text = "✕",
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            closeButton.FlatAppearance.BorderSize = 0;
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
                Location = new Point(18, 56),
                Size = new Size(TotalFormWidth - 36, 44),
                BackColor = BackgroundColor
            };

            // Tab 1 Button
            _btnTabWorkflow = new Button
            {
                Text = "1. วิธีการใช้งานโปรแกรม",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Size = new Size(220, 36),
                Location = new Point(0, 4),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnTabWorkflow.FlatAppearance.BorderSize = 0;
            _btnTabWorkflow.Click += (_, _) => SwitchTab(0);
            ApplyRoundedRegion(_btnTabWorkflow, 5);
            toolbar.Controls.Add(_btnTabWorkflow);

            // Tab 2 Button
            _btnTabDataExample = new Button
            {
                Text = "2. ตัวอย่างการกรอกข้อมูล",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Size = new Size(220, 36),
                Location = new Point(230, 4),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnTabDataExample.FlatAppearance.BorderSize = 0;
            _btnTabDataExample.Click += (_, _) => SwitchTab(1);
            ApplyRoundedRegion(_btnTabDataExample, 5);
            toolbar.Controls.Add(_btnTabDataExample);

            // Right side: Prev / Next / Indicator
            int rightX = toolbar.Width;

            _btnNext = new Button
            {
                Text = "ถัดไป ▶",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Size = new Size(88, 36),
                Location = new Point(rightX - 88, 4),
                BackColor = NavBtnBg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnNext.FlatAppearance.BorderSize = 0;
            _btnNext.Click += (_, _) =>
            {
                if (_currentTab == 0 && _currentWorkflowSlide < 2)
                {
                    UpdateWorkflowSlide(_currentWorkflowSlide + 1);
                }
            };
            ApplyRoundedRegion(_btnNext, 5);
            toolbar.Controls.Add(_btnNext);

            _lblPageIndicator = new Label
            {
                Text = "หน้า 1 / 3",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = ExampleTextColor,
                Size = new Size(80, 36),
                Location = new Point(rightX - 176, 4),
                TextAlign = ContentAlignment.MiddleCenter
            };
            toolbar.Controls.Add(_lblPageIndicator);

            _btnPrev = new Button
            {
                Text = "◀ ย้อนกลับ",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Size = new Size(95, 36),
                Location = new Point(rightX - 279, 4),
                BackColor = NavBtnBg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnPrev.FlatAppearance.BorderSize = 0;
            _btnPrev.Click += (_, _) =>
            {
                if (_currentTab == 0 && _currentWorkflowSlide > 0)
                {
                    UpdateWorkflowSlide(_currentWorkflowSlide - 1);
                }
            };
            ApplyRoundedRegion(_btnPrev, 5);
            toolbar.Controls.Add(_btnPrev);

            return toolbar;
        }

        private Panel BuildContentPanel()
        {
            _contentPanel = new Panel
            {
                Location = new Point(18, 104),
                Size = new Size(TotalFormWidth - 36, 560),
                BackColor = BackgroundColor
            };

            // Build Page 1: วิธีการใช้งานโปรแกรม
            _panelWorkflow = BuildWorkflowPage(_contentPanel.Width, _contentPanel.Height);
            _contentPanel.Controls.Add(_panelWorkflow);

            // Build Page 2: ตัวอย่างการกรอกข้อมูล
            _panelDataExample = BuildDataExamplePage(_contentPanel.Width, _contentPanel.Height);
            _contentPanel.Controls.Add(_panelDataExample);

            return _contentPanel;
        }

        private Panel BuildWorkflowPage(int width, int height)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BackgroundColor
            };

            // 1. Picture Container Card
            int picCardHeight = 445;
            var picCard = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(width, picCardHeight),
                BackColor = Color.White
            };
            ApplyRoundedRegion(picCard, 6);

            _picBoxWorkflow = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                Image = _workflowImages[0]
            };
            picCard.Controls.Add(_picBoxWorkflow);
            panel.Controls.Add(picCard);

            // 2. Step Summary Cards below image (2 cards: Left & Right)
            int stepCardY = picCardHeight + 10;
            int stepCardHeight = 95;
            int cardGap = 14;
            int stepCardWidth = (width - cardGap) / 2;

            // Step Card 1 (Left)
            var card1 = CreateStepSummaryCard(0, stepCardY, stepCardWidth, stepCardHeight, out _lblStepCard1Title, out _lblStepCard1Desc);
            panel.Controls.Add(card1);

            // Step Card 2 (Right)
            var card2 = CreateStepSummaryCard(stepCardWidth + cardGap, stepCardY, stepCardWidth, stepCardHeight, out _lblStepCard2Title, out _lblStepCard2Desc);
            panel.Controls.Add(card2);

            return panel;
        }

        private Panel CreateStepSummaryCard(int x, int y, int width, int height, out Label lblTitle, out Label lblDesc)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = CardBg
            };
            ApplyRoundedRegion(card, 5);

            lblTitle = new Label
            {
                Text = "",
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                ForeColor = AccentColor,
                Location = new Point(14, 8),
                Size = new Size(width - 28, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(lblTitle);

            lblDesc = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = SubTextColor,
                Location = new Point(14, 32),
                Size = new Size(width - 28, height - 38),
                TextAlign = ContentAlignment.TopLeft
            };
            card.Controls.Add(lblDesc);

            return card;
        }

        private Panel BuildDataExamplePage(int width, int height)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BackgroundColor
            };

            int y = 0;
            int cardHeight = 52;
            int gap = 8;

            // 1. S/N Card
            panel.Controls.Add(CreateHelpCard(
                "S/N : หมายเลขประจำเครื่องของอุปกรณ์ (Serial Number)",
                "ex. 67030001",
                0, y, width, cardHeight));
            y += cardHeight + gap;

            // 2. LOT Card
            panel.Controls.Add(CreateHelpCard(
                "LOT : หมายเลขล็อตการผลิตของอุปกรณ์ (LOT Number)",
                "ex. 2603/0726",
                0, y, width, cardHeight));
            y += cardHeight + gap;

            // 3. ECN Card
            panel.Controls.Add(CreateHelpCard(
                "ECN: หมายเลขเอกสารการเปลี่ยนแปลง (Engineering Change Notice)",
                "ex. 2603/0726",
                0, y, width, cardHeight));
            y += cardHeight + gap;

            // 4. Firmware Card
            panel.Controls.Add(CreateHelpCard(
                "Firmware : เวอร์ชันเฟิร์มแวร์ที่อุปกรณ์ใช้งาน",
                "ex. DEF-03N v1.00",
                0, y, width, cardHeight));
            y += cardHeight + gap;

            // 5. Revision Card
            panel.Controls.Add(CreateHelpCard(
                "Revision : หมายเลขการแก้ไขของเอกสารที่ใช้ในการทดสอบ",
                "ex. F-PD-07 Rev.6",
                0, y, width, cardHeight));
            y += cardHeight + gap;

            // 6. Equipment Group Card
            panel.Controls.Add(CreateEquipmentGroupCard(0, y, width, 192));

            return panel;
        }

        private Panel CreateHelpCard(string labelText, string exampleText, int x, int y, int width, int height)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = CardBg
            };
            ApplyRoundedRegion(card, 5);

            int exampleBoxX = width - ExampleBoxWidth - 14;
            int exampleBoxHeight = 34;
            int labelWidth = exampleBoxX - 16;

            var lbl = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(14, 0),
                Size = new Size(labelWidth, height),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = false
            };

            var exampleBox = CreateExampleBox(
                exampleText,
                exampleBoxX,
                (height - exampleBoxHeight) / 2,
                ExampleBoxWidth,
                exampleBoxHeight);

            card.Controls.Add(lbl);
            card.Controls.Add(exampleBox);
            return card;
        }

        private Panel CreateEquipmentGroupCard(int x, int y, int width, int height)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = CardBg
            };
            ApplyRoundedRegion(card, 5);

            var titleLbl = new Label
            {
                Text = "อุปกรณ์ที่ใช้ในการทดสอบ",
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                ForeColor = AccentColor,
                Location = new Point(14, 8),
                Size = new Size(350, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(titleLbl);

            string[] names = new[]
            {
                "Name : ชื่ออุปกรณ์ที่ใช้ทดสอบ",
                "Series : รุ่น/ซีรีส์ของอุปกรณ์",
                "Equipment ID : รหัสประจำอุปกรณ์ในระบบ/หน่วยงาน",
                "EXP : วันหมดอายุการสอบเทียบ (Expiration Date)"
            };

            string[] examples = new[]
            {
                "ex. OhmSource",
                "ex. OS-270",
                "ex. OS-274",
                "ex. 01/04/2027"
            };

            int exampleBoxX = width - ExampleBoxWidth - 14;
            int exampleBoxHeight = 32;
            int labelWidth = exampleBoxX - 16;

            int rowY = 38;
            for (int i = 0; i < names.Length; i++)
            {
                var lbl = new Label
                {
                    Text = names[i],
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                    ForeColor = TextColor,
                    Location = new Point(16, rowY),
                    Size = new Size(labelWidth, exampleBoxHeight),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                var exampleBox = CreateExampleBox(
                    examples[i],
                    exampleBoxX,
                    rowY,
                    ExampleBoxWidth,
                    exampleBoxHeight);

                card.Controls.Add(lbl);
                card.Controls.Add(exampleBox);
                rowY += 37;
            }

            return card;
        }

        private Panel CreateExampleBox(string text, int x, int y, int width, int height)
        {
            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = ExampleBoxBg
            };
            ApplyRoundedRegion(pnl, 4);

            var lbl = new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = ExampleTextColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            pnl.Controls.Add(lbl);
            return pnl;
        }

        private void SwitchTab(int tabIndex)
        {
            _currentTab = Math.Clamp(tabIndex, 0, 1);

            if (_currentTab == 0)
            {
                _panelWorkflow.Visible = true;
                _panelDataExample.Visible = false;

                _btnTabWorkflow.BackColor = AccentColor;
                _btnTabWorkflow.ForeColor = Color.FromArgb(7, 23, 35);

                _btnTabDataExample.BackColor = CardBg;
                _btnTabDataExample.ForeColor = ExampleTextColor;

                _lblPageIndicator.Visible = true;
                _btnPrev.Visible = true;
                _btnNext.Visible = true;

                UpdateWorkflowSlide(_currentWorkflowSlide);
            }
            else
            {
                _panelWorkflow.Visible = false;
                _panelDataExample.Visible = true;

                _btnTabWorkflow.BackColor = CardBg;
                _btnTabWorkflow.ForeColor = ExampleTextColor;

                _btnTabDataExample.BackColor = AccentColor;
                _btnTabDataExample.ForeColor = Color.FromArgb(7, 23, 35);

                // ในหน้าตัวอย่าง ให้ซ่อนปุ่มเปลี่ยนสไลด์ เพื่อให้สลับแท็บผ่านการคลิกเท่านั้น
                _lblPageIndicator.Visible = false;
                _btnPrev.Visible = false;
                _btnNext.Visible = false;
            }
        }

        private void UpdateWorkflowSlide(int slideIndex)
        {
            _currentWorkflowSlide = Math.Clamp(slideIndex, 0, 2);

            if (_workflowImages[_currentWorkflowSlide] != null)
            {
                _picBoxWorkflow.Image = _workflowImages[_currentWorkflowSlide];
            }

            switch (_currentWorkflowSlide)
            {
                case 0: // Steps 1 & 2
                    _lblStepCard1Title.Text = "1. Connect Port";
                    _lblStepCard1Desc.Text = "• เลือกพอร์ต Ohm Source, DEF-03N, PH-01\n• กดปุ่ม Connect เพื่อเชื่อมต่อพอร์ตสื่อสารทั้งหมด";

                    _lblStepCard2Title.Text = "2. Save Report (กรอกข้อมูลอุปกรณ์)";
                    _lblStepCard2Desc.Text = "• ระบุ S/N, LOT, ECN และเลือกโฟลเดอร์สำหรับเซฟ Report\n• กดปุ่ม 'รายละเอียด' เพื่อเช็คและตั้งค่าเครื่องมือทดสอบ";
                    break;

                case 1: // Steps 3 & 4
                    _lblStepCard1Title.Text = "3. เลือกโมเดลและแรงดัน (Model & Supply)";
                    _lblStepCard1Desc.Text = "• เลือกรุ่น: DEF-03N-A หรือ DEF-03N-A-B\n• เลือกแรงดัน Supply: 220 Volt หรือ 24 Volt";

                    _lblStepCard2Title.Text = "4. เลือกโหมดการทดสอบและกด Run";
                    _lblStepCard2Desc.Text = "• ติ๊กเลือกโหมด Calibration และ/หรือ Test For All\n• กดปุ่ม Run เพื่อเริ่มกระบวนการทดสอบอัตโนมัติ";
                    break;

                case 2: // Steps 5 & 6
                    _lblStepCard1Title.Text = "5. ล็อกอินเชื่อมต่อ Server (Login Primus)";
                    _lblStepCard1Desc.Text = "• คลิกปุ่ม 'Login...' มุมขวาบน กรอก IP, Port, Token, User/Pass\n• *สำหรับกรณีต้องการส่งผลเข้าฐานข้อมูลกลาง (ไม่ส่งสามารถข้ามได้)";

                    _lblStepCard2Title.Text = "6. ตรวจสอบสถานะ & ติ๊กเปิดส่งผล (Auto Upload)";
                    _lblStepCard2Desc.Text = "• สังเกตปุ่มมุมขวาบนเปลี่ยนเป็น 'Login OK'\n• ติ๊กถูกช่อง [✓] Server และดูสถานะส่งผลที่กล่อง MASSANGER";
                    break;
            }

            // Update Navigation state
            _lblPageIndicator.Text = $"หน้า {_currentWorkflowSlide + 1} / 3";
            _btnPrev.Enabled = _currentWorkflowSlide > 0;
            _btnPrev.ForeColor = _btnPrev.Enabled ? Color.White : Color.FromArgb(100, 120, 135);
            _btnNext.Enabled = _currentWorkflowSlide < 2;
            _btnNext.ForeColor = _btnNext.Enabled ? Color.White : Color.FromArgb(100, 120, 135);
        }

        private void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else if (e.KeyCode == Keys.Left)
            {
                if (_currentTab == 0 && _currentWorkflowSlide > 0)
                {
                    UpdateWorkflowSlide(_currentWorkflowSlide - 1);
                }
            }
            else if (e.KeyCode == Keys.Right)
            {
                if (_currentTab == 0 && _currentWorkflowSlide < 2)
                {
                    UpdateWorkflowSlide(_currentWorkflowSlide + 1);
                }
            }
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

        private void Header_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _isDragging = true;
            _dragStartCursor = Cursor.Position;
            _dragStartForm = Location;
        }

        private void Header_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
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
    }
}
