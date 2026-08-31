using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CMATestVer1
{

    public partial class Form4 : Form
    {
        //-- Serial Port & Thread Safety
        public SerialPort? _serialPort;
        private readonly object _lock = new object();
        private List<byte> rxBuffer = new List<byte>();


        //-- State Machine
        // ★ ปรับลำดับใหม่: ลด → F → เพิ่ม (3 steps) ตัด "F ค้าง 5 วิ" ตอนต้น และ "F ปิดท้าย" ออก
        private enum AppState
        {
            Idle,
            ExpectingSilenceDown,
            WaitingForFunctionF,
            ExpectingSilenceUp
        }

        private AppState currentState = AppState.Idle;


        //-- Timer
        private System.Windows.Forms.Timer silenceTimer;
        private System.Windows.Forms.Timer _countdownTick;
        private int _countdownSeconds;


        //-- ตัวแปรเก็บประวัติการตรวจสอบเพื่อรายงานผล
        private bool isArrowDownSilenceSuccess = false;
        private bool isFunctionResumeSuccess = false;
        private bool isArrowUpSilenceSuccess = false;

        private readonly HashSet<int> _failedSteps = new HashSet<int>(); // เก็บทุก step ที่ fail แทนตัวเดียว

        //-- Constants
        private static readonly string[] StepNames =
        {
                "เจอ I n P → กดปุ่มลด",
                "เจอ 0 → กดปุ่ม F",
                "เจอ PUS → กดปุ่มเพิ่ม",
                "เจอ 0 → กดปุ่ม F"
         };

        //-- Write Queue (กัน DataReceived thread ไป Write ตรงๆ)
        private readonly Queue<byte[]> _writeQueue = new Queue<byte[]>();
        private readonly object _writeQueueLock = new object();
        private System.Windows.Forms.Timer _writeQueueTimer;


        //-- from Ref
        private Form1 _parentForm;
        public bool AutoCloseOnComplete { get; set; } = true;

        public Form4(SerialPort sharedPort, Form1 parentForm)
        {
            InitializeComponent();

            var _ = this.Handle;

            _serialPort = sharedPort;
            _parentForm = parentForm;

            _parentForm.SetDisPortTimeoutCheckSuppressed(true);

            if (_serialPort != null)
            {
                _serialPort.DataReceived -= DataReceivedHandler;
                _serialPort.DataReceived += DataReceivedHandler;
            }

            silenceTimer = new System.Windows.Forms.Timer();
            silenceTimer.Interval = 3000;
            silenceTimer.Tick += SilenceTimer_Tick;

            _countdownTick = new System.Windows.Forms.Timer();
            _countdownTick.Interval = 1000;
            _countdownTick.Tick += CountdownTick_Tick;

            // ★ เพิ่ม timer สำหรับ flush write queue
            _writeQueueTimer = new System.Windows.Forms.Timer();
            _writeQueueTimer.Interval = 20;   // เดินถี่พอที่จะไม่หน่วง response time
            _writeQueueTimer.Tick += WriteQueueTimer_Tick;
            _writeQueueTimer.Start();

            InitStepLeds();

        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _writeQueueTimer?.Stop();

            if (_serialPort != null)
            {
                _serialPort.DataReceived -= DataReceivedHandler;
            }

            _parentForm?.SetDisPortTimeoutCheckSuppressed(false);
            _parentForm?.OnForm4Closed();

            base.OnFormClosed(e);
        }

        //-- fromload4
        private async void Form4_Load(object sender, EventArgs e)
        {
            try
            {
                if (!IsSafeToInvoke()) return;
                await ResetAndInitializeAsync();
            }
            catch (Exception ex)
            {
                // ★ กัน async void ทำแอปทั้งตัวเด้งปิดเงียบๆ — โชว์ error ให้เห็นแทน
                LogUnexpectedError("Form4_Load", ex);
            }
        }

        // ★ ช่วย log error ที่ไม่คาดคิดแทนการปล่อยให้แอปเด้งปิด
        private void LogUnexpectedError(string context, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{context}] {ex}");
            try
            {
                if (IsSafeToInvoke() && RecieverBox != null && !RecieverBox.IsDisposed)
                {
                    RecieverBox.AppendText($"\r\n[!! ERROR @ {context}] {ex.GetType().Name}: {ex.Message}\r\n");
                }
            }
            catch { }
        }

        //-- ปุ่มกด Test
        private void testDis_Click(object sender, EventArgs e)
        {
            isArrowDownSilenceSuccess = false;
            isFunctionResumeSuccess = false;
            isArrowUpSilenceSuccess = false;

            _failedSteps.Clear();

            // เริ่มขั้นตอนแรก: กดปุ่มลด
            currentState = AppState.ExpectingSilenceDown;

            silenceTimer.Stop();
            TimeoutTimer.Stop();
            StopCountdown();

            ResetAllSteps();
            SetStep(0, StepState.Running);

            RecieverBox.Clear();
            RecieverBox.AppendText(
                "======================================\r\n");

            RecieverBox.AppendText(
                "เริ่มทำการทดสอบการกดปุ่ม\r\n");

            RecieverBox.AppendText(
                "ลำดับ: ลด → F → เพิ่ม\r\n");

            RecieverBox.AppendText(
                "======================================\r\n\r\n");

            RecieverBox.AppendText(
                "[ขั้นตอนที่ 1] กดปุ่มลด\r\n");

            RecieverBox.AppendText(
                "  → รอตรวจสัญญาณนิ่ง สูงสุด 10 วินาที...\r\n");

            // เริ่มรอตรวจการกดปุ่มลด
            silenceTimer.Start();
            ResetTimeoutTimer(10);
        }
        private async void btnRefreshDisplay_Click(object sender, EventArgs e)
        {
            try
            {
                // เตือนก่อนถ้ากำลังทดสอบอยู่
                if (currentState != AppState.Idle)
                {
                    var confirm = BigMessageBox.Show("กำลังทดสอบอยู่ การ Refresh จะยกเลิกการทดสอบปัจจุบัน ต้องการดำเนินการต่อหรือไม่?", "ตรวจตอบ", MessageBoxIcon.Warning, MessageBoxButtons.YesNo, fontSize: 14f);

                    if (confirm == DialogResult.No) return;
                }

                // เรียกการตั้งต้นใหม่เหมือนตอนเปิด Form
                await ResetAndInitializeAsync();
            }
            catch (Exception ex)
            {
                // ★ กัน async void ทำแอปทั้งตัวเด้งปิดเงียบๆ — โชว์ error ให้เห็นแทน
                LogUnexpectedError("btnRefreshDisplay_Click", ex);
            }
        }
        private async Task ResetAndInitializeAsync()
        {
            if (!IsSafeToInvoke()) return;   // ★ เช็คตั้งแต่เริ่มเข้าฟังก์ชัน

            // 1. หยุด Timer ทั้งหมด
            TimeoutTimer.Stop();
            silenceTimer.Stop();
            StopCountdown();

            // 2. Re-subscribe DataReceived เสมอ (ป้องกันกรณี Event หลุดจากการทดสอบครั้งก่อน)
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= DataReceivedHandler;
                _serialPort.DataReceived += DataReceivedHandler;
            }

            // 3. Reset ตัวแปรเก็บผลการทดสอบทั้งหมด
            isArrowDownSilenceSuccess = false;
            isFunctionResumeSuccess = false;
            isArrowUpSilenceSuccess = false;
            _failedSteps.Clear();
            currentState = AppState.Idle;

            // 4. Reset หน้าจอ UI (ไฟ LED และช่อง Log)
            ResetAllSteps();
            RecieverBox.Clear();
            RecieverBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] 🔄 เริ่มต้นระบบใหม่ (Re-initialized)\r\n");

            // 5. เคลียร์ข้อมูลค้างใน Buffer ทั้ง C# และ Hardware SerialPort
            lock (_lock)
            {
                rxBuffer.Clear();
            }

            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();
                }
                catch { }
            }

            // 6. หน่วงเวลา 300ms ให้ Port นิ่ง แล้วส่งสัญญาณ 120
            await Task.Delay(300);
            SendDisplay120();
        }
        private void EnqueueWrite(byte[] frame)
        {
            lock (_writeQueueLock)
            {
                _writeQueue.Enqueue(frame);
            }
        }



        private bool IsSafeToInvoke()
        {
            return !this.IsDisposed && this.IsHandleCreated;
        }
        private void SafeBeginInvoke(Action action)
        {
            if (!IsSafeToInvoke()) return;
            try { this.BeginInvoke(action); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }
        private void SafeInvoke(Action action)
        {
            if (!IsSafeToInvoke()) return;
            try { this.Invoke(action); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }


        //-- อ่านข้อมูล
        public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;
            if (!IsSafeToInvoke()) return;   // ✅ เพิ่มบรรทัดนี้

            try
            {
                int count = _serialPort.BytesToRead;
                byte[] buffer = new byte[count];
                _serialPort.Read(buffer, 0, count);

                lock (_lock) { rxBuffer.AddRange(buffer); }

                ParseModbusRTU();
            }
            catch { }
        }
        private void ParseModbusRTU()
        {
            lock (_lock)
            {
                while (rxBuffer.Count >= 2)
                {
                    if (rxBuffer[0] != 0x01)
                    {
                        rxBuffer.RemoveAt(0);
                        continue;
                    }

                    byte functionCode = rxBuffer[1];

                    if (functionCode == 0x10)
                    {
                        if (rxBuffer.Count < 41) break;

                        byte[] fullFrame =
                            rxBuffer.GetRange(0, 41).ToArray();

                        rxBuffer.RemoveRange(0, 41);

                        _parentForm?.OnDisPortFrameReceived();

                        SafeBeginInvoke(() =>
                        {
                            // ขั้นตอนที่ 2: รอการกด F
                            if (currentState == AppState.WaitingForFunctionF)
                            {
                                isFunctionResumeSuccess = true;
                                currentState = AppState.ExpectingSilenceUp;

                                TimeoutTimer.Stop();
                                silenceTimer.Stop();

                                SetStep(1, StepState.Pass);
                                SetStep(2, StepState.Running);

                                RecieverBox.AppendText(
                                    $"[{DateTime.Now:HH:mm:ss.fff}] " +
                                    "✓ ผ่านขั้นตอน 2: กด F สำเร็จ\r\n");

                                RecieverBox.AppendText(
                                    "[ขั้นตอนที่ 3] กดปุ่มเพิ่ม\r\n");

                                RecieverBox.AppendText(
                                    "  → รอตรวจสัญญาณนิ่ง สูงสุด 10 วินาที...\r\n");

                                // เริ่มตรวจช่วงสัญญาณเงียบของปุ่มเพิ่ม
                                silenceTimer.Start();
                                ResetTimeoutTimer(10);
                            }
                        });

                        continue;
                    }
                    else if (functionCode == 0x03)
                    {
                        if (rxBuffer.Count < 8) break;

                        byte[] requestFrame = rxBuffer.GetRange(0, 8).ToArray();
                        rxBuffer.RemoveRange(0, 8);

                        ProcessModbusFunction03Response(requestFrame);

                        _parentForm?.OnDisPortFrameReceived();

                        SafeBeginInvoke(() =>   // ✅ เปลี่ยนแบบเดียวกัน
                        {
                            if (currentState == AppState.ExpectingSilenceDown ||
                                currentState == AppState.ExpectingSilenceUp)
                            {
                                silenceTimer.Stop();
                                silenceTimer.Start();
                            }
                        });

                        continue;
                    }
                    else
                    {
                        rxBuffer.RemoveAt(0);
                        continue;
                    }

                }
            }
        }


        //-- โชว์ 120
        private void ProcessModbusFunction03Response(byte[] requestFrame)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen) return;

                int numberOfRegisters = (requestFrame[4] << 8) | requestFrame[5];
                int byteCount = numberOfRegisters * 2;

                byte[] responseWithoutCRC = new byte[3 + byteCount];
                responseWithoutCRC[0] = 0x01;
                responseWithoutCRC[1] = 0x03;
                responseWithoutCRC[2] = (byte)byteCount;

                if (byteCount >= 2)
                {
                    responseWithoutCRC[3] = 0x00;
                    responseWithoutCRC[4] = 0x78;
                }
                if (byteCount >= 4)
                {
                    responseWithoutCRC[5] = 0x00;
                    responseWithoutCRC[6] = 0x19;
                }

                ushort crc = CalculateCRC(responseWithoutCRC, responseWithoutCRC.Length);

                byte[] finalResponse = new byte[responseWithoutCRC.Length + 2];
                Array.Copy(responseWithoutCRC, finalResponse, responseWithoutCRC.Length);
                finalResponse[finalResponse.Length - 2] = (byte)(crc & 0xFF);
                finalResponse[finalResponse.Length - 1] = (byte)((crc >> 8) & 0xFF);

                // ★ เปลี่ยนจาก lock(_lock) { _serialPort.Write(...) } ตรงๆ
                EnqueueWrite(finalResponse);
            }
            catch { }
        }
        private void SendDisplay120()
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            try
            {
                byte[] response = new byte[] { 0x01, 0x03, 0x04, 0x00, 0x78, 0x00, 0x19 };
                ushort crc = CalculateCRC(response, response.Length);

                byte[] finalFrame = new byte[response.Length + 2];
                Array.Copy(response, finalFrame, response.Length);
                finalFrame[finalFrame.Length - 2] = (byte)(crc & 0xFF);
                finalFrame[finalFrame.Length - 1] = (byte)((crc >> 8) & 0xFF);

                // ★ เปลี่ยนจาก lock(_lock) { _serialPort.Write(...) } ตรงๆ
                EnqueueWrite(finalFrame);
            }
            catch { }
        }
        private ushort CalculateCRC(byte[] buffer, int length)
        {
            ushort crc = 0xFFFF;
            for (int pos = 0; pos < length; pos++)
            {
                crc ^= (ushort)buffer[pos];
                for (int i = 8; i != 0; i--)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }



        //-- timer
        private void TimeoutTimer_Tick(object? sender, EventArgs e)
        {
            if (!IsSafeToInvoke()) return;

            TimeoutTimer.Stop();
            silenceTimer.Stop();
            StopCountdown();

            int failedIndex = currentState switch
            {
                AppState.ExpectingSilenceDown => 0,
                AppState.WaitingForFunctionF => 1,
                AppState.ExpectingSilenceUp => 2,
                _ => -1
            };

            if (failedIndex < 0)
            {
                ReportTestResult();
                return;
            }

            SetStep(failedIndex, StepState.Fail);
            _failedSteps.Add(failedIndex);

            RecieverBox.AppendText(
                $"\r\n[⏱ หมดเวลา] ขั้นตอนที่ {failedIndex + 1} " +
                "ไม่มีสัญญาณตอบสนอง " +
                "(บันทึกเป็น FAIL แต่ทดสอบต่อ)\r\n");

            // เคลียร์ข้อมูลค้างก่อนเริ่มขั้นตอนใหม่
            lock (_lock)
            {
                rxBuffer.Clear();
            }

            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();
                }
                catch
                {
                }
            }

            switch (failedIndex)
            {
                // ปุ่มลดหมดเวลา
                // ไปทดสอบปุ่ม F ต่อ
                case 0:
                    currentState = AppState.WaitingForFunctionF;

                    SetStep(1, StepState.Running);

                    RecieverBox.AppendText(
                        "[ขั้นตอนที่ 2] กดปุ่ม F\r\n");

                    RecieverBox.AppendText(
                        "  → รอสัญญาณตอบกลับ สูงสุด 14 วินาที...\r\n");

                    ResetTimeoutTimer(14);
                    break;

                // ปุ่ม F หมดเวลา
                // ไปทดสอบปุ่มเพิ่มต่อ
                case 1:
                    currentState = AppState.ExpectingSilenceUp;

                    SetStep(2, StepState.Running);

                    RecieverBox.AppendText(
                        "[ขั้นตอนที่ 3] กดปุ่มเพิ่ม\r\n");

                    RecieverBox.AppendText(
                        "  → รอตรวจสัญญาณนิ่ง สูงสุด 10 วินาที...\r\n");

                    silenceTimer.Stop();
                    silenceTimer.Start();

                    ResetTimeoutTimer(10);
                    break;

                // ปุ่มเพิ่มหมดเวลา
                // ครบทั้งสามขั้นตอนแล้ว
                case 2:
                    currentState = AppState.Idle;
                    ReportTestResult();
                    break;
            }
        }
        private void SilenceTimer_Tick(object? sender, EventArgs e)
        {
            silenceTimer.Stop();

            if (!IsSafeToInvoke()) return;

            // ── ขั้นตอนที่ 1: ตรวจปุ่มลด ──
            if (currentState == AppState.ExpectingSilenceDown)
            {
                isArrowDownSilenceSuccess = true;
                currentState = AppState.WaitingForFunctionF;

                SafeInvoke(() =>
                {
                    TimeoutTimer.Stop();

                    SetStep(0, StepState.Pass);
                    SetStep(1, StepState.Running);

                    RecieverBox.AppendText(
                        $"[{DateTime.Now:HH:mm:ss.fff}] " +
                        "✓ ผ่านขั้นตอน 1: กดปุ่มลดสำเร็จ\r\n");

                    RecieverBox.AppendText(
                        "[ขั้นตอนที่ 2] กดปุ่ม F\r\n");

                    RecieverBox.AppendText(
                        "  → รอสัญญาณตอบกลับ สูงสุด 14 วินาที...\r\n");

                    ResetTimeoutTimer(14);
                });
            }

            // ── ขั้นตอนที่ 3: ตรวจปุ่มเพิ่ม ──
            else if (currentState == AppState.ExpectingSilenceUp)
            {
                isArrowUpSilenceSuccess = true;
                currentState = AppState.Idle;

                SafeInvoke(() =>
                {
                    TimeoutTimer.Stop();
                    StopCountdown();

                    SetStep(2, StepState.Pass);

                    RecieverBox.AppendText(
                        $"[{DateTime.Now:HH:mm:ss.fff}] " +
                        "✓ ผ่านขั้นตอน 3: กดปุ่มเพิ่มสำเร็จ\r\n");

                    // ขั้นตอนสุดท้ายผ่านแล้ว จบการทดสอบทันที
                    ReportTestResult();
                });
            }
        }
        private void WriteQueueTimer_Tick(object sender, EventArgs e)
        {
            byte[]? frame = null;

            lock (_writeQueueLock)
            {
                if (_writeQueue.Count > 0)
                {
                    frame = _writeQueue.Dequeue();
                }
            }

            if (frame == null) return;

            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Write(frame, 0, frame.Length);   // ★ Write เกิดขึ้นบน UI thread เท่านั้น
                }
            }
            catch { }
        }


        //-- โชว์ Step Process
        private enum StepState { Waiting, Running, Pass, Fail }
        private PictureBox[] _stepLeds = Array.Empty<PictureBox>();
        private void InitStepLeds()
        {
            _stepLeds = new PictureBox[]
            {
                dot0,   // ขั้น 1: เจอ I n P → กดปุ่มลด
                dot1,   // ขั้น 2: เจอ 0 → กดปุ่ม F
                dot2,    // ขั้น 3: เจอ PUS → กดปุ่มเพิ่ม
            };

            foreach (var pb in _stepLeds)
                DrawStepLed(pb, StepState.Waiting);
        }




        //-- นาฬิกาจับเวลา
        private void CountdownTick_Tick(object? sender, EventArgs e)
        {
            _countdownSeconds--;

            if (_countdownSeconds <= 0)
            {
                _countdownTick.Stop();
                countdown.Text = "00:00";
                return;
            }

            countdown.ForeColor = _countdownSeconds <= 5 ? Color.Red : Color.DarkGreen;
            countdown.Text = $"⏱ {_countdownSeconds} วินาที";
        }
        private void StartCountdown(int seconds = 10)
        {
            _countdownSeconds = seconds;
            countdown.ForeColor = Color.DarkGreen;
            countdown.Text = $"⏱ {_countdownSeconds} วินาที";
            _countdownTick.Stop();
            _countdownTick.Start();
        }
        private void StopCountdown()
        {
            _countdownTick.Stop();
            countdown.Text = "00:00";
        }
        private void ResetTimeoutTimer(int seconds = 10)
        {
            TimeoutTimer.Stop();
            TimeoutTimer.Interval = seconds * 1000; 
            TimeoutTimer.Start();

            StartCountdown(seconds);
        }

        //-- สรุปผลการ test
        private string GetLedTestResult()
        {
            bool hasFailedLed =
                chkOut.Checked ||
                chkHP.Checked ||
                chkWL.Checked ||
                chk7Segment.Checked;

            return hasFailedLed ? "FAIL" : "OK";
        }
        private void ReportTestResult()
        {
            try
            {
                ReportTestResultCore();
            }
            catch (Exception ex)
            {
                // ★ กันไม่ให้ exception จากขั้นตอนสรุปผลทำให้ทั้งแอปเด้งปิดแบบไม่รู้สาเหตุ
                LogUnexpectedError("ReportTestResult", ex);
            }
        }
        private void ReportTestResultCore()
        {
            TimeoutTimer.Stop();
            silenceTimer.Stop();
            StopCountdown();

            currentState = AppState.Idle;

            if (_serialPort != null)
            {
                _serialPort.DataReceived -= DataReceivedHandler;
            }

            // 0 = ลด, 1 = F, 2 = เพิ่ม
            string[] stepResults = new string[3];

            for (int i = 0; i < 3; i++)
            {
                stepResults[i] =
                    _failedSteps.Contains(i) ? "FAIL" : "OK";
            }

            _parentForm._btnDownResult = stepResults[0];
            _parentForm._btnFunctionResult = stepResults[1];
            _parentForm._btnUpResult = stepResults[2];

            // ตรวจ Checkbox ของ LED
            string ledResult = GetLedTestResult();
            _parentForm._ledResult = ledResult;

            // ผลรวมต้องผ่านทั้งปุ่มและ LED
            bool buttonTestPassed = _failedSteps.Count == 0;
            bool ledTestPassed = ledResult == "OK";
            bool allPassed = buttonTestPassed && ledTestPassed;

            if (allPassed)
            {
                RecieverBox.AppendText(
                    "\r\nผลการตรวจสอบ : SUCCESS\r\n");
            }
            else
            {
                if (!ledTestPassed)
                {
                    RecieverBox.AppendText(
                        "\r\n✗ การตรวจสอบ LED: FAIL " +
                        "(มีรายการ LED ไม่ติด)\r\n");
                }

                RecieverBox.AppendText(
                    $"\r\nผลการตรวจสอบ : FAILED " +
                    $"(ปุ่มล้มเหลว {_failedSteps.Count} ขั้นตอน)\r\n");

                BigMessageBox.Show(
                    "ทดสอบเรียบร้อย!\nผลการตรวจสอบ: FAILED ✗",
                    "Test Complete",
                    MessageBoxIcon.Warning,
                    MessageBoxButtons.OK,
                    fontSize: 14f);
            }

            string finalStatus = allPassed ? "PASS" : "FAIL";

            _parentForm?.OnForm4TestComplete(finalStatus);

            if (AutoCloseOnComplete)
            {
                this.Close();
            }
        }



        //-- step process
        private void DrawStepLed(PictureBox pb, StepState state)
        {
            if (pb.IsDisposed) return;   // ✅ เขียนใหม่ให้อ่านง่ายขึ้น (ของเดิม operator precedence สับสน)

            if (pb.InvokeRequired)
            {
                if (!IsSafeToInvoke()) return;   // ✅ เพิ่ม guard ของฟอร์มด้วย ไม่ใช่แค่ของ pb
                try { pb.Invoke(new Action(() => DrawStepLed(pb, state))); }
                catch (ObjectDisposedException) { }    // ✅ เพิ่ม
                catch (InvalidOperationException) { }
                return;
            }

            Color ledColor = state switch
            {
                StepState.Waiting => Color.FromArgb(200, 200, 200),
                StepState.Running => Color.FromArgb(245, 197, 24),
                StepState.Pass => Color.FromArgb(58, 158, 80),
                StepState.Fail => Color.FromArgb(192, 48, 48),
                _ => Color.Gray
            };

            Bitmap bmp = new Bitmap(pb.Width, pb.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(pb.BackColor == Color.Transparent ? SystemColors.Control : pb.BackColor);

                Rectangle rect = new Rectangle(2, 2, pb.Width - 4, pb.Height - 4);

                // วาด glow ถ้า pass/fail/running
                if (state != StepState.Waiting)
                {
                    using var glowBrush = new SolidBrush(Color.FromArgb(50, ledColor));
                    g.FillEllipse(glowBrush, new Rectangle(0, 0, pb.Width, pb.Height));
                }

                // วาดหลอดไฟ gradient
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddEllipse(rect);
                using var brush = new System.Drawing.Drawing2D.PathGradientBrush(path);
                brush.CenterColor = Color.White;
                brush.SurroundColors = new[] { ledColor };
                brush.CenterPoint = new PointF(rect.X + rect.Width * 0.35f, rect.Y + rect.Height * 0.3f);
                g.FillEllipse(new SolidBrush(ledColor), rect);
                g.FillEllipse(brush, rect);

                // วาดขอบ
                using var pen = new Pen(Color.FromArgb(150, ledColor), 1.2f);
                g.DrawEllipse(pen, rect);
            }

            pb.Image?.Dispose();
            pb.Image = bmp;
        }
        private void SetStep(int index, StepState state)
        {
            if (index < 0 || index >= _stepLeds.Length) return;
            if (!IsSafeToInvoke()) return;   // ✅ เพิ่มบรรทัดนี้
            DrawStepLed(_stepLeds[index], state);
        }
        private void ResetAllSteps()
        {
            for (int i = 0; i < _stepLeds.Length; i++)
                SetStep(i, StepState.Waiting);
        }




        private void label1_Click(object sender, EventArgs e) { }
    }
}