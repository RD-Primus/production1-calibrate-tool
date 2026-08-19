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
        private enum AppState
        {
            Idle,
            WaitingForFunction1,
            ExpectingSilenceDown,
            WaitingForFunction2,
            ExpectingSilenceUp,
            WaitingForFunction3
        }

        private AppState currentState = AppState.Idle;


        //-- Timer
        private System.Windows.Forms.Timer silenceTimer;
        private System.Windows.Forms.Timer _countdownTick;
        private int _countdownSeconds;


        //-- ตัวแปรเก็บประวัติการตรวจสอบเพื่อรายงานผล
        private bool isFunction1Success = false;
        private bool isArrowDownSilenceSuccess = false;
        private bool isFunction2ResumeSuccess = false;
        private bool isArrowUpSilenceSuccess = false;
        private bool isFunction3ResumeSuccess = false;

        private readonly HashSet<int> _failedSteps = new HashSet<int>(); // เก็บทุก step ที่ fail แทนตัวเดียว

        //-- Constants
        private static readonly string[] StepNames =
        {
                "LED ติดครบ / โชว์ 120 → กด F ค้าง 5 วิ",
                "เจอ I n P → กดปุ่มลด",
                "เจอ 0 → กดปุ่ม F",
                "เจอ PUS → กดปุ่มเพิ่ม",
                "เจอ 0 → กดปุ่ม F"
            };


        //-- from Ref
        private Form1 _parentForm;


        public Form4(SerialPort sharedPort, Form1 parentForm)
        {
            InitializeComponent();

            var _ = this.Handle;

            _serialPort = sharedPort;
            _parentForm = parentForm;

            if (_serialPort != null)
            {
                _serialPort.DataReceived -= DataReceivedHandler;
                _serialPort.DataReceived += DataReceivedHandler;
            }

            silenceTimer = new System.Windows.Forms.Timer();
            silenceTimer.Interval = 500;
            silenceTimer.Tick += SilenceTimer_Tick;

            _countdownTick = new System.Windows.Forms.Timer();
            _countdownTick.Interval = 1000;
            _countdownTick.Tick += CountdownTick_Tick;

            InitStepLeds();

        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= DataReceivedHandler;   // unsubscribe ตัวเองเสมอ ไม่ว่าจะปิดจากทางไหน
            }
            base.OnFormClosed(e);
        }

        //-- fromload4
        private async void Form4_Load(object sender, EventArgs e)
        {
            isFunction1Success = false;
            isArrowDownSilenceSuccess = false;
            isFunction2ResumeSuccess = false;
            isArrowUpSilenceSuccess = false;
            isFunction3ResumeSuccess = false; ;

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

            currentState = AppState.Idle;
            await Task.Delay(300);
            SendDisplay120();

            await ResetAndInitializeAsync();
        }


        //-- ปุ่มกด Test
        private void testDis_Click(object sender, EventArgs e)
        {

            isFunction1Success = false;
            isArrowDownSilenceSuccess = false;
            isFunction2ResumeSuccess = false;
            isArrowUpSilenceSuccess = false;
            isFunction3ResumeSuccess = false;
            _failedSteps.Clear();
            currentState = AppState.WaitingForFunction1;

            silenceTimer.Stop();
            TimeoutTimer.Stop();

            ResetAllSteps();
            SetStep(0, StepState.Running);

            RecieverBox.Clear();
            RecieverBox.AppendText($"======================================\r\n");
            RecieverBox.AppendText($"เริ่มทำการทดสอบการกดปุ่มแบบลำดับ\r\n");
            RecieverBox.AppendText($"======================================\r\n\n");

            RecieverBox.AppendText("[ขั้นตอนที่ 1] LED ติดครบ / โชว์ 120\r\n");
            RecieverBox.AppendText("  → กดปุ่ม F ค้างไว้ 5 วินาที...\r\n");

            TimeoutTimer.Interval = 10000;
            TimeoutTimer.Start();
            StartCountdown(10);
        }


        private bool IsSafeToInvoke()
        {
            return !this.IsDisposed && this.IsHandleCreated;
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

                        byte[] fullFrame = rxBuffer.GetRange(0, 41).ToArray();
                        rxBuffer.RemoveRange(0, 41);

                        //แจ้ง Form1 ว่า _DisPort ยังมีสัญญาณตอบกลับจริง
                        _parentForm?.OnDisPortFrameReceived();

                        if (!IsSafeToInvoke()) continue;

                        try
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                // ขั้น 1 → กด F ครั้งที่ 1
                                if (currentState == AppState.WaitingForFunction1)
                                {
                                    isFunction1Success = true;
                                    currentState = AppState.ExpectingSilenceDown;

                                    ResetTimeoutTimer();
                                    SetStep(0, StepState.Pass);
                                    SetStep(1, StepState.Running);

                                    RecieverBox.AppendText($"\r\n[{DateTime.Now:HH:mm:ss.fff}] ✓ ผ่านขั้นตอน 1: กด F ค้างไว้สำเร็จ\r\n");
                                    RecieverBox.AppendText("[ขั้นตอนที่ 2] เจอ I n P\r\n");
                                    RecieverBox.AppendText("  → กดปุ่มลด (รอสัญญาณนิ่ง)...\r\n");

                                    silenceTimer.Stop();
                                    silenceTimer.Start();
                                    ResetTimeoutTimer();
                                }
                                // ขั้น 3 → กด F ครั้งที่ 2
                                else if (currentState == AppState.WaitingForFunction2)
                                {
                                    isFunction2ResumeSuccess = true;
                                    currentState = AppState.ExpectingSilenceUp;

                                    ResetTimeoutTimer();
                                    SetStep(2, StepState.Pass);
                                    SetStep(3, StepState.Running);

                                    RecieverBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] ✓ ผ่านขั้นตอน 3: กด F สำเร็จ\r\n");
                                    RecieverBox.AppendText("[ขั้นตอนที่ 4] เจอ PUS\r\n");
                                    RecieverBox.AppendText("  → กดปุ่มเพิ่ม (รอสัญญาณนิ่ง)...\r\n");

                                    silenceTimer.Stop();
                                    silenceTimer.Start();
                                    ResetTimeoutTimer();
                                }
                                // ขั้น 5 → กด F ครั้งที่ 3
                                else if (currentState == AppState.WaitingForFunction3)
                                {
                                    isFunction3ResumeSuccess = true;
                                    currentState = AppState.Idle;

                                    SetStep(4, StepState.Pass);

                                    RecieverBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] ✓ ผ่านขั้นตอน 5: กด F สำเร็จ\r\n");
                                    ReportTestResult();
                                }
                            }));
                        }
                        catch (InvalidOperationException) { }   // ครอบคลุม ObjectDisposedException ด้วยในตัว

                        continue;
                    }
                    else if (functionCode == 0x03)
                    {
                        if (rxBuffer.Count < 8) break;

                        byte[] requestFrame = rxBuffer.GetRange(0, 8).ToArray();
                        rxBuffer.RemoveRange(0, 8);

                        ProcessModbusFunction03Response(requestFrame);

                        _parentForm?.OnDisPortFrameReceived();

                        if (!IsSafeToInvoke()) continue;

                        try
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                if (currentState == AppState.ExpectingSilenceDown ||
                                    currentState == AppState.ExpectingSilenceUp)
                                {
                                    silenceTimer.Stop();
                                    silenceTimer.Start();
                                }
                            }));
                        }
                        catch (InvalidOperationException) { }   // ครอบคลุม ObjectDisposedException ด้วยในตัว

                        continue;
                    }
                    else
                    {
                        rxBuffer.RemoveAt(0);
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
                int byteCount = numberOfRegisters * 2; // 1 ช่องเก็บข้อมูลขนาด 2 ไบต์


                byte[] responseWithoutCRC = new byte[3 + byteCount];
                responseWithoutCRC[0] = 0x01;
                responseWithoutCRC[1] = 0x03;
                responseWithoutCRC[2] = (byte)byteCount;

                // เติมข้อมูลจำลอง 
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

                // เขียนตอบกลับเข้าสู่ SerialPort ช่องทางที่ Form1 ถืออยู่
                lock (_lock)
                {
                    _serialPort.Write(finalResponse, 0, finalResponse.Length);
                }
            }
            catch { }
        }
        private void SendDisplay120()
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            try
            {
                // Response frame: ID=01, FC=03, ByteCount=04, 0x0078=120, 0x0019=25, + CRC
                byte[] response = new byte[] { 0x01, 0x03, 0x04, 0x00, 0x78, 0x00, 0x19 };
                ushort crc = CalculateCRC(response, response.Length);

                byte[] finalFrame = new byte[response.Length + 2];
                Array.Copy(response, finalFrame, response.Length);
                finalFrame[finalFrame.Length - 2] = (byte)(crc & 0xFF);
                finalFrame[finalFrame.Length - 1] = (byte)((crc >> 8) & 0xFF);

                lock (_lock)
                {
                    _serialPort.Write(finalFrame, 0, finalFrame.Length);
                }
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
                AppState.WaitingForFunction1 => 0,
                AppState.ExpectingSilenceDown => 1,
                AppState.WaitingForFunction2 => 2,
                AppState.ExpectingSilenceUp => 3,
                AppState.WaitingForFunction3 => 4,
                _ => -1
            };

            if (failedIndex < 0)
            {
                ReportTestResult();
                return;
            }

            SetStep(failedIndex, StepState.Fail);
            _failedSteps.Add(failedIndex);

            RecieverBox.AppendText($"\r\n[⏱ หมดเวลา] ขั้นตอนที่ {failedIndex + 1} ไม่มีสัญญาณตอบสนอง (บันทึกเป็น FAIL แต่ทดสอบต่อ)\r\n");

            // เคลียร์ buffer ก่อนไปขั้นถัดไป กันข้อมูลค้าง
            lock (_lock) { rxBuffer.Clear(); }
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try { _serialPort.DiscardInBuffer(); _serialPort.DiscardOutBuffer(); } catch { }
            }

            // ── ไป step ถัดไปแทนที่จะหยุด ──
            switch (failedIndex)
            {
                case 0: // step1 timeout -> ไปต่อ step2
                    currentState = AppState.ExpectingSilenceDown;
                    SetStep(1, StepState.Running);
                    RecieverBox.AppendText("[ขั้นตอนที่ 2] เจอ I n P → กดปุ่มลด (รอสัญญาณนิ่ง)...\r\n");
                    silenceTimer.Stop();
                    silenceTimer.Start();
                    ResetTimeoutTimer();
                    break;

                case 1: // step2 timeout -> ไปต่อ step3
                    currentState = AppState.WaitingForFunction2;
                    SetStep(2, StepState.Running);
                    RecieverBox.AppendText("[ขั้นตอนที่ 3] เจอ 0 → กดปุ่ม F...\r\n");
                    ResetTimeoutTimer();
                    break;

                case 2: // step3 timeout -> ไปต่อ step4
                    currentState = AppState.ExpectingSilenceUp;
                    SetStep(3, StepState.Running);
                    RecieverBox.AppendText("[ขั้นตอนที่ 4] เจอ PUS → กดปุ่มเพิ่ม (รอสัญญาณนิ่ง)...\r\n");
                    silenceTimer.Stop();
                    silenceTimer.Start();
                    ResetTimeoutTimer();
                    break;

                case 3: // step4 timeout -> ไปต่อ step5
                    currentState = AppState.WaitingForFunction3;
                    SetStep(4, StepState.Running);
                    RecieverBox.AppendText("[ขั้นตอนที่ 5] เจอ 0 → กดปุ่ม F เพื่อจบการทดสอบ...\r\n");
                    ResetTimeoutTimer();
                    break;

                case 4: // step5 timeout -> ครบทุก step แล้ว จบการทดสอบ
                    currentState = AppState.Idle;
                    ReportTestResult();
                    break;
            }
        }

        private void SilenceTimer_Tick(object? sender, EventArgs e)
        {
            silenceTimer.Stop();

            if (!IsSafeToInvoke()) return;   // ✅ เพิ่มบรรทัดนี้

            // ขั้น 2 สำเร็จ → เจอ I n P กดปุ่มลดแล้ว
            if (currentState == AppState.ExpectingSilenceDown)
            {
                isArrowDownSilenceSuccess = true;
                currentState = AppState.WaitingForFunction2;

                try
                {
                    this.Invoke(new MethodInvoker(() =>
                    {
                        ResetTimeoutTimer();
                        SetStep(1, StepState.Pass);
                        SetStep(2, StepState.Running);

                        RecieverBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] ✓ ผ่านขั้นตอน 2: กดปุ่มลดสำเร็จ\r\n");
                        RecieverBox.AppendText("[ขั้นตอนที่ 3] เจอ 0\r\n");
                        RecieverBox.AppendText("  → กดปุ่ม F...\r\n");
                    }));
                }
                catch (InvalidOperationException) { }
            }

            // ขั้น 4 สำเร็จ → เจอ PUS กดปุ่มเพิ่มแล้ว
            else if (currentState == AppState.ExpectingSilenceUp)
            {
                isArrowUpSilenceSuccess = true;
                currentState = AppState.WaitingForFunction3;

                try
                {
                    this.Invoke(new MethodInvoker(() =>
                    {
                        ResetTimeoutTimer();
                        SetStep(3, StepState.Pass);
                        SetStep(4, StepState.Running);

                        RecieverBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] ✓ ผ่านขั้นตอน 4: กดปุ่มเพิ่มสำเร็จ\r\n");
                        RecieverBox.AppendText("[ขั้นตอนที่ 5] เจอ 0\r\n");
                        RecieverBox.AppendText("  → กดปุ่ม F เพื่อจบการทดสอบ...\r\n");
                    }));
                }
                catch (InvalidOperationException) { }
            }
        }



        //-- โชว์ Step Process
        private enum StepState { Waiting, Running, Pass, Fail }
        private PictureBox[] _stepLeds = Array.Empty<PictureBox>();
        private void InitStepLeds()
        {
            _stepLeds = new PictureBox[]
            {
                dot0,   // ขั้น 1: LED ติดครบ / โชว์ 120 → กด F
                dot1,   // ขั้น 2: เจอ I n P → กดปุ่มลด
                dot2,   // ขั้น 3: เจอ 0 → กดปุ่ม F
                dot3,   // ขั้น 4: เจอ PUS → กดปุ่มเพิ่ม
                dot4    // ขั้น 5: เจอ 0 → กดปุ่ม F
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
        private void StartCountdown(int seconds = 8) // ✅ default ลดเหลือ 10 (ใช้กับ step 2-5)
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
        private void ResetTimeoutTimer(int seconds = 8)
        {
            TimeoutTimer.Stop();
            TimeoutTimer.Interval = seconds * 1000; // ✅ ตั้ง timeout จริงให้ตรงกับตัวเลขที่โชว์
            TimeoutTimer.Start(); // ← เริ่มนับใหม่จาก 0 ทุก step

            StartCountdown();
        }



        //-- สรุปผลการ test
        private void ReportTestResult()
        {
            TimeoutTimer.Stop();
            silenceTimer.Stop();
            StopCountdown();

            if (_serialPort != null)
            {
                _serialPort.DataReceived -= DataReceivedHandler;
            }

            // ตอนนี้ทดสอบครบทุก step เสมอ (ไม่หยุดกลางทาง) เลยเช็คตรงๆ จาก _failedSteps ได้เลย
            string[] stepResults = new string[5];
            for (int i = 0; i < 5; i++)
            {
                stepResults[i] = _failedSteps.Contains(i) ? "FAIL" : "OK";
            }

            _parentForm._btnFunctionResult = stepResults[0];
            _parentForm._ledResult = stepResults[0];
            _parentForm._btnDownResult = stepResults[1];
            _parentForm._btnUpResult = stepResults[3];

            bool allPassed = _failedSteps.Count == 0;

            if (allPassed)
            {
                RecieverBox.AppendText($" ผลการตรวจสอบ :  SUCCESS \r\n");
               // MessageBox.Show("ทดสอบเรียบร้อย!\nผลการตรวจสอบ: SUCCESS ✓",
                                //"Test Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                RecieverBox.AppendText($" ผลการตรวจสอบ :  FAILED (ล้มเหลว {_failedSteps.Count} ขั้นตอน) \r\n");
                BigMessageBox.Show("ทดสอบเรียบร้อย!\nผลการตรวจสอบ: FAILED ✗", "Test Complete", MessageBoxIcon.Warning, MessageBoxButtons.OK, fontSize: 14f);
            }

            string finalStatus = allPassed ? "PASS" : "FAIL";
            _parentForm?.OnForm4TestComplete(finalStatus);

            this.Close();
        }


        //-- step process
        private void DrawStepLed(PictureBox pb, StepState state)
        {
            if (pb.IsDisposed || !pb.IsHandleCreated && pb.InvokeRequired) return;   // ✅ กันไว้

            if (pb.InvokeRequired)
            {
                try { pb.Invoke(new Action(() => DrawStepLed(pb, state))); }
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

        private async Task ResetAndInitializeAsync()
        {
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
            isFunction1Success = false;
            isArrowDownSilenceSuccess = false;
            isFunction2ResumeSuccess = false;
            isArrowUpSilenceSuccess = false;
            isFunction3ResumeSuccess = false;
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

        private async void btnRefreshDisplay_Click(object sender, EventArgs e)
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
    }
}
