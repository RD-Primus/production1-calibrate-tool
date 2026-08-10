using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.VariantTypes;
using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace CMATestVer1
{
    public partial class Form1 : Form
    {
        //-- Serail Port
        public SerialPort _serialPort;
        public SerialPort _ohmPort;
        public SerialPort _DisPort;


        private readonly object _lock = new object();
        private List<byte> rxBuffer = new List<byte>();
        private List<byte> ohmRxBuffer = new List<byte>();


        private ushort lastRequestedStart = 0; // ตัวแปรเก็บค่าเริ่มต้นล่าสุด

        // เก็บค่า register ดิบล่าสุด แยกตาม (slaveId, address) ไม่ผ่าน UI
        private readonly Dictionary<(byte slaveId, int addr), short> _lastRegisterValues
            = new Dictionary<(byte, int), short>();
        private readonly object _regLock = new object();

        private bool TryGetLastRegister(byte slaveId, int addr, out short value)
        {
            lock (_regLock)
            {
                return _lastRegisterValues.TryGetValue((slaveId, addr), out value);
            }
        }


        //-- เบื้องหลัง
        public BackgroundWorker backgroundWorkerOhm;


        //-- สำหรับการ Calibration 
        private List<double> _tempAdcBuffer = new List<double>(); // บัฟเฟอร์เก็บค่าระหว่าง 5 วินาที
        public double[] finalAdcArray = new double[14];           // Array 14 ค่าที่ต้องการ
        private int _sequenceStep = 0;                            // ตัวนับลำดับ 0-13


        //-- status ผลการ test
        private volatile bool ID2_reg0IsOn = false;
        private volatile bool ID2_reg1IsOn = false;
        private volatile bool ID3_reg3IsOn = false;
        private volatile bool ID3_reg4IsOn = false;
        private volatile bool ID3_reg5IsOn = false;

        private volatile bool _alarm1IsOn = false;
        private volatile bool _alarm2IsOn = false;
        private volatile bool Led_Relay = false;

        private DateTime _lastID3FrameTime = DateTime.MinValue;
        private readonly object _id3TimeLock = new object();


        //-- Timer
        private System.Diagnostics.Stopwatch _runStopwatch = new System.Diagnostics.Stopwatch();
        private System.Windows.Forms.Timer _snDebounceTimer; // Debounce timer สำหรับ Serial Number input

        //-- API
        private static readonly HttpClient client = new HttpClient();
        private readonly string _token; // ตัวแปรสำหรับจำกุญแจ Token ที่ได้มา


        //-- EXCEL 
        private string? _historyExcelFilePath = string.Empty;
        private string _excelFilePath = string.Empty;


        //-- Show from4
        private int _autoOpenCountdown = 0;
        private Form4? _form4Instance = null;
        private bool _isForm4ResultReady = false; // ความพร้อมของผลลัพธ์ Form4


        //-- สำหรับการเขียน
        private string _originalText = string.Empty;
        private readonly HashSet<string> _dirtyRegis = new HashSet<string>();// รีจิสเตอร์ที่เปลี่ยนแปลง
        private bool _isWriting = false; // ธงเช็คสถานะ: true = กำลังมีคำสั่งเขียนวิ่งอยู่
        private DateTime _isWritingStartTime = DateTime.MinValue; //เวลาที่เริ่มเขียน ใช้กัน _isWriting ค้าง

        private bool _forceDisplay120 = false; // เพื่อควบคุมการส่ง 120
        private string _register0FinalResult = "-";




        public Form1(string token)
        {
            InitializeComponent();
            _token = token;

            tabControl2.SelectedIndex = 0;

            _serialPort = new SerialPort();
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            string[] ports = SerialPort.GetPortNames();
            PortBox.Items.AddRange(ports);

            _ohmPort = new SerialPort();
            SerialboxOhm.Items.AddRange(ports);

            _DisPort = new SerialPort();
            portDis.Items.AddRange(ports);

            backgroundWorkerOhm = new BackgroundWorker();
            backgroundWorkerOhm.WorkerSupportsCancellation = true;
            backgroundWorkerOhm.WorkerReportsProgress = true;

            backgroundWorkerOhm.DoWork += backgroundWorker1_DoWork;
            backgroundWorkerOhm.ProgressChanged += backgroundWorker1_ProgressChanged;
            backgroundWorkerOhm.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;

            InitStepLeds();
            txtSerialNumber.Focus();

            // ใน Constructor เพิ่ม
            _snDebounceTimer = new System.Windows.Forms.Timer();
            _snDebounceTimer.Interval = 500; // รอ 500ms หลังหยุดพิมพ์
            _snDebounceTimer.Tick += async (s, e) =>
            {
                _snDebounceTimer.Stop();
                await CheckSerialNumberAsync();
            };

            DrawLedBulb(picWL, false, Color.Red);
            DrawLedBulb(picHp, false, Color.Red);
            DrawLedBulb(picCom, false, Color.Red);
            DrawLedBulb(picHotFan, false, Color.Red);
            DrawLedBulb(picCoolFan, false, Color.Red);
            //DrawLedBulb(picRelay, false, Color.Red);
        }

        //-- ส่วนของการกดเชื่อมต่อ  
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    if (PortBox.SelectedItem == null || portDis.SelectedItem == null || SerialboxOhm.SelectedItem == null)
                    {
                        MessageBox.Show("กรุณาเลือก Port (Modbus), Baudrate และ Port (Ohm) ให้ครบถ้วน!",
                                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    //  ตั้งค่าและเปิด _serialPort (Modbus)
                    _serialPort.PortName = PortBox.SelectedItem.ToString()!;
                    _serialPort.BaudRate = 19200;
                    //_serialPort.BaudRate = int.Parse(BuadBox.SelectedItem!.ToString()!);
                    _serialPort.Parity = Parity.None;
                    _serialPort.DataBits = 8;
                    _serialPort.StopBits = StopBits.One;
                    _serialPort.ReadTimeout = 500;
                    _serialPort.WriteTimeout = 500;
                    _serialPort.Open();

                    //  ตั้งค่าและเปิด _ohmPort (Ohm Source)
                    _ohmPort.PortName = SerialboxOhm.SelectedItem.ToString()!;
                    _ohmPort.BaudRate = 9600;
                    _ohmPort.Parity = Parity.None;
                    _ohmPort.DataBits = 8;
                    _ohmPort.StopBits = StopBits.One;
                    _ohmPort.Open();

                    _DisPort.PortName = portDis.SelectedItem.ToString()!;
                    _DisPort.BaudRate = 19200;
                    _DisPort.Parity = Parity.None;
                    _DisPort.DataBits = 8;
                    _DisPort.StopBits = StopBits.One;
                    _DisPort.Open();


                    timer1.Interval = 200;
                    timer1.Start();
                    _fastPollCount = 0;

                    btnConnect.Text = "Disconnect";
                    btnConnect.BackColor = Color.LightCoral;

                    PortBox.Enabled = false;
                    portDis.Enabled = false;
                    SerialboxOhm.Enabled = false;

                    //MessageBox.Show("All Ports Connected Successfully!", "Success");

                    ExecuteWriteSingleRegister(2, 0, 0);
                    System.Threading.Thread.Sleep(100);
                    ExecuteWriteSingleRegister(2, 1, 0);
                    System.Threading.Thread.Sleep(100);
                    ExecuteWriteSingleRegister(2, 12, 1);


                }
                else
                {
                    if (backgroundWorkerOhm.IsBusy)
                    {
                        backgroundWorkerOhm.CancelAsync();
                        // รอให้ worker หยุดก่อน (max 3 วินาที)
                        int waited = 0;
                        while (backgroundWorkerOhm.IsBusy && waited < 3000)
                        {
                            Application.DoEvents();
                            Thread.Sleep(100);
                            waited += 100;
                        }
                    }

                    timer1.Stop();
                    HandleDisconnect("UserDisconnect");

                    if (_serialPort.IsOpen) _serialPort.Close();
                    if (_ohmPort.IsOpen) _ohmPort.Close();
                    if (_DisPort.IsOpen) _DisPort.Close();

                    btnConnect.Text = "Connect";
                    btnConnect.BackColor = Color.LightGreen;

                    PortBox.Enabled = true;
                    portDis.Enabled = true;
                    SerialboxOhm.Enabled = true;

                    txtSerialNumber.Focus();

                    //MessageBox.Show("All Ports Disconnected.", "Information");
                }
            }
            catch (Exception ex)
            {
                // หากเกิด Error ให้พยายามปิดพอร์ตที่อาจค้างอยู่
                if (_serialPort.IsOpen) _serialPort.Close();
                if (_ohmPort.IsOpen) _ohmPort.Close();
                if (_DisPort.IsOpen) _DisPort.Close();
                MessageBox.Show($"Error during connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void bntRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                // ── 1. หยุดการทำงานที่กำลังรัน ──────────────────────────────
                if (backgroundWorkerOhm.IsBusy)
                    backgroundWorkerOhm.CancelAsync();

                if (_serialPort != null && _serialPort.IsOpen)
                    HandleDisconnect("UserDisconnect");
                else
                    timer1.Stop();

                // ── 2. Reset runtime state (Cal/Test) ────────────────────────
                _sequenceStep = 0;
                _testStepResumed = 0;
                _calDone = false;
                Array.Clear(finalAdcArray, 0, finalAdcArray.Length);
                lock (_tempAdcBuffer) { _tempAdcBuffer.Clear(); }
                lock (_regLock) { _lastRegisterValues.Clear(); }

                // ── 3. Reset volatile device flags ───────────────────────────
                ID2_reg0IsOn = false;
                ID2_reg1IsOn = false;
                ID3_reg3IsOn = false;
                ID3_reg4IsOn = false;
                ID3_reg5IsOn = false;
                _alarm1IsOn = false;
                _alarm2IsOn = false;
                Led_Relay = false;

                // ── 4. Reset ผลการ Test ──────────────────────────────────────
                _verify200Result = "-";
                _verify2000Result = "-";
                _verify8000Result = "-";
                _wlResult = "-";
                _hpResult = "-";
                _wl_AL2Result = "-";
                _hp_AL2Result = "-";
                _alarm1Result = "-";
                _Compressor = "-";
                _RelayResult = "-";

                // ── 5. Reset UI indicators ───────────────────────────────────
                DrawLedBulb(picWL, false, Color.Red);
                DrawLedBulb(picHp, false, Color.Red);
                DrawLedBulb(picCom, false, Color.Red);
                DrawLedBulb(picHotFan, false, Color.Red);
                DrawLedBulb(picCoolFan, false, Color.Red);
                //DrawLedBulb(picRelay, false, Color.Red);

                ResetAllSteps();

                _runStopwatch.Reset();
                lblElapsedTime.Text = "00:00";
                progressBar1.Value = 0;

                // ── 6. ล้าง Regis TextBox ────────────────────────────────────
                for (int i = 0; i <= 26; i++)
                {
                    Control[] found = this.Controls.Find("Regis" + i, true);
                    if (found.Length > 0)
                    {
                        found[0].Text = "";
                        found[0].ForeColor = Color.Black;
                    }
                }

                // ── 7. Reset UI controls ─────────────────────────────────────
                RxBox.Clear();

                btnConnect.Text = "Connect";
                btnConnect.ResetBackColor();
                btnConnect.UseVisualStyleBackColor = true;
                btnConnect.Enabled = true;

                btnRun.Text = "Run";
                btnRun.Enabled = true;
                btnRun.BackColor = Color.Gold;
                bntStop.Enabled = false;

                PortBox.Enabled = true;
                portDis.Enabled = true;
                SerialboxOhm.Enabled = true;

                chkCal.Checked = false;
                chkTest.Checked = false;

                txtSerialNumber.ReadOnly = false;
                txtSerialNumber.BackColor = SystemColors.Window;

                // ── 8. Reset data source + โหลดข้อมูลใหม่ ───────────────────
                _lastDatabaseData = null;
                cmbDisplaySource.SelectedIndex = -1;
                cmbLot.SelectedIndex = -1;
                cmbLot.Text = "";

                Get_compoart_list();
                LoadLotHistory();
                LoadExcelToDataGrid();

                txtSerialNumber.Focus();
                MessageBox.Show("All settings have been reset!", "Refresh",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Get_compoart_list()
        {
            PortBox.Items.Clear();
            SerialboxOhm.Items.Clear();
            portDis.Items.Clear();

            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM")) //เข้าไปอ่าน Registry ของ Windows path นี้:HARDWARE\DEVICEMAP\SERIALCOMM (เป็นที่เก็บรายชื่อ COM Port ทั้งหมดในเครื่อง)
            {
                if (key != null)
                {
                    foreach (var name in key.GetValueNames()) // วนลูปอ่านชื่อทั้งหมด ดึง “ชื่อของข้อมูล” ใน Registry key => GetValueNames() = เอา “ชื่อไฟล์” ในโฟลเดอร์
                    {
                        PortBox.Items.Add(key.GetValue(name)?.ToString() ?? "");
                        SerialboxOhm.Items.Add(key.GetValue(name)?.ToString() ?? "");
                        portDis.Items.Add(key.GetValue(name)?.ToString() ?? "");
                    }
                }
            }

            PortBox.SelectedIndex = 0;
            SerialboxOhm.SelectedIndex = 0;
            portDis.SelectedIndex = 0;

            PortBox.Text = "";
            SerialboxOhm.Text = "";
            portDis.Text = "";

            if (PortBox.Items.Count == 0)
            {
                MessageBox.Show("ไม่พบ COM Port!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void HandleDisconnect(string reason)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleDisconnect(reason)));
                return;
            }

            timer1.Stop();

            // 1. ถ้าระบบหลักทำงานค้างอยู่ ให้สั่งยกเลิกทันที
            if (backgroundWorker1.IsBusy)
            {
                backgroundWorker1.CancelAsync();
            }

            // 2. ถ้า Form4 เปิดค้างอยู่ให้ปิดตัวลงทันทีเพราะสายหลุดแล้ว
            if (_form4Instance != null && !_form4Instance.IsDisposed)
            {
                try
                {
                    if (_DisPort != null)
                    {
                        _DisPort.DataReceived -= _form4Instance.DataReceivedHandler;
                    }
                    _form4Instance.Close();
                }
                catch { }
                _form4Instance = null;
            }

            // ปิดพอร์ตก่อนเพื่อให้ DataReceivedHandler หยุดทำงาน
            try { if (_serialPort?.IsOpen == true) _serialPort.Close(); } catch { }
            try { if (_ohmPort?.IsOpen == true) _ohmPort.Close(); } catch { }
            try { if (_DisPort?.IsOpen == true) _DisPort.Close(); } catch { }

            lock (_lock)
            {
                rxBuffer.Clear();
            }

            // 3. รีเซ็ตปุ่มกลับมาพร้อมทำงานรอบถัดไป
            btnRun.Text = "Run";
            btnRun.Enabled = true;
            bntStop.Enabled = false;
            btnRun.BackColor = Color.Gold;

            btnConnect.Text = "Open Port";
            btnConnect.BackColor = Color.FromKnownColor(KnownColor.ControlLight);
            PortBox.Enabled = true;
            portDis.Enabled = true;
            SerialboxOhm.Enabled = true;

            if (reason != "UserDisconnect")
            {
                MessageBox.Show($"การเชื่อมต่อขาดหาย: {reason}", "Connection Lost",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _isWriting = false;
        }
        private void btnClr_Click(object sender, EventArgs e)
        {
            RxBox.Clear();
        }


        //-- ส่วนของการส่งข้อมูล
        private double _currentPv;
        private double _currentSV;
        private double _currentALH;
        private double _currentALL;

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            try
            {
                int count = _serialPort.BytesToRead;
                if (count == 0) return;

                byte[] buffer = new byte[count];
                _serialPort.Read(buffer, 0, count);

                lock (_lock) { rxBuffer.AddRange(buffer); }

                ParseModbusRTU();
            }
            catch { /* พอร์ตปิดระหว่างอ่าน — ไม่ต้องทำอะไร */ }
        }
        private void ParseModbusRTU()
        {
            lock (_lock)
            {
                while (rxBuffer.Count >= 5)
                {
                    byte function = rxBuffer[1];
                    int frameLength = 0;

                    if (function == 0x03)

                    {
                        frameLength = rxBuffer[2] + 5;
                    }
                    else if (function == 0x06)
                    {
                        frameLength = 8;
                    }
                    else if (function == 0x55)
                    {
                        frameLength = 41;
                    }

                    else { rxBuffer.RemoveAt(0); continue; }

                    if (rxBuffer.Count < frameLength) break;

                    byte[] frame = rxBuffer.GetRange(0, frameLength).ToArray();
                    rxBuffer.RemoveRange(0, frameLength);

                    ProcessFrame(frame);
                }
            }
        }
        private void ProcessFrame(byte[] frame)
        {
            byte slaveID = frame[0];

            if (slaveID == 1)
            {
                UpdateMainBoardUI(frame);

            }
            else if (slaveID == 2)
            {
                UpdatePH07UI(frame);

            }
            else if (slaveID == 3)
            {
                UpdatePH01UI(frame);
            }
        }

        private void UpdatePH01UI(byte[] frame)
        {
            lock (_id3TimeLock) { _lastID3FrameTime = DateTime.Now; }

            if (frame.Length >= 5 && frame[1] == 0x03)
            {
                int byteCount = frame[2];
                int numRegs = byteCount / 2;

                this.BeginInvoke(new MethodInvoker(() =>
                {
                    for (int i = 0; i < numRegs; i++)
                    {
                        short value = (short)(frame[3 + (i * 2)] << 8 | frame[4 + (i * 2)]);

                        switch (i)
                        {
                            case 0:
                                _alarm2IsOn = (value != 0);
                                break;

                            case 1:
                                _alarm1IsOn = (value != 0);
                                break;
                            case 3:
                                ID3_reg3IsOn = (value != 0);
                                DrawLedBulb(picCom, ID3_reg3IsOn, Color.Red);
                                break;
                            case 4:
                                ID3_reg4IsOn = (value != 0);
                                DrawLedBulb(picHotFan, ID3_reg4IsOn, Color.Red);
                                break;
                            case 5:
                                ID3_reg5IsOn = (value != 0);
                                DrawLedBulb(picCoolFan, ID3_reg5IsOn, Color.Red);
                                break;
                        }

                    }
                }));
            }
            else if (frame[1] == 0x06)
            {
                //LogToRx("ID 3: Write Single Register Success!");
            }
        }
        private void UpdatePH07UI(byte[] frame)
        {
            if (frame.Length >= 5 && frame[1] == 0x03)
            {
                int byteCount = frame[2];
                int numRegs = byteCount / 2;

                for (int i = 0; i < numRegs; i++)
                {
                    short value = (short)(frame[3 + (i * 2)] << 8 | frame[4 + (i * 2)]);

                    switch (i)
                    {
                        case 0:
                            ID2_reg0IsOn = (value != 0);
                            break;
                        case 1:
                            ID2_reg1IsOn = (value != 0);
                            break;
                    }
                }

                this.BeginInvoke(new MethodInvoker(() =>
                {
                    DrawLedBulb(picWL, ID2_reg0IsOn, Color.Red);
                    DrawLedBulb(picHp, ID2_reg1IsOn, Color.Red);
                }));
            }
            else if (frame[1] == 0x06)
            {
                int addr = (frame[2] << 8) | frame[3];
                int val = (frame[4] << 8) | frame[5];

                if (addr == 0) ID2_reg0IsOn = (val != 0);
                if (addr == 1) ID2_reg1IsOn = (val != 0);
            }
        }
        private void UpdateMainBoardUI(byte[] frame)
        {
            if (frame == null) return;

            if (frame.Length > 2 && frame[1] == 0x03)
            {
                int byteCount = frame[2];
                int numRegs = byteCount / 2;

                this.BeginInvoke(new MethodInvoker(() =>
                {
                    for (int i = 0; i < numRegs; i++)
                    {
                        int actualRegister = lastRequestedStart + i;
                        short value = (short)(frame[3 + (i * 2)] << 8 | frame[4 + (i * 2)]);

                        lock (_regLock) { _lastRegisterValues[((byte)1, actualRegister)] = value; }

                        string targetName = "Regis" + actualRegister;
                        Control[] found = this.Controls.Find(targetName, true);

                        if (found.Length > 0 && found[0] is TextBox txt)
                        {
                            if (!txt.Focused)
                            {
                                switch (actualRegister)
                                {
                                    case 1:
                                        _currentSV = value / 10.0;
                                        txt.Text = _currentSV.ToString("F1");
                                        break;

                                    case 15:
                                        _currentSV = value / 10.0;
                                        txt.Text = _currentSV.ToString();
                                        break;

                                    case 16:
                                        _currentSV = value / 10.0;
                                        txt.Text = _currentSV.ToString();
                                        break;

                                    case 19:
                                        ushort maskedValue = (ushort)value;
                                        txt.Text = maskedValue.ToString("X4");

                                        if (backgroundWorkerOhm.IsBusy)
                                        {
                                            lock (_tempAdcBuffer)
                                            {
                                                _tempAdcBuffer.Clear();
                                                _tempAdcBuffer.Add(maskedValue);
                                            }
                                        }
                                        break;

                                    default:
                                        txt.Text = value.ToString();
                                        break;
                                }

                                txt.ForeColor = Color.Blue;
                            }
                        }
                        switch (i)
                        {
                            case 0:
                                _currentPv = value / 10.0;
                                lblRegis0.Text = _currentPv.ToString("F1") + " °C";
                                break;
                            case 20:
                                Led_Relay = (value != 0);
                                //DrawLedBulb(picRelay, Led_Relay, Color.Red);
                                break;
                        }
                    }
                }));
            }
            else if (frame[1] == 0x06)
            {
                int addr = (frame[2] << 8) | frame[3];
                int val = (frame[4] << 8) | frame[5];

                lock (_regLock) { _lastRegisterValues[((byte)1, addr)] = (short)val; }

                // ✅ reset _isWriting เฉพาะตอนได้ ack เขียนจริงๆ เท่านั้น
                _isWriting = false;

                this.Invoke(new MethodInvoker(() =>
                {
                    string targetName = "Regis" + addr;
                    Control[] found = this.Controls.Find(targetName, true);

                    if (found.Length > 0 && found[0] is TextBox txt)
                    {
                        if (addr == 1)
                        {
                            txt.Text = (val / 10.0).ToString("F1");
                        }
                        else
                        {
                            txt.Text = val.ToString();
                        }

                        txt.ForeColor = Color.Blue;
                    }
                }));
            }
            else if (frame[1] == 0x55)
            {
                //LogToRx("Calibration Success!\r\n");
            }
        }
        private void SendFrame(byte[] data)
        {
            lock (_lock)
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    HandleDisconnect("Port is closed.");
                    return;
                }

                try
                {
                    byte[] crc = CalculateCRC(data);
                    byte[] frame = new byte[data.Length + 2];

                    Array.Copy(data, frame, data.Length);

                    frame[data.Length] = crc[0];
                    frame[data.Length + 1] = crc[1];


                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Write(frame, 0, frame.Length);
                    }


                }
                catch (Exception ex)
                {
                    // ใช้ BeginInvoke แทน Invoke เพื่อไม่ให้ block thread นี้
                    this.BeginInvoke(new Action(() => HandleDisconnect("Write Error: " + ex.Message)));
                }
            }
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



        //-- timer
        private bool _timerBusy = false;
        private int _fastPollCount = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_timerBusy) return;
            _timerBusy = true;

            try
            {
                _fastPollCount++;
                if (_fastPollCount >= 15)
                {
                    timer1.Interval = 1000;
                }

                // ── 1. อัปเดตนาฬิกา ──────────────────────────────────────
                if (_runStopwatch.IsRunning)
                {
                    var t = _runStopwatch.Elapsed;
                    lblElapsedTime.Text = $" {t.Minutes:D2}:{t.Seconds:D2}";
                }

                // ── 2. นับถอยหลังเปิด Form4 ──────────────────────────────
                if (_autoOpenCountdown > 0)
                {
                    _autoOpenCountdown--;
                    if (_autoOpenCountdown == 0 && _DisPort != null && _DisPort.IsOpen)
                    {
                        // ✅ เลื่อนการเปิด Form4 ออกไปทำหลัง tick นี้จบ (ผ่าน message queue)
                        // ไม่ block ExecuteReadCycle ของ tick ปัจจุบัน และไม่มี Thread.Sleep ค้างเธรด UI
                        this.BeginInvoke(new Action(OpenForm4Deferred));
                    }
                }

                // ── 3. เช็คพอร์ต ─────────────────────────────────────────
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    HandleDisconnect("Port is not open.");
                    return;
                }

                if (_ohmPort == null || !_ohmPort.IsOpen)
                {
                    HandleDisconnect("Port Ohm Source is not open.");
                    return;
                }

                if (_DisPort == null || !_DisPort.IsOpen)
                {
                    HandleDisconnect("Port Display is not open.");
                    return;
                }

                // ── 4. ส่ง Modbus Read ───────────────────────────────────
                ExecuteReadCycle();
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

        private void OpenForm4Deferred()
        {
            if (_form4Instance != null && !_form4Instance.IsDisposed)
            {
                if (_DisPort != null)
                {
                    _DisPort.DataReceived -= _form4Instance.DataReceivedHandler;
                }
                _form4Instance.Close();
                _form4Instance = null;
            }

            _form4Instance = new Form4(_DisPort!, this);
            _form4Instance.Owner = this;
            _form4Instance.Show();
        }


        //-- ส่วนของการสั่งอ่านและเขียน
        int currentPollingID = 1;
        private void ExecuteReadCycle()
        {
            if (_isWriting && (DateTime.Now - _isWritingStartTime).TotalMilliseconds > 3000)
            {
                //LogToRx("⚠ _isWriting ค้างเกิน 3 วิ (ack หาย) บังคับปลดล็อกให้ poll ทำงานต่อ", Color.Orange);
                _isWriting = false;
            }

            if (_isWriting) return;

            byte idToSend = (byte)currentPollingID;
            ushort startAddress = 0;
            ushort quantity = 0;

            if (idToSend == 3)
            {
                startAddress = 0;
                quantity = 6;
                currentPollingID = 1;
            }

            else if (idToSend == 2)
            {
                startAddress = 0;
                quantity = 6;
                currentPollingID = 3;
            }

            else if (idToSend == 1)
            {
                startAddress = 0;
                quantity = 27;
                currentPollingID = 2;
            }

            lastRequestedStart = startAddress;

            if (_serialPort == null || !_serialPort.IsOpen) return;
            byte[] frame = new byte[6];
            frame[0] = idToSend;
            frame[1] = 0x03;
            frame[2] = (byte)(startAddress >> 8);
            frame[3] = (byte)(startAddress & 0xFF);
            frame[4] = (byte)(quantity >> 8);
            frame[5] = (byte)(quantity & 0xFF);

            SendFrame(frame);
        }
        private void ExecuteWriteSingleRegister(byte targetID, ushort address, short value)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            byte[] frame = new byte[6];
            frame[0] = targetID;
            frame[1] = 0x06;
            frame[2] = (byte)(address >> 8);
            frame[3] = (byte)(address & 0xFF);
            frame[4] = (byte)(value >> 8);
            frame[5] = (byte)(value & 0xFF);

            SendFrame(frame);
        }


        private void CommonRegis_Enter(object sender, EventArgs e)
        {
            if (sender is TextBox txt)
            {
                _originalText = txt.Text; // จำค่าเดิมเอาไว้ก่อน
            }
        }
        private void CommonRegis_TextChanged(object sender, EventArgs e)
        {
            if (sender is TextBox txt)
            {
                // ถ้าพิมพ์แล้วค่าเหมือนเดิมเป๊ะ ไม่ต้องปรับเป็น Dirty (ป้องกันกรณีกด Backspace แล้วพิมพ์ตัวเดิม)
                if (txt.Text == _originalText)
                {
                    _dirtyRegis.Remove(txt.Name);
                }
                else
                {
                    _dirtyRegis.Add(txt.Name);
                }
            }
        }
        private void CommonRegis_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                WriteRegisValue(sender);
                this.ActiveControl = null; // ← ทำให้ focus ออก = trigger Leave ด้วย
            }
        }
        private void CommonRegis_Leave(object sender, EventArgs e)
        {

        }

        // เขียน Register
        private readonly HashSet<string> _writingInProgress = new HashSet<string>(); // กันกดซ้ำระหว่างที่ retry ตัวเดิมยังไม่จบ
        private void WriteRegisValue(object sender)
        {
            if (sender is not TextBox txt) return;

            _dirtyRegis.Remove(txt.Name); // เคลียร์ dirty ไปเลย ไม่ต้องพึ่งเป็นเงื่อนไข block อีกต่อไป

            string regNumberStr = txt.Name.Replace("Regis", "");
            if (!ushort.TryParse(regNumberStr, out ushort address)) return;

            short valueToWrite = 0;

            switch (address)
            {
                case 1:
                case 15:
                case 16:
                case 17:
                    if (double.TryParse(txt.Text, out double dblValue))
                        valueToWrite = (short)Math.Round(dblValue * 10);
                    else return;
                    break;

                default:
                    if (short.TryParse(txt.Text, out short intValue))
                        valueToWrite = intValue;
                    else return;
                    break;
            }

            if (_writingInProgress.Contains(txt.Name)) return; // กันกด Enter รัวๆ ระหว่างรอบก่อนหน้ายังไม่จบ

            txt.ForeColor = Color.Orange;

            _ = WriteRegisValueWithRetryAsync(txt, address, valueToWrite);
        }
        private async Task WriteRegisValueWithRetryAsync(TextBox txt, ushort address, short valueToWrite, int delayMs = 300, int maxRetry = 8)
        {
            byte targetID = 1;
            _writingInProgress.Add(txt.Name);

            try
            {
                for (int attempt = 0; attempt < maxRetry; attempt++)
                {
                    if (_serialPort == null || !_serialPort.IsOpen) return;

                    // ถ้ามีคำสั่งเขียนอื่นวิ่งอยู่ (จาก background worker) รอสักครู่ก่อน กันชนกันบนบัส
                    int waitBusy = 0;
                    while (_isWriting && waitBusy < 1000)
                    {
                        await Task.Delay(100);
                        waitBusy += 100;
                    }

                    _isWriting = true;
                    _isWritingStartTime = DateTime.Now;

                    ExecuteWriteSingleRegister(targetID, address, valueToWrite);

                    await Task.Delay(delayMs);
                    await Task.Delay(400);

                    if (TryGetLastRegister(targetID, address, out short actualRaw))
                    {
                        //LogToRx($"[DEBUG] Manual Write addr={address} actualRaw={actualRaw} expected={valueToWrite} (ลองครั้งที่ {attempt + 1}/{maxRetry})");

                        if (Math.Abs((int)actualRaw - valueToWrite) <= 1)
                        {
                            // เปลี่ยนสีทันทีตรงนี้เลย ไม่ต้องพึ่งใคร
                            if (txt.InvokeRequired)
                                txt.Invoke(new Action(() => txt.ForeColor = Color.Blue));
                            else
                                txt.ForeColor = Color.Blue;

                            return;
                        }
                    }

                    // progressive backoff: รอเพิ่มขึ้นทีละรอบ กันกรณีบัสยุ่งชั่วคราว
                    if (attempt < maxRetry - 1)
                    {
                        await Task.Delay(200 * (attempt + 1));
                    }
                }

                // ครบ maxRetry แล้วยังไม่ผ่าน -> แดง
                if (txt.InvokeRequired)
                {
                    txt.Invoke(new Action(() => txt.ForeColor = Color.Red));
                }
                else
                {
                    txt.ForeColor = Color.Red;
                }

                LogToRx($"⚠ Reg{address} (ID 1) ไม่ถูก Set หลังลอง {maxRetry} ครั้ง (ส่ง {valueToWrite}) [Manual Write]", Color.Red);
            }
            finally
            {
                _writingInProgress.Remove(txt.Name); // ปลดล็อกเสมอ ไม่ว่าจะสำเร็จหรือ fail กด Enter ใหม่ได้ทันที
            }
        }



        //-- ส่วนของการสั่งให้ทำงาน Run, Stop
        private enum WorkerMode { Calibrate, Test, CalAndTest, CalAndSaveExcel }
        private WorkerMode _currentMode;

        private int _testStepResumed = 0;
        private bool _calDone = false;

        private async void btnRun_Click(object sender, EventArgs e)
        {
            txtSerialNumber.BackColor = SystemColors.Window;
            txtSerialNumber.ReadOnly = true; // ล็อคระหว่าง Run

            if (!_serialPort.IsOpen || !_ohmPort.IsOpen)
            {
                MessageBox.Show("กรุณาเชื่อมต่อทั้งพอร์ต Modbus และพอร์ต Ohm", "Warning");
                txtSerialNumber.ReadOnly = false; // ปลดล็อกกลับคืน
                return;
            }

            bool isTestMode = chkTest.Checked;

            if (isTestMode && string.IsNullOrWhiteSpace(txtSerialNumber.Text))
            {
                MessageBox.Show("กรุณาใส่ Serial Number ก่อนกด Run!", "Warning",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSerialNumber.ReadOnly = false; // ปลดล็อกกลับคืน
                txtSerialNumber.Focus();
                return;
            }

            if (!chkCal.Checked && !chkTest.Checked)
            {
                MessageBox.Show("กรุณาเลือกอย่างน้อย 1 กระบวนการ (Cal / Test)", "Warning");
                txtSerialNumber.ReadOnly = false; // ปลดล็อกกลับคืน
                return;
            }


            if (chkCal.Checked && chkTest.Checked)
                _currentMode = WorkerMode.CalAndTest;
            else if (chkCal.Checked)
                _currentMode = WorkerMode.Calibrate;
            else
                _currentMode = WorkerMode.Test;

            string modeText = _currentMode switch
            {
                WorkerMode.CalAndTest => "Cal + Test",
                WorkerMode.Calibrate => "Calibration",
                WorkerMode.Test => "Test",
                _ => ""
            };

            var confirm = MessageBox.Show($"เริ่ม {modeText} ใช่หรือไม่?", "Confirm", MessageBoxButtons.YesNo);
            if (confirm == DialogResult.No)
            {
                txtSerialNumber.ReadOnly = false;
                return;
            }

            _autoOpenCountdown = isTestMode ? 8 : 0;
            _runStopwatch.Restart();

            bool isCalibrateMode = (_currentMode == WorkerMode.Calibrate || _currentMode == WorkerMode.CalAndTest);

            bool hasCalPending = isCalibrateMode && _sequenceStep > 0 && !_calDone;
            bool hasTestPending = isTestMode && _testStepResumed > 0 && _testStepResumed < 8;

            List<string> sequenceValues = new List<string>
                   {
                        "100", "150", "300", "600", "1000", "1200", "1600",
                        "1800", "2000", "2600", "4000", "6000", "10000", "30000"
                   };

            if (hasCalPending || hasTestPending)
            {
                string detail = "";
                if (hasCalPending) detail += $"• Cal: ค้างอยู่ที่จุดที่ {_sequenceStep}/{sequenceValues.Count}\n";
                if (hasTestPending) detail += $"• Test: ค้างอยู่ที่ Step {_testStepResumed}\n";

                var resume = MessageBox.Show(
                    $"มีงานค้างอยู่:\n{detail}\n" +
                    $"กด Yes = ทำต่อจากที่หยุด\n" +
                    $"กด No  = เริ่มใหม่ทั้งหมด",
                    "Resume หรือ Restart?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resume == DialogResult.Yes)
                {
                    //RxBox.Clear();
                    LogToRx("ทำต่อจากที่หยุด...");

                }
                else
                {
                    ResetAllSteps();
                    ResetTestResults();
                    _sequenceStep = 0;
                    _testStepResumed = 0;
                    _calDone = false;
                    Array.Clear(finalAdcArray, 0, finalAdcArray.Length);
                    RxBox.Clear();
                    LogToRx("เริ่มใหม่ทั้งหมด");
                }
            }
            else
            {
                ResetAllSteps();
                ResetTestResults();
                _sequenceStep = 0;
                _testStepResumed = 0;
                _calDone = false;
                Array.Clear(finalAdcArray, 0, finalAdcArray.Length);
                RxBox.Clear();
            }

            progressBar1.Value = 0;
            await Task.Delay(500);

            btnRun.Text = "Running...";
            btnRun.Enabled = false;
            bntStop.Enabled = true;
            btnRun.BackColor = Color.Orange;

            RxBox.Clear();

            _isForm4ResultReady = false;
            backgroundWorkerOhm.RunWorkerAsync(sequenceValues);

        }
        private void bntStop_Click(object sender, EventArgs e)
        {
            if (!backgroundWorkerOhm.IsBusy) return;

            var confirm = MessageBox.Show("ต้องการหยุดกระบวนการใช่หรือไม่?", "Confirm", MessageBoxButtons.YesNo);
            if (confirm == DialogResult.No) return;

            backgroundWorkerOhm.CancelAsync();
            LogToRx("กำลังหยุดกระบวนการ...", Color.Orange);
            bntStop.Enabled = false;
        }



        //-- ส่วนของฟังก์ชัน Calibrate และ Test
        public void SendPreCalibrationFrame(BackgroundWorker worker, string roundName)
        {
            LogToRx($"--- กำลังส่ง Pre-Calibration {roundName} ---", Color.Blue);

            byte[] fullFrame = new byte[] {
                0x01, 0x55, 0x00, 0x00, 0x00, 0x10, 0x20, 0x50, 0x41, 0x53, 0x53, // Header
                0x17, 0x5E, 0x12, 0x94, 0x11, 0x94, 0x11, 0x0F, 0x0B, 0x10, 0x08, 0x83, 0x07, 0xAA, // Data
                0x06, 0xD0, 0x05, 0x1B, 0x04, 0x3F, 0x02, 0x89, 0x01, 0x40, 0x00, 0x9B, 0x00, 0x64, // Data
                0x09, 0xA7 // CRC
                };

            SendFrame(fullFrame);

            for (int i = 0; i < 30; i++)
            {
                if (worker.CancellationPending)
                {
                    LogToRx("--- ยกเลิก Pre-Calibration ---", Color.Red);
                    return;
                }

                System.Threading.Thread.Sleep(100);
            }

            LogToRx($"--- ส่ง Pre-Calibration {roundName} เรียบร้อยแล้ว ---", Color.Green);

        }
        private void DoCalibrate(DoWorkEventArgs e, int progressStart = 0, int progressEnd = 100)
        {

            if (_sequenceStep == 0)
            {
                LogToRx("--- เริ่มกระบวนการ Calibrate หลัก ---", Color.Black);
            }

            if (e.Argument is not List<string> dataList) { e.Cancel = true; return; }

            int total = dataList.Count;
            SetStep(0, StepState.Running);

            for (int idx = _sequenceStep; idx < total; idx++)
            {

                if (int.TryParse(dataList[idx], out int ohmValue))
                {
                    SendOhm(ohmValue);
                    System.Threading.Thread.Sleep(500);
                    CollectDataToArray(ohmValue);
                }

                _sequenceStep = idx + 1;
                int p = progressStart + (int)((idx + 1) / (double)total * 0.98 * (progressEnd - progressStart));
                backgroundWorkerOhm.ReportProgress(p);

                if (backgroundWorkerOhm.CancellationPending) { e.Cancel = true; return; }
            }

            byte[] calFrame = PrepareCalibrationFrame();
            SendFrame(calFrame);

            backgroundWorkerOhm.ReportProgress(progressEnd);

            if (!CancellableSleep(1000)) { e.Cancel = true; return; }

            LogToRx("[CAL DONE] Calibrate เสร็จสิ้น");
            SetStep(0, StepState.Pass);

            _calDone = true;
            _sequenceStep = 0;

            e.Result = "SUCCESS";
        }

        private int _calAttemptCount = 0;
        private const int MaxCalAttempts = 3;
        private bool _ohmCheckFailed = false; // true เฉพาะตอน fail ที่จุด 200/2000/8000 Ohm
        private void DoTest(DoWorkEventArgs e, int progressStart = 0, int progressEnd = 100)
        {
            LogToRx("--- เริ่มกระบวนการ Test ---");
            bool ShouldSkip(int step) => step < _testStepResumed;

            int Pct(int raw) => progressStart + raw * (progressEnd - progressStart) / 100;

            if (!ShouldSkip(1))
            {
                LogToRx(" set ค่า SV, D1, D2, D3");

                if (!WriteRegister(1, 1, 300)) { ReportStepFailure(e); return; } //SV   
                if (!WriteRegister(1, 9, 0)) { ReportStepFailure(e); return; }   //D1
                if (!WriteRegister(1, 10, 0)) { ReportStepFailure(e); return; }  //D2

                backgroundWorkerOhm.ReportProgress(Pct(5));

                if (!WriteRegister(1, 11, 0)) { ReportStepFailure(e); return; }  //D3
                if (!WriteRegister(1, 14, 1)) { ReportStepFailure(e); return; }  //ALF
                if (!WriteRegister(1, 12, 1)) { ReportStepFailure(e); return; }  //CLA

                if (!CancellableSleep(1000)) { e.Cancel = true; return; }

                _testStepResumed = 2;
            }

            if (!ShouldSkip(2))
            {
                SetStep(1, StepState.Running);

                backgroundWorkerOhm.ReportProgress(Pct(10));
                if (!VerifyOhmValueSync(200, 99.0)) { _ohmCheckFailed = true; e.Result = "FAILED"; return; }

                SetStep(1, StepState.Pass);

                _testStepResumed = 3;
            }

            if (!ShouldSkip(3))
            {
                backgroundWorkerOhm.ReportProgress(Pct(15));
                if (!VerifyOhmValueSync(821, 50.0)) { _ohmCheckFailed = true; e.Result = "FAILED"; return; }
                _testStepResumed = 4;
            }

            if (!ShouldSkip(4))
            {
                backgroundWorkerOhm.ReportProgress(Pct(20));
                if (!VerifyOhmValueSync(1158, 40.0)) { _ohmCheckFailed = true; e.Result = "FAILED"; return; }
                _testStepResumed = 5;
            }

            if (!ShouldSkip(5))
            {
                backgroundWorkerOhm.ReportProgress(Pct(25));
                if (!VerifyOhmValueSync(1703, 30.0)) { _ohmCheckFailed = true; e.Result = "FAILED"; return; }
                _testStepResumed = 6;
            }

            if (!ShouldSkip(6))
            {
                SetStep(2, StepState.Running);

                backgroundWorkerOhm.ReportProgress(Pct(30));
                if (!VerifyOhmValueSync(2000, 25.6)) { _ohmCheckFailed = true; e.Result = "FAILED"; return; }
                if (!CancellableSleep(1000)) { e.Cancel = true; return; }
                SetStep(2, StepState.Pass);

                SetStep(3, StepState.Running);
                if (!CheckRegisterStatus_ALL(1, 20, true, "LED Relay")) { e.Result = "FAILED"; return; }
                if (!CancellableSleep(1000)) { e.Cancel = true; return; }
                SetStep(3, StepState.Pass);

                _testStepResumed = 7;
            }

            if (!ShouldSkip(7))
            {
                backgroundWorkerOhm.ReportProgress(Pct(45));
                if (!VerifyOhmValueSync(2489, 20.0)) { _ohmCheckFailed = true; e.Result = "FAILED"; return; }
                _testStepResumed = 8;
            }

            if (!ShouldSkip(8))
            {
                SetStep(4, StepState.Running);

                backgroundWorkerOhm.ReportProgress(Pct(50));
                if (!VerifyOhmValueSync(8000, -6.4)) { _ohmCheckFailed = true; e.Result = "FAILED"; return; }

                SetStep(4, StepState.Pass);
                if (!CancellableSleep(500)) { e.Cancel = true; return; }

                _testStepResumed = 9;
            }

            if (!ShouldSkip(9))
            {
                SetStep(5, StepState.Running);
                backgroundWorkerOhm.ReportProgress(Pct(60));

                if (!CheckRegisterStatus(2, 0, 1, true, "ไฟ WL")) { e.Result = "FAILED"; return; }
                if (!WriteAndVerifyFlag(2, 0, 0, () => ID2_reg0IsOn, false)) { ReportStepFailure(e); return; }

                if (!CancellableSleep(1000)) { e.Cancel = true; return; }
                SetStep(5, StepState.Pass);
                _testStepResumed = 10;
            }

            if (!ShouldSkip(10))
            {
                SetStep(6, StepState.Running);
                backgroundWorkerOhm.ReportProgress(Pct(70));

                if (!CheckRegisterStatus(2, 1, 1, true, "ไฟ HP")) { e.Result = "FAILED"; return; }
                if (!WriteAndVerifyFlag(2, 0, 1, () => ID2_reg0IsOn, true)) { ReportStepFailure(e); return; }

                if (!CancellableSleep(1000)) { e.Cancel = true; return; }
                SetStep(6, StepState.Pass);
                _testStepResumed = 11;
            }

            if (!ShouldSkip(11))
            {
                SetStep(7, StepState.Running);
                LogToRx("set ค่า ALH = 50 และ ALF = 1");

                if (!WriteRegister(1, 15, 500)) { ReportStepFailure(e); return; }


                backgroundWorkerOhm.ReportProgress(Pct(80));
                if (!CheckRegisterStatus_ALL(3, 1, true, "Alarm1")) { ReportStepFailure(e); return; }

                SetStep(7, StepState.Pass);
                if (!CancellableSleep(3000)) { e.Cancel = true; return; }

                _testStepResumed = 12;
            }

            if (!ShouldSkip(12))
            {
                LogToRx("ตรวจสอบการทำงาน Compressor และ Fan ");

                backgroundWorkerOhm.ReportProgress(Pct(90));
                SetStep(8, StepState.Running);

                if (!WriteRegister(1, 1, 200)) { ReportStepFailure(e); return; }

                ID3_reg3IsOn = false;
                ID3_reg4IsOn = false;
                ID3_reg5IsOn = false;

                if (!CheckRegisterStatus_ALL(3, 5, true, "CoolFan")) { ReportStepFailure(e); return; }

                if (!CancellableSleep(1000)) { e.Cancel = true; return; }

                SendOhm(2000);

                if (!CheckCompressorAndHotFan(20)) { ReportStepFailure(e); return; }

                if (!CancellableSleep(3000)) { e.Cancel = true; return; }

                SetStep(8, StepState.Pass);

                backgroundWorkerOhm.ReportProgress(Pct(95));

                _testStepResumed = 13;
            }

            if (!CancellableSleep(1000)) { e.Cancel = true; return; }

            if (!ShouldSkip(13))
            {
                LogToRx("Setค่า SV = 20, ALF = 1, D1 = 3, D2 = 3, D = 1, ON = 5, CRL = 1");

                SetStep(9, StepState.Running);

                //if (!WriteRegister(1, 1, 200)) { ReportStepFailure(e); return; }
                if (!WriteRegister(1, 9, 3)) { ReportStepFailure(e); return; }
                if (!WriteRegister(1, 10, 3)) { ReportStepFailure(e); return; }

                backgroundWorkerOhm.ReportProgress(Pct(95));

                if (!WriteRegister(1, 11, 1)) { ReportStepFailure(e); return; }
                if (!WriteRegister(1, 7, 5)) { ReportStepFailure(e); return; }
                if (!WriteRegister(1, 12, 1)) { ReportStepFailure(e); return; }

                SetStep(9, StepState.Pass);
                _testStepResumed = 14;
            }

            if (!CancellableSleep(500)) { e.Cancel = true; return; }
            LogToRx("[ALL PASS] ตรวจสอบเสร็จสิ้นทุกขั้นตอน");
            e.Result = "SUCCESS";
        }

        private bool CheckCompressorAndHotFan(int timeoutSeconds = 20)
        {
            LogToRx("กำลังตรวจสอบ Compressor และ HotFan");

            bool isCompressorOn = false;
            bool isHotFanOn = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (sw.Elapsed.TotalSeconds < timeoutSeconds)
            {
                if (ID3_reg3IsOn) isCompressorOn = true;
                if (ID3_reg4IsOn) isHotFanOn = true;

                if (isCompressorOn && isHotFanOn) break;

                if (backgroundWorkerOhm.CancellationPending) return false;
                if (!CancellableSleep(200)) return false;
            }

            if (isCompressorOn)
            {
                _Compressor = "OK";
                LogToRx("[PASS] Compressor ทำงาน", Color.Green);
            }
            else
            {
                _Compressor = "FAIL";
                LogToRx("[FAIL] Compressor ไม่ทำงาน!", Color.Red);
            }

            if (isHotFanOn)
            {
                _HotFan = "OK";
                LogToRx("[PASS] HotFan ทำงาน", Color.Green);
            }
            else
            {
                _HotFan = "FAIL";
                LogToRx("[FAIL] HotFan ไม่ทำงาน!", Color.Red);
            }

            return isCompressorOn && isHotFanOn;
        }

        private void DoCalAndTestWithRetry(DoWorkEventArgs e)
        {
            _calAttemptCount = 0;

            while (true)
            {
                _calAttemptCount++;

                if (_calAttemptCount == 1)
                {
                    LogToRx($"--- เริ่ม Calibrate (รอบที่ {_calAttemptCount}/{MaxCalAttempts}) ---", Color.Blue);
                }
                else
                {
                    LogToRx($"--- Recalibrate รอบที่ {_calAttemptCount}/{MaxCalAttempts} (เนื่องจาก Test Ohm ไม่ผ่าน) ---", Color.Orange);
                }

                // Cal ใหม่ทั้งหมด (progress 0-50%)
                DoCalibrate(e, 0, 50);
                if (e.Cancel || e.Result?.ToString() == "FAILED") return;

                // Test: รอบแรกทำเต็ม (รวม step1), รอบ retry ข้าม step1 เริ่มที่ 200 Ohm เลย
                if (_calAttemptCount > 1)
                {
                    _testStepResumed = 2;
                    ResetAllSteps();
                    SetStep(0, StepState.Pass); // Cal LED ให้ค้าง Pass ไว้
                }

                _ohmCheckFailed = false;
                DoTest(e, 50, 100);

                if (e.Cancel) return;

                if (e.Result?.ToString() == "SUCCESS")
                {
                    return; // ผ่านหมดแล้ว จบ
                }

                // fail ที่จุด Ohm และยังมีโควต้า retry เหลือ -> วนกลับไป Cal ใหม่
                if (_ohmCheckFailed && _calAttemptCount < MaxCalAttempts)
                {
                    _sequenceStep = 0;
                    _calDone = false;
                    _testStepResumed = 0;
                    Array.Clear(finalAdcArray, 0, finalAdcArray.Length);
                    backgroundWorkerOhm.ReportProgress(0);
                    continue;
                }

                // fail จากจุดอื่น หรือ retry ครบโควต้าแล้ว -> จบแบบ FAILED จริง
                if (_ohmCheckFailed)
                {
                    LogToRx($"[FAILED] Recalibrate ครบ {MaxCalAttempts} รอบแล้ว ยังไม่ผ่าน Ohm Test", Color.Red);
                }
                return;
            }
        }
        private bool WriteRegister(byte id, ushort addr, short val, int delayMs = 300, int maxRetry = 8)
        {
            for (int attempt = 0; attempt < maxRetry; attempt++)
            {
                if (backgroundWorkerOhm.CancellationPending) { _isWriting = false; return false; }

                _isWriting = true;
                ExecuteWriteSingleRegister(id, addr, val);

                // ✅ เพิ่มเวลารอหลังเขียน ให้มีเวลาพอสำหรับ ack/poll กลับมาก่อนเช็ค
                if (!CancellableSleep(delayMs)) { _isWriting = false; return false; }
                if (!CancellableSleep(400)) { _isWriting = false; return false; }

                _isWriting = false;

                // 👇 เช็คจากค่าดิบที่เก็บไว้ตรงๆ ไม่ผ่าน UI แล้ว
                if (TryGetLastRegister(id, addr, out short actualRaw))
                {
                    //LogToRx($"[DEBUG] addr={addr} actualRaw={actualRaw} expected={val}");

                    if (Math.Abs((int)actualRaw - val) <= 1) return true;
                }

                // ✅ progressive backoff: รอเพิ่มขึ้นทีละรอบ กันกรณีบัสยุ่งชั่วคราว
                if (attempt < maxRetry - 1)
                {
                    if (!CancellableSleep(200 * (attempt + 1))) { return false; }
                }
            }

            LogToRx($"⚠ Reg{addr} (ID {id}) ไม่ถูก Set หลังลอง {maxRetry} ครั้ง (ส่ง {val})", Color.Red);
            return false;
        }
        private void ReportStepFailure(DoWorkEventArgs e)
        {
            if (backgroundWorkerOhm.CancellationPending)
                e.Cancel = true;
            else
                e.Result = "FAILED";
        }
        private bool WriteAndVerifyFlag(byte id, ushort addr, short val, Func<bool> currentFlag, bool expected, int delayMs = 300, int maxRetry = 5)
        {
            for (int attempt = 0; attempt < maxRetry; attempt++)
            {
                if (backgroundWorkerOhm.CancellationPending) return false;

                _isWriting = true;                          // ✅
                ExecuteWriteSingleRegister(id, addr, val);
                if (!CancellableSleep(delayMs)) return false;
                _isWriting = false;                          // ✅

                if (currentFlag() == expected) return true;
            }
            return false;
        }


        //-- ส่วนของการทำงานเบื้องหลัง
        private void backgroundWorker1_DoWork(object? sender, DoWorkEventArgs e)
        {
            switch (_currentMode)
            {
                case WorkerMode.Calibrate:
                    DoCalibrate(e, 0, 100);
                    break;

                case WorkerMode.CalAndSaveExcel:
                    DoCalibrate(e, 0, 100);
                    break;

                case WorkerMode.Test:
                    DoTest(e, 0, 100);
                    break;

                case WorkerMode.CalAndTest:
                    DoCalAndTestWithRetry(e);
                    break;
            }
        }
        private void backgroundWorker1_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action(() => progressBar1.Value = e.ProgressPercentage));
            }
            else
            {
                progressBar1.Value = e.ProgressPercentage;
            }
        }
        private void CloseForm4IfOpen()
        {
            if (_form4Instance != null && !_form4Instance.IsDisposed)
            {
                try
                {
                    if (_DisPort != null)
                    {
                        _DisPort.DataReceived -= _form4Instance.DataReceivedHandler;
                    }
                    _form4Instance.Close();
                }
                catch { }
            }
            _form4Instance = null;
            _autoOpenCountdown = 0;
        }
        private void backgroundWorker1_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            btnRun.Text = "Run";
            btnRun.Enabled = true;
            bntStop.Enabled = false;
            btnRun.BackColor = Color.Gold;

            btnCalSave.Text = "ทดสอบ";
            btnCalSave.Enabled = true;
            btnRun.Enabled = true;

            txtSerialNumber.ReadOnly = false;
            //txtSerialNumber.Clear();      
            txtSerialNumber.Focus();

            progressBar1.Value = 100;

            _runStopwatch.Stop();

            ExecuteWriteSingleRegister(2, 0, 0);
            System.Threading.Thread.Sleep(100);
            ExecuteWriteSingleRegister(2, 1, 0);

            var t = _runStopwatch.Elapsed;
            lblElapsedTime.Text = $"{t.Minutes:D2}:{t.Seconds:D2}";

            string modeLabel = _currentMode switch
            {
                WorkerMode.Calibrate => "Calibration",
                WorkerMode.Test => "Test",
                WorkerMode.CalAndTest => "Cal + Test",
                WorkerMode.CalAndSaveExcel => "Calibration & Save",
                _ => ""
            };

            bool hasTest = (_currentMode == WorkerMode.Test || _currentMode == WorkerMode.CalAndTest);

            if (e.Cancelled)
            {
                CloseForm4IfOpen();
                LogToRx($"{modeLabel}: หยุดการทำงานชั่วคราว");
            }
            else if (e.Error != null)
            {
                CloseForm4IfOpen();
                MessageBox.Show("เกิดข้อผิดพลาด: " + e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (e.Result?.ToString() == "SUCCESS")
            {
                if (_currentMode == WorkerMode.CalAndSaveExcel)
                {
                    SaveAdcToExcel(finalAdcArray);
                    MessageBox.Show("Calibration และบันทึกข้อมูลลง Excel สำเร็จ!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _sequenceStep = 0;
                _testStepResumed = 0;
                _calDone = false;

                chkCal.Checked = false;
                chkTest.Checked = false;

                LogToRx($"{modeLabel}: เสร็จสิ้นสมบูรณ์", Color.Green);
                MessageBox.Show($"{modeLabel} เสร็จสิ้น!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (hasTest)
                {
                    if (_isForm4ResultReady)
                    {
                        bool btnAllPass = (_btnFunctionResult == "OK" || _btnFunctionResult == "-")
                                       && (_btnDownResult == "OK" || _btnDownResult == "-")
                                       && (_btnUpResult == "OK" || _btnUpResult == "-");
                        string finalStatus = btnAllPass ? "PASS" : "FAIL";
                        SaveTestDataToExcel(finalStatus);
                    }
                    else
                    {
                        LogToRx("รอผลการทดสอบปุ่มจาก Form4 เพื่อบันทึกข้อมูล...", Color.Orange);
                    }
                }
            }
            else
            {
                CloseForm4IfOpen();
                LogToRx($"{modeLabel}: ล้มเหลว");
                MessageBox.Show($"ไม่สามารถ {modeLabel} ได้ตามเกณฑ์ที่กำหนด", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (hasTest) SaveTestDataToExcel("FAIL");
            }
        }


        //--- ส่วนประกอบในการสั่ง Calibrate
        public void SendOhm(double ohmValue) // 1. เปลี่ยนตรงนี้จาก int เป็น double
        {
            if (_ohmPort == null || !_ohmPort.IsOpen)
            {
                MessageBox.Show("กรุณาเชื่อมต่อพอร์ต Ohm ก่อนครับ", "Warning");
                return;
            }

            try
            {
                int multiplier = chkMultiplyBy100.Checked ? 100 : 10;

                // 2. คูณเสร็จแล้วใช้ Math.Round ปัดเศษให้เป็นจำนวนเต็ม ก่อนจะแปลงเป็น int
                int result = (int)Math.Round(ohmValue * multiplier);

                // --- ส่วนการแปลง Hex และส่งข้อมูลด้านล่างนี้ใช้เหมือนเดิมได้เลยครับ ---
                string resultHex = result.ToString("X").PadLeft(8, '0');
                string reversedStr = resultHex.Substring(6, 2) + resultHex.Substring(4, 2) +
                                     resultHex.Substring(2, 2) + resultHex.Substring(0, 2);
                reversedStr = reversedStr.PadRight(10, '0');

                byte[] byteArray = Enumerable.Range(0, reversedStr.Length / 2)
                                     .Select(x => Convert.ToByte(reversedStr.Substring(x * 2, 2), 16))
                                     .ToArray();

                byte[] finalFrame = new byte[] { 0x50 }.Concat(byteArray).ToArray();

                _ohmPort.DiscardOutBuffer();
                _ohmPort.Write(finalFrame, 0, finalFrame.Length);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending data: " + ex.Message);
            }
        }
        private void CollectDataToArray(int currentOhm)
        {
            int waitSteps = (currentOhm == 100) ? 60 : 50;

            for (int i = 0; i < waitSteps; i++)
            {
                if (backgroundWorkerOhm.CancellationPending) return;
                System.Threading.Thread.Sleep(100);
            }

            double lastValue = 0;
            lock (_tempAdcBuffer)
            {
                if (_tempAdcBuffer.Count > 0)
                {
                    lastValue = _tempAdcBuffer[_tempAdcBuffer.Count - 1];
                    _tempAdcBuffer.Clear();
                }
            }

            if (_sequenceStep < finalAdcArray.Length)
            {
                finalAdcArray[_sequenceStep] = lastValue;

                this.Invoke(new MethodInvoker(() =>
                {
                    string timeStamp = DateTime.Now.ToString("HH:mm:ss");
                    int maskedAdc = (int)lastValue;
                    RxBox.AppendText($"[{timeStamp}] ADC: {maskedAdc:X3}|Ohm: {currentOhm}\r\n");
                    RxBox.ScrollToCaret();
                }));

                _sequenceStep++;
            }
        }
        private byte[] PrepareCalibrationFrame()
        {
            string generatedHex = "";
            string displayLog = "\r\n--- Final CalData to be Sent (Reversed & Modified) ---\r\n";

            for (int i = 13; i >= 0; i--)
            {
                short valueToConvert = (short)finalAdcArray[i];

                string hexPart = valueToConvert.ToString("X4");
                generatedHex += hexPart;

                displayLog += $"Index [{i:D2}] -> {hexPart}\r\n";
            }

            displayLog += "------------------------------------------------------\r\n";

            LogToRx(displayLog);


            byte[] header = { 0x01, 0x55, 0x00, 0x00, 0x00, 0x10, 0x20, 0x50, 0x41, 0x53, 0x53 };
            byte[] calData = HexStringToByteArray(generatedHex);

            byte[] fullFrame = new byte[header.Length + calData.Length];
            Array.Copy(header, 0, fullFrame, 0, header.Length);
            Array.Copy(calData, 0, fullFrame, header.Length, calData.Length);

            return fullFrame;
        }
        private byte[] HexStringToByteArray(string hex)
        {
            hex = hex.Replace(" ", "").Replace("-", "");
            try
            {
                byte[] buffer = new byte[hex.Length / 2];
                for (int i = 0; i < hex.Length; i += 2)
                {
                    buffer[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
                return buffer;
            }
            catch
            {
                throw new Exception("รูปแบบ Hex ไม่ถูกต้อง กรุณาตรวจสอบอีกครั้ง");
            }
        }



        //--- ส่วนของการตรวจสอบ
        private string _verify200Result = "-";
        private string _verify2000Result = "-";
        private string _verify8000Result = "-";
        private string _verify20Result = "-";
        private string _verify30Result = "-";
        private string _verify40Result = "-";
        private string _verify50Result = "-";
        private bool VerifyOhmValueSync(int ohm, double expectedPv)
        {
            LogToRx($"กำลังทดสอบที่: {ohm} Ohm (เป้าหมาย PV: {expectedPv})...");
            SendOhm(ohm);

            int timeoutSeconds = 8;
            bool isPass = false;

            for (int i = 0; i < timeoutSeconds; i++)
            {
                System.Threading.Thread.Sleep(1000);

                double diff = Math.Abs(_currentPv - expectedPv);
                //LogToRx($"วินาทีที่ {i + 1}: ค่าปัจจุบัน PV = {_currentPv:F2} (Diff: {diff:F2})");

                if (diff < 1.0)
                {
                    isPass = true;
                    break;
                }
            }

            LogToRx(isPass ? $"[PASS] {ohm} Ohm" : $"[FAIL] {ohm} Ohm (PV สุดท้าย={_currentPv})", Color.Green);

            string result = _currentPv.ToString("F1");
            if (ohm == 200) _verify200Result = result;
            if (ohm == 2000) _verify2000Result = result;
            if (ohm == 8000) _verify8000Result = result;
            if (ohm == 2489) _verify20Result = result;
            if (ohm == 1703) _verify30Result = result;
            if (ohm == 1158) _verify40Result = result;
            if (ohm == 821) _verify50Result = result;
            return isPass;
        }

        private bool RunTempSweepFinal(int waitMs = 5000)
        {
            for (int i = 0; i < TempSweepPoints.Length; i++)
            {
                var (ohm, targetTemp) = TempSweepPoints[i];

                LogToRx($"[FINAL SWEEP] จ่าย {ohm} Ohm (เป้าหมาย {targetTemp}°C) รอ {waitMs / 1000} วิ...");
                SendOhm(ohm);

                if (!CancellableSleep(waitMs)) return false;

                _tempSweepResults[i] = _currentPv.ToString("F1");
                LogToRx($"[FINAL SWEEP] {targetTemp}°C -> Register0 = {_tempSweepResults[i]} °C", Color.Blue);
            }
            return true;
        }

        private string _wlResult = "-";
        private string _hpResult = "-";
        private string _wl_AL2Result = "-";
        private string _hp_AL2Result = "-";
        private bool CheckRegisterStatus(byte targetID, int address, int value, bool expectedStatus, string label)
        {
            LogToRx($"กำลังตรวจสอบ {label}");

            // จำเวลาตอนเริ่มฟังก์ชันนี้ไว้ก่อน ใช้เป็นจุดอ้างอิง
            DateTime checkStartTime = DateTime.Now;

            bool currentStatus = false;
            int maxAttempts = 12;

            for (int i = 0; i < maxAttempts; i++)
            {
                if (backgroundWorkerOhm.CancellationPending) return false;   // เช็ค cancel ก่อนทุกรอบ

                if (i % 3 == 0)   // ยิงซ้ำทุก 3 รอบ (1.5 วิ) กันเฟรมหลุด
                {
                    ExecuteWriteSingleRegister(targetID, (ushort)address, (short)value);
                }

                if (!CancellableSleep(500)) return false;

                currentStatus = address switch
                {
                    0 => ID2_reg0IsOn,
                    1 => ID2_reg1IsOn,
                    _ => false
                };

                if (currentStatus == expectedStatus) break;
            }

            if (!CancellableSleep(500)) return false;

            if (currentStatus != expectedStatus)
            {
                LogToRx($"[FAIL] {label} ไม่ทำงาน! (ค่าปัจจุบัน: {currentStatus}, ค่าที่หวัง: {expectedStatus})", Color.Red);
                return false;
            }

            LogToRx($"[PASS] {label} ทำงาน", Color.Green);

            // ✅ เพิ่มส่วนนี้: รอให้ ID3 ส่ง frame ใหม่เข้ามาจริงๆ ก่อนอ่าน _alarm2IsOn
            // ป้องกันอ่านค่าค้างจาก poll cycle ก่อนหน้า (สูงสุด 5 วิ)
            DateTime waitStart = DateTime.Now;
            while ((DateTime.Now - waitStart).TotalMilliseconds < 5000)
            {
                DateTime lastFrame;
                lock (_id3TimeLock) { lastFrame = _lastID3FrameTime; }

                if (lastFrame > checkStartTime) break;   // มี frame ใหม่มาแล้วหลังจากเริ่มเช็ค
                if (backgroundWorkerOhm.CancellationPending) return false;
                Thread.Sleep(100);
            }

            if (address == 0)
            {
                _wlResult = "OK";
                if (_alarm2IsOn)
                {
                    _wl_AL2Result = "OK";
                    LogToRx($"[PASS] {label} ทำงาน และ Alarm2 ทำงาน", Color.Green);
                }
                else
                {
                    _wl_AL2Result = "FAIL";
                    LogToRx($"[FAIL] {label} ทำงาน แต่ Alarm2 ไม่ทำงาน", Color.Orange);
                    return false;
                }
            }

            if (address == 1)
            {
                _hpResult = "OK";
                if (_alarm2IsOn)
                {
                    _hp_AL2Result = "OK";
                    LogToRx($"[PASS] {label} ทำงาน และ Alarm2 ทำงาน", Color.Green);
                }
                else
                {
                    _hp_AL2Result = "FAIL";
                    LogToRx($"[FAIL] {label} ทำงาน แต่ Alarm2 ไม่ทำงาน", Color.Orange);
                    return false;
                }
            }

            return true;
        }

        private string _alarm1Result = "-";
        private string _Compressor = "-";
        private string _HotFan = "-";
        private string _CoolFan = "-";
        private string _RelayResult = "-";
        private bool CheckRegisterStatus_ALL(byte targetID, int address, bool expectedStatus, string label)
        {
            LogToRx($"กำลังตรวจสอบ {label}");

            bool currentStatus = false;
            int maxAttempts = 10;

            for (int i = 0; i < maxAttempts; i++)
            {
                currentStatus = address switch
                {
                    1 => _alarm1IsOn,
                    3 => ID3_reg3IsOn,
                    4 => ID3_reg4IsOn,
                    5 => ID3_reg5IsOn,
                    20 => Led_Relay,
                    _ => false
                };

                if (currentStatus == expectedStatus) break;

                if (backgroundWorkerOhm.CancellationPending) return false;

                System.Threading.Thread.Sleep(500);
            }

            if (currentStatus == expectedStatus)
            {
                LogToRx($"[PASS] {label} ทำงาน", Color.Green);

                if (address == 1) _alarm1Result = "OK";
                if (address == 3) _Compressor = "OK";
                if (address == 4) _HotFan = "OK";
                if (address == 5) _CoolFan = "OK";
                if (address == 20) _RelayResult = "OK";

                return true;
            }
            else
            {

                LogToRx($"[FAIL] {label} ไม่ทำงาน! (ค่าปัจจุบัน: {currentStatus}, ค่าที่หวัง: {expectedStatus})", Color.Red);

                if (address == 1) _alarm1Result = "FAIL";
                if (address == 3) _Compressor = "FAIL";
                if (address == 4) _HotFan = "FAIL";
                if (address == 5) _CoolFan = "FAIL";
                if (address == 20) _RelayResult = "FAIL";

                return false;
            }
        }



        //-- ส่วนของการแสดงผล RXBOX
        private void LogToRx(string message, Color? color = null)
        {
            if (RxBox.InvokeRequired)
            {
                RxBox.Invoke(new MethodInvoker(() => LogToRx(message, color)));
                return;
            }

            string timeStamp = DateTime.Now.ToString("HH:mm:ss");
            string fullMessage = $"[{timeStamp}] {message}\r\n";
            RxBox.SelectionStart = RxBox.TextLength;
            RxBox.SelectionLength = 0;
            RxBox.SelectionColor = color ?? RxBox.ForeColor; // ถ้าไม่ระบุสี ใช้สีปกติ
            RxBox.AppendText(fullMessage);
            RxBox.SelectionColor = RxBox.ForeColor; // reset กลับ
            RxBox.ScrollToCaret();
        }
        private bool CancellableSleep(int milliseconds)
        {
            int steps = milliseconds / 100;
            for (int i = 0; i < steps; i++)
            {
                if (backgroundWorkerOhm.CancellationPending) return false; // สั่งหยุด
                Thread.Sleep(100);
            }
            return true; // ครบเวลาปกติ
        }



        //-- ปุ่มการทำงานใน From 2 (Control)
        private void setting_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Form2 f2 = new Form2(this);
            f2.Show();
        }

        private async Task CheckOhm(int ohm, double expectedPv)
        {
            try
            {
                SendOhm(ohm);

                bool isPass = false;

                for (int i = 0; i < 8; i++)
                {
                    await Task.Delay(1000);

                    double diff = Math.Abs(_currentPv - expectedPv);

                    if (diff < 1.0)
                    {
                        isPass = true;
                        break;
                    }
                }

                if (isPass)
                    LogToRx($"[PASS] {ohm} Ohm | PV = {_currentPv:F1}", Color.Green);
                else
                    LogToRx($"[FAIL] {ohm} Ohm | PV = {_currentPv:F1} (เป้าหมาย {expectedPv})", Color.Red);
            }
            catch (Exception ex)
            {
                LogToRx($"Error: {ex.Message}", Color.Red);
            }
        }
        public async void btn2000_Click(object sender, EventArgs e) => await CheckOhm(2000, 25.6);
        public async void btn200_Click(object sender, EventArgs e) => await CheckOhm(200, 99.2);
        public async void btn8000_Click(object sender, EventArgs e) => await CheckOhm(8000, -6.4);


        public void btnHP_ON_Click(object sender, EventArgs e) { ExecuteWriteSingleRegister(2, 1, 1); }
        public void btnHP_Off_Click(object sender, EventArgs e) { ExecuteWriteSingleRegister(2, 1, 0); }
        public async void btnWL_Off_Click(object sender, EventArgs e) { ExecuteWriteSingleRegister(2, 0, 0); }
        public async void btnWL_On_Click(object sender, EventArgs e) { ExecuteWriteSingleRegister(2, 0, 1); }



        //-- ควบคุมการสลับหน้า(tap)
        private void displaybox_MouseDoubleClick(object sender, MouseEventArgs e) { tabControl2.SelectedIndex = 1; }
        private void pictHome_DoubleClick(object sender, EventArgs e) { tabControl2.SelectedIndex = 0; }
        private void picdata_DoubleClick(object sender, EventArgs e) { tabControl2.SelectedIndex = 2; }
        private void BackHome_Click(object sender, EventArgs e) { tabControl2.SelectedIndex = 0; }
        private void displaybox_Click(object sender, EventArgs e) { }



        //-- การตั้งค่าFrom 1
        private void Form1_Load(object sender, EventArgs e)
        {
            _excelFilePath = string.Empty;
            txtExcelPath.Text = "กรุณาเลือก LOT เพื่อโหลดข้อมูล";

            _customResultFolder = Properties.Settings.Default.SavedResultFolder;
            if (!string.IsNullOrEmpty(_customResultFolder))
            {
                txtExcelPath.Text = _customResultFolder;
            }

            LoadLotHistory();

            string? savedLot = Properties.Settings.Default.SavedLot?.Trim();
            if (!string.IsNullOrEmpty(savedLot) && !string.IsNullOrEmpty(ResultFolder))
            {
                txtLot.Text = savedLot;
                string safeFileName = savedLot.Replace("/", "-");
                _excelFilePath = Path.Combine(ResultFolder, safeFileName + ".xlsx");
                txtExcelPath.Text = _excelFilePath;
            }
            else if (!string.IsNullOrEmpty(savedLot))
            {
                // มี LOT จำไว้ แต่ยังไม่เคยเลือกโฟลเดอร์ — โชว์ชื่อ LOT ไว้ก่อน แต่ไม่ต้อง build path
                txtLot.Text = savedLot;
            }

            cmbDisplaySource.Text = "EXCEL";

            LoadExcelToDataGrid();

            cmbLot.Text = "";
            txtLot.Text = Properties.Settings.Default.SavedLot;

            // เติมข้อมูล Dropdown วัน/เดือน/ปี
            for (int i = 1; i <= 31; i++) cmbDay.Items.Add(i.ToString("00"));
            for (int i = 1; i <= 12; i++) cmbMonth.Items.Add(i.ToString("00"));
            for (int i = 2025; i <= 2035; i++) cmbYear.Items.Add(i.ToString());

            cmbDay.SelectedIndex = -1;
            cmbMonth.SelectedIndex = -1;
            cmbYear.SelectedIndex = -1;
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "กรุณาเลือกโฟลเดอร์สำหรับบันทึกผลการทดสอบ (Excel)";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _customResultFolder = fbd.SelectedPath;
                    txtExcelPath.Text = _customResultFolder;

                    // เพิ่ม 2 บรรทัดนี้
                    Properties.Settings.Default.SavedResultFolder = _customResultFolder;
                    Properties.Settings.Default.Save();

                    RefreshFolderData();
                }
            }
        }
        private void RefreshFolderData()
        {
            if (string.IsNullOrEmpty(_customResultFolder)) return;

            // อัปเดต ComboBox แสดงรายการไฟล์ในโฟลเดอร์ที่เลือก
            LoadLotHistory();
        }



        //-- ส่วนของการเก็บผลการ test ลง Excel
        private string? _customResultFolder = null;
        public string? ResultFolder => _customResultFolder;
        private bool EnsureFolderSelected()
        {
            if (string.IsNullOrEmpty(_customResultFolder) || !Directory.Exists(_customResultFolder))
            {
                MessageBox.Show("กรุณาเลือกโฟลเดอร์สำหรับจัดเก็บข้อมูลก่อนดำเนินการ!",
                                "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // เปิดหน้าต่างให้เลือกโฟลเดอร์ทันที
                btnSelectFolder_Click(this, EventArgs.Empty);

                // เช็คอีกรอบหลังเลือก
                return !string.IsNullOrEmpty(_customResultFolder) && Directory.Exists(_customResultFolder);
            }
            return true;
        }
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            Directory.CreateDirectory(ResultFolder!);
            System.Diagnostics.Process.Start("explorer.exe", ResultFolder!);
        }
        private void btnDelLot_Click(object sender, EventArgs e)
        {
            txtLot.Text = "";
            Properties.Settings.Default.SavedLot = "";
            Properties.Settings.Default.Save();

            //MessageBox.Show("ลบข้อมูล LOT เรียบร้อยแล้ว", "ข้อมูลอัปเดต");
        }
        private bool IsSerialNumberDuplicate(string sn, string lotNumber)
        {
            if (string.IsNullOrEmpty(ResultFolder)) return false;

            if (string.IsNullOrEmpty(sn) || string.IsNullOrEmpty(lotNumber))
                return false;

            string safeFileName = lotNumber.Replace("/", "-");
            string currentLotPath = Path.Combine(ResultFolder, safeFileName + ".xlsx");


            if (!File.Exists(currentLotPath))
                return false;

            try
            {
                using (var workbook = new XLWorkbook(currentLotPath))
                {
                    var sheet = workbook.Worksheet(1);

                    int startRow = 12;
                    int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 11;

                    for (int row = startRow; row <= lastRow; row++)
                    {
                        string existingSN = sheet.Cell(row, 28).GetString().Trim();

                        if (!string.IsNullOrEmpty(existingSN) &&
                            existingSN.Equals(sn.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToRx($"เกิดข้อผิดพลาดขณะเปิดเช็คไฟล์ Excel: {ex.Message}", Color.Red);
            }

            return false;
        }
        private void picCir_DoubleClick(object sender, EventArgs e) { LoadExcelToDataGrid(); }



        private string _lastTestStatus = "-";
        public string _btnFunctionResult = "-";
        public string _btnDownResult = "-";
        public string _btnUpResult = "-";
        public string _ledResult = "-";
        private bool _isFirstChar = true; // รอรับตัวแรกจากสแกนใหม่

        private void SaveTestDataToExcel(string testStatus)
        {
            // 🟢 1. บังคับเช็คว่าเลือก Folder หรือยัง
            if (!EnsureFolderSelected()) return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SaveTestDataToExcel(testStatus)));
                return;
            }

            try
            {
                string serialNumber = txtSerialNumber.Text.Trim();
                string lotNumber = txtLot.Text.Trim();

                if (string.IsNullOrEmpty(serialNumber))
                {
                    MessageBox.Show("กรุณาใส่ Serial Number!", "Warning",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(lotNumber))
                {
                    MessageBox.Show("กรุณาใส่ LOT Number!", "Warning",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 🟢 2. กำหนด Path โดยอิงจาก ResultFolder ที่เลือกเท่านั้น
                string safeFileName = lotNumber.Replace("/", "-");
                string lotFilePath = Path.Combine(ResultFolder!, safeFileName + ".xlsx");

                if (!File.Exists(lotFilePath))
                {
                    string templatePath = Path.Combine(Application.StartupPath, "template.xlsx");
                    if (!File.Exists(templatePath))
                    {
                        MessageBox.Show("ไม่พบไฟล์ template.xlsx!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    Directory.CreateDirectory(ResultFolder!);
                    File.Copy(templatePath, lotFilePath);
                }

                _excelFilePath = lotFilePath;
                txtExcelPath.Text = lotFilePath;

                using (var workbook = new XLWorkbook(_excelFilePath))
                {
                    var sheet = workbook.Worksheet(1);

                    sheet.Cell("P3").Value = lotNumber;
                    sheet.Cell("AA3").Value = DateTime.Now.ToString("yyyy-MM-dd");

                    int[] pageBreakRows = new int[] { 30, 49, 68, 87, 106 };

                    int nextRow = 12;
                    int duplicateRow = -1;
                    bool nextRowFound = false;

                    int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 11;

                    for (int r = 12; r <= Math.Max(lastRow, 12); r++)
                    {
                        if (pageBreakRows.Contains(r)) continue;

                        string existingSN = sheet.Cell(r, 28).GetString().Trim();

                        if (!string.IsNullOrEmpty(existingSN) && existingSN.Equals(serialNumber, StringComparison.OrdinalIgnoreCase))
                        {
                            duplicateRow = r;
                            break;
                        }

                        if (string.IsNullOrEmpty(existingSN) && !nextRowFound)
                        {
                            nextRow = r;
                            nextRowFound = true;
                        }
                    }

                    if (duplicateRow != -1)
                    {
                        nextRow = duplicateRow;
                    }
                    else if (lastRow >= 12 && !string.IsNullOrEmpty(sheet.Cell(lastRow, 28).GetString()))
                    {
                        nextRow = lastRow + 1;
                    }

                    if (pageBreakRows.Contains(nextRow))
                    {
                        nextRow++;
                    }

                    int pageBreaksBefore = pageBreakRows.Count(b => b < nextRow);
                    int no = (nextRow - 11) - pageBreaksBefore;

                    SetCellWithFont(sheet, nextRow, 1, no);
                    SetCellWithFont(sheet, nextRow, 3, _RelayResult);
                    SetCellWithFont(sheet, nextRow, 5, _wlResult);
                    SetCellWithFont(sheet, nextRow, 6, _wl_AL2Result);
                    SetCellWithFont(sheet, nextRow, 8, _hpResult);
                    SetCellWithFont(sheet, nextRow, 9, _hp_AL2Result);
                    SetCellWithFont(sheet, nextRow, 11, _alarm1Result);

                    SetCellWithFont(sheet, nextRow, 12, _HotFan);
                    SetCellWithFont(sheet, nextRow, 13, _CoolFan);
                    SetCellWithFont(sheet, nextRow, 14, _Compressor);
                    SetCellWithFont(sheet, nextRow, 15, _verify200Result);
                    SetCellWithFont(sheet, nextRow, 16, _verify2000Result);
                    SetCellWithFont(sheet, nextRow, 17, _verify8000Result);

                    //SetCellWithFont(sheet, nextRow, 18, _tempSweepResults[1]); // 20°C
                    //SetCellWithFont(sheet, nextRow, 19, _tempSweepResults[2]); // 30°C
                    //SetCellWithFont(sheet, nextRow, 20, _tempSweepResults[3]); // 40°C
                    //SetCellWithFont(sheet, nextRow, 21, _tempSweepResults[4]); // 50°C

                    SetCellWithFont(sheet, nextRow, 18, _verify20Result); // 20°C
                    SetCellWithFont(sheet, nextRow, 19, _verify30Result); // 30°C
                    SetCellWithFont(sheet, nextRow, 20, _verify40Result); // 40°C
                    SetCellWithFont(sheet, nextRow, 21, _verify50Result); // 50°C

                    SetCellWithFont(sheet, nextRow, 22, _btnFunctionResult);
                    SetCellWithFont(sheet, nextRow, 23, _btnDownResult);
                    SetCellWithFont(sheet, nextRow, 24, _btnUpResult);
                    SetCellWithFont(sheet, nextRow, 25, _ledResult);

                    if (testStatus == "PASS")
                    {
                        SetCellWithFont(sheet, nextRow, 26, "OK");   // Z = ปกติ
                        SetCellWithFont(sheet, nextRow, 27, "");     // AA = ไม่ปกติ (เคลียร์ว่าง)
                    }
                    else
                    {
                        SetCellWithFont(sheet, nextRow, 26, "");     // Z = ปกติ (เคลียร์ว่าง)
                        SetCellWithFont(sheet, nextRow, 27, "OK");   // AA = ไม่ปกติ
                    }
                    SetCellWithFont(sheet, nextRow, 28, serialNumber); // AB = S/N


                    int actualCount = 0;
                    int goodCount = 0;
                    int defectCount = 0;
                    int checkRow = 12;

                    while (true)
                    {
                        // ถ้าเจอแถวคั่นหน้า ให้ข้ามไปแถวถัดไป
                        if (pageBreakRows.Contains(checkRow))
                        {
                            checkRow++;
                            continue;
                        }

                        // ถ้าไม่มีข้อมูล S/N (Col 27) และ ลำดับ (Col 1) แสดงว่าหมดแถวที่มีข้อมูลแล้ว
                        if (string.IsNullOrWhiteSpace(sheet.Cell(checkRow, 28).GetString()) &&
                            string.IsNullOrWhiteSpace(sheet.Cell(checkRow, 1).GetString()))
                        {
                            break;
                        }

                        actualCount++;

                        // นับ Good/Defect จากคอลัมน์ Z(26)=ปกติ, AA(27)=ไม่ปกติ
                        string zCheck = sheet.Cell(checkRow, 26).GetString().Trim().ToUpper();
                        string aaCheck = sheet.Cell(checkRow, 27).GetString().Trim().ToUpper();

                        if (zCheck == "OK") goodCount++;
                        else if (aaCheck == "OK") defectCount++;

                        checkRow++;
                    }

                    sheet.Cell("U3").Value = actualCount;

                    // ── เขียนสรุป Q'ty / Good / Defect / % Yield ──
                    double yieldPercent = actualCount > 0 ? Math.Round((double)goodCount / actualCount * 100.0, 2) : 0;

                    sheet.Cell("F124").Value = actualCount;
                    sheet.Cell("H124").Value = goodCount;
                    sheet.Cell("J124").Value = defectCount;
                    sheet.Cell("L124").Value = yieldPercent;
                    sheet.Cell("L124").Style.NumberFormat.Format = "0.00\"%\"";

                    workbook.Save();
                }

                LoadLotHistory();
                LoadExcelToDataGrid(_excelFilePath);
                //_ = PostResultAsync(testStatus); //  post ผลลง Server

                LogToRx($"S/N: {serialNumber} | LOT: {lotNumber} บันทึกเรียบร้อย", Color.Green);

                _lastTestStatus = testStatus;
            }
            catch (IOException)
            {
                MessageBox.Show("ไม่สามารถบันทึกได้ กรุณาปิดไฟล์ Excel ก่อนครับ!",
                                "File Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ไม่สามารถบันทึกข้อมูลลง Excel ได้: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        public void OnForm4TestComplete(string form4Status)
        {
            _isForm4ResultReady = true;

            // ตรวจสอบสถานะว่า BackgroundWorker หลักทำงานจบไปแล้วหรือยัง
            // (เช็คว่าปุ่มกลับมาเป็น "Run" และผู้ใช้ไม่ได้กด Stop กะทันหัน)
            if (btnRun.Text == "Run" && chkTest.Checked == false)
            {

                this.Invoke(new Action(() =>
                {
                    bool btnAllPass = (_btnFunctionResult == "OK" || _btnFunctionResult == "-")
                                   && (_btnDownResult == "OK" || _btnDownResult == "-")
                                   && (_btnUpResult == "OK" || _btnUpResult == "-");

                    string finalExcelStatus = (btnAllPass && form4Status == "PASS") ? "PASS" : "FAIL";
                    SaveTestDataToExcel(finalExcelStatus);
                }));
            }
        }
        private void txtSerialNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _isFirstChar = true;

                btnRun.Focus();
            }
        }
        private async void txtSerialNumber_TextChanged(object sender, EventArgs e)
        {
            _snDebounceTimer.Stop();
            _snDebounceTimer.Start();
        }
        private Task CheckSerialNumberAsync()
        {
            string sn = txtSerialNumber.Text.Trim();
            string lotNumber = txtLot.Text.Trim();

            if (string.IsNullOrWhiteSpace(lotNumber))
            {
                txtSerialNumber.BackColor = Color.LightPink;
                return Task.CompletedTask;
            }

            string safeFileName = lotNumber.Replace("/", "-");
            _excelFilePath = Path.Combine(ResultFolder!, safeFileName + ".xlsx");
            txtExcelPath.Text = _excelFilePath;

            if (IsSerialNumberDuplicate(sn, lotNumber))
            {
                txtSerialNumber.BackColor = Color.LightCoral;
                LogToRx($"⚠️S/N:{sn} ซ้ำในระบบ!", Color.Orange);
            }
            else
            {
                txtSerialNumber.BackColor = Color.LightGreen;
            }

            return Task.CompletedTask;
        }
        private void txtSerialNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (_isFirstChar && e.KeyChar != (char)Keys.Back)
            {
                txtSerialNumber.Clear(); // ลบค่าเดิมทันทีเมื่อรับตัวอักษรแรก
                _isFirstChar = false;
            }
        }
        private void SetCellWithFont(IXLWorksheet sheet, int row, int col, object value)
        {
            var cell = sheet.Cell(row, col);
            cell.Value = XLCellValue.FromObject(value);
            cell.Style.Font.FontName = "Cordia New";
            cell.Style.Font.FontSize = 11;
        }


        //-- ส่วนของการดึงข้อมูลจาก excel มาโชว์ datagrid
        private void picCir_Click(object sender, EventArgs e)
        {
            cmbLot.SelectedIndex = -1;
            cmbLot.Text = "";

            LoadLotHistory();
            LoadExcelToDataGrid();
        }
        private void txtLot_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.SavedLot = txtLot.Text.Trim();
            Properties.Settings.Default.Save();
        }

        private void cmbDisplaySource_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedSource = cmbDisplaySource.Text.Trim();

            if (selectedSource == "EXCEL")
            {
                _excelFilePath = txtExcelPath.Text.Trim();
                LoadExcelToDataGrid();
            }
            else if (selectedSource == "DATABASE")
            {
                if (_lastDatabaseData != null)
                {
                    dataGridView1.DataSource = null;
                    dataGridView1.DataSource = _lastDatabaseData;
                    FormatDataGridView();

                    lblLotCount.Text = $"จำนวนทั้งหมด: {_lastDatabaseData.Count} ชิ้น";
                    lblResultStatus.Text = "✅ แสดงข้อมูลจาก DATABASE ล่าสุด (กด GET อีกครั้งเพื่ออัปเดต)";
                    lblResultStatus.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    dataGridView1.DataSource = null;
                    lblLotCount.Text = "จำนวนทั้งหมด: 0 ชิ้น";
                    lblResultStatus.Text = "⏳ ยังไม่มีข้อมูลในระบบ รอกดปุ่มเพื่อดึงประวัติล่าสุด...";
                    lblResultStatus.ForeColor = System.Drawing.Color.Blue;
                }
            }
            else
            {
                dataGridView1.DataSource = null;
            }
        }
        private void cmbLot_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbLot.SelectedItem == null || string.IsNullOrEmpty(ResultFolder)) return;

            string selectedLot = cmbLot.SelectedItem.ToString() ?? "";

            // 🟡 แก้ไข: บังคับให้อ่านไฟล์จาก ResultFolder (โฟลเดอร์ที่เราเลือก) เท่านั้น
            string filePath = Path.Combine(ResultFolder, selectedLot + ".xlsx");

            if (File.Exists(filePath))
            {
                LoadExcelToDataGrid(filePath);
            }
        }
        private void LoadExcelToDataGrid(string? filePath = null)
        {

            if (!string.IsNullOrEmpty(filePath))
            {
                _historyExcelFilePath = filePath;
            }
            else if (!string.IsNullOrEmpty(cmbLot.Text) && !string.IsNullOrEmpty(ResultFolder))
            {
                string safeFileName = cmbLot.Text.Trim().Replace("/", "-");
                _historyExcelFilePath = Path.Combine(ResultFolder, safeFileName + ".xlsx");
            }

            if (cmbDisplaySource.Text.Trim() != "EXCEL")
            {
                dataGridView1.DataSource = null;
                lblLotCount.Text = "จำนวนทั้งหมด: 0 ชิ้น";
                return;
            }

            if (string.IsNullOrEmpty(_historyExcelFilePath) || !File.Exists(_historyExcelFilePath))
            {
                dataGridView1.DataSource = null;
                return;
            }

            try
            {
                using var workbook = new XLWorkbook(_historyExcelFilePath);
                var sheet = workbook.Worksheet(1);

                string excelLotFromHeader = sheet.Cell("P3").GetString().Trim();
                string excelLotSizeFromHeader = sheet.Cell("U3").GetString().Trim();
                string excelDateFromHeader = sheet.Cell("AA3").GetString().Trim();

                var rows = new List<TestResultExcel>();
                int row = 12;
                int lotCount = 0;
                int passCount = 0;
                int failCount = 0;

                int[] pageBreakRows = new int[] { 30, 49, 68, 87, 106 };

                string selectedLotOnUI = cmbLot.Text.Trim();

                while (true)
                {
                    if (pageBreakRows.Contains(row))
                    {
                        row++;
                        continue;
                    }
                    // เช็คจาก S/N (col 28) แทน No. (col 1) เพราะ No. ถูกพิมพ์ไว้ล่วงหน้าในเทมเพลต
                    if (string.IsNullOrWhiteSpace(sheet.Cell(row, 28).GetString()) &&
                        string.IsNullOrWhiteSpace(sheet.Cell(row, 1).GetString()))
                    {
                        break;
                    }

                    // ถ้า No. มีเลขแต่ S/N ว่าง = แถวที่เทมเพลต pre-print ไว้แต่ยังไม่มีข้อมูลจริง ข้ามไป
                    if (string.IsNullOrWhiteSpace(sheet.Cell(row, 28).GetString()))
                    {
                        row++;
                        continue;
                    }

                    lotCount++;

                    // ── ตรวจสถานะจาก 2 คอลัมน์แยกกัน: Z(26)=ปกติ, AA(27)=ไม่ปกติ ──
                    string zVal = sheet.Cell(row, 26).GetString().Trim().ToUpper();  // ปกติ
                    string aaVal = sheet.Cell(row, 27).GetString().Trim().ToUpper(); // ไม่ปกติ

                    string statusValue;
                    if (zVal == "OK")
                    {
                        statusValue = "PASS";
                        passCount++;
                    }
                    else if (aaVal == "OK")
                    {
                        statusValue = "FAIL";
                        failCount++;
                    }
                    else
                    {
                        statusValue = "";
                    }

                    string currentSn = sheet.Cell(row, 28).GetString().Trim();

                    rows.Add(new TestResultExcel
                    {
                        No = (int)sheet.Cell(row, 1).GetDouble(),          // A: ลำดับ
                        RelayLED = sheet.Cell(row, 3).GetString(),         // B: (ฝากค่าสลับหรือเทียบเคียงกับตัวแปรเก่า)
                        WL = sheet.Cell(row, 5).GetString(),               // C: WL
                        WL_AL2 = sheet.Cell(row, 6).GetString(),           // D: WL/AL2
                        HP = sheet.Cell(row, 8).GetString(),               // E: HP
                        HP_AL2 = sheet.Cell(row, 9).GetString(),           // F: HP/AL2
                        Alarm1 = sheet.Cell(row, 11).GetString(),          // G: Alarm1

                        hotFAN = sheet.Cell(row, 12).GetString(),          // H: Compressor
                        coolFAN = sheet.Cell(row, 13).GetString(),         // H: Compressor
                        Compressor = sheet.Cell(row, 14).GetString(),      // H: Compressor

                        Ohm200 = sheet.Cell(row, 15).GetString(),          // I: จ่าย 200
                        Ohm2000 = sheet.Cell(row, 16).GetString(),         // J: จ่าย 2000
                        Ohm8000 = sheet.Cell(row, 17).GetString(),         // K: จ่าย 8000

                        test20 = sheet.Cell(row, 18).GetString(),          // H: Compressor
                        test30 = sheet.Cell(row, 19).GetString(),          // I: จ่าย 200
                        test40 = sheet.Cell(row, 20).GetString(),          // J: จ่าย 2000
                        test50 = sheet.Cell(row, 21).GetString(),          // K: จ่าย 8000

                        BtnFunction = sheet.Cell(row, 22).GetString(),     // L: ปุ่ม F
                        BtnDown = sheet.Cell(row, 23).GetString(),         // M: ปุ่ม ลด
                        BtnUp = sheet.Cell(row, 24).GetString(),           // N: ปุ่ม เพิ่ม
                        LedCheck = sheet.Cell(row, 25).GetString(),        // O: LED ติดครบ
                        Status = statusValue,                              // T: Status
                        LOT = excelLotFromHeader,                          // ดึงจาก K3 มาใส่ใน Object
                        SerialNumber = sheet.Cell(row, 28).GetString()     // U: S/N
                    });

                    row++;
                }

                dataGridView1.DataSource = null;
                dataGridView1.DataSource = rows;

                lblPassCount.Text = passCount.ToString();
                lblFailCount.Text = failCount.ToString();

                if (string.IsNullOrEmpty(selectedLotOnUI))
                {
                    lblLotCount.Text = $" จำนวนทั้งหมด: {lotCount} ชิ้น";
                }
                else
                {
                    lblLotCount.Text = $" ล็อต {selectedLotOnUI} มีทั้งหมด: {lotCount} ชิ้น";
                }

                FormatDataGridView();
            }
            catch (Exception ex)
            {
                LogToRx($"โหลด DataGrid ล้มเหลว: {ex.Message}", Color.Red);
            }
        }
        private void LoadLotHistory()
        {
            if (string.IsNullOrEmpty(ResultFolder) || !Directory.Exists(ResultFolder)) return;

            try
            {
                string currentSelected = cmbLot.Text.Trim();

                cmbLot.Items.Clear();

                var files = Directory.GetFiles(ResultFolder, "*.xlsx")
                                     .Select(f => Path.GetFileNameWithoutExtension(f))
                                     .Where(name => !name.Equals("template", StringComparison.OrdinalIgnoreCase))
                                     .Select(name => name.Replace("-", "/"))
                                     .Distinct(StringComparer.OrdinalIgnoreCase)
                                     .OrderByDescending(x => x)
                                     .ToList();

                foreach (var displayName in files)
                {
                    cmbLot.Items.Add(displayName);
                }

                string? savedLot = Properties.Settings.Default.SavedLot?.Trim();
                if (!string.IsNullOrEmpty(savedLot))
                {
                    string formattedSavedLot = savedLot.Replace("-", "/");
                    if (!cmbLot.Items.Contains(formattedSavedLot))
                    {
                        cmbLot.Items.Insert(0, formattedSavedLot);
                    }
                }

                if (!string.IsNullOrEmpty(currentSelected) && cmbLot.Items.Contains(currentSelected))
                {
                    cmbLot.Text = currentSelected;
                }
                else
                {
                    cmbLot.SelectedIndex = -1;
                    cmbLot.Text = "";
                }
            }
            catch (Exception ex)
            {
                LogToRx($"โหลดประวัติ LOT ล้มเหลว: {ex.Message}", Color.Red);
            }
        }
        private void ResetTestResults()
        {
            // ล้างค่าผลการตรวจเช็คทางไฟฟ้า/สถานะ ให้เป็นค่าว่างหรือเครื่องหมายขีดก่อนเริ่มเทสตัวใหม่
            _RelayResult = "-";
            _wlResult = "-";
            _wl_AL2Result = "-";
            _hpResult = "-";
            _hp_AL2Result = "-";
            _alarm1Result = "-";
            _Compressor = "-";

            _btnFunctionResult = "-";
            _btnDownResult = "-";
            _btnUpResult = "-";

            _ledResult = "-";

            // ส่วนค่าโอห์ม ถ้าไม่ได้เช็คต่อ อาจจะเซ็ตเป็น 0 หรือค่าว่างไว้ก่อน
            _verify200Result = "-";
            _verify2000Result = "-";
            _verify8000Result = "-";

            _register0FinalResult = "-";

            for (int i = 0; i < _tempSweepResults.Length; i++)
                _tempSweepResults[i] = "-";
        }
        private void dataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                string status = r.Cells["Status"].Value?.ToString() ?? "";
                r.DefaultCellStyle.BackColor = status.ToUpper() switch
                {
                    "PASS" => Color.FromArgb(220, 255, 220),
                    "FAIL" => Color.FromArgb(255, 220, 220),
                    _ => Color.White
                };

            }
        }

        private void FormatDataGridView()
        {
            if (dataGridView1.Columns.Count == 0) return;

            dataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        }



        //-- ส่วนของการดึงข้อมูลโดยใช้ date จาก cmb
        private void OnDateFilterChanged()
        {
            ClearLotSelection();

            if (!TryGetSelectedDate(out DateTime selectedDate))
                return;

            FindAndLoadLotByDate(selectedDate);
        }
        private void ClearLotSelection()
        {
            cmbLot.SelectedIndex = -1;
            cmbLot.Text = "";

            _historyExcelFilePath = null;
            dataGridView1.DataSource = null;
            lblLotCount.Text = " จำนวนทั้งหมด: 0 ชิ้น";
            lblPassCount.Text = "0";
            lblFailCount.Text = "0";
        }
        private bool TryGetSelectedDate(out DateTime result)
        {
            result = default;

            if (cmbDay.SelectedIndex == -1 || cmbMonth.SelectedIndex == -1 || cmbYear.SelectedIndex == -1)
                return false;

            if (!int.TryParse(cmbDay.Text.Trim(), out int day)) return false;
            if (!int.TryParse(cmbMonth.Text.Trim(), out int month)) return false; // ปี 2026 เป็น ค.ศ. ตรงๆ ไม่ต้องแปลง
            if (!int.TryParse(cmbYear.Text.Trim(), out int year)) return false;

            try
            {
                result = new DateTime(year, month, day);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                LogToRx($"วันที่ {day}/{month}/{year} ไม่ถูกต้อง", Color.Red);
                return false;
            }
        }
        private void FindAndLoadLotByDate(DateTime targetDate)
        {
            if (string.IsNullOrEmpty(ResultFolder) || !Directory.Exists(ResultFolder))
            {
                LogToRx("กรุณาเลือกโฟลเดอร์เก็บผลลัพธ์ก่อน", Color.Red);
                return;
            }

            cmbDisplaySource.Text = "EXCEL";

            var matchedLots = new List<(string DisplayName, string FilePath)>();

            var files = Directory.GetFiles(ResultFolder, "*.xlsx")   // เปลี่ยนจาก resultFolder ตัวแปรเดิม
                                  .Where(f => !Path.GetFileName(f).StartsWith("~$"))
                                  .Where(f => !Path.GetFileNameWithoutExtension(f)
                                                  .Equals("template", StringComparison.OrdinalIgnoreCase));

            foreach (var filePath in files)
            {
                try
                {
                    using var workbook = new XLWorkbook(filePath);
                    var sheet = workbook.Worksheet(1);

                    if (TryGetCellDate(sheet.Cell("Z3"), out DateTime lotDate) && lotDate.Date == targetDate.Date)
                    {
                        string displayName = Path.GetFileNameWithoutExtension(filePath).Replace("-", "/");
                        matchedLots.Add((displayName, filePath));
                    }
                }
                catch (Exception ex)
                {
                    LogToRx($"อ่านไฟล์ {Path.GetFileName(filePath)} ไม่ได้: {ex.Message}", Color.Red);
                }
            }

            if (matchedLots.Count == 0)
            {
                LogToRx($"ไม่พบ LOT ที่มีวันที่ {targetDate:yyyy-MM-dd}", Color.Orange);
            }
            else if (matchedLots.Count == 1)
            {
                var (displayName, filePath) = matchedLots[0];
                _historyExcelFilePath = filePath;
                cmbLot.Text = displayName;
                LoadExcelToDataGrid();

                LogToRx($"พบ LOT {displayName} ตรงกับวันที่ {targetDate:yyyy-MM-dd} โหลดให้แล้ว", Color.Green);
            }
            else
            {
                cmbLot.Items.Clear();
                foreach (var (displayName, _) in matchedLots)
                    cmbLot.Items.Add(displayName);

                cmbLot.SelectedIndex = -1;
                cmbLot.Text = "";
                cmbLot.DroppedDown = true;
                LogToRx($"พบ {matchedLots.Count} LOT ที่ตรงกับวันที่ {targetDate:yyyy-MM-dd} กรุณาเลือก", Color.Orange);
            }
        }
        private bool TryGetCellDate(IXLCell cell, out DateTime date)
        {
            if (cell.DataType == XLDataType.DateTime)
            {
                date = cell.GetDateTime();
                return true;
            }

            string text = cell.GetString().Trim();
            return DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
        private void cmbDay_SelectedIndexChanged_2(object? sender, EventArgs e) { OnDateFilterChanged(); }
        private void cmbMonth_SelectedIndexChanged_2(object? sender, EventArgs e) { OnDateFilterChanged(); }
        private void cmbYear_SelectedIndexChanged_2(object? sender, EventArgs e) { OnDateFilterChanged(); }



        //-- ส่วนของการส่ง API สำหรัยส่งข้อมูลไปเก็บไว้ที่ DATABASE
        private async Task PostResultAsync(string testStatus)
        {
            string url = "http://192.168.109.170:5180/api/qc/inspections/by-serial";

            string serialNumber = txtSerialNumber.Text.Trim();
            string lotNumber = txtLot.Text.Trim();
            string currentDateTimeIso = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            if (string.IsNullOrEmpty(serialNumber)) return;

            double.TryParse(_verify200Result, out double val200);
            double.TryParse(_verify2000Result, out double val2000);
            double.TryParse(_verify8000Result, out double val8000);

            string resQc001 = (_RelayResult == "OK") ? "PASS" : "FAIL";
            string resQc002 = (_wlResult == "OK") ? "PASS" : "FAIL";
            string resQc003 = (_wl_AL2Result == "OK") ? "PASS" : "FAIL";
            string resQc004 = (_hpResult == "OK") ? "PASS" : "FAIL";
            string resQc005 = (_hp_AL2Result == "OK") ? "PASS" : "FAIL";
            string resQc006 = (_alarm1Result == "OK") ? "PASS" : "FAIL";
            string resQc007 = (_Compressor == "OK") ? "PASS" : "FAIL";
            string resQc008 = (_verify200Result != "-") ? "PASS" : "FAIL";
            string resQc009 = (_verify8000Result != "-") ? "PASS" : "FAIL";
            string resQc010 = (_verify2000Result != "-") ? "PASS" : "FAIL";

            string overallResult = testStatus;

            string jsonBody = $@"{{
                                      ""lot_number"": ""{lotNumber}"",
                                      ""serial_number"": ""{serialNumber}"",
                                      ""station_name"": ""QC-STATION-01"",
                                      ""equipment_code"": [""DMM-001""],
                                      ""remark"": ""Postman save draft QC by item code"",
                                      ""items"": [
                                        {{ ""item_code"": ""QC001"", ""measured_text"": ""{_RelayResult}"",  ""result"": ""{resQc001}"" }},
                                        {{ ""item_code"": ""QC002"", ""measured_text"": ""{_wlResult}"",     ""result"": ""{resQc002}"" }},
                                        {{ ""item_code"": ""QC003"", ""measured_text"": ""{_wl_AL2Result}"", ""result"": ""{resQc003}"" }},
                                        {{ ""item_code"": ""QC004"", ""measured_text"": ""{_hpResult}"",     ""result"": ""{resQc004}"" }},
                                        {{ ""item_code"": ""QC005"", ""measured_text"": ""{_hp_AL2Result}"", ""result"": ""{resQc005}"" }},
                                        {{ ""item_code"": ""QC006"", ""measured_text"": ""{_alarm1Result}"", ""result"": ""{resQc006}"" }},
                                        {{ ""item_code"": ""QC007"", ""measured_text"": ""{_Compressor}"",   ""result"": ""{resQc007}"" }},
                                        {{ ""item_code"": ""QC008"", ""measured_value"": {val200:F1},        ""result"": ""{resQc008}"" }},
                                        {{ ""item_code"": ""QC009"", ""measured_value"": {val8000:F1},       ""result"": ""{resQc009}"" }},
                                        {{ ""item_code"": ""QC010"", ""measured_value"": {val2000:F1},       ""result"": ""{resQc010}"" }}
                                      ]
                                    }}";
            try
            {
                lblResultStatus.Text = "⏳ กำลังส่งผลบันทึกไปฐานข้อมูล...";
                lblResultStatus.ForeColor = Color.Orange;

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

                // ✅ ตรงนี้แหละที่ต้องใส่ API Key
                //client.DefaultRequestHeaders.Clear();
                //client.DefaultRequestHeaders.Add("X-API-Key", "ABCDE");

                ///RxBox.AppendText($"\r\n[DEBUG JSON] {jsonBody}\r\n");

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseText = await response.Content.ReadAsStringAsync();

                    //RxBox.AppendText($"\r\n[API SUCCESS] {responseText}\r\n");

                    lblResultStatus.Text = $"✅ อัปโหลด S/N: {serialNumber} เรียบร้อย!";
                    lblResultStatus.ForeColor = Color.Green;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    lblResultStatus.Text = $"❌ Error: {response.StatusCode}";
                    lblResultStatus.ForeColor = Color.Red;
                    //RxBox.AppendText($"\r\n[API ERROR] {errorResponse}\r\n");
                }
            }
            catch (Exception ex)
            {
                lblResultStatus.Text = "⚠ ไม่สามารถเชื่อมต่อเน็ตเวิร์กได้";
                lblResultStatus.ForeColor = Color.DarkRed;
                RxBox.AppendText($"\r\n[NETWORK EXCEPTION] {ex.Message}\r\n");
            }
        }
        private async void btnPost_Click(object sender, EventArgs e)
        {
            string serialNumber = txtSerialNumber.Text.Trim();
            if (string.IsNullOrEmpty(serialNumber))
            {
                MessageBox.Show("กรุณาสแกน Serial Number ก่อนส่งข้อมูลขึ้นระบบ!", "Warning",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            await PostResultAsync(_lastTestStatus);
        }

        private List<TestResultRow>? _lastDatabaseData = null;
        private async void btnGet_Click(object sender, EventArgs e)
        {
            MassagerBox.Clear();

            if (cmbDisplaySource.Text.Trim() != "DATABASE")
            {
                dataGridView1.DataSource = null;
                lblResultStatus.Text = "⚠ กรุณาเลือกตัวเลือกเป็น DATABASE ก่อนทำการดึงประวัติ!";
                lblResultStatus.ForeColor = System.Drawing.Color.Orange;
                return;
            }

            string url = "https://jsonplaceholder.typicode.com/posts";

            try
            {
                lblResultStatus.Text = "⏳ กำลังดึงข้อมูลมาแสดงในตาราง...";
                lblResultStatus.ForeColor = System.Drawing.Color.Orange;

                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseText = await response.Content.ReadAsStringAsync();
                    MassagerBox.Text = responseText;

                    // แกะกล่องข้อมูลแล้วเอาไปฝากไว้ในตัวแปรกลางที่เราสร้างไว้ด้านบน
                    _lastDatabaseData = System.Text.Json.JsonSerializer.Deserialize<List<TestResultRow>>(responseText);

                    // แสดงผลลงตาราง
                    dataGridView1.DataSource = null;
                    dataGridView1.DataSource = _lastDatabaseData;

                    FormatDataGridView();

                    lblResultStatus.Text = $"✅ ดึงประวัติสำเร็จ! แสดงข้อมูลทั้งหมด {_lastDatabaseData?.Count} รายการ";
                    lblResultStatus.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    lblResultStatus.Text = $"❌ ดึงข้อมูลล้มเหลว: {response.StatusCode}";
                    lblResultStatus.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch
            {
                lblResultStatus.Text = "⚠ เชื่อมต่อเซิร์ฟเวอร์ไม่ได้!";
                lblResultStatus.ForeColor = System.Drawing.Color.DarkRed;
            }
        }



        //-- แสดง LED และ process time
        private void DrawLedBulb(PictureBox pb, bool isOn, Color onColor)
        {
            Bitmap bmp = new Bitmap(pb.Width, pb.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(pb.BackColor == Color.Transparent ?
                        SystemColors.Control : pb.BackColor);

                int w = pb.Width;
                int h = pb.Height;

                Rectangle bulbRect = new Rectangle(4, 2, w - 8, w - 8);

                Color bulbColor = isOn ? onColor : Color.FromArgb(80, 80, 80);
                Color centerColor = isOn ? Color.White : Color.FromArgb(130, 130, 130);

                if (isOn)
                {
                    Color glowColor = Color.FromArgb(40, onColor);
                    using (var glowBrush = new SolidBrush(glowColor))
                    {
                        Rectangle glowRect = new Rectangle(0, 0, w, w);
                        g.FillEllipse(glowBrush, glowRect);
                    }
                }

                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddEllipse(bulbRect);
                    using (var brush = new System.Drawing.Drawing2D.PathGradientBrush(path))
                    {
                        brush.CenterColor = centerColor;
                        brush.SurroundColors = new Color[] { bulbColor };
                        brush.CenterPoint = new PointF(
                            bulbRect.X + bulbRect.Width * 0.35f,
                            bulbRect.Y + bulbRect.Height * 0.3f);
                        g.FillEllipse(new SolidBrush(bulbColor), bulbRect);
                        g.FillEllipse(brush, bulbRect);
                    }
                }

                // วาดขอบหลอด
                using (var pen = new Pen(isOn ?
                       Color.FromArgb(180, onColor) : Color.FromArgb(60, 60, 60), 1.5f))
                {
                    g.DrawEllipse(pen, bulbRect);
                }

                // --- วาดขั้วหลอด (สีเทา) ---
                int baseY = bulbRect.Bottom - 4;
                int baseW = 20;
                int baseX = (w - baseW) / 2;

                // ขั้วบน
                g.FillRectangle(Brushes.Gray, baseX, baseY, baseW, 6);
                // ขั้วล่าง (แคบกว่า)
                g.FillRectangle(Brushes.DimGray, baseX + 4, baseY + 6, baseW - 8, 5);
            }

            // คืน Bitmap เก่าก่อน set ใหม่ เพื่อป้องกัน memory leak
            pb.Image?.Dispose();
            pb.Image = bmp;
        }
        private enum StepState { Waiting, Running, Pass, Fail }
        private PictureBox[] _stepLeds = Array.Empty<PictureBox>();
        private void InitStepLeds()
        {
            _stepLeds = new PictureBox[]
            {
                picCal,picStep1, picStep2, picStep3,
                picStep4, picStep5, picStep6, picStep7,picStep8,picStep9
            };

            foreach (var pb in _stepLeds)
                DrawStepLed(pb, StepState.Waiting);
        }
        private void DrawStepLed(PictureBox pb, StepState state)
        {
            if (pb.InvokeRequired)
            {
                pb.Invoke(new Action(() => DrawStepLed(pb, state)));
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
            DrawStepLed(_stepLeds[index], state);
        }
        private void ResetAllSteps()
        {
            for (int i = 0; i < _stepLeds.Length; i++)
                SetStep(i, StepState.Waiting);
        }



        //-- for Debuck Calibration 
        private void btnCalSave_Click(object sender, EventArgs e)
        {
            // ถ้ากำลัง Calibrate อยู่ แล้วกดซ้ำ -> ให้สั่งหยุด
            if (btnCalSave.Text == "กำลังทดสอบ")
            {
                var confirmStop = MessageBox.Show("คุณต้องการหยุดกระบวนการ Calibration ใช่หรือไม่?", "Stop", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirmStop == DialogResult.Yes)
                {
                    if (backgroundWorkerOhm.IsBusy) backgroundWorkerOhm.CancelAsync();
                    LogToRx("--- สั่งหยุดการทำงานโดยผู้ใช้ ---", Color.Red);
                    btnCalSave.Text = "ทดสอบ";
                    btnCalSave.Enabled = true;
                    btnRun.Enabled = true;
                }
                return;
            }

            if (!_serialPort.IsOpen || !_ohmPort.IsOpen)
            {
                MessageBox.Show("กรุณาเชื่อมต่อทั้งพอร์ต Modbus และพอร์ต Ohm", "Warning");
                return;
            }

            var confirm = MessageBox.Show("เริ่มกระบวนการ Calibration และบันทึก Excel ใช่หรือไม่?", "Confirm", MessageBoxButtons.YesNo);
            if (confirm == DialogResult.No) return;

            _currentMode = WorkerMode.CalAndSaveExcel;

            ResetAllSteps();
            _sequenceStep = 0;
            _calDone = false;
            Array.Clear(finalAdcArray, 0, finalAdcArray.Length);
            RxBox.Clear();

            List<string> sequenceValues = new List<string>
        {
        "100", "150", "300", "600", "1000", "1200", "1600",
        "1800", "2000", "2600", "4000", "6000", "10000", "30000"
        };

            btnCalSave.Text = "Calibrating...";
            btnCalSave.Enabled = true; // เปิดให้กดซ้ำได้
            btnRun.Enabled = false;   // บล็อกปุ่ม Run เดิมไว้ป้องกันการกดซ้อน
            progressBar1.Value = 0;

            // สั่งให้ BackgroundWorker ตัวเดิมเป็นคนรัน (ค่า ADC จะอ่านได้ปกติเหมือนโหมดเดิม 100%)
            backgroundWorkerOhm.RunWorkerAsync(sequenceValues);
        }
        private void SaveAdcToExcel(double[] adcData)
        {
            try
            {
                string folderPath = Path.Combine(Application.StartupPath, "Cal_Reports");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                // 1. กำหนดตำแหน่งไฟล์เทมเพลต และไฟล์ที่จะเซฟจริง
                string templatePath = Path.Combine(Application.StartupPath, "CalReport_Template.xlsx");
                string fileName = "CalReport_Accumulated.xlsx";
                string filePath = Path.Combine(folderPath, fileName);

                ClosedXML.Excel.XLWorkbook workbook;
                ClosedXML.Excel.IXLWorksheet worksheet;

                // 2. ถ้ายังไม่มีไฟล์สะสม ให้ก๊อปปี้ไฟล์เทมเพลตมาเริ่มใช้ แต่ถ้ามีไฟล์เดิมอยู่แล้วให้เปิดไฟล์เดิมขึ้นมาทำต่อ
                if (!File.Exists(filePath))
                {
                    if (!File.Exists(templatePath))
                    {
                        MessageBox.Show("ไม่พบไฟล์เทมเพลต 'CalReport_Template.xlsx' ในโฟลเดอร์โปรแกรม กรุณาตรวจสอบ", "Error");
                        return;
                    }
                    File.Copy(templatePath, filePath); // ก๊อปปี้เอาหน้าตาเทมเพลตมาใช้
                }

                // เปิดไฟล์ที่ต้องการเขียนข้อมูล
                workbook = new ClosedXML.Excel.XLWorkbook(filePath);
                worksheet = workbook.Worksheet(1); // เลือกแผ่นงานแรกในเทมเพลต

                // 3. ตรวจสอบว่าในไฟล์มีการบันทึกค่า ADC ไปแล้วกี่ครั้ง (สแกนแถวที่ 5 คอลัมน์เลขคู่เพื่อตรวจดูค่าเดิม)
                int currentRunCount = 0;

                // ตรวจบล็อกชุดที่ 1 (ครั้งที่ 1-5) แถวที่ 5 คอลัมน์ B, E, H, K, N
                for (int b = 0; b < 5; b++)
                {
                    int checkCol = 2 + (b * 3); // คอลัมน์ ADC (B=2, E=5, H=8, K=11, N=14)
                    if (worksheet.Cell(5, checkCol).Value.ToString() != "") currentRunCount++;
                }
                // ตรวจบล็อกชุดที่ 2 (ครั้งที่ 6-10) แถวที่ 22 คอลัมน์ B, E, H, K, N
                for (int b = 0; b < 5; b++)
                {
                    int checkCol = 2 + (b * 3);
                    if (worksheet.Cell(22, checkCol).Value.ToString() != "") currentRunCount++;
                }
                // ตรวจบล็อกชุดที่ 3 (ครั้งที่ 11-15) แถวที่ 39 คอลัมน์ B, E, H, K, N
                for (int b = 0; b < 5; b++)
                {
                    int checkCol = 2 + (b * 3);
                    if (worksheet.Cell(39, checkCol).Value.ToString() != "") currentRunCount++;
                }

                for (int b = 0; b < 5; b++)
                {
                    int checkCol = 2 + (b * 3);
                    if (worksheet.Cell(56, checkCol).Value.ToString() != "") currentRunCount++;
                }

                int nextRunNumber = currentRunCount + 1; // ครั้งที่ที่จะเขียนค่า ADC ล่าสุดลงไป (เช่น ครั้งที่ 1, 2, 3...)

                if (nextRunNumber > 20)
                {
                    MessageBox.Show("ตารางเทมเพลตเต็มแล้ว (บันทึกครบ 20 ครั้งแล้ว)!", "Warning");
                    workbook.Dispose();
                    return;
                }

                // 4. คำนวณพิกัดคอลัมน์ที่จะหยอดค่า ADC (ตามดีไซน์ 5 บล็อกต่อ 1 แถวใหญ่)
                int blockIndex = nextRunNumber - 1; // ทำเป็น Index เริ่มจาก 0
                int rowGroup = blockIndex / 5;      // อยู่แถวชุดไหน (0=ชุดแรก, 1=ชุดสอง, 2=ชุดสาม)
                int colGroup = blockIndex % 5;      // บล็อกย่อยอันที่เท่าไหร่ในแถวนั้น (0-4)

                // หาแถวเริ่มต้นสำหรับหยอดตัวเลข (อิงตามรูปตารางของคุณ)
                // บล็อกชุดแรกเริ่มแถว 5, ชุดสองเริ่มแถว 22, ชุดสามเริ่มแถว 39
                int startRow = 5 + (rowGroup * 17);

                // หาคอลัมน์ของ ADC Value ที่เราจะเอาเลขไปหยอดลงไป (ครั้งที่ 1 = B (2), ครั้งที่ 2 = E (5), ครั้งที่ 3 = H (8)...)
                int targetAdcCol = 2 + (colGroup * 3);

                // 5. ลูปเขียนค่า ADC ลงช่องในเทมเพลตเฉยๆ
                for (int i = 0; i < adcData.Length; i++)
                {
                    int currentWriteRow = startRow + i;

                    if (i < adcData.Length)
                    {
                        // แปลงทศนิยม (double) เป็นเลขจำนวนเต็ม (int) ก่อนแปลงฐาน
                        int adcIntValue = Convert.ToInt32(adcData[i]);

                        // จัดรูปแบบข้อความให้อยู่ในบล็อกเดียวกัน เช่น "0x00AF (175)"
                        string hexAndDecValue = $"{adcIntValue.ToString("X4")} ({adcIntValue})";

                        // หยอดข้อความที่ผสมแล้วลงช่อง ADC ใน Excel
                        worksheet.Cell(currentWriteRow, targetAdcCol).Value = hexAndDecValue;
                    }
                    else
                    {
                        worksheet.Cell(currentWriteRow, targetAdcCol).Value = "0000 (0)";
                    }
                }

                // 6. บันทึกงานและปิดไฟล์
                workbook.Save();
                workbook.Dispose();

                LogToRx($"[EXCEL] หยอดค่า ADC ครั้งที่ {nextRunNumber} ลงในเทมเพลตเรียบร้อย", Color.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ไม่สามารถบันทึกข้อมูลลงเทมเพลตได้: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void btnSet_Click(object sender, EventArgs e)
        {
            // ปิดปุ่มชั่วคราวเพื่อป้องกันผู้ใช้กดซ้ำระหว่างทำงาน
            btnSet.Enabled = false;

            try
            {
                // ใช้วิธีรันงานเบื้องหลัง หรือ await Delay เพื่อไม่ให้UI ค้าง
                await Task.Run(() => ExecuteWriteSingleRegister(1, 1, 200));
                await Task.Delay(500);

                await Task.Run(() => ExecuteWriteSingleRegister(1, 14, 1));
                await Task.Delay(500);

                await Task.Run(() => ExecuteWriteSingleRegister(1, 9, 3));
                await Task.Delay(500);

                await Task.Run(() => ExecuteWriteSingleRegister(1, 10, 3));
                await Task.Delay(500);

                await Task.Run(() => ExecuteWriteSingleRegister(1, 11, 1));
                await Task.Delay(500);

                await Task.Run(() => ExecuteWriteSingleRegister(1, 7, 5));
                await Task.Delay(500);

                await Task.Run(() => ExecuteWriteSingleRegister(1, 12, 1));
                await Task.Delay(500);

                MessageBox.Show("ตั้งค่าเรียบร้อยแล้ว!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}");
            }
            finally
            {
                // เปิดให้กดปุ่มได้อีกครั้ง
                btnSet.Enabled = true;
            }

        }

        //check 5temp impostend
        private string[] _tempSweepResults = new string[5] { "-", "-", "-", "-", "-" }; // index 0=10°C, 1=20°C, 2=30°C, 3=40°C, 4=50°C
        private static readonly (int Ohm, int TargetTemp)[] TempSweepPoints = new (int, int)[]
        {
            (3684, 10),(2489, 20),(1703, 30),(1158, 40),(821,  50)
        };

        public void btnTestCoolFan_Click(object sender, EventArgs e)
        {
            if (ID3_reg5IsOn)
                LogToRx("[PASS] Cool Fan ทำงานอยู่ (สถานะ: ON)", Color.Green);
            else
                LogToRx("[FAIL] Cool Fan ไม่ทำงาน (สถานะ: OFF)", Color.Red);
        }

        private async Task CheckCompressorAndHotFanManual(int timeoutSeconds = 10)
        {
            try
            {
                LogToRx("--- เริ่มทดสอบ Compressor & HotFan ---", Color.Blue);

                // 1. Set SV = 20.0
                ExecuteWriteSingleRegister(1, 1, 200);
                await Task.Delay(500);

                // 2. สั่ง Ohm จ่าย 2000
                SendOhm(2000);
                await Task.Delay(500);

                // 3. เปิด WL และ HP ให้ติดทั้งคู่
                ExecuteWriteSingleRegister(2, 0, 1); // WL ON
                await Task.Delay(300);
                ExecuteWriteSingleRegister(2, 1, 1); // HP ON
                await Task.Delay(500);

                // 4. รอเช็คสถานะ Compressor (reg3) และ HotFan (reg4) ~5 วินาที
                bool isCompressorOn = false;
                bool isHotFanOn = false;

                for (int i = 0; i < timeoutSeconds; i++)
                {
                    await Task.Delay(1000);

                    if (ID3_reg3IsOn) isCompressorOn = true;
                    if (ID3_reg4IsOn) isHotFanOn = true;

                    if (isCompressorOn && isHotFanOn) break;
                }

                if (isCompressorOn)
                    LogToRx("[PASS] Compressor ทำงาน", Color.Green);
                else
                    LogToRx("[FAIL] Compressor ไม่ทำงาน!", Color.Red);

                if (isHotFanOn)
                    LogToRx("[PASS] HotFan ทำงาน", Color.Green);
                else
                    LogToRx("[FAIL] HotFan ไม่ทำงาน!", Color.Red);

                await Task.Delay(5000);

                // 5. ปิดไฟ WL และ HP
                ExecuteWriteSingleRegister(2, 0, 0); // WL OFF
                await Task.Delay(300);
                ExecuteWriteSingleRegister(2, 1, 0); // HP OFF

                LogToRx("--- ปิดไฟ WL/HP เรียบร้อย ---", Color.Blue);
            }
            catch (Exception ex)
            {
                LogToRx($"Error: {ex.Message}", Color.Red);
            }
        }

        public async void btnTestCompressorHotFan_Click(object sender, EventArgs e)
            => await CheckCompressorAndHotFanManual(10);

        private bool _isTestingCompressorHotFan = false;
        public async void btnTestCompHotFan_Click(object sender, EventArgs e)
        {
            if (_isTestingCompressorHotFan) return; // กันกดซ้ำ

            _isTestingCompressorHotFan = true;

            try
            {
                await CheckCompressorAndHotFanManual(10);
            }
            finally
            {
                _isTestingCompressorHotFan = false;
            }
        }
    }
}