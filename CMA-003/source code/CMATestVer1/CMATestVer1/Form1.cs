using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using DocumentFormat.OpenXml.Office2013.Excel;
using DocumentFormat.OpenXml.VariantTypes;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace CMATestVer1
{
    public partial class Form1 : Form
    {
        #region 01 - ตัวแปรกลางและสถานะของระบบ

        //-- Serail Port
        public SerialPort _serialPort;
        public SerialPort _ohmPort;
        public SerialPort _DisPort;


        private readonly object _lock = new object();
        private List<byte> rxBuffer = new List<byte>();
        private List<byte> ohmRxBuffer = new List<byte>();


        private ushort lastRequestedStart = 0; // ตัวแปรเก็บค่าเริ่มต้นล่าสุด
        private volatile bool _calAckReceived = false;

        // เก็บค่า register ดิบล่าสุด แยกตาม (slaveId, address) ไม่ผ่าน UI
        private readonly Dictionary<(byte slaveId, int addr), short> _lastRegisterValues
            = new Dictionary<(byte, int), short>();
        private readonly object _regLock = new object();
        private readonly Dictionary<int, TextBox> _regisTextBoxCache = new Dictionary<int, TextBox>();

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


        //-- Timer
        private System.Diagnostics.Stopwatch _runStopwatch = new System.Diagnostics.Stopwatch();
        private DateTime? _inspectionStartTime;
        private DateTime? _inspectionEndTime;
        private System.Windows.Forms.Timer _snDebounceTimer; // Debounce timer สำหรับ Serial Number input
        private bool _isDuplicateSN = false;
        private int _duplicateSNRow = -1;

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
        private enum Form4TriggerMode { AutoFlow, StandaloneWithSN, StandaloneNoSN }
        private Form4TriggerMode _form4TriggerMode = Form4TriggerMode.AutoFlow;


        //-- สำหรับการเขียน
        private string _originalText = string.Empty;
        private readonly HashSet<string> _dirtyRegis = new HashSet<string>();// รีจิสเตอร์ที่เปลี่ยนแปลง
        private bool _isWriting = false; // ธงเช็คสถานะ: true = กำลังมีคำสั่งเขียนวิ่งอยู่
        private DateTime _isWritingStartTime = DateTime.MinValue; //เวลาที่เริ่มเขียน ใช้กัน _isWriting ค้าง

        private bool _forceDisplay120 = false; // เพื่อควบคุมการส่ง 120
        private string _register0FinalResult = "-";

        //สำหรับการตรวจจับสายหลุด
        private DateTime _lastID3FrameTime = DateTime.MinValue;
        private readonly object _id3TimeLock = new object();

        //ตรวจจับสายหลุดฝั่ง _serialPort (บัส RS-485 หลัก ID1/2/3)
        private DateTime _lastAnyFrameTime = DateTime.MinValue;
        private readonly object _anyFrameLock = new object();
        private const int BusTimeoutMs = 6000;

        private DateTime _lastID1FrameTime = DateTime.MinValue;
        private readonly object _id1TimeLock = new object();
        private const int MainboardTimeoutMs = 6000;
        private const int InitialRs485CheckTimeoutMs = 2000;
        private volatile bool _initialConnectionCheckInProgress = false;

        //ตรวจจับสายหลุดฝั่ง _DisPort (ผ่าน callback จาก Form4)
        private DateTime _lastDisPortFrameTime = DateTime.MinValue;
        private readonly object _disPortTimeLock = new object();
        private const int DisPortTimeoutMs = 8000; // นานกว่าหน่อยเพราะ Form4 อาจ poll ช้ากว่า
        private const bool EnableDisPortResponseTimeoutCheck = true; // เปิดตรวจสาย Display และการตอบกลับผ่าน RS-485
        private volatile bool _suppressDisPortTimeoutCheck = false;
        private DateTime _resumeDisPortTimeoutCheckAt = DateTime.MinValue;

        #endregion

        #region 02 - การสร้างหน้าจอและกำหนดค่าเริ่มต้น

        public Form1(string token)
        {
            InitializeComponent();
            InitializeHiddenServerTestButton();
            InitializeDetailsFeature();
            InitializeTokenFeature();
            InitializeDescriptionFeature();
            InitializeOperatorNameFeature();
            InitializeServerAddressFeature();
            InitializeLoginFeature();
            InitializeHelpFeature();
            InitializeBarcodeScannerFeature();
            _token = token;
            InitAutoScale();

            BigMessageBox.MainOwner = this;
            _logBuffer.CreateControl();

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
            CacheRegisTextBoxes();
            txtSerialNumber.Focus();

            // ใน Constructor เพิ่ม
            _snDebounceTimer = new System.Windows.Forms.Timer();
            _snDebounceTimer.Interval = 400; // รอ 400ms หลังหยุดพิมพ์ (เหมือน DEF-03N)
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
            ResetDeviceStatusLabels();
            //DrawLedBulb(picRelay, false, Color.Red);
        }

        #endregion

        #region 03 - การเชื่อมต่อ COM Port และสถานะอุปกรณ์

        //-- ส่วนของการกดเชื่อมต่อ  
        private async void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    if (PortBox.SelectedItem == null || portDis.SelectedItem == null || SerialboxOhm.SelectedItem == null)
                    {
                        BigMessageBox.Show("กรุณาเลือก Port ให้ครบถ้วน!", "Warning",
                                    MessageBoxIcon.Warning, fontSize: 14f);
                        return;
                    }

                    _initialConnectionCheckInProgress = true;
                    btnConnect.Enabled = false;
                    btnConnect.Text = "Connecting...";

                    // ต้องได้รับข้อมูลใหม่หลังจากกด Connect เท่านั้น จึงจะถือว่าเชื่อมต่อสำเร็จ
                    lock (_anyFrameLock) { _lastAnyFrameTime = DateTime.MinValue; }
                    lock (_id1TimeLock) { _lastID1FrameTime = DateTime.MinValue; }
                    lock (_disPortTimeLock) { _lastDisPortFrameTime = DateTime.MinValue; }

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
                    _ohmPort.WriteTimeout = 500;   // ★ เพิ่ม
                    _ohmPort.ReadTimeout = 500;    // ★ เพิ่ม
                    _ohmPort.Open();

                    _DisPort.PortName = portDis.SelectedItem.ToString()!;
                    _DisPort.BaudRate = 19200;
                    _DisPort.Parity = Parity.None;
                    _DisPort.DataBits = 8;
                    _DisPort.StopBits = StopBits.One;
                    _DisPort.WriteTimeout = 500;   // ★ เพิ่ม
                    _DisPort.ReadTimeout = 500;    // ★ เพิ่ม

                    //subscribe เพื่อ track timeout ตั้งแต่ connect (แยกจาก handler ของ Form4)
                    _DisPort.DataReceived -= DisPort_TimeoutTracker; // กันซ้ำ เผื่อ reconnect
                    _DisPort.DataReceived += DisPort_TimeoutTracker;
                    _DisPort.Open();

                    timer1.Interval = 200;
                    timer1.Start();
                    _fastPollCount = 0;

                    var initialCheck = await WaitForInitialRs485DataAsync();
                    if (!initialCheck.MainboardReceived || !initialCheck.DisplayReceived)
                    {
                        string missingDevice = !initialCheck.MainboardReceived && !initialCheck.DisplayReceived
                            ? "Mainboard และ Display"
                            : !initialCheck.MainboardReceived ? "Mainboard (ID1)" : "Display";

                        _initialConnectionCheckInProgress = false;
                        HandleDisconnect(
                            $"ไม่พบข้อมูล RS-485 จาก {missingDevice} ภายในเวลาที่กำหนด\n" +
                            "กรุณาตรวจสอบสาย RS-485 และอุปกรณ์");
                        return;
                    }

                    _initialConnectionCheckInProgress = false;
                    UpdateConnectionStatusUI(true);

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

                    UpdateConnectionStatusUI(false);

                    PortBox.Enabled = true;
                    portDis.Enabled = true;
                    SerialboxOhm.Enabled = true;

                    txtSerialNumber.Focus();
                }
            }
            catch (Exception ex)
            {
                _initialConnectionCheckInProgress = false;
                timer1.Stop();

                // หากเกิด Error ให้พยายามปิดพอร์ตที่อาจค้างอยู่
                if (_serialPort.IsOpen) _serialPort.Close();
                if (_ohmPort.IsOpen) _ohmPort.Close();
                if (_DisPort.IsOpen) _DisPort.Close();

                UpdateConnectionStatusUI(false);
                PortBox.Enabled = true;
                portDis.Enabled = true;
                SerialboxOhm.Enabled = true;

                BigMessageBox.Show($"Error during connection: {ex.Message}", "Error",
                    MessageBoxIcon.Error, fontSize: 14f);
            }
            finally
            {
                if (!IsDisposed)
                    btnConnect.Enabled = true;
            }
        }
        private async Task<(bool MainboardReceived, bool DisplayReceived)> WaitForInitialRs485DataAsync()
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(InitialRs485CheckTimeoutMs);

            while (DateTime.UtcNow < deadline)
            {
                bool mainboardReceived;
                bool displayReceived;

                lock (_id1TimeLock)
                {
                    mainboardReceived = _lastID1FrameTime != DateTime.MinValue;
                }

                lock (_disPortTimeLock)
                {
                    displayReceived = _lastDisPortFrameTime != DateTime.MinValue;
                }

                if (mainboardReceived && displayReceived)
                    return (true, true);

                await Task.Delay(50);
            }

            bool finalMainboardReceived;
            bool finalDisplayReceived;

            lock (_id1TimeLock)
            {
                finalMainboardReceived = _lastID1FrameTime != DateTime.MinValue;
            }

            lock (_disPortTimeLock)
            {
                finalDisplayReceived = _lastDisPortFrameTime != DateTime.MinValue;
            }

            return (finalMainboardReceived, finalDisplayReceived);
        }
        private void bntRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                if (backgroundWorkerOhm.IsBusy)
                    backgroundWorkerOhm.CancelAsync();

                if (_serialPort != null && _serialPort.IsOpen)
                {
                    HandleDisconnect("UserDisconnect");
                }
                else
                {
                    timer1.Stop();
                    ResetRuntimeStateAndUI();
                    UpdateConnectionStatusUI(false);
                }

                // ── Reset data source + โหลดข้อมูลใหม่ ───────────────
                _lastDatabaseData = null;

                cmbLot.SelectedIndex = -1;
                cmbLot.Text = "";


                Get_compoart_list();
                LoadLotHistory();
                LoadExcelToDataGrid();

                txtSerialNumber.Focus();

                BigMessageBox.Show("All settings have been reset!", "Refresh",
                    MessageBoxIcon.Information, fontSize: 14f);
            }
            catch (Exception ex)
            {
                BigMessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxIcon.Error, fontSize: 14f);
            }
        }
        private void ResetRuntimeStateAndUI(bool clearRxLog = true)
        {
            // ── Reset runtime state (Cal/Test) ──────────────────────
            _sequenceStep = 0;
            _testStepResumed = 0;
            _calDone = false;
            Array.Clear(finalAdcArray, 0, finalAdcArray.Length);
            lock (_tempAdcBuffer) { _tempAdcBuffer.Clear(); }
            lock (_regLock) { _lastRegisterValues.Clear(); }

            // ── Reset volatile device flags ──────────────────────────
            ID2_reg0IsOn = false;
            ID2_reg1IsOn = false;
            ID3_reg3IsOn = false;
            ID3_reg4IsOn = false;
            ID3_reg5IsOn = false;
            _alarm1IsOn = false;
            _alarm2IsOn = false;
            Led_Relay = false;

            // ── Reset ผลการ Test ─────────────────────────────────────
            ResetTestResults();

            // ── Reset UI indicators (LED) ────────────────────────────
            DrawLedBulb(picWL, false, Color.Red);
            DrawLedBulb(picHp, false, Color.Red);
            DrawLedBulb(picCom, false, Color.Red);
            DrawLedBulb(picHotFan, false, Color.Red);
            DrawLedBulb(picCoolFan, false, Color.Red);
            ResetDeviceStatusLabels();

            ResetAllSteps();

            _runStopwatch.Reset();
            lblElapsedTime.Text = "00:00";
            UpdateProgressDisplay(0);
            lblRegis0.Text = "0 °C"; // ✅ เคลียร์ตัวเลขอุณหภูมิที่โชว์ค้าง

            // ── ล้าง Regis TextBox ทั้งหมด ────────────────────────────
            for (int i = 0; i <= 26; i++)
            {
                Control[] found = this.Controls.Find("Regis" + i, true);
                if (found.Length > 0)
                {
                    found[0].Text = "";
                    found[0].ForeColor = Color.Black;
                }
            }

            if (clearRxLog)
            {
                ClearRxLog();
            }

            //chkCal.Checked = false;
            //chkTest.Checked = false;

            txtSerialNumber.ReadOnly = false;
            txtSerialNumber.BackColor = SystemColors.Window;
        }
        private void UpdateConnectionStatusUI(bool connected)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateConnectionStatusUI(connected)));
                return;
            }

            lblConnStatusValue.Text = connected ? "●ON" : "●OFF";
            lblConnStatusValue.ForeColor = connected ? Color.FromArgb(0, 140, 0) : Color.Red;

            btnConnect.Text = connected ? "🔗Disconnect" : "🔗Connect";
            //btnConnect.ForeColor = connected ? Color.Red : Color.Black;
            //btnConnect.BackColor = connected ? Color.LightGreen : Color.LightCoral;

            UpdateOpenForm4ButtonState();   // ★ เพิ่ม
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

            PortBox.SelectedIndex = -1;
            SerialboxOhm.SelectedIndex = -1;
            portDis.SelectedIndex = -1;

            PortBox.Text = "";
            SerialboxOhm.Text = "";
            portDis.Text = "";

            if (PortBox.Items.Count == 0)
            {
                BigMessageBox.Show("ไม่พบ COM Port!", "Warning", MessageBoxIcon.Warning, fontSize: 14f);
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
            _initialConnectionCheckInProgress = false;

            if (backgroundWorkerOhm.IsBusy)
            {
                backgroundWorkerOhm.CancelAsync();
            }

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

            try { if (_DisPort != null) _DisPort.DataReceived -= DisPort_TimeoutTracker; } catch { }

            lock (_lock)
            {
                try { if (_serialPort?.IsOpen == true) _serialPort.Close(); } catch { }
            }

            try { if (_ohmPort?.IsOpen == true) _ohmPort.Close(); } catch { }
            try { if (_DisPort?.IsOpen == true) _DisPort.Close(); } catch { }

            lock (_lock)
            {
                rxBuffer.Clear();
            }

            // ไม่ล้าง RxBox เมื่อเกิดพอร์ตหลุด เพื่อให้ดูประวัติย้อนหลังได้
            ResetRuntimeStateAndUI(clearRxLog: false);

            btnRun.Text = "▶Run";
            btnRun.Enabled = true;
            bntStop.Enabled = false;
            btnRun.BackColor = Color.Gold;

            btnConnect.Text = "🔗Connect";
            btnConnect.ResetBackColor();
            btnConnect.UseVisualStyleBackColor = true;

            lblConnStatusValue.Text = "●OFF";
            lblConnStatusValue.ForeColor = Color.Red;

            PortBox.Enabled = true;
            portDis.Enabled = true;
            SerialboxOhm.Enabled = true;

            if (reason != "UserDisconnect")
            {
                LogToRx($"[PORT DISCONNECTED] พอร์ตการเชื่อมต่อหลุดขณะทดสอบ: {reason}", Color.Red);
                BigMessageBox.Show($"{reason}", "Connection Lost",
                                    MessageBoxIcon.Error, fontSize: 14f);
            }
            else
            {
                LogToRx("[DISCONNECT] ผู้ใช้งานตัดการเชื่อมต่อพอร์ต", Color.DarkOrange);
            }

            _isWriting = false;
        }
        private void btnClr_Click(object sender, EventArgs e)
        {
            RxBox.Clear();
            _logBuffer.Clear();
        }



        #endregion

        #region 04 - การรับและแปลผล Modbus RTU

        //-- ส่วนของการส่งข้อมูล
        private double _currentPv;
        private double _currentSV;
        private double _currentALH;
        private double _currentALL;

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;
            if (!IsSafeToInvoke()) return;

            try
            {
                int count = _serialPort.BytesToRead;
                if (count == 0) return;

                byte[] buffer = new byte[count];
                _serialPort.Read(buffer, 0, count);

                lock (_lock) { rxBuffer.AddRange(buffer); }

                ParseModbusRTU();
            }
            catch (ObjectDisposedException) { }   // กันเวลาบัง blanket catch หลุด edge case
            catch { }
        }
        private void DisPort_TimeoutTracker(object sender, SerialDataReceivedEventArgs e)
        {
            lock (_disPortTimeLock) { _lastDisPortFrameTime = DateTime.Now; }
        }
        private void ParseModbusRTU()
        {
            lock (_lock)
            {
                while (rxBuffer.Count >= 5)
                {
                    // ★ จุดที่ 1: เช็ค slave ID ก่อนเชื่อ function code
                    byte slaveId = rxBuffer[0];
                    if (slaveId != 1 && slaveId != 2 && slaveId != 3)
                    {
                        rxBuffer.RemoveAt(0);
                        continue;
                    }

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

                    // ★ จุดที่ 2: sanity cap กัน frameLength เพี้ยนแล้วค้างรอ
                    const int MaxFrameLength = 65; // ID1 อ่าน 27 registers = byteCount 54 + 5 = 59, เผื่อ margin
                    if (frameLength > MaxFrameLength)
                    {
                        rxBuffer.RemoveAt(0);
                        continue;
                    }

                    if (rxBuffer.Count < frameLength) break;

                    byte[] frame = rxBuffer.GetRange(0, frameLength).ToArray();
                    rxBuffer.RemoveRange(0, frameLength);

                    ProcessFrame(frame);
                }

                // ★ จุดที่ 3: safety net กัน buffer โตค้างไม่สิ้นสุด
                const int MaxBufferSize = 200;
                if (rxBuffer.Count > MaxBufferSize)
                {
                    rxBuffer.Clear();
                }
            }
        }
        private void ProcessFrame(byte[] frame)
        {
            lock (_anyFrameLock) { _lastAnyFrameTime = DateTime.Now; }   // ยังคงไว้ เผื่อใช้เช็ค "บัสรวม" ต่อ

            byte slaveID = frame[0];

            if (slaveID == 1)
            {
                lock (_id1TimeLock) { _lastID1FrameTime = DateTime.Now; }   // ★ เพิ่ม: จับเวลาเฉพาะ Mainboard
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
            if (frame.Length >= 5 && frame[1] == 0x03)
            {
                int byteCount = frame[2];
                int numRegs = byteCount / 2;

                // ★ อัปเดตค่า status แบบ synchronous ทันที ไม่ต้องรอคิว UI thread
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
                            break;
                        case 4:
                            ID3_reg4IsOn = (value != 0);
                            break;
                        case 5:
                            ID3_reg5IsOn = (value != 0);
                            break;
                    }
                }

                // ★ ประทับเวลาหลังจากค่าจริงอัปเดตครบแล้วเท่านั้น
                lock (_id3TimeLock) { _lastID3FrameTime = DateTime.Now; }

                // ส่วนวาด LED ที่ต้องแตะ UI control ค่อยส่งเข้า UI thread แยกต่างหาก
                SafeBeginInvoke(() =>
                {
                    DrawLedBulb(picCoolFan, ID3_reg3IsOn, Color.Red);
                    DrawLedBulb(picHotFan, ID3_reg4IsOn, Color.Red);
                    DrawLedBulb(picCom, ID3_reg5IsOn, Color.Red);
                    UpdateDeviceStatusLabel(status_CoolFan, ID3_reg3IsOn);
                    UpdateDeviceStatusLabel(status_HotFan, ID3_reg4IsOn);
                    UpdateDeviceStatusLabel(status_Compressor, ID3_reg5IsOn);
                });
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

                SafeBeginInvoke(() =>
                {
                    DrawLedBulb(picWL, ID2_reg0IsOn, Color.Red);
                    DrawLedBulb(picHp, ID2_reg1IsOn, Color.Red);
                    UpdateDeviceStatusLabel(status_WL, ID2_reg0IsOn);
                    UpdateDeviceStatusLabel(status_HP, ID2_reg1IsOn);
                });
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

                SafeBeginInvoke(() =>
                {
                    for (int i = 0; i < numRegs; i++)
                    {
                        int actualRegister = lastRequestedStart + i;
                        short value = (short)(frame[3 + (i * 2)] << 8 | frame[4 + (i * 2)]);

                        lock (_regLock) { _lastRegisterValues[((byte)1, actualRegister)] = value; }

                        if (_regisTextBoxCache.TryGetValue(actualRegister, out TextBox? txt) && txt != null)
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
                });
            }
            else if (frame[1] == 0x06)
            {
                int addr = (frame[2] << 8) | frame[3];
                int val = (frame[4] << 8) | frame[5];

                lock (_regLock) { _lastRegisterValues[((byte)1, addr)] = (short)val; }
                _isWriting = false;

                SafeBeginInvoke(() =>
                {
                    if (_regisTextBoxCache.TryGetValue(addr, out TextBox? txt) && txt != null)
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
                });
            }
            else if (frame[1] == 0x55)
            {
                _calAckReceived = true;
                //LogToRx("Calibration Success!\r\n");
            }
        }
        private void SendFrame(byte[] data)
        {
            bool portClosed = false;

            lock (_lock)
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    portClosed = true;
                }
                else
                {
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
                        portClosed = true;
                        SafeBeginInvoke(() => HandleDisconnect("Write Error: " + ex.Message));
                        return;
                    }
                }
            }

            if (portClosed)
            {
                SafeBeginInvoke(() => HandleDisconnect("Port is closed."));
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




        #endregion

        #region 05 - Timer, Polling และการเขียน Register

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

                // ── 1. อัปเดตนาฬิกา ─────────────────────────────
                if (_runStopwatch.IsRunning)
                {
                    var t = _runStopwatch.Elapsed;
                    lblElapsedTime.Text = $" {t.Minutes:D2}:{t.Seconds:D2}";
                }

                // ── 2. นับถอยหลังเพื่อเปิด Form4 ────────────────
                if (_autoOpenCountdown > 0)
                {
                    _autoOpenCountdown--;

                    if (_autoOpenCountdown == 0 &&
                        _DisPort != null &&
                        _DisPort.IsOpen)
                    {
                        this.BeginInvoke(new Action(OpenForm4Deferred));
                    }
                }

                // ── 3. ตรวจว่าพอร์ตยังเปิดอยู่หรือไม่ ─────────────

                // พอร์ต RS-485 บัสหลัก
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    HandleDisconnect("Port is not open.");
                    return;
                }

                // พอร์ตเครื่องวัด Ohm
                if (_ohmPort == null || !_ohmPort.IsOpen)
                {
                    HandleDisconnect("Port Ohm Source is not open.");
                    return;
                }

                // พอร์ต RS-485 ของ Display
                if (_DisPort == null || !_DisPort.IsOpen)
                {
                    HandleDisconnect("Port Display is not open.");
                    return;
                }

                // ── 4. ตรวจ timeout ของ RS-485 บัสหลัก ──────────
                // บัสหลักยังตรวจตามปกติ แม้ว่า Form4 กำลังเปิดอยู่

                DateTime lastID1Frame;

                lock (_id1TimeLock)
                {
                    lastID1Frame = _lastID1FrameTime;
                }

                if (lastID1Frame != DateTime.MinValue &&   // กันกรณียังไม่เคยได้รับเฟรม ID1 เลยตั้งแต่ connect
                    (DateTime.Now - lastID1Frame).TotalMilliseconds > MainboardTimeoutMs)
                {
                    HandleDisconnect(
                        "No response from Mainboard (ID1)\n" +
                        "(บอร์ด CMA-003 ไม่ตอบสนอง กรุณาตรวจสอบสาย Power และสายสัญญาณว่าเชื่อมต่ออยู่หรือไม่\")");

                    return;
                }

                // ── 5. ตรวจช่วงพักของ Display ───────────────────
                // เมื่อ Form4 ปิด ระบบจะกำหนดเวลาให้พักต่ออีก 30 วินาที
                // เมื่อครบ 30 วินาที จึงกลับมาตรวจ timeout ของ Display

                if (_suppressDisPortTimeoutCheck &&
                    _resumeDisPortTimeoutCheckAt != DateTime.MinValue &&
                    DateTime.Now >= _resumeDisPortTimeoutCheckAt)
                {
                    // เริ่มจับเวลา timeout ใหม่จากตอนนี้
                    // ป้องกันการแจ้งสายหลุดทันทีหลังหมดช่วงพัก
                    lock (_disPortTimeLock)
                    {
                        _lastDisPortFrameTime = DateTime.Now;
                    }

                    _resumeDisPortTimeoutCheckAt = DateTime.MinValue;
                    _suppressDisPortTimeoutCheck = false;
                }

                // ── 6. ตรวจ timeout ของ Display ─────────────────
                // จะไม่เข้าตรงนี้ขณะที่ Form4 เปิด
                // และไม่เข้าตรงนี้ในช่วง 30 วินาทีหลัง Form4 ปิด

                if (EnableDisPortResponseTimeoutCheck &&
                    !_suppressDisPortTimeoutCheck &&
                    !_initialConnectionCheckInProgress)
                {
                    DateTime lastDisFrame;

                    lock (_disPortTimeLock)
                    {
                        lastDisFrame = _lastDisPortFrameTime;
                    }

                    if ((DateTime.Now - lastDisFrame).TotalMilliseconds >
                        DisPortTimeoutMs)
                    {
                        HandleDisconnect(
                            "No response from Display port\n" +
                            "(สาย RS-485 ของ Display หลุดหรืออุปกรณ์ไม่ตอบสนอง)");

                        return;
                    }
                }

                // ── 7. ส่งคำสั่งอ่าน Modbus ตามปกติ ─────────────
                ExecuteReadCycle();
            }
            catch (Exception ex)
            {
                HandleDisconnect(ex.Message);
            }
            finally
            {
                _timerBusy = false;
                UpdateOpenForm4ButtonState();
            }
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
        private void CommonRegis_Leave(object sender, EventArgs e) { }





        #endregion

        #region 06 - ลำดับ Run, Stop, Calibration และ Test

        //-- ส่วนของการสั่งให้ทำงาน Run, Stop
        private enum WorkerMode { Calibrate, Test, CalAndTest, CalAndSaveExcel }
        private WorkerMode _currentMode;
        private int _testStepResumed = 0;
        private bool _calDone = false;
        private const int CalibrationFinalizeWorkUnits = 1;
        private const int TestWorkUnitCount = 13;
        private readonly object _workProgressLock = new object();
        private int _calibrationPointWorkUnits = 14;
        private int _completedWorkUnits = 0;
        private int _totalWorkUnits = 1;

        private void ConfigureWorkProgress(int calibrationPointCount)
        {
            _calibrationPointWorkUnits = calibrationPointCount;

            int completedTestUnits = Math.Clamp(
                _testStepResumed - 1,
                0,
                TestWorkUnitCount);

            lock (_workProgressLock)
            {
                switch (_currentMode)
                {
                    case WorkerMode.Calibrate:
                    case WorkerMode.CalAndSaveExcel:
                        _totalWorkUnits = calibrationPointCount + CalibrationFinalizeWorkUnits;
                        _completedWorkUnits = Math.Clamp(
                            _sequenceStep,
                            0,
                            calibrationPointCount);
                        break;

                    case WorkerMode.Test:
                        _totalWorkUnits = TestWorkUnitCount;
                        _completedWorkUnits = completedTestUnits;
                        break;

                    case WorkerMode.CalAndTest:
                        _totalWorkUnits = calibrationPointCount
                            + CalibrationFinalizeWorkUnits
                            + TestWorkUnitCount;
                        _completedWorkUnits = _calDone
                            ? calibrationPointCount + CalibrationFinalizeWorkUnits + completedTestUnits
                            : Math.Clamp(_sequenceStep, 0, calibrationPointCount);
                        break;
                }
            }

            UpdateProgressDisplay(CalculateWorkProgressPercentage());
        }

        private int CalculateWorkProgressPercentage()
        {
            lock (_workProgressLock)
            {
                if (_totalWorkUnits <= 0) return 0;

                return Math.Clamp(
                    (int)Math.Round(_completedWorkUnits * 100.0 / _totalWorkUnits),
                    0,
                    100);
            }
        }

        private void CompleteWorkUnit()
        {
            int percentage;

            lock (_workProgressLock)
            {
                if (_completedWorkUnits < _totalWorkUnits)
                    _completedWorkUnits++;

                percentage = _totalWorkUnits > 0
                    ? Math.Clamp(
                        (int)Math.Round(_completedWorkUnits * 100.0 / _totalWorkUnits),
                        0,
                        100)
                    : 0;
            }

            backgroundWorkerOhm.ReportProgress(percentage);
        }

        private void ResetWorkProgressForRetry(bool skipInitialTestSetup)
        {
            lock (_workProgressLock)
            {
                _completedWorkUnits = 0;
                _totalWorkUnits = _calibrationPointWorkUnits
                    + CalibrationFinalizeWorkUnits
                    + TestWorkUnitCount
                    - (skipInitialTestSetup ? 1 : 0);
            }

            backgroundWorkerOhm.ReportProgress(0);
        }

        private void CompleteWorkProgress()
        {
            lock (_workProgressLock)
            {
                _completedWorkUnits = _totalWorkUnits;
            }

            UpdateProgressDisplay(100);
        }

        private void UpdateProgressDisplay(int percentage)
        {
            int safePercentage = Math.Clamp(percentage, 0, 100);

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateProgressDisplay(safePercentage)));
                return;
            }

            progressBar1.Value = safePercentage;
            lbl_percent.Text = $"{safePercentage}%";
        }

        private async void btnRun_Click(object sender, EventArgs e)
        {
            if (backgroundWorkerOhm.IsBusy || !btnRun.Enabled) return;

            txtSerialNumber.ReadOnly = true; // ล็อคระหว่าง Run

            if (!_serialPort.IsOpen || !_ohmPort.IsOpen)
            {
                BigMessageBox.Show("กรุณาเชื่อมต่อพอร์ตให้ครบ", "Warning", MessageBoxIcon.Warning, fontSize: 14f);
                txtSerialNumber.ReadOnly = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(txt_name.Text))
            {
                BigMessageBox.Show("กรุณากรอกชื่อผู้ทดสอบก่อนเริ่มการทดสอบ!", "Warning", MessageBoxIcon.Warning, fontSize: 14f);
                txtSerialNumber.ReadOnly = false;
                txt_name.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtLot.Text))
            {
                BigMessageBox.Show("กรุณากรอก LOT ก่อนเริ่มการทดสอบ!", "Warning", MessageBoxIcon.Warning, fontSize: 14f);
                txtSerialNumber.ReadOnly = false;
                txtLot.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtECN.Text))
            {
                BigMessageBox.Show("กรุณากรอก ECN ก่อนเริ่มการทดสอบ!", "Warning", MessageBoxIcon.Warning, fontSize: 14f);
                txtSerialNumber.ReadOnly = false;
                txtECN.Focus();
                return;
            }

            bool isTestMode = chkTest.Checked;

            if (isTestMode && string.IsNullOrWhiteSpace(txtSerialNumber.Text))
            {
                BigMessageBox.Show("กรุณาใส่ Serial Number ก่อนกด Run!", "Warning", MessageBoxIcon.Warning, fontSize: 14f);
                txtSerialNumber.ReadOnly = false;
                txtSerialNumber.Focus();
                return;
            }

            // ตรวจสอบ S/N ซ้ำก่อนเริ่มทดสอบ (เหมือน DEF-03N)
            if (isTestMode)
            {
                string currentSN = txtSerialNumber.Text.Trim();
                string currentLot = txtLot.Text.Trim();
                int dupRow = -1;
                bool isDup = _isDuplicateSN || IsSerialNumberDuplicate(currentSN, currentLot, out dupRow);
                if (dupRow == -1 && _duplicateSNRow != -1) dupRow = _duplicateSNRow;

                if (isDup)
                {
                    _isDuplicateSN = true;
                    _duplicateSNRow = dupRow;
                    txtSerialNumber.ForeColor = Color.White;
                    txtSerialNumber.BackColor = Color.Firebrick;

                    DialogResult answer = MessageBox.Show(
                        $"S/N: {currentSN} มีอยู่ในรายงานแล้ว\n" +
                        $"ตำแหน่ง: AB{dupRow}\n\n" +
                        "ต้องการทดสอบซ้ำและเขียนผลทับข้อมูลเดิมหรือไม่?",
                        "พบ Serial Number ซ้ำ",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (answer != DialogResult.Yes)
                    {
                        txtSerialNumber.ReadOnly = false;
                        txtSerialNumber.Focus();
                        txtSerialNumber.SelectAll();
                        return;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(_firmwareValue) ||
                string.IsNullOrWhiteSpace(_displayFirmwareValue) ||
                string.IsNullOrWhiteSpace(_revisionValue))
            {
                BigMessageBox.Show("กรุณากรอกข้อมูล Firmware และ Revision ในเมนูรายละเอียดก่อนเริ่มการทดสอบ!", "Warning", MessageBoxIcon.Warning, fontSize: 14f);
                txtSerialNumber.ReadOnly = false;
                BtnDetails_Click(null, EventArgs.Empty);
                return;
            }

            if (!chkCal.Checked && !chkTest.Checked)
            {
                BigMessageBox.Show("กรุณาเลือกอย่างน้อย 1 กระบวนการ (Cal/Test)", "Warning", MessageBoxIcon.Warning, fontSize: 14f);
                txtSerialNumber.ReadOnly = false;
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

            // 1. เปลี่ยนการเช็กเป็น != DialogResult.Yes เพื่อป้องกันการกดปิดกากบาท (X) หรือ Cancel หลุดเข้าไปทำงาน
            var confirm = BigMessageBox.Show($"เริ่ม {modeText} ใช่หรือไม่?", "Confirm", MessageBoxIcon.Question, MessageBoxButtons.YesNo, fontSize: 14f);
            if (confirm != DialogResult.Yes)
            {
                txtSerialNumber.ReadOnly = false;
                return;
            }

            txtSerialNumber.ForeColor = SystemColors.WindowText;
            txtSerialNumber.BackColor = SystemColors.Window;

            btnRun.Text = "▶Running...";
            btnRun.Enabled = false;

            _autoOpenCountdown = isTestMode ? 8 : 0;
            _runStopwatch.Restart();

            bool isCalibrateMode = (_currentMode == WorkerMode.Calibrate || _currentMode == WorkerMode.CalAndTest);
            bool hasCalPending = isCalibrateMode && _sequenceStep > 0 && !_calDone;
            bool hasTestPending = isTestMode && _testStepResumed > 0 && _testStepResumed < 8;
            bool resumedExistingTest = false;

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

                var resume = BigMessageBox.Show(
                    $"มีงานค้างอยู่:\n{detail}\n" +
                    $"กด Yes = ทำต่อจากที่หยุด\n" +
                    $"กด No  = เริ่มใหม่ทั้งหมด",
                    "Resume หรือ Restart?",
                    MessageBoxIcon.Question, MessageBoxButtons.YesNo, fontSize: 14f);

                if (resume == DialogResult.Yes)
                {
                    resumedExistingTest = hasTestPending;
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
                    ClearRxLog(); // ล้าง Log เก่าออกเมื่อเลือกเริ่มใหม่
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
                ClearRxLog();
            }

            if (isTestMode && (!resumedExistingTest || !_inspectionStartTime.HasValue))
            {
                _inspectionStartTime = DateTime.Now;
                _inspectionEndTime = null;
            }

            ConfigureWorkProgress(sequenceValues.Count);
            await Task.Delay(500);

            bntStop.Enabled = true;
            btnRun.BackColor = Color.Orange;

            // 2. ลบคำสั่ง ClearRxLog(); บรรทัดนี้ออก เพราะจะไปลบข้อความ "ทำต่อจากที่หยุด..." ทิ้งทันที

            _isForm4ResultReady = false;
            _form4TestAlreadyDone = false;
            _form4ManuallyClosed = false;
            _pendingForm4ResultAfterRun = false;
            backgroundWorkerOhm.RunWorkerAsync(sequenceValues);
        }
        private void bntStop_Click(object sender, EventArgs e)
        {
            if (!backgroundWorkerOhm.IsBusy) return;
            var confirm = BigMessageBox.Show("ต้องการหยุดกระบวนการใช่หรือไม่?", "Confirm",
                MessageBoxIcon.Question, MessageBoxButtons.YesNo, fontSize: 14f);

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
        private void DoCalibrate(DoWorkEventArgs e)
        {

            if (_sequenceStep == 0)
            {
                LogToRx("--- เริ่มกระบวนการ Calibrate ---", Color.Black);
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

                    var adcResult = CollectDataToArray(ohmValue);

                    if (backgroundWorkerOhm.CancellationPending) { e.Cancel = true; return; }

                    if (adcResult != AdcReadResult.Ok)
                    {
                        string faultMsg = adcResult switch
                        {
                            AdcReadResult.NoData => $"ไม่ได้รับข้อมูล ADC จากบอร์ดที่จุด {ohmValue} Ohm\nกรุณาตรวจสอบการเชื่อมต่อบอร์ด",
                            AdcReadResult.Shorted => $"ตรวจพบ ADC = 0 ที่จุด {ohmValue} Ohm\nOhm Source อาจช็อต กรุณาตรวจสอบ",
                            AdcReadResult.Open => $"ตรวจพบ ADC = FFFF ที่จุด {ohmValue} Ohm\nสาย Ohm Source อาจหลวมหรือหลุด กรุณาตรวจสอบ",
                            _ => "เกิดข้อผิดพลาดที่ไม่ทราบสาเหตุระหว่าง Calibrate"
                        };

                        LogToRx($"[STOP] {faultMsg}", Color.Red);
                        SafeBeginInvoke(() => BigMessageBox.Show(faultMsg, "Calibration หยุดทำงาน", MessageBoxIcon.Error, MessageBoxButtons.OK, fontSize: 14f));

                        e.Cancel = true;
                        return;
                    }
                }

                _sequenceStep = idx + 1;
                CompleteWorkUnit();

                if (backgroundWorkerOhm.CancellationPending) { e.Cancel = true; return; }
            }

            byte[] calFrame = PrepareCalibrationFrame();

            const int maxCalAckRetry = 3;
            bool gotAck = false;

            for (int attempt = 0; attempt < maxCalAckRetry; attempt++)
            {
                if (attempt > 0)
                {
                    //LogToRx($"[RETRY] ยังไม่ได้รับการตอบกลับ Calibration กำลังส่งซ้ำ (ครั้งที่ {attempt + 1}/{maxCalAckRetry})", Color.Orange);
                }

                _calAckReceived = false;
                SendFrame(calFrame);

                for (int i = 0; i < 30; i++)
                {
                    if (_calAckReceived) { gotAck = true; break; }
                    if (!CancellableSleep(100)) { e.Cancel = true; return; }
                }

                if (gotAck)
                {
                    //LogToRx($"[OK] ได้รับการตอบกลับ Calibration แล้ว (ครั้งที่ {attempt + 1}/{maxCalAckRetry})", Color.Green);
                    break;
                }

                // progressive backoff ก่อน retry รอบถัดไป (ยกเว้นรอบสุดท้าย)
                if (attempt < maxCalAckRetry - 1)
                {
                    if (!CancellableSleep(500 * (attempt + 1))) { e.Cancel = true; return; }
                }
            }

            if (!gotAck)
            {
                LogToRx($"[FAIL] ไม่ได้รับการตอบกลับจากบอร์ดหลังส่ง Calibration (ลองแล้ว {maxCalAckRetry} ครั้ง)", Color.Red);
                ReportStepFailure(e);
                return;
            }

            CompleteWorkUnit();
            SetStep(0, StepState.Pass);
            _calDone = true;
            _sequenceStep = 0;
            e.Result = "SUCCESS";
        }
        private void DoTest(DoWorkEventArgs e)
        {
            LogToRx("--- เริ่มกระบวนการ Test ---");
            bool ShouldSkip(int step) => step < _testStepResumed;

            if (!ShouldSkip(1))
            {
                LogToRx(" set ค่า SV, D1, D2, D3");

                if (!WriteRegister(1, 1, 300)) { ReportStepFailure(e); return; } //SV   
                if (!WriteRegister(1, 9, 0)) { ReportStepFailure(e); return; }   //D1
                if (!WriteRegister(1, 10, 0)) { ReportStepFailure(e); return; }  //D2

                if (!WriteRegister(1, 11, 0)) { ReportStepFailure(e); return; }  //D3
                if (!WriteRegister(1, 14, 1)) { ReportStepFailure(e); return; }  //ALF
                if (!WriteRegister(1, 12, 1)) { ReportStepFailure(e); return; }  //CLA

                if (!CancellableSleep(1000)) { e.Cancel = true; return; }

                _testStepResumed = 2;
                CompleteWorkUnit();
            }

            //─────────────── check temp ───────────────────────────────────
            if (!ShouldSkip(2))
            {
                SetStep(1, StepState.Running);

                if (!VerifyOhmValueSync(200, 99.0)) { _ohmCheckFailed = true; ReportStepFailure(e); return; }
                if (!CancellableSleep(100)) { e.Cancel = true; return; }
                _testStepResumed = 3;
                CompleteWorkUnit();
            }

            if (!ShouldSkip(3))
            {
                if (!VerifyOhmValueSync(818, 50.0)) { _ohmCheckFailed = true; ReportStepFailure(e); return; }
                if (!CancellableSleep(100)) { e.Cancel = true; return; }
                _testStepResumed = 4;
                CompleteWorkUnit();
            }

            if (!ShouldSkip(4))
            {
                if (!VerifyOhmValueSync(1150, 40.0)) { _ohmCheckFailed = true; ReportStepFailure(e); return; }
                if (!CancellableSleep(100)) { e.Cancel = true; return; }
                _testStepResumed = 5;
                CompleteWorkUnit();
            }

            if (!ShouldSkip(5))
            {
                if (!VerifyOhmValueSync(1653, 30.0)) { _ohmCheckFailed = true; ReportStepFailure(e); return; }
                if (!CancellableSleep(100)) { e.Cancel = true; return; }
                _testStepResumed = 6;
                CompleteWorkUnit();
            }

            if (!ShouldSkip(6))
            {
                if (!VerifyOhmValueSync(2000, 25.6)) { _ohmCheckFailed = true; ReportStepFailure(e); return; }
                if (!CancellableSleep(1000)) { e.Cancel = true; return; }

                if (!CheckRegisterStatus_ALL(1, 20, true, "LED Relay")) { ReportStepFailure(e); return; }
                if (!CancellableSleep(1000)) { e.Cancel = true; return; }

                _testStepResumed = 7;
                CompleteWorkUnit();
            }

            if (!ShouldSkip(7))
            {
                if (!VerifyOhmValueSync(2435, 20.0)) { _ohmCheckFailed = true; ReportStepFailure(e); return; }
                _testStepResumed = 8;
                CompleteWorkUnit();
            }

            if (!ShouldSkip(8))
            {
                if (!VerifyOhmValueSync(8000, -6.4)) { _ohmCheckFailed = true; ReportStepFailure(e); return; }

                SetStep(1, StepState.Pass);
                if (!CancellableSleep(500)) { e.Cancel = true; return; }

                _testStepResumed = 9;
                CompleteWorkUnit();
            }

            //─────────────── check ไฟ WL ───────────────────────────────────
            if (!ShouldSkip(9))
            {
                SetStep(2, StepState.Running);

                if (!CheckRegisterStatus(2, 0, 1, true, "ไฟ WL")) { ReportStepFailure(e); return; }
                if (!WriteAndVerifyFlag(2, 0, 0, () => ID2_reg0IsOn, false)) { ReportStepFailure(e); return; }

                if (!CancellableSleep(1000)) { e.Cancel = true; return; }

                SetStep(2, StepState.Pass);
                _testStepResumed = 10;
                CompleteWorkUnit();
            }

            //─────────────── check ไฟ HP ───────────────────────────────────
            if (!ShouldSkip(10))
            {
                SetStep(3, StepState.Running);

                if (!CheckRegisterStatus(2, 1, 1, true, "ไฟ HP")) { ReportStepFailure(e); return; }
                if (!WriteAndVerifyFlag(2, 0, 1, () => ID2_reg0IsOn, true)) { ReportStepFailure(e); return; }

                if (!WriteRegister(1, 12, 1)) { ReportStepFailure(e); return; }  //CLA
                if (!CancellableSleep(1000)) { e.Cancel = true; return; }
                SetStep(3, StepState.Pass);
                _testStepResumed = 11;
                CompleteWorkUnit();
            }

            //─────────────── check Alarm ───────────────────────────────────
            if (!ShouldSkip(11))
            {
                SetStep(4, StepState.Running);
                LogToRx("set ค่า ALH = 50 และ ALF = 1");

                if (!WriteRegister(1, 15, 500)) { ReportStepFailure(e); return; }

                if (!CheckRegisterStatus_ALL(3, 1, true, "Alarm1")) { ReportStepFailure(e); return; }

                SetStep(4, StepState.Pass);
                if (!WriteRegister(1, 12, 1)) { ReportStepFailure(e); return; }  //CLA
                if (!CancellableSleep(1500)) { e.Cancel = true; return; }

                _testStepResumed = 12;
                CompleteWorkUnit();
            }

            //─────────────── check Compressor and Fan ───────────────────────────────────
            if (!ShouldSkip(12))
            {
                LogToRx("ตรวจสอบการทำงาน Compressor และ Fan ");

                SetStep(5, StepState.Running);

                if (!WriteRegister(1, 1, 200)) { ReportStepFailure(e); return; }

                ID3_reg3IsOn = false;
                ID3_reg4IsOn = false;
                ID3_reg5IsOn = false;

                if (!CheckRegisterStatus_ALL(3, 3, true, "CoolFan")) { ReportStepFailure(e); return; }

                if (!CancellableSleep(1000)) { e.Cancel = true; return; }

                SendOhm(2000);

                if (!CheckCompressorAndHotFan(20)) { ReportStepFailure(e); return; }

                if (!CancellableSleep(3000)) { e.Cancel = true; return; }

                SetStep(5, StepState.Pass);

                _testStepResumed = 13;
                CompleteWorkUnit();
            }

            //─────────────── set ค่า ───────────────────────────────────
            if (!CancellableSleep(1000)) { e.Cancel = true; return; }

            if (!ShouldSkip(13))
            {
                LogToRx("Setค่า SV = 20, ALF = 1, D1 = 3, D2 = 3, D = 1, ON = 5, CRL = 1");

                SetStep(6, StepState.Running);

                //if (!WriteRegister(1, 1, 200)) { ReportStepFailure(e); return; }
                if (!WriteRegister(1, 9, 3)) { ReportStepFailure(e); return; }
                if (!WriteRegister(1, 10, 3)) { ReportStepFailure(e); return; }

                if (!WriteRegister(1, 11, 1)) { ReportStepFailure(e); return; }
                if (!WriteRegister(1, 7, 5)) { ReportStepFailure(e); return; }
                if (!WriteRegister(1, 12, 1)) { ReportStepFailure(e); return; }

                SetStep(6, StepState.Pass);
                _testStepResumed = 14;
                CompleteWorkUnit();
            }

            if (!CancellableSleep(500)) { e.Cancel = true; return; }
            LogToRx("[ALL PASS] ตรวจสอบเสร็จสิ้นทุกขั้นตอน");
            e.Result = "SUCCESS";
        }




        //-- ส่วนของการทำงานเบื้องหลัง
        private void backgroundWorker1_DoWork(object? sender, DoWorkEventArgs e)
        {
            switch (_currentMode)
            {
                case WorkerMode.Calibrate:
                    DoCalibrate(e);
                    break;

                case WorkerMode.CalAndSaveExcel:
                    DoCalibrate(e);
                    break;

                case WorkerMode.Test:
                    DoTest(e);
                    break;

                case WorkerMode.CalAndTest:
                    DoCalAndTestWithRetry(e);
                    break;
            }
        }
        private void backgroundWorker1_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            UpdateProgressDisplay(e.ProgressPercentage);
        }
        private async void backgroundWorker1_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            btnRun.Text = "▶Run";
            btnRun.Enabled = true;
            bntStop.Enabled = false;
            btnRun.BackColor = Color.Gold;

            UpdateOpenForm4ButtonState();

            //btnCalSave.Text = "ทดสอบ";
            //btnCalSave.Enabled = true;
            btnRun.Enabled = true;

            txtSerialNumber.ReadOnly = false;
            //txtSerialNumber.Clear();      
            txtSerialNumber.Focus();

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
                BigMessageBox.Show("เกิดข้อผิดพลาด: " + e.Error.Message, "Error", MessageBoxIcon.Error, MessageBoxButtons.OK, fontSize: 14f);
            }
            else if (e.Result?.ToString() == "SUCCESS")
            {
                CompleteWorkProgress();

                if (_currentMode == WorkerMode.CalAndSaveExcel)
                {
                    SaveAdcToExcel(finalAdcArray);
                    BigMessageBox.Show("Calibration และบันทึกข้อมูลลง Excel สำเร็จ!", "Success", MessageBoxIcon.Information, MessageBoxButtons.OK, fontSize: 14f);
                    return;
                }

                _sequenceStep = 0;
                _testStepResumed = 0;
                _calDone = false;

                //chkCal.Checked = false;
                //chkTest.Checked = false;

                if (hasTest)
                {
                    if (_isForm4ResultReady)
                    {
                        bool btnAllPass = (_btnFunctionResult == "OK" || _btnFunctionResult == "-" || _btnFunctionResult == "N/A")
                                       && (_btnDownResult == "OK" || _btnDownResult == "-" || _btnDownResult == "N/A")
                                       && (_btnUpResult == "OK" || _btnUpResult == "-" || _btnUpResult == "N/A");
                        bool ledPass = (_ledResult == "OK" || _ledResult == "-" || _ledResult == "N/A");
                        string finalStatus = (btnAllPass && ledPass) ? "PASS" : "FAIL";
                        await CompleteTestAndPersistAsync(modeLabel, finalStatus);
                    }
                    else
                    {
                        btnRun.Enabled = false;
                        txtSerialNumber.ReadOnly = true;
                        _pendingForm4ResultAfterRun = true;
                        LogToRx("รอผลการทดสอบปุ่มจาก Form4 เพื่อบันทึกข้อมูล...", Color.Orange);
                    }
                }
                else
                {
                    LogToRx($"{modeLabel}: เสร็จสิ้นสมบูรณ์", Color.Green);
                    BigMessageBox.Show($"{modeLabel} เสร็จสิ้น!", "Success", MessageBoxIcon.Information, MessageBoxButtons.OK, fontSize: 14f);
                }
            }
            else
            {
                CloseForm4IfOpen();
                LogToRx($"{modeLabel}: ล้มเหลว");
                if (hasTest)
                    await CompleteTestAndPersistAsync(modeLabel, "FAIL");
                else
                    BigMessageBox.Show($"ไม่สามารถ {modeLabel} ได้ตามเกณฑ์ที่กำหนด", "Failed", MessageBoxIcon.Warning, MessageBoxButtons.OK, fontSize: 14f);
            }
        }





        //เตรียมค่า ADC สำหรับ Cal
        private enum AdcReadResult { Ok, NoData, Shorted, Open }
        public void SendOhm(double ohmValue) // 1. เปลี่ยนตรงนี้จาก int เป็น double
        {
            if (_ohmPort == null || !_ohmPort.IsOpen)
            {
                SafeBeginInvoke(() => BigMessageBox.Show("กรุณาเชื่อมต่อพอร์ต Ohm ก่อนครับ", "Warning", MessageBoxIcon.Warning, MessageBoxButtons.OK, fontSize: 14f));
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
                this.BeginInvoke(new Action(() => HandleDisconnect("Ohm Port Write Error: " + ex.Message)));
            }
        }
        private AdcReadResult CollectDataToArray(int currentOhm)
        {
            int waitSteps = (currentOhm == 100) ? 60 : 50;

            for (int i = 0; i < waitSteps; i++)
            {
                if (backgroundWorkerOhm.CancellationPending) return AdcReadResult.Ok; // ให้ loop เดิมจัดการ cancel เอง
                System.Threading.Thread.Sleep(100);
            }

            double lastValue = 0;
            bool hasData = false; // ★ เพิ่ม: แยกกรณี buffer ว่างจริงๆ ออกจากค่า 0 จริง

            lock (_tempAdcBuffer)
            {
                if (_tempAdcBuffer.Count > 0)
                {
                    lastValue = _tempAdcBuffer[_tempAdcBuffer.Count - 1];
                    hasData = true;
                    _tempAdcBuffer.Clear();
                }
            }

            // ★ กรณี A: ไม่มีข้อมูล ADC เข้ามาเลย (บอร์ดไม่ตอบ/ไม่เกี่ยวกับ Ohm Source)
            if (!hasData)
            {
                return AdcReadResult.NoData;
            }

            int adcIntValue = (int)lastValue;

            // ★ กรณี B: ADC = 0 -> Ohm Source ช็อต
            if (adcIntValue == 0)
            {
                return AdcReadResult.Shorted;
            }

            // ★ กรณี C: ADC = 0xFFFF (65535) -> สาย Ohm Source หลวมหรือหลุด
            if (adcIntValue == 0xFFFF)
            {
                return AdcReadResult.Open;
            }

            // ── ค่าปกติ: เก็บลง array เหมือนเดิม ──────────────────────
            if (_sequenceStep < finalAdcArray.Length)
            {
                finalAdcArray[_sequenceStep] = lastValue;

                SafeInvoke(() =>
                {
                    string timeStamp = DateTime.Now.ToString("HH:mm:ss");
                    LogToRx($"ADC: {adcIntValue:X4} ({adcIntValue}) |Ohm: {currentOhm}", Color.Black);
                });

                _sequenceStep++;
            }

            return AdcReadResult.Ok;
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

                // Cal ใหม่ทั้งหมด โดยคิด Progress จากจำนวนงานที่สำเร็จจริง
                DoCalibrate(e);
                if (e.Cancel || e.Result?.ToString() == "FAILED") return;

                // Test: รอบแรกทำเต็ม (รวม step1), รอบ retry ข้าม step1 เริ่มที่ 200 Ohm เลย
                if (_calAttemptCount > 1)
                {
                    _testStepResumed = 2;
                    ResetAllSteps();
                    SetStep(0, StepState.Pass); // Cal LED ให้ค้าง Pass ไว้
                }

                _ohmCheckFailed = false;
                DoTest(e);

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
                    ResetWorkProgressForRetry(skipInitialTestSetup: true);
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


        //---สั่งทดสอบ
        private int _calAttemptCount = 0;
        private const int MaxCalAttempts = 3;
        private bool _ohmCheckFailed = false;
        private bool CheckCompressorAndHotFan(int timeoutSeconds = 20)
        {
            LogToRx("กำลังตรวจสอบ Compressor และ HotFan");

            bool isCompressorOn = false;
            bool isHotFanOn = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (sw.Elapsed.TotalSeconds < timeoutSeconds)
            {
                if (ID3_reg5IsOn) isCompressorOn = true;
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
        private bool WriteRegister(byte id, ushort addr, short val, int delayMs = 300, int maxRetry = 8)
        {
            for (int attempt = 0; attempt < maxRetry; attempt++)
            {
                if (backgroundWorkerOhm.CancellationPending) { _isWriting = false; return false; }

                _isWriting = true;
                _isWritingStartTime = DateTime.Now;
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
            RecordAutomaticFailureReason("Register");
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
                _isWritingStartTime = DateTime.Now;
                ExecuteWriteSingleRegister(id, addr, val);
                if (!CancellableSleep(delayMs)) return false;
                _isWriting = false;                          // ✅

                if (currentFlag() == expected) return true;
            }
            RecordAutomaticFailureReason("Register");
            return false;
        }


        #endregion

        #region 07 - ผลทดสอบและการตรวจสอบแต่ละรายการ

        //--- ส่วนของการตรวจสอบ
        private string _verify200Result = "N/A";
        private string _verify2000Result = "N/A";
        private string _verify8000Result = "N/A";
        private string _verify20Result = "N/A";
        private string _verify30Result = "N/A";
        private string _verify40Result = "N/A";
        private string _verify50Result = "N/A";

        private string _wlResult = "N/A";
        private string _hpResult = "N/A";
        private string _wl_AL2Result = "N/A";
        private string _hp_AL2Result = "N/A";

        private string _alarm1Result = "N/A";
        private string _Compressor = "N/A";
        private string _HotFan = "N/A";
        private string _CoolFan = "N/A";
        private string _RelayResult = "N/A";
        private bool VerifyOhmValueSync(int ohm, double expectedPv)
        {
            if (backgroundWorkerOhm.CancellationPending) return false;

            LogToRx($"กำลังทดสอบที่: {ohm} Ohm (เป้าหมาย PV: {expectedPv})...");
            SendOhm(ohm);

            int timeoutSeconds = 8;
            bool isPass = false;

            for (int i = 0; i < timeoutSeconds; i++)
            {
                if (backgroundWorkerOhm.CancellationPending) return false;
                if (!CancellableSleep(1000)) return false;

                double diff = Math.Abs(_currentPv - expectedPv);
                //LogToRx($"วินาทีที่ {i + 1}: ค่าปัจจุบัน PV = {_currentPv:F2} (Diff: {diff:F2})");

                if (diff < 1.0)
                {
                    isPass = true;
                    break;
                }
            }

            if (backgroundWorkerOhm.CancellationPending) return false;

            LogToRx(isPass ? $"[PASS] {ohm} Ohm" : $"[FAIL] {ohm} Ohm (PV สุดท้าย={_currentPv})", isPass ? Color.Green : Color.Red);

            string result = _currentPv.ToString("F1");
            if (ohm == 200) _verify200Result = result;
            if (ohm == 2000) _verify2000Result = result;
            if (ohm == 8000) _verify8000Result = result;
            if (ohm == 2435) _verify20Result = result;
            if (ohm == 1653) _verify30Result = result;
            if (ohm == 1150) _verify40Result = result;
            if (ohm == 818) _verify50Result = result;
            return isPass;
        }
        private bool CheckRegisterStatus(byte targetID, int address, int value, bool expectedStatus, string label)
        {
            LogToRx($"กำลังตรวจสอบ {label}");

            bool currentStatus = false;
            int maxAttempts = 12;

            int writeCount = 0;
            bool isAlarmLatchLabel = (label == "ไฟ WL" || label == "ไฟ HP"); // ★ เพิ่ม

            for (int i = 0; i < maxAttempts; i++)
            {
                if (backgroundWorkerOhm.CancellationPending) return false;   // เช็ค cancel ก่อนทุกรอบ

                if (i % 3 == 0)
                {
                    ExecuteWriteSingleRegister(targetID, (ushort)address, (short)value);
                    writeCount++;   // ★ เพิ่ม

                    if (isAlarmLatchLabel && writeCount == 3)   // ★ เพิ่ม: ครบ 3 ครั้งพอดี (i=6)
                    {
                        //LogToRx($"⚠ {label} ยิงคำสั่งครบ 3 ครั้ง สั่ง Clear Alarm อัตโนมัติ", Color.Orange);
                        WriteRegister(1, 12, 1);
                    }
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
                // WL เปิดไม่สำเร็จ
                if (targetID == 2 && address == 0)
                {
                    _wlResult = "FAIL";
                }

                // HP เปิดไม่สำเร็จ
                if (targetID == 2 && address == 1)
                {
                    _hpResult = "FAIL";
                }

                LogToRx($"[FAIL] {label} ไม่ทำงาน! (ค่าปัจจุบัน: {currentStatus}, ค่าที่หวัง: {expectedStatus})", Color.Red);
                return false;
            }

            LogToRx($"[PASS] {label} ทำงาน", Color.Green);

            // ★ ย้ายมาจับเวลาตรงนี้แทน — หลังยืนยัน WL/HP เปลี่ยนสถานะสำเร็จแล้วจริง ๆ
            DateTime confirmedChangeTime = DateTime.Now;

            Thread.Sleep(200);

            DateTime waitStart = DateTime.Now;
            while ((DateTime.Now - waitStart).TotalMilliseconds < 5000)
            {
                DateTime lastFrame;
                lock (_id3TimeLock) { lastFrame = _lastID3FrameTime; }

                if (lastFrame > confirmedChangeTime) break;   // ★ เทียบกับเวลาที่เพิ่งยืนยันเสร็จ ไม่ใช่ตอนต้นฟังก์ชัน
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
                if (address == 3) _CoolFan = "OK";
                if (address == 4) _HotFan = "OK";
                if (address == 5) _Compressor = "OK";
                if (address == 20) _RelayResult = "OK";

                return true;
            }
            else
            {

                LogToRx($"[FAIL] {label} ไม่ทำงาน! (ค่าปัจจุบัน: {currentStatus}, ค่าที่หวัง: {expectedStatus})", Color.Red);

                if (address == 1) _alarm1Result = "FAIL";
                if (address == 3) _CoolFan = "FAIL";
                if (address == 4) _HotFan = "FAIL";
                if (address == 5) _Compressor = "FAIL";
                if (address == 20) _RelayResult = "FAIL";

                return false;
            }
        }



        #endregion

        #region 08 - Log และการแสดงข้อความใน RXBOX

        //-- ส่วนของการแสดงผล RXBOX
        private readonly RichTextBox _logBuffer = new RichTextBox(); // Memory Buffer ไว้เก็บ Log สมบูรณ์ใน RAM (สร้างขึ้นนอก UI จึงไม่โดน Windows ลบค่าสี)
        private class LogItem
        {
            public string Message { get; set; } = string.Empty;
            public Color Color { get; set; }
        }
        private readonly List<LogItem> _logHistory = new List<LogItem>();

        private void LogToRx(string message, Color? color = null)
        {
            if (RxBox.InvokeRequired)
            {
                RxBox.Invoke(new MethodInvoker(() => LogToRx(message, color)));
                return;
            }

            string timeStamp = DateTime.Now.ToString("HH:mm:ss");
            string fullMessage = $"[{timeStamp}] {message}\r\n";
            Color targetColor = color ?? RxBox.ForeColor;

            _logHistory.Add(new LogItem { Message = fullMessage, Color = targetColor });

            RxBox.SelectionStart = RxBox.TextLength;
            RxBox.SelectionLength = 0;
            RxBox.SelectionFont = RxBox.Font;
            RxBox.SelectionColor = targetColor;
            RxBox.AppendText(fullMessage);
            RxBox.ScrollToCaret();
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.WindowState != FormWindowState.Minimized && _logHistory.Count > 0)
            {
                RxBox.Clear();
                foreach (var item in _logHistory)
                {
                    RxBox.SelectionStart = RxBox.TextLength;
                    RxBox.SelectionLength = 0;
                    RxBox.SelectionFont = RxBox.Font;
                    RxBox.SelectionColor = item.Color;
                    RxBox.AppendText(item.Message);
                }
                RxBox.ScrollToCaret();
            }
        }
        private void ClearRxLog()
        {
            if (RxBox.InvokeRequired)
            {
                RxBox.Invoke(new MethodInvoker(ClearRxLog));
                return;
            }

            RxBox.Clear();
            _logBuffer.Clear();
        }



        #endregion

        #region 09 - ปุ่มควบคุมแบบ Manual และ Form2

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



        #endregion

        #region 10 - การสลับหน้าและวงจรชีวิตของ Form

        //-- ควบคุมการสลับหน้า(tap)
        private void displaybox_MouseDoubleClick(object sender, MouseEventArgs e) { tabControl2.SelectedIndex = 1; }
        private void pictHome_DoubleClick(object sender, EventArgs e) { tabControl2.SelectedIndex = 0; }
        private void picdata_DoubleClick(object sender, EventArgs e) { tabControl2.SelectedIndex = 2; }
        private void BackHome_Click(object sender, EventArgs e) { tabControl2.SelectedIndex = 0; }
        private void displaybox_Click(object sender, EventArgs e) { }






        private void Form1_Load(object sender, EventArgs e)
        {
            sendTosever.Visible = false;  // ซ่อนปุ่ม test send — เปิดผ่าน Ctrl+Shift+T
            btnPost.Visible = false;       // ซ่อนปุ่ม Post — เปิดผ่าน Ctrl+Shift+P
            _excelFilePath = string.Empty;
            txtExcelPath.Text = "กรุณาเลือก LOT เพื่อโหลดข้อมูล";

            _customResultFolder = Properties.Settings.Default.SavedResultFolder;
            if (!string.IsNullOrEmpty(_customResultFolder))
            {
                txtExcelPath.Text = _customResultFolder;
            }

            txtExcelPath.Text = _customResultFolder ?? "กรุณาเลือกโฟลเดอร์ผ่านปุ่ม Select Folder";

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

            txtECN.Text = Properties.Settings.Default.SavedECN?.Trim() ?? "RD-CN-18-0023";

            // ไม่โหลด DataGrid อัตโนมัติในตอนเปิดเครื่องครั้งแรก รอให้ผู้ใช้เลือก LOT ก่อน
            dataGridView1.DataSource = null;
            lblPassCount.Text = "0";
            lblFailCount.Text = "0";
            lblLotCount.Text = "กรุณาเลือก LOT เพื่อแสดงข้อมูล";

            cmbLot.SelectedIndex = -1;
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





        #endregion

        #region 11 - การบันทึกผลลง Excel และจัดการไฟล์รายงาน

        //-- ส่วนของการเก็บผลการ test ลง Excel
        private string? _customResultFolder = null;

        private string _lastTestStatus = "-";
        public string _btnFunctionResult = "N/A";
        public string _btnDownResult = "N/A";
        public string _btnUpResult = "N/A";
        public string _ledResult = "N/A";
        private bool _isFirstChar = true; // รอรับตัวแรกจากสแกนใหม่

        private const int ExcelFirstTestRow = 9;
        private const int ExcelLastResultColumn = 28;
        private const string ExcelResultSheetPassword = "PassCMA003";

        public string? ResultFolder => _customResultFolder;
        private bool EnsureFolderSelected()
        {
            if (string.IsNullOrEmpty(_customResultFolder) || !Directory.Exists(_customResultFolder))
            {
                BigMessageBox.Show("กรุณาเลือกโฟลเดอร์สำหรับจัดเก็บข้อมูลก่อนดำเนินการ!", "Warning", MessageBoxIcon.Warning, MessageBoxButtons.OK, fontSize: 14f);

                // เปิดหน้าต่างให้เลือกโฟลเดอร์ทันที
                btnSelectFolder_Click(this, EventArgs.Empty);

                // เช็คอีกรอบหลังเลือก
                return !string.IsNullOrEmpty(_customResultFolder) && Directory.Exists(_customResultFolder);
            }
            return true;
        }
        private bool IsSerialNumberDuplicate(string sn, string lotNumber)
        {
            return IsSerialNumberDuplicate(sn, lotNumber, out _);
        }

        private bool IsSerialNumberDuplicate(string sn, string lotNumber, out int duplicateRow)
        {
            duplicateRow = -1;
            if (string.IsNullOrEmpty(ResultFolder) || !Directory.Exists(ResultFolder)) return false;

            if (string.IsNullOrEmpty(sn) || string.IsNullOrEmpty(lotNumber))
                return false;

            string safeFileName = lotNumber.Replace("/", "-");
            string currentLotPath = Path.Combine(ResultFolder, safeFileName + ".xlsx");

            if (!File.Exists(currentLotPath))
                return false;

            try
            {
                // ใช้ MemoryStream Snapshot เหมือน DEF-03N เพื่อป้องกันข้อผิดพลาดเวลาไฟล์ถูกเปิดใช้งานอยู่
                using var snapshot = new MemoryStream();
                using (var source = new FileStream(currentLotPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    source.CopyTo(snapshot);
                }
                snapshot.Position = 0;

                using (var workbook = new XLWorkbook(snapshot))
                {
                    var sheet = workbook.Worksheet(1);

                    int summaryHeaderRow = FindSummaryHeaderRow(sheet);

                    for (int row = ExcelFirstTestRow; row < summaryHeaderRow; row++)
                    {
                        if (!IsExcelTestRow(sheet, row)) continue;

                        string existingSN = sheet.Cell(row, 28).GetString().Trim();

                        if (string.IsNullOrEmpty(existingSN) || IsPlaceholderSN(existingSN))
                            continue;   // ข้ามแถวนี้ไปเลย ไม่ต้องเช็คต่อ

                        if (existingSN.Equals(sn.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            duplicateRow = row;
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToRx($"⚠️ ตรวจ S/N ไม่สำเร็จ: {ex.Message}", Color.Orange);
            }

            return false;
        }

        private int FindSummaryHeaderRow(IXLWorksheet sheet)
        {
            int lastRow = sheet.LastRowUsed()?.RowNumber() ?? ExcelFirstTestRow;

            for (int row = ExcelFirstTestRow; row <= lastRow; row++)
            {
                for (int col = 1; col <= 20; col++)
                {
                    string text = sheet.Cell(row, col).GetString().Trim().Replace('’', '\'');
                    if (text.Equals("Q'ty", StringComparison.OrdinalIgnoreCase) || text.Equals("Qty", StringComparison.OrdinalIgnoreCase))
                    {
                        return row;
                    }
                }
            }

            throw new InvalidOperationException("ไม่พบส่วนสรุป Q'ty / Good / Defect ในไฟล์ Excel");
        }

        private bool IsExcelTestRow(IXLWorksheet sheet, int row)
        {
            return int.TryParse(sheet.Cell(row, 1).GetString().Trim(), out _);
        }

        private void UnprotectExcelResultSheet(IXLWorksheet sheet)
        {
            if (!sheet.IsProtected) return;

            try
            {
                sheet.Unprotect(ExcelResultSheetPassword);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "ไม่สามารถปลดล็อก Sheet ผลทดสอบได้ ไฟล์อาจถูก Protect ด้วยรหัสผ่านอื่น",
                    ex);
            }
        }

        private void ProtectExcelResultSheet(IXLWorksheet sheet, int summaryHeaderRow)
        {
            var usedRange = sheet.RangeUsed(XLCellsUsedOptions.All);
            if (usedRange != null)
            {
                usedRange.Style.Protection.Locked = true;
            }

            // อนุญาตให้กรอกชื่อผู้ตรวจ ผู้ยืนยัน และผู้อนุมัติได้เฉพาะช่องว่างใต้หัวข้อ
            int signatureInputRow = summaryHeaderRow + 1;
            sheet.Range(signatureInputRow, 15, signatureInputRow, 28)
                .Style.Protection.Locked = false;

            var allowedElements = XLSheetProtectionElements.SelectLockedCells
                | XLSheetProtectionElements.SelectUnlockedCells
                | XLSheetProtectionElements.EditObjects;

            sheet.Protect(
                ExcelResultSheetPassword,
                XLProtectionAlgorithm.Algorithm.SHA512,
                allowedElements);
        }
        private void picCir_DoubleClick(object sender, EventArgs e) { LoadExcelToDataGrid(); }
        private bool SaveTestDataToExcel(string testStatus)
        {
            // 🟢 1. บังคับเช็คว่าเลือก Folder หรือยัง
            if (!EnsureFolderSelected()) return false;

            if (this.InvokeRequired)
            {
                return (bool)this.Invoke(
                    new Func<bool>(() => SaveTestDataToExcel(testStatus)));
            }

            try
            {
                string serialNumber = txtSerialNumber.Text.Trim();
                string lotNumber = txtLot.Text.Trim();

                if (string.IsNullOrEmpty(serialNumber))
                {
                    BigMessageBox.Show("กรุณาใส่ Serial Number!", "Warning", MessageBoxIcon.Warning, MessageBoxButtons.OK, fontSize: 14f);
                    return false;
                }

                if (string.IsNullOrEmpty(lotNumber))
                {
                    BigMessageBox.Show("กรุณาใส่ LOT Number!", "Warning", MessageBoxIcon.Warning, MessageBoxButtons.OK, fontSize: 14f);
                    return false;
                }

                _inspectionEndTime = DateTime.Now;

                // 🟢 2. กำหนด Path โดยอิงจาก ResultFolder ที่เลือกเท่านั้น
                string safeFileName = lotNumber.Replace("/", "-");
                string lotFilePath = Path.Combine(ResultFolder!, safeFileName + ".xlsx");

                if (!File.Exists(lotFilePath))
                {
                    string templatePath = Path.Combine(Application.StartupPath, "CMA-003 QC Inspection.xlsx");
                    if (!File.Exists(templatePath))
                    {
                        templatePath = Path.Combine(Application.StartupPath, "template.xlsx");
                    }
                    if (!File.Exists(templatePath))
                    {
                        BigMessageBox.Show($"ไม่พบไฟล์เทมเพลต:\n{templatePath}\n\nกรุณาตรวจสอบ",
                            "Template Not Found", MessageBoxIcon.Error, MessageBoxButtons.OK, fontSize: 14f);
                        return false;
                    }
                    Directory.CreateDirectory(ResultFolder!);
                    File.Copy(templatePath, lotFilePath);
                }

                _excelFilePath = lotFilePath;
                txtExcelPath.Text = lotFilePath;

                using (var workbook = new XLWorkbook(_excelFilePath))
                {
                    var sheet = workbook.Worksheet(1);
                    UnprotectExcelResultSheet(sheet);

                    WriteLotToHeader(sheet, lotNumber);
                    DateTime now = DateTime.Now;
                    int buddhistYear2Digit = (now.Year + 543) % 100;
                    string thaiDateStr = $"{now.Day}/{now.Month}/{buddhistYear2Digit:00}";
                    WriteDateToHeader(sheet, thaiDateStr);                      // Date
                    WriteEcnToHeader(sheet, txtECN.Text.Trim());               // ECN
                    WriteFirmwareToHeader(sheet, _firmwareValue);              // Firmware
                    WriteDisplayFirmwareToHeader(sheet, _displayFirmwareValue); // Display firmware
                    WriteEquipmentExpiryToHeader(sheet, GetEquipmentExcelText()); // Equipment expiration

                    int targetRow = -1;
                    int duplicateRow = -1;
                    int summaryHeaderRow = FindSummaryHeaderRow(sheet);

                    // เตรียมหน้าสุดท้ายให้มีช่อง Test ต่อเนื่อง และวาง Summary ไว้ท้ายหน้า
                    CmaReportLayout.EnsureSummaryPageCapacity(sheet, summaryHeaderRow);
                    summaryHeaderRow = FindSummaryHeaderRow(sheet);

                    // 🟢 3. ค้นหาแถว Test จริงจนถึงส่วนสรุปท้ายเอกสาร
                    for (int r = ExcelFirstTestRow; r < summaryHeaderRow; r++)
                    {
                        if (!IsExcelTestRow(sheet, r)) continue;

                        string existingSN = sheet.Cell(r, 28).GetString().Trim();

                        bool hasSN = !string.IsNullOrEmpty(existingSN) && !IsPlaceholderSN(existingSN);

                        // เช็ก S/N ซ้ำ: ถ้าตรงกับเครื่องที่กำลังทดสอบ ให้บันทึกทับแถวนี้
                        if (hasSN && existingSN.Equals(serialNumber, StringComparison.OrdinalIgnoreCase))
                        {
                            duplicateRow = r;
                            break;
                        }

                        // แถวที่มีเลข Column A แต่ช่อง S/N ยังว่างอยู่ (แถวว่างแรกสุด)
                        if (!hasSN && targetRow == -1)
                        {
                            targetRow = r;
                        }
                    }

                    int nextRow;
                    if (duplicateRow != -1)
                    {
                        nextRow = duplicateRow;
                    }
                    else if (targetRow != -1)
                    {
                        nextRow = targetRow;
                    }
                    else
                    {
                        nextRow = CmaReportLayout.AddFullPageCapacity(sheet, summaryHeaderRow);
                        summaryHeaderRow = FindSummaryHeaderRow(sheet);
                    }

                    // ถ้าหน้า Summary ถูกใช้เต็ม ให้ปิดหน้านั้นเป็นหน้าปกติ
                    // และเตรียมหน้า Summary ใหม่ไว้ท้ายเอกสารเสมอ
                    CmaReportLayout.EnsureSummaryPageCapacity(sheet, summaryHeaderRow);
                    summaryHeaderRow = FindSummaryHeaderRow(sheet);

                    // ดึงเลขลำดับ No จาก Column A มาบันทึกกลับ
                    int no = int.TryParse(sheet.Cell(nextRow, 1).GetString().Trim(), out int currentNo) ? currentNo : 0;

                    // 🟢 4. บันทึกข้อมูลลง Cell
                    SetValueCell(sheet, nextRow, 1, no);
                    SetSymbolCell(sheet, nextRow, 3, FormatCheckmarkForExcel(_RelayResult));
                    SetSymbolCell(sheet, nextRow, 5, FormatCheckmarkForExcel(_wlResult));
                    SetSymbolCell(sheet, nextRow, 6, FormatCheckmarkForExcel(_wl_AL2Result));
                    SetSymbolCell(sheet, nextRow, 8, FormatCheckmarkForExcel(_hpResult));
                    SetSymbolCell(sheet, nextRow, 9, FormatCheckmarkForExcel(_hp_AL2Result));
                    SetSymbolCell(sheet, nextRow, 11, FormatCheckmarkForExcel(_alarm1Result));

                    SetSymbolCell(sheet, nextRow, 12, FormatCheckmarkForExcel(_HotFan));
                    SetSymbolCell(sheet, nextRow, 13, FormatCheckmarkForExcel(_CoolFan));
                    SetSymbolCell(sheet, nextRow, 14, FormatCheckmarkForExcel(_Compressor));
                    SetValueCell(sheet, nextRow, 15, FormatNumericForExcel(_verify200Result));
                    SetValueCell(sheet, nextRow, 16, FormatNumericForExcel(_verify2000Result));
                    SetValueCell(sheet, nextRow, 17, FormatNumericForExcel(_verify8000Result));

                    SetValueCell(sheet, nextRow, 18, FormatNumericForExcel(_verify20Result)); // 20°C
                    SetValueCell(sheet, nextRow, 19, FormatNumericForExcel(_verify30Result)); // 30°C
                    SetValueCell(sheet, nextRow, 20, FormatNumericForExcel(_verify40Result)); // 40°C
                    SetValueCell(sheet, nextRow, 21, FormatNumericForExcel(_verify50Result)); // 50°C

                    SetSymbolCell(sheet, nextRow, 22, FormatCheckmarkForExcel(_btnFunctionResult));
                    SetSymbolCell(sheet, nextRow, 23, FormatCheckmarkForExcel(_btnDownResult));
                    SetSymbolCell(sheet, nextRow, 24, FormatCheckmarkForExcel(_btnUpResult));
                    SetSymbolCell(sheet, nextRow, 25, FormatCheckmarkForExcel(_ledResult));

                    if (testStatus == "PASS")
                    {
                        SetSymbolCell(sheet, nextRow, 26, "✔");   // Z = ปกติ
                        SetSymbolCell(sheet, nextRow, 27, "");     // AA = ไม่ปกติ
                    }
                    else
                    {
                        SetSymbolCell(sheet, nextRow, 26, "");     // Z = ปกติ
                        SetSymbolCell(sheet, nextRow, 27, "✖");   // AA = ไม่ปกติ
                    }
                    SetValueCell(sheet, nextRow, 28, serialNumber); // AB = S/N

                    // 🟢 5. คำนวณสรุปผล Q'ty / Good / Defect / Yield จากแถว Test ทั้งหมด
                    int actualCount = 0;
                    int goodCount = 0;
                    int defectCount = 0;

                    summaryHeaderRow = FindSummaryHeaderRow(sheet);
                    for (int checkRow = ExcelFirstTestRow; checkRow < summaryHeaderRow; checkRow++)
                    {
                        if (!IsExcelTestRow(sheet, checkRow)) continue;

                        string snCheck = sheet.Cell(checkRow, 28).GetString().Trim();

                        if (string.IsNullOrEmpty(snCheck) || IsPlaceholderSN(snCheck))
                        {
                            continue;
                        }

                        actualCount++;

                        string zCheck = sheet.Cell(checkRow, 26).GetString().Trim();
                        string aaCheck = sheet.Cell(checkRow, 27).GetString().Trim();

                        if (zCheck == "✔" || zCheck.Equals("OK", StringComparison.OrdinalIgnoreCase)) goodCount++;
                        else if (aaCheck == "✖" || aaCheck == "✘" || aaCheck.Equals("OK", StringComparison.OrdinalIgnoreCase) || aaCheck.Equals("FAIL", StringComparison.OrdinalIgnoreCase)) defectCount++;
                    }

                    WriteLotSizeToHeader(sheet, actualCount);

                    // เขียนสรุปในแถวถัดจากหัวข้อ Q'ty ซึ่งอาจถูกเลื่อนลง
                    double yieldPercent = actualCount > 0 ? Math.Round((double)goodCount / actualCount * 100.0, 2) : 0;
                    int summaryValueRow = summaryHeaderRow + 1;

                    SetSummaryCell(sheet, summaryValueRow, 10, actualCount);
                    SetSummaryCell(sheet, summaryValueRow, 12, goodCount);
                    SetSummaryCell(sheet, summaryValueRow, 14, defectCount);
                    SetSummaryCell(sheet, summaryValueRow, 15, yieldPercent, "0.00\"%\"");

                    WriteOperatorNameToSummary(sheet, summaryHeaderRow);

                    ProtectExcelResultSheet(sheet, summaryHeaderRow);
                    workbook.Save();
                }

                LoadLotHistory();
                LoadExcelToDataGrid(_excelFilePath);

                LogToRx($"S/N: {serialNumber} | LOT: {lotNumber} บันทึกเรียบร้อย", Color.Green);

                _lastTestStatus = testStatus;
                return true;
            }
            catch (IOException)
            {
                BigMessageBox.Show("ไม่สามารถบันทึกได้ กรุณาปิดไฟล์ Excel ก่อน!",
                    "File Locked", MessageBoxIcon.Warning, MessageBoxButtons.OK, fontSize: 14f);
                return false;
            }
            catch (Exception ex)
            {
                BigMessageBox.Show($"ไม่สามารถบันทึกข้อมูลลง Excel ได้: {ex.Message}",
                    "Error", MessageBoxIcon.Warning, MessageBoxButtons.OK, fontSize: 14f);
                return false;
            }
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

                    Properties.Settings.Default.SavedResultFolder = _customResultFolder;
                    Properties.Settings.Default.Save();

                    RefreshFolderData();
                    ScheduleSnDuplicateCheck();
                }
            }
        }
        private void RefreshFolderData()
        {
            if (string.IsNullOrEmpty(_customResultFolder)) return;

            // อัปเดต ComboBox แสดงรายการไฟล์ในโฟลเดอร์ที่เลือก
            LoadLotHistory();
        }
        private static void SetSymbolCell(IXLWorksheet sheet, int row, int col, string symbol)
        {
            var cell = sheet.Cell(row, col);
            cell.Value = symbol;
            cell.Style.Font.FontName = "Segoe UI Symbol";
            cell.Style.Font.FontSize = 6;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private static void SetValueCell(IXLWorksheet sheet, int row, int col, object value)
        {
            var cell = sheet.Cell(row, col);
            cell.Value = XLCellValue.FromObject(value);
            cell.Style.Font.FontName = "Cordia New";
            cell.Style.Font.FontSize = 10;
            cell.Style.Font.Bold = false;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private static void SetSummaryCell(IXLWorksheet sheet, int row, int col, object value, string? numberFormat = null)
        {
            var cell = sheet.Cell(row, col);
            cell.Value = XLCellValue.FromObject(value);
            cell.Style.Font.FontName = "Cordia New";
            cell.Style.Font.FontSize = 11;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            if (!string.IsNullOrEmpty(numberFormat))
            {
                cell.Style.NumberFormat.Format = numberFormat;
            }
        }

        private static string FormatCheckmarkForExcel(string resultText)
        {
            if (string.IsNullOrWhiteSpace(resultText) 
                || resultText == "-" 
                || resultText.Equals("N/A", StringComparison.OrdinalIgnoreCase))
            {
                return "N/A";
            }

            string trimmed = resultText.Trim();
            if (string.Equals(trimmed, "OK", StringComparison.OrdinalIgnoreCase) 
                || trimmed == "✔" 
                || string.Equals(trimmed, "PASS", StringComparison.OrdinalIgnoreCase))
            {
                return "✔";
            }

            if (string.Equals(trimmed, "FAIL", StringComparison.OrdinalIgnoreCase) 
                || trimmed == "✖"
                || trimmed == "✘")
            {
                return "✖";
            }

            return trimmed;
        }

        private static string FormatNumericForExcel(string numericText)
        {
            if (string.IsNullOrWhiteSpace(numericText) 
                || numericText == "-" 
                || numericText.Equals("N/A", StringComparison.OrdinalIgnoreCase))
            {
                return "N/A";
            }

            return numericText.Trim();
        }





        #endregion

        #region 12 - Form4, Display Test และการตรวจสาย Display

        //เรียกจาก Form4 ทุกครั้งที่ได้รับ frame จาก _DisPort เพื่อ reset timeout
        private bool _form4TestAlreadyDone = false;
        private bool _form4ManuallyClosed = false;
        private bool _pendingForm4ResultAfterRun = false;
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
        private void UpdateOpenForm4ButtonState()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateOpenForm4ButtonState));
                return;
            }

            bool portsReady = _serialPort.IsOpen && _ohmPort.IsOpen && _DisPort.IsOpen;

            if (!portsReady)
            {
                btnOpenForm4.Enabled = false;
                return;
            }

            bool isRunning = backgroundWorkerOhm.IsBusy;
            bool involvesTest = _currentMode == WorkerMode.Test || _currentMode == WorkerMode.CalAndTest;

            if (isRunning && !involvesTest)
            {
                // ★ กำลังรัน Calibrate อย่างเดียว หรือ CalAndSaveExcel -> ปิดปุ่มไปเลย ไม่มีข้อยกเว้น
                btnOpenForm4.Enabled = false;
                return;
            }

            if (isRunning && involvesTest)
            {
                // อยู่ระหว่าง Test/CalAndTest -> ตามพฤติกรรมเดิม (เผื่อเผลอปิด Form4 ที่เปิดอัตโนมัติ)
                btnOpenForm4.Enabled = _form4ManuallyClosed;
                return;
            }

            // ไม่ได้กำลังรัน (idle) -> เปิด standalone ได้ตามปกติ
            btnOpenForm4.Enabled = true;
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
            }
            _form4Instance = new Form4(_DisPort!, this);
            _form4Instance.AutoCloseOnComplete = true; // ★ flow อัตโนมัติปิดเองเหมือนเดิม
            _form4Instance.Owner = this;
            _form4Instance.Show();
            UpdateOpenForm4ButtonState(); // ★

        }

        public void OnForm4Closed()
        {
            bool duringCalTest = backgroundWorkerOhm.IsBusy &&

            (_currentMode == WorkerMode.Test || _currentMode == WorkerMode.CalAndTest);
            if (duringCalTest && !_form4TestAlreadyDone)

            {
                _form4ManuallyClosed = true; // ★ ยังไม่ทันจบ test แล้วหายไป = โดนกากบาท

            }
            UpdateOpenForm4ButtonState();

        }
        public void OnForm4TestComplete(string form4Status)
        {
            _isForm4ResultReady = true;
            _pendingForm4ResultAfterRun = false;
            switch (_form4TriggerMode)
            {
                case Form4TriggerMode.StandaloneWithSN:
                    this.BeginInvoke(new Action(() =>
                        _ = CompleteTestAndPersistAsync("Test", form4Status)));
                    _form4TriggerMode = Form4TriggerMode.AutoFlow;
                    break;

                case Form4TriggerMode.StandaloneNoSN:
                    this.Invoke(new Action(() =>
                        LogToRx("ทดสอบปุ่ม (Standalone) เสร็จสิ้น - ไม่มี S/N จึงไม่บันทึกผลลง Excel", Color.Green)));
                    _form4TriggerMode = Form4TriggerMode.AutoFlow;
                    break;

                case Form4TriggerMode.AutoFlow:
                default:
                    _form4TestAlreadyDone = true;      // ★ เพิ่ม
                    UpdateOpenForm4ButtonState();      // ★ เพิ่ม รีเฟรชปุ่มทันที
                    if (btnRun.Text == "▶Run")//&& chkTest.Checked == false)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            bool btnAllPass = (_btnFunctionResult == "OK" || _btnFunctionResult == "-" || _btnFunctionResult == "N/A")
                                           && (_btnDownResult == "OK" || _btnDownResult == "-" || _btnDownResult == "N/A")
                                           && (_btnUpResult == "OK" || _btnUpResult == "-" || _btnUpResult == "N/A");
                            bool ledPass = (_ledResult == "OK" || _ledResult == "-" || _ledResult == "N/A");

                            string finalExcelStatus = (btnAllPass && ledPass && form4Status == "PASS") ? "PASS" : "FAIL";
                            string finalModeLabel = _currentMode == WorkerMode.CalAndTest
                                ? "Cal + Test"
                                : "Test";
                            _ = CompleteTestAndPersistAsync(finalModeLabel, finalExcelStatus);
                        }));
                    }
                    break;
            }
        }
        public void OnDisPortFrameReceived()
        {
            lock (_disPortTimeLock) { _lastDisPortFrameTime = DateTime.Now; }
        }
        public void SetDisPortTimeoutCheckSuppressed(bool suppressed)
        {
            if (suppressed)
            {
                // Form4 เปิด: พักการตรวจ timeout ของ Display ทันที
                _suppressDisPortTimeoutCheck = true;

                // ยกเลิกเวลารอเดิม เผื่อเปิด Form4 ซ้ำ
                _resumeDisPortTimeoutCheckAt = DateTime.MinValue;
            }
            else
            {
                // Form4 ปิด: ยังพักต่ออีก 30 วินาที
                _suppressDisPortTimeoutCheck = true;
                _resumeDisPortTimeoutCheckAt = DateTime.Now.AddSeconds(30);
            }
        }
        private void btnOpenForm4_Click(object sender, EventArgs e)
        {
            if (!_serialPort.IsOpen || !_ohmPort.IsOpen || !_DisPort.IsOpen)
            {
                BigMessageBox.Show("กรุณาเชื่อมต่อพอร์ตให้ครบก่อนเปิด Form4!", "Warning",
                    MessageBoxIcon.Warning, MessageBoxButtons.OK, fontSize: 14f);
                return;
            }

            bool duringCalTest = backgroundWorkerOhm.IsBusy &&
                                  (_currentMode == WorkerMode.Test || _currentMode == WorkerMode.CalAndTest);

            if (duringCalTest || _pendingForm4ResultAfterRun)
            {
                // ★ แก้: รวมเคส worker จบไปแล้วแต่ยังรอผล Form4 อยู่ด้วย
                if (_form4Instance == null || _form4Instance.IsDisposed)
                {
                    _form4TriggerMode = Form4TriggerMode.AutoFlow;
                    _form4ManuallyClosed = false;
                    OpenForm4Deferred();
                }
                return;
            }

            // กรณี 3: ไม่ได้อยู่ระหว่าง Cal+Test -> เปิดแบบ standalone
            string sn = txtSerialNumber.Text.Trim();

            if (!string.IsNullOrEmpty(sn))
            {
                _form4TriggerMode = Form4TriggerMode.StandaloneWithSN;
                LoadExistingResultsForSN(sn, txtLot.Text.Trim());
            }
            else
            {
                _form4TriggerMode = Form4TriggerMode.StandaloneNoSN;
            }

            if (_form4Instance != null && !_form4Instance.IsDisposed)
            {
                if (_DisPort != null) _DisPort.DataReceived -= _form4Instance.DataReceivedHandler;
                _form4Instance.Close();
            }

            _form4Instance = new Form4(_DisPort!, this);
            _form4Instance.AutoCloseOnComplete = false;   // ★ standalone -> ไม่ปิดเอง
            _form4Instance.Owner = this;
            _form4Instance.Show();

            UpdateOpenForm4ButtonState();
        }






        #endregion

        #region 13 - Serial Number, Lot, History และตารางข้อมูล


        private void txtSerialNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _barcodeDebounceTimer?.Stop();
                _isFirstChar = true;

                if (TryProcessCombinedBarcode(txtSerialNumber.Text, txtSerialNumber))
                    return;

                txtSerialNumber.SelectAll();
            }
        }
        private void ScheduleSnDuplicateCheck()
        {
            txtSerialNumber.ForeColor = SystemColors.WindowText;
            txtSerialNumber.BackColor = SystemColors.Window;
            _isDuplicateSN = false;
            _duplicateSNRow = -1;

            _snDebounceTimer?.Stop();
            if (!string.IsNullOrWhiteSpace(txtSerialNumber.Text))
            {
                _snDebounceTimer?.Start();
            }
        }

        private void txtSerialNumber_TextChanged(object sender, EventArgs e)
        {
            if (_isProcessingBarcode) return;
            ScheduleSnDuplicateCheck();
        }

        private Task CheckSerialNumberAsync()
        {
            string sn = txtSerialNumber.Text.Trim();
            string lotNumber = txtLot.Text.Trim();

            _isDuplicateSN = false;
            _duplicateSNRow = -1;

            // SN ว่าง ให้แสดงสีปกติ
            if (string.IsNullOrWhiteSpace(sn))
            {
                txtSerialNumber.ForeColor = SystemColors.WindowText;
                txtSerialNumber.BackColor = SystemColors.Window;
                return Task.CompletedTask;
            }

            // ถ้ายังไม่เลือกโฟลเดอร์หรือกรอก LOT ไม่ครบ ให้แสดงสีปกติ
            if (string.IsNullOrWhiteSpace(lotNumber) || string.IsNullOrEmpty(ResultFolder) || !Directory.Exists(ResultFolder))
            {
                txtSerialNumber.ForeColor = SystemColors.WindowText;
                txtSerialNumber.BackColor = SystemColors.Window;
                return Task.CompletedTask;
            }

            string safeFileName = lotNumber.Replace("/", "-");
            _excelFilePath = Path.Combine(ResultFolder!, safeFileName + ".xlsx");
            txtExcelPath.Text = _excelFilePath;

            // ยังไม่มีไฟล์ LOT นี้ แสดงว่าไม่ซ้ำแน่นอน (ผ่านทันที เหมือน DEF-03N)
            if (!File.Exists(_excelFilePath))
            {
                _isDuplicateSN = false;
                _duplicateSNRow = -1;
                txtSerialNumber.ForeColor = Color.White;
                txtSerialNumber.BackColor = Color.SeaGreen;
                return Task.CompletedTask;
            }

            if (IsSerialNumberDuplicate(sn, lotNumber, out int dupRow))
            {
                _isDuplicateSN = true;
                _duplicateSNRow = dupRow;
                txtSerialNumber.ForeColor = Color.White;
                txtSerialNumber.BackColor = Color.Firebrick;
                LogToRx($"⚠️S/N:{sn} ซ้ำในระบบ! (ตำแหน่งแถว {dupRow})", Color.Orange);
            }
            else
            {
                _isDuplicateSN = false;
                _duplicateSNRow = -1;
                txtSerialNumber.ForeColor = Color.White;
                txtSerialNumber.BackColor = Color.SeaGreen;
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
        private void txtECN_TextChanged(object sender, EventArgs e)
        {
            if (_isProcessingBarcode) return;
            Properties.Settings.Default.SavedECN = txtECN.Text.Trim();
            Properties.Settings.Default.Save();
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
            if (_isProcessingBarcode) return;
            Properties.Settings.Default.SavedLot = txtLot.Text.Trim();
            Properties.Settings.Default.Save();
            ScheduleSnDuplicateCheck();
        }
        private void cmbLot_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbLot.SelectedItem == null || string.IsNullOrEmpty(ResultFolder)) return;

            string selectedLot = cmbLot.SelectedItem.ToString() ?? "";
            string safeFileName = selectedLot.Trim().Replace("/", "-");
            string filePath = Path.Combine(ResultFolder, safeFileName + ".xlsx");

            if (File.Exists(filePath))
            {
                LoadExcelToDataGrid(filePath);
            }
            else
            {
                string rawFilePath = Path.Combine(ResultFolder, selectedLot + ".xlsx");
                if (File.Exists(rawFilePath))
                {
                    LoadExcelToDataGrid(rawFilePath);
                }
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
            else
            {
                _historyExcelFilePath = null;
            }

            if (string.IsNullOrEmpty(_historyExcelFilePath) || !File.Exists(_historyExcelFilePath))
            {
                dataGridView1.DataSource = null;
                lblPassCount.Text = "0";
                lblFailCount.Text = "0";
                lblLotCount.Text = "กรุณาเลือก LOT เพื่อแสดงข้อมูล";
                return;
            }

            try
            {
                using var workbook = new XLWorkbook(_historyExcelFilePath);
                var sheet = workbook.Worksheet(1);

                string excelLotFromHeader = ReadLotFromHeader(sheet);
                string excelLotSizeFromHeader = ReadLotSizeFromHeader(sheet);
                string excelDateFromHeader = ReadDateFromHeader(sheet);

                var rows = new List<TestResultExcel>();
                int row = ExcelFirstTestRow;
                int lotCount = 0;
                int passCount = 0;
                int failCount = 0;

                string selectedLotOnUI = cmbLot.Text.Trim();
                int maxRow = FindSummaryHeaderRow(sheet) - 1;

                while (row <= maxRow)
                {
                    // 1. เช็กว่า Column A (คอลัมน์ 1) เป็นตัวเลขลำดับหรือไม่
                    bool isNumericRow = int.TryParse(
                        sheet.Cell(row, 1).GetString().Trim(),
                        out int rowNumber);

                    if (!isNumericRow)
                    {
                        row++;
                        continue;
                    }

                    // 🟢 2. เช็กว่าช่อง S/N (Column 28) มีข้อมูลการทดสอบจริงหรือไม่
                    string currentSn = sheet.Cell(row, 28).GetString().Trim();
                    bool isValidSN = !string.IsNullOrWhiteSpace(currentSn) && !IsPlaceholderSN(currentSn);

                    // 🟢 ถ้ายังไม่มี S/N (แถวว่างยังไม่ได้ Test) ให้ข้ามแถวนี้ไปเลย
                    if (!isValidSN)
                    {
                        row++;
                        continue;
                    }

                    // นับเฉพาะแถวที่มี S/N ทดสอบจริงแล้วเท่านั้น
                    lotCount++;

                    // ── ตรวจสถานะจาก 2 คอลัมน์แยกกัน: Z(26)=ปกติ, AA(27)=ไม่ปกติ ──
                    string zVal = sheet.Cell(row, 26).GetString().Trim();  // ปกติ
                    string aaVal = sheet.Cell(row, 27).GetString().Trim(); // ไม่ปกติ

                    string statusValue;
                    if (zVal == "✔" || zVal.Equals("OK", StringComparison.OrdinalIgnoreCase))
                    {
                        statusValue = "PASS";
                        passCount++;
                    }
                    else if (aaVal == "✖" || aaVal == "✘" || aaVal.Equals("OK", StringComparison.OrdinalIgnoreCase) || aaVal.Equals("FAIL", StringComparison.OrdinalIgnoreCase))
                    {
                        statusValue = "FAIL";
                        failCount++;
                    }
                    else
                    {
                        statusValue = "";
                    }

                    rows.Add(new TestResultExcel
                    {
                        No = rowNumber,                                    // A: ลำดับ
                        RelayLED = sheet.Cell(row, 3).GetString(),         // B
                        WL = sheet.Cell(row, 5).GetString(),               // C
                        WL_AL2 = sheet.Cell(row, 6).GetString(),           // D
                        HP = sheet.Cell(row, 8).GetString(),               // E
                        HP_AL2 = sheet.Cell(row, 9).GetString(),           // F
                        Alarm1 = sheet.Cell(row, 11).GetString(),          // G

                        hotFAN = sheet.Cell(row, 12).GetString(),          // H
                        coolFAN = sheet.Cell(row, 13).GetString(),         // H
                        Compressor = sheet.Cell(row, 14).GetString(),      // H

                        Ohm200 = sheet.Cell(row, 15).GetString(),          // I
                        Ohm2000 = sheet.Cell(row, 16).GetString(),         // J
                        Ohm8000 = sheet.Cell(row, 17).GetString(),         // K

                        test20 = sheet.Cell(row, 18).GetString(),
                        test30 = sheet.Cell(row, 19).GetString(),
                        test40 = sheet.Cell(row, 20).GetString(),
                        test50 = sheet.Cell(row, 21).GetString(),

                        BtnFunction = sheet.Cell(row, 22).GetString(),     // L
                        BtnDown = sheet.Cell(row, 23).GetString(),         // M
                        BtnUp = sheet.Cell(row, 24).GetString(),           // N
                        LedCheck = sheet.Cell(row, 25).GetString(),        // O
                        Status = statusValue,                              // Status
                        LOT = excelLotFromHeader,                          // Header
                        SerialNumber = currentSn                           // U: S/N
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
                                     .Where(name => !name.Equals("template", StringComparison.OrdinalIgnoreCase)
                                                 && !name.StartsWith("CMA-003 QC Inspection", StringComparison.OrdinalIgnoreCase)
                                                 && !name.Equals("CalReport_Template", StringComparison.OrdinalIgnoreCase))
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
        private bool IsPlaceholderSN(string sn)
        {
            if (string.IsNullOrWhiteSpace(sn)) return false;

            // จับ pattern แบบ "F-PD-07 Rev.6" หรือ "F-XX-00 Rev.X" ทั่วไป
            // ขึ้นต้นด้วย F- ตามด้วยตัวอักษร/ตัวเลข แล้วมีคำว่า "Rev" ต่อท้าย
            if (System.Text.RegularExpressions.Regex.IsMatch(
                    sn, @"^F-.*Rev\.?\s*\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return true;
            }

            // เผื่อกรณีเทียบตรงๆ กับค่าที่รู้แน่ชัดว่าเป็น placeholder ของเทมเพลตนี้
            if (sn.Equals("F-PD-07 Rev.6", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private string[] _tempSweepResults = new string[5] { "-", "-", "-", "-", "-" }; // index 0=10°C, 1=20°C, 2=30°C, 3=40°C, 4=50°C
        private void ResetTestResults()
        {
            ResetDescriptionForNewTest();
            // ล้างค่าผลการตรวจเช็คทางไฟฟ้า/สถานะ ให้เป็นค่าว่างหรือเครื่องหมายขีดก่อนเริ่มเทสตัวใหม่
            _RelayResult = "N/A";
            _wlResult = "N/A";
            _wl_AL2Result = "N/A";
            _hpResult = "N/A";
            _hp_AL2Result = "N/A";
            _alarm1Result = "N/A";
            _Compressor = "N/A";
            _HotFan = "N/A";          // ★ เพิ่ม
            _CoolFan = "N/A";         // ★ เพิ่ม

            _btnFunctionResult = "N/A";
            _btnDownResult = "N/A";
            _btnUpResult = "N/A";

            _ledResult = "N/A";

            // ส่วนค่าโอห์ม ถ้าไม่ได้เช็คต่อ อาจจะเซ็ตเป็น 0 หรือค่าว่างไว้ก่อน
            _verify200Result = "N/A";
            _verify2000Result = "N/A";
            _verify8000Result = "N/A";
            _verify20Result = "N/A";  // ★ เพิ่ม
            _verify30Result = "N/A";  // ★ เพิ่ม
            _verify40Result = "N/A";  // ★ เพิ่ม
            _verify50Result = "N/A";  // ★ เพิ่ม

            _register0FinalResult = "-";

            for (int i = 0; i < _tempSweepResults.Length; i++)
                _tempSweepResults[i] = "-";
        }
        private void LoadExistingResultsForSN(string sn, string lotNumber)
        {
            ResetTestResults();   // baseline "-" ทั้งหมดก่อน (เผื่อไม่เจอแถวเดิม)

            if (string.IsNullOrEmpty(lotNumber) || string.IsNullOrEmpty(ResultFolder)) return;

            string safeFileName = lotNumber.Replace("/", "-");
            string lotFilePath = Path.Combine(ResultFolder, safeFileName + ".xlsx");

            if (!File.Exists(lotFilePath)) return;

            try
            {
                using var workbook = new XLWorkbook(lotFilePath);
                var sheet = workbook.Worksheet(1);

                int summaryHeaderRow = FindSummaryHeaderRow(sheet);

                for (int row = ExcelFirstTestRow; row < summaryHeaderRow; row++)
                {
                    if (!IsExcelTestRow(sheet, row)) continue;

                    string existingSN = sheet.Cell(row, 28).GetString().Trim();
                    if (string.IsNullOrEmpty(existingSN) || IsPlaceholderSN(existingSN)) continue;

                    if (existingSN.Equals(sn, StringComparison.OrdinalIgnoreCase))
                    {
                        // ★ โหลดค่าเดิมกลับมา ยกเว้นช่องปุ่ม (จะถูกทับด้วยผลทดสอบรอบนี้)
                        _RelayResult = sheet.Cell(row, 3).GetString();
                        _wlResult = sheet.Cell(row, 5).GetString();
                        _wl_AL2Result = sheet.Cell(row, 6).GetString();
                        _hpResult = sheet.Cell(row, 8).GetString();
                        _hp_AL2Result = sheet.Cell(row, 9).GetString();
                        _alarm1Result = sheet.Cell(row, 11).GetString();
                        _HotFan = sheet.Cell(row, 12).GetString();
                        _CoolFan = sheet.Cell(row, 13).GetString();
                        _Compressor = sheet.Cell(row, 14).GetString();
                        _verify200Result = sheet.Cell(row, 15).GetString();
                        _verify2000Result = sheet.Cell(row, 16).GetString();
                        _verify8000Result = sheet.Cell(row, 17).GetString();
                        _verify20Result = sheet.Cell(row, 18).GetString();
                        _verify30Result = sheet.Cell(row, 19).GetString();
                        _verify40Result = sheet.Cell(row, 20).GetString();
                        _verify50Result = sheet.Cell(row, 21).GetString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogToRx($"โหลดผลเดิมของ S/N {sn} ไม่สำเร็จ: {ex.Message}", Color.Red);
            }
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

            var matchedLots = new List<(string DisplayName, string FilePath)>();

            var files = Directory.GetFiles(ResultFolder, "*.xlsx")   // เปลี่ยนจาก resultFolder ตัวแปรเดิม
                                  .Where(f => !Path.GetFileName(f).StartsWith("~$"))
                                  .Where(f => !Path.GetFileNameWithoutExtension(f).Equals("template", StringComparison.OrdinalIgnoreCase)
                                           && !Path.GetFileNameWithoutExtension(f).StartsWith("CMA-003 QC Inspection", StringComparison.OrdinalIgnoreCase)
                                           && !Path.GetFileNameWithoutExtension(f).Equals("CalReport_Template", StringComparison.OrdinalIgnoreCase));

            foreach (var filePath in files)
            {
                try
                {
                    using var workbook = new XLWorkbook(filePath);
                    var sheet = workbook.Worksheet(1);

                    var dateCell = FindDateCell(sheet) ?? sheet.Cell("AB3");
                    if (TryGetCellDate(dateCell, out DateTime lotDate) && lotDate.Date == targetDate.Date)
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

            // ★ เพิ่ม: parse รูปแบบ "d/M/yy" ที่ SaveTestDataToExcel ใช้จริง (ปีพุทธ 2 หลัก เช่น "27/8/69")
            var parts = text.Split('/');
            if (parts.Length == 3
                && int.TryParse(parts[0], out int day)
                && int.TryParse(parts[1], out int month)
                && int.TryParse(parts[2], out int buddhistYear2Digit))
            {
                int gregorianYear = (2500 + buddhistYear2Digit) - 543;

                try
                {
                    date = new DateTime(gregorianYear, month, day);
                    return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // day/month ไม่ valid ตกไป fallback ด้านล่าง
                }
            }

            return DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
        private void cmbDay_SelectedIndexChanged_2(object? sender, EventArgs e) { OnDateFilterChanged(); }
        private void cmbMonth_SelectedIndexChanged_2(object? sender, EventArgs e) { OnDateFilterChanged(); }
        private void cmbYear_SelectedIndexChanged_2(object? sender, EventArgs e) { OnDateFilterChanged(); }



        #endregion

        #region 14 - QC Payload และการสื่อสารกับ Server API

        //-- ส่วนของการส่ง API สำหรัยส่งข้อมูลไปเก็บไว้ที่ DATABASE
        private static double? ParseQcMeasuredValue(string measuredText)
        {
            if (string.IsNullOrWhiteSpace(measuredText) 
                || measuredText.Trim() == "-" 
                || measuredText.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            const NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands;
            if (double.TryParse(measuredText, styles, CultureInfo.CurrentCulture, out double currentCultureValue))
                return currentCultureValue;

            if (double.TryParse(measuredText, styles, CultureInfo.InvariantCulture, out double invariantValue))
                return invariantValue;

            return null;
        }

        private static string GetQcTextResult(string measuredText, bool isPending = false)
        {
            if (string.IsNullOrWhiteSpace(measuredText) 
                || measuredText.Trim() == "-" 
                || measuredText.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase))
            {
                return isPending ? "PENDING" : "SKIP";
            }

            return string.Equals(measuredText.Trim(), "OK", StringComparison.OrdinalIgnoreCase)
                || measuredText.Trim() == "✔"
                || string.Equals(measuredText.Trim(), "PASS", StringComparison.OrdinalIgnoreCase)
                ? "PASS"
                : "FAIL";
        }

        private static string GetQcNumericResult(double? measuredValue, double minValue, double maxValue, bool isPending = false)
        {
            if (!measuredValue.HasValue)
            {
                return isPending ? "PENDING" : "SKIP";
            }

            return measuredValue.Value >= minValue && measuredValue.Value <= maxValue
                ? "PASS"
                : "FAIL";
        }

        private static string GetQcMeasuredText(double? measuredValue, string originalText)
        {
            return measuredValue.HasValue
                ? measuredValue.Value.ToString("F1", CultureInfo.InvariantCulture)
                : (string.IsNullOrWhiteSpace(originalText) || originalText.Trim() == "-" || originalText.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase) ? "N/A" : originalText.Trim());
        }

        private QcPayload BuildQcPayload(string serialNumber, string lotNumber)
        {
            double? val200 = ParseQcMeasuredValue(_verify200Result);
            double? val8000 = ParseQcMeasuredValue(_verify8000Result);
            double? val2000 = ParseQcMeasuredValue(_verify2000Result);
            double? val2435 = ParseQcMeasuredValue(_verify20Result);
            double? val1653 = ParseQcMeasuredValue(_verify30Result);
            double? val1150 = ParseQcMeasuredValue(_verify40Result);
            double? val818 = ParseQcMeasuredValue(_verify50Result);

            DateTime inspectionEnd = _inspectionEndTime ?? DateTime.Now;
            DateTime inspectionStart = _inspectionStartTime ?? inspectionEnd;
            string startIso = new DateTimeOffset(inspectionStart).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
            string endIso = new DateTimeOffset(inspectionEnd).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
            string reportDate = inspectionEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // ถ้ารอบนี้จบแบบไม่ได้ PASS (เช่น ติดขัด/เกิด Error/หยุดกลางคัน) รายการที่ยังไม่มีผลจะถือเป็น PENDING (FAIL)
            bool isPending = !string.Equals(_lastTestStatus, "PASS", StringComparison.OrdinalIgnoreCase);

            var items = new List<QcItem>
            {
                CreateQcTextItem("CMA003_INPUT_2000_OHM", "จ่าย Input 2000 Ohm", _RelayResult, GetQcTextResult(_RelayResult, isPending), new List<string> { "จ่าย Input 2000 Ohm" }, offsetX: 1),
                CreateQcTextItem("CMA003_WL", "WL", _wlResult, GetQcTextResult(_wlResult, isPending), new List<string> { "WL" }, offsetX: 1),
                CreateQcTextItem("CMA003_WL_AL2", "WL/Al2", _wl_AL2Result, GetQcTextResult(_wl_AL2Result, isPending), new List<string> { "WL/Al2" }, offsetX: 0),
                CreateQcTextItem("CMA003_HP", "HP", _hpResult, GetQcTextResult(_hpResult, isPending), new List<string> { "HP" }, offsetX: 1),
                CreateQcTextItem("CMA003_HP_AL2", "HP/Al2", _hp_AL2Result, GetQcTextResult(_hp_AL2Result, isPending), new List<string> { "HP/Al2" }, offsetX: 0),
                CreateQcTextItem("CMA003_ALARM1", "Alarm1", _alarm1Result, GetQcTextResult(_alarm1Result, isPending), new List<string> { "Alarm1" }, offsetX: 1),

                CreateQcTextItem("CMA003_FAN_HOT", "Fan / Hot", _HotFan, GetQcTextResult(_HotFan, isPending), new List<string> { "Fan", "Hot" }, offsetX: 0),
                CreateQcTextItem("CMA003_FAN_COOL", "Fan / Cool", _CoolFan, GetQcTextResult(_CoolFan, isPending), new List<string> { "Fan", "Cool" }, offsetX: 0),
                CreateQcTextItem("CMA003_COMPRESSOR", "Compressor", _Compressor, GetQcTextResult(_Compressor, isPending), new List<string> { "Compressor" }, offsetX: 0),

                CreateQcNumericItem("CMA003_200_99", "จ่าย 200 / 99", val200, _verify200Result, 98.0, 100.0, new List<string> { "จ่าย 200", "99" }, isPending: isPending),
                CreateQcNumericItem("CMA003_2000_25_6", "จ่าย 2000 / 25.6", val2000, _verify2000Result, 24.6, 26.6, new List<string> { "จ่าย 2000", "25.6" }, isPending: isPending),
                CreateQcNumericItem("CMA003_8000_M6_4", "จ่าย 8000 / -6.4", val8000, _verify8000Result, -7.4, -5.4, new List<string> { "จ่าย 8000", "-6.4" }, isPending: isPending),

                CreateQcNumericItem("CMA003_2435_20", "จ่าย 2435 / 20", val2435, _verify20Result, 19.0, 21.0, new List<string> { "จ่าย 2435", "20" }, isPending: isPending),
                CreateQcNumericItem("CMA003_1653_30", "จ่าย 1653 / 30", val1653, _verify30Result, 29.0, 31.0, new List<string> { "จ่าย 1653", "30" }, isPending: isPending),
                CreateQcNumericItem("CMA003_1150_40", "จ่าย 1150 / 40", val1150, _verify40Result, 39.0, 41.0, new List<string> { "จ่าย 1150", "40" }, isPending: isPending),
                CreateQcNumericItem("CMA003_818_50", "จ่าย 818 / 50", val818, _verify50Result, 49.0, 51.0, new List<string> { "จ่าย 818", "50" }, isPending: isPending),

                CreateQcTextItem("CMA003_F", "ปุ่ม F", _btnFunctionResult, GetQcTextResult(_btnFunctionResult, isPending), new List<string> { "ปุ่ม F" }, offsetX: 0),
                CreateQcTextItem("CMA003_ITEM_018", "ปุ่ม ลด", _btnDownResult, GetQcTextResult(_btnDownResult, isPending), new List<string> { "ปุ่ม ลด" }, offsetX: 0),
                CreateQcTextItem("CMA003_ITEM_019", "ปุ่ม เพิ่ม", _btnUpResult, GetQcTextResult(_btnUpResult, isPending), new List<string> { "ปุ่ม เพิ่ม" }, offsetX: 0),
                CreateQcTextItem("CMA003_LED", "LED ติดครบ", _ledResult, GetQcTextResult(_ledResult, isPending), new List<string> { "LED ติดครบ" }, offsetX: 0)
            };

            // หากมีรายการใดที่เป็น FAIL หรือ PENDING (ยังเทสไม่ถึง) จะทำให้ overallResult เป็น FAIL ทันที
            bool hasFailOrPending = items.Any(i => string.Equals(i.Result, "FAIL", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(i.Result, "PENDING", StringComparison.OrdinalIgnoreCase));
            string overallResult = hasFailOrPending ? "FAIL" : (string.Equals(_lastTestStatus, "PASS", StringComparison.OrdinalIgnoreCase) ? "PASS" : "FAIL");
            string remark = overallResult == "PASS" ? "All 7 temperatures and functional items passed" : "Inspection failed or incomplete";

            return new QcPayload
            {
                SchemaVersion = 2,
                UpdateMode = false,
                ModelCode = "CMA-003",
                LotNumber = lotNumber,
                EcnNumber = txtECN.Text.Trim(),
                SerialNumber = serialNumber,
                StationName = "QC-STATION-01",
                OperatorUsername = string.IsNullOrWhiteSpace(_operatorUsername) ? "admin" : _operatorUsername,
                StartTime = startIso,
                InspectionTime = endIso,
                TemplateName = "CMA-003 QC Inspection.xlsx / Sheet1",
                Description = GetDescriptionForPayload(),
                Firmware = new List<QcFirmware>
                {
                    new QcFirmware { Name = "AirControl", Version = _firmwareValue },
                    new QcFirmware { Name = "MCU_DISPLAY_AIR_CONTROL", Version = _displayFirmwareValue }
                },
                FirmwareMain = _firmwareValue,
                FirmwareDisplay = _displayFirmwareValue,
                PowerSupply = "220 VAC 50 Hz",
                EquipmentCodes = GetEquipmentCodesForPayload(),
                Approve = BuildQcApproval(inspectionEnd),
                Remark = remark,
                Uuid = Guid.NewGuid().ToString(),
                InspectionStatus = "final",
                ProductName = "CMA-003 QC Inspection",
                ReportSerialNumber = serialNumber,
                JigName = "CMA-003-JIG-01",
                ReportDate = reportDate,
                OverallResult = overallResult,
                AcceptanceChecks = BuildCmaAcceptanceChecks(),
                Items = items,
                ReportMapping = BuildCmaReportMapping(),
                Images = new List<object>()
            };
        }

        private async Task<ServerPostResult> PostResultAsync(string testStatus)
        {
            string serialNumber = txtSerialNumber.Text.Trim();
            string lotNumber = txtLot.Text.Trim();
            string apiKey = txt_token.Text.Trim();

            if (string.IsNullOrEmpty(serialNumber))
                return ServerPostResult.Failed;

            if (!TryGetQcResultApiUrl(out string apiUrl))
                return ServerPostResult.Failed;

            if (apiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                apiKey = apiKey.Substring("Bearer ".Length).Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                BigMessageBox.Show(
                    "กรุณากรอก Device Token (API Key) ในช่อง Token ก่อนส่งข้อมูลขึ้นระบบ!",
                    "Warning",
                    MessageBoxIcon.Warning,
                    MessageBoxButtons.OK,
                    fontSize: 14f);
                txt_token.Focus();
                return ServerPostResult.Failed;
            }

            if (string.IsNullOrWhiteSpace(_operatorToken))
            {
                BigMessageBox.Show(
                    "กรุณากดปุ่ม Login เพื่อเข้าสู่ระบบก่อนส่งข้อมูลขึ้นระบบ!",
                    "Warning",
                    MessageBoxIcon.Warning,
                    MessageBoxButtons.OK,
                    fontSize: 14f);
                button1.Focus();
                return ServerPostResult.Failed;
            }

            QcPayload payload = BuildQcPayload(serialNumber, lotNumber);
            string jsonBody = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.Indented
            });
            try
            {
                lblResultStatus.Text = "⏳ กำลังส่งผลบันทึกไปฐานข้อมูล...";
                lblResultStatus.ForeColor = Color.Orange;

                ///RxBox.AppendText($"\r\n[DEBUG JSON] {jsonBody}\r\n");

                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Headers.TryAddWithoutValidation("X-API-KEY", apiKey);
                request.Headers.TryAddWithoutValidation("X-OPERATOR-TOKEN", _operatorToken);
                request.Content = content;
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using HttpResponseMessage response = await client.SendAsync(request, timeout.Token);

                if (response.IsSuccessStatusCode)
                {
                    string responseText = await response.Content.ReadAsStringAsync();

                    //RxBox.AppendText($"\r\n[API SUCCESS] {responseText}\r\n");
                    //RxBox.AppendText($"\r\n✅ อัปโหลด S/N: {serialNumber} เรียบร้อย!");

                    lblResultStatus.Text = $"✅ อัปโหลด S/N: {serialNumber} เรียบร้อย!";
                    lblResultStatus.ForeColor = Color.Green;
                    return ServerPostResult.Success;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    lblResultStatus.Text = $"❌ Error: {response.StatusCode}";
                    lblResultStatus.ForeColor = Color.Red;
                    RxBox.AppendText(
                        $"\r\n[AUTO REQUEST FAILED]\r\n{jsonBody}" +
                        $"\r\n[API ERROR {(int)response.StatusCode}]\r\n{errorResponse}\r\n");
                    return ServerPostResult.Failed;
                }
            }
            catch (OperationCanceledException)
            {
                lblResultStatus.Text = "⚠ ส่ง Server ไม่สำเร็จ: ไม่ตอบกลับภายใน 10 วินาที";
                lblResultStatus.ForeColor = Color.DarkRed;
                LogToRx("[SERVER TIMEOUT] Server ไม่ตอบกลับภายใน 10 วินาที", Color.Red);
                RxBox.AppendText($"\r\n[AUTO REQUEST TIMEOUT]\r\n{jsonBody}\r\n");
                return ServerPostResult.Timeout;
            }
            catch (Exception ex)
            {
                lblResultStatus.Text = "⚠ ไม่สามารถเชื่อมต่อเน็ตเวิร์กได้";
                lblResultStatus.ForeColor = Color.DarkRed;
                RxBox.AppendText(
                    $"\r\n[AUTO REQUEST EXCEPTION]\r\n{jsonBody}" +
                    $"\r\n[NETWORK EXCEPTION]\r\n{ex.Message}\r\n");
                return ServerPostResult.Failed;
            }
        }
        private async void btnPost_Click(object sender, EventArgs e)
        {
            string serialNumber = txtSerialNumber.Text.Trim();
            if (string.IsNullOrEmpty(serialNumber))
            {
                BigMessageBox.Show("กรุณาสแกน Serial Number ก่อนส่งข้อมูลขึ้นระบบ!", "Warning",
                    MessageBoxIcon.Warning, MessageBoxButtons.OK, fontSize: 14f);
                return;
            }
            await PostResultAsync(_lastTestStatus);
        }

        private List<TestResultRow>? _lastDatabaseData = null;
        private async void btnGet_Click(object sender, EventArgs e)
        {
            MassagerBox.Clear();

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



        #endregion

        #region 15 - LED และสถานะความคืบหน้า

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

        private void UpdateDeviceStatusLabel(Label statusLabel, bool isReady)
        {
            statusLabel.Text = isReady ? "Ready" : "OFF";
            statusLabel.ForeColor = isReady
                ? Color.FromArgb(46, 125, 50)
                : Color.FromArgb(65, 67, 27);
        }

        private void ResetDeviceStatusLabels()
        {
            UpdateDeviceStatusLabel(status_WL, false);
            UpdateDeviceStatusLabel(status_HP, false);
            UpdateDeviceStatusLabel(status_Compressor, false);
            UpdateDeviceStatusLabel(status_HotFan, false);
            UpdateDeviceStatusLabel(status_CoolFan, false);
        }

        private enum StepState { Waiting, Running, Pass, Fail }
        private PictureBox[] _stepLeds = Array.Empty<PictureBox>();
        private void InitStepLeds()
        {
            _stepLeds = new PictureBox[]
            {
                picCal,picStep1, picStep2, picStep3,
                picStep4, picStep5, picStep6
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
        private void CacheRegisTextBoxes()
        {
            for (int i = 0; i <= 26; i++)
            {
                Control[] found = this.Controls.Find("Regis" + i, true);
                if (found.Length > 0 && found[0] is TextBox txt)
                    _regisTextBoxCache[i] = txt;
            }
        }




        #endregion

        #region 16 - การ Scale UI ตามขนาดหน้าจอ

        //-- ส่วนของการ Scale UI ตามขนาดหน้าจอ
        private Size _baseClientSize;
        private readonly Dictionary<Control, Rectangle> _originalBounds = new Dictionary<Control, Rectangle>();
        private readonly Dictionary<Control, float> _originalFontSize = new Dictionary<Control, float>();
        private bool _isScaling = false;
        private void InitAutoScale()
        {
            _baseClientSize = this.ClientSize;   // จำขนาดตอนเปิดโปรแกรมครั้งแรกไว้เป็น baseline
            CacheOriginalLayout(this);
            this.Resize += Form1_AutoScale_Resize;
        }
        private void CacheOriginalLayout(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                _originalBounds[c] = c.Bounds;
                _originalFontSize[c] = c.Font.Size;

                if (c.HasChildren)
                    CacheOriginalLayout(c);
            }
        }
        private const float ScalePadding = 1.0f;
        private void Form1_AutoScale_Resize(object? sender, EventArgs e)
        {
            if (_isScaling) return;
            if (_baseClientSize.Width <= 0 || _baseClientSize.Height <= 0) return;
            if (this.WindowState == FormWindowState.Minimized) return;

            _isScaling = true;
            try
            {
                float scaleX = (float)this.ClientSize.Width / _baseClientSize.Width;
                float scaleY = (float)this.ClientSize.Height / _baseClientSize.Height;
                float scale = Math.Min(scaleX, scaleY) * ScalePadding;   // ★ เพิ่ม * ScalePadding ตรงนี้

                int scaledWidth = (int)(_baseClientSize.Width * scale);
                int scaledHeight = (int)(_baseClientSize.Height * scale);
                int offsetX = (this.ClientSize.Width - scaledWidth) / 2;
                int offsetY = (this.ClientSize.Height - scaledHeight) / 2;

                this.SuspendLayout();
                ApplyScale(this, scale, offsetX, offsetY);
                this.ResumeLayout(true);
            }
            finally
            {
                _isScaling = false;
            }
        }
        private void ApplyScale(Control parent, float scale, int offsetX, int offsetY)
        {
            foreach (Control c in parent.Controls)
            {
                if (_originalBounds.TryGetValue(c, out Rectangle orig))
                {
                    // ส่วน control ที่ซ้อนอยู่ใน panel ไม่ต้องบวก offset ซ้ำ เพราะตำแหน่งเป็น relative ต่อ parent panel อยู่แล้ว
                    int x = (int)(orig.X * scale);
                    int y = (int)(orig.Y * scale);

                    if (parent == this)   // ★ เฉพาะชั้นบนสุดเท่านั้นที่ต้อง +offset เพื่อจัดกึ่งกลาง
                    {
                        x += offsetX;
                        y += offsetY;
                    }

                    c.Bounds = new Rectangle(x, y,
                        (int)(orig.Width * scale),
                        (int)(orig.Height * scale));
                }

                if (_originalFontSize.TryGetValue(c, out float origFontSize))
                {
                    float newSize = Math.Max(6f, origFontSize * scale);
                    c.Font = new Font(c.Font.FontFamily, newSize, c.Font.Style);
                }

                if (c.HasChildren)
                    ApplyScale(c, scale, offsetX, offsetY);   // ชั้นลึกกว่าไม่ใช้ offset ซ้ำ เพราะเช็ค parent == this ด้านบนแล้ว
            }
        }




        //-- for Debuck Calibration 
        //private void btnCalSave_Click(object sender, EventArgs e)
        //{
        //    // ถ้ากำลัง Calibrate อยู่ แล้วกดซ้ำ -> ให้สั่งหยุด
        //    if (btnCalSave.Text == "กำลังทดสอบ")
        //    {
        //        var confirmStop = BigMessageBox.Show("คุณต้องการหยุดกระบวนการ Calibration ใช่หรือไม่?", "Stop", MessageBoxIcon.Question, MessageBoxButtons.YesNo, fontSize: 14f);

        //        if (confirmStop == DialogResult.Yes)
        //        {
        //            if (backgroundWorkerOhm.IsBusy) backgroundWorkerOhm.CancelAsync();
        //            LogToRx("--- สั่งหยุดการทำงานโดยผู้ใช้ ---", Color.Red);
        //            btnCalSave.Text = "ทดสอบ";
        //            btnCalSave.Enabled = true;
        //            btnRun.Enabled = true;
        //        }
        //        return;
        //    }

        //    if (!_serialPort.IsOpen || !_ohmPort.IsOpen || !_DisPort.IsOpen)
        //    {
        //        BigMessageBox.Show("กรุณาเชื่อมต่อทุกพอร์ตให้ครบ", "Warning", MessageBoxIcon.Warning, MessageBoxButtons.OK, fontSize: 14f);
        //        return;
        //    }

        //    var confirm = BigMessageBox.Show("เริ่มกระบวนการ Calibration และบันทึก Excel ใช่หรือไม่?", "Confirm", MessageBoxIcon.Information, MessageBoxButtons.YesNo, fontSize: 14f);

        //    if (confirm == DialogResult.No) return;

        //    _currentMode = WorkerMode.CalAndSaveExcel;

        //    ResetAllSteps();
        //    _sequenceStep = 0;
        //    _calDone = false;
        //    Array.Clear(finalAdcArray, 0, finalAdcArray.Length);
        //    ClearRxLog();

        //    List<string> sequenceValues = new List<string>
        //{
        //"100", "150", "300", "600", "1000", "1200", "1600",
        //"1800", "2000", "2600", "4000", "6000", "10000", "30000"
        //};

        //    btnCalSave.Text = "Calibrating...";
        //    btnCalSave.Enabled = true; // เปิดให้กดซ้ำได้
        //    btnRun.Enabled = false;   // บล็อกปุ่ม Run เดิมไว้ป้องกันการกดซ้อน
        //    progressBar1.Value = 0;

        //    // สั่งให้ BackgroundWorker ตัวเดิมเป็นคนรัน (ค่า ADC จะอ่านได้ปกติเหมือนโหมดเดิม 100%)
        //    backgroundWorkerOhm.RunWorkerAsync(sequenceValues);
        //}
        #endregion

        #region 17 - การส่งออกผล Calibration และ Utility ท้ายไฟล์

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

                if (!File.Exists(filePath))
                {
                    if (!File.Exists(templatePath))
                    {
                        BigMessageBox.Show("ไม่พบไฟล์เทมเพลต:\n{templatePath}\n\nกรุณาตรวจสอบไฟล์นี้",
                            "Template Not Found", MessageBoxIcon.Error, MessageBoxButtons.OK, fontSize: 14f);
                        return;
                    }
                    File.Copy(templatePath, filePath);
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
                    BigMessageBox.Show("ตารางเทมเพลตเต็มแล้ว (บันทึกครบ 20 ครั้งแล้ว)!", "แจ้งเตือน", MessageBoxIcon.Information, MessageBoxButtons.OK, fontSize: 14f);
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
                BigMessageBox.Show($"ไม่สามารถบันทึกข้อมูลลงเทมเพลตได้: {ex.Message}", "Error", MessageBoxIcon.Error, MessageBoxButtons.OK, fontSize: 14f);
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

                BigMessageBox.Show("ตั้งค่าเรียบร้อยแล้ว!", "Success", MessageBoxIcon.Information, MessageBoxButtons.OK, fontSize: 14f);
            }
            catch (Exception ex)
            {
                BigMessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}", "Error", MessageBoxIcon.Error, MessageBoxButtons.OK, fontSize: 14f);
            }
            finally
            {
                // เปิดให้กดปุ่มได้อีกครั้ง
                btnSet.Enabled = true;
            }

        }




        //--ยกเลิกการทำงาน
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

        #endregion
    }
}
