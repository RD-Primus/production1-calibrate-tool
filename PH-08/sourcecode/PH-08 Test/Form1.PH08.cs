// =========================================================================================
// [DEVELOPER GUIDE]
// Form1.PH08.cs: "การสื่อสาร Serial Port กับบอร์ด PH-08 (Remote RS-485)"
// - จัดการการเชื่อมต่อพอร์ต Serial ของ PH-08 (Auto-connect, Polling Status ทุก 200ms)
// - อ่าน/เขียนพารามิเตอร์ของบอร์ด PH-08 และแสดงผลสถานะบนหน้าจอ
// - ส่งมอบสิทธิ์การส่ง-รับข้อมูล Hardware ให้กับ IPh08TestHardwareProvider ขณะทำ Master Test Sequence
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PH_08_Test
{
    public partial class Form1
    {
        private SerialPort? _serialPortPh08;
        private System.Windows.Forms.Timer? _ph08PollTimer;
        private readonly List<byte> _ph08RxBuffer = new List<byte>();
        private readonly object _ph08Lock = new object();
        private byte _currentPh08SlaveId = 1;
        private int _ph08TimeoutCount = 0;
        private bool _isWritingOrTesting = false;
        private bool _isTestingSwitchId = false;
        private bool _isPollingPaused = false;
        public bool IsPollingPaused
        {
            get => _isPollingPaused;
            set => _isPollingPaused = value;
        }
        private int _ph08PollStep = 0; // 0 = Read Reg 1..24, 1 = Read Reg 1000..1063
        private byte[]? _lastPh08WriteEchoFrame = null;
        private byte[]? _lastPh08ReadResponseFrame = null;
        private byte? _lastPh08ReadExceptionCode = null;

        private string? _connectedPh08PortName;
        private string? _connectedDevicePortName;
        private bool _disconnectInProgress = false;
        private bool _timerBusy = false;
        private const int CommunicationFailureThreshold = 5;

        // Parameters managed via Popup Modal FormParameter
        public string ParamDeviceAdr { get; set; } = "1";
        public string ParamModuleType { get; set; } = "1";
        public string ParamRunFlag { get; set; } = "0";
        private FormParameter? _paramForm;

        // Serial Port Settings for PH-08
        public int Ph08DataBits { get; set; } = 8;
        public StopBits Ph08StopBits { get; set; } = StopBits.One;
        public Parity Ph08Parity { get; set; } = Parity.None;

        private void InitPH08Controls()
        {
            // 1. Initial status display
            UpdatePH08Status("Disconnected", Color.Gray);

            // 2. Initial value for Address TextBox (1 - 255)
            txt_AddressPh08.Text = "1";

            // 3. Initial items for Baudrate
            cmb_BaudratePh08.Items.Clear();
            cmb_BaudratePh08.Items.AddRange(new object[] { "2400", "4800", "9600", "19200", "38400", "57600", "115200" });
            cmb_BaudratePh08.SelectedItem = "9600";

            // 4. Setup Polling & Port Health Check Timer (200 ms เหมือน CMA/DEF)
            _ph08PollTimer = new System.Windows.Forms.Timer
            {
                Interval = 200
            };
            _ph08PollTimer.Tick += (s, e) => OnPollingTimerTick();

            // 5. Refresh COM Port list
            RefreshPH08PortList();

            // 6. Bind Unified Buttons (Connect / Refresh / Setting Gear / Advance Popup)
            btn_Refresh.Click += Btn_Refresh_Click;
            btn_connect.Click += Btn_connect_Click;
            openadd_Ph08.Click += Openadd_Ph08_Click;

            // Setting Gear PictureBox setup
            setting.Click += Setting_Click;
            RenderSettingGearIcon();

            this.FormClosing += Form1_PH08_FormClosing;
        }

        private void Openadd_Ph08_Click(object? sender, EventArgs e)
        {
            using var popup = new FormSerialSettingsPopup("PH-08", Ph08DataBits, Ph08StopBits, Ph08Parity);
            popup.SetPositionBelow(openadd_Ph08);
            if (popup.ShowDialog(this) == DialogResult.OK)
            {
                Ph08DataBits = popup.SelectedDataBits;
                Ph08StopBits = popup.SelectedStopBits;
                Ph08Parity = popup.SelectedParity;
            }
        }

        private void RefreshPH08PortList()
        {
            string? currentSelection = cmb_ComportPh08.SelectedItem?.ToString();
            cmb_ComportPh08.Items.Clear();

            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                cmb_ComportPh08.Items.Add(port);
            }

            if (ports.Length > 0)
            {
                if (!string.IsNullOrEmpty(currentSelection) && cmb_ComportPh08.Items.Contains(currentSelection))
                {
                    cmb_ComportPh08.SelectedItem = currentSelection;
                }
                else
                {
                    cmb_ComportPh08.SelectedIndex = -1;
                }
            }
        }

        private void Btn_Refresh_Click(object? sender, EventArgs e)
        {
            RefreshPH08PortList();
            RefreshDevicePortList();
        }

        private async void Btn_connect_Click(object? sender, EventArgs e)
        {
            bool isPh08Connected = _serialPortPh08 != null && _serialPortPh08.IsOpen;
            bool isDeviceConnected = _serialPortDevice != null && _serialPortDevice.IsOpen;

            if (isPh08Connected || isDeviceConnected)
            {
                HandleDisconnect("UserDisconnect");
                return;
            }

            // Validate PH-08 COM Port
            if (cmb_ComportPh08.SelectedItem == null || string.IsNullOrWhiteSpace(cmb_ComportPh08.SelectedItem.ToString()))
            {
                MessageBox.Show("กรุณาเลือก COM Port สำหรับ PH-08 ให้ครบถ้วน", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate Device COM Port
            if (cmb_ComportDevice.SelectedItem == null || string.IsNullOrWhiteSpace(cmb_ComportDevice.SelectedItem.ToString()))
            {
                MessageBox.Show("กรุณาเลือก COM Port สำหรับ Device ให้ครบถ้วน", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string ph08Port = cmb_ComportPh08.SelectedItem.ToString()!;
            string devicePort = cmb_ComportDevice.SelectedItem.ToString()!;

            // Check if same COM Port selected
            if (ph08Port.Equals(devicePort, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("พอร์ต COM ของ PH-08 และ Device ต้องไม่เป็นพอร์ตเดียวกัน", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate PH-08 Address
            if (!byte.TryParse(txt_AddressPh08.Text.Trim(), out byte addressPh08) || addressPh08 < 1)
            {
                MessageBox.Show("กรุณากรอก PH-08 Address ที่ถูกต้อง (1 - 255)", "Invalid Address", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txt_AddressPh08.Focus();
                return;
            }
            _currentPh08SlaveId = addressPh08;

            btn_connect.Enabled = false;
            btn_connect.Text = "⏳ Connecting...";
            this.Cursor = Cursors.WaitCursor;
            UpdatePH08Status("กำลังเชื่อมต่อและตรวจสอบอุปกรณ์...", Color.FromArgb(0, 120, 215));

            int baudRatePh08 = int.TryParse(cmb_BaudratePh08.SelectedItem?.ToString(), out int b1) ? b1 : 9600;
            int dataBitsPh08 = Ph08DataBits;
            StopBits stopBitsPh08 = Ph08StopBits;
            Parity parityPh08 = Ph08Parity;

            try
            {
                // 1. Connect PH-08
                try
                {
                    ClosePH08SerialPort();

                    _serialPortPh08 = new SerialPort(ph08Port, baudRatePh08, parityPh08, dataBitsPh08, stopBitsPh08)
                    {
                        ReadTimeout = 1000,
                        WriteTimeout = 1000,
                        DtrEnable = true,
                        RtsEnable = true
                    };
                    _serialPortPh08.DataReceived += Ph08_DataReceived;
                    _serialPortPh08.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"เชื่อมต่อ PH-08 ({ph08Port}) ไม่สำเร็จ:\n{ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ClosePH08SerialPort();
                    UpdatePH08Status("Disconnected", Color.Gray);
                    UpdateTestButtonAndReadyState(false);
                    btn_connect.Text = "🔗Connect";
                    return;
                }

                // 2. Connect Device
                if (!OpenDeviceConnection(out string devErrMsg))
                {
                    MessageBox.Show(devErrMsg, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ClosePH08SerialPort();
                    CloseDeviceSerialPort();
                    UpdatePH08Status("Disconnected", Color.Gray);
                    UpdateTestButtonAndReadyState(false);
                    btn_connect.Text = "🔗Connect";
                    return;
                }

                // 3. Handshake Pre-Check (ตรวจสอบการตอบสนองจริงจากบอร์ด PH-08)
                try
                {
                    var checkResult = await SendPH08ReadRegistersWithValidationAsync(1, 1, timeoutMs: 1200);
                    if (!checkResult.success)
                    {
                        throw new InvalidOperationException(
                            $"เปิดพอร์ตสำเร็จ แต่ไม่พบการตอบสนองจากบอร์ด PH-08 (ID: {_currentPh08SlaveId})\n\n" +
                            "กรุณาตรวจสอบ:\n" +
                            "1. สายสัญญาณ RS-485 ของ PH-08 (ขั้ว A/B หลวมหรือสลับขั้วหรือไม่)\n" +
                            "2. แหล่งจ่ายไฟของบอร์ด PH-08 เปิดอยู่หรือไม่\n" +
                            $"3. การตั้งค่า Slave ID ({_currentPh08SlaveId}) และ Baud Rate ({baudRatePh08} bps)\n\n" +
                            $"รายละเอียด: {checkResult.message}");
                    }
                }
                catch (Exception ex)
                {
                    ClosePH08SerialPort();
                    CloseDeviceSerialPort();
                    UpdatePH08Status("Disconnected", Color.Gray);
                    UpdateTestButtonAndReadyState(false);
                    SetAllInputsEnabled(true);
                    btn_connect.Text = "🔗Connect";
                    MessageBox.Show(ex.Message, "เชื่อมต่อไม่สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Both connected and verified successfully!
                _connectedPh08PortName = ph08Port;
                _connectedDevicePortName = devicePort;
                btn_connect.Text = "🔗DisConnect";
                SetAllInputsEnabled(false);

                _ph08TimeoutCount = 0;
                _isWritingOrTesting = false;
                _ph08PollStep = 0;
                UpdatePH08Status($"Connected ({ph08Port} & {devicePort})", Color.FromArgb(0, 120, 215));
                UpdateTestButtonAndReadyState(true);

                // Start Round-Robin Modbus Polling on PH-08
                StartPH08Polling();
            }
            finally
            {
                btn_connect.Enabled = true;
                this.Cursor = Cursors.Default;
            }
        }

        private void SetAllInputsEnabled(bool enabled)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SetAllInputsEnabled(enabled)));
                return;
            }

            SetPH08InputsEnabled(enabled);
            SetDeviceInputsEnabled(enabled);
            btn_Refresh.Enabled = enabled;

            if (btn_Refresh != null)
            {
                if (enabled)
                {
                    btn_Refresh.BackColor = Color.FromArgb(203, 211, 205);
                    btn_Refresh.BorderColor = Color.FromArgb(203, 211, 205);
                    btn_Refresh.ForeColor = Color.FromArgb(27, 94, 32);
                    btn_Refresh.Cursor = Cursors.Hand;
                }
                else
                {
                    btn_Refresh.BackColor = Color.FromArgb(235, 238, 240);
                    btn_Refresh.BorderColor = Color.FromArgb(215, 219, 223);
                    btn_Refresh.ForeColor = Color.FromArgb(125, 140, 148);
                    btn_Refresh.Cursor = Cursors.Default;
                }
                btn_Refresh.Invalidate();
            }
        }

        #region Modbus Round-Robin Request & Polling

        private void StartPH08Polling()
        {
            lock (_ph08Lock)
            {
                _ph08RxBuffer.Clear();
            }

            _ph08PollStep = 0;
            SendPH08RoundRobinReadRequest();

            // Start recurring timer
            _ph08PollTimer?.Start();
        }

        private void StopPH08Polling()
        {
            _ph08PollTimer?.Stop();
            lock (_ph08Lock)
            {
                _ph08RxBuffer.Clear();
            }
        }

        private void OnPollingTimerTick()
        {
            if (_timerBusy) return;
            _timerBusy = true;

            try
            {
                // ── 1. ตรวจสอบว่าพอร์ตยังเปิดอยู่หรือไม่ (IsOpen) เหมือน CMA & DEF ─────────────
                if (_serialPortPh08 == null || !_serialPortPh08.IsOpen)
                {
                    HandleDisconnect("พอร์ต PH-08 หลุดการเชื่อมต่อ (Port is not open)");
                    return;
                }

                if (_serialPortDevice == null || !_serialPortDevice.IsOpen)
                {
                    HandleDisconnect("พอร์ต Device หลุดการเชื่อมต่อ (Port is not open)");
                    return;
                }

                // ── 2. ตรวจพอร์ตใน Windows (FindUnavailableConnectedPort เหมือน DEF-03N) ─
                string? missingPort = FindUnavailableConnectedPort();
                if (missingPort != null)
                {
                    HandleDisconnect($"ไม่พบพอร์ต {missingPort} ใน Windows (สายหลุด/ถูกถอด)");
                    return;
                }

                // ── 3. ตรวจสอบ Timeout (เหมือน CMA / DEF) ──────────────────
                if (_ph08TimeoutCount >= CommunicationFailureThreshold)
                {
                    HandleDisconnect($"บอร์ด PH-08 ไม่ตอบสนอง Modbus ติดต่อกัน {_ph08TimeoutCount} ครั้ง (กรุณาตรวจสอบสายและไฟเลี้ยง)");
                    return;
                }

                // ── 4. ส่ง Modbus Polling (ข้ามถ้ากำลังสั่งหยุดชั่วคราว หรือเขียน Register) ────────
                if (!_isWritingOrTesting && !_isPollingPaused)
                {
                    SendPH08RoundRobinReadRequest();
                }
            }
            catch (Exception ex)
            {
                HandleDisconnect(ex.Message);
            }
            finally
            {
                _timerBusy = false;
            }
        }

        private void SendPH08RoundRobinReadRequest()
        {
            if (_serialPortPh08 == null || !_serialPortPh08.IsOpen || _isWritingOrTesting) return;

            try
            {
                _ph08TimeoutCount++;
                if (_ph08TimeoutCount >= 3)
                {
                    UpdatePH08Status($"Timeout (No response from ID {_currentPh08SlaveId})", Color.Red);
                }

                ushort startRegister;
                ushort quantity;

                if (_ph08PollStep == 0)
                {
                    // Block 1: Read Register 1 (Quantity 24) -> Tx: 01 03 00 01 00 18 (For txt_Runflag at Reg 1)
                    startRegister = 1;
                    quantity = 24;
                    _ph08PollStep = 1; // Next step will read Block 2
                }
                else
                {
                    // Block 2: Read Register 1000 (Quantity 64) -> Tx: 01 03 03 E8 00 40 (For txt_Address1001 & 1002)
                    startRegister = 1000;
                    quantity = 64;
                    _ph08PollStep = 0; // Next step will read Block 1
                }

                byte[] data = new byte[6];
                data[0] = _currentPh08SlaveId;
                data[1] = 0x03; // Function Code
                data[2] = (byte)(startRegister >> 8);
                data[3] = (byte)(startRegister & 0xFF);
                data[4] = (byte)(quantity >> 8);
                data[5] = (byte)(quantity & 0xFF);

                byte[] crc = CalculateCRC(data);
                byte[] frame = new byte[8];
                Array.Copy(data, 0, frame, 0, 6);
                frame[6] = crc[0]; // CRC Low
                frame[7] = crc[1]; // CRC High

                lock (_ph08Lock)
                {
                    _serialPortPh08.Write(frame, 0, frame.Length);
                }
            }
            catch (Exception ex)
            {
                if (ex is System.IO.IOException || ex is UnauthorizedAccessException)
                {
                    HandleDisconnect($"พอร์ต PH-08 หลุดระหว่างส่งคำสั่ง Polling ({ex.Message})");
                }
                else
                {
                    UpdatePH08Status($"Write Error: {ex.Message}", Color.Red);
                }
            }
        }

        private void Ph08_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPortPh08 == null || !_serialPortPh08.IsOpen) return;

            try
            {
                int bytesToRead = _serialPortPh08.BytesToRead;
                if (bytesToRead <= 0) return;

                byte[] buffer = new byte[bytesToRead];
                _serialPortPh08.Read(buffer, 0, bytesToRead);

                lock (_ph08Lock)
                {
                    _ph08RxBuffer.AddRange(buffer);
                    if (!_isTestingSwitchId)
                    {
                        ParsePH08ModbusRTU();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is System.IO.IOException || ex is UnauthorizedAccessException)
                {
                    HandleDisconnect($"พอร์ต PH-08 หลุดระหว่างอ่านข้อมูล ({ex.Message})");
                }
            }
        }

        private void ParsePH08ModbusRTU()
        {
            while (_ph08RxBuffer.Count >= 5)
            {
                // 1. Search for matching slave ID
                if (_ph08RxBuffer[0] != _currentPh08SlaveId)
                {
                    _ph08RxBuffer.RemoveAt(0);
                    continue;
                }

                byte functionCode = _ph08RxBuffer[1];

                // 2. Check for Exception Responses (0x83, 0x90, 0x86) -> Length: 5 bytes
                if (functionCode == 0x83 || functionCode == 0x90 || functionCode == 0x86)
                {
                    if (_ph08RxBuffer.Count < 5) break;

                    byte[] exFrame = _ph08RxBuffer.Take(5).ToArray();
                    byte[] crcCheck = CalculateCRC(exFrame.Take(3).ToArray());

                    if (crcCheck[0] == exFrame[3] && crcCheck[1] == exFrame[4])
                    {
                        _ph08RxBuffer.RemoveRange(0, 5);
                        _ph08TimeoutCount = 0;
                        byte exCode = exFrame[2];
                        if (functionCode == 0x83)
                        {
                            _lastPh08ReadExceptionCode = exCode;
                        }
                        string action = functionCode == 0x83 ? "Read" : "Write";
                        UpdatePH08Status($"{action} Exception 0x{exCode:X2} (ID {_currentPh08SlaveId})", Color.OrangeRed);
                        continue;
                    }
                    else
                    {
                        _ph08RxBuffer.RemoveAt(0);
                        continue;
                    }
                }

                // 3. Modbus Write Multiple Registers Echo Response (Function 0x10) -> Length: 8 bytes
                if (functionCode == 0x10 || functionCode == 0x06)
                {
                    if (_ph08RxBuffer.Count < 8) break;

                    byte[] writeFrame = _ph08RxBuffer.Take(8).ToArray();
                    byte[] crcCheck = CalculateCRC(writeFrame.Take(6).ToArray());

                    if (crcCheck[0] == writeFrame[6] && crcCheck[1] == writeFrame[7])
                    {
                        _ph08RxBuffer.RemoveRange(0, 8);
                        _ph08TimeoutCount = 0;
                        _lastPh08WriteEchoFrame = writeFrame;

                        ushort reg = (ushort)((writeFrame[2] << 8) | writeFrame[3]);
                        ushort qtyOrVal = (ushort)((writeFrame[4] << 8) | writeFrame[5]);

                        UpdatePH08Status($"Write Reg {reg} OK", Color.ForestGreen);
                        continue;
                    }
                    else
                    {
                        _ph08RxBuffer.RemoveAt(0);
                        continue;
                    }
                }

                // 4. Modbus Read Normal Response (Function 0x03)
                if (functionCode == 0x03)
                {
                    byte byteCount = _ph08RxBuffer[2];
                    int expectedFrameLength = 3 + byteCount + 2; // Header(3) + Data(byteCount) + CRC(2)

                    if (_ph08RxBuffer.Count < expectedFrameLength)
                    {
                        // Need more bytes
                        break;
                    }

                    byte[] frame = _ph08RxBuffer.Take(expectedFrameLength).ToArray();
                    _ph08RxBuffer.RemoveRange(0, expectedFrameLength);

                    // Verify CRC
                    byte[] dataForCrc = frame.Take(expectedFrameLength - 2).ToArray();
                    byte[] calcCrc = CalculateCRC(dataForCrc);

                    if (calcCrc[0] == frame[expectedFrameLength - 2] && calcCrc[1] == frame[expectedFrameLength - 1])
                    {
                        // Valid Frame!
                        _ph08TimeoutCount = 0;
                        _lastPh08ReadResponseFrame = frame;
                        UpdatePH08Status($"Online (ID {_currentPh08SlaveId})", Color.ForestGreen);

                        // If byteCount == 48 (24 Registers, starting from Register 1):
                        // Register 1 (RunFlag) = frame[3], frame[4] (Bytes 4-5 of frame: 00 01 or 00 00)
                        if (byteCount == 48)
                        {
                            ushort runFlag = (ushort)((frame[3] << 8) | frame[4]);
                            ParamRunFlag = runFlag.ToString();
                            _paramForm?.UpdateLiveValues(null, null, ParamRunFlag);
                        }
                        // If byteCount >= 6 (e.g. 128 bytes, 64 Registers, starting from Register 1000):
                        // Regis 1000 = Offset 0, 1 (Frame index 3, 4)
                        // Regis 1001 = Offset 2, 3 (Frame index 5, 6)
                        // Regis 1002 = Offset 4, 5 (Frame index 7, 8)
                        else if (byteCount >= 6)
                        {
                            ushort reg1001 = (ushort)((frame[5] << 8) | frame[6]);
                            ushort reg1002 = (ushort)((frame[7] << 8) | frame[8]);
                            ParamDeviceAdr = reg1001.ToString();
                            ParamModuleType = reg1002.ToString();
                            _paramForm?.UpdateLiveValues(ParamDeviceAdr, ParamModuleType, null);
                        }
                        continue;
                    }
                    else
                    {
                        // CRC mismatch, drop 1 byte and re-sync
                        continue;
                    }
                }

                // If unknown function code, advance 1 byte
                _ph08RxBuffer.RemoveAt(0);
            }
        }

        #endregion

        #region Modbus Write (Function 0x10 / 16 with Collision Prevention)

        public async Task<bool> WritePH08RegisterAsync(ushort registerAddress, string inputText, Control? sourceControl = null)
        {
            if (_serialPortPh08 == null || !_serialPortPh08.IsOpen)
            {
                MessageBox.Show("กรุณาเชื่อมต่อพอร์ต PH-08 ก่อน", "Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!ushort.TryParse(inputText, out ushort valueToWrite))
            {
                MessageBox.Show($"กรุณากรอกตัวเลขที่ถูกต้องสำหรับ Register {registerAddress}", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                sourceControl?.Focus();
                return false;
            }

            try
            {
                UpdatePH08Status($"Writing Reg {registerAddress} = {valueToWrite}...", Color.FromArgb(0, 120, 215));
                bool ok = await SendPH08WriteRegistersAsync(registerAddress, new ushort[] { valueToWrite });
                if (ok)
                {
                    if (registerAddress == 1001) ParamDeviceAdr = inputText;
                    else if (registerAddress == 1002) ParamModuleType = inputText;
                    else if (registerAddress == 1) ParamRunFlag = inputText;
                }
                return ok;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to write Register {registerAddress}:\n{ex.Message}", "Write Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdatePH08Status($"Write Error: {ex.Message}", Color.Red);
                return false;
            }
        }

        public void RenderSettingGearIcon()
        {
            if (setting == null || setting.IsDisposed) return;
            int w = setting.Width > 0 ? setting.Width : 25;
            int h = setting.Height > 0 ? setting.Height : 25;

            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                float cx = w / 2f;
                float cy = h / 2f;
                float outerR = Math.Min(w, h) * 0.44f;
                float innerR = Math.Min(w, h) * 0.30f;
                float holeR = Math.Min(w, h) * 0.14f;
                int teeth = 6;

                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    for (int i = 0; i < teeth; i++)
                    {
                        double angle1 = (i * 360.0 / teeth - 15) * Math.PI / 180.0;
                        double angle2 = (i * 360.0 / teeth - 8) * Math.PI / 180.0;
                        double angle3 = (i * 360.0 / teeth + 8) * Math.PI / 180.0;
                        double angle4 = (i * 360.0 / teeth + 15) * Math.PI / 180.0;

                        float x1 = cx + (float)(innerR * Math.Cos(angle1));
                        float y1 = cy + (float)(innerR * Math.Sin(angle1));
                        float x2 = cx + (float)(outerR * Math.Cos(angle2));
                        float y2 = cy + (float)(outerR * Math.Sin(angle2));
                        float x3 = cx + (float)(outerR * Math.Cos(angle3));
                        float y3 = cy + (float)(outerR * Math.Sin(angle3));
                        float x4 = cx + (float)(innerR * Math.Cos(angle4));
                        float y4 = cy + (float)(innerR * Math.Sin(angle4));

                        path.AddLine(x1, y1, x2, y2);
                        path.AddLine(x2, y2, x3, y3);
                        path.AddLine(x3, y3, x4, y4);
                    }
                    path.CloseFigure();
                    path.AddEllipse(cx - holeR, cy - holeR, holeR * 2, holeR * 2);

                    using (var brush = new SolidBrush(Color.FromArgb(90, 100, 115)))
                    {
                        g.FillPath(brush, path);
                    }
                }
            }

            var old = setting.Image;
            setting.Image = bmp;
            setting.Cursor = Cursors.Hand;
            old?.Dispose();
        }

        private void Setting_Click(object? sender, EventArgs e)
        {
            using var popup = new FormParameter(this);
            _paramForm = popup;
            popup.ShowDialog(this);
            _paramForm = null;
        }

        private async Task<bool> SendPH08WriteRegistersAsync(ushort startRegister, ushort[] values)
        {
            if (_serialPortPh08 == null || !_serialPortPh08.IsOpen) return false;

            _isWritingOrTesting = true;

            try
            {
                // Wait small silence interval for RS-485 line to be completely idle
                await Task.Delay(60);

                lock (_ph08Lock)
                {
                    _ph08RxBuffer.Clear();
                }

                // Modbus Function 0x10 (Write Multiple Registers)
                ushort quantity = (ushort)values.Length;
                byte byteCount = (byte)(quantity * 2);

                byte[] payload = new byte[7 + byteCount];
                payload[0] = _currentPh08SlaveId;
                payload[1] = 0x10; // Function Code 16
                payload[2] = (byte)(startRegister >> 8);
                payload[3] = (byte)(startRegister & 0xFF);
                payload[4] = (byte)(quantity >> 8);
                payload[5] = (byte)(quantity & 0xFF);
                payload[6] = byteCount;

                for (int i = 0; i < values.Length; i++)
                {
                    payload[7 + (i * 2)] = (byte)(values[i] >> 8);
                    payload[7 + (i * 2) + 1] = (byte)(values[i] & 0xFF);
                }

                byte[] crc = CalculateCRC(payload);
                byte[] frame = new byte[payload.Length + 2];
                Array.Copy(payload, 0, frame, 0, payload.Length);
                frame[frame.Length - 2] = crc[0];
                frame[frame.Length - 1] = crc[1];

                lock (_ph08Lock)
                {
                    _serialPortPh08.Write(frame, 0, frame.Length);
                }

                // Wait for PH-08 to process write and send echo response (150ms)
                await Task.Delay(150);

                _ph08TimeoutCount = 0;
                return true;
            }
            finally
            {
                _isWritingOrTesting = false;
            }
        }

        public byte CurrentPh08SlaveId => _currentPh08SlaveId;

        public async Task<(bool success, byte[]? echoFrame, string message)> SendPH08WriteRegistersWithResponseAsync(ushort startRegister, ushort[] values, int timeoutMs = 2000, CancellationToken cancellationToken = default)
        {
            if (_serialPortPh08 == null || !_serialPortPh08.IsOpen)
                return (false, null, "พอร์ต PH-08 ไม่ได้เชื่อมต่อ");

            _isWritingOrTesting = true;

            try
            {
                await Task.Delay(60, cancellationToken);

                lock (_ph08Lock)
                {
                    _ph08RxBuffer.Clear();
                    _lastPh08WriteEchoFrame = null;
                }

                ushort quantity = (ushort)values.Length;
                byte byteCount = (byte)(quantity * 2);

                byte[] payload = new byte[7 + byteCount];
                payload[0] = _currentPh08SlaveId;
                payload[1] = 0x10; // Function Code 16
                payload[2] = (byte)(startRegister >> 8);
                payload[3] = (byte)(startRegister & 0xFF);
                payload[4] = (byte)(quantity >> 8);
                payload[5] = (byte)(quantity & 0xFF);
                payload[6] = byteCount;

                for (int i = 0; i < values.Length; i++)
                {
                    payload[7 + (i * 2)] = (byte)(values[i] >> 8);
                    payload[7 + (i * 2) + 1] = (byte)(values[i] & 0xFF);
                }

                byte[] crc = CalculateCRC(payload);
                byte[] frame = new byte[payload.Length + 2];
                Array.Copy(payload, 0, frame, 0, payload.Length);
                frame[frame.Length - 2] = crc[0];
                frame[frame.Length - 1] = crc[1];

                lock (_ph08Lock)
                {
                    _serialPortPh08.Write(frame, 0, frame.Length);
                }

                int elapsed = 0;
                while (elapsed < timeoutMs)
                {
                    await Task.Delay(50, cancellationToken);
                    elapsed += 50;

                    lock (_ph08Lock)
                    {
                        if (_lastPh08WriteEchoFrame != null)
                        {
                            byte[] echo = _lastPh08WriteEchoFrame;
                            if (echo.Length >= 8 && echo[0] == _currentPh08SlaveId && echo[1] == 0x10)
                            {
                                return (true, echo, "OK");
                            }
                        }
                    }
                }

                return (false, null, $"Timeout (ไม่ได้รับการตอบกลับจาก PH-08 สำหรับ Register {startRegister})");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (ex is System.IO.IOException || ex is UnauthorizedAccessException)
                {
                    HandleDisconnect($"พอร์ต PH-08 หลุดระหว่างเขียนรีจิสเตอร์ ({ex.Message})");
                }
                return (false, null, ex.Message);
            }
            finally
            {
                _isWritingOrTesting = false;
            }
        }

        public async Task<bool> TestPH08SlaveIdResponseAsync(byte slaveId, int timeoutMs = 400)
        {
            return await TestPH08SlaveIdDualBlockResponseAsync(slaveId, timeoutMs);
        }

        public async Task<bool> TestPH08SlaveIdDualBlockResponseAsync(byte slaveId, int timeoutPerBlockMs = 400)
        {
            if (_serialPortPh08 == null || !_serialPortPh08.IsOpen) return false;

            _isWritingOrTesting = true;
            _isTestingSwitchId = true;

            try
            {
                // 1. Read System Block (Reg 0..23, 24 Regs = 48 Bytes)
                bool sysBlockOk = await ReadAndValidateBlockAsync(slaveId, 0, 24, 48, timeoutPerBlockMs);
                if (!sysBlockOk) return false;

                await Task.Delay(40);

                // 2. Read Map Table Block (Reg 1000..1063, 64 Regs = 128 Bytes)
                bool mapBlockOk = await ReadAndValidateBlockAsync(slaveId, 1000, 64, 128, timeoutPerBlockMs);
                return mapBlockOk;
            }
            catch
            {
                return false;
            }
            finally
            {
                _isTestingSwitchId = false;
                _isWritingOrTesting = false;
            }
        }

        private async Task<bool> ReadAndValidateBlockAsync(byte slaveId, ushort startReg, ushort qty, byte expectedByteCount, int timeoutMs)
        {
            lock (_ph08Lock)
            {
                _ph08RxBuffer.Clear();
            }

            // Build Modbus RTU Read Request (Function 0x03)
            byte[] payload = new byte[6];
            payload[0] = slaveId;
            payload[1] = 0x03;
            payload[2] = (byte)(startReg >> 8);
            payload[3] = (byte)(startReg & 0xFF);
            payload[4] = (byte)(qty >> 8);
            payload[5] = (byte)(qty & 0xFF);

            byte[] crc = CalculateCRC(payload);
            byte[] frame = new byte[8];
            Array.Copy(payload, 0, frame, 0, 6);
            frame[6] = crc[0];
            frame[7] = crc[1];

            lock (_ph08Lock)
            {
                _serialPortPh08?.Write(frame, 0, frame.Length);
            }

            int expectedFrameLength = 3 + expectedByteCount + 2; // SlaveID(1) + Func(1) + ByteCount(1) + Data(expectedByteCount) + CRC(2)
            int elapsed = 0;

            while (elapsed < timeoutMs)
            {
                await Task.Delay(30);
                elapsed += 30;

                lock (_ph08Lock)
                {
                    // Scan buffer for matching response frame
                    for (int i = 0; i <= _ph08RxBuffer.Count - expectedFrameLength; i++)
                    {
                        if (_ph08RxBuffer[i] == slaveId && 
                            _ph08RxBuffer[i + 1] == 0x03 && 
                            _ph08RxBuffer[i + 2] == expectedByteCount)
                        {
                            byte[] candidate = _ph08RxBuffer.Skip(i).Take(expectedFrameLength).ToArray();
                            byte[] calcCrc = CalculateCRC(candidate.Take(expectedFrameLength - 2).ToArray());

                            if (calcCrc[0] == candidate[expectedFrameLength - 2] && calcCrc[1] == candidate[expectedFrameLength - 1])
                            {
                                return true; // Valid frame with matching Slave ID, correct byte count, and valid CRC!
                            }
                        }
                    }
                }
            }

            return false;
        }

        public async Task<(bool success, byte[]? rxFrame, string message)> SendPH08ReadRegistersWithValidationAsync(ushort startRegister, ushort quantity, int timeoutMs = 2000, CancellationToken cancellationToken = default)
        {
            if (_serialPortPh08 == null || !_serialPortPh08.IsOpen)
                return (false, null, "พอร์ต PH-08 ไม่ได้เชื่อมต่อ");

            _isWritingOrTesting = true;

            try
            {
                await Task.Delay(60, cancellationToken);

                lock (_ph08Lock)
                {
                    _ph08RxBuffer.Clear();
                }

                byte expectedByteCount = (byte)(quantity * 2);
                int expectedLength = 3 + expectedByteCount + 2; // Header(3) + Data(expectedByteCount) + CRC(2)

                lock (_ph08Lock)
                {
                    _ph08RxBuffer.Clear();
                    _lastPh08ReadResponseFrame = null;
                    _lastPh08ReadExceptionCode = null;
                }

                // Modbus Function 0x03 (Read Holding Registers)
                byte[] payload = new byte[6];
                payload[0] = _currentPh08SlaveId;
                payload[1] = 0x03;
                payload[2] = (byte)(startRegister >> 8);
                payload[3] = (byte)(startRegister & 0xFF);
                payload[4] = (byte)(quantity >> 8);
                payload[5] = (byte)(quantity & 0xFF);

                byte[] crc = CalculateCRC(payload);
                byte[] frame = new byte[8];
                Array.Copy(payload, 0, frame, 0, 6);
                frame[6] = crc[0];
                frame[7] = crc[1];

                lock (_ph08Lock)
                {
                    _serialPortPh08.Write(frame, 0, frame.Length);
                }

                int elapsed = 0;
                while (elapsed < timeoutMs)
                {
                    await Task.Delay(40, cancellationToken);
                    elapsed += 40;

                    lock (_ph08Lock)
                    {
                        if (_lastPh08ReadExceptionCode.HasValue)
                        {
                            return (false, null, $"Modbus Read Exception 0x{_lastPh08ReadExceptionCode.Value:X2}");
                        }

                        if (_lastPh08ReadResponseFrame != null)
                        {
                            byte[] rx = _lastPh08ReadResponseFrame;
                            if (rx.Length >= expectedLength && rx[0] == _currentPh08SlaveId && rx[1] == 0x03 && rx[2] == expectedByteCount)
                            {
                                return (true, rx, "OK");
                            }
                        }
                    }
                }

                return (false, null, $"Timeout (ไม่ได้รับเฟรมตอบกลับ Read Reg {startRegister} ขนาด {expectedLength} Bytes ภายใน {timeoutMs}ms)");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (ex is System.IO.IOException || ex is UnauthorizedAccessException)
                {
                    HandleDisconnect($"พอร์ต PH-08 หลุดระหว่างอ่านรีจิสเตอร์ ({ex.Message})");
                }
                return (false, null, ex.Message);
            }
            finally
            {
                _isWritingOrTesting = false;
            }
        }

        #endregion

        private void UpdatePH08Status(string message, Color color)
        {
            try
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        // 1. Determine ON / OFF state: True as long as Serial Port is Open & Connected
                        bool isPortConnected = _serialPortPh08 != null && _serialPortPh08.IsOpen;
                        bool isOnline = isPortConnected;

                        if (!isPortConnected || message == "Disconnected" || message.StartsWith("Disconnect") || color == Color.Gray)
                        {
                            isOnline = false;
                        }

                        // 2. badge_connection: แสดงสถานะ เชื่อมต่อแล้ว (เขียวพาสเทล) / ยังไม่เชื่อมต่อ (แดงพาสเทล)
                        badge_connection?.SetConnected(isOnline);

                        // 4. Detail message is appended to txt_massage (Ignore repetitive "Online" polling messages)
                        if (!message.StartsWith("Online") && !string.IsNullOrWhiteSpace(message))
                        {
                            bool isInternalRegisterMsg = message.StartsWith("Writing Reg") || 
                                                          message.StartsWith("Write Reg") ||
                                                          message.StartsWith("Timeout (No response") ||
                                                          message.Contains("Exception 0x");

                            if (!isInternalRegisterMsg || IsDebugMode)
                            {
                                LogMessage(message);
                            }
                        }
                    }));
                }
            }
            catch
            {
                // UI disposed
            }
        }

        public bool IsDebugMode => chk_debug != null && chk_debug.Checked;

        public static readonly Color LogSuccessColor = Color.FromArgb(46, 204, 113); // Emerald Green
        public static readonly Color LogFailColor = Color.FromArgb(231, 76, 60);    // Crimson Red
        public static readonly Color LogNormalColor = Color.White;                  // White

        public void LogMessage(string message, Color? color = null, bool isDebugOnly = false)
        {
            if (isDebugOnly && !IsDebugMode)
            {
                return;
            }

            try
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        string logLine = $"[{DateTime.Now:HH:mm:ss}] {message}";
                        Color textColor = color ?? LogNormalColor;

                        if (string.IsNullOrWhiteSpace(txt_massage.Text))
                        {
                            txt_massage.AppendText(logLine, textColor);
                        }
                        else
                        {
                            txt_massage.AppendText(Environment.NewLine + logLine, textColor);
                        }
                    }));
                }
            }
            catch { }
        }

        public void LogMessage(string message, bool isDebugOnly)
        {
            LogMessage(message, color: null, isDebugOnly: isDebugOnly);
        }

        public void LogDebug(string message, Color? color = null)
        {
            LogMessage(message, color, isDebugOnly: true);
        }

        private byte[] CalculateCRC(byte[] data)
        {
            ushort crc = 0xFFFF;

            foreach (byte b in data)
            {
                crc ^= b;

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                        crc = (ushort)((crc >> 1) ^ 0xA001);
                    else
                        crc >>= 1;
                }
            }

            return new byte[] { (byte)(crc & 0xFF), (byte)(crc >> 8) };
        }

        private void SetPH08InputsEnabled(bool enabled)
        {
            cmb_ComportPh08.Enabled = enabled;
            txt_AddressPh08.Enabled = enabled;
            cmb_BaudratePh08.Enabled = enabled;
            openadd_Ph08.Enabled = enabled;

            if (openadd_Ph08 != null)
            {
                if (enabled)
                {
                    openadd_Ph08.BackColor = Color.FromArgb(241, 248, 241);
                    openadd_Ph08.BorderColor = Color.FromArgb(241, 248, 241);
                    openadd_Ph08.ForeColor = Color.FromArgb(27, 94, 32);
                    openadd_Ph08.Cursor = Cursors.Hand;
                }
                else
                {
                    openadd_Ph08.BackColor = Color.FromArgb(235, 238, 240);
                    openadd_Ph08.BorderColor = Color.FromArgb(215, 219, 223);
                    openadd_Ph08.ForeColor = Color.FromArgb(125, 140, 148);
                    openadd_Ph08.Cursor = Cursors.Default;
                }
                openadd_Ph08.Invalidate();
            }
        }

        private void ClosePH08SerialPort()
        {
            try
            {
                if (_serialPortPh08 != null)
                {
                    if (_serialPortPh08.IsOpen)
                    {
                        _serialPortPh08.Close();
                    }
                    _serialPortPh08.Dispose();
                    _serialPortPh08 = null;
                }
            }
            catch
            {
                // Ignored
            }
        }

        private void Form1_PH08_FormClosing(object? sender, FormClosingEventArgs e)
        {
            StopPH08Polling();
            ClosePH08SerialPort();
        }

        public string? FindUnavailableConnectedPort()
        {
            try
            {
                var availablePorts = new HashSet<string>(
                    SerialPort.GetPortNames(),
                    StringComparer.OrdinalIgnoreCase);

                if (_serialPortPh08 != null && _serialPortPh08.IsOpen &&
                    !availablePorts.Contains(_serialPortPh08.PortName))
                {
                    return $"PH-08 ({_serialPortPh08.PortName})";
                }

                if (_serialPortDevice != null && _serialPortDevice.IsOpen &&
                    !availablePorts.Contains(_serialPortDevice.PortName))
                {
                    return $"Device ({_serialPortDevice.PortName})";
                }
            }
            catch { }

            return null;
        }

        public void HandleDisconnect(string reason)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleDisconnect(reason)));
                return;
            }

            if (_disconnectInProgress) return;
            _disconnectInProgress = true;

            try
            {
                // 1. หยุด Polling Timer ทันที
                StopPH08Polling();

                // 2. ยกเลิกการทดสอบทันทีถ้ากำลังรันอยู่
                if (_testCts != null && !_testCts.IsCancellationRequested)
                {
                    try
                    {
                        _testCts.Cancel();
                    }
                    catch (ObjectDisposedException) { }
                }

                // 3. ปิดพอร์ตทั้งหมดอย่างปลอดภัย
                ClosePH08SerialPort();
                CloseDeviceSerialPort();

                // 4. รีเซ็ตสถานะภายใน
                _connectedPh08PortName = null;
                _connectedDevicePortName = null;
                _isWritingOrTesting = false;
                _ph08TimeoutCount = 0;

                // 5. คืนค่า UI ทั้งหมด
                btn_connect.Text = "🔗Connect";
                SetAllInputsEnabled(true);
                UpdatePH08Status("Disconnected", Color.Gray);
                UpdateTestButtonAndReadyState(false);
                btn_ReadDevice.Enabled = false;
                ResetPhaseIndicators();

                // 6. แจ้งเตือนผู้ใช้เหมือน CMA และ DEF
                if (reason != "UserDisconnect")
                {
                    LogMessage($"[PORT DISCONNECTED] การเชื่อมต่อขาดหาย: {reason}", Color.Red);
                    MessageBox.Show($"การเชื่อมต่อขาดหาย: {reason}", "Connection Lost",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    LogMessage("[DISCONNECT] ผู้ใช้งานตัดการเชื่อมต่อพอร์ต", Color.DarkOrange);
                }
            }
            finally
            {
                _disconnectInProgress = false;
            }
        }
    }
}
