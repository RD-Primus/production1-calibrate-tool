// =========================================================================================
// [DEVELOPER GUIDE]
// Form1.Test.cs: "ส่วนควบคุมหน้าจอการทดสอบ (Test UI Controller)"
// - จัดการ Event ปุ่มกด START TEST (Btn_test_Click), ป้ายสถานะ Phase 1-4 (Pass/Fail)
// - ประสานงานกับ Ph08TestRunner เพื่อรัน Master Sequence
// - อัปเดตสี PictureBox (phase1, phase2, phase3, phase4) และ Summary Label
// - หากต้องการแก้ไข "เงื่อนไขการทดสอบ หรือลำดับขั้นตอน" -> ไปที่ Services/Orchestrator/Ph08TestRunner.cs
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PH_08_Test.Services.Orchestrator;
using PH_08_Test.Services.Reporting;

namespace PH_08_Test
{
    public partial class Form1
    {
        private Ph08TestRecord? _lastTestRecord;
        private CancellationTokenSource? _testCts;
        private bool _isTestRunning = false;
        public bool IsActiveTesting => _isTestRunning;

        private readonly Dictionary<PictureBox, PhaseStatus> _phaseStatuses = new Dictionary<PictureBox, PhaseStatus>();

        private void InitTestControls()
        {
            if (phase1 != null) phase1.SizeMode = PictureBoxSizeMode.Zoom;
            if (phase2 != null) phase2.SizeMode = PictureBoxSizeMode.Zoom;
            if (phase3 != null) phase3.SizeMode = PictureBoxSizeMode.Zoom;
            if (phase4 != null) phase4.SizeMode = PictureBoxSizeMode.Zoom;

            btn_test.Click += Btn_test_Click;
            if (btn_ReadDevice != null)
            {
                btn_ReadDevice.Enabled = false;
                btn_ReadDevice.Click += Btn_Stop_Click;
            }
            btn_TestSwitchID.Click += Btn_TestSwitchID_Click;
            btn_TestLED.Click += Btn_TestLED_Click;
            btn_TestCommunication.Click += Btn_TestCommunication_Click;
            btn_Clear.Click += (s, e) =>
            {
                txt_massage.Clear();
                ResetMessageStyle();
            };

            ResetPhaseIndicators();
            UpdateTestButtonAndReadyState(false);
        }

        public void UpdateTestButtonAndReadyState(bool isConnected)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateTestButtonAndReadyState(isConnected)));
                return;
            }

            if (isConnected)
            {
                btn_test.Enabled = true;
                btn_test.BackColor = Color.FromArgb(46, 125, 50);
                btn_test.BorderColor = Color.FromArgb(46, 125, 50);
                btn_test.ForeColor = Color.White;
                btn_test.Cursor = Cursors.Hand;
                btn_test.Invalidate();

                if (label1 != null)
                {
                    label1.Text = "✔ พร้อมทดสอบ";
                    label1.ForeColor = Color.FromArgb(241, 248, 241);
                    label1.Visible = true;
                }

                if (label3 != null)
                {
                    label3.Text = "เชื่อมต่อสำเร็จ สามารถกดปุ่ม Test ได้";
                    label3.ForeColor = Color.FromArgb(241, 248, 241);
                    label3.Visible = true;
                }
            }
            else
            {
                btn_test.Enabled = false;
                btn_test.BackColor = Color.FromArgb(225, 230, 228);
                btn_test.BorderColor = Color.FromArgb(215, 220, 218);
                btn_test.ForeColor = Color.FromArgb(125, 140, 148);
                btn_test.Cursor = Cursors.Default;
                btn_test.Invalidate();

                if (label1 != null)
                {
                    label1.Text = "⚠️ ยังไม่พร้อมทดสอบ";
                    label1.ForeColor = Color.FromArgb(239, 83, 80); 
                    label1.Visible = true;
                }

                if (label3 != null)
                {
                    label3.Text = "เชื่อมต่อ Device ให้สำเร็จก่อน จึงกด Test ได้";
                    label3.ForeColor = Color.DarkGray;
                    label3.Visible = true;
                }
            }
        }

        private void Btn_Stop_Click(object? sender, EventArgs e)
        {
            if (_testCts != null && !_testCts.IsCancellationRequested)
            {
                btn_ReadDevice.Enabled = false;
                LogMessage("⏹ ผู้ใช้กดปุ่ม STOP: กำลังหยุดการทดสอบทันที...", Color.DarkOrange);
                try
                {
                    _testCts.Cancel();
                }
                catch (ObjectDisposedException) { }
            }
        }

        public void SetMessageStyle(Color? backColor, Color foreColor)
        {
            if (txt_massage.InvokeRequired)
            {
                txt_massage.BeginInvoke(new Action(() => SetMessageStyle(backColor, foreColor)));
                return;
            }
            if (backColor.HasValue)
            {
                txt_massage.BackColor = backColor.Value;
            }
            txt_massage.ForeColor = foreColor;
        }

        public void ResetMessageStyle()
        {
            SetMessageStyle(null, LogNormalColor);
        }

        public void SetMessagePassStyle()
        {
            SetMessageStyle(null, LogNormalColor);
        }

        public void SetMessageFailStyle()
        {
            SetMessageStyle(null, LogNormalColor);
        }

        public void ClearMessage()
        {
            if (txt_massage.InvokeRequired)
            {
                txt_massage.BeginInvoke(new Action(ClearMessage));
                return;
            }
            txt_massage.Clear();
        }

        public void ResetPhaseIndicators()
        {
            SetPhaseStatus(phase1, PhaseStatus.Idle);
            SetPhaseStatus(phase2, PhaseStatus.Idle);
            SetPhaseStatus(phase3, PhaseStatus.Idle);
            SetPhaseStatus(phase4, PhaseStatus.Idle);
        }

        public void RefreshPhaseIndicators()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshPhaseIndicators));
                return;
            }

            if (phase1 != null && _phaseStatuses.TryGetValue(phase1, out var s1)) SetPhaseStatus(phase1, s1);
            if (phase2 != null && _phaseStatuses.TryGetValue(phase2, out var s2)) SetPhaseStatus(phase2, s2);
            if (phase3 != null && _phaseStatuses.TryGetValue(phase3, out var s3)) SetPhaseStatus(phase3, s3);
            if (phase4 != null && _phaseStatuses.TryGetValue(phase4, out var s4)) SetPhaseStatus(phase4, s4);
        }

        public void SetPhaseStatus(PictureBox? pb, PhaseStatus status)
        {
            if (pb == null || pb.IsDisposed) return;
            if (pb.InvokeRequired)
            {
                pb.BeginInvoke(new Action(() => SetPhaseStatus(pb, status)));
                return;
            }

            _phaseStatuses[pb] = status;
            pb.SizeMode = PictureBoxSizeMode.Zoom;

            int width = pb.Width > 0 ? pb.Width : 69;
            int height = pb.Height > 0 ? pb.Height : 65;
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                int size = Math.Min(width, height) - Math.Max(4, (int)(Math.Min(width, height) * 0.08f));
                int x = (width - size) / 2;
                int y = (height - size) / 2;
                var rect = new Rectangle(x, y, size, size);

                float circlePenWidth = Math.Max(2f, size * 0.045f);
                float checkPenWidth = Math.Max(2.8f, size * 0.075f);
                float ringInset = Math.Max(4f, size * 0.12f);

                switch (status)
                {
                    case PhaseStatus.Idle:
                        using (var pen = new Pen(Color.FromArgb(190, 195, 200), circlePenWidth))
                        {
                            g.DrawEllipse(pen, rect);
                        }
                        break;

                    case PhaseStatus.Testing:
                        using (var brush = new SolidBrush(Color.FromArgb(243, 156, 18))) // Amber / Orange
                        {
                            g.FillEllipse(brush, rect);
                        }
                        using (var pen = new Pen(Color.White, circlePenWidth * 1.1f))
                        {
                            g.DrawEllipse(pen, rect.X + ringInset, rect.Y + ringInset, rect.Width - (ringInset * 2), rect.Height - (ringInset * 2));
                        }
                        break;

                    case PhaseStatus.Pass:
                        using (var brush = new SolidBrush(Color.FromArgb(46, 204, 113))) // Emerald Green
                        {
                            g.FillEllipse(brush, rect);
                        }
                        using (var pen = new Pen(Color.White, checkPenWidth))
                        {
                            pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                            pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                            g.DrawLines(pen, new PointF[]
                            {
                                new PointF(x + size * 0.28f, y + size * 0.52f),
                                new PointF(x + size * 0.45f, y + size * 0.70f),
                                new PointF(x + size * 0.74f, y + size * 0.32f)
                            });
                        }
                        break;

                    case PhaseStatus.Fail:
                        using (var brush = new SolidBrush(Color.FromArgb(231, 76, 60))) // Crimson Red
                        {
                            g.FillEllipse(brush, rect);
                        }
                        using (var pen = new Pen(Color.White, checkPenWidth))
                        {
                            pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                            float offset = size * 0.28f;
                            g.DrawLine(pen, x + offset, y + offset, x + size - offset, y + size - offset);
                            g.DrawLine(pen, x + size - offset, y + offset, x + offset, y + size - offset);
                        }
                        break;
                }
            }

            var oldImg = pb.Image;
            pb.Image = bmp;
            oldImg?.Dispose();
        }

        private void Btn_TestLED_Click(object? sender, EventArgs e)
        {
            using var popup = new FormLEDTest(this);
            popup.ShowDialog(this);
        }

        private void Btn_TestSwitchID_Click(object? sender, EventArgs e)
        {
            if (_serialPortPh08 == null || !_serialPortPh08.IsOpen)
            {
                MessageBox.Show("กรุณาเชื่อมต่อพอร์ต PH-08 ก่อนทำการทดสอบ Switch ID", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var popup = new FormSwitchIDTest(this);
            popup.ShowDialog(this);
        }

        private async void Btn_TestCommunication_Click(object? sender, EventArgs e)
        {
            if (_serialPortPh08 == null || !_serialPortPh08.IsOpen)
            {
                MessageBox.Show("กรุณาเชื่อมต่อพอร์ต PH-08 ก่อนทำการทดสอบการสื่อสาร", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_serialPortDevice == null || !_serialPortDevice.IsOpen)
            {
                MessageBox.Show("กรุณาเชื่อมต่อพอร์ต Device ก่อนทำการทดสอบการสื่อสาร", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _testCts?.Dispose();
            _testCts = new CancellationTokenSource();
            var ct = _testCts.Token;

            btn_TestCommunication.Enabled = false;
            btn_ReadDevice.Enabled = true;
            _isPollingPaused = true;
            ClearMessage();
            ResetMessageStyle();

            // รอ 250ms เพื่อให้เฟรม Polling ก่อนหน้าที่อาจกำลังส่งอยู่ในสาย RS-485 ส่งจบและสายเงียบสนิท 100%
            await Task.Delay(250, ct);

            if (IsDebugMode)
            {
                LogMessage("==========================================");
                LogMessage("📡 เริ่มต้นการทดสอบการสื่อสาร (Phase 1 & Phase 2)");
                LogMessage($"🎯 Target PH-08 ID: {_currentPh08SlaveId}");
                LogMessage("==========================================");
            }
            else
            {
                LogMessage("กำลังทดสอบ Remote line...", LogNormalColor);
            }

            try
            {
                // ========================================================
                // PHASE 1: ตรวจสอบสาย PH-08 (Remote RS-485)
                // ========================================================
                ct.ThrowIfCancellationRequested();
                LogDebug("▶ [Phase 1/2] ตรวจสอบสายและคำสั่ง PH-08 (Remote RS-485)...");

                // 1.1 อ่าน System Block (Reg 1..24, 24 Regs -> 48 Bytes)
                LogDebug($"1.1 ขออ่าน System Block (Reg 1, 24 Regs) จาก PH-08 ID {_currentPh08SlaveId}...");
                ct.ThrowIfCancellationRequested();
                var (sysOk, sysFrame, sysMsg) = await SendPH08ReadRegistersWithValidationAsync(1, 24, 2000, ct);
                ct.ThrowIfCancellationRequested();
                if (!sysOk || sysFrame == null)
                {
                    SetMessageFailStyle();
                    if (IsDebugMode)
                    {
                        LogMessage("==========================================");
                        LogMessage("❌ ผลการทดสอบ: FAIL", LogFailColor);
                        LogMessage($"  - ไม่ผ่านที่: Phase 1 (Step 1.1 Read System Block - {sysMsg})", LogFailColor);
                        LogMessage("==========================================");
                    }
                    else
                    {
                        LogMessage("การทดสอบ Remote line ไม่ผ่าน", LogFailColor);
                    }
                    MessageBox.Show("การทดสอบไม่ผ่าน", "ผลการทดสอบ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string sysFrameHex = BitConverter.ToString(sysFrame).Replace("-", " ");
                LogDebug($"✔ [Step 1.1 PASS] System Read สำเร็จ: {sysFrameHex}", LogSuccessColor);

                await Task.Delay(80, ct);

                // 1.2 อ่าน Map Table Block (Reg 1000..1063, 128 Bytes)
                LogDebug($"1.2 ขออ่าน Map Table Block (Reg 1000, 64 Regs) จาก PH-08 ID {_currentPh08SlaveId}...");
                ct.ThrowIfCancellationRequested();
                var (mapReadOk, mapReadFrame, mapReadMsg) = await SendPH08ReadRegistersWithValidationAsync(1000, 64, 2500, ct);
                ct.ThrowIfCancellationRequested();
                if (!mapReadOk || mapReadFrame == null)
                {
                    SetMessageFailStyle();
                    if (IsDebugMode)
                    {
                        LogMessage("==========================================");
                        LogMessage("❌ ผลการทดสอบ: FAIL", LogFailColor);
                        LogMessage($"  - ไม่ผ่านที่: Phase 1 (Step 1.2 Read Map Table Block - {mapReadMsg})", LogFailColor);
                        LogMessage("==========================================");
                    }
                    else
                    {
                        LogMessage("การทดสอบ Remote line ไม่ผ่าน", LogFailColor);
                    }
                    MessageBox.Show("การทดสอบไม่ผ่าน", "ผลการทดสอบ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string mapFrameHex = BitConverter.ToString(mapReadFrame).Replace("-", " ");
                LogDebug($"✔ [Step 1.2 PASS] MapTable Read สำเร็จ: {mapFrameHex}", LogSuccessColor);

                await Task.Delay(80, ct);

                // 1.3 สั่งเขียนเปิด RunFlag = 1 (Register 1)
                LogDebug($"1.3 สั่งเขียน RunFlag = 1 (Reg 1) ไปที่ PH-08 ID {_currentPh08SlaveId}...");
                ct.ThrowIfCancellationRequested();
                var (runFlagOk, runFlagEcho, runFlagMsg) = await SendPH08WriteRegistersWithResponseAsync(1, new ushort[] { 1 }, 2000, ct);
                ct.ThrowIfCancellationRequested();
                if (!runFlagOk || runFlagEcho == null)
                {
                    SetMessageFailStyle();
                    if (IsDebugMode)
                    {
                        LogMessage("==========================================");
                        LogMessage("❌ ผลการทดสอบ: FAIL", LogFailColor);
                        LogMessage($"  - ไม่ผ่านที่: Phase 1 (Step 1.3 Write RunFlag - {runFlagMsg})", LogFailColor);
                        LogMessage("==========================================");
                    }
                    else
                    {
                        LogMessage("การทดสอบ Remote line ไม่ผ่าน", LogFailColor);
                    }
                    MessageBox.Show("การทดสอบไม่ผ่าน", "ผลการทดสอบ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string runFlagEchoHex = BitConverter.ToString(runFlagEcho).Replace("-", " ");
                LogDebug($"✔ [Step 1.3 PASS] เขียน RunFlag สำเร็จ (Echo: {runFlagEchoHex})", LogSuccessColor);
                ParamRunFlag = "1";
                _paramForm?.UpdateLiveValues(null, null, "1");

                await Task.Delay(80, ct);

                // 1.4 สั่งเขียน Mapping Slot 1 (Reg 1001 = PH08 ID, Reg 1002 = 1)
                LogDebug($"1.4 สั่งเขียน Mapping Slot 1 (Reg 1001 = {_currentPh08SlaveId}, Reg 1002 = 1)...");
                // เคลียร์ประวัติเฟรม 0x03 เก่าของสาย Device ก่อนเริ่มเขียน Mapping
                lock (_deviceLock)
                {
                    _hasReceived03Command = false;
                    _lastReceived03Frame = null;
                    _deviceRxBuffer.Clear();
                }
                ct.ThrowIfCancellationRequested();
                var (mapWriteOk, mapWriteEcho, mapWriteMsg) = await SendPH08WriteRegistersWithResponseAsync(1001, new ushort[] { _currentPh08SlaveId, 1 }, 2000, ct);
                ct.ThrowIfCancellationRequested();
                if (!mapWriteOk || mapWriteEcho == null)
                {
                    SetMessageFailStyle();
                    if (IsDebugMode)
                    {
                        LogMessage("==========================================");
                        LogMessage("❌ ผลการทดสอบ: FAIL", LogFailColor);
                        LogMessage($"  - ไม่ผ่านที่: Phase 1 (Step 1.4 Write Mapping Reg 1001/1002 - {mapWriteMsg})", LogFailColor);
                        LogMessage("==========================================");
                    }
                    else
                    {
                        LogMessage("การทดสอบ Remote line ไม่ผ่าน", LogFailColor);
                    }
                    MessageBox.Show("การทดสอบไม่ผ่าน", "ผลการทดสอบ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string mapWriteEchoHex = BitConverter.ToString(mapWriteEcho).Replace("-", " ");
                LogDebug($"✔ [Step 1.4 PASS] เขียน Mapping สำเร็จ (Echo: {mapWriteEchoHex})", LogSuccessColor);
                ParamDeviceAdr = _currentPh08SlaveId.ToString();
                ParamModuleType = "1";
                _paramForm?.UpdateLiveValues(ParamDeviceAdr, ParamModuleType, null);

                LogDebug("✔ [Phase 1 PASS] ตรวจสอบสายและคำสั่ง PH-08 (Remote RS-485) ผ่านสมบูรณ์", LogSuccessColor);
                if (!IsDebugMode)
                {
                    LogMessage("การทดสอบ Remote line ผ่าน", LogSuccessColor);
                }

                // ========================================================
                // PHASE 2: ตรวจสอบสาย Device (Local RS-485)
                // ========================================================
                ct.ThrowIfCancellationRequested();
                LogDebug("------------------------------------------");
                LogDebug("▶ [Phase 2/2] ตรวจสอบสาย Device (Local RS-485)...");
                LogDebug($"⏳ กำลังดักรอรับเฟรม Modbus 0x03 จากสาย Device (Expected ID: {_currentPh08SlaveId}, Timeout 3s)...");
                if (!IsDebugMode)
                {
                    LogMessage("กำลังทดสอบ Local line...", LogNormalColor);
                }

                byte[]? detectedRemoteFrame = null;
                // ตรวจสอบทันทีว่ามีเฟรม 0x03 ที่เพิ่งได้รับเข้ามาหลังเขียน Mapping หรือยัง
                lock (_deviceLock)
                {
                    if (_hasReceived03Command && _lastReceived03Frame != null)
                    {
                        detectedRemoteFrame = _lastReceived03Frame;
                    }
                }

                if (detectedRemoteFrame == null)
                {
                    for (int i = 0; i < 35; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        await Task.Delay(100, ct);

                        lock (_deviceLock)
                        {
                            if (_hasReceived03Command && _lastReceived03Frame != null)
                            {
                                detectedRemoteFrame = _lastReceived03Frame;
                                break;
                            }
                        }
                    }
                }
                ct.ThrowIfCancellationRequested();

                if (detectedRemoteFrame == null)
                {
                    SetMessageFailStyle();
                    string failMsg = "FAIL: Timeout (ไม่พบคำสั่ง 0x03 จากสาย Device ภายใน 3s)";
                    if (IsDebugMode)
                    {
                        LogMessage("==========================================");
                        LogMessage("❌ ผลการทดสอบ: FAIL", LogFailColor);
                        LogMessage($"  - ไม่ผ่านที่: Phase 2 Device Local RS-485 ({failMsg})", LogFailColor);
                        LogMessage("==========================================");
                    }
                    else
                    {
                        LogMessage("การทดสอบ Local line ไม่ผ่าน", LogFailColor);
                    }
                    MessageBox.Show("การทดสอบไม่ผ่าน", "ผลการทดสอบ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string remoteHex = BitConverter.ToString(detectedRemoteFrame).Replace("-", " ");
                byte receivedRemoteId = detectedRemoteFrame[0];

                if (receivedRemoteId != _currentPh08SlaveId)
                {
                    SetMessageFailStyle();
                    string failMsg = $"FAIL: ID ไม่ตรง (Expected: {_currentPh08SlaveId}, Received: {receivedRemoteId})";
                    if (IsDebugMode)
                    {
                        LogMessage("==========================================");
                        LogMessage("❌ ผลการทดสอบ: FAIL", LogFailColor);
                        LogMessage($"  - ไม่ผ่านที่: Phase 2 Device Local RS-485 ({failMsg}) | เฟรม: {remoteHex}", LogFailColor);
                        LogMessage("==========================================");
                    }
                    else
                    {
                        LogMessage("การทดสอบ Local line ไม่ผ่าน", LogFailColor);
                    }
                    MessageBox.Show("การทดสอบไม่ผ่าน", "ผลการทดสอบ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                LogDebug($"✔ [Phase 2 PASS] ตรวจพบเฟรมคำสั่งจาก PH-08 ทางสาย Device/Local (ID: {receivedRemoteId})", LogSuccessColor);
                LogDebug($"  ↳ เฟรมคำสั่งจาก PH-08 (Rx): {remoteHex}");

                if (_lastSentSlaveResponseFrame != null)
                {
                    string slaveRespHex = BitConverter.ToString(_lastSentSlaveResponseFrame).Replace("-", " ");
                    LogDebug($"  ↳ จำลอง Slave ส่งตอบกลับ (Tx): {slaveRespHex} (Reg 0..1 = 1)");
                }

                if (!IsDebugMode)
                {
                    LogMessage("การทดสอบ Local line ผ่าน", LogSuccessColor);
                }

                // ========================================================
                // สรุปผล Communication Test (Phase 1 & Phase 2 PASS)
                // ========================================================
                ct.ThrowIfCancellationRequested();
                SetMessagePassStyle();
                if (IsDebugMode)
                {
                    LogMessage("==========================================");
                    LogMessage("🎉 Test สำเร็จ", LogSuccessColor);
                    LogMessage("==========================================");
                }
                else
                {
                    LogMessage("ทดสอบสำเร็จ", LogSuccessColor);
                }

                MessageBox.Show("การทดสอบผ่าน", "ผลการทดสอบ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                ResetMessageStyle();
                LogMessage("⏹ การทดสอบการสื่อสารถูกหยุด (STOP) โดยผู้ใช้", Color.DarkOrange);
            }
            catch (Exception ex)
            {
                SetMessageFailStyle();
                if (IsDebugMode)
                {
                    LogMessage("==========================================");
                    LogMessage("❌ ผลการทดสอบ: FAIL", LogFailColor);
                    LogMessage($"  - ไม่ผ่านเนื่องจากเกิดข้อผิดพลาด: {ex.Message}", LogFailColor);
                    LogMessage("==========================================");
                }
                else
                {
                    LogMessage("การทดสอบไม่ผ่าน", LogFailColor);
                }
                MessageBox.Show("การทดสอบไม่ผ่าน", "ผลการทดสอบ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isPollingPaused = false;
                btn_TestCommunication.Enabled = true;
                btn_ReadDevice.Enabled = false;
                _testCts?.Dispose();
                _testCts = null;
            }
        }

        private async void Btn_test_Click(object? sender, EventArgs e)
        {
            // 1. Validate device metadata inputs
            if (!ValidateDeviceInputsForTest())
            {
                return;
            }

            // 2. Validate connections
            if (_serialPortPh08 == null || !_serialPortPh08.IsOpen)
            {
                MessageBox.Show("กรุณาเชื่อมต่อพอร์ต PH-08 ก่อนทำการทดสอบ", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_serialPortDevice == null || !_serialPortDevice.IsOpen)
            {
                MessageBox.Show("กรุณาเชื่อมต่อพอร์ต Device ก่อนทำการทดสอบ", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _testCts?.Dispose();
            _testCts = new CancellationTokenSource();
            _isTestRunning = true;
            var ct = _testCts.Token;

            btn_test.Enabled = false;
            btn_test.BackColor = Color.FromArgb(225, 230, 228);
            btn_test.BorderColor = Color.FromArgb(215, 220, 218);
            btn_test.ForeColor = Color.FromArgb(125, 140, 148);
            btn_test.Cursor = Cursors.Default;
            btn_test.Invalidate();
            btn_ReadDevice.Enabled = true;

            if (label1 != null)
            {
                label1.Text = "⏳ กำลังทดสอบ...";
                label1.ForeColor = Color.DodgerBlue;
            }
            if (label3 != null)
            {
                label3.Text = "กรุณารอระบบทำการทดสอบให้เสร็จสิ้น";
                label3.ForeColor = Color.DimGray;
            }
            ResetPhaseIndicators();
            ClearMessage();
            ResetMessageStyle();

            if (IsDebugMode)
            {
                LogMessage("==========================================");
                LogMessage("🚀 เริ่มต้นกระบวนการทดสอบ PH-08 (Master Test Sequence)");
                LogMessage($"🎯 Target PH-08 ID: {_currentPh08SlaveId}");
                LogMessage($"📦 S/N: {txt_SN.Text.Trim()} | LOT: {txt_Lot.Text.Trim()} | ECN NO: {txt_ECN.Text.Trim()} | FW: {txt_FW.Text.Trim()}");
                LogMessage("==========================================");
            }
            else
            {
                LogMessage("กำลังทดสอบ Remote line...", LogNormalColor);
            }

            DateTime testStartTime = DateTime.Now;
            var record = new Ph08TestRecord
            {
                Timestamp = testStartTime,
                Ph08SlaveId = _currentPh08SlaveId
            };

            try
            {
                var testContext = new Ph08TestContext
                {
                    SlaveId = _currentPh08SlaveId,
                    SN = txt_SN.Text.Trim(),
                    Lot = txt_Lot.Text.Trim(),
                    ECN = txt_ECN.Text.Trim(),
                    FW = txt_FW.Text.Trim(),
                    IsDebugMode = IsDebugMode
                };

                var progress = new Progress<Ph08TestProgress>(p =>
                {
                    if (p.PhaseIndex > 0)
                    {
                        var pb = p.PhaseIndex switch
                        {
                            1 => phase1,
                            2 => phase2,
                            3 => phase3,
                            4 => phase4,
                            _ => null
                        };
                        if (pb != null) SetPhaseStatus(pb, p.Status);
                        if (p.Status == PhaseStatus.Fail) SetMessageFailStyle();
                    }
                    if (!string.IsNullOrEmpty(p.MainMessage))
                    {
                        if (p.Status == PhaseStatus.Pass)
                            LogMessage(p.MainMessage, LogSuccessColor);
                        else if (p.Status == PhaseStatus.Fail)
                            LogMessage(p.MainMessage, LogFailColor);
                        else
                            LogMessage(p.MainMessage, LogNormalColor);
                    }
                    if (!string.IsNullOrEmpty(p.LogMessage))
                    {
                        if (p.IsDebugOnly)
                            LogDebug(p.LogMessage, p.LogColor);
                        else
                            LogMessage(p.LogMessage, p.LogColor);
                    }
                });

                record = await _testRunner.ExecuteMasterTestSequenceAsync(testContext, this, progress, ct);
                bool isPass = record.OverallPassed;

                if (isPass)
                {
                    SetMessagePassStyle();
                    if (IsDebugMode)
                    {
                        LogMessage("==========================================");
                        LogMessage("🎉 Test สำเร็จ", LogSuccessColor);
                        LogMessage("==========================================");
                    }
                    else
                    {
                        LogMessage("ทดสอบสำเร็จ", LogSuccessColor);
                    }
                }
                else
                {
                    SetMessageFailStyle();
                }

                await HandleTestCompletionAsync(record, testStartTime, isPass);
            }
            catch (OperationCanceledException)
            {
                ResetPhaseIndicators();
                ResetMessageStyle();
                if (_disconnectInProgress || _serialPortPh08?.IsOpen != true || _serialPortDevice?.IsOpen != true)
                {
                    LogMessage("❌ การทดสอบถูกยกเลิกเนื่องจากพอร์ตหลุดการเชื่อมต่อ", LogFailColor);
                }
                else
                {
                    if (IsDebugMode)
                    {
                        LogMessage("==========================================", Color.DarkOrange);
                        LogMessage("⏹ ผู้ใช้กดหยุดการทดสอบ (STOP) - ยกเลิกขั้นตอน ไม่มีการบันทึกผล", Color.DarkOrange);
                        LogMessage("==========================================", Color.DarkOrange);
                    }
                    else
                    {
                        LogMessage("หยุดการทดสอบแล้ว (STOP) - ไม่บันทึกผล", Color.DarkOrange);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is System.IO.IOException || ex is UnauthorizedAccessException)
                {
                    HandleDisconnect($"พอร์ตหลุดระหว่างการทดสอบ ({ex.Message})");
                    return;
                }

                SetMessageFailStyle();
                if (IsDebugMode)
                {
                    LogMessage("==========================================");
                    LogMessage("❌ ผลการทดสอบ: FAIL", LogFailColor);
                    LogMessage($"  - ไม่ผ่านเนื่องจากเกิดข้อผิดพลาด: {ex.Message}", LogFailColor);
                    LogMessage("==========================================");
                }
                else
                {
                    LogMessage("การทดสอบไม่ผ่าน", LogFailColor);
                }

                if (!_disconnectInProgress)
                {
                    MessageBox.Show("การทดสอบไม่ผ่าน", "ผลการทดสอบ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _isTestRunning = false;
                _isPollingPaused = false;
                bool isConnected = (_serialPortPh08 != null && _serialPortPh08.IsOpen) &&
                                   (_serialPortDevice != null && _serialPortDevice.IsOpen);
                UpdateTestButtonAndReadyState(isConnected);
                btn_ReadDevice.Enabled = false;
                _testCts?.Dispose();
                _testCts = null;

                if (!string.IsNullOrWhiteSpace(txt_SN.Text))
                {
                    txt_SN.Focus();
                    txt_SN.SelectAll();
                    _isFirstChar = true;
                }
            }
        }

        private async Task HandleTestCompletionAsync(Ph08TestRecord record, DateTime startTime, bool isPass)
        {
            _lastTestRecord = record;
            bool excelSaved = SaveTestRecordToExcel(record);

            // ตรวจ S/N ซ้ำทันทีหลังบันทึกลง Excel สำเร็จ เพื่ออัปเดตสีสถานะล่าสุด (เปลี่ยนเป็นสีแดง Firebrick เตือนว่าบันทึกแล้ว)
            if (excelSaved && !string.IsNullOrWhiteSpace(txt_SN.Text))
            {
                CheckSNWhileTyping();
            }

            ServerPostResult serverResult = ServerPostResult.Skipped;
            if (chk_resultToserver != null && chk_resultToserver.Checked && IsOperatorLoggedIn)
            {
                serverResult = await PostQcResultAsync(record, startTime, DateTime.Now);
            }

            if (isPass)
            {
                string message = serverResult switch
                {
                    ServerPostResult.Success => "การทดสอบผ่าน\n\nบันทึก Excel และส่ง Server สำเร็จ",
                    ServerPostResult.Timeout => "การทดสอบผ่านและบันทึก Excel แล้ว\n\nแต่ Server ไม่ตอบกลับภายใน 10 วินาที",
                    ServerPostResult.Failed => "การทดสอบผ่านและบันทึก Excel แล้ว\n\nแต่ส่งข้อมูลไป Server ไม่สำเร็จ",
                    _ => "การทดสอบผ่าน"
                };

                MessageBox.Show(
                    message,
                    "ผลการทดสอบ",
                    MessageBoxButtons.OK,
                    serverResult is ServerPostResult.Timeout or ServerPostResult.Failed
                        ? MessageBoxIcon.Warning
                        : MessageBoxIcon.Information);
            }
            else
            {
                string message = serverResult switch
                {
                    ServerPostResult.Success => "การทดสอบไม่ผ่าน\n\nบันทึก Excel และส่ง Server สำเร็จ",
                    ServerPostResult.Timeout => "การทดสอบไม่ผ่านและบันทึก Excel แล้ว\n\nแต่ Server ไม่ตอบกลับภายใน 10 วินาที",
                    ServerPostResult.Failed => "การทดสอบไม่ผ่านและบันทึก Excel แล้ว\n\nแต่ส่งข้อมูลไป Server ไม่สำเร็จ",
                    _ => "การทดสอบไม่ผ่าน"
                };

                MessageBox.Show(message, "ผลการทดสอบ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // โฟกัสและคลุมดำช่อง S/N พร้อมรับการสแกนหรือกรอกชิ้นงานถัดไปทันที
            if (!string.IsNullOrWhiteSpace(txt_SN.Text))
            {
                CheckSNWhileTyping();
            }
            txt_SN.Focus();
            txt_SN.SelectAll();
            _isFirstChar = true;
        }

        #region IPh08TestHardwareProvider Implementation

        Task<(bool success, byte[]? rxFrame, string message)> IPh08TestHardwareProvider.ReadPh08RegistersAsync(
            ushort startRegister, ushort quantity, int timeoutMs, CancellationToken ct) =>
            SendPH08ReadRegistersWithValidationAsync(startRegister, quantity, timeoutMs, ct);

        Task<(bool success, byte[]? echoFrame, string message)> IPh08TestHardwareProvider.WritePh08RegistersAsync(
            ushort startRegister, ushort[] values, int timeoutMs, CancellationToken ct) =>
            SendPH08WriteRegistersWithResponseAsync(startRegister, values, timeoutMs, ct);

        void IPh08TestHardwareProvider.ResetDeviceTrigger()
        {
            lock (_deviceLock)
            {
                _hasReceived03Command = false;
                _lastReceived03Frame = null;
                _deviceRxBuffer.Clear();
            }
        }

        async Task<(bool success, byte[]? frame, byte receivedId, string message)> IPh08TestHardwareProvider.WaitForDeviceTriggerAsync(
            byte expectedSlaveId, int timeoutMs, CancellationToken ct)
        {
            // 1. ตรวจสอบทันทีว่าเฟรม 0x03 เข้ามาแล้วหรือไม่ (PH-08 อาจเริ่มส่งทันทีหลัง Step 1.4 เขียน Mapping สำเร็จ)
            lock (_deviceLock)
            {
                if (_hasReceived03Command && _lastReceived03Frame != null)
                {
                    return (true, _lastReceived03Frame, _lastReceived03Frame[0], "OK");
                }
            }

            // 2. ถ้ายังไม่มา ให้วนลูปดักรอจนครบ timeout
            int loopCount = Math.Max(1, timeoutMs / 100);
            for (int i = 0; i < loopCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(100, ct);

                lock (_deviceLock)
                {
                    if (_hasReceived03Command && _lastReceived03Frame != null)
                    {
                        return (true, _lastReceived03Frame, _lastReceived03Frame[0], "OK");
                    }
                }
            }

            return (false, null, 0, $"Timeout (ไม่พบคำสั่ง 0x03 จากสาย Device ภายใน {timeoutMs / 1000}s)");
        }

        async Task<(bool passed, int problemCount, string problemSummary)> IPh08TestHardwareProvider.ShowLedModalDialogAsync(CancellationToken ct)
        {
            using var ledForm = new FormLEDTest(this);
            using (ct.Register(() =>
            {
                try
                {
                    ledForm.BeginInvoke(new Action(() =>
                    {
                        ledForm.DialogResult = DialogResult.Abort;
                        ledForm.Close();
                    }));
                }
                catch { }
            }))
            {
                var dialogRes = ledForm.ShowDialog(this);
                ct.ThrowIfCancellationRequested();
                if (dialogRes == DialogResult.Abort)
                    throw new OperationCanceledException();

                bool passed = dialogRes == DialogResult.OK && ledForm.TestPassed;
                return (passed, ledForm.ProblemCount, ledForm.ProblemSummary);
            }
        }

        async Task<(bool passed, string status)> IPh08TestHardwareProvider.ShowSwitchIdModalDialogAsync(CancellationToken ct)
        {
            using var swForm = new FormSwitchIDTest(this, normalOnly: true);
            using (ct.Register(() =>
            {
                try
                {
                    swForm.BeginInvoke(new Action(() =>
                    {
                        swForm.DialogResult = DialogResult.Abort;
                        swForm.Close();
                    }));
                }
                catch { }
            }))
            {
                var dialogRes = swForm.ShowDialog(this);
                ct.ThrowIfCancellationRequested();
                if (dialogRes == DialogResult.Abort)
                    throw new OperationCanceledException();

                bool passed = dialogRes == DialogResult.OK && swForm.SwitchTestPassed;
                string status = passed
                    ? "PASS (ทดสอบ Step 1, Step 2 ผ่านครบทั้ง 8 Bits และคืนค่า ID 1 สำเร็จ)"
                    : "FAIL: การทดสอบ DIP Switch ไม่ผ่าน หรือถูกยกเลิก";
                return (passed, status);
            }
        }

        void IPh08TestHardwareProvider.UpdateLiveParameterUi(string? devAdr, string? moduleType, string? runFlag)
        {
            if (devAdr != null) ParamDeviceAdr = devAdr;
            if (moduleType != null) ParamModuleType = moduleType;
            if (runFlag != null) ParamRunFlag = runFlag;
            _paramForm?.UpdateLiveValues(devAdr, moduleType, runFlag);
        }

        void IPh08TestHardwareProvider.SetPollingPaused(bool paused)
        {
            _isPollingPaused = paused;
        }

        #endregion
    }
}
