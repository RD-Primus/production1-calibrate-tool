using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO.Ports;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.IO;

namespace Tower_Light
{
    public partial class Form1 : Form
    {
        public SerialPort _serialPort;
        private static readonly ushort[] wCRCTable = {
        0X0000, 0XC0C1, 0XC181, 0X0140, 0XC301, 0X03C0, 0X0280, 0XC241, 0XC601, 0X06C0, 0X0780, 0XC741, 0X0500, 0XC5C1, 0XC481, 0X0440,
        0XCC01, 0X0CC0, 0X0D80, 0XCD41, 0X0F00, 0XCFC1, 0XCE81, 0X0E40, 0X0A00, 0XCAC1, 0XCB81, 0X0B40, 0XC901, 0X09C0, 0X0880, 0XC841,
        0XD801, 0X18C0, 0X1980, 0XD941, 0X1B00, 0XDBC1, 0XDA81, 0X1A40, 0X1E00, 0XDEC1, 0XDF81, 0X1F40, 0XDD01, 0X1DC0, 0X1C80, 0XDC41,
        0X1400, 0XD4C1, 0XD581, 0X1540, 0XD701, 0X17C0, 0X1680, 0XD641, 0XD201, 0X12C0, 0X1380, 0XD341, 0X1100, 0XD1C1, 0XD081, 0X1040,
        0XF001, 0X30C0, 0X3180, 0XF141, 0X3300, 0XF3C1, 0XF281, 0X3240, 0X3600, 0XF6C1, 0XF781, 0X3740, 0XF501, 0X35C0, 0X3480, 0XF441,
        0X3C00, 0XFCC1, 0XFD81, 0X3D40, 0XFF01, 0X3FC0, 0X3E80, 0XFE41, 0XFA01, 0X3AC0, 0X3B80, 0XFB41, 0X3900, 0XF9C1, 0XF881, 0X3840,
        0X2800, 0XE8C1, 0XE981, 0X2940, 0XEB01, 0X2BC0, 0X2A80, 0XEA41, 0XEE01, 0X2EC0, 0X2F80, 0XEF41, 0X2D00, 0XEDC1, 0XEC81, 0X2C40,
        0XE401, 0X24C0, 0X2580, 0XE541, 0X2700, 0XE7C1, 0XE681, 0X2640, 0X2200, 0XE2C1, 0XE381, 0X2340, 0XE101, 0X21C0, 0X2080, 0XE041,
        0XA001, 0X60C0, 0X6180, 0XA141, 0X6300, 0XA3C1, 0XA281, 0X6240, 0X6600, 0XA6C1, 0XA781, 0X6740, 0XA501, 0X65C0, 0X6480, 0XA441,
        0X6C00, 0XACC1, 0XAD81, 0X6D40, 0XAF01, 0X6FC0, 0X6E80, 0XAE41, 0XAA01, 0X6AC0, 0X6B80, 0XAB41, 0X6900, 0XA9C1, 0XA881, 0X6840,
        0X7800, 0XB8C1, 0XB981, 0X7940, 0XBB01, 0X7BC0, 0X7A80, 0XBA41, 0XBE01, 0X7EC0, 0X7F80, 0XBF41, 0X7D00, 0XBDC1, 0XBC81, 0X7C40,
        0XB401, 0X74C0, 0X7580, 0XB541, 0X7700, 0XB7C1, 0XB681, 0X7640, 0X7200, 0XB2C1, 0XB381, 0X7340, 0XB101, 0X71C0, 0X7080, 0XB041,
        0X5000, 0X90C1, 0X9181, 0X5140, 0X9301, 0X53C0, 0X5280, 0X9241, 0X9601, 0X56C0, 0X5780, 0X9741, 0X5500, 0X95C1, 0X9481, 0X5440,
        0X9C01, 0X5CC0, 0X5D80, 0X9D41, 0X5F00, 0X9FC1, 0X9E81, 0X5E40, 0X5A00, 0X9AC1, 0X9B81, 0X5B40, 0X9901, 0X59C0, 0X5880, 0X9841,
        0X8801, 0X48C0, 0X4980, 0X8941, 0X4B00, 0X8BC1, 0X8A81, 0X4A40, 0X4E00, 0X8EC1, 0X8F81, 0X4F40, 0X8D01, 0X4DC0, 0X4C80, 0X8C41,
        0X4400, 0X84C1, 0X8581, 0X4540, 0X8701, 0X47C0, 0X4680, 0X8641, 0X8201, 0X42C0, 0X4380, 0X8341, 0X4100, 0X81C1, 0X8081, 0X4040
        };

        private bool _isReading = false;
        private readonly Label[] _registerLabels;
        private System.Windows.Forms.Timer _readTimer;
        private const ushort PULSE_THRESHOLD = 500;

        private const byte SLAVE_LAMP = 0x01;
        private const byte SLAVE_MCU = 0x08;
        private const ushort REG_GREEN = 0;
        private const ushort REG_YELLOW = 1;
        private const ushort REG_RED = 2;
        private const ushort REG_BLUE = 3;
        private const ushort REG_WHITE = 4;
        private const ushort REG_MODE = 5;

        private int _testCount = 0;
        private bool _lastTestPass = false;
        private string _csvPath = "";
        private readonly HttpClient _httpClient = new HttpClient();
        private string[] _staticResult = new string[5] { "FAIL", "FAIL", "FAIL", "FAIL", "FAIL" };
        private string[] _blinkResult = new string[5] { "FAIL", "FAIL", "FAIL", "FAIL", "FAIL" };

        private ushort[] _latestRegs = new ushort[17];
        private Dictionary<int, int> ColorToRelay = new Dictionary<int, int>();
        private Dictionary<int, int> ColorToSensor = new Dictionary<int, int>();
        private Dictionary<int, ushort> StandardLDR = new Dictionary<int, ushort>();

        private List<byte> _rxBuffer = new List<byte>();
        //private bool _isTesting = false;
        private bool _isWaitingReply = false;
        private int _timeoutCounter = 0;

        // แก้ไขจุด Nullable Reference Type: ใส่เครื่องหมาย ? หลังคลาส TaskCompletionSource
        private TaskCompletionSource<byte[]>? _modbusResponseTcs = null;
        private string _templatePath = "";
        private string _excelSavePath = "";

        private string GetColorName(int id)
        {
            return id switch { 1 => "Red", 2 => "Yellow", 3 => "Green", 4 => "Blue", 5 => "White", _ => "Unknown" };
        }

        private Rectangle _originalFormSize;
        private Dictionary<Control, Rectangle> _originalControlBounds = new Dictionary<Control, Rectangle>();
        private Dictionary<Control, float> _originalControlFonts = new Dictionary<Control, float>();

        private bool _isBlinking = false;
        private int _selectedTiers = 3;

        private int _goodCount = 0;
        private int _defectCount = 0;
        private double _yieldPercent = 0.0;

        private bool _is220VAC = false; // false = 24VDC, true = 220VAC

        public Form1()
        {
            InitializeComponent();
            _serialPort = new SerialPort();
            int[] baudRates = { 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200, 128000, 256000 };

            baud_combo.Items.AddRange(baudRates.Select(b => b.ToString()).ToArray());
            baud_combo.SelectedItem = "9600";

            string[] lightcolors = { "Green", "Yellow", "Red", "White", "Blue" };
            comboColor1.Items.AddRange(lightcolors.Select(b => b.ToString()).ToArray());
            comboColor1.SelectedItem = "Green";
            comboColor2.Items.AddRange(lightcolors.Select(b => b.ToString()).ToArray());
            comboColor2.SelectedItem = "Yellow";
            comboColor3.Items.AddRange(lightcolors.Select(b => b.ToString()).ToArray());
            comboColor3.SelectedItem = "Red";
            comboColor4.Items.AddRange(lightcolors.Select(b => b.ToString()).ToArray());
            comboColor4.SelectedItem = "White";
            comboColor5.Items.AddRange(lightcolors.Select(b => b.ToString()).ToArray());
            comboColor5.SelectedItem = "Blue";

            comboColor1.SelectedIndexChanged += ComboColor_SelectedIndexChanged;
            comboColor2.SelectedIndexChanged += ComboColor_SelectedIndexChanged;
            comboColor3.SelectedIndexChanged += ComboColor_SelectedIndexChanged;
            comboColor4.SelectedIndexChanged += ComboColor_SelectedIndexChanged;
            comboColor5.SelectedIndexChanged += ComboColor_SelectedIndexChanged;
            string[] ports = SerialPort.GetPortNames();
            Portcombo.Items.AddRange(ports);
            if (ports.Length > 0) Portcombo.SelectedIndex = 0;

            _readTimer = new System.Windows.Forms.Timer();
            _readTimer.Interval = 500;
            _readTimer.Tick += ReadTimer_Tick;

            _serialPort.DataReceived += DataReceivedHandler;
            _serialPort.ErrorReceived += SerialPort_ErrorReceived;

            _registerLabels = new Label[] { adc1_val, adc2_val, adc3_val, adc4_val, adc5_val }; // แก้ไขลำดับ UI ชั่วคราวให้ตรงกับอาเรย์
            tlr_tb.Text = "150";
        }

        private void ComboColor_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // เพิ่มเครื่องหมาย ? เพื่อแก้แจ้งเตือน Converting null literal
            ComboBox? combo = sender as ComboBox;
            if (combo == null) return;

            Panel? targetPanel = null;
            if (combo == comboColor1) targetPanel = dis1;
            else if (combo == comboColor2) targetPanel = dis2;
            else if (combo == comboColor3) targetPanel = dis3;
            else if (combo == comboColor4) targetPanel = dis4;
            else if (combo == comboColor5) targetPanel = dis5;

            if (targetPanel != null)
            {
                string colorText = combo.SelectedItem?.ToString() ?? "";

                targetPanel.BackColor = colorText switch
                {
                    "Red" => Color.Red,
                    "Yellow" => Color.Yellow,
                    "Green" => Color.LimeGreen,
                    "Blue" => Color.Blue,
                    "White" => Color.White,
                    _ => Color.LightGray
                };
            }
        }

        private byte[] CalculateCRC(byte[] data)
        {
            ushort wCRCWord = 0xFFFF;
            foreach (byte b in data)
            {
                byte nTemp = (byte)(b ^ wCRCWord);
                wCRCWord >>= 8;
                wCRCWord ^= wCRCTable[nTemp];
            }
            return new byte[] { (byte)(wCRCWord & 0xFF), (byte)(wCRCWord >> 8) };
        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            Portcombo.Items.Clear();
            string[] ports = Get_compoart_list();
            if (ports.Length > 0)
            {
                Portcombo.Items.AddRange(ports);
                Portcombo.SelectedIndex = 0;
            }
        }
        private bool IsDebugMode
        {
            get
            {
                if (this.InvokeRequired)
                    return (bool)this.Invoke(new Func<bool>(() => chkDebug.Checked));
                return chkDebug.Checked;
            }
        }
        private async void Conn_btn_Click(object sender, EventArgs e)
        {
            try
            {
                if (baud_combo.SelectedItem == null)
                {
                    MessageBox.Show("Please select a Baud Rate!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!_serialPort.IsOpen)
                {
                    // 1. ล็อคปุ่มและเปลี่ยนข้อความระหว่างรอทดสอบสายสัญญาณ
                    Conn_btn.Enabled = false;
                    Conn_btn.Text = "Connecting...";

                    _serialPort.PortName = Portcombo.SelectedItem?.ToString() ?? "";
                    _serialPort.BaudRate = int.Parse(baud_combo.SelectedItem?.ToString() ?? "9600");
                    _serialPort.DataBits = 8;
                    _serialPort.Parity = Parity.None;
                    _serialPort.StopBits = StopBits.One;

                    _serialPort.WriteTimeout = 300;
                    _serialPort.ReadTimeout = 300;

                    // 2. เปิด COM Port ฝั่ง USB
                    _serialPort.Open();
                    _rxBuffer.Clear();

                    // 3. ทดลองส่ง Modbus ไปเช็คอุปกรณ์ (Ping) เพื่อทดสอบสาย A/B
                    bool isDeviceConnected = await ForceReadAdcAsync();

                    if (!isDeviceConnected)
                    {
                        // ถ้าไม่มีการตอบกลับ (สาย A/B หลุด หรือ อุปกรณ์ไม่มีไฟเลี้ยง)
                        _serialPort.Close();
                        Conn_btn.Text = "Connect";
                        MessageBox.Show("เชื่อมต่อไม่สำเร็จ!\nไม่พบสัญญาณจากอุปกรณ์ (กรุณาเช็คสาย RS485 A/B หรือไฟเลี้ยงอุปกรณ์)",
                                        "Connection Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 4. ถ้าอุปกรณ์ตอบกลับมาปกติ ถือว่าการเชื่อมต่อสมบูรณ์
                    Conn_btn.Text = "Disconnect";
                    AddLog("เชื่อมต่ออุปกรณ์สำเร็จ (RS485 OK)", Color.ForestGreen);
                }
                else
                {
                    StopReading();
                    _serialPort.Close();
                    Conn_btn.Text = "Connect";
                    AddLog("ตัดการเชื่อมต่อเรียบร้อย", Color.Gray);
                }
            }
            catch (Exception ex)
            {
                if (_serialPort.IsOpen) _serialPort.Close();
                Conn_btn.Text = "Connect";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // คืนค่าให้ปุ่มกดได้ตามปกติ
                Conn_btn.Enabled = true;
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            int available = _serialPort.BytesToRead;
            if (available <= 0) return;

            byte[] buffer = new byte[available];
            _serialPort.Read(buffer, 0, available);

            lock (_rxBuffer)
            {
                _rxBuffer.AddRange(buffer);

                while (_rxBuffer.Count >= 5)
                {
                    byte slave = _rxBuffer[0];
                    if (slave != SLAVE_MCU && slave != SLAVE_LAMP && slave != 0x02)
                    {
                        _rxBuffer.RemoveAt(0);
                        continue;
                    }

                    byte funcCode = _rxBuffer[1];
                    int expectedLen = 0;

                    if (funcCode == 0x03)
                    {
                        expectedLen = 3 + _rxBuffer[2] + 2;
                    }
                    else if (funcCode == 0x06)
                    {
                        expectedLen = 8;
                    }
                    else
                    {
                        _rxBuffer.RemoveAt(0);
                        continue;
                    }

                    if (_rxBuffer.Count < expectedLen)
                    {
                        break;
                    }

                    byte[] frame = _rxBuffer.GetRange(0, expectedLen).ToArray();
                    byte[] crc = CalculateCRC(frame.Take(expectedLen - 2).ToArray());

                    if (crc[0] == frame[expectedLen - 2] && crc[1] == frame[expectedLen - 1])
                    {
                        _rxBuffer.RemoveRange(0, expectedLen);
                        ProcessValidFrame(frame, funcCode);
                    }
                    else
                    {
                        _rxBuffer.RemoveAt(0);
                    }
                }
            }
        }

        private void ProcessValidFrame(byte[] frame, byte funcCode)
        {
            _isWaitingReply = false;
            _timeoutCounter = 0;

            _modbusResponseTcs?.TrySetResult(frame);

            this.BeginInvoke(new Action(() =>
            {
                string rxHex = BitConverter.ToString(frame).Replace("-", " ");

                // ตรวจสอบ Debug Mode ก่อนแสดง Log RX
                if (IsDebugMode)
                    AddLog($"[RX] Reply: {rxHex}", Color.Aqua);

                if (funcCode == 0x03)
                {
                    int byteCount = frame[2];
                    int totalQty = byteCount / 2;
                    int maxReg = Math.Min(totalQty, 17);

                    for (int i = 0; i < maxReg; i++)
                    {
                        _latestRegs[i] = (ushort)((frame[3 + i * 2] << 8) | frame[4 + i * 2]);
                    }

                    for (int i = 0; i < Math.Min(5, maxReg); i++)
                    {
                        if (_registerLabels[i] != null)
                            _registerLabels[i].Text = _latestRegs[i].ToString();
                    }

                    if (maxReg > 5 && _latestRegs[5] == 1)
                    {
                        _ = WriteSingleRegAsync(SLAVE_MCU, 5, 0);
                        AddLog("BTN_FLAG detected! Triggering Test...", Color.Orange);
                        Test1_btn.PerformClick();
                    }
                }
            }));
        }

        private string[] Get_compoart_list()
        {
            string[] ArrayComPortsNames = SerialPort.GetPortNames();
            if (ArrayComPortsNames.Length <= 0) return Array.Empty<string>();
            Array.Sort(ArrayComPortsNames);
            return ArrayComPortsNames;
        }

        private void ReadTimer_Tick(object? sender, EventArgs e)
        {
            if (!_serialPort.IsOpen)
            {
                HandleDisconnect();
                return;
            }

            if (_isWaitingReply)
            {
                _timeoutCounter++;
                int maxTicks = (400 / _readTimer.Interval);
                if (maxTicks < 1) maxTicks = 1;

                if (_timeoutCounter > maxTicks)
                {
                    _isWaitingReply = false;
                    lock (_rxBuffer) { _rxBuffer.Clear(); }
                }
                return;
            }

            _timeoutCounter = 0;
            _isWaitingReply = true;

            try
            {
                byte[] request = BuildReadHoldingRegisters(SLAVE_MCU, 0x0000, 17);
                _serialPort.Write(request, 0, request.Length);
            }
            catch (Exception)
            {
                HandleDisconnect();
            }
        }
        private async Task<bool> ForceReadAdcAsync()
        {
            if (!_serialPort.IsOpen) return false;

            // หยุด Timer ชั่วคราวเพื่อไม่ให้แย่งกันส่งข้อมูล
            bool wasTimerRunning = _readTimer.Enabled;
            _readTimer.Stop();

            lock (_rxBuffer) { _rxBuffer.Clear(); }
            _isWaitingReply = false;
            _modbusResponseTcs = new TaskCompletionSource<byte[]>();

            // สั่งอ่าน Register จำนวน 17 ตัวจากฝั่ง MCU (SLAVE_MCU = 0x08)
            byte[] req = BuildReadHoldingRegisters(SLAVE_MCU, 0x0000, 17);
            _serialPort.Write(req, 0, req.Length);

            var delayTask = Task.Delay(500); // รอคำตอบสูงสุด 500ms
            var completedTask = await Task.WhenAny(_modbusResponseTcs.Task, delayTask);

            if (wasTimerRunning) _readTimer.Start();
            _isWaitingReply = false;

            if (completedTask == _modbusResponseTcs.Task)
            {
                await Task.Delay(50); // รอให้ระบบอัปเดตตัวแปร _latestRegs จนเสร็จสมบูรณ์
                return true;
            }
            else
            {
                AddLog("[TX Timeout] Failed to read ADC from MCU!", Color.Red);
                return false;
            }
        }

        private byte[] BuildReadHoldingRegisters(byte slaveId, ushort startAddress, ushort quantity)
        {
            byte[] data = { slaveId, 0x03, (byte)(startAddress >> 8), (byte)(startAddress & 0xFF), (byte)(quantity >> 8), (byte)(quantity & 0xFF) };
            byte[] crc = CalculateCRC(data);
            return data.Concat(crc).ToArray();
        }

        private byte[] BuildWriteSingleRegister(byte slaveId, ushort address, ushort value)
        {
            byte[] data = { slaveId, 0x06, (byte)(address >> 8), (byte)(address & 0xFF), (byte)(value >> 8), (byte)(value & 0xFF) };
            byte[] crc = CalculateCRC(data);
            return data.Concat(crc).ToArray();
        }

        private async Task<bool> WriteSingleRegAsync(byte slaveId, ushort address, ushort value)
        {
            if (!_serialPort.IsOpen) return false;

            bool wasTimerRunning = _readTimer.Enabled;
            _readTimer.Stop();

            lock (_rxBuffer) { _rxBuffer.Clear(); }
            _isWaitingReply = false;
            _modbusResponseTcs = new TaskCompletionSource<byte[]>();

            byte[] req = BuildWriteSingleRegister(slaveId, address, value);
            string txHex = BitConverter.ToString(req).Replace("-", " ");

            // ตรวจสอบ Debug Mode ก่อนแสดง Log TX
            if (IsDebugMode)
                AddLog($"[TX Write] Reg {address} -> {value} ({txHex})", Color.Yellow);

            _serialPort.Write(req, 0, req.Length);

            var delayTask = Task.Delay(300);
            var completedTask = await Task.WhenAny(_modbusResponseTcs.Task, delayTask);

            if (wasTimerRunning) _readTimer.Start();

            _isWaitingReply = false;
            if (completedTask == _modbusResponseTcs.Task)
            {
                await Task.Delay(50);
                return true;
            }
            else
            {
                AddLog($"[TX Timeout] Write Register {address} Failed (No response)!", Color.Red);
                return false;
            }
        }

        private void StopReading()
        {
            _readTimer.Stop();
            _isReading = false;
            Read_btn.Text = "Read";
        }

        private void Read_btn_Click(object sender, EventArgs e)
        {
            if (!_serialPort.IsOpen)
            {
                MessageBox.Show("Port is not open!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_isReading)
            {
                _readTimer.Start();
                _isReading = true;
                Read_btn.Text = "Stop Read";
            }
            else
            {
                StopReading();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _readTimer?.Stop();
            if (_serialPort.IsOpen) _serialPort.Close();
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                AddLog($"[Hardware Warning] Noise Detected: {e.EventType}", Color.Orange);
                try
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.DiscardInBuffer();
                        lock (_rxBuffer) { _rxBuffer.Clear(); }
                    }
                }
                catch { }
            }));
        }

        private void HandleDisconnect()
        {
            this.BeginInvoke(new Action(() =>
            {
                StopReading();
                try { _serialPort.Close(); } catch { }
                Conn_btn.Text = "Open Port";
                MessageBox.Show("Port disconnected!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }));
        }

        private void panel1_Paint(object sender, PaintEventArgs e) { }

        private async Task AllOff()
        {
            // ⭐ เปลี่ยนจากที่เคยไปยุ่งกับ REG_MODE (Slave 1, Address 5) 
            // มาเป็นการเคลียร์สถานะกระพริบที่ Slave 2 ทั้ง 2 แอดเดรสเพื่อความชัวร์
            await WriteSingleRegAsync(0x02, 3, 0);
            await WriteSingleRegAsync(0x02, 4, 0);
            await Task.Delay(100);

            // ปิดไฟสีต่างๆ (Address 0 ถึง 4) ตามปกติ
            for (ushort r = 0; r < 5; r++)
            {
                await WriteSingleRegAsync(SLAVE_LAMP, r, 0);
                await Task.Delay(100);
            }
            await Task.Delay(500);
        }

        private async void set_btn_Click(object sender, EventArgs e)
        {
            int activeTiers = GetActiveTiers();
            if (!_serialPort.IsOpen) { MessageBox.Show("Port is not open!", "Warning"); return; }
            if (btnMode24V.BackColor != Color.LimeGreen && btnMode220V.BackColor != Color.LimeGreen)
            {
                MessageBox.Show("กรุณาเลือกโหมดการทำงาน (24V หรือ 220V) ก่อนเริ่มทำการ Set!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // เด้งออกจากการทำงานทันที
            }

            set_btn.Enabled = false;
            set_btn.Text = "Scanning...";

            if (!IsDebugMode) AddLog("กำลัง set...", Color.Black);
            else AddLog("=== Manual-Mapping Started ===", Color.Black);

            if (_isReading)
            {
                _readTimer.Stop();
                _isReading = false;
                Read_btn.Text = "Read";
                await Task.Delay(500);
            }

            ColorToRelay.Clear();
            ColorToSensor.Clear();
            StandardLDR.Clear();

            Label[] colorLabels = { Color1_val, Color2_val, Color3_val, Color4_val, Color5_val };
            Label[] stdLabels = { std1_val, std2_val, std3_val, std4_val, std5_val };
            Panel[] colorPanels = { dis1, dis2, dis3, dis4, dis5 };
            ComboBox[] colorCombos = { comboColor1, comboColor2, comboColor3, comboColor4, comboColor5 };

            for (int i = 0; i < 5; i++)
            {
                colorLabels[i].Text = "-";
                colorLabels[i].ForeColor = Color.Black;
                stdLabels[i].Text = "-";
                if (colorPanels[i] != null) colorPanels[i].BackColor = Color.LightGray;
            }

            try
            {
                // เรียกใช้ AllOff() แทน TurnOffAllRelaysAsync
                await AllOff();

                for (ushort relay = 0; relay < activeTiers; relay++)
                {
                    await WriteSingleRegAsync(SLAVE_LAMP, relay, 1);
                    if (IsDebugMode) AddLog($"Reading Relay {relay + 1} ADC... waiting 1 second.", Color.White);
                    await Task.Delay(1000);

                    bool isReadSuccess = await ForceReadAdcAsync();
                    if (!isReadSuccess)
                    {
                        throw new Exception($"Time out! MCU ไม่ตอบสนองขณะอ่านค่า Relay ที่ {relay + 1}");
                    }

                    ushort finalAdc = _latestRegs[relay];
                    stdLabels[relay].Text = finalAdc.ToString();

                    string selectedColor = colorCombos[relay].SelectedItem?.ToString() ?? "";
                    int finalColorID = selectedColor switch
                    {
                        "Red" => 1,
                        "Yellow" => 2,
                        "Green" => 3,
                        "Blue" => 4,
                        "White" => 5,
                        _ => 0
                    };

                    if (finalColorID != 0)
                    {
                        ColorToRelay[finalColorID] = relay;
                        ColorToSensor[finalColorID] = relay;
                        StandardLDR[finalColorID] = finalAdc;

                        string cName = GetColorName(finalColorID);
                        colorLabels[relay].Text = cName;
                        colorLabels[relay].ForeColor = Color.Black;

                        Color pColor = finalColorID switch
                        {
                            1 => Color.Red,
                            2 => Color.Yellow,
                            3 => Color.LimeGreen,
                            4 => Color.Blue,
                            5 => Color.White,
                            _ => Color.LightGray
                        };

                        if (colorPanels[relay] != null) colorPanels[relay].BackColor = pColor;
                        if (IsDebugMode) AddLog($"Relay {relay + 1} -> Selected: {cName}, ADC: {finalAdc}", Color.Cyan);
                    }
                    else
                    {
                        colorLabels[relay].Text = "No Color";
                        colorLabels[relay].ForeColor = Color.Black;
                        if (colorPanels[relay] != null) colorPanels[relay].BackColor = Color.LightGray;
                        if (IsDebugMode) AddLog($"Relay {relay + 1} -> No Color Selected! ADC: {finalAdc}", Color.Red);
                    }

                    await WriteSingleRegAsync(SLAVE_LAMP, relay, 0);
                    await Task.Delay(400);
                }

                if (ColorToRelay.Count == activeTiers)
                {
                    if (!IsDebugMode) AddLog("set เสร็จแล้ว", Color.ForestGreen);
                    else AddLog("=== Manual-Mapping Complete ===", Color.Black);

                    MessageBox.Show("บันทึกค่าแสงและการเลือกสีสำเร็จ!", "Set OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    AddLog($"=== Mapping Incomplete! Found {ColorToRelay.Count}/5 ===", Color.Red);
                    MessageBox.Show("พบความผิดปกติ เลือกสีไม่ครบ 5 สี หรือเลือกสีซ้ำกัน! ระบบจะทำการปิดไฟทั้งหมด", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    await AllOff();
                }
            }
            catch (Exception ex)
            {
                AddLog($"[System Error] {ex.Message}", Color.Red);
                MessageBox.Show($"หยุดการทำงานฉุกเฉิน!\nสาเหตุ: {ex.Message}\nระบบกำลังปิด Relay ทั้งหมดเพื่อความปลอดภัย", "Error Halt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                await AllOff();
            }
            finally
            {
                set_btn.Enabled = true;
                set_btn.Text = "Set";
            }
        }

        private async void Test1_btn_Click(object sender, EventArgs e)
        {
            if (!_serialPort.IsOpen) { MessageBox.Show("Port is not open!", "Warning"); return; }

            int activeTiers = GetActiveTiers();
            if (ColorToRelay.Count < activeTiers)
            {
                MessageBox.Show($"กรุณากด Set เพื่อสแกน Auto-Mapping ให้ครบ {activeTiers} ชั้นก่อนเริ่มทดสอบ!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool soundPass = false;
            string blinkMsg = "=== Blink Result ===\n";

            sn_txt.Focus();
            sn_txt.SelectAll();
            _testCount++;
            testnumber_lbl.Text = _testCount.ToString();
            Test1_btn.Enabled = false;

            Label[] statLabels = { stats1_val, stats2_val, stats3_val, stats4_val, stats5_val };
            for (int i = 0; i < 5; i++)
            {
                if (statLabels[i] != null) { statLabels[i].Text = "-"; statLabels[i].ForeColor = Color.Black; }
            }

            // ตั้งค่าตั้งต้นเป็น NONE
            _staticResult = new string[5] { "NONE", "NONE", "NONE", "NONE", "NONE" };
            _blinkResult = new string[5] { "NONE", "NONE", "NONE", "NONE", "NONE" };

            Test1_btn.Text = "Testing...";
            if (!IsDebugMode) AddLog($"กำลัง test ({activeTiers} ชั้น)...", Color.Black);
            else AddLog($"=== Test Started ({activeTiers} Tiers) ===", Color.Black);

            bool allPass = true;
            ushort tol = ushort.TryParse(tlr_tb.Text, out ushort t) ? t : (ushort)150;

            _isReading = true;
            Read_btn.Text = "Stop Read";
            _readTimer.Stop();
            _readTimer.Interval = 500;
            _readTimer.Start();

            await AllOff();

            // ==========================================
            // PART 1: Static Test
            // ==========================================
            int[] cIDs = new int[5];
            string[] cNames = new string[5];
            int[] minAcceptable = new int[5];
            ushort[] stdAdcs = new ushort[5];

            for (ushort relay = 0; relay < activeTiers; relay++)
            {
                int cID = ColorToRelay.FirstOrDefault(x => x.Value == relay).Key;
                cIDs[relay] = cID;
                cNames[relay] = cID != 0 ? GetColorName(cID) : "Unknown";
                ushort stdAdc = StandardLDR.ContainsKey(cID) ? StandardLDR[cID] : (ushort)0;
                stdAdcs[relay] = stdAdc;
                minAcceptable[relay] = stdAdc - tol;
            }

            if (IsDebugMode) AddLog("Testing all relays simultaneously... waiting 5 seconds.", Color.White);

            for (ushort r = 0; r < activeTiers; r++) { await WriteSingleRegAsync(SLAVE_LAMP, r, 1); }

            bool[] isPassedThisRound = new bool[5];
            ushort[] peakAdc = new ushort[5];

            for (int w = 0; w < 20; w++)
            {
                await Task.Delay(250);
                Application.DoEvents();

                bool allActivePassed = true;

                for (int relay = 0; relay < activeTiers; relay++)
                {
                    ushort currentAdc = _latestRegs[relay];
                    if (currentAdc > peakAdc[relay]) peakAdc[relay] = currentAdc;
                    if (currentAdc >= minAcceptable[relay]) isPassedThisRound[relay] = true;
                    if (!isPassedThisRound[relay]) allActivePassed = false;
                }

                if (allActivePassed)
                {
                    if (IsDebugMode) AddLog($"[Static Test] ค่า ADC ถึงเกณฑ์แล้ว ออกจากการรอที่รอบ {w + 1}", Color.Gray);
                    break;
                }
            }

            List<string> staticErrors = new List<string>();

            for (int relay = 0; relay < activeTiers; relay++)
            {
                bool pass = isPassedThisRound[relay];
                int cID = cIDs[relay];
                string cName = cNames[relay];

                int resultIdx = -1;
                if (cID == 3) resultIdx = 0;
                else if (cID == 2) resultIdx = 1;
                else if (cID == 1) resultIdx = 2;
                else if (cID == 4) resultIdx = 3;
                else if (cID == 5) resultIdx = 4;

                if (resultIdx != -1) _staticResult[resultIdx] = pass ? "PASS" : "FAIL";

                statLabels[relay].Text = pass ? "PASS" : "FAIL";
                statLabels[relay].ForeColor = pass ? Color.Green : Color.Red;

                if (pass)
                {
                    if (!IsDebugMode) AddLog($"สี {cName} ผ่านในขั้นตอน Static Test", Color.ForestGreen);
                    else AddLog($"Relay {relay + 1} ({cName}): Peak ADC={peakAdc[relay]} Std={stdAdcs[relay]} -> PASS ✔", Color.ForestGreen);
                }
                else
                {
                    allPass = false;
                    string thaiColor = cName switch { "Red" => "แดง", "Yellow" => "เหลือง", "Green" => "เขียว", "Blue" => "น้ำเงิน", "White" => "ขาว", _ => cName };
                    staticErrors.Add($"- สี{thaiColor}ไม่ติด");
                    AddLog($"สี {cName} ไม่ผ่านในขั้นตอน Static Test! (ADC={peakAdc[relay]})", Color.Red);
                }
            }

            if (!allPass && IsDebugMode) AddLog($"❌ Error detected during Static Test! (Continuing...)", Color.Red);
            await AllOff();

            // ==========================================
            // PART 2: Blink Test (ตรวจสอบ Frequency)
            // ==========================================
            bool allBlinkPass = true;
            List<string> blinkErrors = new List<string>();

            if (IsFlashEnabled)
            {
                if (IsDebugMode) AddLog("Static PASS ✔ → Starting Blink Test (10 seconds)...", Color.Cyan);
                Test1_btn.Text = "Blink Testing...";

                for (ushort r = 0; r < activeTiers; r++) { await WriteSingleRegAsync(SLAVE_LAMP, r, 1); }
                ushort blinkAddress = _is220VAC ? (ushort)3 : (ushort)4;
                await WriteSingleRegAsync(0x02, blinkAddress, 1);

                // ตัวแปรสำหรับคำนวณ Hz
                int[] totalBlinkCount = new int[5];
                bool[] isHigh = new bool[5];
                _readTimer.Stop();

                DateTime startTime = DateTime.Now;

                // 🌟 ให้ทดสอบและอ่านค่าเก็บสถิติ เป็นเวลา 10 วินาที
                while ((DateTime.Now - startTime).TotalSeconds < 10.0)
                {
                    await ForceReadAdcAsync();

                    for (int relay = 0; relay < activeTiers; relay++)
                    {
                        ushort currentAdc = _latestRegs[relay];
                        if (currentAdc > 1000)
                        {
                            if (!isHigh[relay])
                            {
                                totalBlinkCount[relay]++;
                                isHigh[relay] = true;
                            }
                        }
                        else if (currentAdc < 500)
                        {
                            isHigh[relay] = false;
                        }
                    }
                    await Task.Delay(100);
                    Application.DoEvents(); // ป้องกัน UI ค้างระหว่างรอ 10 วินาที
                }

                double finalTotalSeconds = (DateTime.Now - startTime).TotalSeconds;
                _readTimer.Start();
                blinkMsg = "=== Blink Result ===\n";

                for (ushort relay = 0; relay < activeTiers; relay++)
                {
                    int cID = ColorToRelay.FirstOrDefault(x => x.Value == relay).Key;
                    string cName = GetColorName(cID);

                    // คำนวณความถี่ที่ได้จากการสะสมค่า 10 วินาที
                    double hz = totalBlinkCount[relay] / finalTotalSeconds;

                    // 🌟 เช็คเงื่อนไข: ความถี่ต้องอยู่ระหว่าง 0.75 - 0.95 Hz
                    bool pass = hz >= 0.50 && hz <= 1.50;

                    if (pass)
                    {
                        if (!IsDebugMode) AddLog($"สี {cName} ผ่าน Blink Test ({hz:F2} Hz)", Color.ForestGreen);
                        else AddLog($"Blink Relay {relay + 1}: OK ✔ ({hz:F2} Hz)", Color.ForestGreen);
                        blinkMsg += $"Relay {relay + 1} ({cName}): ✔ OK ({hz:F2} Hz)\n";
                    }
                    else
                    {
                        statLabels[relay].Text = "BLINK FAIL";
                        statLabels[relay].ForeColor = Color.Red;
                        allBlinkPass = false;
                        string thaiColor = cName switch { "Red" => "แดง", "Yellow" => "เหลือง", "Green" => "เขียว", "Blue" => "น้ำเงิน", "White" => "ขาว", _ => cName };
                        blinkErrors.Add($"- สี{thaiColor}ไม่กระพริบ");
                        AddLog($"สี {cName} ไม่ผ่าน Blink Test! ได้ {hz:F2} Hz", Color.Red);
                        blinkMsg += $"Relay {relay + 1} ({cName}): ✘ Fail ({hz:F2} Hz)\n";
                    }
                }
                if (!allBlinkPass && IsDebugMode) AddLog($"❌ Error detected during Blink Test! (Continuing...)", Color.Red);
            }
            else
            {
                blinkMsg += "Blink Test: None (Skipped)\n";
            }
            await AllOff();

            // ==========================================
            // PART 3: Sound Test (Buzzer)
            // ==========================================
            ushort capturedSound = 0;

            if (IsBuzzerEnabled)
            {
                if (IsDebugMode) AddLog("Blink Complete -> Starting Sound Test...", Color.Cyan);
                Test1_btn.Text = "Sound Testing...";

                await AllOff();
                await WriteSingleRegAsync(0x02, 0, 1);
                await Task.Delay(200);

                for (int w = 0; w < 5; w++)
                {
                    await Task.Delay(200);
                    Application.DoEvents();
                    ushort currentSound = _latestRegs[6];

                    if (currentSound < 500 || currentSound > 2500)
                    {
                        soundPass = true;
                        capturedSound = currentSound;
                        break;
                    }
                }

                await WriteSingleRegAsync(0x02, 0, 0);
                await AllOff();

                if (soundPass)
                {
                    if (!IsDebugMode) AddLog("ผ่านในขั้นตอน Sound Test", Color.ForestGreen);
                    blinkMsg += $"\nSound Sensor: ✔ OK\n";
                }
                else
                {
                    AddLog($"ไม่ผ่านในขั้นตอน Sound Test! (Value was {capturedSound})", Color.Red);
                    blinkMsg += $"\nSound Sensor: ✘ Fail\n";
                }
            }
            else
            {
                soundPass = true;
                blinkMsg += $"\nSound Sensor: None (Skipped)\n";
            }

            // ==========================================
            // Evaluation (สรุปรวมทั้งหมด)
            // ==========================================
            await AllOff();
            await WriteSingleRegAsync(0x02, 0, 0);
            await Task.Delay(300);

            // 🌟 เปลี่ยนการประเมิน Blink ว่าผ่านหรือไม่ โดยอิงจากสถานะของ allBlinkPass 
            bool finalBlinkPass = IsFlashEnabled ? allBlinkPass : true;

            _lastTestPass = allPass && finalBlinkPass && soundPass;

            if (_lastTestPass)
            {
                _goodCount++;
                AddLog($"Tower Light serial {sn_txt.Text} นี้ผ่านเรียบร้อย", Color.LimeGreen);
                ShowCustomResultBox(true, "การทดสอบสำเร็จผ่านทุกขั้นตอน!");
            }
            else
            {
                _defectCount++;
                List<string> errorReasons = new List<string>();
                List<string> displayErrors = new List<string>();

                if (!allPass) { errorReasons.Add("Static Fail"); displayErrors.AddRange(staticErrors); }
                if (IsFlashEnabled && !finalBlinkPass) { displayErrors.AddRange(blinkErrors); }
                if (IsBuzzerEnabled && !soundPass) { displayErrors.Add("- เสียงเตือนบัซเซอร์ไม่ผ่าน"); }

                AddLog($"❌ Tower Light serial {sn_txt.Text} ไม่ผ่านการทดสอบ", Color.Red);
                ShowCustomResultBox(false, "สรุปผลการทดสอบ: พบปัญหาดังนี้\n\n" + string.Join("\n", displayErrors));
            }
            // 🌟 คำนวณ %Yield (ป้องกันการหารด้วยศูนย์)
            if (_testCount > 0)
            {
                _yieldPercent = ((double)_goodCount / _testCount) * 100.0;
            }

            // 🌟 อัปเดตค่าขึ้นแสดงบน Label หน้า UI
            lblQty.Text = _testCount.ToString();
            lblGood.Text = _goodCount.ToString();
            lblDefect.Text = _defectCount.ToString();
            lblYield.Text = _yieldPercent.ToString("F2");

            await SaveResultToExcelAsync(allPass, finalBlinkPass, soundPass, IsFlashEnabled, IsBuzzerEnabled);

            _readTimer.Stop();
            _isReading = false;
            Read_btn.Text = "Read";
            _readTimer.Interval = 500;
            Test1_btn.Enabled = true;
            Test1_btn.Text = "Test";
        }

        private async void rstja_btn_Click(object sender, EventArgs e)
        {
            if (!_serialPort.IsOpen) return;

            rstja_btn.Enabled = false;
            AddLog("=== RESETTING SYSTEM ===", Color.Yellow);

            StopReading();
            await Task.Delay(100);

            // ⭐ [ส่วนที่แก้ไข] เอาคำสั่งที่ยุ่งกับ REG_MODE ออก 
            // และเปลี่ยนเป็นสั่งเคลียร์โหมดกระพริบของทั้ง 24V และ 220V ที่ Slave 2 แทน
            await WriteSingleRegAsync(0x02, 3, 0);
            await WriteSingleRegAsync(0x02, 4, 0);

            // ปิดไฟแต่ละชั้น
            for (ushort r = 0; r < 5; r++)
            {
                await WriteSingleRegAsync(SLAVE_LAMP, r, 0);
            }

            Label[] colorLabels = { Color1_val, Color2_val, Color3_val, Color4_val, Color5_val };
            Label[] stdLabels = { std1_val, std2_val, std3_val, std4_val, std5_val };
            Label[] adcLabels = { adc1_val, adc2_val, adc3_val, adc4_val, adc5_val };
            Label[] statLabels = { stats1_val, stats2_val, stats3_val, stats4_val, stats5_val };
            Panel[] colorPanels = { dis1, dis2, dis3, dis4, dis5 };

            for (int i = 0; i < 5; i++)
            {
                if (colorLabels[i] != null) { colorLabels[i].Text = "-"; colorLabels[i].ForeColor = Color.Black; }
                if (stdLabels[i] != null) stdLabels[i].Text = "-";
                if (adcLabels[i] != null) adcLabels[i].Text = "-";
                if (statLabels[i] != null) { statLabels[i].Text = "-"; statLabels[i].ForeColor = Color.Black; }
                if (colorPanels[i] != null) colorPanels[i].BackColor = Color.LightGray;
            }

            ColorToRelay.Clear();
            ColorToSensor.Clear();
            StandardLDR.Clear();

            Test1_btn.Enabled = true;
            Test1_btn.Text = "Test 1";
            set_btn.Enabled = true;
            set_btn.Text = "Set";

            AddLog("System Reset Complete.", Color.ForestGreen);
            rstja_btn.Enabled = true;
        }

        private void AddLog(string message, Color color)
        {
            if (log_richtxt.InvokeRequired) { log_richtxt.BeginInvoke(new Action(() => AddLog(message, color))); return; }
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            log_richtxt.SelectionStart = log_richtxt.TextLength;
            log_richtxt.SelectionLength = 0;
            log_richtxt.SelectionColor = color;
            log_richtxt.AppendText($"[{timestamp}] {message}\n");
            log_richtxt.SelectionColor = log_richtxt.ForeColor;
            log_richtxt.ScrollToCaret();
        }

        private void clearlog_btn_Click(object sender, EventArgs e) { log_richtxt.Clear(); }

        private void export_btn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_csvPath) || !File.Exists(_csvPath)) { MessageBox.Show("ยังไม่มีข้อมูล กรุณาทดสอบก่อน!", "Warning"); return; }
            System.Diagnostics.Process.Start("explorer.exe", _csvPath);
        }

        private async Task SaveResultToExcelAsync(bool staticPass, bool blinkPass, bool soundPass, bool testFlash, bool testBuzzer)
        {
            // =========================================================
            // 1. กำหนดไฟล์ Template อัตโนมัติ (หาจากโฟลเดอร์ที่รันโปรแกรม)
            // =========================================================
            if (string.IsNullOrEmpty(_templatePath) || !File.Exists(_templatePath))
            {
                _templatePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Template.xlsx");

                if (!File.Exists(_templatePath))
                {
                    MessageBox.Show($"ไม่พบไฟล์ Template.xlsx ในโฟลเดอร์โปรแกรม!\nกรุณานำไฟล์มาวางไว้ที่:\n{_templatePath}", "Template Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // =========================================================
            // 2. เลือกที่เก็บบันทึกผลการทดสอบ และ ทำการ Copy ไฟล์
            // =========================================================
            if (string.IsNullOrEmpty(_excelSavePath))
            {
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    string ext = Path.GetExtension(_templatePath);
                    dlg.Filter = $"Excel file (*{ext})|*{ext}";
                    dlg.FileName = $"TestLog_{DateTime.Now:yyyyMMdd}{ext}";
                    dlg.AutoUpgradeEnabled = false;
                    dlg.RestoreDirectory = true;

                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    _excelSavePath = dlg.FileName;
                }
            }

            if (!File.Exists(_excelSavePath))
            {
                try
                {
                    File.Copy(_templatePath, _excelSavePath);
                    await Task.Delay(400);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ไม่สามารถสร้างไฟล์ Excel ได้: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // =========================================================
            // 3. ระบบเขียนข้อมูลลง Excel (พร้อมระบบ Pagination บรรทัด 12-81)
            // =========================================================
            int maxRetries = 3;

            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    using (var workbook = new XLWorkbook(_excelSavePath))
                    {
                        // 🌟 ตั้งค่าบรรทัดเริ่มต้นและบรรทัดสูงสุดของตารางตามรูปภาพ
                        int startRow = 12;
                        int maxRow = 81;

                        // ดึง Sheet ล่าสุดมาทำงาน
                        var worksheet = workbook.Worksheet(workbook.Worksheets.Count);

                        // หาบรรทัดว่างสำหรับใส่ข้อมูลผลทดสอบในหน้าปัจจุบัน
                        int row = startRow;
                        while (row <= maxRow && (!worksheet.Cell(row, 20).IsEmpty() || !worksheet.Cell(row, 2).IsEmpty()))
                        {
                            row++;
                        }

                        // 🌟 หากตารางในหน้าปัจจุบันเต็ม (เกินบรรทัดที่ 81) ให้สร้างหน้าใหม่
                        if (row > maxRow)
                        {
                            var templateSheet = workbook.Worksheet(1); // ใช้หน้า 1 เป็นต้นแบบในการ Copy
                            string newSheetName = $"Page_{workbook.Worksheets.Count + 1}";
                            worksheet = templateSheet.CopyTo(newSheetName);

                            // เคลียร์เฉพาะข้อมูลในตารางของหน้าใหม่ให้ว่างเปล่า (บรรทัด 12 ถึง 81, คอลัมน์ A ถึง T)
                            for (int r = startRow; r <= maxRow; r++)
                            {
                                worksheet.Range(r, 1, r, 20).Clear(XLClearOptions.Contents);
                            }
                            row = startRow; // เซ็ตให้เริ่มเขียนบรรทัด 12 ของหน้าใหม่
                        }

                        // 🌟 อัปเดตเลขหน้าในช่อง S1 สำหรับทุกๆ Sheet
                        int totalPages = workbook.Worksheets.Count;
                        for (int i = 1; i <= totalPages; i++)
                        {
                            workbook.Worksheet(i).Cell("S1").Value = $"Page {i} / {totalPages}";
                        }

                        // --- เขียนข้อมูล Header ลงใน Sheet ปัจจุบัน ---
                        worksheet.Cell("A3").Value = $"Product : {txtProduct.Text}";
                        worksheet.Cell("E3").Value = $"Model : {txtModel.Text}";
                        worksheet.Cell("I3").Value = $"Lot : {txtLot.Text}";
                        worksheet.Cell("K3").Value = $"Lot Size : {txtLotSize.Text} pcs.";
                        worksheet.Cell("O3").Value = txtDate.Text;

                        // --- เขียนข้อมูล Footer (พิกัดตามรูปภาพ บรรทัดที่ 84) ---
                        worksheet.Cell("L84").Value = txtCheckBy.Text;
                        worksheet.Cell("R84").Value = txtDocRef.Text;
                        worksheet.Cell("G84").Value = _testCount;
                        worksheet.Cell("H84").Value = _goodCount;
                        worksheet.Cell("I84").Value = _defectCount;
                        worksheet.Cell("J84").Value = _yieldPercent;

                        // --- เขียนข้อมูล Criteria ---
                        worksheet.Cell("A6").Value = chk3Colors.Checked ? $"☑ {chk3Colors.Text}" : $"☐ {chk3Colors.Text}";
                        worksheet.Cell("A7").Value = chkSwapColors.Checked ? $"☑ {chkSwapColors.Text}" : $"☐ {chkSwapColors.Text}";
                        worksheet.Cell("A8").Value = chkContinuousBlink.Checked ? $"☑ {chkContinuousBlink.Text}" : $"☐ {chkContinuousBlink.Text}";
                        worksheet.Cell("H5").Value = chkShockTest.Checked ? $"☑ {chkShockTest.Text}" : $"☐ {chkShockTest.Text}";
                        worksheet.Cell("H6").Value = chkBuzzer1.Checked ? $"☑ {chkBuzzer1.Text}" : $"☐ {chkBuzzer1.Text}";
                        worksheet.Cell("H7").Value = chkLED360.Checked ? $"☑ {chkLED360.Text}" : $"☐ {chkLED360.Text}";
                        worksheet.Cell("H8").Value = chkSupply.Checked ? $"☑ {chkSupply.Text}" : $"☐ {chkSupply.Text}";
                        worksheet.Cell("O5").Value = chkCE.Checked ? $"☑ {chkCE.Text}" : $"☐ {chkCE.Text}";
                        worksheet.Cell("O6").Value = chkCurrent.Checked ? $"☑ {chkCurrent.Text}" : $"☐ {chkCurrent.Text}";
                        worksheet.Cell("O7").Value = chkECN.Checked ? $"☑ {chkECN.Text}" : $"☐ {chkECN.Text}";


                        // --- เริ่มกรอกข้อมูล Test Result ในบรรทัดที่หาได้ ---
                        worksheet.Cell(row, 1).Value = _testCount; // ใส่ลำดับ No. จริงที่รันมา

                        Action<int, int, int> FillColorCell = (colorIndex, colOk, colBad) =>
                        {
                            if (_staticResult[colorIndex] == "PASS")
                            {
                                worksheet.Cell(row, colOk).Value = "ok";
                                worksheet.Cell(row, colBad).Value = "";
                            }
                            else if (_staticResult[colorIndex] == "FAIL")
                            {
                                worksheet.Cell(row, colOk).Value = "";
                                worksheet.Cell(row, colBad).Value = "bad";
                            }
                            else
                            {
                                worksheet.Cell(row, colOk).Value = "None";
                                worksheet.Cell(row, colBad).Value = "None";
                            }
                        };

                        FillColorCell(0, 2, 3);   // เขียว
                        FillColorCell(1, 4, 5);   // เหลือง
                        FillColorCell(2, 6, 7);   // แดง
                        FillColorCell(4, 8, 9);   // ขาว
                        FillColorCell(3, 10, 11); // น้ำเงิน

                        if (staticPass)
                        {
                            worksheet.Cell(row, 12).Value = "ok";
                            worksheet.Cell(row, 13).Value = "";
                        }
                        else
                        {
                            worksheet.Cell(row, 12).Value = "";
                            worksheet.Cell(row, 13).Value = "bad";
                        }

                        worksheet.Cell(row, 14).Value = testBuzzer ? (soundPass ? "ok" : "bad") : "None";
                        worksheet.Cell(row, 15).Value = testFlash ? (blinkPass ? "ok" : "bad") : "None";
                        worksheet.Cell(row, 16).Value = staticPass ? "ok" : "bad";

                        if (_lastTestPass)
                        {
                            worksheet.Cell(row, 18).Value = "ok";
                            worksheet.Cell(row, 19).Value = "";
                        }
                        else
                        {
                            worksheet.Cell(row, 18).Value = "";
                            worksheet.Cell(row, 19).Value = "bad";
                        }

                        worksheet.Cell(row, 20).Value = sn_txt.Text;

                        var rowRange = worksheet.Range(row, 1, row, 20);
                        rowRange.Style.Font.FontName = "Cordia New";

                        // บันทึกไฟล์
                        workbook.Save();
                    }

                    break;
                }
                catch (IOException ex)
                {
                    if (retry == maxRetries - 1)
                    {
                        MessageBox.Show($"บันทึกไม่สำเร็จ เนื่องจากไฟล์กำลังถูกใช้งาน!\n(พยายามลองเซฟ {maxRetries} ครั้งแล้ว)\n\nกรุณาตรวจสอบว่าไม่ได้เปิดไฟล์ Excel ค้างไว้\n\nError: {ex.Message}", "File Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"เกิดข้อผิดพลาดในการบันทึก Excel: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }
            }
        }

        private void panel9_Paint(object sender, PaintEventArgs e)
        {

        }

        private bool IsFlashEnabled
        {
            get { return this.InvokeRequired ? (bool)this.Invoke(new Func<bool>(() => chkFlash.Checked)) : chkFlash.Checked; }
        }

        private bool IsBuzzerEnabled
        {
            get { return this.InvokeRequired ? (bool)this.Invoke(new Func<bool>(() => chkBuzzer.Checked)) : chkBuzzer.Checked; }
        }
        private int GetActiveTiers()
        {
            return _selectedTiers;
        }

        // 2. ฟังก์ชันสำหรับซ่อน/แสดง UI ตามจำนวนชั้น
        private void UpdateUIVisibility()
        {
            int tiers = GetActiveTiers();

            // จัดกลุ่ม UI เพื่อให้ง่ายต่อการวนลูปซ่อน
            Panel[] colorPanels = { dis1, dis2, dis3, dis4, dis5 };
            ComboBox[] colorCombos = { comboColor1, comboColor2, comboColor3, comboColor4, comboColor5 };

            // ป้ายกำกับคำว่า ADC:, Status:, Std:
            Label[] adclb = { adclb1, adclb2, adclb3, adclb4, adclb5 };
            Label[] statuslb = { statuslb1, statuslb2, statuslb3, statuslb4, statuslb5 };
            Label[] stdlb = { stdlb1, stdlb2, stdlb3, stdlb4, stdlb5 };

            // ค่า Value 
            Label[] adcVals = { adc1_val, adc2_val, adc3_val, adc4_val, adc5_val };
            Label[] statVals = { stats1_val, stats2_val, stats3_val, stats4_val, stats5_val };
            Label[] stdVals = { std1_val, std2_val, std3_val, std4_val, std5_val };

            for (int i = 0; i < 5; i++)
            {
                bool isVisible = i < tiers; // ถ้า Index น้อยกว่าจำนวนชั้น ให้แสดง

                if (colorPanels[i] != null) colorPanels[i].Visible = isVisible;
                if (colorCombos[i] != null) colorCombos[i].Visible = isVisible;
                if (adclb[i] != null) adclb[i].Visible = isVisible;
                if (statuslb[i] != null) statuslb[i].Visible = isVisible;
                if (stdlb[i] != null) stdlb[i].Visible = isVisible;
                if (adcVals[i] != null) adcVals[i].Visible = isVisible;
                if (statVals[i] != null) statVals[i].Visible = isVisible;
                if (stdVals[i] != null) stdVals[i].Visible = isVisible;
            }
        }

        private async void onbtn_Click(object sender, EventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                MessageBox.Show("กรุณาเปิดการเชื่อมต่อ (Open Port) ก่อนสั่งงาน", "Warning");
                return;
            }

            onbtn.Enabled = false;

            try
            {
                if (onbtn.Text == "TURN OFF")
                {
                    // สถานะปัจจุบัน: สั่งปิดไฟ
                    await AllOff();

                    onbtn.Text = "TURN ON";
                    onbtn.BackColor = Color.GreenYellow;
                    onbtn.ForeColor = Color.Black;
                }
                else if (onbtn.Text == "TURN ON")
                {
                    // ⭐ [ส่วนที่แก้] ถ้าไฟกระพริบทำงานอยู่ ให้ยกเลิกก่อน
                    if (_isBlinking)
                    {
                        _isBlinking = false;
                        blinkToggle_btn.Text = "START BLINK";
                        blinkToggle_btn.BackColor = Color.GreenYellow; // ใส่สีตั้งต้นของปุ่ม Blink
                        blinkToggle_btn.ForeColor = Color.Black;

                        // เลือกว่าจะเคลียร์กระพริบที่ Address ไหน (4 หรือ 5) บน Slave 2
                        ushort blinkAddress = _is220VAC ? (ushort)3 : (ushort)4;
                        await WriteSingleRegAsync(0x02, blinkAddress, 0); // 0 = เคลียร์โหมดกระพริบกลับเป็นติดค้าง
                    }

                    // สถานะปัจจุบัน: สั่งเปิดไฟค้าง
                    int activeTiers = GetActiveTiers();
                    for (ushort r = 0; r < activeTiers; r++)
                    {
                        await WriteSingleRegAsync(SLAVE_LAMP, r, 1);
                    }

                    onbtn.Text = "TURN OFF";
                    onbtn.BackColor = Color.Red;
                    onbtn.ForeColor = Color.White;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการส่งคำสั่ง: {ex.Message}", "Error");
            }
            finally
            {
                onbtn.Enabled = true;
            }
        }
        private void SaveOriginalBounds(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                _originalControlBounds[c] = new Rectangle(c.Location, c.Size);
                _originalControlFonts[c] = c.Font.Size;

                // ถ้า Control นี้มี Control ย่อยข้างใน (เช่น Panel หรือ GroupBox) ให้เข้าไปจดจำด้วย
                if (c.HasChildren)
                {
                    SaveOriginalBounds(c);
                }
            }
        }

        // 2. ฟังก์ชันคำนวณสัดส่วนและจับขยาย
        private void ResizeAllControls(Control parent, float ratioX, float ratioY)
        {
            foreach (Control c in parent.Controls)
            {
                if (_originalControlBounds.ContainsKey(c))
                {
                    Rectangle orig = _originalControlBounds[c];

                    // ขยายตำแหน่งและขนาด
                    c.Location = new Point((int)(orig.X * ratioX), (int)(orig.Y * ratioY));
                    c.Size = new Size((int)(orig.Width * ratioX), (int)(orig.Height * ratioY));

                    // ขยายขนาด Font (ใช้อัตราส่วนที่น้อยกว่าระหว่างแกน X และ Y ป้องกันตัวหนังสือล้น)
                    float ratioFont = Math.Min(ratioX, ratioY);
                    c.Font = new Font(c.Font.FontFamily, _originalControlFonts[c] * ratioFont, c.Font.Style);
                }

                if (c.HasChildren)
                {
                    ResizeAllControls(c, ratioX, ratioY);
                }
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // เปิดโหมดลดการกระพริบของหน้าจอเวลาขยาย
            this.DoubleBuffered = true;

            // จดจำขนาดของหน้าต่างเริ่มต้น
            _originalFormSize = new Rectangle(this.Location, this.Size);

            // สั่งให้จดจำขนาดของทุกอย่างในหน้าจอ
            SaveOriginalBounds(this);
            SetTierSelection(3);
            AttachCriteriaEditEvents();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            // ป้องกัน Error ตอนฟอร์มยังโหลดไม่เสร็จ
            if (_originalFormSize.Width == 0 || _originalFormSize.Height == 0) return;

            // คำนวณหาอัตราส่วนว่าหน้าจอใหญ่ขึ้นกี่เท่า
            float ratioX = (float)this.Width / _originalFormSize.Width;
            float ratioY = (float)this.Height / _originalFormSize.Height;

            // สั่งขยายทุกอย่าง
            ResizeAllControls(this, ratioX, ratioY);

            // --- ส่วนที่เพิ่มเข้ามาเพื่อแก้ไฮไลต์สีฟ้า ---
            this.ActiveControl = null; // ย้าย Focus ออกจากคอนโทรลที่กำลังถูกเลือก
            ClearHighlight(this);      // สั่งยกเลิกการคลุมดำทั้งหมด
        }

        // ฟังก์ชันสำหรับวนลูปยกเลิกการคลุมดำใน ComboBox และ TextBox
        private void ClearHighlight(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is ComboBox cb) cb.SelectionLength = 0;
                if (c is TextBox tb) tb.SelectionLength = 0;

                if (c.HasChildren)
                {
                    ClearHighlight(c);
                }
            }
        }

        private async void blinkToggle_btn_Click(object sender, EventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                MessageBox.Show("กรุณาเปิดการเชื่อมต่อ (Open Port) ก่อนสั่งงาน", "Warning");
                return;
            }

            blinkToggle_btn.Enabled = false;

            try
            {
                if (!_isBlinking)
                {
                    if (onbtn.Text == "TURN OFF")
                    {
                        onbtn.Text = "TURN ON";
                        onbtn.BackColor = Color.GreenYellow;
                        onbtn.ForeColor = Color.Black;
                    }

                    // สั่งเปิดไฟแต่ละชั้น
                    int activeTiers = GetActiveTiers();
                    for (ushort r = 0; r < activeTiers; r++)
                    {
                        await WriteSingleRegAsync(SLAVE_LAMP, r, 1);
                    }

                    // ⭐ [ส่วนที่แก้] สั่งเปิดไฟกระพริบตามโหมด 24V หรือ 220V
                    ushort blinkAddress = _is220VAC ? (ushort)3 : (ushort)4;
                    await WriteSingleRegAsync(0x02, blinkAddress, 1); // 1 = สั่งกระพริบ

                    _isBlinking = true;
                    blinkToggle_btn.Text = "STOP BLINK";
                    blinkToggle_btn.BackColor = Color.Red;
                    blinkToggle_btn.ForeColor = Color.White;

                    _ = MonitorBlinkFrequencyAsync();
                }
                else
                {
                    // 1. สั่งเปลี่ยนสถานะตัวแปรเป็น false ก่อน
                    _isBlinking = false;

                    // 2. หน่วงเวลาเคลียร์พอร์ต
                    await Task.Delay(300);

                    // 3. ⭐ [ส่วนที่แก้] ส่งคำสั่งปิดไฟกระพริบตามโหมดที่เลือก
                    ushort blinkAddress = _is220VAC ? (ushort)3 : (ushort)4;
                    await WriteSingleRegAsync(0x02, blinkAddress, 0); // 0 = เลิกกระพริบ

                    await AllOff();

                    blinkToggle_btn.Text = "START BLINK";
                    blinkToggle_btn.BackColor = Color.GreenYellow;
                    blinkToggle_btn.ForeColor = Color.Black;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการส่งคำสั่ง: {ex.Message}", "Error");
            }
            finally
            {
                blinkToggle_btn.Enabled = true;
            }
        }
        private void SetTierSelection(int tier)
        {
            _selectedTiers = tier;

            // 1. รีเซ็ตทุกปุ่มให้เป็นสีเทา (สถานะไม่ได้เลือก)
            btnTier1.BackColor = Color.LightGray;
            btnTier2.BackColor = Color.LightGray;
            btnTier3.BackColor = Color.LightGray;
            btnTier4.BackColor = Color.LightGray;
            btnTier5.BackColor = Color.LightGray;

            // 2. ไฮไลท์เฉพาะปุ่มที่ถูกเลือก
            switch (tier)
            {
                case 1: btnTier1.BackColor = Color.LimeGreen; break;
                case 2: btnTier2.BackColor = Color.LimeGreen; break;
                case 3: btnTier3.BackColor = Color.LimeGreen; break;
                case 4: btnTier4.BackColor = Color.LimeGreen; break;
                case 5: btnTier5.BackColor = Color.LimeGreen; break;
            }

            // ⭐ 3. เพิ่มบรรทัดนี้ เพื่อให้หน้าจอซ่อน/แสดงชั้น ตามปุ่มที่กด
            UpdateUIVisibility();
        }

        private void btnTier1_Click(object sender, EventArgs e) { SetTierSelection(1); }
        private void btnTier2_Click(object sender, EventArgs e) { SetTierSelection(2); }
        private void btnTier3_Click(object sender, EventArgs e) { SetTierSelection(3); }
        private void btnTier4_Click(object sender, EventArgs e) { SetTierSelection(4); }
        private void btnTier5_Click(object sender, EventArgs e) { SetTierSelection(5); }

        private async Task MonitorBlinkFrequencyAsync()
        {
            // พักการอ่านค่าจาก Timer ปกติ
            bool wasTimerRunning = _readTimer.Enabled;
            _readTimer.Stop();

            int activeTiers = GetActiveTiers();
            bool[] isHigh = new bool[5];

            // ตัวแปรใหม่: เก็บนับรวมตั้งแต่ต้น ไม่รีเซ็ตกลางทาง
            int[] totalBlinkCount = new int[5];

            // จดจำเวลาเริ่มต้นที่กดปุ่ม START
            DateTime startTime = DateTime.Now;
            DateTime lastReportTime = DateTime.Now;

            while (_isBlinking)
            {
                bool success = await ForceReadAdcAsync();

                if (success)
                {
                    for (int i = 0; i < activeTiers; i++)
                    {
                        ushort adc = _latestRegs[i];

                        if (adc > 1000 && !isHigh[i])
                        {
                            isHigh[i] = true;
                            totalBlinkCount[i]++; // บวกสะสมไปเรื่อยๆ
                        }
                        else if (adc < 500)
                        {
                            isHigh[i] = false;
                        }
                    }
                }

                // เช็คว่าผ่านไป 3 วินาทีหรือยัง เพื่อพิมพ์รายงานลง Log
                double secondsSinceLastReport = (DateTime.Now - lastReportTime).TotalSeconds;
                if (secondsSinceLastReport >= 3.0)
                {
                    // คำนวณเวลาที่ผ่านไปทั้งหมด "ตั้งแต่เริ่มเทส"
                    double totalSeconds = (DateTime.Now - startTime).TotalSeconds;

                    // คำนวณหาค่าเฉลี่ย Hz รวมจากทุกชั้น
                    double sumHz = 0;
                    for (int i = 0; i < activeTiers; i++)
                    {
                        sumHz += (totalBlinkCount[i] / totalSeconds);
                    }
                    double averageHz = sumHz / activeTiers;

                    // แปลงความถี่ (Hz) เป็นเวลา (วินาทีต่อ 1 รอบกระพริบ)
                    double averagePeriod = averageHz > 0 ? (1.0 / averageHz) : 0;

                    // แสดงผลรูปแบบที่ต้องการ
                    AddLog($"Frequency : {averageHz:F2} Hz | Time: {averagePeriod:F2} s", Color.Black);

                    lastReportTime = DateTime.Now;
                }

                await Task.Delay(100);
            }

            // เมื่อหยุดกระพริบ ให้เปิด Timer ปกติกลับมาทำงาน
            if (wasTimerRunning) _readTimer.Start();
        }

        private async void btnMode24V_Click(object sender, EventArgs e)
        {
            int activeTiers = GetActiveTiers();
            if (!_serialPort.IsOpen) { MessageBox.Show("Port is not open!", "Warning"); return; }
            // ⭐ [เพิ่มส่วนนี้] ถ้ามีระบบใดทำงานอยู่ ให้แจ้งเตือนและเด้งออกทันที
            if (IsSystemBusy())
            {
                MessageBox.Show("กรุณาหยุดการทำงาน (Set, Test, Turn On หรือ Blink) ก่อนเปลี่ยนโหมด", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _is220VAC = false;
            btnMode24V.BackColor = Color.LimeGreen;
            btnMode220V.BackColor = Color.LightGray;

            // สั่งฮาร์ดแวร์ผ่าน Modbus: Slave 1, Address 6 ให้เป็น 0 (เลือก 24VDC)
            await WriteSingleRegAsync(0x01, 5, 0);
            AddLog("System Mode: 24VDC Selected", Color.Black);
        }

        private async void btnMode220V_Click(object sender, EventArgs e)
        {
            int activeTiers = GetActiveTiers();
            if (!_serialPort.IsOpen) { MessageBox.Show("Port is not open!", "Warning"); return; }
            // ⭐ [เพิ่มส่วนนี้] ถ้ามีระบบใดทำงานอยู่ ให้แจ้งเตือนและเด้งออกทันที
            if (IsSystemBusy())
            {
                MessageBox.Show("กรุณาหยุดการทำงาน (Set, Test, Turn On หรือ Blink) ก่อนเปลี่ยนโหมด", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _is220VAC = true;
            btnMode220V.BackColor = Color.LimeGreen;
            btnMode24V.BackColor = Color.LightGray;

            // สั่งฮาร์ดแวร์ผ่าน Modbus: Slave 1, Address 6 ให้เป็น 1 (เลือก 220VAC)
            await WriteSingleRegAsync(0x01, 5, 1);
            AddLog("System Mode: 220VAC Selected", Color.Black);
        }
        private bool IsSystemBusy()
        {
            // เช็คว่าระบบกำลัง Test (ปุ่มถูกปิด), Set (ปุ่มถูกปิด), เปิดไฟค้าง (ปุ่มขึ้นคำว่า TURN OFF) หรือกระพริบอยู่ หรือไม่
            if (!Test1_btn.Enabled || !set_btn.Enabled || onbtn.Text == "TURN OFF" || _isBlinking)
            {
                return true;
            }
            return false;
        }
        // ฟังก์ชันกลางสำหรับเพิ่ม/ลด Serial Number
        private void IncrementSerialNumber(int step)
        {
            string currentText = sn_txt.Text.Trim();
            if (string.IsNullOrEmpty(currentText)) return;

            // จดจำความยาวของข้อความเดิมไว้ เพื่อรักษาเลข 0 ด้านหน้า (เช่น 0001 -> 0002)
            int length = currentText.Length;

            // พยายามแปลงข้อความเป็นตัวเลข (ใช้ long รองรับเลขยาวๆ)
            if (long.TryParse(currentText, out long currentNumber))
            {
                long newNumber = currentNumber + step;

                // ป้องกันไม่ให้ Serial Number ติดลบ
                if (newNumber < 0) newNumber = 0;

                // แปลงกลับเป็น String โดยคงจำนวนหลักเท่าเดิม
                sn_txt.Text = newNumber.ToString("D" + length);
            }
            else
            {
                // กรณีที่มีตัวอักษรผสมอยู่ด้วย จะแจ้งเตือน
                MessageBox.Show("กรุณากรอกตัวเลข Serial Number เพียวๆ ก่อนใช้ปุ่มเพิ่ม/ลด", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Event เมื่อคลิกปุ่มเพิ่ม
        private void btnUpSN_Click(object sender, EventArgs e)
        {
            IncrementSerialNumber(1);
        }

        // Event เมื่อคลิกปุ่มลด
        private void btnDownSN_Click(object sender, EventArgs e)
        {
            IncrementSerialNumber(-1);
        }

        private void btnCriteria_Click(object sender, EventArgs e)
        {
            // สลับสถานะการแสดงผลของ Panel
            panelCriteria.Visible = !panelCriteria.Visible;

            // เปลี่ยนสีปุ่มเล็กน้อยเพื่อให้รู้ว่าเปิดอยู่
            btnCriteria.BackColor = panelCriteria.Visible ? Color.LawnGreen : Color.GreenYellow;
        }
        private void ShowCustomResultBox(bool isPass, string details)
        {
            // 1. สร้างหน้าต่าง (กำหนดแค่ความกว้าง ส่วนความสูงเดี๋ยวเราให้คำนวณอัตโนมัติ)
            Form popup = new Form();
            popup.StartPosition = FormStartPosition.CenterScreen;
            popup.Text = isPass ? "Test Result: PASS" : "Test Result: FAIL";
            popup.FormBorderStyle = FormBorderStyle.FixedDialog;
            popup.MaximizeBox = false;
            popup.MinimizeBox = false;
            popup.BackColor = Color.WhiteSmoke;
            popup.ClientSize = new Size(500, 100); // กำหนดความกว้างเริ่มต้น ความสูงตั้งไว้ชั่วคราว

            // 2. สร้างตัวหนังสือ PASS / FAIL
            Label lblStatus = new Label();
            lblStatus.Text = isPass ? "PASS" : "FAIL";
            lblStatus.Font = new Font("Arial", 56, FontStyle.Bold);
            lblStatus.ForeColor = isPass ? Color.LimeGreen : Color.Red;
            lblStatus.AutoSize = false;
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblStatus.Size = new Size(500, 100);
            lblStatus.Location = new Point(0, 10);

            // 3. สร้างตัวหนังสือรายละเอียด (ตั้งค่าให้ยืดความสูงอัตโนมัติ)
            Label lblDetails = new Label();
            lblDetails.Text = details;
            lblDetails.Font = new Font("Tahoma", 14);
            lblDetails.MaximumSize = new Size(460, 0); // บังคับความกว้างสูงสุดไม่ให้ล้นหน้าจอ (0 = ยืดความสูงได้ไม่อั้น)
            lblDetails.AutoSize = true;
            lblDetails.TextAlign = ContentAlignment.TopCenter;

            // นำ Label ข้อความใส่ลงไปในหน้าต่างก่อน เพื่อให้ระบบคำนวณความกว้าง-สูงที่แท้จริง
            popup.Controls.Add(lblDetails);

            // จัดตำแหน่งข้อความให้อยู่กึ่งกลางหน้าต่าง และอยู่ใต้คำว่า PASS/FAIL
            lblDetails.Location = new Point((popup.ClientSize.Width - lblDetails.Width) / 2, lblStatus.Bottom + 10);

            // 4. สร้างปุ่ม OK และจัดตำแหน่งให้อยู่ "ใต้ข้อความรายละเอียด" อัตโนมัติ
            Button btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.Size = new Size(120, 45);
            btnOk.Font = new Font("Tahoma", 12, FontStyle.Bold);
            // ตำแหน่ง Y ของปุ่ม = ขอบล่างของข้อความ (Bottom) + ระยะห่าง 30px
            btnOk.Location = new Point((popup.ClientSize.Width - btnOk.Width) / 2, lblDetails.Bottom + 30);
            btnOk.DialogResult = DialogResult.OK;

            // 5. ปรับขนาดความสูงของหน้าต่าง popup ตามตำแหน่งขอบล่างสุดของปุ่ม OK
            popup.ClientSize = new Size(popup.ClientSize.Width, btnOk.Bottom + 20);

            // นำองค์ประกอบที่เหลือใส่ลงในหน้าต่าง
            popup.Controls.Add(lblStatus);
            popup.Controls.Add(btnOk);

            // ตั้งค่าให้กดปุ่ม Enter บนคีย์บอร์ดแทนการคลิกเมาส์ได้
            popup.AcceptButton = btnOk;

            // แสดงหน้าต่าง
            popup.ShowDialog();
        }
        // ฟังก์ชันสำหรับเรียกหน้าต่างกรอกข้อความ
        private string PromptEditCriteria(string currentText)
        {
            Form prompt = new Form()
            {
                Width = 450,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "แก้ไขหัวข้อ Criteria",
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label textLabel = new Label() { Left = 20, Top = 15, Text = "กำหนดข้อความ (จำกัดความยาว 65 ตัวอักษร):", AutoSize = true };

            // ตั้งค่า MaxLength = 65 ตัวอักษร เพื่อไม่ให้ข้อความยาวล้นจอ
            TextBox textBox = new TextBox() { Left = 20, Top = 40, Width = 390, Text = currentText, MaxLength = 65 };

            Button confirmation = new Button() { Text = "ตกลง", Left = 310, Width = 100, Top = 70, DialogResult = DialogResult.OK };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : currentText;
        }

        // ฟังก์ชันสำหรับเปิดการแก้ไขเมื่อ "คลิกขวา"
        private void AttachCriteriaEditEvents()
        {
            // รวม CheckBox ทั้ง 10 ตัวที่มีอยู่แล้วในระบบ
            CheckBox[] criteriaBoxes = { chk3Colors, chkSwapColors, chkContinuousBlink, chkShockTest, chkBuzzer1, chkLED360, chkSupply, chkCE, chkCurrent, chkECN };

            foreach (CheckBox chk in criteriaBoxes)
            {
                if (chk != null)
                {
                    // เพิ่ม Tooltip เพื่อบอกผู้ใช้ว่าคลิกขวาแก้ไขได้
                    ToolTip tt = new ToolTip();
                    tt.SetToolTip(chk, "คลิกขวาเพื่อแก้ไขข้อความ");

                    chk.MouseDown += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Right) // ถ้าคลิกขวา
                        {
                            // ใช้คำสั่ง 'is' แทน 'as' เพื่อเช็คและกำหนดค่าในบรรทัดเดียว
                            if (s is CheckBox clickedChk)
                            {
                                string newText = PromptEditCriteria(clickedChk.Text);
                                clickedChk.Text = newText; // อัปเดตข้อความใหม่
                            }
                        }
                    };
                }
            }
        }
    }
}