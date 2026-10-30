using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PH_08_Test
{
    public class FormSwitchIDTest : Form
    {
        private readonly Form1 _mainForm;
        private CancellationTokenSource? _cts;
        private bool _isTesting = false;

        // Modes: Normal (ปกติ) vs Diagnostic (หาตัวเสีย)
        public enum ModeTab { Normal, Diagnostic }
        private ModeTab _activeTab = ModeTab.Normal;

        // Normal Mode States
        public enum NormalStepState { Step1_Ready, Step1_Testing, Step2_Ready, Step2_Testing, Step3_Ready, Step3_Testing, Summary }
        private NormalStepState _normalStep = NormalStepState.Step1_Ready;
        public enum StepResult { Pending, Testing, Pass, Fail }
        private StepResult _normalStep1Result = StepResult.Pending;
        private StepResult _normalStep2Result = StepResult.Pending;
        private StepResult _normalStep3Result = StepResult.Pending;

        // Diagnostic Mode States
        public enum DiagStepState { Ready, Testing, Summary }
        private DiagStepState _diagState = DiagStepState.Ready;
        public enum DiagSwitchStatus { NotTested, Testing, Pass, Fail }
        private readonly DiagSwitchStatus[] _diagStatuses = new DiagSwitchStatus[8];
        private readonly int[] _singleIds = new int[] { 1, 2, 4, 8, 16, 32, 64, 128 };
        private int _diagActiveIndex = -1; // -1 = None

        // Countdown Timer
        private int _countdownSeconds = 5;
        private System.Windows.Forms.Timer? _countdownTimer;

        // DIP Switch Preview States (8 switches: true = ON, false = OFF)
        private readonly bool[] _previewSwitches = new bool[8];
        private string _instructionMain = "";
        private string _instructionDetail = "";

        // UI Controls
        private Button btnClose = null!;
        private Button btnTabNormal = null!;
        private Button btnTabDiag = null!;

        // Normal Mode Buttons
        private Button btnNormalTest1 = null!;
        private Button btnNormalTest2 = null!;
        private Button btnNormalTest3 = null!;
        private Button btnNormalReset = null!;
        private Button btnNormalRetest = null!;
        private Button btnNormalClose = null!;

        // Diagnostic Mode Buttons
        private readonly Button[] btnDiagTests = new Button[8];
        private Button btnDiagReset = null!;
        private Button btnDiagSummary = null!;
        private Button btnDiagConfirm = null!;

        // Dragging
        private Point _dragStart;

        // Normal Only Mode & Results
        public bool IsNormalOnly { get; set; } = false;
        public bool SwitchTestPassed => _normalStep1Result == StepResult.Pass && _normalStep2Result == StepResult.Pass && _normalStep3Result == StepResult.Pass;

        protected override CreateParams CreateParams => FormDropShadowHelper.ApplyCreateParams(base.CreateParams);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            FormDropShadowHelper.ApplyDropShadow(this);
        }

        public FormSwitchIDTest(Form1 mainForm, bool normalOnly = false)
        {
            _mainForm = mainForm;
            IsNormalOnly = normalOnly;
            InitializeComponentCustom();
            InitializeCountdownTimer();
            ResetNormalMode();
            ResetDiagMode();
        }

        private void InitializeCountdownTimer()
        {
            _countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _countdownTimer.Tick += (s, e) =>
            {
                if (_countdownSeconds > 0)
                {
                    _countdownSeconds--;
                    UpdateActiveButtonTimerText();
                    this.Invalidate();
                }
            };
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Switch ID Test";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(520, 780);
            this.BackColor = Color.White;
            this.DoubleBuffered = true;

            this.MouseDown += Form_MouseDown;

            // 1. Close Button (Top-Right)
            btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(248, 250, 252),
                Size = new Size(32, 30),
                Location = new Point(480, 8),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 226, 226);
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(239, 68, 68);
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(100, 116, 139);
            btnClose.Click += (s, e) => { StopActiveTest(); this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnClose);

            // 2. Top Tabs (ปกติ / หาตัวเสีย)
            btnTabNormal = new Button
            {
                Text = IsNormalOnly ? "✔  ทดสอบ DIP Switch (โหมดปกติ)" : "✔  ปกติ",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Size = IsNormalOnly ? new Size(472, 44) : new Size(230, 44),
                Location = new Point(24, 46),
                FlatStyle = FlatStyle.Flat,
                Cursor = IsNormalOnly ? Cursors.Default : Cursors.Hand
            };
            if (!IsNormalOnly)
            {
                btnTabNormal.Click += (s, e) => SwitchTab(ModeTab.Normal);
            }
            this.Controls.Add(btnTabNormal);

            btnTabDiag = new Button
            {
                Text = "🔍  หาตัวเสีย",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Size = new Size(230, 44),
                Location = new Point(266, 46),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = !IsNormalOnly
            };
            btnTabDiag.Click += (s, e) => SwitchTab(ModeTab.Diagnostic);
            this.Controls.Add(btnTabDiag);

            // 3. Normal Mode Controls (Single Balanced Full-width Buttons)
            btnNormalTest1 = CreateActionButton("▷ ทดสอบ", new Point(40, 352), new Size(440, 38), Color.FromArgb(254, 240, 138), Color.FromArgb(15, 23, 42), (s, e) => StartNormalStep1());
            this.Controls.Add(btnNormalTest1);

            btnNormalTest2 = CreateActionButton("▷ ทดสอบ", new Point(40, 478), new Size(440, 38), Color.FromArgb(254, 240, 138), Color.FromArgb(15, 23, 42), (s, e) => StartNormalStep2());
            this.Controls.Add(btnNormalTest2);

            btnNormalTest3 = CreateActionButton("▷ ทดสอบคืนค่า ID 1", new Point(40, 604), new Size(440, 38), Color.FromArgb(254, 240, 138), Color.FromArgb(15, 23, 42), (s, e) => StartNormalStep3());
            this.Controls.Add(btnNormalTest3);

            btnNormalReset = CreateActionButton("🔄 Reset", new Point(24, 710), new Size(120, 46), Color.FromArgb(254, 240, 138), Color.FromArgb(15, 23, 42), (s, e) => ResetNormalMode());
            this.Controls.Add(btnNormalReset);

            btnNormalRetest = CreateActionButton("🔄 ทดสอบใหม่", new Point(24, 710), new Size(290, 46), Color.FromArgb(254, 240, 138), Color.FromArgb(15, 23, 42), (s, e) => ResetNormalMode());
            btnNormalClose = CreateActionButton("ปิด", new Point(326, 710), new Size(170, 46), Color.White, Color.FromArgb(15, 23, 42), (s, e) =>
            {
                this.DialogResult = SwitchTestPassed ? DialogResult.OK : DialogResult.Cancel;
                this.Close();
            });
            btnNormalClose.FlatAppearance.BorderSize = 1;
            btnNormalClose.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            this.Controls.Add(btnNormalRetest);
            this.Controls.Add(btnNormalClose);

            // 4. Diagnostic Mode Controls (8 Rows with balanced right buttons)
            int rowStartY = 206;
            int rowSpacing = 56;
            for (int i = 0; i < 8; i++)
            {
                int index = i;
                int y = rowStartY + (i * rowSpacing);

                btnDiagTests[i] = CreateActionButton("▷ ทดสอบ", new Point(348, y + 5), new Size(136, 38), Color.FromArgb(254, 240, 138), Color.FromArgb(15, 23, 42), (s, e) => StartDiagSingleTest(index));
                btnDiagTests[i].Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                this.Controls.Add(btnDiagTests[i]);
            }

            btnDiagReset = CreateActionButton("🔄 Reset", new Point(24, 710), new Size(120, 46), Color.FromArgb(254, 240, 138), Color.FromArgb(15, 23, 42), (s, e) => ResetDiagMode());
            this.Controls.Add(btnDiagReset);

            btnDiagSummary = CreateActionButton("📑 สรุป", new Point(156, 710), new Size(340, 46), Color.FromArgb(241, 245, 249), Color.FromArgb(148, 163, 184), (s, e) => OpenDiagSummaryView());
            this.Controls.Add(btnDiagSummary);

            btnDiagConfirm = CreateActionButton("✔ ตกลง", new Point(24, 680), new Size(472, 46), Color.FromArgb(254, 240, 138), Color.FromArgb(15, 23, 42), (s, e) => ResetDiagMode());
            this.Controls.Add(btnDiagConfirm);

            UpdateTabButtonStyles();
            ApplyVisibilityRules();
        }

        private Button CreateActionButton(string text, Point loc, Size sz, Color back, Color fore, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = sz,
                BackColor = back,
                ForeColor = fore,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private void SwitchTab(ModeTab tab)
        {
            if (_isTesting) return; // Locked during testing
            _activeTab = tab;
            UpdateTabButtonStyles();

            // Restore DIP Switch Preview state for the selected tab
            if (_activeTab == ModeTab.Normal)
            {
                if (_normalStep == NormalStepState.Step2_Ready || _normalStep == NormalStepState.Step2_Testing)
                {
                    SetPreviewDIP(new bool[] { false, true, true, true, true, true, true, true }, "สับ SW1 เป็น OFF", "SW2-SW8 เป็น ON");
                }
                else
                {
                    SetPreviewDIP(new bool[] { true, false, false, false, false, false, false, false }, "สับ SW1 เป็น ON", "SW2-SW8 เป็น OFF");
                }
            }

            ApplyVisibilityRules();
            this.Invalidate();
        }

        private void UpdateTabButtonStyles()
        {
            if (_activeTab == ModeTab.Normal)
            {
                btnTabNormal.BackColor = Color.FromArgb(254, 240, 138); // Yellow #FEF08A
                btnTabNormal.ForeColor = Color.FromArgb(15, 23, 42);
                btnTabNormal.FlatAppearance.BorderSize = 1;
                btnTabNormal.FlatAppearance.BorderColor = Color.FromArgb(234, 179, 8);

                btnTabDiag.BackColor = Color.FromArgb(248, 250, 252);
                btnTabDiag.ForeColor = Color.FromArgb(100, 116, 139);
                btnTabDiag.FlatAppearance.BorderSize = 1;
                btnTabDiag.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            }
            else
            {
                btnTabDiag.BackColor = Color.FromArgb(254, 240, 138); // Yellow
                btnTabDiag.ForeColor = Color.FromArgb(15, 23, 42);
                btnTabDiag.FlatAppearance.BorderSize = 1;
                btnTabDiag.FlatAppearance.BorderColor = Color.FromArgb(234, 179, 8);

                btnTabNormal.BackColor = Color.FromArgb(248, 250, 252);
                btnTabNormal.ForeColor = Color.FromArgb(100, 116, 139);
                btnTabNormal.FlatAppearance.BorderSize = 1;
                btnTabNormal.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            }
        }

        private void UpdateActiveButtonTimerText()
        {
            if (_activeTab == ModeTab.Normal)
            {
                if (_normalStep == NormalStepState.Step1_Testing)
                {
                    btnNormalTest1.Text = $"↻ กำลังรอสัญญาณ... ({_countdownSeconds}s)";
                }
                else if (_normalStep == NormalStepState.Step2_Testing)
                {
                    btnNormalTest2.Text = $"↻ กำลังรอสัญญาณ... ({_countdownSeconds}s)";
                }
                else if (_normalStep == NormalStepState.Step3_Testing)
                {
                    btnNormalTest3.Text = "↻ กำลังรอสัญญาณ ID 1... (สับ SW1 เป็น ON)";
                }
            }
            else
            {
                if (_diagState == DiagStepState.Testing && _diagActiveIndex >= 0)
                {
                    btnDiagTests[_diagActiveIndex].Text = $"↻ รอ... ({_countdownSeconds}s)";
                }
            }
        }

        #region Normal Mode Flow & Handlers

        private void ResetNormalMode()
        {
            StopActiveTest();
            _normalStep = NormalStepState.Step1_Ready;
            _normalStep1Result = StepResult.Pending;
            _normalStep2Result = StepResult.Pending;
            _normalStep3Result = StepResult.Pending;
            SetPreviewDIP(new bool[] { true, false, false, false, false, false, false, false }, "สับ SW1 เป็น ON", "SW2-SW8 เป็น OFF");
            ApplyVisibilityRules();
            this.Invalidate();
        }

        private async void StartNormalStep1()
        {
            if (_isTesting) return;
            _isTesting = true;
            _normalStep = NormalStepState.Step1_Testing;
            _normalStep1Result = StepResult.Testing;
            _countdownSeconds = 5;

            ApplyVisibilityRules();
            this.Invalidate();

            _countdownTimer?.Start();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            bool pass = await PollForIdAsync(1, 5, token);
            _countdownTimer?.Stop();
            _isTesting = false;

            if (pass)
            {
                _normalStep1Result = StepResult.Pass;
                _normalStep = NormalStepState.Step2_Ready;
                SetPreviewDIP(new bool[] { false, true, true, true, true, true, true, true }, "สับ SW1 เป็น OFF", "SW2-SW8 เป็น ON");
            }
            else
            {
                _normalStep1Result = StepResult.Fail;
                if (IsNormalOnly)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return;
                }
                _normalStep = NormalStepState.Summary;
            }

            ApplyVisibilityRules();
            this.Invalidate();
        }

        private async void StartNormalStep2()
        {
            if (_isTesting) return;
            _isTesting = true;
            _normalStep = NormalStepState.Step2_Testing;
            _normalStep2Result = StepResult.Testing;
            _countdownSeconds = 5;

            ApplyVisibilityRules();
            this.Invalidate();

            _countdownTimer?.Start();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            bool pass = await PollForIdAsync(254, 5, token);
            _countdownTimer?.Stop();
            _isTesting = false;

            if (pass)
            {
                _normalStep2Result = StepResult.Pass;
                _normalStep = NormalStepState.Step3_Ready;
                SetPreviewDIP(new bool[] { true, false, false, false, false, false, false, false }, "สับ SW1 เป็น ON (คืนค่า ID 1)", "SW2-SW8 เป็น OFF");
                ApplyVisibilityRules();
                this.Invalidate();

                // เข้าสู่ขั้นตอนที่ 3: ตรวจจับการคืนค่า ID 1 อัตโนมัติทันที
                StartNormalStep3();
                return;
            }
            else
            {
                _normalStep2Result = StepResult.Fail;
                if (IsNormalOnly)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return;
                }
            }

            _normalStep = NormalStepState.Summary;
            ApplyVisibilityRules();
            this.Invalidate();
        }

        private async void StartNormalStep3()
        {
            if (_isTesting) return;
            _isTesting = true;
            _normalStep = NormalStepState.Step3_Testing;
            _normalStep3Result = StepResult.Testing;

            ApplyVisibilityRules();
            this.Invalidate();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            // ส่ง maxSeconds = 0 เพื่อไม่จำกัดเวลา (รอจนกว่าจะสับสวิตช์กลับเป็น ID 1 หรือกดยกเลิก)
            bool pass = await PollForIdAsync(1, 0, token);
            _isTesting = false;

            if (pass)
            {
                _normalStep3Result = StepResult.Pass;
                ApplyVisibilityRules();
                this.Invalidate();

                if (IsNormalOnly)
                {
                    await Task.Delay(800);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }
            }
            else
            {
                _normalStep3Result = StepResult.Fail;
                _normalStep = NormalStepState.Step3_Ready;
            }

            ApplyVisibilityRules();
            this.Invalidate();
        }

        #endregion

        #region Diagnostic Mode Flow & Handlers

        private void ResetDiagMode()
        {
            StopActiveTest();
            _diagState = DiagStepState.Ready;
            _diagActiveIndex = -1;
            for (int i = 0; i < 8; i++)
            {
                _diagStatuses[i] = DiagSwitchStatus.NotTested;
            }
            ApplyVisibilityRules();
            this.Invalidate();
        }

        private async void StartDiagSingleTest(int index)
        {
            if (_isTesting) return;
            _isTesting = true;
            _diagState = DiagStepState.Testing;
            _diagActiveIndex = index;
            _diagStatuses[index] = DiagSwitchStatus.Testing;
            _countdownSeconds = 5;

            ApplyVisibilityRules();
            this.Invalidate();

            _countdownTimer?.Start();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            byte targetId = (byte)_singleIds[index];
            bool pass = await PollForIdAsync(targetId, 5, token);
            _countdownTimer?.Stop();
            _isTesting = false;

            _diagStatuses[index] = pass ? DiagSwitchStatus.Pass : DiagSwitchStatus.Fail;
            _diagState = DiagStepState.Ready;
            _diagActiveIndex = -1;

            ApplyVisibilityRules();
            this.Invalidate();
        }

        private void OpenDiagSummaryView()
        {
            int testedCount = _diagStatuses.Count(x => x == DiagSwitchStatus.Pass || x == DiagSwitchStatus.Fail);
            if (testedCount == 0) return;

            _diagState = DiagStepState.Summary;
            ApplyVisibilityRules();
            this.Invalidate();
        }

        #endregion

        private void SetPreviewDIP(bool[] arr, string main, string detail)
        {
            for (int i = 0; i < 8 && i < arr.Length; i++)
            {
                _previewSwitches[i] = arr[i];
            }
            _instructionMain = main;
            _instructionDetail = detail;
        }

        private void StopActiveTest()
        {
            _isTesting = false;
            _countdownTimer?.Stop();
            _cts?.Cancel();
            _cts = null;
        }

        private async Task<bool> PollForIdAsync(byte slaveId, int maxSeconds, CancellationToken token, int requiredConsecutiveHits = 3)
        {
            var timeoutTask = maxSeconds > 0 ? Task.Delay(maxSeconds * 1000, token) : null;
            int consecutiveHits = 0;

            while ((timeoutTask == null || !timeoutTask.IsCompleted) && !token.IsCancellationRequested)
            {
                bool isAlive = await _mainForm.TestPH08SlaveIdDualBlockResponseAsync(slaveId, 300);
                if (isAlive)
                {
                    consecutiveHits++;
                    if (consecutiveHits >= requiredConsecutiveHits)
                    {
                        return true;
                    }
                }
                else
                {
                    consecutiveHits = 0;
                }
                await Task.Delay(80, token);
            }
            return false;
        }

        private void ApplyVisibilityRules()
        {
            bool isNormal = _activeTab == ModeTab.Normal;

            // Tabs lock
            btnTabNormal.Enabled = !_isTesting;
            btnTabDiag.Enabled = !_isTesting;

            // Hide all by default
            btnNormalTest1.Visible = false;
            btnNormalTest2.Visible = false;
            btnNormalTest3.Visible = false;
            btnNormalReset.Visible = false;
            btnNormalRetest.Visible = false;
            btnNormalClose.Visible = false;

            for (int i = 0; i < 8; i++)
            {
                btnDiagTests[i].Visible = false;
            }
            btnDiagReset.Visible = false;
            btnDiagSummary.Visible = false;
            btnDiagConfirm.Visible = false;

            if (isNormal)
            {
                if (_normalStep == NormalStepState.Summary)
                {
                    btnNormalRetest.Visible = true;
                    btnNormalClose.Visible = true;
                }
                else
                {
                    btnNormalReset.Visible = true;
                    btnNormalReset.Enabled = !_isTesting;

                    if (_normalStep == NormalStepState.Step1_Ready)
                    {
                        btnNormalTest1.Visible = true;
                        btnNormalTest1.Enabled = true;
                        btnNormalTest1.Text = "▷ ทดสอบ";
                        btnNormalTest1.BackColor = Color.FromArgb(254, 240, 138);
                        btnNormalTest1.ForeColor = Color.FromArgb(15, 23, 42);

                        btnNormalTest2.Visible = true;
                        btnNormalTest2.Enabled = false;
                        btnNormalTest2.BackColor = Color.FromArgb(241, 245, 249);
                        btnNormalTest2.ForeColor = Color.FromArgb(148, 163, 184);

                        btnNormalTest3.Visible = true;
                        btnNormalTest3.Enabled = false;
                        btnNormalTest3.BackColor = Color.FromArgb(241, 245, 249);
                        btnNormalTest3.ForeColor = Color.FromArgb(148, 163, 184);
                    }
                    else if (_normalStep == NormalStepState.Step1_Testing)
                    {
                        btnNormalTest1.Visible = true;
                        btnNormalTest1.Enabled = false;
                        btnNormalTest1.Text = $"↻ กำลังรอสัญญาณ... ({_countdownSeconds}s)";
                        btnNormalTest1.BackColor = Color.FromArgb(254, 252, 232);
                        btnNormalTest1.ForeColor = Color.FromArgb(15, 23, 42);

                        btnNormalTest2.Visible = true;
                        btnNormalTest2.Enabled = false;
                        btnNormalTest2.BackColor = Color.FromArgb(241, 245, 249);
                        btnNormalTest2.ForeColor = Color.FromArgb(148, 163, 184);

                        btnNormalTest3.Visible = true;
                        btnNormalTest3.Enabled = false;
                        btnNormalTest3.BackColor = Color.FromArgb(241, 245, 249);
                        btnNormalTest3.ForeColor = Color.FromArgb(148, 163, 184);
                    }
                    else if (_normalStep == NormalStepState.Step2_Ready)
                    {
                        btnNormalTest1.Visible = false;

                        btnNormalTest2.Visible = true;
                        btnNormalTest2.Enabled = true;
                        btnNormalTest2.Text = "▷ ทดสอบ";
                        btnNormalTest2.BackColor = Color.FromArgb(254, 240, 138);
                        btnNormalTest2.ForeColor = Color.FromArgb(15, 23, 42);

                        btnNormalTest3.Visible = true;
                        btnNormalTest3.Enabled = false;
                        btnNormalTest3.BackColor = Color.FromArgb(241, 245, 249);
                        btnNormalTest3.ForeColor = Color.FromArgb(148, 163, 184);
                    }
                    else if (_normalStep == NormalStepState.Step2_Testing)
                    {
                        btnNormalTest1.Visible = false;

                        btnNormalTest2.Visible = true;
                        btnNormalTest2.Enabled = false;
                        btnNormalTest2.Text = $"↻ กำลังรอสัญญาณ... ({_countdownSeconds}s)";
                        btnNormalTest2.BackColor = Color.FromArgb(254, 252, 232);
                        btnNormalTest2.ForeColor = Color.FromArgb(15, 23, 42);

                        btnNormalTest3.Visible = true;
                        btnNormalTest3.Enabled = false;
                        btnNormalTest3.BackColor = Color.FromArgb(241, 245, 249);
                        btnNormalTest3.ForeColor = Color.FromArgb(148, 163, 184);
                    }
                    else if (_normalStep == NormalStepState.Step3_Ready)
                    {
                        btnNormalTest1.Visible = false;
                        btnNormalTest2.Visible = false;

                        btnNormalTest3.Visible = true;
                        btnNormalTest3.Enabled = true;
                        btnNormalTest3.Text = "▷ ตรวจสอบการคืนค่า ID 1 (ลองใหม่)";
                        btnNormalTest3.BackColor = Color.FromArgb(254, 240, 138);
                        btnNormalTest3.ForeColor = Color.FromArgb(15, 23, 42);
                    }
                    else if (_normalStep == NormalStepState.Step3_Testing)
                    {
                        btnNormalTest1.Visible = false;
                        btnNormalTest2.Visible = false;

                        btnNormalTest3.Visible = true;
                        btnNormalTest3.Enabled = false;
                        btnNormalTest3.Text = "↻ กำลังรอสัญญาณ ID 1... (สับ SW1 เป็น ON)";
                        btnNormalTest3.BackColor = Color.FromArgb(254, 252, 232);
                        btnNormalTest3.ForeColor = Color.FromArgb(15, 23, 42);
                    }
                }
            }
            else
            {
                // Diagnostic Mode
                if (_diagState == DiagStepState.Summary)
                {
                    btnDiagConfirm.Visible = true;
                }
                else
                {
                    btnDiagReset.Visible = true;
                    btnDiagSummary.Visible = true;
                    btnDiagReset.Enabled = !_isTesting;

                    int testedCount = _diagStatuses.Count(x => x == DiagSwitchStatus.Pass || x == DiagSwitchStatus.Fail);
                    btnDiagSummary.Enabled = !_isTesting && testedCount > 0;
                    btnDiagSummary.Text = testedCount > 0 ? $"📑 สรุป ({testedCount} ตัว)" : "📑 สรุป";
                    if (btnDiagSummary.Enabled)
                    {
                        btnDiagSummary.BackColor = Color.FromArgb(254, 240, 138);
                        btnDiagSummary.ForeColor = Color.FromArgb(15, 23, 42);
                    }
                    else
                    {
                        btnDiagSummary.BackColor = Color.FromArgb(241, 245, 249);
                        btnDiagSummary.ForeColor = Color.FromArgb(148, 163, 184);
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        btnDiagTests[i].Visible = true;

                        if (_diagState == DiagStepState.Testing)
                        {
                            if (i == _diagActiveIndex)
                            {
                                btnDiagTests[i].Enabled = false;
                                btnDiagTests[i].Text = $"↻ รอ... ({_countdownSeconds}s)";
                                btnDiagTests[i].BackColor = Color.FromArgb(254, 252, 232);
                                btnDiagTests[i].ForeColor = Color.FromArgb(15, 23, 42);
                            }
                            else
                            {
                                btnDiagTests[i].Enabled = false;
                                btnDiagTests[i].BackColor = Color.FromArgb(241, 245, 249);
                                btnDiagTests[i].ForeColor = Color.FromArgb(148, 163, 184);
                            }
                        }
                        else
                        {
                            btnDiagTests[i].Enabled = true;
                            btnDiagTests[i].BackColor = Color.FromArgb(254, 240, 138);
                            btnDiagTests[i].ForeColor = Color.FromArgb(15, 23, 42);

                            if (_diagStatuses[i] == DiagSwitchStatus.NotTested)
                            {
                                btnDiagTests[i].Text = "▷ ทดสอบ";
                            }
                            else
                            {
                                btnDiagTests[i].Text = "▷ ซ้ำ";
                            }
                        }
                    }
                }
            }
        }

        #region Custom Painting

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

            // Form Title Bar Text
            using (var titleFont = new Font("Segoe UI", 10.5F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("Switch ID Test", titleFont, brush, 24, 12);
            }

            // Lock warning under tabs
            if (_isTesting)
            {
                using var lockFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                using var lockBrush = new SolidBrush(Color.FromArgb(100, 116, 139));
                string lockTxt = "(สลับโหมดไม่ได้ระหว่างทดสอบ)";
                var sz = g.MeasureString(lockTxt, lockFont);
                g.DrawString(lockTxt, lockFont, lockBrush, (this.Width - sz.Width) / 2, 94);
            }

            if (_activeTab == ModeTab.Normal)
            {
                DrawNormalMode(g);
            }
            else
            {
                DrawDiagnosticMode(g);
            }
        }

        private void DrawNormalMode(Graphics g)
        {
            if (_normalStep == NormalStepState.Summary)
            {
                DrawNormalSummary(g);
                return;
            }

            // Header (Ready / Testing..)
            string headerText = _isTesting ? "Testing.." : "Ready";
            using (var headerFont = new Font("Segoe UI", 16F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString(headerText, headerFont, brush, 24, 100);
            }

            // Progress text & 3-Segment Bar (Increased gap below Ready)
            int stepNum = _normalStep switch
            {
                NormalStepState.Step3_Ready or NormalStepState.Step3_Testing => 3,
                NormalStepState.Step2_Ready or NormalStepState.Step2_Testing => 2,
                _ => 1
            };
            using (var stepFont = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (var stepBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString($"ขั้นที่ {stepNum} จาก 3", stepFont, stepBrush, 24, 138);
            }

            // 3-Segment Progress Bar
            int barY = 146;
            int barW = 75;
            int barH = 5;

            // Segment 1 (Step 1)
            Color seg1Color = _normalStep1Result == StepResult.Pass ? Color.FromArgb(34, 197, 94) : Color.FromArgb(234, 179, 8);
            using (var b1 = new SolidBrush(seg1Color))
                g.FillRectangle(b1, 140, barY, barW, barH);

            // Segment 2 (Step 2)
            Color seg2Color = _normalStep2Result == StepResult.Pass ? Color.FromArgb(34, 197, 94) :
                              (stepNum >= 2 ? Color.FromArgb(234, 179, 8) : Color.FromArgb(226, 232, 240));
            using (var b2 = new SolidBrush(seg2Color))
                g.FillRectangle(b2, 222, barY, barW, barH);

            // Segment 3 (Step 3)
            Color seg3Color = _normalStep3Result == StepResult.Pass ? Color.FromArgb(34, 197, 94) :
                              (stepNum == 3 ? Color.FromArgb(234, 179, 8) : Color.FromArgb(226, 232, 240));
            using (var b3 = new SolidBrush(seg3Color))
                g.FillRectangle(b3, 304, barY, barW, barH);

            // DIP Switch Visual Box (Comfortable gap below progress bar)
            DrawDIPSwitchBox(g, new Rectangle(24, 170, 472, 110), _instructionMain, _instructionDetail, _previewSwitches);

            // Step 1 Card (Y: 290, H: 114)
            DrawStepCard(g, 1, new Rectangle(24, 290, 472, 114), _normalStep1Result, "ID 1",
                "Step 1 · ทดสอบ Switch 1",
                _normalStep1Result == StepResult.Pass ? "ID 1 ตอบกลับ · ผ่านแล้ว" :
                (_normalStep1Result == StepResult.Testing ? $"กำลังรอสัญญาณจาก ID 1... ({_countdownSeconds}s)" : "สับสวิตช์ตามด้านบน แล้วกด \"ทดสอบ\""));

            // Step 2 Card (Y: 416, H: 114)
            DrawStepCard(g, 2, new Rectangle(24, 416, 472, 114), _normalStep2Result, "ID 254",
                "Step 2 · ทดสอบ Switch 2-8",
                _normalStep2Result == StepResult.Pass ? "ID 254 ตอบกลับ · ผ่านแล้ว" :
                (_normalStep2Result == StepResult.Testing ? $"กำลังรอสัญญาณจาก ID 254... ({_countdownSeconds}s)" : (_normalStep1Result == StepResult.Pass ? "สับสวิตช์ตามด้านบน แล้วกด \"ทดสอบ\"" : "รอทดสอบ")));

            // Step 3 Card (Y: 542, H: 114)
            string step3Sub = _normalStep3Result switch
            {
                StepResult.Pass => "ตรวจพบ ID 1 แล้ว · คืนค่าโรงงานสำเร็จ",
                StepResult.Testing => "กรุณาสับสวิตช์กลับเป็น ID 1 (กำลังรอสัญญาณ...)",
                _ => (_normalStep2Result == StepResult.Pass ? "สับ SW1=ON, SW2-8=OFF แล้วกดตรวจจับ" : "รอทดสอบ")
            };
            DrawStepCard(g, 3, new Rectangle(24, 542, 472, 114), _normalStep3Result, "ID 1 (คืนค่า)",
                "Step 3 · คืนค่า Switch เป็น ID 1", step3Sub);

            // Footnote if testing
            if (_isTesting)
            {
                using var footFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                using var footBrush = new SolidBrush(Color.FromArgb(100, 116, 139));
                string note = _normalStep == NormalStepState.Step3_Testing
                    ? "ระบบกำลังรอการสับสวิตช์กลับเป็น ID 1 เพื่อคืนค่าโรงงาน (ไม่จำกัดเวลา)"
                    : $"ระบบจะนับถอยหลัง {_countdownSeconds} วินาที หากไม่มีการตอบกลับจะสรุปผลอัตโนมัติ";
                var sz = g.MeasureString(note, footFont);
                g.DrawString(note, footFont, footBrush, (this.Width - sz.Width) / 2, 668);
            }
        }

        private void DrawNormalSummary(Graphics g)
        {
            // Header
            using (var headerFont = new Font("Segoe UI", 16F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("สรุปผล", headerFont, brush, 24, 114);
            }

            bool allPass = _normalStep1Result == StepResult.Pass && _normalStep2Result == StepResult.Pass && _normalStep3Result == StepResult.Pass;

            // 1. Big Top Card (Generous height with 2-line detail text)
            Rectangle topCardRect = new Rectangle(24, 146, 472, 102);
            Color fillCard = allPass ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 242, 242);
            Color borderCard = allPass ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);

            using (var path = GetRoundedRectPath(topCardRect, 10))
            {
                using (var fb = new SolidBrush(fillCard))
                    g.FillPath(fb, path);
                using (var pb = new Pen(borderCard, 1.5f))
                    g.DrawPath(pb, path);
            }

            if (allPass)
            {
                DrawCircleBadge(g, 56, 197, 18, Color.FromArgb(34, 197, 94), "✔");
                using var boldFont = new Font("Segoe UI", 12F, FontStyle.Bold);
                using var subFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);
                using var textBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
                using var subBrush = new SolidBrush(Color.FromArgb(100, 116, 139));

                g.DrawString("ผ่านทั้งหมด", boldFont, textBrush, 88, 168);
                g.DrawString("Switch 1-8 ใช้งานได้ปกติ และคืนค่า ID 1 เรียบร้อย", subFont, subBrush, 88, 198);
            }
            else
            {
                DrawWarningIcon(g, 56, 197, 18);
                using var boldFont = new Font("Segoe UI", 12F, FontStyle.Bold);
                using var subFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);
                using var textBrush = new SolidBrush(Color.FromArgb(185, 28, 28));
                using var subBrush = new SolidBrush(Color.FromArgb(100, 116, 139));

                g.DrawString("ไม่ผ่าน", boldFont, textBrush, 88, 160);
                if (_normalStep1Result == StepResult.Fail)
                {
                    g.DrawString("Switch 1 ไม่ตอบกลับ (Timeout 5s)", subFont, subBrush, 88, 188);
                    g.DrawString("Switch 2-8 ยังไม่ได้ทดสอบ", subFont, subBrush, 88, 212);
                }
                else if (_normalStep2Result == StepResult.Fail)
                {
                    g.DrawString("พบปัญหาที่ Switch 2-8", subFont, subBrush, 88, 188);
                    g.DrawString("ID 254 ไม่ตอบกลับ (Timeout 5s)", subFont, subBrush, 88, 212);
                }
                else
                {
                    g.DrawString("ยังไม่ได้คืนค่า Switch กลับเป็น ID 1", subFont, subBrush, 88, 188);
                    g.DrawString("กรุณาสับ SW1=ON, SW2-8=OFF", subFont, subBrush, 88, 212);
                }
            }

            // 2. 8-Badges Indicator Row (Y: 264)
            int badgeSpacing = 56;
            for (int i = 0; i < 8; i++)
            {
                int bx = 36 + (i * badgeSpacing);
                int by = 264;

                StepResult res = (i == 0) ? _normalStep1Result : _normalStep2Result;
                Color badgeBg;
                string symbol;

                if (res == StepResult.Pass)
                {
                    badgeBg = Color.FromArgb(34, 197, 94);
                    symbol = "✔";
                }
                else if (res == StepResult.Fail)
                {
                    badgeBg = Color.FromArgb(239, 68, 68);
                    symbol = "✕";
                }
                else
                {
                    // Pending (ยังไม่ได้ทดสอบ)
                    badgeBg = Color.FromArgb(203, 213, 225);
                    symbol = "🕒";
                }

                DrawCircleBadge(g, bx + 14, by + 14, 14, badgeBg, symbol);

                using var lblFont = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                using var lblBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
                string swLbl = $"SW{i + 1}";
                var lsz = g.MeasureString(swLbl, lblFont);
                g.DrawString(swLbl, lblFont, lblBrush, bx + 14 - (lsz.Width / 2), by + 36);
            }

            // 3. Detailed Results Cards
            // Step 1 summary card
            Rectangle card1 = new Rectangle(24, 340, 472, 64);
            DrawSummaryCardRow(g, card1, _normalStep1Result, "Step 1 · ทดสอบ Switch 1",
                _normalStep1Result == StepResult.Pass ? "ID 1 ตอบกลับ · ผ่านแล้ว" : "ID 1 ไม่ตอบกลับ (Timeout 5s)");

            // Step 2 summary card
            Rectangle card2 = new Rectangle(24, 412, 472, 64);
            string step2Desc;
            if (_normalStep2Result == StepResult.Pass)
            {
                step2Desc = "ID 254 ตอบกลับ · ผ่านแล้ว";
            }
            else if (_normalStep2Result == StepResult.Fail)
            {
                step2Desc = "ID 254 ไม่ตอบกลับ (Timeout 5s)";
            }
            else
            {
                step2Desc = "ยังไม่ได้ทดสอบ (เนื่องจาก Step 1 ไม่ผ่าน)";
            }
            DrawSummaryCardRow(g, card2, _normalStep2Result, "Step 2 · ทดสอบ Switch 2-8", step2Desc);

            // Step 3 summary card
            Rectangle card3 = new Rectangle(24, 484, 472, 64);
            string step3Desc;
            if (_normalStep3Result == StepResult.Pass)
            {
                step3Desc = "สับสวิตช์กลับเป็น ID 1 เรียบร้อย (SW1=ON, SW2-8=OFF)";
            }
            else if (_normalStep3Result == StepResult.Fail)
            {
                step3Desc = "ยังไม่ได้สับกลับ หรือไม่พบสัญญาณจาก ID 1";
            }
            else
            {
                step3Desc = "ยังไม่ได้ดำเนินการ";
            }
            DrawSummaryCardRow(g, card3, _normalStep3Result, "Step 3 · คืนค่า Switch เป็น ID 1", step3Desc);
        }

        private void DrawDiagnosticMode(Graphics g)
        {
            if (_diagState == DiagStepState.Summary)
            {
                DrawDiagSummary(g);
                return;
            }

            // Header (Ready / Testing..)
            string headerText = _isTesting ? "Testing.." : "Ready";
            using (var headerFont = new Font("Segoe UI", 16F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString(headerText, headerFont, brush, 24, 104);
            }

            // Top Notice Banner (Fixed height 46px, comfortable gap below Ready header)
            Rectangle bannerRect = new Rectangle(24, 148, 472, 46);
            using (var path = GetRoundedRectPath(bannerRect, 8))
            {
                using var fb = new SolidBrush(Color.FromArgb(254, 252, 232)); // Light Yellow
                using var pb = new Pen(Color.FromArgb(250, 204, 21), 1f);
                g.FillPath(fb, path);
                g.DrawPath(pb, path);
            }
            using (var bFont = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (var bBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                string msg;
                if (_diagState == DiagStepState.Testing && _diagActiveIndex >= 0)
                {
                    msg = $"กำลังทดสอบ SW{_diagActiveIndex + 1} (สับ SW{_diagActiveIndex + 1} เป็น ON / ที่เหลือ OFF)";
                }
                else
                {
                    msg = "เลือกสวิตช์ที่ต้องการทดสอบ แล้วกด \"ทดสอบ\" ข้างสวิตช์นั้น";
                }
                var bsz = g.MeasureString(msg, bFont);
                g.DrawString(msg, bFont, bBrush, (this.Width - bsz.Width) / 2, 161);
            }

            // 8 Rows of Switches
            int rowStartY = 206;
            int rowSpacing = 56;
            for (int i = 0; i < 8; i++)
            {
                int y = rowStartY + (i * rowSpacing);
                DrawDiagRowItem(g, i, new Rectangle(24, y, 472, 48));
            }
        }

        private void DrawDiagRowItem(Graphics g, int index, Rectangle rect)
        {
            DiagSwitchStatus status = _diagStatuses[index];
            bool isCurrentTesting = (_diagState == DiagStepState.Testing && _diagActiveIndex == index);

            Color fillColor = Color.FromArgb(248, 250, 252);
            Color borderColor = Color.FromArgb(226, 232, 240);

            if (isCurrentTesting)
            {
                fillColor = Color.FromArgb(254, 252, 232);
                borderColor = Color.FromArgb(234, 179, 8);
            }
            else if (status == DiagSwitchStatus.Pass)
            {
                fillColor = Color.FromArgb(240, 253, 244);
                borderColor = Color.FromArgb(34, 197, 94);
            }
            else if (status == DiagSwitchStatus.Fail)
            {
                fillColor = Color.FromArgb(254, 242, 242);
                borderColor = Color.FromArgb(239, 68, 68);
            }

            using (var path = GetRoundedRectPath(rect, 8))
            {
                using var fb = new SolidBrush(fillColor);
                using var pb = new Pen(borderColor, 1.5f);
                g.FillPath(fb, path);
                g.DrawPath(pb, path);
            }

            // Status Icon
            int iconX = rect.X + 22;
            int iconY = rect.Y + (rect.Height / 2);

            if (isCurrentTesting)
            {
                DrawCircleBadge(g, iconX, iconY, 12, Color.FromArgb(234, 179, 8), "↻");
            }
            else if (status == DiagSwitchStatus.Pass)
            {
                DrawCircleBadge(g, iconX, iconY, 12, Color.FromArgb(34, 197, 94), "✔");
            }
            else if (status == DiagSwitchStatus.Fail)
            {
                DrawCircleBadge(g, iconX, iconY, 12, Color.FromArgb(239, 68, 68), "✕");
            }
            else
            {
                DrawCircleBadge(g, iconX, iconY, 12, Color.FromArgb(203, 213, 225), "🕒");
            }

            // Text info
            using var nameFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            using var textBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
            using var subBrush = new SolidBrush(Color.FromArgb(100, 116, 139));

            g.DrawString($"SW{index + 1}", nameFont, textBrush, rect.X + 44, rect.Y + 8);

            string stateDesc = status switch
            {
                DiagSwitchStatus.Pass => "ผ่าน",
                DiagSwitchStatus.Fail => "ไม่ผ่าน (Timeout 5s)",
                DiagSwitchStatus.Testing => $"กำลังรอ... ({_countdownSeconds}s)",
                _ => "รอทดสอบ"
            };

            g.DrawString($"ID {_singleIds[index]} · {stateDesc}", subFont, subBrush, rect.X + 44, rect.Y + 28);
        }

        private void DrawDiagSummary(Graphics g)
        {
            // Header
            using (var headerFont = new Font("Segoe UI", 16F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("สรุปผล", headerFont, brush, 24, 114);
            }

            int passCount = _diagStatuses.Count(x => x == DiagSwitchStatus.Pass);
            int failCount = _diagStatuses.Count(x => x == DiagSwitchStatus.Fail);
            int totalTested = passCount + failCount;
            int untestedCount = 8 - totalTested;

            // 1. Top Warning/Pass Card
            Rectangle topCard = new Rectangle(24, 150, 472, 68);
            Color bgCard = failCount > 0 ? Color.FromArgb(254, 242, 242) : Color.FromArgb(240, 253, 244);
            Color bdCard = failCount > 0 ? Color.FromArgb(239, 68, 68) : Color.FromArgb(34, 197, 94);

            using (var path = GetRoundedRectPath(topCard, 8))
            {
                using var fb = new SolidBrush(bgCard);
                using var pb = new Pen(bdCard, 1.5f);
                g.FillPath(fb, path);
                g.DrawPath(pb, path);
            }

            if (failCount > 0)
            {
                DrawWarningIcon(g, 50, 184, 15);
                using var bf = new Font("Segoe UI", 10.5F, FontStyle.Bold);
                using var sf = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                using var tf = new SolidBrush(Color.FromArgb(185, 28, 28));
                using var mf = new SolidBrush(Color.FromArgb(100, 116, 139));

                g.DrawString($"พบตัวที่ไม่ผ่าน {failCount} ตัว", bf, tf, 78, 162);
                g.DrawString($"จากที่ทดสอบ {totalTested} ตัว (Timeout 5s)", sf, mf, 78, 186);
            }
            else
            {
                DrawCircleBadge(g, 50, 184, 15, Color.FromArgb(34, 197, 94), "✔");
                using var bf = new Font("Segoe UI", 10.5F, FontStyle.Bold);
                using var sf = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                using var tf = new SolidBrush(Color.FromArgb(15, 23, 42));
                using var mf = new SolidBrush(Color.FromArgb(100, 116, 139));

                g.DrawString($"ผ่านทุกตัวที่ทดสอบ ({passCount} ตัว)", bf, tf, 78, 162);
                g.DrawString("สวิตช์ที่ตรวจสอบทำงานได้ปกติทั้งหมด", sf, mf, 78, 186);
            }

            // 2. 3 Stat Cards in a Row (Y: 228, H: 60)
            int boxW = 150;
            int boxH = 60;
            int boxY = 228;

            // Box 1: Pass
            DrawStatBox(g, new Rectangle(24, boxY, boxW, boxH), passCount.ToString(), "ผ่าน", Color.FromArgb(240, 253, 244), Color.FromArgb(34, 197, 94), Color.FromArgb(22, 101, 52));
            // Box 2: Fail
            DrawStatBox(g, new Rectangle(185, boxY, boxW, boxH), failCount.ToString(), "ไม่ผ่าน", Color.FromArgb(254, 242, 242), Color.FromArgb(239, 68, 68), Color.FromArgb(185, 28, 28));
            // Box 3: Untested
            DrawStatBox(g, new Rectangle(346, boxY, boxW, boxH), untestedCount.ToString(), "ไม่ได้ทดสอบ", Color.FromArgb(248, 250, 252), Color.FromArgb(203, 213, 225), Color.FromArgb(100, 116, 139));

            // 3. List of Tested Switch Cards
            int listY = 302;
            var testedIndices = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                if (_diagStatuses[i] == DiagSwitchStatus.Pass || _diagStatuses[i] == DiagSwitchStatus.Fail)
                {
                    testedIndices.Add(i);
                }
            }

            for (int idx = 0; idx < testedIndices.Count && idx < 5; idx++)
            {
                int swIdx = testedIndices[idx];
                bool isPass = _diagStatuses[swIdx] == DiagSwitchStatus.Pass;
                Rectangle rCard = new Rectangle(24, listY + (idx * 56), 472, 48);

                using (var path = GetRoundedRectPath(rCard, 8))
                {
                    Color bg = isPass ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 242, 242);
                    Color bd = isPass ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
                    using var fb = new SolidBrush(bg);
                    using var pb = new Pen(bd, 1.5f);
                    g.FillPath(fb, path);
                    g.DrawPath(pb, path);
                }

                DrawCircleBadge(g, rCard.X + 22, rCard.Y + 24, 12, isPass ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68), isPass ? "✔" : "✕");

                using var nf = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                using var tf = new SolidBrush(Color.FromArgb(15, 23, 42));
                g.DrawString($"SW{swIdx + 1}", nf, tf, rCard.X + 44, rCard.Y + 8);

                using var sf = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                using var subBrush = new SolidBrush(Color.FromArgb(100, 116, 139));
                g.DrawString($"ID {_singleIds[swIdx]}", sf, subBrush, rCard.X + 44, rCard.Y + 26);

                // Right status text
                using var rf = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                using var rBrush = new SolidBrush(isPass ? Color.FromArgb(22, 101, 52) : Color.FromArgb(185, 28, 28));
                string rText = isPass ? "ผ่าน" : "ไม่ผ่าน";
                var rsz = g.MeasureString(rText, rf);
                g.DrawString(rText, rf, rBrush, rCard.Right - rsz.Width - 18, rCard.Y + 14);
            }

            // Untested text at bottom
            var untestedList = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                if (_diagStatuses[i] == DiagSwitchStatus.NotTested)
                {
                    untestedList.Add($"SW{i + 1}");
                }
            }

            using var unFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            using var unBrush = new SolidBrush(Color.FromArgb(100, 116, 139));
            string unText = untestedList.Count > 0 ? $"ไม่ได้ทดสอบ: {string.Join(", ", untestedList)}" : "ทดสอบครบทุกตัว 100%";
            var usz = g.MeasureString(unText, unFont);
            g.DrawString(unText, unFont, unBrush, (this.Width - usz.Width) / 2, 636);

            // Subtext below Confirm button
            string subConfirm = "กด \"ตกลง\" แล้วโปรแกรมจะ Reset ล้างผลให้เอง";
            var csz = g.MeasureString(subConfirm, unFont);
            g.DrawString(subConfirm, unFont, unBrush, (this.Width - csz.Width) / 2, 738);
        }

        private void DrawStatBox(Graphics g, Rectangle rect, string value, string label, Color fill, Color border, Color textCol)
        {
            using (var path = GetRoundedRectPath(rect, 8))
            {
                using var fb = new SolidBrush(fill);
                using var pb = new Pen(border, 1.5f);
                g.FillPath(fb, path);
                g.DrawPath(pb, path);
            }

            using var vf = new Font("Segoe UI", 12F, FontStyle.Bold);
            using var lf = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            using var vb = new SolidBrush(textCol);
            using var lb = new SolidBrush(Color.FromArgb(100, 116, 139));

            var vsz = g.MeasureString(value, vf);
            var lsz = g.MeasureString(label, lf);

            g.DrawString(value, vf, vb, rect.X + (rect.Width - vsz.Width) / 2, rect.Y + 8);
            g.DrawString(label, lf, lb, rect.X + (rect.Width - lsz.Width) / 2, rect.Y + 32);
        }

        private void DrawSummaryCardRow(Graphics g, Rectangle rect, StepResult result, string title, string sub)
        {
            Color fill = Color.FromArgb(248, 250, 252);
            Color border = Color.FromArgb(226, 232, 240);
            Color badgeBg = Color.FromArgb(203, 213, 225);
            string symbol = "🕒";

            if (result == StepResult.Pass)
            {
                fill = Color.FromArgb(240, 253, 244);
                border = Color.FromArgb(34, 197, 94);
                badgeBg = Color.FromArgb(34, 197, 94);
                symbol = "✔";
            }
            else if (result == StepResult.Fail)
            {
                fill = Color.FromArgb(254, 242, 242);
                border = Color.FromArgb(239, 68, 68);
                badgeBg = Color.FromArgb(239, 68, 68);
                symbol = "✕";
            }

            using (var path = GetRoundedRectPath(rect, 8))
            {
                using var fb = new SolidBrush(fill);
                using var pb = new Pen(border, 1.5f);
                g.FillPath(fb, path);
                g.DrawPath(pb, path);
            }

            DrawCircleBadge(g, rect.X + 24, rect.Y + (rect.Height / 2), 14, badgeBg, symbol);

            using var tf = new Font("Segoe UI", 10F, FontStyle.Bold);
            using var sf = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            using var tb = new SolidBrush(Color.FromArgb(15, 23, 42));
            using var sb = new SolidBrush(Color.FromArgb(100, 116, 139));

            g.DrawString(title, tf, tb, rect.X + 50, rect.Y + 12);
            g.DrawString(sub, sf, sb, rect.X + 50, rect.Y + 36);
        }

        private void DrawStepCard(Graphics g, int stepIndex, Rectangle rect, StepResult result, string idTag, string title, string sub)
        {
            Color fill = Color.FromArgb(248, 250, 252);
            Color border = Color.FromArgb(226, 232, 240);

            if (result == StepResult.Testing)
            {
                fill = Color.FromArgb(254, 252, 232);
                border = Color.FromArgb(234, 179, 8);
            }
            else if (result == StepResult.Pass)
            {
                fill = Color.FromArgb(240, 253, 244);
                border = Color.FromArgb(34, 197, 94);
            }
            else if (result == StepResult.Fail)
            {
                fill = Color.FromArgb(254, 242, 242);
                border = Color.FromArgb(239, 68, 68);
            }

            using (var path = GetRoundedRectPath(rect, 10))
            {
                using var fb = new SolidBrush(fill);
                using var pb = new Pen(border, 1.5f);
                g.FillPath(fb, path);
                g.DrawPath(pb, path);
            }

            // Step Icon
            int iconX = rect.X + 26;
            int iconY = rect.Y + 32;

            if (result == StepResult.Pass)
            {
                DrawCircleBadge(g, iconX, iconY, 14, Color.FromArgb(34, 197, 94), "✔");
            }
            else if (result == StepResult.Fail)
            {
                DrawCircleBadge(g, iconX, iconY, 14, Color.FromArgb(239, 68, 68), "✕");
            }
            else if (result == StepResult.Testing)
            {
                DrawCircleBadge(g, iconX, iconY, 14, Color.FromArgb(234, 179, 8), "↻");
            }
            else
            {
                DrawCircleBadge(g, iconX, iconY, 14, Color.FromArgb(203, 213, 225), "🕒");
            }

            // Titles
            using var tf = new Font("Segoe UI", 10F, FontStyle.Bold);
            using var sf = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            using var idf = new Font("Segoe UI", 8F, FontStyle.Bold);
            using var tb = new SolidBrush(Color.FromArgb(15, 23, 42));
            using var sb = new SolidBrush(Color.FromArgb(100, 116, 139));
            using var idb = new SolidBrush(Color.FromArgb(148, 163, 184));

            g.DrawString(title, tf, tb, rect.X + 50, rect.Y + 14);
            g.DrawString(sub, sf, sb, rect.X + 50, rect.Y + 38);

            // Right ID tag
            var isz = g.MeasureString(idTag, idf);
            g.DrawString(idTag, idf, idb, rect.Right - isz.Width - 16, rect.Y + 16);

            // Progress yellow bar in testing mode
            if (result == StepResult.Testing)
            {
                using var yb = new SolidBrush(Color.FromArgb(234, 179, 8));
                g.FillRectangle(yb, rect.X + 16, rect.Y + 68, rect.Width - 32, 4);
            }
        }

        private void DrawDIPSwitchBox(Graphics g, Rectangle rect, string mainText, string detailText, bool[] switches)
        {
            // Background
            using (var path = GetRoundedRectPath(rect, 10))
            {
                using var fb = new SolidBrush(Color.FromArgb(254, 252, 232)); // Light Yellow
                using var pb = new Pen(Color.FromArgb(250, 204, 21), 1.5f);
                g.FillPath(fb, path);
                g.DrawPath(pb, path);
            }

            // Header Texts
            using var mf = new Font("Segoe UI", 10F, FontStyle.Bold);
            using var df = new Font("Segoe UI", 9F, FontStyle.Bold);
            using var mb = new SolidBrush(Color.FromArgb(15, 23, 42));

            var msz = g.MeasureString(mainText, mf);
            var dsz = g.MeasureString(detailText, df);

            g.DrawString(mainText, mf, mb, rect.X + (rect.Width - msz.Width) / 2, rect.Y + 8);
            if (!string.IsNullOrEmpty(detailText))
            {
                g.DrawString(detailText, df, mb, rect.X + (rect.Width - dsz.Width) / 2, rect.Y + 28);
            }

            // 8 DIP Switch items
            int switchW = 42;
            int switchH = 48;
            int startX = rect.X + 16;
            int switchY = rect.Y + 48;
            int gap = (rect.Width - 32 - (8 * switchW)) / 7;

            for (int i = 0; i < 8; i++)
            {
                int sx = startX + (i * (switchW + gap));
                DrawSingleDIPSwitch(g, new Rectangle(sx, switchY, switchW, switchH), i + 1, switches[i]);
            }
        }

        private void DrawSingleDIPSwitch(Graphics g, Rectangle rect, int num, bool isOn)
        {
            Color borderCol = isOn ? Color.FromArgb(59, 130, 246) : Color.FromArgb(203, 213, 225);
            Color fillCol = Color.White;

            using (var path = GetRoundedRectPath(rect, 5))
            {
                using var fb = new SolidBrush(fillCol);
                using var pb = new Pen(borderCol, 1.5f);
                g.FillPath(fb, path);
                g.DrawPath(pb, path);
            }

            // Number at top
            using var numFont = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            using var numBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
            string nStr = num.ToString();
            var nsz = g.MeasureString(nStr, numFont);
            g.DrawString(nStr, numFont, numBrush, rect.X + (rect.Width - nsz.Width) / 2, rect.Y + 2);

            // Pill toggle
            int pillX = rect.X + 6;
            int pillY = rect.Y + 16;
            int pillW = rect.Width - 12;
            int pillH = 14;

            if (isOn)
            {
                using var pb = new SolidBrush(Color.FromArgb(59, 130, 246));
                g.FillEllipse(pb, pillX, pillY, pillW, pillH);
                using var knob = new SolidBrush(Color.White);
                g.FillEllipse(knob, pillX + pillW - 12, pillY + 1, 12, 12);
            }
            else
            {
                using var pb = new SolidBrush(Color.FromArgb(226, 232, 240));
                g.FillEllipse(pb, pillX, pillY, pillW, pillH);
                using var knob = new SolidBrush(Color.White);
                g.FillEllipse(knob, pillX + 1, pillY + 1, 12, 12);
            }

            // Label ON / OFF at bottom
            using var lblFont = new Font("Segoe UI", 7F, FontStyle.Bold);
            using var lblBrush = new SolidBrush(isOn ? Color.FromArgb(37, 99, 235) : Color.FromArgb(100, 116, 139));
            string sText = isOn ? "ON" : "OFF";
            var ssz = g.MeasureString(sText, lblFont);
            g.DrawString(sText, lblFont, lblBrush, rect.X + (rect.Width - ssz.Width) / 2, rect.Y + 31);
        }

        private void DrawCircleBadge(Graphics g, int cx, int cy, int radius, Color bg, string text)
        {
            Rectangle rect = new Rectangle(cx - radius, cy - radius, radius * 2, radius * 2);
            using (var b = new SolidBrush(bg))
            {
                g.FillEllipse(b, rect);
            }

            using (var pen = new Pen(Color.White, Math.Max(1.6f, radius * 0.18f)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                if (text == "✔")
                {
                    // Vector checkmark
                    PointF p1 = new PointF(cx - radius * 0.45f, cy - radius * 0.05f);
                    PointF p2 = new PointF(cx - radius * 0.1f, cy + radius * 0.32f);
                    PointF p3 = new PointF(cx + radius * 0.45f, cy - radius * 0.35f);
                    g.DrawLines(pen, new PointF[] { p1, p2, p3 });
                }
                else if (text == "✕")
                {
                    // Vector cross
                    float offset = radius * 0.35f;
                    g.DrawLine(pen, cx - offset, cy - offset, cx + offset, cy + offset);
                    g.DrawLine(pen, cx + offset, cy - offset, cx - offset, cy + offset);
                }
                else if (text == "🕒")
                {
                    // Vector clock (Pending)
                    float clockR = radius * 0.45f;
                    using var clockPen = new Pen(Color.White, Math.Max(1.3f, radius * 0.14f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                    g.DrawEllipse(clockPen, cx - clockR, cy - clockR, clockR * 2, clockR * 2);
                    g.DrawLine(clockPen, cx, cy, cx, cy - clockR * 0.62f);
                    g.DrawLine(clockPen, cx, cy, cx + clockR * 0.52f, cy);
                }
                else if (text == "↻")
                {
                    // Vector spinner arc
                    float spinR = radius * 0.42f;
                    using var spinPen = new Pen(Color.White, Math.Max(1.5f, radius * 0.16f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                    g.DrawArc(spinPen, cx - spinR, cy - spinR, spinR * 2, spinR * 2, 45, 270);
                    PointF tip = new PointF(cx + spinR * 0.707f, cy + spinR * 0.707f);
                    g.DrawLine(spinPen, tip.X, tip.Y, tip.X + 2.5f, tip.Y - 2.5f);
                    g.DrawLine(spinPen, tip.X, tip.Y, tip.X - 2.5f, tip.Y - 2.5f);
                }
                else
                {
                    using var f = new Font("Segoe UI", radius * 0.85F, FontStyle.Bold);
                    using var tb = new SolidBrush(Color.White);
                    var sz = g.MeasureString(text, f);
                    g.DrawString(text, f, tb, cx - (sz.Width / 2) + 0.5f, cy - (sz.Height / 2));
                }
            }
        }

        private void DrawWarningIcon(Graphics g, int cx, int cy, int radius)
        {
            Rectangle rect = new Rectangle(cx - radius, cy - radius, radius * 2, radius * 2);
            using (var b = new SolidBrush(Color.FromArgb(239, 68, 68)))
            {
                g.FillEllipse(b, rect);
            }

            using var f = new Font("Segoe UI", radius * 0.9F, FontStyle.Bold);
            using var tb = new SolidBrush(Color.White);
            string icon = "!";
            var sz = g.MeasureString(icon, f);
            g.DrawString(icon, f, tb, cx - (sz.Width / 2), cy - (sz.Height / 2));
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
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

        #region Form Dragging

        private void Form_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y <= 40)
            {
                _dragStart = e.Location;
                this.MouseMove += Form_MouseMove;
                this.MouseUp += Form_MouseUp;
            }
        }

        private void Form_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Left += e.X - _dragStart.X;
                this.Top += e.Y - _dragStart.Y;
            }
        }

        private void Form_MouseUp(object? sender, MouseEventArgs e)
        {
            this.MouseMove -= Form_MouseMove;
            this.MouseUp -= Form_MouseUp;
        }

        #endregion
    }
}
