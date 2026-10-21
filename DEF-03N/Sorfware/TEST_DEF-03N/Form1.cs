using ComboBox = System.Windows.Forms.ComboBox;
using TextBox = System.Windows.Forms.TextBox;
using Button = System.Windows.Forms.Button;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using ClosedXML.Excel;
using Microsoft.VisualBasic.Devices;
using System.ComponentModel;
using System.IO.Ports;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Control = System.Windows.Forms.Control;

namespace TEST_DEF_03N
{
    public partial class Form1 : Form
    {
        #region 01 ตัวแปรและสถานะของ Form1

        public SerialPort _serialPort;
        public SerialPort _ohmPort;

        private readonly object _lock = new object();
        private List<byte> rxBuffer = new List<byte>();
        private List<byte> ohmRxBuffer = new List<byte>();

        public SerialPort _relayPort;
        private List<byte> relayRxBuffer = new List<byte>();
        private TaskCompletionSource<short>? _pendingRelayReadTcs;

        private bool _isConnected = false;
        private bool _defPortConnected = false;
        private bool _ohmPortConnected = false;
        private bool _relayPortConnected = false;
        private bool IsAnyPortOpen => (_serialPort?.IsOpen == true) || (_ohmPort?.IsOpen == true) || (_relayPort?.IsOpen == true);

        private ushort lastRequestedStart = 0; // ตัวแปรเก็บค่าเริ่มต้นล่าสุด

        //-- สำหรับการเขียน
        private string _originalText = string.Empty;
        private readonly Dictionary<string, string> _originalTextByControl = new Dictionary<string, string>();
        private readonly HashSet<string> _dirtyRegis = new HashSet<string>();// รีจิสเตอร์ที่เปลี่ยนแปลง
        private bool _isWriting = false; // ธงเช็คสถานะ: true = กำลังมีคำสั่งเขียนวิ่งอยู่

        private System.Diagnostics.Stopwatch _calStopwatch = new System.Diagnostics.Stopwatch();

        private const ushort PvRegister = 0;
        private const ushort SvRegister = 1;
        private const ushort DecimalPointRegister = 5;
        private const ushort RawAdcRegister = 26;
        private const int AdcTestSampleCount = 100;
        private const int AdcTestSampleIntervalMs = 100;

        //--
        private const byte CalibrationSlaveId = 0x01;
        private const byte CalibrationFunction = 0x55;

        private readonly SemaphoreSlim _calProtocolGate = new SemaphoreSlim(1, 1);

        private TaskCompletionSource<CalibrationAck>? _pendingCalAckTcs;
        private byte _pendingCalCommand;
        private byte _pendingCalSensor;

        private sealed class CalibrationAck
        {
            public byte Command { get; }
            public byte Sensor { get; }
            public byte Status { get; }
            public byte CompletedMask { get; }

            public CalibrationAck(
                byte command,
                byte sensor,
                byte status,
                byte completedMask)
            {
                Command = command;
                Sensor = sensor;
                Status = status;
                CompletedMask = completedMask;
            }
        }


        private sealed class ManualRtPoint
        {
            public double Temperature { get; set; }
            public double Resistance { get; set; }

            public ManualRtPoint(double temperature, double resistance)
            {
                Temperature = temperature;
                Resistance = resistance;
            }
        }

        private Form? _manualInputDropDown;
        private bool _isUpdatingManualValues;


        private volatile bool _writeDebugEnabled;

        // ป้องกันการเลือก Model ซ้ำระหว่างเขียน
        private bool _isChangingModelRegister;
        private string _lastConfirmedModel = string.Empty;

        // Register สำหรับกำหนด Option A/A-B
        private const ushort ModelOptionRegister = 23;

        private const int CommunicationFailureThreshold = 3;
        private static readonly TimeSpan RelayHealthCheckInterval = TimeSpan.FromSeconds(2);
        private int _mainPollTimeoutCount;
        private int _relayPollTimeoutCount;
        private DateTime _nextRelayHealthCheckUtc = DateTime.MinValue;
        private bool _disconnectInProgress;
        private bool _runSequenceActive;
        private bool _runWaitingForButtonRetry;
        private bool _testFailureMessageShown;
        private readonly List<string> _currentRunFailures = new List<string>();

        private void ClearRunFailures()
        {
            _currentRunFailures.Clear();
        }

        private void RecordRunFailure(string failureDetail)
        {
            if (string.IsNullOrWhiteSpace(failureDetail)) return;
            if (!_currentRunFailures.Contains(failureDetail))
            {
                _currentRunFailures.Add(failureDetail);
            }
            RecordAutomaticFailureReason(failureDetail);
        }
        private TaskCompletionSource<bool>? _pendingRunButtonRetryTcs;

        private readonly System.Windows.Forms.Timer _snCheckTimer = new System.Windows.Forms.Timer();
        private readonly Dictionary<Control, Color> _initialTextColors = new();

        private bool _isDuplicateSN = false;
        private int _duplicateSNRow = -1;
        private string _lastCheckedSN = string.Empty;

        private Color _defaultSnBackColor;
        private Color _defaultSnForeColor;
        private Color _defaultTemplatePathForeColor;

        #endregion

        #region 02 เริ่มต้นหน้าจอและโหลดการตั้งค่า

        public Form1()
        {
            InitializeComponent();
            _defaultSnBackColor = txt_SN.BackColor;
            _defaultSnForeColor = txt_SN.ForeColor;
            _defaultTemplatePathForeColor = txt_templatePath.ForeColor;
            InitializeServerAddressFeature();
            InitializeEquipmentAndApiFeature();
            InitializeLoginFeature();
            InitializeDescriptionFeature();
            InitializeHelpFeature();
            InitializeModelSelection();

            // ทำงานเฉพาะเมื่อผู้ใช้เลือก Model จาก ComboBox
            txt_model.SelectionChangeCommitted +=
                txt_model_SelectionChangeCommitted;

            _snCheckTimer.Interval = 400;

            _snCheckTimer.Tick += (s, e) =>
            {
                _snCheckTimer.Stop();
                CheckSNWhileTyping();
            };

            txt_SN.TextChanged += (s, e) => ScheduleSnDuplicateCheck();
            txt_Lot.TextChanged += (s, e) => ScheduleSnDuplicateCheck();
            txt_templatePath.TextChanged += (s, e) => ScheduleSnDuplicateCheck();

            _serialPort = new SerialPort();
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            string[] ports = SerialPort.GetPortNames();
            portDEF.Items.AddRange(ports);

            _ohmPort = new SerialPort();
            portOhm.Items.AddRange(ports);

            _relayPort = new SerialPort();
            _relayPort.DataReceived += new SerialDataReceivedEventHandler(RelayDataReceivedHandler);
            portRelay.Items.AddRange(ports);   // ⚠️ ต้องเพิ่ม ComboBox ชื่อ portRelay ใน Designer ก่อน

            // 🟢 ใส่ตรงนี้เลยครับ เปิดมาปุ๊บ สั่งเซ็ตค่ารอก่อนทันที
            Control[] foundTextBox1 = this.Controls.Find("Regis1", true);
            if (foundTextBox1.Length > 0)
            {
                if (foundTextBox1[0] is System.Windows.Forms.Label lbl1)
                {
                    lbl1.Text = "_°C";
                }
                else if (foundTextBox1[0] is System.Windows.Forms.TextBox txt1)
                {
                    txt1.Text = "_";
                    txt1.ForeColor = Color.Black; // สีดำตามที่ต้องการ
                }
            }

            Control[] foundTextBox26 = this.Controls.Find("Regis26", true);
            if (foundTextBox26.Length > 0)
            {
                if (foundTextBox26[0] is System.Windows.Forms.Label lbl28)
                {
                    lbl28.Text = "XXXX";
                }
                else if (foundTextBox26[0] is System.Windows.Forms.TextBox txt28)
                {
                    txt28.Text = "_";
                    txt28.ForeColor = Color.Black;
                }
            }


        }

        private void ResetSnColorsToDefault()
        {
            txt_SN.BackColor = _defaultSnBackColor;
            txt_SN.ForeColor = _defaultSnForeColor;
        }

        private void ScheduleSnDuplicateCheck()
        {
            ResetSnColorsToDefault();
            _isDuplicateSN = false;
            _duplicateSNRow = -1;
            _lastCheckedSN = string.Empty;

            _snCheckTimer.Stop();
            if (!string.IsNullOrWhiteSpace(txt_SN.Text))
                _snCheckTimer.Start();
        }

        // ── ฟังก์ชันบันทึก Config (จำทั้ง Template Path และ LOT) ─────────────────
        private const string DefaultECN = "RD-CN-24-6-0121";
        private const string DefaultRev = "F-RD-07 Rev.6";
        private const string DefaultInstrument = "OS-276 หมดอายุ 3/2/27";
        private const string DefaultFirmware = "FW DEF_03_c_V3.d43";

        private string _currentRevision = DefaultRev;
        private string _currentInstrument = DefaultInstrument;
        private string _currentFirmware = DefaultFirmware;

        private string GetOhmSourceInstrumentText()
        {
            var ohmRecord = _equipmentDatabaseRecords.FirstOrDefault(r => r.Name.Contains("Ohm", StringComparison.OrdinalIgnoreCase))
                            ?? _equipmentDatabaseRecords.FirstOrDefault();
            if (ohmRecord != null && !string.IsNullOrWhiteSpace(ohmRecord.EquipmentId))
            {
                if (!string.IsNullOrWhiteSpace(ohmRecord.ExpireDate) && ohmRecord.ExpireDate != "-")
                    return $"{ohmRecord.EquipmentId} หมดอายุ {ohmRecord.ExpireDate}";
                return ohmRecord.EquipmentId;
            }
            return _currentInstrument;
        }

        private void SaveConfig()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "path_config.txt");
                string[] lines = new string[]
                {
                    txt_templatePath.Text.Trim(),
                    txt_Lot.Text.Trim(),
                    txt_ECN.Text.Trim(),
                    _currentRevision.Trim(),
                    GetOhmSourceInstrumentText().Trim(),
                    _currentFirmware.Trim(),
                    txt_name.Text.Trim()
                };
                File.WriteAllLines(configPath, lines);
            }
            catch { }
        }
        private void LoadConfig()
        {
            txt_name.PlaceholderText = "กรุณาลงชื่อ";
            txt_name.TextChanged += (s, e) => SaveConfig();

            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "path_config.txt");

                if (File.Exists(configPath))
                {
                    string[] lines = File.ReadAllLines(configPath);

                    _currentRevision = (lines.Length > 3 && !string.IsNullOrWhiteSpace(lines[3]))
                                     ? lines[3].Trim()
                                     : DefaultRev;

                    _currentInstrument = (lines.Length > 4 && !string.IsNullOrWhiteSpace(lines[4]))
                        ? lines[4].Trim()
                        : DefaultInstrument;

                    _currentFirmware = (lines.Length > 5 && !string.IsNullOrWhiteSpace(lines[5]))
                        ? lines[5].Trim()
                        : DefaultFirmware;

                    if (lines.Length > 6 && !string.IsNullOrWhiteSpace(lines[6]))
                    {
                        txt_name.Text = lines[6].Trim();
                    }

                    if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]))
                    {
                        string savedPath = lines[0].Trim();
                        txt_templatePath.Text = savedPath;

                        if (Directory.Exists(savedPath))
                            txt_templatePath.ForeColor = _defaultTemplatePathForeColor;
                        else
                        {
                            txt_templatePath.ForeColor = Color.Red;
                            LogToRx($"⚠️ ไม่พบโฟลเดอร์ที่เคยบันทึกไว้: {savedPath} กรุณาเลือกโฟลเดอร์ใหม่", Color.Orange);
                        }
                    }

                    if (lines.Length > 1)
                    {
                        txt_Lot.Text = lines[1].Trim();
                    }

                    // 🟢 บรรทัดที่ 3: ECN — ถ้ามีค่าที่เคย save ให้ใช้ค่านั้น ถ้าไม่มีใช้ default
                    txt_ECN.Text = (lines.Length > 2 && !string.IsNullOrWhiteSpace(lines[2]))
                        ? lines[2].Trim()
                        : DefaultECN;
                }
                else
                {
                    // ไม่มีไฟล์ config เลย (เปิดโปรแกรมครั้งแรก) ให้ตั้งค่า default ไว้ก่อน
                    txt_ECN.Text = DefaultECN;
                    _currentRevision = DefaultRev;
                    _currentInstrument = DefaultInstrument;
                    _currentFirmware = DefaultFirmware;
                }
            }
            catch (Exception ex)
            {
                txt_ECN.Text = DefaultECN;
                _currentRevision = DefaultRev;
                _currentInstrument = DefaultInstrument;
                _currentFirmware = DefaultFirmware;
                LogToRx($"⚠️ อ่านไฟล์ Config ไม่สำเร็จ: {ex.Message}", Color.Orange);
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            SetConnectionStatusUI(false);

            // 🟢 1. เรียกโหลดค่า Path โฟลเดอร์ และ LOT เป็นอันดับแรก
            LoadConfig();

            // 2. จัดการ TapPage และไฟสถานะตามเดิม
            stepTimeline?.ShowOverviewMode();
            tabcontrol.SelectedTab = tabPage6;

            for (int i = 0; i < 9; i++) SetStepLight(i, Color.LightGray, true);
            for (int i = 0; i < 3; i++) SetStepLight(i, Color.LightGray, false);
            ResetRunStepLights();

            // เก็บสีหลังโหลด Config และตั้งค่าหน้าแรกครบแล้ว เพื่อใช้ตอน Refresh
            CaptureInitialTextColors();
        }

        private void CaptureInitialTextColors()
        {
            _initialTextColors.Clear();

            void Capture(Control parent)
            {
                foreach (Control control in parent.Controls)
                {
                    _initialTextColors[control] = control.ForeColor;
                    if (control.HasChildren)
                        Capture(control);
                }
            }

            Capture(this);
        }

        private void RestoreInitialTextColors()
        {
            foreach (var item in _initialTextColors)
            {
                if (!item.Key.IsDisposed)
                    item.Key.ForeColor = item.Value;
            }
        }
        private void txt_Lot_Leave(object sender, EventArgs e)
        {
            SaveConfig();
        }
        private void txt_ECN_Leave(object sender, EventArgs e)
        {
            SaveConfig();
        }

        private bool _enableRunProgress;

        //── ส่วนของปุ่มควบคุม ──────────────────────────────
        #endregion

        #region 03 เชื่อมต่อและจัดการพอร์ต

        private async void btnCon_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_isConnected && !IsAnyPortOpen)
                {
                    _disconnectInProgress = false;
                    ResetCommunicationHealthCounters();

                    bool isDebug = chkDebugWrite != null && chkDebugWrite.Checked;

                    if (!isDebug)
                    {
                        if (portDEF.SelectedItem == null || portOhm.SelectedItem == null || portRelay.SelectedItem == null)
                        {
                            MessageBox.Show("กรุณาเลือก Port อุปกรณ์, Port Ohm Source และ Port Relay ให้ครบถ้วน!",
                                            "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    else
                    {
                        if (portDEF.SelectedItem == null && portOhm.SelectedItem == null && portRelay.SelectedItem == null)
                        {
                            MessageBox.Show("กรุณาเลือกอย่างน้อย 1 Port เพื่อเชื่อมต่อ (โหมด Debug)",
                                            "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    bool defOpened = false;
                    bool ohmOpened = false;
                    bool relayOpened = false;

                    //  ตั้งค่าและเปิด _serialPort (Modbus)
                    if (portDEF.SelectedItem != null && !string.IsNullOrWhiteSpace(portDEF.SelectedItem.ToString()))
                    {
                        try
                        {
                            _serialPort.PortName = portDEF.SelectedItem.ToString()!;
                            _serialPort.BaudRate = 19200;
                            //_serialPort.BaudRate = int.Parse(BuadBox.SelectedItem!.ToString()!);
                            _serialPort.Parity = Parity.None;
                            _serialPort.DataBits = 8;
                            _serialPort.StopBits = StopBits.One;
                            _serialPort.ReadTimeout = 500;
                            _serialPort.WriteTimeout = 500;
                            _serialPort.Open();
                            portDEF.Enabled = false;
                            defOpened = true;
                        }
                        catch (Exception ex)
                        {
                            if (!isDebug) throw;
                            LogToRx($"[DEBUG] ไม่สามารถเปิด Port DEF-03N ({portDEF.SelectedItem}): {ex.Message}", Color.OrangeRed);
                        }
                    }

                    //  ตั้งค่าและเปิด _ohmPort (Ohm Source)
                    if (portOhm.SelectedItem != null && !string.IsNullOrWhiteSpace(portOhm.SelectedItem.ToString()))
                    {
                        try
                        {
                            _ohmPort.PortName = portOhm.SelectedItem.ToString()!;
                            _ohmPort.BaudRate = 9600;
                            _ohmPort.Parity = Parity.None;
                            _ohmPort.DataBits = 8;
                            _ohmPort.StopBits = StopBits.One;
                            _ohmPort.Open();
                            portOhm.Enabled = false;
                            ohmOpened = true;
                        }
                        catch (Exception ex)
                        {
                            if (!isDebug) throw;
                            LogToRx($"[DEBUG] ไม่สามารถเปิด Port Ohm Source ({portOhm.SelectedItem}): {ex.Message}", Color.OrangeRed);
                        }
                    }

                    //  เปิด Relay Port
                    if (portRelay.SelectedItem != null && !string.IsNullOrWhiteSpace(portRelay.SelectedItem.ToString()))
                    {
                        try
                        {
                            _relayPort.PortName = portRelay.SelectedItem.ToString()!;
                            _relayPort.BaudRate = 19200;
                            _relayPort.Parity = Parity.None;
                            _relayPort.DataBits = 8;
                            _relayPort.StopBits = StopBits.One;
                            _relayPort.ReadTimeout = 500;
                            _relayPort.WriteTimeout = 500;
                            _relayPort.Open();
                            portRelay.Enabled = false;
                            relayOpened = true;
                        }
                        catch (Exception ex)
                        {
                            if (!isDebug) throw;
                            LogToRx($"[DEBUG] ไม่สามารถเปิด Port Relay ({portRelay.SelectedItem}): {ex.Message}", Color.OrangeRed);
                        }
                    }

                    if (!defOpened && !ohmOpened && !relayOpened)
                    {
                        MessageBox.Show("ไม่สามารถเปิดพอร์ตใดๆ ได้ กรุณาตรวจสอบการเชื่อมต่อ",
                                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    _defPortConnected = defOpened;
                    _ohmPortConnected = ohmOpened;
                    _relayPortConnected = relayOpened;
                    _isConnected = true;

                    timer1.Interval = 200;
                    timer1.Start();

                    btnConnect.Text = "🔗Disconnect";

                    var connectedList = new List<string>();
                    if (defOpened) connectedList.Add($"DEF-03N ({_serialPort.PortName})");
                    if (ohmOpened) connectedList.Add($"Ohm ({_ohmPort.PortName})");
                    if (relayOpened) connectedList.Add($"Relay ({_relayPort.PortName})");

                    LogToRx($"[CONNECTED] เชื่อมต่อสำเร็จ: {string.Join(", ", connectedList)}", Color.Green);
                    SetConnectionStatusUI(true); // 🟢 เปลี่ยน badge เป็นเขียว + "•Connect"
                }
                else
                {
                    if (backgroundWorkerOhm.IsBusy)
                    {
                        backgroundWorkerOhm.CancelAsync();
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

                    _isConnected = false;
                    _defPortConnected = false;
                    _ohmPortConnected = false;
                    _relayPortConnected = false;

                    if (_serialPort.IsOpen) _serialPort.Close();
                    if (_ohmPort.IsOpen) _ohmPort.Close();
                    if (_relayPort.IsOpen) _relayPort.Close();

                    btnConnect.Text = "🔗Connect";
                    LogToRx($"[DISCONNECTED] ยกเลิกการเชื่อมต่อสำเร็จ", Color.Red);

                    portDEF.Enabled = true;
                    portOhm.Enabled = true;
                    portRelay.Enabled = true;

                    SetConnectionStatusUI(false); // 🟢 กลับเป็นแดง + "•DisConnect"
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _defPortConnected = false;
                _ohmPortConnected = false;
                _relayPortConnected = false;

                // หากเกิด Error ให้พยายามปิดพอร์ตที่อาจค้างอยู่
                if (_serialPort.IsOpen) _serialPort.Close();
                if (_ohmPort.IsOpen) _ohmPort.Close();
                if (_relayPort.IsOpen) _relayPort.Close();

                portDEF.Enabled = true;
                portOhm.Enabled = true;
                portRelay.Enabled = true;

                SetConnectionStatusUI(false);
                MessageBox.Show($"Error during connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnRefresh_Click(object sender, EventArgs e)
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


                // ── 2. ล้าง Regis TextBox ────────────────────────────────────
                for (int i = 0; i <= 28; i++)
                {
                    Control[] found = this.Controls.Find("Regis" + i, true);
                    if (found.Length > 0)
                    {
                        // ตรวจสอบเงื่อนไขเฉพาะของแต่ละรอบ (i คือหมายเลขของ Regis นั้นๆ)
                        if (i == 0)
                        {
                            found[0].Text = "_" + "°C";
                            //found[0].ForeColor = Color.Red;
                        }
                        else if (i == 1)
                        {
                            found[0].Text = "_";
                            found[0].ForeColor = Color.Black;
                        }
                        else if (i == 26)
                        {
                            found[0].Text = "_";
                            found[0].ForeColor = Color.Black;
                        }
                        else
                        {
                            // Regis หมายเลขอื่นๆ 
                            found[0].ForeColor = Color.Black;
                        }
                    }
                }

                RestoreInitialTextColors();

                // ── 3. Reset UI controls ─────────────────────────────────────
                richTextBoxRx.Clear();

                for (int i = 0; i < 9; i++)
                {
                    SetStepLight(i, Color.LightGray, true); // true = ฝั่ง QA (pic_qa_step)
                }

                // 3. ล้างดวงไฟหน้า QC ทั้งหมด 3 จุด ให้กลับเป็นสีเทา (LightGray)
                for (int i = 0; i < 3; i++)
                {
                    SetStepLight(i, Color.LightGray, false); // false = ฝั่ง QC (pic_qc_step)
                }
                ResetRunStepLights();
                stepTimeline?.ShowOverviewMode();
                tabcontrol.SelectedTab = tabPage6;

                chkCalibration.Checked = false;
                chkTestForAll.Checked = false;
                chkMultiplyBy100.Checked = false;

                progressBarRun.Value = 0;
                progressBarRun.Maximum = 1;
                lblProgressPercent.Text = "0%";

                _manualPopupQAChecked = false;

                _isCalibrating = false;

                btnConnect.Text = "🔗Connect";
                //btnConnect.ResetBackColor();
                //btnConnect.UseVisualStyleBackColor = true;
                btnConnect.Enabled = true;

                SetConnectionStatusUI(false);
                _isConnected = false;
                _defPortConnected = false;
                _ohmPortConnected = false;
                _relayPortConnected = false;

                portDEF.Enabled = true;
                portOhm.Enabled = true;
                portRelay.Enabled = true;

                _calStopwatch.Reset();
                lblTimer.Text = "00:00";
                timerCal.Stop();

                Get_compoart_list();

                MessageBox.Show("ล้างพอร์ตเรียบร้อย", "Refresh",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnClearMessage_Click(object sender, EventArgs e)
        {
            richTextBoxRx.Clear();
        }


        //── ส่วนของการส่ง Modbus ──────────────────────────────
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            try
            {
                int count = _serialPort.BytesToRead;
                if (count == 0) return;

                byte[] buffer = new byte[count];
                _serialPort.Read(buffer, 0, count);

                //LogWriteDebug(
                //    $"[RAW RX] {BitConverter.ToString(buffer).Replace("-", " ")}",
                //        Color.OrangeRed);

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (byte b in buffer)
                {
                    sb.Append(b.ToString("X2") + " ");
                }

                LogWriteDebug($"[RAW RX] ได้รับข้อมูลดิบ: {sb.ToString()}", Color.OrangeRed);

                lock (_lock)
                {
                    rxBuffer.AddRange(buffer);
                }

                ParsePlainText(buffer);

                ParseModbusRTU();
            }
            catch { /* พอร์ตปิดระหว่างอ่าน — ไม่ต้องทำอะไร */ }
        }
        private void Get_compoart_list()
        {
            portDEF.Items.Clear();
            portOhm.Items.Clear();
            portRelay.Items.Clear();

            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM")) //เข้าไปอ่าน Registry ของ Windows path นี้:HARDWARE\DEVICEMAP\SERIALCOMM (เป็นที่เก็บรายชื่อ COM Port ทั้งหมดในเครื่อง)
            {
                if (key != null)
                {
                    foreach (var name in key.GetValueNames()) // วนลูปอ่านชื่อทั้งหมด ดึง “ชื่อของข้อมูล” ใน Registry key => GetValueNames() = เอา “ชื่อไฟล์” ในโฟลเดอร์
                    {
                        portDEF.Items.Add(key.GetValue(name)?.ToString() ?? "");
                        portOhm.Items.Add(key.GetValue(name)?.ToString() ?? "");
                        portRelay.Items.Add(key.GetValue(name)?.ToString() ?? "");
                    }
                }
            }

            portDEF.SelectedIndex = 0;
            portOhm.SelectedIndex = 0;
            portRelay.SelectedIndex = 0;

            portDEF.Text = "";
            portOhm.Text = "";
            portRelay.Text = "";

            if (portDEF.Items.Count == 0)
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

            if (_disconnectInProgress)
                return;

            _disconnectInProgress = true;

            timer1.Stop();

            _userRequestedStop = true;
            _ctsCal?.Cancel();
            _cts?.Cancel();
            _modelContactsCts?.Cancel();
            _pendingRunButtonRetryTcs?.TrySetResult(false);
            CloseParameterPopupOnDisconnect();

            // 1. ถ้าระบบหลักทำงานค้างอยู่ ให้สั่งยกเลิกทันที
            if (backgroundWorker1.IsBusy)
            {
                backgroundWorker1.CancelAsync();
            }

            if (backgroundWorkerOhm.IsBusy)
            {
                backgroundWorkerOhm.CancelAsync();
            }

            try { if (_serialPort?.IsOpen == true) _serialPort.Close(); } catch { }
            try { if (_ohmPort?.IsOpen == true) _ohmPort.Close(); } catch { }
            try { if (_relayPort?.IsOpen == true) _relayPort.Close(); } catch { }

            lock (_lock)
            {
                _pendingPollTcs?.TrySetCanceled();
                _pendingReadTcs?.TrySetCanceled();
                _pendingWriteTcs?.TrySetCanceled();
                _pendingRelayReadTcs?.TrySetCanceled();
                _pendingCalAckTcs?.TrySetCanceled();
                _pendingPollTcs = null;
                _pendingReadTcs = null;
                _pendingWriteTcs = null;
                _pendingRelayReadTcs = null;
                _pendingCalAckTcs = null;
                rxBuffer.Clear();
                relayRxBuffer.Clear();
                ohmRxBuffer.Clear();
            }

            ResetCommunicationHealthCounters();

            _isConnected = false;
            _defPortConnected = false;
            _ohmPortConnected = false;
            _relayPortConnected = false;

            btnConnect.Text = "🔗Connect";
            btnConnect.Enabled = true;

            portDEF.Enabled = true;
            portOhm.Enabled = true;
            portRelay.Enabled = true;

            ResetUiAfterConnectionLoss();

            if (reason != "UserDisconnect")
            {
                ResetUiAfterConnectionLoss();
                MessageBox.Show($"การเชื่อมต่อขาดหาย: {reason}", "Connection Lost",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _isWriting = false;
        }

        private void ResetCommunicationHealthCounters()
        {
            _mainPollTimeoutCount = 0;
            _relayPollTimeoutCount = 0;
            _nextRelayHealthCheckUtc = DateTime.UtcNow.Add(RelayHealthCheckInterval);
        }

        private void ResetUiAfterConnectionLoss()
        {


            for (int i = 0; i <= 28; i++)
            {
                Control[] found = Controls.Find("Regis" + i, true);
                if (found.Length == 0) continue;

                if (i == 0)
                    found[0].Text = "_°C";
                else if (i == 1 || i == 26)
                    found[0].Text = "_";

                found[0].ForeColor = Color.Black;
            }

            RestoreInitialTextColors();

            //richTextBoxRx.Clear();

            for (int i = 0; i < 9; i++)
                SetStepLight(i, Color.LightGray, true);

            for (int i = 0; i < 3; i++)
                SetStepLight(i, Color.LightGray, false);

            ResetRunStepLights();
            stepTimeline?.ShowOverviewMode();
            tabcontrol.SelectedTab = tabPage6;

            chkCalibration.Checked = false;
            chkTestForAll.Checked = false;
            chkMultiplyBy100.Checked = false;

            progressBarRun.Minimum = 0;
            progressBarRun.Maximum = 1;
            progressBarRun.Value = 0;
            lblProgressPercent.Text = "0%";

            _manualPopupQAChecked = false;
            _enableRunProgress = false;
            _isCalibrating = false;
            _currentPv = 0;
            _currentSV = 0;
            _lastKnownDp = 0;

            _calStopwatch.Reset();
            timerCal.Stop();
            lblTimer.Text = "00:00";

            SetAllButtonsEnabled(true);
            btnRun.Enabled = true;
            btnStop.Enabled = false;
            SetConnectionStatusUI(false);


            //try { Get_compoart_list(); } catch { }
        }
        #endregion


        #region 04 รับและประมวลผล Modbus ของ DEF-03N
        private void ParseModbusRTU()
        {
            lock (_lock)
            {
                while (rxBuffer.Count >= 5)
                {
                    if (rxBuffer[0] != CalibrationSlaveId)
                    {
                        rxBuffer.RemoveAt(0);
                        continue;
                    }


                    byte function = rxBuffer[1];
                    int frameLength;

                    if (function == 0x03)
                    {
                        frameLength = rxBuffer[2] + 5;
                    }
                    else if (function == 0x06)
                    {
                        frameLength = 8;
                    }
                    else if (function == CalibrationFunction)
                    {
                        if (rxBuffer.Count < 3)
                            break;

                        byte command = rxBuffer[2];

                        // Start ACK:
                        // 01 55 01 00 00 CRC_LO CRC_HI
                        if (command == 0x01)
                        {
                            frameLength = 8;
                        }
                        // Data / Commit / Cancel ACK:
                        // 01 55 CMD SENSOR STATUS MASK CRC_LO CRC_HI
                        else if (command == 0x02 ||
                                 command == 0x03 ||
                                 command == 0x04)
                        {
                            frameLength = 8;
                        }
                        else
                        {
                            rxBuffer.RemoveAt(0);
                            continue;
                        }
                    }
                    else
                    {
                        rxBuffer.RemoveAt(0);
                        continue;
                    }

                    if (frameLength < 5 || frameLength > 65)
                    {
                        rxBuffer.RemoveAt(0);
                        continue;
                    }

                    if (rxBuffer.Count < frameLength)
                        break;

                    byte[] frame =
                        rxBuffer.GetRange(0, frameLength).ToArray();

                    byte[] dataOnly =
                        frame.Take(frameLength - 2).ToArray();

                    byte[] expectedCrc = CalculateCRC(dataOnly);

                    bool crcOk =
                        frame[frameLength - 2] == expectedCrc[0] &&
                        frame[frameLength - 1] == expectedCrc[1];

                    if (!crcOk)
                    {
                        rxBuffer.RemoveAt(0);
                        continue;
                    }

                    rxBuffer.RemoveRange(0, frameLength);
                    ProcessFrame(frame);
                }
            }
        }
        private void ProcessFrame(byte[] frame)
        {
            byte slaveID = frame[0];

            UpdateMainBoardUI(frame);

        }

        private double _currentPv;
        private double _currentSV;
        private ushort _lastKnownDp = 0;
        private void UpdateMainBoardUI(byte[] frame)
        {
            if (frame == null || frame.Length < 2)
            {
                return;
            }

            byte function = frame[1];

            // =========================================================
            // Function 0x03: Read Holding Registers
            // =========================================================
            if (function == 0x03)
            {
                if (frame.Length < 5)
                {
                    return;
                }

                int byteCount = frame[2];

                // ตรวจสอบว่าข้อมูลใน Frame มีครบตาม Byte Count
                if (byteCount <= 0 || frame.Length < byteCount + 5)
                {
                    return;
                }

                int numRegs = byteCount / 2;
                ushort responseStart = numRegs == MainPollQuantity
                    ? MainPollStartAddress
                    : lastRequestedStart;

                // -----------------------------------------------------
                // ส่งค่ากลับให้เมธอดที่กำลังรออ่าน Register
                // -----------------------------------------------------
                lock (_lock)
                {
                    if (_pendingReadTcs != null && numRegs == 1 && frame[0] == _pendingReadSlaveId)
                    {
                        short readValue =
                            (short)((frame[3] << 8) | frame[4]);

                        _pendingReadTcs.TrySetResult(readValue);
                        _pendingReadTcs = null;
                    }

                    if (_pendingPollTcs != null &&
                        numRegs == MainPollQuantity &&
                        frame[0] == TargetID)
                    {
                        _pendingPollTcs.TrySetResult(true);
                        _pendingPollTcs = null;
                    }
                }

                // -----------------------------------------------------
                // อัปเดต Control บน UI Thread
                // -----------------------------------------------------
                if (IsDisposed || Disposing || !IsHandleCreated)
                {
                    return;
                }

                BeginInvoke(new MethodInvoker(() =>
                {
                    if (IsDisposed || Disposing)
                    {
                        return;
                    }

                    // อ่านค่า Register 5 จาก Frame ถ้ามีอยู่ใน Frame นี้
                    ushort register5Value = _lastKnownDp;

                    int register5Offset = 5 - responseStart;

                    if (register5Offset >= 0 &&
                        register5Offset < numRegs)
                    {
                        int register5HighByteIndex =
                            3 + (register5Offset * 2);

                        int register5LowByteIndex =
                            register5HighByteIndex + 1;

                        if (register5LowByteIndex < frame.Length - 2)
                        {
                            register5Value = (ushort)(
                                (frame[register5HighByteIndex] << 8) |
                                frame[register5LowByteIndex]);

                            _lastKnownDp = register5Value;
                        }
                    }

                    for (int i = 0; i < numRegs; i++)
                    {
                        int highByteIndex = 3 + (i * 2);
                        int lowByteIndex = highByteIndex + 1;

                        if (lowByteIndex >= frame.Length - 2)
                        {
                            break;
                        }

                        int actualRegister = responseStart + i;

                        short value = (short)(
                            (frame[highByteIndex] << 8) |
                            frame[lowByteIndex]);

                        // Popup พารามิเตอร์ใช้ข้อมูลจาก Polling ชุดเดียวกับหน้าหลัก
                        UpdateParameterPopupRegister((ushort)actualRegister, value);

                        // =================================================
                        // Register 0: PV Value
                        // =================================================
                        if (actualRegister == PvRegister)
                        {
                            if (register5Value == 1)
                            {
                                _currentPv = value / 10.0;
                            }
                            else
                            {
                                _currentPv = value;
                            }

                            Control[] foundRegister0 =
                                Controls.Find("Regis0", true);

                            if (foundRegister0.Length > 0)
                            {
                                if (foundRegister0[0] is Label label0)
                                {
                                    label0.Text = register5Value == 1
                                        ? _currentPv.ToString("F1") + "°C"
                                        : _currentPv.ToString("F0") + "°C";
                                }
                                else if (foundRegister0[0] is System.Windows.Forms.TextBox textBox0 &&
                                         !textBox0.Focused)
                                {
                                    textBox0.Text = register5Value == 1
                                        ? _currentPv.ToString("F1") + "°C"
                                        : _currentPv.ToString("F0") + "°C";

                                    textBox0.ForeColor = Color.Blue;
                                }
                            }
                        }

                        // =================================================
                        // Register 1: SV Value
                        // =================================================
                        else if (actualRegister == SvRegister)
                        {
                            _currentSV = value;

                            System.Windows.Forms.Control[] foundRegister1 =
                                Controls.Find("Regis1", true);

                            if (foundRegister1.Length > 0)
                            {
                                if (foundRegister1[0] is System.Windows.Forms.Label label1)
                                {
                                    label1.Text = _currentSV.ToString("0.#") + "°C";
                                }
                                else if (foundRegister1[0] is System.Windows.Forms.TextBox textBox1 &&
                                         !textBox1.Focused)
                                {
                                    textBox1.Text =
                                        _currentSV.ToString("0.#");

                                    textBox1.ForeColor = Color.Blue;
                                }
                            }
                        }

                        // =================================================
                        // Register 26: ADC Value แสดงเป็น Hex 4 หลัก
                        // =================================================
                        else if (actualRegister == RawAdcRegister)
                        {
                            Control[] foundRegister26 =
                                Controls.Find("Regis26", true);

                            if (foundRegister26.Length > 0)
                            {
                                ushort adcValue = (ushort)(
                                    (frame[highByteIndex] << 8) |
                                    frame[lowByteIndex]);

                                string hexValue =
                                    adcValue.ToString("X4");

                                if (foundRegister26[0] is Label label28)
                                {
                                    label28.Text = hexValue;
                                }
                                else if (foundRegister26[0] is System.Windows.Forms.TextBox textBox28 &&
                                         !textBox28.Focused)
                                {
                                    textBox28.Text = hexValue;
                                    textBox28.ForeColor = Color.Blue;
                                }
                            }
                        }

                        // =================================================
                        // TextBox Register อื่น ๆ
                        // =================================================
                        string targetControlName =
                            "Regis" + actualRegister;

                        Control[] foundControls =
                            Controls.Find(targetControlName, true);

                        if (foundControls.Length > 0 &&
                            foundControls[0] is System.Windows.Forms.TextBox registerTextBox &&
                            !registerTextBox.Focused)
                        {
                            switch (actualRegister)
                            {
                                case 0:
                                    registerTextBox.Text =
                                        register5Value == 1
                                            ? _currentPv.ToString("F1") + "°C"
                                            : _currentPv.ToString("F0") + "°C";

                                    registerTextBox.ForeColor = Color.Blue;
                                    break;

                                case 1:
                                    // Register 1 ถูกจัดการไว้ด้านบนแล้ว
                                    break;

                                case 26:
                                    // Register 28 ถูกแปลงเป็น Hex ไว้ด้านบนแล้ว
                                    // ห้ามเขียนค่าฐานสิบทับ
                                    break;

                                default:
                                    registerTextBox.Text = value.ToString();
                                    registerTextBox.ForeColor = Color.Blue;
                                    break;
                            }
                        }
                    }
                }));
            }

            // =========================================================
            // Function 0x06: Write Single Register Echo
            // =========================================================
            else if (function == 0x06)
            {
                if (frame.Length < 8)
                {
                    return;
                }

                ushort writtenAddress = (ushort)(
                    (frame[2] << 8) |
                    frame[3]);

                ushort writtenValue = (ushort)(
                    (frame[4] << 8) |
                    frame[5]);

                bool hadPendingWrite = false;
                bool matchedPendingWrite = false;

                lock (_lock)
                {
                    hadPendingWrite = _pendingWriteTcs != null;

                    if (_pendingWriteTcs != null &&
                        writtenAddress == _pendingWriteAddress &&
                        writtenValue == _pendingWriteValue)
                    {
                        matchedPendingWrite = true;

                        _pendingWriteTcs.TrySetResult(true);
                        _pendingWriteTcs = null;
                    }
                }

                if (matchedPendingWrite)
                {
                    _isWriting = false;
                }

                if (IsDisposed || Disposing || !IsHandleCreated)
                {
                    return;
                }

                BeginInvoke(new MethodInvoker(() =>
                {
                    if (matchedPendingWrite)
                    {
                        LogWriteDebug(
                            $"[WRITE OK] Register {writtenAddress} = {writtenValue}",
                            Color.DarkGreen);

                        // แสดงค่าที่เขียนสำเร็จลงช่อง Regis ทันที
                        string controlName = "Regis" + writtenAddress;

                        System.Windows.Forms.Control[] found =
                            Controls.Find(controlName, true);

                        if (found.Length > 0 &&
                            found[0] is System.Windows.Forms.TextBox textBox &&
                            !textBox.Focused)
                        {
                            short signedValue =
                                unchecked((short)writtenValue);

                            if (writtenAddress == 1)
                            {
                                textBox.Text = signedValue.ToString();
                            }
                            else if (writtenAddress == 26)
                            {
                                textBox.Text = writtenValue.ToString("X4");
                            }
                            else
                            {
                                textBox.Text = signedValue.ToString();
                            }

                            textBox.ForeColor = Color.Blue;
                        }
                    }
                    else if (hadPendingWrite)
                    {
                        LogToRx(
                            $"[ECHO MISMATCH] Register {writtenAddress} = " +
                            $"{writtenValue}",
                            Color.OrangeRed);
                    }
                }));
            }

            // =========================================================
            // Function 0x55: Response พิเศษของ Calibration
            // =========================================================
            else if (function == CalibrationFunction)
            {
                if (frame.Length < 7 ||
                    frame[0] != CalibrationSlaveId)
                {
                    return;
                }

                byte command = frame[2];
                byte sensor = frame[3];
                byte status = frame[4];
                byte completedMask =
                    frame.Length >= 8 ? frame[5] : (byte)0;

                var ack = new CalibrationAck(
                    command,
                    sensor,
                    status,
                    completedMask);

                TaskCompletionSource<CalibrationAck>? pending;
                byte waitingCommand;
                byte waitingSensor;

                lock (_lock)
                {
                    pending = _pendingCalAckTcs;
                    waitingCommand = _pendingCalCommand;
                    waitingSensor = _pendingCalSensor;
                }

                bool matched =
                    pending != null &&
                    command == waitingCommand &&
                    sensor == waitingSensor;

                BeginInvoke(new Action(() =>
                    LogWriteDebug(
                        $"[CAL ACK] CMD={command:X2}, " +
                        $"Sensor={sensor:X2}, Status={status:X2}, " +
                        $"Mask={completedMask:X2}, Matched={matched}",
                        matched ? Color.Green : Color.OrangeRed)));

                if (matched)
                    pending!.TrySetResult(ack);
            }
        }
        private void SendFrame(byte[] data)
        {
            lock (_lock)
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    HandleDisconnect("กรุณาเชื่อมต่อพอร์ต");
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

        private static byte[] BuildCalibrationControlFrame(byte command)
        {
            return new byte[]
            {
                CalibrationSlaveId,
                CalibrationFunction,
                command,
                0x00,
                0x02,
                0x7A,
                0x00
                    };
        }

        private static byte[] BuildCalibrationDataFrame(
            byte sensor,
            IReadOnlyList<ushort> adcValues)
        {
            if (adcValues.Count > byte.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(adcValues));

            byte[] frame = new byte[7 + (adcValues.Count * 2)];

            frame[0] = CalibrationSlaveId;
            frame[1] = CalibrationFunction;
            frame[2] = 0x02;
            frame[3] = sensor;
            frame[4] = 0x02;
            frame[5] = 0x7A;
            frame[6] = (byte)adcValues.Count;

            for (int i = 0; i < adcValues.Count; i++)
            {
                ushort adc = adcValues[i];

                // ADC 16-bit Big Endian
                frame[7 + (i * 2)] = (byte)(adc >> 8);
                frame[8 + (i * 2)] = (byte)(adc & 0xFF);
            }

            return frame;
        }
        private async Task<CalibrationAck> SendCalibrationFrameAsync(
            byte[] frameWithoutCrc,
            byte expectedCommand,
            byte expectedSensor,
            byte expectedMask,
            CancellationToken token,
            int maxAttempts = 3,
            int timeoutMs = 3000)
        {
            await _calProtocolGate.WaitAsync(token);
            Interlocked.Increment(ref _priorityCommandCount);

            try
            {
                await _mainReadGate.WaitAsync(token);

                try
                {
                    for (int attempt = 1; attempt <= maxAttempts; attempt++)
                    {
                        token.ThrowIfCancellationRequested();

                        var tcs =
                            new TaskCompletionSource<CalibrationAck>(
                                TaskCreationOptions.RunContinuationsAsynchronously);

                        lock (_lock)
                        {
                            rxBuffer.Clear();

                            _pendingCalCommand = expectedCommand;
                            _pendingCalSensor = expectedSensor;
                            _pendingCalAckTcs = tcs;
                        }

                        try
                        {
                            // SendFrame() จะเติม CRC ให้อัตโนมัติ
                            SendFrame(frameWithoutCrc);

                            CalibrationAck ack = await tcs.Task.WaitAsync(
                                TimeSpan.FromMilliseconds(timeoutMs),
                                token);

                            if (ack.Status != 0x00)
                            {
                                throw new InvalidOperationException(
                                    $"บอร์ดตอบ Calibration Error " +
                                    $"CMD=0x{ack.Command:X2}, " +
                                    $"Sensor=0x{ack.Sensor:X2}, " +
                                    $"Status=0x{ack.Status:X2}");
                            }

                            if (ack.CompletedMask != expectedMask)
                            {
                                throw new InvalidOperationException(
                                    $"Calibration ACK Mask ไม่ถูกต้อง " +
                                    $"CMD=0x{ack.Command:X2}, " +
                                    $"ได้รับ=0x{ack.CompletedMask:X2}, " +
                                    $"คาดหวัง=0x{expectedMask:X2}");
                            }

                            return ack;
                        }
                        catch (TimeoutException) when (attempt < maxAttempts)
                        {
                            LogWriteDebug(
                                $"⚠️ Calibration ACK Timeout " +
                                $"รอบ {attempt}/{maxAttempts}",
                                Color.Orange);

                            await Task.Delay(300, token);
                        }
                        finally
                        {
                            lock (_lock)
                            {
                                if (ReferenceEquals(_pendingCalAckTcs, tcs))
                                    _pendingCalAckTcs = null;
                            }
                        }
                    }

                    throw new TimeoutException(
                        $"ไม่ได้รับ Calibration ACK " +
                        $"CMD=0x{expectedCommand:X2}, " +
                        $"Sensor=0x{expectedSensor:X2}");
                }
                finally
                {
                    _mainReadGate.Release();
                }
            }
            finally
            {
                Interlocked.Decrement(ref _priorityCommandCount);
                _calProtocolGate.Release();
            }
        }

        private void ThrowIfStopRequested(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_userRequestedStop)
                throw new OperationCanceledException(token);
        }

        private void SendCalibrationOhm(double ohmValue)
        {
            if (_ohmPort == null || !_ohmPort.IsOpen)
                throw new InvalidOperationException(
                    "ยังไม่ได้เชื่อมต่อ Ohm Source");

            int multiplier =
                chkMultiplyBy100.Checked ? 100 : 10;

            int result =
                checked((int)Math.Round(ohmValue * multiplier));

            byte[] frame =
            {
        0x50,
        (byte)(result & 0xFF),
        (byte)((result >> 8) & 0xFF),
        (byte)((result >> 16) & 0xFF),
        (byte)((result >> 24) & 0xFF),
        0x00
    };

            _ohmPort.DiscardOutBuffer();
            _ohmPort.Write(frame, 0, frame.Length);
        }
        private async Task<ushort[]> CollectCalibrationAdcAsync(
            string label,
            IReadOnlyList<int> ohmValues,
            CancellationToken token)
        {
            ushort[] adcValues = new ushort[ohmValues.Count];

            for (int i = 0; i < ohmValues.Count; i++)
            {
                ThrowIfStopRequested(token);

                int ohm = ohmValues[i];

                SendCalibrationOhm(ohm);

                LogToRx(
                    $"[{label}] ตั้งค่า {ohm} Ω " +
                    $"และรอ ADC 5 วินาที...",
                    Color.DarkBlue);

                await Task.Delay(3500, token);

                short rawValue = await ReadSingleRegisterAsync(
                    RawAdcRegister,
                    timeoutMs: 2000,
                    retries: 2,
                    slaveId: CalibrationSlaveId,
                    cancellationToken: token);

                ushort adc = unchecked((ushort)rawValue);

                if (adc == 0xFFFF || adc == 0x0000)
                {
                    string problem = adc == 0xFFFF
                        ? "ไม่ได้เชื่อมต่อ Ohm Source หรือสายสัญญาณ"
                        : "สาย Ohm Source เกิดการ Short กัน";

                    string detail =
                        $"Calibration หยุดการทำงาน\n\n{problem}\n\n" +
                        $"ชุดทดสอบ: {label}\n" +
                        $"จุดที่: {i + 1}\n" +
                        $"ค่าที่ตั้ง: {ohm} Ω\n" +
                        $"ADC: {adc:X4}";

                    LogToRx($"❌ [CAL {label}] จุดที่ {i + 1} | {ohm} Ω | " +
                            $"ADC = 0x{adc:X4} | {problem}", Color.Red);

                    MessageBox.Show(
                        this,
                        detail,
                        "Calibration ไม่สำเร็จ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    throw new InvalidOperationException(
                        $"{problem} ({label} จุดที่ {i + 1}, {ohm} Ω, ADC {adc:X4})");
                }

                adcValues[i] = adc;

                byte adcHigh = (byte)(adc >> 8);
                byte adcLow = (byte)(adc & 0xFF);

                LogToRx(
                    $"[CAL {label}] จุดที่ {i + 1}/{ohmValues.Count} | {ohm} Ω | " +
                    $"ADC = {adc} (0x{adc:X4})",
                    Color.DarkGreen);

                IncrementRunProgress();
                ThrowIfStopRequested(token);
            }

            return adcValues;
        }
        private async Task TryCancelCalibrationAsync()
        {
            try
            {
                // TX: 01 55 04 00 02 7A 00 + CRC
                // RX: 01 55 04 00 00 00 + CRC
                await SendCalibrationFrameAsync(
                    BuildCalibrationControlFrame(0x04),
                    expectedCommand: 0x04,
                    expectedSensor: 0x00,
                    expectedMask: 0x00,
                    token: CancellationToken.None,
                    maxAttempts: 2,
                    timeoutMs: 2000);

                LogToRx(
                    "✅ ส่ง Cancel Calibration สำเร็จ",
                    Color.Orange);
            }
            catch (Exception ex)
            {
                LogToRx(
                    $"⚠️ ส่ง Cancel Calibration ไม่สำเร็จ: {ex.Message}",
                    Color.Red);
            }
        }

        #endregion

        #region 05 สื่อสารกับบอร์ด Relay และ Alarm

        private void RelayDataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (_relayPort == null || !_relayPort.IsOpen)   // ✅ แก้เป็น _relayPort
                return;

            try
            {
                int count = _relayPort.BytesToRead;          // ✅
                if (count <= 0) return;

                byte[] buffer = new byte[count];
                _relayPort.Read(buffer, 0, count);            // ✅

                string rawHex = BitConverter.ToString(buffer).Replace("-", " ");
                BeginInvoke(new Action(() => LogWriteDebug($"[RAW RX-RELAY] {rawHex}", Color.DarkCyan)));

                lock (_lock)
                {
                    relayRxBuffer.AddRange(buffer);           // ✅ เติมคนละ buffer
                }

                ParseRelayModbusRTU();                        // ✅ เรียกคนละ parser
            }
            catch (Exception ex)
            {
                if (!IsDisposed && IsHandleCreated)
                    BeginInvoke(new Action(() =>
                        LogToRx($"[RELAY RX ERROR] {ex.GetType().Name}: {ex.Message}", Color.Red)));
            }
        }
        private void ParseRelayModbusRTU()
        {
            lock (_lock)
            {
                while (relayRxBuffer.Count >= 5)
                {
                    byte function = relayRxBuffer[1];
                    int frameLength = 0;

                    if (function == 0x03) frameLength = relayRxBuffer[2] + 5;
                    else if (function == 0x83) frameLength = 5;
                    else { relayRxBuffer.RemoveAt(0); continue; }

                    if (relayRxBuffer.Count < frameLength) break;

                    byte[] frame = relayRxBuffer.GetRange(0, frameLength).ToArray();

                    byte[] dataOnly = frame.Take(frameLength - 2).ToArray();
                    byte[] expectedCrc = CalculateCRC(dataOnly);
                    bool crcOk = frame[frameLength - 2] == expectedCrc[0] && frame[frameLength - 1] == expectedCrc[1];

                    if (!crcOk)
                    {
                        relayRxBuffer.RemoveAt(0);
                        continue;
                    }

                    relayRxBuffer.RemoveRange(0, frameLength);
                    ProcessRelayFrame(frame);
                }
            }
        }
        private void ProcessRelayFrame(byte[] frame)
        {
            lock (_lock)
            {
                if (frame.Length < 5 || frame[0] != _pendingRelaySlaveId ||
                    _pendingRelayReadTcs == null) return;

                if (frame[1] == 0x83 && frame.Length == 5)
                {
                    _pendingRelayReadTcs.TrySetException(
                        new InvalidOperationException($"Modbus exception 0x{frame[2]:X2} (ID {frame[0]})"));
                    _pendingRelayReadTcs = null;
                }
                else if (frame[1] == 0x03 && frame.Length == 7 && frame[2] == 2)
                {
                    short value = (short)((frame[3] << 8) | frame[4]);
                    _pendingRelayReadTcs.TrySetResult(value);
                    _pendingRelayReadTcs = null;
                }
            }
        }
        private void SendRelayFrame(byte[] data)
        {
            lock (_lock)
            {
                if (_relayPort == null || !_relayPort.IsOpen)
                {
                    LogToRx("[RELAY] ยังไม่ได้เชื่อมต่อพอร์ต Relay", Color.Red);
                    return;
                }

                try
                {
                    byte[] crc = CalculateCRC(data);
                    byte[] frame = new byte[data.Length + 2];
                    Array.Copy(data, frame, data.Length);
                    frame[data.Length] = crc[0];
                    frame[data.Length + 1] = crc[1];

                    _relayPort.Write(frame, 0, frame.Length);
                }
                catch (Exception ex)
                {
                    LogToRx($"[RELAY] Write Error: {ex.Message}", Color.Red);
                }
            }
        }
        private async Task<short> ReadRelaySingleRegisterAsync(ushort address,
            int timeoutMs = 2000, int retries = 2, byte slaveId = RelayCheckSlaveID,
            CancellationToken cancellationToken = default)
        {
            await _relayReadGate.WaitAsync(cancellationToken);
            try
            {
                for (int attempt = 0; attempt <= retries; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (_relayPort == null || !_relayPort.IsOpen)
                        throw new InvalidOperationException("ยังไม่ได้เชื่อมต่อพอร์ต Relay");

                    var tcs = new TaskCompletionSource<short>(TaskCreationOptions.RunContinuationsAsynchronously);
                    lock (_lock)
                    {
                        relayRxBuffer.Clear();
                        _pendingRelaySlaveId = slaveId;
                        _pendingRelayReadTcs = tcs;
                    }
                    try
                    {
                        SendRelayFrame(new byte[]
                        {
                            slaveId, 0x03, (byte)(address >> 8), (byte)address, 0x00, 0x01
                        });
                        return await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), cancellationToken);
                    }
                    catch (TimeoutException) when (attempt < retries)
                    {
                        // Wait between retries, but allow Stop to end the wait immediately.
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            if (ReferenceEquals(_pendingRelayReadTcs, tcs))
                                _pendingRelayReadTcs = null;
                        }
                    }
                    await Task.Delay(300, cancellationToken);
                }
                throw new TimeoutException($"Timeout อ่าน ID {slaveId}, Register {address}");
            }
            finally
            {
                _relayReadGate.Release();
            }
        }

        #endregion

        #region 06 Timer และคำสั่งอ่านเขียน Register
        //── ส่วนของ timer ──────────────────────────────
        private bool _timerBusy = false;

        private async void timer1_Tick(object sender, EventArgs e)
        {
            if (Volatile.Read(ref _priorityCommandCount) > 0)
            {
                return;
            }

            if (_timerBusy) return;
            _timerBusy = true;

            try
            {
                bool isDebug = chkDebugWrite != null && chkDebugWrite.Checked;

                if (!isDebug)
                {
                    if (_serialPort == null || !_serialPort.IsOpen)
                    {
                        HandleDisconnect("พอร์ต DEF-03N หลุด");
                        return;
                    }

                    if (_ohmPort == null || !_ohmPort.IsOpen)
                    {
                        HandleDisconnect("พอร์ต Ohmsource หลุด");
                        return;
                    }
                    if (_relayPort == null || !_relayPort.IsOpen)
                    {
                        HandleDisconnect("พอร์ต PH-01 Relay / Alarm หลุด");
                        return;
                    }
                }
                else
                {
                    if (_defPortConnected && (_serialPort == null || !_serialPort.IsOpen))
                    {
                        HandleDisconnect("พอร์ต DEF-03N หลุด");
                        return;
                    }
                    if (_ohmPortConnected && (_ohmPort == null || !_ohmPort.IsOpen))
                    {
                        HandleDisconnect("พอร์ต Ohmsource หลุด");
                        return;
                    }
                    if (_relayPortConnected && (_relayPort == null || !_relayPort.IsOpen))
                    {
                        HandleDisconnect("พอร์ต PH-01 Relay / Alarm หลุด");
                        return;
                    }
                }

                // ── 4. ส่ง Modbus Read ───────────────────────────────────
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    await ExecuteReadCycleAsync();

                    if (_mainPollTimeoutCount >= CommunicationFailureThreshold)
                    {
                        HandleDisconnect(
                            $"DEF-03N ไม่ตอบ Modbus ติดต่อกัน {_mainPollTimeoutCount} ครั้ง");
                        return;
                    }
                }

                if (_relayPort != null && _relayPort.IsOpen)
                {
                    if (DateTime.UtcNow >= _nextRelayHealthCheckUtc)
                    {
                        _nextRelayHealthCheckUtc =
                            DateTime.UtcNow.Add(RelayHealthCheckInterval);

                        string? missingPort = FindUnavailableConnectedPort();
                        if (missingPort != null)
                        {
                            HandleDisconnect($"ไม่พบพอร์ต {missingPort} ใน Windows");
                            return;
                        }

                        await CheckRelayCommunicationAsync();

                        if (_relayPollTimeoutCount >= CommunicationFailureThreshold)
                        {
                            HandleDisconnect(
                                $"PH-01 Relay / Alarm ไม่ตอบ Modbus " +
                                $"ติดต่อกัน {_relayPollTimeoutCount} ครั้ง");
                            return;
                        }
                    }
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
        private void timerCal_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsed = _calStopwatch.Elapsed;
            lblTimer.Text = string.Format("{0:00}:{1:00}", (int)elapsed.TotalMinutes, elapsed.Seconds);
        }




        //── ส่วนของ การสั่งเขียนและอ่าน ──────────────────────────────
        private const byte TargetID = 1;
        private bool _isCalibrating = false;

        private const byte RelayCheckSlaveID = 3;
        private const ushort RelayStatusRegister = 0;
        private const ushort AlarmStatusRegister = 1;

        private const ushort MainPollStartAddress = 0;
        private const ushort MainPollQuantity = 27; // Register 0..26

        private async Task ExecuteReadCycleAsync()
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;
            if (Volatile.Read(ref _priorityCommandCount) > 0) return;

            // Polling is low priority. Skip this tick instead of queuing behind
            // a register read, write, or calibration transaction.
            if (!await _mainReadGate.WaitAsync(0)) return;

            try
            {
                if (Volatile.Read(ref _priorityCommandCount) > 0) return;

                var pollTcs = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                lock (_lock)
                {
                    rxBuffer.Clear();
                    lastRequestedStart = MainPollStartAddress;
                    _pendingPollTcs = pollTcs;
                }

                try
                {
                    SendFrame(new byte[]
                    {
                        TargetID,
                        0x03,
                        (byte)(MainPollStartAddress >> 8),
                        (byte)MainPollStartAddress,
                        (byte)(MainPollQuantity >> 8),
                        (byte)MainPollQuantity
                    });

                    await pollTcs.Task.WaitAsync(TimeSpan.FromMilliseconds(1000));
                    _mainPollTimeoutCount = 0;
                }
                catch (TimeoutException)
                {
                    _mainPollTimeoutCount++;
                    LogWriteDebug("[POLL] Timeout อ่าน PV/SV/ADC", Color.Orange);
                }
                finally
                {
                    lock (_lock)
                    {
                        if (ReferenceEquals(_pendingPollTcs, pollTcs))
                            _pendingPollTcs = null;
                    }
                }
            }
            finally
            {
                _mainReadGate.Release();
            }
        }

        private string? FindUnavailableConnectedPort()
        {
            var availablePorts = new HashSet<string>(
                SerialPort.GetPortNames(),
                StringComparer.OrdinalIgnoreCase);

            if (_serialPort != null && _serialPort.IsOpen &&
                !availablePorts.Contains(_serialPort.PortName))
                return $"DEF-03N ({_serialPort.PortName})";

            if (_ohmPort != null && _ohmPort.IsOpen &&
                !availablePorts.Contains(_ohmPort.PortName))
                return $"Ohm Source ({_ohmPort.PortName})";

            if (_relayPort != null && _relayPort.IsOpen &&
                !availablePorts.Contains(_relayPort.PortName))
                return $"PH-01 Relay / Alarm ({_relayPort.PortName})";

            return null;
        }

        private async Task CheckRelayCommunicationAsync()
        {
            try
            {
                // ค่าสถานะจะเป็น 0 หรือ 1 ก็ได้ จุดประสงค์คือตรวจว่า ID 3 ยังตอบ
                await ReadRelaySingleRegisterAsync(
                    RelayStatusRegister,
                    timeoutMs: 700,
                    retries: 0,
                    slaveId: RelayCheckSlaveID);

                _relayPollTimeoutCount = 0;
            }
            catch (TimeoutException)
            {
                _relayPollTimeoutCount++;
                LogWriteDebug(
                    $"[RELAY POLL] Timeout ID {RelayCheckSlaveID} " +
                    $"({_relayPollTimeoutCount}/{CommunicationFailureThreshold})",
                    Color.Orange);
            }
            catch (Exception ex)
            {
                _relayPollTimeoutCount = CommunicationFailureThreshold;
                LogWriteDebug($"[RELAY POLL ERROR] {ex.Message}", Color.Red);
            }
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


        private TaskCompletionSource<short>? _pendingReadTcs;
        private TaskCompletionSource<bool>? _pendingPollTcs;
        private TaskCompletionSource<bool>? _pendingWriteTcs;
        private ushort _pendingWriteAddress;
        private ushort _pendingWriteValue;
        private async Task<short> ReadSingleRegisterAsync(ushort address,
            int timeoutMs = 2000, int retries = 2, byte slaveId = TargetID,
            CancellationToken cancellationToken = default)
        {
            await _mainReadGate.WaitAsync(cancellationToken);
            try
            {
                for (int attempt = 0; attempt <= retries; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (_serialPort == null || !_serialPort.IsOpen)
                        throw new InvalidOperationException("ยังไม่ได้เชื่อมต่อพอร์ต DEF-03N");

                    var tcs = new TaskCompletionSource<short>(TaskCreationOptions.RunContinuationsAsynchronously);
                    lock (_lock)
                    {
                        rxBuffer.Clear();
                        lastRequestedStart = address;
                        _pendingReadSlaveId = slaveId;
                        _pendingReadTcs = tcs;
                    }
                    try
                    {
                        SendFrame(new byte[]
                        {
                            slaveId, 0x03, (byte)(address >> 8), (byte)address, 0x00, 0x01
                        });
                        return await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), cancellationToken);
                    }
                    catch (TimeoutException) when (attempt < retries)
                    {
                        // Wait between retries, but allow Stop to end the wait immediately.
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            if (ReferenceEquals(_pendingReadTcs, tcs))
                                _pendingReadTcs = null;
                        }
                    }
                    await Task.Delay(300, cancellationToken);
                }
                throw new TimeoutException($"Timeout อ่าน ID {slaveId}, Register {address}");
            }
            finally
            {
                _mainReadGate.Release();
            }
        }
        private async Task<bool> ExecuteWriteSingleRegisterVerifiedAsync(
            byte slaveId,
            ushort registerAddress,
            short value,
            int maxAttempts = 5,
            int timeoutMs = 1000)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                LogToRx(
                    "[WRITE ERROR] ยังไม่ได้เชื่อมต่อ DEF-03N",
                    Color.Red);

                return false;
            }

            Interlocked.Increment(ref _priorityCommandCount);
            await _mainReadGate.WaitAsync();

            try
            {
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    TaskCompletionSource<bool> echoTcs =
                        new TaskCompletionSource<bool>(
                            TaskCreationOptions.RunContinuationsAsynchronously);

                    lock (_lock)
                    {
                        rxBuffer.Clear();
                        _pendingWriteAddress = registerAddress;
                        _pendingWriteValue = unchecked((ushort)value);
                        _pendingWriteTcs = echoTcs;
                    }

                    try
                    {
                        _isWriting = true;

                        LogWriteDebug(
                            $"[WRITE] Register {registerAddress} = {value} " +
                            $"(ครั้งที่ {attempt}/{maxAttempts})",
                            Color.Blue);

                        ExecuteWriteSingleRegister(
                            slaveId,
                            registerAddress,
                            unchecked((short)value));

                        Task completedTask = await Task.WhenAny(
                            echoTcs.Task,
                            Task.Delay(timeoutMs));

                        if (completedTask == echoTcs.Task &&
                            await echoTcs.Task)
                        {
                            return true;
                        }

                        LogWriteDebug(
                            $"[RETRY] ไม่พบ Echo ที่ตรงกับ Register " +
                            $"{registerAddress} = {value}",
                            Color.OrangeRed);
                    }
                    catch (Exception ex)
                    {
                        LogWriteDebug(
                            $"[WRITE ERROR] ครั้งที่ {attempt}: {ex.Message}",
                            Color.Red);
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            if (ReferenceEquals(_pendingWriteTcs, echoTcs))
                                _pendingWriteTcs = null;
                        }

                        _isWriting = false;
                    }

                    if (attempt < maxAttempts)
                        await Task.Delay(300);
                }

                LogToRx(
                    $"[WRITE FAILED] เขียน Register {registerAddress} ไม่สำเร็จ",
                    Color.Red);

                return false;
            }
            finally
            {
                _mainReadGate.Release();
                Interlocked.Decrement(ref _priorityCommandCount);
            }
        }



        #endregion

        #region 07 รอสถานะและยืนยันคำสั่ง Calibration



        #endregion

        #region 08 แสดงข้อความและแก้ค่า Register บนหน้าจอ

        private void LogToRx(string message, Color color)
        {
            // ตรวจสอบว่าเป็นการเรียกข้าม Thread หรือไม่ (ป้องกันโปรแกรมค้าง)
            if (richTextBoxRx.InvokeRequired)
            {
                richTextBoxRx.BeginInvoke(new Action(() => LogToRx(message, color)));
                return;
            }

            richTextBoxRx.SelectionStart = richTextBoxRx.TextLength;
            richTextBoxRx.SelectionLength = 0;
            richTextBoxRx.SelectionColor = color;

            // ใส่เวลาปัจจุบันเข้าไปด้วย เพื่อจะได้รู้ว่าบอร์ดตอบกลับมาตอนไหน
            richTextBoxRx.AppendText($"[{DateTime.Now:HH:mm:ss}] -> {message}\r\n");

            richTextBoxRx.SelectionColor = richTextBoxRx.ForeColor;
            richTextBoxRx.ScrollToCaret(); // เลื่อนหน้าจอลงมาล่างสุดอัตโนมัติเมื่อมีข้อความใหม่
        }


        //── ส่วนของ การเขียน Register ──────────────────────────────
        private void CommonRegis_Enter(object sender, EventArgs e)
        {
            if (sender is System.Windows.Forms.TextBox txt)
            {
                _originalTextByControl[txt.Name] = txt.Text; // จำค่าเดิมแยกตามชื่อ control
            }
        }
        private void CommonRegis_TextChanged(object sender, EventArgs e)
        {
            if (sender is System.Windows.Forms.TextBox txt)
            {
                string original = _originalTextByControl.TryGetValue(txt.Name, out var val) ? val : txt.Text;

                if (txt.Text == original)
                    _dirtyRegis.Remove(txt.Name);
                else
                    _dirtyRegis.Add(txt.Name);
            }
        }
        private void CommonRegis_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                this.ActiveControl = null; // ← ทำให้ focus ออก = trigger Leave ด้วย
            }
        }
        private async void CommonRegis_Leave(object sender, EventArgs e)
        {
            await WriteRegisValueAsync(sender);
        }
        private async Task WriteRegisValueAsync(object sender)
        {
            if (sender is not System.Windows.Forms.TextBox txt) return;
            if (!_dirtyRegis.Contains(txt.Name)) return;

            _dirtyRegis.Remove(txt.Name);

            string originalText = _originalTextByControl.TryGetValue(txt.Name, out var val) ? val : txt.Text;

            string regNumberStr = txt.Name.Replace("Regis", "");
            if (!ushort.TryParse(regNumberStr, out ushort address)) return;

            short valueToWrite;
            switch (address)
            {
                case 1:
                    if (!double.TryParse(txt.Text, out double dblValue))
                    {
                        MessageBox.Show("กรุณากรอกค่า SV ให้ถูกต้อง", "Write Register",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txt.Text = originalText;
                        return;
                    }
                    valueToWrite = (short)Math.Round(dblValue);
                    break;

                default:
                    if (!short.TryParse(txt.Text, out valueToWrite))
                    {
                        MessageBox.Show("กรุณากรอกค่า Register ให้ถูกต้อง", "Write Register",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txt.Text = originalText;
                        return;
                    }
                    break;
            }

            bool timerWasRunning = timer1.Enabled;
            string requestedText = txt.Text;

            try
            {
                txt.Enabled = false;
                txt.Text = requestedText;
                txt.ForeColor = Color.Blue;
                _isWriting = true;

                // ── 1. เขียน + รอ echo ยืนยัน (retry สูงสุด 5 ครั้ง) ──
                bool writeOk = false;
                for (int attempt = 1; attempt <= 5 && !writeOk; attempt++)
                {
                    writeOk = await ExecuteWriteSingleRegisterVerifiedAsync(1, address, valueToWrite);
                    if (!writeOk) await Task.Delay(300);
                }

                if (!writeOk)
                {
                    txt.Text = originalText;
                    txt.ForeColor = Color.Red;
                    LogWriteDebug($"[WRITE FAILED] Register {address} ไม่ตอบรับค่าที่เขียน", Color.Red);
                    return;
                }

                // ── 2. อ่านย้อนกลับมาเช็คว่าค่าจริงเปลี่ยนตามไหม (สูงสุด 10 รอบ × 200ms) ──
                const int maxReadBackAttempts = 5;
                bool valueChanged = false;
                short actualValue = 0;

                for (int attempt = 1; attempt <= maxReadBackAttempts; attempt++)
                {
                    try
                    {
                        await Task.Delay(100);
                        actualValue = await ReadSingleRegisterAsync(address, timeoutMs: 500, retries: 0);

                        if (actualValue == valueToWrite)
                        {
                            valueChanged = true;
                            break;
                        }

                        LogWriteDebug($"[WAIT REGISTER] Reg{address} ยังเป็น {actualValue}, กำลังรอ {valueToWrite} ({attempt}/{maxReadBackAttempts})", Color.Orange);
                    }
                    catch (TimeoutException)
                    {
                        LogWriteDebug($"[READ BACK] Reg{address} ไม่ตอบกลับ ({attempt}/{maxReadBackAttempts})", Color.Orange);
                    }
                }

                if (!valueChanged)
                {
                    txt.Text = originalText;
                    txt.ForeColor = Color.Red;
                    LogWriteDebug($"[VERIFY FAILED] Reg{address} รับคำสั่งแล้ว แต่ค่าจริงยังไม่เปลี่ยนเป็น {valueToWrite}", Color.Red);
                    return;
                }

                // ── 3. สำเร็จ ──
                txt.Text = actualValue.ToString();

                txt.ForeColor = Color.Blue;
                _originalTextByControl[txt.Name] = txt.Text; // อัปเดตค่าดั้งเดิมใหม่หลัง write สำเร็จ

                LogWriteDebug($"✓ เขียน Register {address} = {valueToWrite} และอ่านกลับตรงกัน", Color.Green);
            }
            catch (Exception ex)
            {
                txt.Text = originalText;
                txt.ForeColor = Color.Red;
                LogToRx($"[WRITE ERROR] Register {address}: {ex.Message}", Color.Red);
            }
            finally
            {
                _isWriting = false;
                txt.Enabled = true;

                if (timerWasRunning && _serialPort != null && _serialPort.IsOpen)
                    timer1.Start();
            }
        }


        #endregion

        #region 09 Calibration และสั่งค่า Ohm Source
        public void SendOhm(double ohmValue) // 1. เปลี่ยนตรงนี้จาก int เป็น double
        {
            if (_ohmPort == null || !_ohmPort.IsOpen)
            {
                if (_serialPort?.IsOpen == true || _relayPort?.IsOpen == true)
                    HandleDisconnect("พอร์ต Ohm Source หลุด");
                else
                    MessageBox.Show("กรุณาเชื่อมต่อพอร์ต Ohm ก่อนครับ", "Warning");
                return;
            }

            try
            {
                int multiplier = chkMultiplyBy100.Checked ? 100 : 10;
                int result = (int)Math.Round(ohmValue * multiplier);

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
                HandleDisconnect("Ohm Source Write Error: " + ex.Message);
            }
        }

        #endregion

        #region 10 ตรวจ Relay และ Alarm แบบแยก

        private async Task CheckRelayOutputAsync()
        {
            if (_relayPort == null || !_relayPort.IsOpen)
            {
                LogToRx("ยังไม่ได้เชื่อมต่ออุปกรณ์ตรวจสอบ Relay", Color.Red);
                LogWriteDebug("[RELAY CHECK] ยังไม่ได้เชื่อมต่อพอร์ต Relay", Color.Red);
                return;
            }

            LogToRx("กำลังตรวจสอบ Relay Output...", Color.Blue);
            LogWriteDebug($"[RELAY OUT CHECK] กำลังอ่านสถานะ Relay Out (ID 3, Register {RelayStatusRegister})...", Color.Black);

            try
            {
                short value = await ReadRelaySingleRegisterAsync(RelayStatusRegister, timeoutMs: 1000, retries: 2);

                if (value == 1)
                    LogToRx("Relay Output: ON", Color.Green);
                else if (value == 0)
                    LogToRx("Relay Output: OFF", Color.Gray);
                else
                    LogToRx("Relay Output ส่งสถานะผิดปกติ", Color.Orange);

                LogWriteDebug($"[RELAY OUT CHECK] ID 3, Register {RelayStatusRegister} = {value}",
                    value == 1 ? Color.Green : value == 0 ? Color.Gray : Color.Orange);
            }
            catch (Exception ex)
            {
                LogToRx("ตรวจสอบ Relay Output ไม่สำเร็จ", Color.Red);
                LogWriteDebug($"[RELAY OUT CHECK] ล้มเหลว: {ex.Message}", Color.Red);
            }
        }

        private async Task CheckAlarmAsync()
        {
            if (_relayPort == null || !_relayPort.IsOpen)
            {
                LogToRx("ยังไม่ได้เชื่อมต่ออุปกรณ์ตรวจสอบ Relay", Color.Red);
                LogWriteDebug("[RELAY CHECK] ยังไม่ได้เชื่อมต่อพอร์ต Relay", Color.Red);
                return;
            }

            LogToRx("กำลังตรวจสอบ Alarm Relay...", Color.Blue);
            LogWriteDebug($"[RELAY ALARM CHECK] กำลังอ่านสถานะ Relay Alarm (ID 3, Register {AlarmStatusRegister})...", Color.Black);

            try
            {
                short value = await ReadRelaySingleRegisterAsync(AlarmStatusRegister, timeoutMs: 1000, retries: 2);

                if (value == 1)
                    LogToRx("Alarm Relay: ON", Color.Green);
                else if (value == 0)
                    LogToRx("Alarm Relay: OFF", Color.Gray);
                else
                    LogToRx("Alarm Relay ส่งสถานะผิดปกติ", Color.Orange);

                LogWriteDebug($"[RELAY ALARM CHECK] ID 3, Register {AlarmStatusRegister} = {value}",
                    value == 1 ? Color.Green : value == 0 ? Color.Gray : Color.Orange);
            }
            catch (Exception ex)
            {
                LogToRx("ตรวจสอบ Alarm Relay ไม่สำเร็จ", Color.Red);
                LogWriteDebug($"[RELAY CHECK] ล้มเหลว: {ex.Message}", Color.Red);
            }
        }

        //── ส่วนของ Cal ──────────────────────────────
        private CancellationTokenSource? _ctsCal;
        #endregion

        #region 11 ลำดับ Calibration หลัก

        private async Task DocalibrationAsync(CancellationToken token)
        {
            _isCalibrating = true;

            bool transactionStarted = false;
            bool committed = false;

            try
            {
                await Task.Delay(500, token);

                // ---------------------------------------------------------
                // 1. Start
                // TX: 01 55 01 00 02 7A 00 + CRC
                // RX: 01 55 01 00 00 + CRC
                // ---------------------------------------------------------
                transactionStarted = true;

                await SendCalibrationFrameAsync(
                    BuildCalibrationControlFrame(0x01),
                    expectedCommand: 0x01,
                    expectedSensor: 0x00,
                    expectedMask: 0x00,
                    token: token);

                ThrowIfStopRequested(token);

                LogToRx(
                    "✅ เข้าสู่โหมด Calibration สำเร็จ",
                    Color.Green);

                // ---------------------------------------------------------
                // 2. PTC 2K จำนวน 7 จุด
                // ---------------------------------------------------------
                int[] ptc2kOhms =
                {
                    1000, 1500, 2000, 2500,
                    3000, 3500, 4000
                };

                ushort[] ptc2kAdc =
                    await CollectCalibrationAdcAsync(
                        "PTC2K",
                        ptc2kOhms,
                        token);

                ThrowIfStopRequested(token);

                await SendCalibrationFrameAsync(
                    BuildCalibrationDataFrame(0x00, ptc2kAdc),
                    expectedCommand: 0x02,
                    expectedSensor: 0x00,
                    expectedMask: 0x01,
                    token: token);

                LogToRx(
                    "✅ บอร์ดรับข้อมูล PTC 2K ครบแล้ว",
                    Color.Green);

                // ---------------------------------------------------------
                // 3. NTC 2K จำนวน 11 จุด
                // ---------------------------------------------------------
                int[] ntc2kOhms =
                {
                    100, 1000, 2000, 3000, 4000, 5000,
                    10000, 20000, 30000, 40000, 50000
                };

                ushort[] ntc2kAdc =
                    await CollectCalibrationAdcAsync(
                        "NTC2K",
                        ntc2kOhms,
                        token);

                ThrowIfStopRequested(token);

                await SendCalibrationFrameAsync(
                    BuildCalibrationDataFrame(0x01, ntc2kAdc),
                    expectedCommand: 0x02,
                    expectedSensor: 0x01,
                    expectedMask: 0x03,
                    token: token);

                LogToRx(
                    "✅ บอร์ดรับข้อมูล NTC 2K ครบแล้ว",
                    Color.Green);

                // ---------------------------------------------------------
                // 4. NTC 10K จำนวน 10 จุด
                // ---------------------------------------------------------
                int[] ntc10kOhms =
                {
                    1000, 5000, 10000, 40000, 70000,
                    100000, 130000, 160000, 190000, 220000
                };

                ushort[] ntc10kAdc =
                    await CollectCalibrationAdcAsync(
                        "NTC10K",
                        ntc10kOhms,
                        token);

                ThrowIfStopRequested(token);

                await SendCalibrationFrameAsync(
                    BuildCalibrationDataFrame(0x02, ntc10kAdc),
                    expectedCommand: 0x02,
                    expectedSensor: 0x02,
                    expectedMask: 0x07,
                    token: token);

                LogToRx(
                    "✅ บอร์ดรับข้อมูล NTC 10K ครบแล้ว",
                    Color.Green);

                // ---------------------------------------------------------
                // 5. Commit
                // TX: 01 55 03 00 02 7A 00 + CRC
                // RX: 01 55 03 00 00 07 + CRC
                // ---------------------------------------------------------
                await SendCalibrationFrameAsync(
                    BuildCalibrationControlFrame(0x03),
                    expectedCommand: 0x03,
                    expectedSensor: 0x00,
                    expectedMask: 0x07,
                    token: token);

                committed = true;

                LogToRx(
                    "✅ Commit Calibration สำเร็จ",
                    Color.Green);
            }
            catch (OperationCanceledException)
            {
                if (transactionStarted && !committed && _serialPort?.IsOpen == true)
                    await TryCancelCalibrationAsync();

                LogToRx(
                    "⚠️ Calibration ถูกยกเลิก",
                    Color.Orange);

                throw;
            }
            catch
            {
                if (transactionStarted && !committed && _serialPort?.IsOpen == true)
                    await TryCancelCalibrationAsync();

                throw;
            }
            finally
            {
                await Task.Delay(500);

                _isCalibrating = false;

                if (_serialPort?.IsOpen == true)
                    timer1.Start();
            }
        }



        //── ส่วนของ ของการ Test ───────────────────────────────────
        private bool _skipExcelWrite = false; // true = ทดสอบอย่างเดียว ไม่เขียน Excel
        private string? _autoReportStagingFolder;
        private string? _autoReportStagedPath;
        private string? _autoReportFinalPath;
        #endregion

        #region 12 ตรวจข้อมูลและเตรียมไฟล์รายงาน

        private bool ValidateBeforeTest(string outputFolder, string sn, string lot)
        {
            if (_skipExcelWrite)
                return true;

            if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
            {
                MessageBox.Show("กรุณากดปุ่ม Save เพื่อเลือกโฟลเดอร์สำหรับเซฟไฟล์รายงานก่อนครับ!", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(sn))
            {
                MessageBox.Show("กรุณากรอก S/N ก่อนเริ่มการทดสอบครับ!", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(lot))
            {
                MessageBox.Show("กรุณากรอก LOT ก่อนเริ่มการทดสอบครับ!", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private string GetSelectedReportModelCode()
        {
            string supplyCode = txt_supply.SelectedIndex == 0 ? "220" : "24";
            return $"{txt_model.Text.Trim()}-{supplyCode}";
        }

        private void BeginAutoReportTransaction(string outputFolder, string lotName)
        {
            DiscardAutoReportTransaction(logDiscard: false);

            string safeLotName = string.Join(
                "_",
                lotName.Split(Path.GetInvalidFileNameChars()));
            string finalPath = Path.Combine(outputFolder, $"{safeLotName}.xlsx");
            string stagingFolder = Path.Combine(
                outputFolder,
                $".def03n-stage-{Guid.NewGuid():N}");
            string stagedPath = Path.Combine(stagingFolder, $"{safeLotName}.xlsx");

            Directory.CreateDirectory(stagingFolder);
            if (File.Exists(finalPath))
                File.Copy(finalPath, stagedPath, overwrite: true);

            _autoReportStagingFolder = stagingFolder;
            _autoReportStagedPath = stagedPath;
            _autoReportFinalPath = finalPath;
        }

        private bool CommitAutoReportTransaction()
        {
            if (string.IsNullOrWhiteSpace(_autoReportStagingFolder) ||
                string.IsNullOrWhiteSpace(_autoReportStagedPath) ||
                string.IsNullOrWhiteSpace(_autoReportFinalPath) ||
                !File.Exists(_autoReportStagedPath))
            {
                return false;
            }

            string stagingFolder = _autoReportStagingFolder;
            string stagedPath = _autoReportStagedPath;
            string finalPath = _autoReportFinalPath;
            string backupPath = Path.Combine(stagingFolder, "previous-report.backup.xlsx");

            while (true)
            {
                try
                {
                    if (File.Exists(finalPath))
                    {
                        File.Replace(stagedPath, finalPath, backupPath, ignoreMetadataErrors: true);
                    }
                    else
                    {
                        File.Move(stagedPath, finalPath);
                    }

                    foreach (ModelReportTarget target in _modelReportTargets)
                    {
                        if (target.Path.Equals(stagedPath, StringComparison.OrdinalIgnoreCase))
                            target.Path = finalPath;
                    }

                    _autoReportStagingFolder = null;
                    _autoReportStagedPath = null;
                    _autoReportFinalPath = null;

                    try
                    {
                        if (Directory.Exists(stagingFolder))
                            Directory.Delete(stagingFolder, recursive: true);
                    }
                    catch (Exception cleanupEx)
                    {
                        LogWriteDebug(
                            $"[AUTO SAVE] ล้างโฟลเดอร์ชั่วคราวไม่สำเร็จ: {cleanupEx.Message}",
                            Color.Orange);
                    }

                    return true;
                }
                catch (IOException ex)
                {
                    DialogResult choice = MessageBox.Show(
                        this,
                        "ไม่สามารถบันทึกผลสุดท้ายลง Excel ได้\n\n" +
                        $"ไฟล์: {Path.GetFileName(finalPath)}\n" +
                        $"รายละเอียด: {ex.Message}\n\n" +
                        "กรุณาปิดไฟล์ Excel แล้วกด Retry",
                        "บันทึกผลสุดท้ายไม่สำเร็จ",
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Warning);

                    if (choice == DialogResult.Cancel)
                        return false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        this,
                        $"บันทึกผลสุดท้ายลง Excel ไม่สำเร็จ\n\n{ex.Message}",
                        "บันทึกผลสุดท้ายไม่สำเร็จ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }
            }
        }

        private void DiscardAutoReportTransaction(bool logDiscard = true)
        {
            string? stagingFolder = _autoReportStagingFolder;

            _autoReportStagingFolder = null;
            _autoReportStagedPath = null;
            _autoReportFinalPath = null;

            if (!string.IsNullOrWhiteSpace(stagingFolder) &&
                Directory.Exists(stagingFolder))
            {
                try
                {
                    Directory.Delete(stagingFolder, recursive: true);
                }
                catch (Exception ex)
                {
                    LogWriteDebug($"[AUTO SAVE] ลบไฟล์ชั่วคราวไม่สำเร็จ: {ex.Message}", Color.Orange);
                }
            }

            if (logDiscard)
                LogToRx("ยกเลิกผลรอบนี้แล้ว — ไม่มีการบันทึกลง Excel", Color.DarkOrange);
        }

        private bool ValidateLotModelBeforeTest(
            string outputFolder,
            string lotName,
            bool showMessage,
            out string failureMessage)
        {
            failureMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(outputFolder) ||
                !Directory.Exists(outputFolder) ||
                string.IsNullOrWhiteSpace(lotName))
            {
                return true;
            }

            string safeLotName = string.Join(
                "_",
                lotName.Split(Path.GetInvalidFileNameChars()));
            string reportPath = Path.Combine(outputFolder, $"{safeLotName}.xlsx");

            // A new LOT has no assigned model yet. J3 will be initialized when
            // the first test result is written.
            if (!File.Exists(reportPath))
                return true;

            string selectedModelCode = GetSelectedReportModelCode();

            try
            {
                using var snapshot = new MemoryStream();
                using (var source = new FileStream(
                           reportPath,
                           FileMode.Open,
                           FileAccess.Read,
                           FileShare.ReadWrite | FileShare.Delete))
                {
                    source.CopyTo(snapshot);
                }

                snapshot.Position = 0;
                using var workbook = new XLWorkbook(snapshot);
                string lotModelCode = workbook.Worksheet(1)
                    .Cell("J3")
                    .GetFormattedString()
                    .Trim();

                // An untouched template/report may not have a model assigned.
                if (string.IsNullOrWhiteSpace(lotModelCode) ||
                    lotModelCode.Equals(selectedModelCode, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                failureMessage =
                    "ไม่สามารถเริ่มการทดสอบได้ เนื่องจาก Model ของไฟล์ LOT ไม่ตรงกับรายการที่เลือก\n\n" +
                    $"ไฟล์: {Path.GetFileName(reportPath)}\n" +
                    $"Model ของ LOT: {lotModelCode}\n" +
                    $"Model ที่เลือก: {selectedModelCode}\n\n" +
                    "กรุณาเปลี่ยน Model และ Supply ให้ตรงกับไฟล์ LOT หรือเลือก LOT ที่ถูกต้อง";
            }
            catch (Exception ex)
            {
                failureMessage =
                    "ไม่สามารถตรวจสอบ Model ของไฟล์ LOT ได้ จึงยังไม่อนุญาตให้เริ่มการทดสอบ\n\n" +
                    $"ไฟล์: {Path.GetFileName(reportPath)}\n" +
                    $"รายละเอียด: {ex.Message}";
            }

            LogToRx(failureMessage, Color.Red);
            if (showMessage)
            {
                MessageBox.Show(
                    this,
                    failureMessage,
                    "Model ของ LOT ไม่ตรงกัน",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return false;
        }

        private (string path, int targetRow) GetOrCreateReportFileQC(string outputFolder, string lotName, bool isQAMode)
        {
            if (!ModelTestResults.IsValidModel(txt_model.Text.Trim()) || txt_supply.SelectedIndex < 0)
                throw new InvalidOperationException("กรุณาเลือก Model และ supply ก่อนสร้างรายงาน");

            if (!ValidateLotModelBeforeTest(
                    outputFolder,
                    lotName,
                    showMessage: false,
                    out string modelFailureMessage))
            {
                throw new InvalidOperationException(modelFailureMessage);
            }

            string templateFileName = isQAMode ? "TempleteQA.xlsx" : "DEF-03N QC Inspection.xlsx";
            string templateFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, templateFileName);

            if (!File.Exists(templateFullPath))
            {
                throw new FileNotFoundException($"ไม่พบไฟล์ Template ต้นฉบับ: {templateFileName} ในโฟลเดอร์โปรแกรม");
            }

            if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
            {
                outputFolder = AppDomain.CurrentDomain.BaseDirectory;
            }

            string safeLotName = string.Join("_", lotName.Split(Path.GetInvalidFileNameChars()));
            string currentFilePath = Path.Combine(outputFolder, $"{safeLotName}.xlsx");

            if (!File.Exists(currentFilePath))
            {
                if (isQAMode) File.Copy(templateFullPath, currentFilePath, true);
                else CreateNewQcReport(templateFullPath, currentFilePath);
            }

            // --- [ คำนวณบรรทัดที่จะเขียนลง Excel ] ---
            if (isQAMode)
            {
                int targetStartRow = 69;
                using (var workbook = new ClosedXML.Excel.XLWorkbook(currentFilePath))
                {
                    var sheet = workbook.Worksheet(1);
                    if (string.IsNullOrEmpty(sheet.Cell(69, 4).GetFormattedString().Trim())) targetStartRow = 69;
                    else if (string.IsNullOrEmpty(sheet.Cell(78, 4).GetFormattedString().Trim())) targetStartRow = 78;
                    else if (string.IsNullOrEmpty(sheet.Cell(87, 4).GetFormattedString().Trim())) targetStartRow = 87;
                    else targetStartRow = 69;
                }
                return (currentFilePath, targetStartRow);
            }
            else
            {
                using var workbook = new XLWorkbook(currentFilePath);
                bool rowsExpanded;
                int qcTargetRow = QcReportLayout.FindRow(
                    workbook.Worksheet(1),
                    txt_SN.Text.Trim(),
                    out rowsExpanded);

                if (rowsExpanded &&
                    !SaveWorkbookWithRetry(
                        workbook,
                        currentFilePath,
                        "ขยายตาราง QC เพิ่ม 52 รายการ"))
                {
                    throw new IOException("บันทึกตาราง QC ที่ขยายแล้วไม่สำเร็จ");
                }

                return (currentFilePath, qcTargetRow);
            }
        }
        #endregion

        #region 13 เตรียมอุปกรณ์และทดสอบอุณหภูมิ

        private async Task<bool> PrepareHardwareForCalibrationAsync(string testType)
        {
            // 🎯 แปลงชื่อประเภทสินค้าเป็นค่า Register 2 ที่ต้องสั่งงาน
            short reg2Value = 2; // ค่าเริ่มต้น (ถ้าเป็น ntc2k ให้เป็น 2)

            switch (testType.ToLower())
            {
                case "ntc2k":
                    reg2Value = 2;
                    break;
                case "ptc2k":
                    reg2Value = 0; // ⚠️ สมมติว่า PTC 2K คือเลข 0 (คุณสามารถเปลี่ยนเป็นเลขที่ถูกต้องของบอร์ดได้เลยครับ)
                    break;
                case "ntc10ka":
                    reg2Value = 1; // ⚠️ สมมติว่า NTC 10K ยี่ห้อ A คือเลข 3
                    break;
                case "ntc10kb":
                    reg2Value = 3; // ⚠️ สมมติว่า NTC 10K ยี่ห้อ B คือเลข 4
                    break;
                default:
                    reg2Value = 2; // เผื่อกรณีปุ่มเก่าไม่ได้ส่งมา ให้กลับมาเป็นค่ามาตรฐาน
                    break;
            }

            LogToRx($"กำลังปรับ Input เป็น {reg2Value}...", Color.Blue);
            LogWriteDebug($"⏳ กำลังตั้งค่า Register 2 เป็น {reg2Value}...", Color.Black);

            bool writeReg2Ok =
                await ExecuteWriteSingleRegisterVerifiedAsync(
                    slaveId: 1,
                    registerAddress: 2,
                    value: reg2Value,
                    maxAttempts: 5,
                    timeoutMs: 1000);

            if (!writeReg2Ok)
            {
                LogToRx($"ปรับ Input เป็น {reg2Value} ไม่สำเร็จ ยกเลิกการทดสอบ", Color.Red);
                LogWriteDebug($"❌ ไม่สามารถเขียน Register 2 เป็น {reg2Value} ได้ ขอยกเลิกการทดสอบ", Color.Red);
                return false;
            }

            LogWriteDebug("⏳ กำลังรอให้ Register 2 อัปเดตบน Hardware...", Color.Gray);

            for (int timeoutCounter = 0; timeoutCounter < 10; timeoutCounter++)
            {
                short readBackReg2 = await ReadSingleRegisterAsync(2);
                // 🎯 เช็กค่ากลับมาตามตัวแปร reg2Value
                if (readBackReg2 == reg2Value)
                {
                    LogToRx($"ปรับ Input เป็น {reg2Value} สำเร็จ", Color.Green);
                    LogWriteDebug($"✅ Register 2 เปลี่ยนเป็น {reg2Value} เรียบร้อยแล้ว เริ่มการทดสอบ...", Color.Green);
                    return true;
                }
                await Task.Delay(500);
            }

            LogToRx($"ปรับ Input เป็น {reg2Value} ไม่สำเร็จ ยกเลิกการทดสอบ", Color.Red);
            LogWriteDebug($"❌ ข้อผิดพลาด: Hardware ไม่ยอมเปลี่ยนค่า Register 2 เป็น {reg2Value} ยกเลิกการทดสอบ", Color.Red);
            return false;
        }
        private void WriteSNToReport(string reportPath, int startRow, string sn)
        {
            using (var workbook = new XLWorkbook(reportPath))
            {
                var sheet = workbook.Worksheet(1);

                // 🟢 อัปเดตข้อมูล N3, Q3, R3
                UpdateReportHeaderInfo(sheet, txt_Lot.Text.Trim());

                sheet.Cell(startRow, 4).Value = sn;
                workbook.Save();
            }
        }
        private async Task<TestPointResult> RunSingleTestPointAsync(
                int ohm, double expectedTemp, double tolerance,
                string reportPath, int startRow, int index,
                bool isQAMode, string testType)
        {
            const short decValue = 0;

            LogToRx($"[จุดที่ {index + 1}] ส่งค่า Ohm = {ohm}Ω (คาดหวัง: {expectedTemp}°C)", Color.Black);

            bool writeOk =
                await ExecuteWriteSingleRegisterVerifiedAsync(
                    slaveId: 1,
                    registerAddress: DecimalPointRegister,
                    value: decValue,
                    maxAttempts: 5,
                    timeoutMs: 1000);

            if (writeOk)
            {
                try
                {
                    short readBackDp = await ReadSingleRegisterAsync(DecimalPointRegister);
                    if (readBackDp != decValue)
                    {
                        LogWriteDebug($"⚠️ [จุดที่ {index + 1}] Register 5 readback ไม่ตรง (ได้ {readBackDp} คาดหวัง {decValue}) แต่จะเดินหน้าต่อ", Color.Orange);
                    }
                }
                catch { /* readback ไม่ตอบ ก็ไม่เป็นไร เดินหน้าต่อ */ }
                _lastKnownDp = (ushort)decValue;
            }
            else
            {
                LogWriteDebug($"⚠️ [จุดที่ {index + 1}] เขียน Register 5 ไม่สำเร็จ แต่จะเดินหน้าวัดค่าต่อ", Color.Orange);
            }

            SendOhm(ohm);

            short rawPv = 0;
            int maxLoops = 12;
            string format = (decValue == 0) ? "F0" : "F1";

            int stableCount = 0;
            const int requiredStableReads = 2; // ต้องเข้า tolerance ติดกันกี่รอบถึงถือว่านิ่ง

            for (int loop = 0; loop < maxLoops; loop++)
            {
                try
                {
                    rawPv = await ReadSingleRegisterAsync(0);
                }
                catch { }

                double realTimePv = (decValue == 0) ? rawPv : (rawPv / 10.0);
                _currentPv = realTimePv;
                _lastKnownDp = (ushort)decValue;

                System.Windows.Forms.Control[] foundLabel0 = this.Controls.Find("Regis0", true);
                if (foundLabel0.Length > 0 && foundLabel0[0] is System.Windows.Forms.Label lbl0)
                {
                    lbl0.Text = realTimePv.ToString(format) + "°C";
                }

                System.Windows.Forms.Control[] foundStatus = this.Controls.Find("lbl_testStatus", true);
                if (foundStatus.Length > 0)
                {
                    foundStatus[0].Text = $"🎯 กำลังทดสอบจุดที่ {index + 1} ({expectedTemp}°C) | PV ปัจจุบัน = {realTimePv.ToString(format)}°C";
                    foundStatus[0].ForeColor = Color.DarkOrange;
                }

                // 🟢 เช็คว่าค่าปัจจุบันเข้า tolerance หรือยัง
                if (Math.Abs(realTimePv - expectedTemp) <= tolerance)
                {
                    stableCount++;
                    if (stableCount >= requiredStableReads)
                    {
                        LogWriteDebug($"⚡ [จุดที่ {index + 1}] ค่านิ่งเข้าเป้าแล้วที่รอบ {loop + 1}/{maxLoops} (ประหยัดเวลาไป {(maxLoops - loop - 1) * 250}ms)", Color.DarkCyan);
                        break; // นิ่งแล้ว ไปต่อทันที ไม่ต้องรอครบ 12 รอบ
                    }
                }
                else
                {
                    stableCount = 0; // หลุด tolerance เมื่อไหร่ นับใหม่
                }

                await Task.Delay(250);
            }

            double actualPv = (decValue == 0) ? rawPv : (rawPv / 10.0);
            double difference = Math.Abs(actualPv - expectedTemp);

            bool pass = writeOk && (difference <= tolerance);
            string failureReason = !writeOk
                ? "เขียน Register 5 ไม่สำเร็จ"
                : difference > tolerance
                    ? $"ค่า PV ต่างจากค่าเป้าหมายเกินเกณฑ์ ±{tolerance:0.##}°C"
                    : string.Empty;


            // ── ส่วนการบันทึกผลลง Excel ──
            if (!_skipExcelWrite)
            {
                using (var workbook = new ClosedXML.Excel.XLWorkbook(reportPath))
                {
                    var sheet = workbook.Worksheet(1);

                    if (isQAMode)
                    {
                        // โหมด QA บันทึกคอลัมน์ I (หลักที่ 9)
                        var cell = sheet.Cell(startRow + index, 9);
                        cell.Value = Math.Round(actualPv, 2);
                        cell.Style.NumberFormat.Format = "0.00";
                        cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                        // 🟢 ไฮไลต์สีเทาอ่อนเมื่อไม่ผ่านเกณฑ์ (Fail)
                        if (!pass)
                        {
                            cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(217, 217, 217);
                        }
                        else
                        {
                            cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.NoColor;
                        }
                    }
                    else
                    {
                        // โหมด QC: ค้นหาคอลัมน์แบบไดนามิกจากแถวที่ 9 หรือ 12
                        int targetColumnIndex = -1;
                        string searchKey = "";

                        switch (testType.ToLower())
                        {
                            case "ptc2k": searchKey = ") 00"; break;
                            case "ntc10ka": searchKey = ") 01"; break;
                            case "ntc2k": searchKey = ") 02"; break;
                            case "ntc10kb": searchKey = ") 03"; break;
                            default: searchKey = testType; break;
                        }

                        for (int col = 2; col <= 18; col++)
                        {
                            string headerText = sheet.Cell(9, col).GetFormattedString().Trim();
                            if (string.IsNullOrEmpty(headerText))
                            {
                                headerText = sheet.Cell(12, col).GetFormattedString().Trim();
                            }
                            if (headerText.Contains(searchKey, System.StringComparison.OrdinalIgnoreCase))
                            {
                                targetColumnIndex = col;
                                break;
                            }
                        }

                        // 🟢 FIX จุดที่ 2: หา header ไม่เจอ → ใช้ column index สำรองแทน ไม่ปล่อยผ่านเฉยๆ
                        if (targetColumnIndex == -1)
                        {
                            switch (testType.ToLower())
                            {
                                case "ptc2k": targetColumnIndex = 2; break;
                                case "ntc10ka": targetColumnIndex = 5; break;
                                case "ntc2k": targetColumnIndex = 8; break;
                                case "ntc10kb": targetColumnIndex = 11; break;
                            }
                            LogWriteDebug($"⚠️ ไม่พบ header ที่ตรงกับ '{searchKey}' ใช้ column index สำรองแทน", Color.Orange);
                        }

                        if (targetColumnIndex != -1)
                        {
                            var cell = sheet.Cell(startRow, targetColumnIndex + index);
                            cell.Value = Math.Round(actualPv, 2);
                            cell.Style.NumberFormat.Format = "0.00";
                            cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                            // 🟢 ไฮไลต์สีเทาอ่อนเมื่อไม่ผ่านเกณฑ์ (Fail)
                            if (!pass)
                            {
                                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(217, 217, 217);
                            }
                            else
                            {
                                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.NoColor;
                            }

                            if (sheet.Cell(startRow, 1).IsEmpty())
                            {
                                sheet.Cell(startRow, 1).Value = isQAMode ? (startRow - 14) : (startRow - QcReportLayout.FirstDataRow + 1);
                                sheet.Cell(startRow, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                            }

                            var snCell = sheet.Cell(startRow, QcReportLayout.SerialColumn);
                            snCell.Value = txt_SN.Text.Trim();
                            snCell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        }
                    }

                    workbook.Save();
                }
            }
            else
            {
                LogWriteDebug($"[จุดที่ {index + 1}] (โหมดทดสอบ) ข้ามการเขียนผลลง Excel", Color.Gray);
            }

            if (pass)
                LogToRx($"-> ผลการทดสอบ: [PASS] ค่า PV จริง = {actualPv.ToString(format)}°C", Color.Green);
            else
                LogToRx($"-> ผลการทดสอบ: [FAIL] ค่า PV จริง = {actualPv.ToString(format)}°C", Color.Red);
            LogToRx("--------------------------------------------", Color.LightGray);
            return new TestPointResult(pass, actualPv, failureReason);
        }
        private void LogFinalResult(string sn, bool isAllPass)
        {
            if (isAllPass)
            {
                LogToRx($"🎉 [ S/N: {sn} ] ผล: PASS ทั้งตาราง! ", Color.DarkGreen);
            }
            else
            {
                if (_currentRunFailures.Count > 0)
                {
                    LogToRx($"❌ [ S/N: {sn} ] ผล: FAIL — จุดที่ผิดพลาด:", Color.DarkRed);
                    foreach (string failure in _currentRunFailures)
                    {
                        LogToRx($"   • {failure}", Color.Red);
                    }
                }
                else
                {
                    LogToRx($"❌ [ S/N: {sn} ] ผล: FAIL (มีบางจุดไม่ผ่านเกณฑ์การทดสอบ)", Color.DarkRed);
                }
            }
        }


        private void btn_browseTemplate_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "เลือกโฟลเดอร์ปลายทางสำหรับเซฟไฟล์รายงานผลการทดสอบ (Report)";
                folderDialog.UseDescriptionForTitle = true;

                if (!string.IsNullOrEmpty(txt_templatePath.Text) && Directory.Exists(txt_templatePath.Text))
                {
                    folderDialog.SelectedPath = txt_templatePath.Text;
                }

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txt_templatePath.Text = folderDialog.SelectedPath;
                    txt_templatePath.ForeColor = _defaultTemplatePathForeColor;
                    LogToRx($"📁 ตั้งค่าโฟลเดอร์สำหรับเซฟรายงาน: {folderDialog.SelectedPath}", Color.DarkGreen);
                    SaveConfig();
                }
            }
        }




        //── ส่วนของปุ่ม Test ──────────────────────────────
        public class TestCaseItem
        {
            public int Ohm { get; set; }
            public double ExpectedTemp { get; set; }
        }
        public struct TestRunResult
        {
            public string TestName;
            public bool IsAllPass;
            public bool ExcelSaved;
            public bool Completed;
        }

        private readonly struct TestPointResult
        {
            public bool Passed { get; }
            public double ActualPv { get; }
            public string FailureReason { get; }

            public TestPointResult(bool passed, double actualPv, string failureReason)
            {
                Passed = passed;
                ActualPv = actualPv;
                FailureReason = failureReason;
            }
        }

        private void ShowRunFailureMessage(string failureDetail)
        {
            if (_testFailureMessageShown || _userRequestedStop)
                return;

            _testFailureMessageShown = true;
            MessageBox.Show(
                this,
                $"การทดสอบไม่สำเร็จ\n\nผิดพลาดที่:\n{failureDetail}",
                "การทดสอบหยุด",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private async Task<TestRunResult> RunCalibrationTestAsync(
                string testName,
                string testType,
                TestCaseItem[] testCases,
                System.Windows.Forms.TabPage qaTab,
                System.Windows.Forms.TabPage qcTab,
                System.Windows.Forms.Button? sourceButton,
                double tolerance = 1.0,
                bool showCompletionPopup = true)
        {
            string selectedTemplatePath = !string.IsNullOrWhiteSpace(_autoReportStagingFolder)
                ? _autoReportStagingFolder
                : txt_templatePath.Text.Trim();
            string currentSN = txt_SN.Text.Trim();
            string currentLOT = txt_Lot.Text.Trim();
            bool isQAMode = _manualPopupQAChecked;

            if (!ValidateBeforeTest(selectedTemplatePath, currentSN, currentLOT))
                return new TestRunResult { TestName = testName, IsAllPass = false, ExcelSaved = false };

            SaveConfig();

            if (sourceButton != null)
            {
                sourceButton.Enabled = false;
            }

            _isCalibrating = true;

            _cts?.Dispose();
            _cts = new System.Threading.CancellationTokenSource();

            if (richTextBoxRx != null) richTextBoxRx.Clear();

            System.Windows.Forms.Control[] foundRegis0 = this.Controls.Find("Regis0", true);
            if (foundRegis0.Length > 0 && foundRegis0[0] is System.Windows.Forms.Label lblRegis0)
            {
                lblRegis0.Text = "---";
            }

            if (isQAMode)
            {
                if (qaTab != null && qaTab.Parent is System.Windows.Forms.TabControl tc) tc.SelectedTab = qaTab;
                stepTimeline?.ShowTempMode(true, testType, testCases.Select(c => $"{c.ExpectedTemp:0.#} °C"));
                LogToRx($"=== เริ่มต้นการทดสอบ {testName} [โหมด QA] ===", Color.Purple);
            }
            else
            {
                if (qcTab != null && qcTab.Parent is System.Windows.Forms.TabControl tc) tc.SelectedTab = qcTab;
                stepTimeline?.ShowTempMode(false, testType, testCases.Select(c => $"{c.ExpectedTemp:0.#} °C"));
                LogToRx($"==== เริ่มต้นการทดสอบ {testName} [โหมด QC] ===", Color.Purple);
            }

            await Task.Yield();
            await Task.Delay(100);

            for (int k = 0; k < testCases.Length; k++)
            {
                SetStepLight(k, Color.LightGray, isQAMode, testType);
            }

            string resultLabelName = isQAMode ? "lbl_QA_result" : $"lbl_QC{testType.ToUpper()}_result";
            System.Windows.Forms.Control[] resultLabelFound = this.Controls.Find(resultLabelName, true);
            System.Windows.Forms.Label? resultLabel = resultLabelFound.Length > 0 ? resultLabelFound[0] as System.Windows.Forms.Label : null;
            if (resultLabel != null)
            {
                resultLabel.Text = "---------";
                resultLabel.ForeColor = Color.Black;
            }

            bool isAllPass = true;
            bool summarySaved = true;
            bool completed = false;

            string targetReportPath = string.Empty;
            int targetStartRow = 0;

            try
            {
                if (!_skipExcelWrite)
                {
                    var (path, startRow) = GetOrCreateReportFileQC(selectedTemplatePath, currentLOT, isQAMode);
                    targetReportPath = path;
                    targetStartRow = startRow;

                    using (var workbook = new ClosedXML.Excel.XLWorkbook(targetReportPath))
                    {
                        var sheet = workbook.Worksheet(1);
                        UpdateReportHeaderInfo(sheet, currentLOT);
                        InitializeModelReportRow(sheet, targetReportPath, targetStartRow);
                        if (!isQAMode)
                        {
                            if (sheet.Cell(targetStartRow, 1).IsEmpty())
                            {
                                SetReportCell(sheet.Cell(targetStartRow, 1), (targetStartRow - QcReportLayout.FirstDataRow + 1));
                            }
                            sheet.Cell(targetStartRow, QcReportLayout.SerialColumn).Value = currentSN;
                        }

                        if (!SaveWorkbookWithRetry(workbook, targetReportPath, $"[{testName}] จองแถว/บันทึกหัวเอกสารก่อนเริ่มทดสอบ"))
                        {
                            RecordRunFailure($"{testName}: บันทึกไฟล์ Excel ไม่สำเร็จ");
                            LogToRx($"🛑 ยกเลิกการทดสอบ {testName} เนื่องจากบันทึกไฟล์ Excel ไม่สำเร็จ", Color.Red);
                            if (_runSequenceActive || _modelTestAllActive)
                                ShowRunFailureMessage($"{testName}\nสาเหตุ: บันทึกไฟล์ Excel ไม่สำเร็จ");
                            return new TestRunResult { TestName = testName, IsAllPass = false, ExcelSaved = false };
                        }
                        TrackModelReportRow(targetReportPath, targetStartRow);
                    }
                }

                // 🟢 เตรียมบอร์ด ฮาร์ดแวร์
                if (!await PrepareHardwareForCalibrationAsync(testType))
                {
                    isAllPass = false;
                    RecordRunFailure($"{testName}: ไม่สามารถตั้งค่า Register 2 บน Hardware ได้");
                    if (_runSequenceActive || _modelTestAllActive)
                    {
                        ShowRunFailureMessage(
                            $"{testName}\nสาเหตุ: ไม่สามารถตั้งค่า Register 2 บน Hardware ได้");
                    }
                    if (!_skipExcelWrite && !string.IsNullOrEmpty(targetReportPath))
                    {
                        summarySaved = WriteSummaryToReport(targetReportPath, targetStartRow, false);
                    }
                    return new TestRunResult { TestName = testName, IsAllPass = isAllPass, ExcelSaved = summarySaved, Completed = completed };
                }

                if (!_skipExcelWrite && isQAMode && !string.IsNullOrEmpty(targetReportPath))
                {
                    WriteSNToReport(targetReportPath, targetStartRow, currentSN);
                }

                for (int i = 0; i < testCases.Length; i++)
                {
                    if (_userRequestedStop || _cts.Token.IsCancellationRequested)
                    {
                        LogToRx("🛑 การทดสอบถูกสั่งหยุดโดยผู้ใช้งาน", Color.Red);
                        isAllPass = false;
                        break;
                    }

                    var test = testCases[i];
                    SetStepLight(i, Color.Yellow, isQAMode, testType);
                    LogToRx($"🚀 [PROCESS] จุดที่ {i + 1} / {testCases.Length} -> เป้าหมาย: {test.ExpectedTemp}°C", Color.Blue);

                    TestPointResult pointResult = await RunSingleTestPointAsync(
                        test.Ohm, test.ExpectedTemp, tolerance,
                        targetReportPath, targetStartRow, i, isQAMode, testType);
                    bool pointPass = pointResult.Passed;

                    if (pointPass)
                    {
                        SetStepLight(i, Color.LimeGreen, isQAMode, testType);
                    }
                    else
                    {
                        SetStepLight(i, Color.Red, isQAMode, testType);
                        isAllPass = false;

                        string reasonText = string.IsNullOrWhiteSpace(pointResult.FailureReason)
                            ? $"ค่า PV ต่างจากเป้าหมายเกินเกณฑ์ (เป้าหมาย {test.ExpectedTemp:0.##}°C, ค่าจริง {pointResult.ActualPv:0.##}°C)"
                            : pointResult.FailureReason;
                        string failDetail = $"{testName} จุดที่ {i + 1} ({test.ExpectedTemp:0.##}°C): ค่าจริง {pointResult.ActualPv:0.##}°C ({reasonText})";
                        RecordRunFailure(failDetail);

                        if (_runSequenceActive || _modelTestAllActive)
                        {
                            ShowRunFailureMessage(
                                $"{testName}\n" +
                                $"จุดที่ {i + 1}\n" +
                                $"ค่าที่ต้องการ: {test.ExpectedTemp:0.##}°C\n" +
                                $"ค่าที่อ่านได้: {pointResult.ActualPv:0.##}°C\n" +
                                $"สาเหตุ: {pointResult.FailureReason}");
                        }
                    }

                    IncrementRunProgress();

                    if ((_modelTestAllActive || _runSequenceActive) && !pointPass) break;
                    if (_userRequestedStop || _cts.Token.IsCancellationRequested)
                    {
                        LogToRx("🛑 จุดปัจจุบันเสร็จแล้ว หยุดการทดสอบตามคำสั่ง", Color.DarkOrange);
                        isAllPass = false;
                        break;
                    }
                }

                completed = !_userRequestedStop && !_cts.Token.IsCancellationRequested;
                string resText = _userRequestedStop ? "STOPPED" : isAllPass ? "PASS" : "FAIL";
                Color resColor = _userRequestedStop
                    ? Color.DarkOrange
                    : isAllPass ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);
                stepTimeline?.SetResult(resText, resColor);
                if (resultLabel != null)
                {
                    resultLabel.Text = resText;
                    resultLabel.ForeColor = resColor;
                }

                if (!_userRequestedStop && !_skipExcelWrite && !string.IsNullOrEmpty(targetReportPath))
                {
                    summarySaved = WriteSummaryToReport(targetReportPath, targetStartRow, isAllPass);
                }

                if (!_modelTestAllActive) LogFinalResult(currentSN, isAllPass);

                if (!_skipExcelWrite && showCompletionPopup)
                {
                    if (summarySaved)
                    {
                        MessageBox.Show(
                            $"บันทึกผลการทดสอบลงไฟล์ Excel เรียบร้อยแล้ว\n\nไฟล์: {Path.GetFileName(targetReportPath)}\nผลรวม: {(isAllPass ? "PASS" : "FAIL")}",
                            "บันทึกไฟล์สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"⚠️ ผลการทดสอบรันจบแล้ว แต่บันทึกลงไฟล์ Excel ไม่สำเร็จ!\n\n" +
                            $"กรุณาตรวจสอบว่าไฟล์ {Path.GetFileName(targetReportPath)} ถูกเปิดค้างอยู่หรือไม่ แล้วลองกดทดสอบใหม่",
                            "⚠️ ไม่ได้บันทึกผลลงไฟล์", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogToRx($"เกิดข้อผิดพลาดระหว่างทดสอบ: {ex.Message}", Color.Red);
                isAllPass = false;
                RecordRunFailure($"{testName}: {ex.Message}");
                if ((_runSequenceActive || _modelTestAllActive) && !_userRequestedStop)
                    ShowRunFailureMessage($"{testName}\nสาเหตุ: {ex.Message}");
                if (!_skipExcelWrite && !string.IsNullOrEmpty(targetReportPath))
                {
                    summarySaved = WriteSummaryToReport(targetReportPath, targetStartRow, false);
                    if (!summarySaved && showCompletionPopup)
                    {
                        MessageBox.Show(
                            $"เกิดข้อผิดพลาดระหว่างทดสอบ และบันทึกผลลง Excel ไม่สำเร็จด้วย!\n\nรายละเอียด: {ex.Message}",
                            "⚠️ ทดสอบล้มเหลว + บันทึกไฟล์ไม่สำเร็จ",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            finally
            {
                _isCalibrating = false;
                timer1.Start();
                if (sourceButton != null) sourceButton.Enabled = true;
                _cts?.Dispose();
                _cts = null;
            }

            return new TestRunResult { TestName = testName, IsAllPass = isAllPass, ExcelSaved = summarySaved, Completed = completed };
        }


        //── ส่วนของ การแสดงผล ──────────────────────────────
        #endregion

        #region 14 ไฟสถานะ หยุดทดสอบ และแสดงผลสรุป

        private void SetStepLight(int stepIndex, Color lightColor, bool isQAMode, string testType = "")
        {
            stepTimeline?.SetTempStepStatus(stepIndex, lightColor);

            // 🎯 ถ้า testType เป็นค่าว่าง ให้เดาว่าเป็น "ntc2k" (ปุ่มเดิมของคุณ) เพื่อไม่ให้ระบบพัง
            if (string.IsNullOrEmpty(testType))
            {
                testType = "ntc2k";
            }

            // สร้างชื่อตามโหมดและประเภทสินค้า
            string picName = isQAMode ? $"pic_qa_step{stepIndex + 1}" : $"pic_{testType.ToLower()}_step{stepIndex + 1}";

            // 🎯 ค้นหาจากตัวฟอร์มหลัก (this) แทน tabControl เพื่อตัดปัญหาเรื่องชื่อคอนโทรลไม่ตรง
            Control[] found = this.Controls.Find(picName, true);

            if (found.Length > 0 && found[0] is PictureBox pic)
            {
                int width = pic.Width > 0 ? pic.Width : 25;
                int height = pic.Height > 0 ? pic.Height : 25;

                Bitmap bmp = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    using (Brush brush = new SolidBrush(lightColor))
                    {
                        g.FillEllipse(brush, 2, 2, width - 5, height - 5);
                    }

                    using (Pen pen = new Pen(Color.DimGray, 1))
                    {
                        g.DrawEllipse(pen, 2, 2, width - 5, height - 5);
                    }
                }
                pic.Image = bmp;
                pic.Refresh();
            }
        }

        private void SetRunStepLight(int stepIndex, Color lightColor)
        {
            stepTimeline?.SetOverviewStepStatus(stepIndex, lightColor);

            Control[] found = Controls.Find($"step{stepIndex + 1}", true);
            if (found.Length == 0 || found[0] is not PictureBox pic)
                return;

            int width = pic.Width > 0 ? pic.Width : 40;
            int height = pic.Height > 0 ? pic.Height : 40;
            Bitmap bmp = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using Brush brush = new SolidBrush(lightColor);
                using Pen pen = new Pen(Color.DimGray, 1);
                g.FillEllipse(brush, 2, 2, width - 5, height - 5);
                g.DrawEllipse(pen, 2, 2, width - 5, height - 5);
            }

            Image? oldImage = pic.Image;
            pic.Image = bmp;
            oldImage?.Dispose();
            pic.Refresh();
        }

        private void ResetRunStepLights()
        {
            stepTimeline?.ResetOverview();
            for (int i = 0; i < 4; i++)
                SetRunStepLight(i, Color.LightGray);
        }

        private void ShowRunOverview()
        {
            stepTimeline?.ShowOverviewMode();
            if (!tabPage6.IsDisposed)
                tabcontrol.SelectedTab = tabPage6;
        }


        private CancellationTokenSource? _cts;
        private void btn_StopTest_Click(object sender, EventArgs e)
        {
            _userRequestedStop = true;
            LogToRx("🛑 รับคำสั่ง Stop แล้ว — จะหยุดหลังจากจุดปัจจุบันเสร็จ", Color.DarkRed);
        }



        private StringBuilder plainTextAccumulator = new StringBuilder();
        private void ShowTestAllSummary(List<TestRunResult> results)
        {
            if (results.Count == 0)
            {
                MessageBox.Show("การทดสอบถูกยกเลิกก่อนเริ่มรันจุดใด ๆ", "Test All ไม่สำเร็จ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool overallPass = results.All(r => r.IsAllPass);
            bool overallSaved = results.All(r => r.ExcelSaved);

            var sb = new StringBuilder();
            sb.AppendLine($"ทดสอบเสร็จสิ้น {results.Count}/4 ประเภท");
            sb.AppendLine();
            foreach (var r in results)
            {
                string passText = r.IsAllPass ? "✅ PASS" : "❌ FAIL";
                string saveText = r.ExcelSaved ? "บันทึกสำเร็จ" : "⚠️ บันทึกไม่สำเร็จ";
                sb.AppendLine($"• {r.TestName}: {passText} ({saveText})");
            }

            string title = overallPass
                ? (overallSaved ? "🎉 Test เสร็จสิ้น - PASS ทั้งหมด" : "⚠️ Test เสร็จสิ้น - PASS แต่บันทึกไฟล์มีปัญหา")
                : (overallSaved ? "Test เสร็จสิ้น - มีจุด FAIL" : "⚠️ Test เสร็จสิ้น - FAIL และบันทึกไฟล์มีปัญหา");

            var icon = overallPass && overallSaved ? MessageBoxIcon.Information : MessageBoxIcon.Warning;

            MessageBox.Show(sb.ToString(), title, MessageBoxButtons.OK, icon);
        }
        #endregion

        #region 15 Calibration แรงดัน ความต้านทาน และเปลี่ยนโหมด


        private void ParsePlainText(byte[] buffer)
        {
            try
            {
                // แปลง bytes ที่ได้รับมาเป็นข้อความ String
                string receivedText = System.Text.Encoding.ASCII.GetString(buffer);

                foreach (char c in receivedText)
                {
                    // สะสมตัวอักษรไว้จนกว่าจะเจอการขึ้นบรรทัดใหม่ (\n หรือ \r)
                    if (c == '\n' || c == '\r')
                    {
                        string line = plainTextAccumulator.ToString().Trim();
                        if (!string.IsNullOrEmpty(line))
                        {
                            // 🎯 เช็คว่าข้อความมีรูปแบบตามที่บอร์ดส่งมาหรือไม่ (เช่น ขึ้นต้นด้วย ADC=)
                            if (line.StartsWith("ADC="))
                            {
                                // ส่งไปแสดงผลบน richTextBoxRx เป็นตัวหนาสีน้ำตาลส้มสวย ๆ เหมือนในรูปเลยครับ
                                this.BeginInvoke(new MethodInvoker(() =>
                                {
                                    LogToRx($" {line}", Color.SaddleBrown);
                                }));
                            }
                        }
                        plainTextAccumulator.Clear();
                    }
                    else
                    {
                        // ถ้ายังไม่ขึ้นบรรทัดใหม่ ให้สะสมตัวอักษรไว้ก่อน
                        plainTextAccumulator.Append(c);
                    }
                }
            }
            catch
            {
                // ป้องกัน Error กรณีเจอบอร์ดส่งข้อมูลขยะที่ไม่ได้เป็น ASCII ออกมา
            }
        }



        // ── บันทึกผลสรุป Inspection ลง Excel (คอลัมน์ P = ปกติ / คอลัมน์ Q = ไม่ปกติ) ──
        #endregion

        #region 16 บันทึกผลและหัวรายงาน Excel

        private bool WriteSummaryToReport(string filePath, int targetRow, bool isAllPass)
        {
            // QC Test For All writes a single verdict after temperature and contact checks.
            if (_modelTestAllActive) return true;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;

            using (var workbook = new ClosedXML.Excel.XLWorkbook(filePath))
            {
                var sheet = workbook.Worksheet(1);
                UpdateReportHeaderInfo(sheet, txt_Lot.Text.Trim());

                if (!_manualPopupQAChecked)
                {
                    if (!isAllPass) QcReportLayout.SetAutomatedResult(sheet, targetRow, txt_model.Text.Trim(), false);
                    QcReportLayout.RefreshInspection(sheet, targetRow, txt_model.Text.Trim());
                    return SaveWorkbookWithRetry(workbook, filePath, "บันทึกผล QC");
                }

                if (isAllPass)
                {
                    SetReportCell(sheet.Cell(targetRow, "P"), "OK");
                    sheet.Cell(targetRow, "Q").Value = "";
                }
                else
                {
                    sheet.Cell(targetRow, "P").Value = "";
                    SetReportCell(sheet.Cell(targetRow, "Q"), "NG");
                }

                bool saved = SaveWorkbookWithRetry(workbook, filePath, "บันทึกผลสรุป Inspection");
                if (saved)
                    LogToRx($"📝 บันทึกผล Inspection ({(isAllPass ? "OK" : "NG")}) เรียบร้อยแล้ว", Color.DarkGreen);

                return saved;
            }
        }


        private void SetReportCell(IXLCell cell, object value, string? numberFormat = null, XLAlignmentHorizontalValues alignment = XLAlignmentHorizontalValues.Center)
        {
            cell.Value = ClosedXML.Excel.XLCellValue.FromObject(value);
            cell.Style.Alignment.Horizontal = alignment;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            if (!string.IsNullOrEmpty(numberFormat))
            {
                cell.Style.NumberFormat.Format = numberFormat;
            }
        }
        private static IXLCell? FindHeaderCell(IXLWorksheet sheet, string keyword, int startRow = 1, int endRow = 10)
        {
            for (int r = startRow; r <= endRow; r++)
            {
                for (int c = 1; c <= 24; c++)
                {
                    string text = sheet.Cell(r, c).GetString().Trim();
                    if (!string.IsNullOrEmpty(text) && text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return sheet.Cell(r, c);
                    }
                }
            }
            return null;
        }

        private void UpdateReportHeaderInfo(IXLWorksheet sheet, string lotName, bool? qaMode = null)
        {
            bool isQa = qaMode ?? _manualPopupQAChecked;
            if (!isQa) QcReportLayout.Validate(sheet);
            UpdateModelReportHeader(sheet, isQa);

            if (isQa)
            {
                SetReportCell(sheet.Cell("N3"), lotName, alignment: XLAlignmentHorizontalValues.Left);
                SetReportCell(sheet.Cell("L5"), $"ECN NO:{txt_ECN.Text.Trim()}", alignment: XLAlignmentHorizontalValues.Left);
                SetReportCell(sheet.Cell("N5"), GetOhmSourceInstrumentText(), alignment: XLAlignmentHorizontalValues.Left);
                SetReportCell(sheet.Cell("N9"), _currentFirmware.Trim(), alignment: XLAlignmentHorizontalValues.Left);
            }
            else
            {
                // QC: ค้นหาตำแหน่งเซลล์จากชื่อ/คำสำคัญ
                var lotCell = FindHeaderCell(sheet, "Lot:");
                if (lotCell != null)
                    SetReportCell(sheet.Cell(lotCell.Address.RowNumber, lotCell.Address.ColumnNumber + 1), lotName, alignment: XLAlignmentHorizontalValues.Left);
                else
                    SetReportCell(sheet.Cell("N2"), lotName, alignment: XLAlignmentHorizontalValues.Left);

                var ecnCell = FindHeaderCell(sheet, "ECN NO") ?? FindHeaderCell(sheet, "ECN");
                if (ecnCell != null)
                    SetReportCell(ecnCell, $"ECN NO:{txt_ECN.Text.Trim()}", alignment: XLAlignmentHorizontalValues.Left);

                var instCell = FindHeaderCell(sheet, "เครื่องมือ");
                if (instCell != null)
                    SetReportCell(instCell, GetOhmSourceInstrumentText(), alignment: XLAlignmentHorizontalValues.Left);

                var fwCell = FindHeaderCell(sheet, "FW");
                if (fwCell != null)
                    SetReportCell(fwCell, _currentFirmware.Trim(), alignment: XLAlignmentHorizontalValues.Left);
            }

            int testedSNCount = 0;
            int lastReportRow = sheet.LastRowUsed()?.RowNumber() ?? 500;
            for (int row = isQa ? 15 : QcReportLayout.FirstDataRow; row <= lastReportRow; row++)
            {
                if ((isQa || QcReportLayout.IsDataRow(sheet, row)) && !sheet.Cell(row, isQa ? 18 : QcReportLayout.SerialColumn).IsEmpty())
                {
                    testedSNCount++;
                }
            }

            if (isQa)
            {
                SetReportCell(sheet.Cell("Q3"), testedSNCount, alignment: XLAlignmentHorizontalValues.Center);
                SetReportCell(sheet.Cell("R3"), $"Date: {DateTime.Now.ToString("dd/MM/yy", System.Globalization.CultureInfo.GetCultureInfo("th-TH"))}");

                string revision = _currentRevision.Trim();
                sheet.Cell("R41").Value = revision;
                sheet.Cell("R66").Value = revision;
            }
            else
            {
                var lotSizeCell = FindHeaderCell(sheet, "Lot Size:");
                if (lotSizeCell != null)
                    SetReportCell(sheet.Cell(lotSizeCell.Address.RowNumber, lotSizeCell.Address.ColumnNumber + 1), testedSNCount, alignment: XLAlignmentHorizontalValues.Center);
                else
                    SetReportCell(sheet.Cell("T2"), testedSNCount, alignment: XLAlignmentHorizontalValues.Center);

                var dateCell = FindHeaderCell(sheet, "Date:");
                if (dateCell != null)
                    SetReportCell(dateCell, $"Date: {DateTime.Now.ToString("dd/MM/yy", System.Globalization.CultureInfo.GetCultureInfo("th-TH"))}");
                else
                    SetReportCell(sheet.Cell("X2"), $"Date: {DateTime.Now.ToString("dd/MM/yy", System.Globalization.CultureInfo.GetCultureInfo("th-TH"))}");
            }
        }

        private bool SaveWorkbookWithRetry(XLWorkbook workbook, string filePath, string context)
        {
            while (true)
            {
                try
                {
                    QcReportLayout.UpdateSummary(workbook.Worksheet(1), txt_name.Text.Trim());
                    QcReportLayout.ProtectTestResults(workbook.Worksheet(1));
                    workbook.Save();
                    return true;
                }
                catch (IOException) // ไฟล์ถูกเปิดค้างอยู่ (Excel เปิดอยู่ / OneDrive sync ล็อก)
                {
                    var result = MessageBox.Show(
                        $"ไม่สามารถบันทึกไฟล์ Excel ได้ เนื่องจากไฟล์ถูกเปิดอยู่หรือถูกล็อกโดยโปรแกรมอื่น\n\n" +
                        $"ไฟล์: {Path.GetFileName(filePath)}\nขั้นตอน: {context}\n\n" +
                        $"กรุณาปิดไฟล์ Excel แล้วกด Retry เพื่อบันทึกใหม่อีกครั้ง\n(กด Cancel เพื่อข้ามการบันทึกจุดนี้)",
                        "บันทึกไฟล์ไม่สำเร็จ - ไฟล์ถูกเปิดอยู่",
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Warning);

                    LogToRx($"❌ [SAVE FAIL] {context}: ไฟล์ {Path.GetFileName(filePath)} ถูกเปิดอยู่", Color.Red);

                    if (result == DialogResult.Cancel)
                        return false;

                    // ถ้ากด Retry วนกลับไป workbook.Save() ใหม่
                }
                catch (Exception ex) // error อื่น ๆ ที่ไม่ใช่ไฟล์ถูกล็อก
                {
                    MessageBox.Show(
                        $"เกิดข้อผิดพลาดในการบันทึก Excel\n\nไฟล์: {Path.GetFileName(filePath)}\nขั้นตอน: {context}\n\nรายละเอียด: {ex.Message}",
                        "บันทึกไฟล์ไม่สำเร็จ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    LogToRx($"❌ [SAVE FAIL] {context}: {ex.Message}", Color.Red);
                    return false;
                }
            }
        }






        //
        private class TestDefinition
        {
            public string TestName = "";
            public string TestType = "";
            public System.Windows.Forms.TabPage QaTab = null!;
            public System.Windows.Forms.TabPage QcTab = null!;
            public Func<bool, TestCaseItem[]> GetTestCases = null!;
        }

        #endregion

        #region 17 ชุดข้อมูลและนิยามการทดสอบอุณหภูมิ

        private TestCaseItem[] GetPTC2KCases(bool isQAMode) => isQAMode
            ? new TestCaseItem[] {
                new TestCaseItem { Ohm = 1120, ExpectedTemp = -40.0 },
                new TestCaseItem { Ohm = 1360, ExpectedTemp = -20.0 },
                new TestCaseItem { Ohm = 1637, ExpectedTemp = 0.0 },
                new TestCaseItem { Ohm = 1940, ExpectedTemp = 20.0 },
                new TestCaseItem { Ohm = 2284, ExpectedTemp = 40.0 },
                new TestCaseItem { Ohm = 2654, ExpectedTemp = 60.0 },
                new TestCaseItem { Ohm = 3055, ExpectedTemp = 80.0 },
                new TestCaseItem { Ohm = 3485, ExpectedTemp = 100.0 },
                new TestCaseItem { Ohm = 4137, ExpectedTemp = 130.0 }
              }
            : new TestCaseItem[] {
                new TestCaseItem { Ohm = 1120, ExpectedTemp = -40.0 },
                new TestCaseItem { Ohm = 2283, ExpectedTemp = 40.0 },
                new TestCaseItem { Ohm = 4135, ExpectedTemp = 130.0 }
        };
        private TestCaseItem[] GetNTC10K01Cases(bool isQAMode) => isQAMode
                ? new TestCaseItem[] {
            new TestCaseItem { Ohm = 35640, ExpectedTemp = -40.0 },
            new TestCaseItem { Ohm = 13113, ExpectedTemp = -20.0 },
            new TestCaseItem { Ohm = 5397,  ExpectedTemp = 0.0 },
            new TestCaseItem { Ohm = 2489,  ExpectedTemp = 20.0 },
            new TestCaseItem { Ohm = 1158,  ExpectedTemp = 40.0 },
            new TestCaseItem { Ohm = 589,   ExpectedTemp = 60.0 },
            new TestCaseItem { Ohm = 318,   ExpectedTemp = 80.0 },
            new TestCaseItem { Ohm = 180,   ExpectedTemp = 100.0 },
            new TestCaseItem { Ohm = 86,    ExpectedTemp = 130.0 }
                  }
                : new TestCaseItem[] {
            new TestCaseItem { Ohm = 233700, ExpectedTemp = -40.0 },
            new TestCaseItem { Ohm = 1016,   ExpectedTemp = 100.0 },
            new TestCaseItem { Ohm = 622,    ExpectedTemp = 120.0 }
        };
        private TestCaseItem[] GetNTC2KCases(bool isQAMode) => isQAMode
                ? new TestCaseItem[] {
            new TestCaseItem { Ohm = 35640, ExpectedTemp = -40.0 },
            new TestCaseItem { Ohm = 13113, ExpectedTemp = -20.0 },
            new TestCaseItem { Ohm = 5397,  ExpectedTemp = 0.0 },
            new TestCaseItem { Ohm = 2489,  ExpectedTemp = 20.0 },
            new TestCaseItem { Ohm = 1158,  ExpectedTemp = 40.0 },
            new TestCaseItem { Ohm = 589,   ExpectedTemp = 60.0 },
            new TestCaseItem { Ohm = 318,   ExpectedTemp = 80.0 },
            new TestCaseItem { Ohm = 180,   ExpectedTemp = 100.0 },
            new TestCaseItem { Ohm = 86,    ExpectedTemp = 130.0 }
                  }
                : new TestCaseItem[] {
            new TestCaseItem { Ohm = 13212, ExpectedTemp = -20.0 },
            new TestCaseItem { Ohm = 1165,  ExpectedTemp = 40.0 },
            new TestCaseItem { Ohm = 111,   ExpectedTemp = 120.0 }
        };
        private TestCaseItem[] GetNTC10K03Cases(bool isQAMode) => isQAMode
                ? new TestCaseItem[] {
            new TestCaseItem { Ohm = 279158, ExpectedTemp = -40.0 },
            new TestCaseItem { Ohm = 86749,  ExpectedTemp = -20.0 },
            new TestCaseItem { Ohm = 31265,  ExpectedTemp = 0.0 },
            new TestCaseItem { Ohm = 12431,  ExpectedTemp = 20.0 },
            new TestCaseItem { Ohm = 5339,   ExpectedTemp = 40.0 },
            new TestCaseItem { Ohm = 2457,   ExpectedTemp = 60.0 },
            new TestCaseItem { Ohm = 1220,   ExpectedTemp = 80.0 },
            new TestCaseItem { Ohm = 649,    ExpectedTemp = 100.0 },
            new TestCaseItem { Ohm = 281,    ExpectedTemp = 130.0 }
                  }
                : new TestCaseItem[] {
            new TestCaseItem { Ohm = 279158, ExpectedTemp = -40.0 },
            new TestCaseItem { Ohm = 2457,   ExpectedTemp = 60.0 },
            new TestCaseItem { Ohm = 367,    ExpectedTemp = 120.0 }
       };


        private List<TestDefinition> GetTestDefinitions()
        {
            return new List<TestDefinition>
            {
            new TestDefinition { TestName = "PTC 2K TEST",       TestType = "ptc2k",   QaTab = tabPage1, QcTab = tabPage2, GetTestCases = GetPTC2KCases },
            new TestDefinition { TestName = "NTC 10K TEST (01)", TestType = "ntc10ka", QaTab = tabPage1, QcTab = tabPage5, GetTestCases = GetNTC10K01Cases },
            new TestDefinition { TestName = "NTC 2K TEST",       TestType = "ntc2k",   QaTab = tabPage1, QcTab = tabPage3, GetTestCases = GetNTC2KCases },
            new TestDefinition { TestName = "NTC 10K TEST (03)", TestType = "ntc10kb", QaTab = tabPage1, QcTab = tabPage4, GetTestCases = GetNTC10K03Cases },
            };
        }
        private bool _userRequestedStop = false;

        #endregion

        #region 18 ตรวจ S/N และป้องกันหมายเลขซ้ำ

        private void CheckSNWhileTyping()
        {
            string sn = txt_SN.Text.Trim();
            string outputFolder = txt_templatePath.Text.Trim();
            string lotName = txt_Lot.Text.Trim();

            _isDuplicateSN = false;
            _duplicateSNRow = -1;
            _lastCheckedSN = string.Empty;

            // SN ว่าง ให้แสดงสีปกติ
            if (string.IsNullOrWhiteSpace(sn))
            {
                ResetSnColorsToDefault();
                return;
            }

            // ถ้ายังเลือกโฟลเดอร์หรือกรอก LOT ไม่ครบ
            // จะยังไม่แสดงว่า SN ผ่าน
            if (string.IsNullOrWhiteSpace(outputFolder) ||
                !Directory.Exists(outputFolder) ||
                string.IsNullOrWhiteSpace(lotName))
            {
                ResetSnColorsToDefault();
                return;
            }

            string safeLotName = string.Join(
                "_",
                lotName.Split(Path.GetInvalidFileNameChars()));

            string reportPath =
                Path.Combine(outputFolder, $"{safeLotName}.xlsx");

            // ยังไม่มีไฟล์ LOT นี้ แสดงว่า SN ไม่ซ้ำ
            if (!File.Exists(reportPath))
            {
                _lastCheckedSN = sn;
                txt_SN.ForeColor = Color.White;
                txt_SN.BackColor = Color.SeaGreen;
                return;
            }

            try
            {
                using var snapshot = new MemoryStream();
                using (var source = new FileStream(reportPath, FileMode.Open,
                           FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    source.CopyTo(snapshot);
                snapshot.Position = 0;
                using var workbook = new XLWorkbook(snapshot);
                var sheet = workbook.Worksheet(1);

                // ตรวจ SN ถึงแถวสุดท้าย (เริ่มที่แถว 12 สำหรับ QC ใหม่ หรือแถว 15 สำหรับ QA)
                int lastReportRow = sheet.LastRowUsed()?.RowNumber() ?? 500;
                int startRow = _manualPopupQAChecked ? 15 : QcReportLayout.FirstDataRow;
                int serialCol = _manualPopupQAChecked ? 18 : QcReportLayout.SerialColumn;

                for (int row = startRow; row <= lastReportRow; row++)
                {
                    if (!_manualPopupQAChecked && !QcReportLayout.IsDataRow(sheet, row))
                        continue;

                    string existingSN = sheet.Cell(row, serialCol).GetString().Trim();
                    if (string.IsNullOrEmpty(existingSN))
                        existingSN = sheet.Cell(row, serialCol).GetFormattedString().Trim();

                    if (!string.IsNullOrEmpty(existingSN) &&
                        existingSN.Equals(
                            sn,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        _isDuplicateSN = true;
                        _duplicateSNRow = row;
                        _lastCheckedSN = sn;

                        // ใช้สีแดงเข้มกว่าเดิม
                        txt_SN.ForeColor = Color.White;
                        txt_SN.BackColor = Color.Firebrick;
                        return;
                    }
                }

                // ตรวจครบแล้วไม่พบ SN ซ้ำ
                _lastCheckedSN = sn;
                txt_SN.ForeColor = Color.White;
                txt_SN.BackColor = Color.SeaGreen;
            }
            catch (Exception ex)
            {
                // ระหว่างพิมพ์ไม่ต้องเด้งข้อความรบกวน
                ResetSnColorsToDefault();

                _isDuplicateSN = false;
                _duplicateSNRow = -1;
                _lastCheckedSN = string.Empty;
                LogToRx($"⚠️ ตรวจ S/N ไม่สำเร็จ: {ex.Message}", Color.Orange);
            }
        }

        private bool EnsureReportFileClosedBeforeRun(string outputFolder, string lotName)
        {
            if (string.IsNullOrWhiteSpace(outputFolder) ||
                !Directory.Exists(outputFolder) ||
                string.IsNullOrWhiteSpace(lotName))
            {
                return true;
            }

            string safeLotName = string.Join(
                "_",
                lotName.Split(Path.GetInvalidFileNameChars()));
            string reportPath = Path.Combine(outputFolder, $"{safeLotName}.xlsx");

            if (!File.Exists(reportPath))
                return true;

            try
            {
                using var stream = new FileStream(
                    reportPath,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None);
                return true;
            }
            catch (IOException)
            {
                MessageBox.Show(
                    this,
                    $"ไม่สามารถเริ่มการทดสอบได้ เนื่องจากไฟล์ Excel ยังเปิดอยู่\n\n" +
                    $"ไฟล์: {Path.GetFileName(reportPath)}\n\n" +
                    "กรุณาปิดไฟล์ Excel เดิมก่อน แล้วกด Run ใหม่อีกครั้ง",
                    "กรุณาปิดไฟล์ Excel",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    this,
                    $"ไม่สามารถเข้าถึงไฟล์รายงานได้\n\nไฟล์: {Path.GetFileName(reportPath)}\n\n" +
                    "กรุณาตรวจสอบสิทธิ์ของไฟล์ หรือปิด Excel แล้วลองใหม่อีกครั้ง",
                    "ไม่สามารถเปิดไฟล์รายงาน",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
        }

        #endregion

        #region 19 ปุ่ม Run Stop และลำดับ Cal + Test

        private async void btnRun_Click(object sender, EventArgs e)
        {
            if (_isCalibrating || _modelTestAllActive || _buttonTestActive)
            {
                MessageBox.Show("โปรแกรมกำลังทำงานอยู่ กรุณารอให้เสร็จหรือกด Stop ก่อนครับ",
                                "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // เริ่มรอบใหม่ ให้แสดงเฉพาะข้อความของรอบการทำงานล่าสุด
            richTextBoxRx.Clear();
            _testFailureMessageShown = false;

            bool doCal = chkCalibration.Checked;
            bool doTest = chkTestForAll.Checked;
            bool modelQcTest = doTest && !_manualPopupQAChecked;
            if (!ValidateTestModel(out _)) return;

            // ⚠️ ป้องกันกรณีเลือก QA ร่วมกับการทดสอบอื่น
            if (_manualPopupQAChecked && doCal)
            {
                MessageBox.Show("ไม่สามารถเลือกรายการอื่นร่วมกับโหมด QA ได้ครับ\nหากต้องการทดสอบ QA ต้องเลือกโหมด QA อย่างเดียวเท่านั้น",
                                "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _skipExcelWrite = false;

            if (!doCal && !doTest)
            {
                MessageBox.Show("กรุณาเลือกอย่างน้อย 1 อย่าง (Calibration หรือ Test For All)",
                                "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (doTest)
            {
                if (string.IsNullOrWhiteSpace(txt_name.Text))
                {
                    MessageBox.Show("กรุณากรอกชื่อผู้ทดสอบก่อนเริ่มการทดสอบ!", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txt_name.Focus();
                    return;
                }

                string currentSN = txt_SN.Text.Trim();

                // SN ว่าง ใช้พฤติกรรมเดิม
                if (string.IsNullOrWhiteSpace(currentSN))
                {
                    MessageBox.Show(
                        "กรุณากรอก Serial Number (S/N) ก่อนเริ่มการทดสอบครับ",
                        "แจ้งเตือน",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    txt_SN.Focus();
                    return;
                }

                if (!EnsureReportFileClosedBeforeRun(
                        txt_templatePath.Text.Trim(),
                        txt_Lot.Text.Trim()))
                {
                    return;
                }

                if (!ValidateLotModelBeforeTest(
                        txt_templatePath.Text.Trim(),
                        txt_Lot.Text.Trim(),
                        showMessage: true,
                        out _))
                {
                    return;
                }

                // ตรวจซ้ำด้วย LOT/Path/โหมดล่าสุดก่อนเริ่ม Run เสมอ
                _snCheckTimer.Stop();
                CheckSNWhileTyping();

                // อ่านรายงานไม่สำเร็จหรือข้อมูลประกอบยังไม่พร้อม
                if (!_lastCheckedSN.Equals(
                        currentSN,
                        StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        "ตรวจสอบ S/N ไม่สำเร็จ กรุณาตรวจสอบ LOT, Report Path " +
                        "และไฟล์ Excel แล้วลองอีกครั้ง",
                        "ตรวจสอบ S/N ไม่สำเร็จ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                // SN ซ้ำจึงถามว่าจะทดสอบทับหรือไม่
                if (_isDuplicateSN)
                {
                    DialogResult answer = MessageBox.Show(
                        $"S/N: {currentSN} มีอยู่ในรายงานแล้ว\n" +
                        $"ตำแหน่ง: {(_manualPopupQAChecked ? "R" : "X")}{_duplicateSNRow}\n\n" +
                        "ต้องการทดสอบซ้ำและเขียนผลทับข้อมูลเดิมหรือไม่?",
                        "พบ Serial Number ซ้ำ",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (answer != DialogResult.Yes)
                    {
                        txt_SN.Focus();
                        txt_SN.SelectAll();
                        return;
                    }
                }
            }

            _userRequestedStop = false;
            _enableRunProgress = true;
            _runSequenceActive = true;
            ClearRunFailures();

            ResetRunStepLights();
            ShowRunOverview();

            btnRun.Enabled = false;
            btnStop.Enabled = true;

            SetAllButtonsEnabled(false, btnRun, btnStop);

            _calStopwatch.Reset();
            _calStopwatch.Start();
            timerCal.Start();

            // 🟢 [แก้ใหม่] คำนวณจำนวนจุดวัดจริงทั้งหมด (Cal 28 จุดคงที่ + Test ตามจำนวนจุดของแต่ละเซนเซอร์)
            bool isQAModeForProgress = _manualPopupQAChecked;

            int calPoints = doCal ? (7 + 11 + 10) : 0; // PTC2K 7 + NTC2K 11 + NTC10K 10

            int testPoints = 0;
            if (doTest)
            {
                var defsForCount = isQAModeForProgress
                    ? GetTestDefinitions().Where(d => d.TestType == "ntc2k").ToList()
                    : GetTestDefinitions();
                testPoints = defsForCount.Sum(d => d.GetTestCases(isQAModeForProgress).Length);
            }

            if (modelQcTest)
            {
                // Relay / Alarm
                testPoints += txt_model.Text == ModelAB ? 2 : 1;
                // ปุ่ม F, ลด, เพิ่ม
                testPoints += 3;
            }

            int totalSteps = calPoints + testPoints;

            progressBarRun.Minimum = 0;
            progressBarRun.Maximum = Math.Max(totalSteps, 1); // กัน error กรณี 0
            progressBarRun.Value = 0;
            lblProgressPercent.Text = "0%";

            var runInputStates = new Dictionary<Control, bool>();
            foreach (string name in new[]
                     {
                         "txt_name", "txt_SN", "txt_Lot", "txt_templatePath",
                         "txt_ECN",
                         "txt_model", "txt_supply", "sent_server"
                     })
            {
                Control? control = Controls.Find(name, true).FirstOrDefault();
                if (control == null) continue;
                runInputStates[control] = control.Enabled;
                control.Enabled = false;
            }

            try
            {
                bool calOk = true;

                if (doCal)
                {
                    SetRunStepLight(0, Color.Gold);
                    LogToRx("=== [1] เริ่มต้น Calibration ===", Color.Purple);
                    calOk = await RunCalibrationStepAsync();
                    ShowRunOverview();
                    SetRunStepLight(0, calOk ? Color.LimeGreen : Color.Red);
                }

                // ถ้า cal ล้มเหลว/ถูกหยุด หรือผู้ใช้กด Stop ระหว่างนั้น -> ไม่ไปต่อ Test
                if (doTest && calOk && !_userRequestedStop)
                {
                    SetRunStepLight(1, Color.Gold);
                    LogToRx("=== [2] เริ่มต้น Test For All ===", Color.Purple);
                    await RunTestAllStepAsync();
                }
                else if (doTest && (!calOk || _userRequestedStop))
                {
                    LogToRx("⏭️ ข้าม Test For All เนื่องจาก Calibration ไม่สำเร็จ/ถูกหยุด", Color.Orange);
                    if (!_userRequestedStop)
                    {
                        LogFinalResult(txt_SN.Text.Trim(), false);
                        ShowRunFailureMessage(_currentRunFailures.Count > 0
                            ? string.Join(Environment.NewLine, _currentRunFailures)
                            : "Calibration ไม่สำเร็จ");
                    }
                }
            }
            finally
            {
                _calStopwatch.Stop();
                timerCal.Stop();

                SetAllButtonsEnabled(true);

                btnRun.Enabled = true;
                btnStop.Enabled = false;

                foreach (var item in runInputStates)
                {
                    if (!item.Key.IsDisposed)
                        item.Key.Enabled = item.Value;
                }

                btn_equipment.Enabled = true;

                // หลังออกจากกระบวนการ Run ห้ามส่วนอื่นอัปเดต Progress
                _enableRunProgress = false;
                _runSequenceActive = false;

                // หาก Run จบเพราะพอร์ตหลุด ให้สถานะสุดท้ายเหมือนกด Refresh จริง ๆ
                // ไม่ปล่อยให้ continuation ของงานที่ถูกยกเลิกกลับมาเปลี่ยนไฟเป็นสีแดง
                if (_disconnectInProgress)
                    ResetUiAfterConnectionLoss();

                //chkCalibration.Checked = false;
                //chkTestForAll.Checked = false;
            }
        }
        private void btnStop_Click(object sender, EventArgs e)
        {
            _userRequestedStop = true;
            _pendingRunButtonRetryTcs?.TrySetResult(false);
            LogToRx("🛑 รับคำสั่ง Stop แล้ว — จะหยุดหลังจากขั้นตอนปัจจุบันเสร็จ", Color.DarkRed);
        }
        private async void btnStartTest_Click(object sender, EventArgs e)
        {
            if (!ValidateTestModel(out _)) return;
            if (_isCalibrating || _modelTestAllActive || _buttonTestActive)
            {
                MessageBox.Show("โปรแกรมกำลังทำงานอยู่ กรุณารอให้การทดสอบปัจจุบันเสร็จสิ้น หรือกดหยุดก่อนครับ",
                                "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txt_name.Text))
            {
                MessageBox.Show("กรุณากรอกชื่อผู้ทดสอบก่อนเริ่มการทดสอบ!", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txt_name.Focus();
                return;
            }

            _userRequestedStop = false;

            string currentSN = txt_SN.Text.Trim();

            _skipExcelWrite = string.IsNullOrEmpty(currentSN);
            if (_skipExcelWrite)
            {
                LogToRx("ℹ️ ไม่ได้กรอก S/N — จะทดสอบแบบไม่บันทึกผลลง Excel", Color.Gray);
            }
            else
            {
                if (!ValidateBeforeTest(
                        txt_templatePath.Text.Trim(),
                        currentSN,
                        txt_Lot.Text.Trim()))
                {
                    return;
                }

                if (!EnsureReportFileClosedBeforeRun(
                        txt_templatePath.Text.Trim(),
                        txt_Lot.Text.Trim()))
                {
                    return;
                }

                if (!ValidateLotModelBeforeTest(
                        txt_templatePath.Text.Trim(),
                        txt_Lot.Text.Trim(),
                        showMessage: true,
                        out _))
                {
                    return;
                }
            }

            var selected = _manualPopupQAChecked
                ? GetTestDefinitions().Where(d => d.TestType == "ntc2k").ToList()
                : GetTestDefinitions().Where(d => _selectedTestTypes[d.TestType]).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("กรุณาเลือกอย่างน้อย 1 รายการที่จะทดสอบ (ติ๊ก checkbox ก่อนนะครับ)",
                                "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool isQAMode = _manualPopupQAChecked;
            var allResults = new List<TestRunResult>();

            _enableRunProgress = false;

            progressBarRun.Minimum = 0;
            progressBarRun.Maximum = 1;
            progressBarRun.Value = 0;
            lblProgressPercent.Text = "0%";

            ClearRunFailures();
            var runInputStates = new Dictionary<Control, bool>();
            foreach (string name in new[]
                     {
                         "txt_name", "txt_SN", "txt_Lot", "txt_templatePath",
                         "txt_ECN", "txt_model", "txt_supply"
                     })
            {
                Control? control = Controls.Find(name, true).FirstOrDefault();
                if (control == null) continue;
                runInputStates[control] = control.Enabled;
                control.Enabled = false;
            }

            try
            {
                LogToRx("==============================================", Color.Purple);
                LogToRx($"🌟 เริ่มต้นทดสอบตามที่เลือก ({selected.Count} รายการ) 🌟", Color.Purple);
                LogToRx("==============================================", Color.Purple);

                for (int i = 0; i < selected.Count; i++)
                {
                    var def = selected[i];
                    LogToRx($"\n▶️ [{i + 1}/{selected.Count}] เริ่มทดสอบ {def.TestName}...", Color.Blue);

                    var testCases = def.GetTestCases(isQAMode);
                    var result = await RunCalibrationTestAsync(
                        def.TestName, def.TestType, testCases, def.QaTab, def.QcTab,
                        sender as System.Windows.Forms.Button, 1.0, showCompletionPopup: false);

                    allResults.Add(result);

                    if (_userRequestedStop || _cts?.Token.IsCancellationRequested == true) break;
                    if (i < selected.Count - 1) await Task.Delay(1500);
                }

                LogToRx("\n🎉 [SUCCESS] ทดสอบตามรายการที่เลือกเสร็จสิ้น", Color.Green);
            }
            catch (Exception ex)
            {
                LogToRx($"เกิดข้อผิดพลาดในการรันระบบ Test: {ex.Message}", Color.Red);
            }
            finally
            {
                foreach (var item in runInputStates)
                {
                    if (!item.Key.IsDisposed)
                        item.Key.Enabled = item.Value;
                }

                foreach (var definition in GetTestDefinitions())
                {
                    _selectedTestTypes[definition.TestType] = false;
                }

                // ล้างเครื่องหมายติ๊กบน Popup
                if (_clearTestPopupSelections != null)
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke(
                            new Action(() =>
                                _clearTestPopupSelections?.Invoke()));
                    }
                    else
                    {
                        _clearTestPopupSelections.Invoke();
                    }
                }

                ShowTestAllSummary(allResults);
            }
        }



        private async Task<bool> RunCalibrationStepAsync()
        {
            try
            {
                _ctsCal = new CancellationTokenSource();
                await DocalibrationAsync(_ctsCal.Token);

                LogToRx("✅ Calibration เสร็จสมบูรณ์!", Color.Green);
                return true;
            }
            catch (OperationCanceledException)
            {
                timerCal.Stop();
                LogToRx("🛑 Calibration ถูกหยุดโดยผู้ใช้งาน", Color.Orange);
                return false;
            }
            catch (Exception ex)
            {
                timerCal.Stop();
                RecordRunFailure($"Calibration เกิดข้อผิดพลาด: {ex.Message}");
                LogToRx($"❌ Calibration เกิดข้อผิดพลาด: {ex.Message}", Color.Red);
                return false;
            }
            finally
            {
                if (_ctsCal != null)
                {
                    _ctsCal.Dispose();
                    _ctsCal = null;
                }
            }
        }
        private async Task RunTestAllStepAsync()
        {
            if (!_manualPopupQAChecked)
            {
                await RunModelTestAllAsync();
                return;
            }
            string currentSN = txt_SN.Text.Trim();
            if (string.IsNullOrEmpty(currentSN))
            {
                LogToRx("⚠️ ข้าม Test For All: ยังไม่ได้กรอก S/N", Color.Orange);
                ShowRunOverview();
                SetRunStepLight(1, Color.Red);
                return;
            }

            bool isQAMode = _manualPopupQAChecked;
            var defs = isQAMode
                ? GetTestDefinitions().Where(d => d.TestType == "ntc2k").ToList()
                : GetTestDefinitions();
            var allResults = new List<TestRunResult>();

            for (int i = 0; i < defs.Count; i++)
            {
                if (_userRequestedStop)
                {
                    LogToRx("🛑 Test For All ถูกหยุดโดยผู้ใช้งาน จะไม่รันรายการถัดไป", Color.Orange);
                    break;
                }

                var def = defs[i];
                LogToRx($"\n▶️ [{i + 1}/{defs.Count}] เริ่มทดสอบ {def.TestName}...", Color.Blue);

                var testCases = def.GetTestCases(isQAMode);
                var result = await RunCalibrationTestAsync(
                    def.TestName, def.TestType, testCases, def.QaTab, def.QcTab,
                    null, 1.0, showCompletionPopup: false);

                allResults.Add(result);

                if (_userRequestedStop || !result.Completed || !result.IsAllPass) break;
                if (i < defs.Count - 1) await Task.Delay(1500);
            }

            bool temperaturePass =
                allResults.Count == defs.Count &&
                allResults.All(r => r.Completed && r.IsAllPass);

            ShowRunOverview();
            SetRunStepLight(1, temperaturePass ? Color.LimeGreen : Color.Red);
            ShowTestAllSummary(allResults);
        }
        #endregion

        #region 20 สถานะการเชื่อมต่อและความคืบหน้า

        private void SetConnectionStatusUI(bool isConnected)
        {
            pnConnect.BackColor = isConnected
                ? ColorTranslator.FromHtml("#2ECC71")   // เขียว
                : ColorTranslator.FromHtml("#E63946");  // แดง

            var lbl = pnConnect.Controls.OfType<System.Windows.Forms.Label>().FirstOrDefault();
            if (lbl != null)
            {
                lbl.Text = isConnected ? "•Connect" : "•DisConnect";

                lbl.AutoSize = true; // ให้คำนวณ PreferredSize ใหม่ก่อน
                lbl.Location = new Point(
                    (pnConnect.Width - lbl.PreferredSize.Width) / 2,
                    (pnConnect.Height - lbl.PreferredSize.Height) / 2
                );
            }
        }
        private void IncrementRunProgress()
        {
            if (!_enableRunProgress)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(IncrementRunProgress));
                return;
            }

            if (progressBarRun.Value < progressBarRun.Maximum)
            {
                progressBarRun.Value++;
            }

            UpdateProgressPercent();
        }
        private void UpdateProgressPercent()
        {
            if (!_enableRunProgress)
            {
                return;
            }

            int percent = 0;

            if (progressBarRun.Maximum > progressBarRun.Minimum)
            {
                percent = (int)Math.Round(
                    (progressBarRun.Value - progressBarRun.Minimum) * 100.0 /
                    (progressBarRun.Maximum - progressBarRun.Minimum));
            }

            percent = Math.Max(0, Math.Min(100, percent));

            lblProgressPercent.Text = $"{percent}%";
        }



        //Manual Control
        private List<System.Windows.Forms.Button> _disabledButtonsCache = new List<System.Windows.Forms.Button>();
        private bool _manualPopupQAChecked = false;
        private bool _isDraggingPopup;
        private Point _popupDragOffset;
        private readonly Dictionary<string, bool> _selectedTestTypes = new Dictionary<string, bool>
            {
                { "ptc2k",   false },
                { "ntc10ka", false },
                { "ntc2k",   false },
                { "ntc10kb",   false }
            };
        private Action? _clearTestPopupSelections;
        private void SetAllButtonsEnabled(bool enabled, params Control[] except)
        {
            var exceptSet = new HashSet<Control>(except);

            if (!enabled)
            {
                // เริ่ม Run รอบใหม่ ให้ล้างรายการเก่าก่อน
                _disabledButtonsCache.Clear();

                void DisableButtons(Control.ControlCollection controls)
                {
                    foreach (Control control in controls)
                    {
                        if (control is System.Windows.Forms.Button button &&
                            !exceptSet.Contains(button) &&
                            button.Enabled)
                        {
                            // จำเฉพาะปุ่มที่ก่อนหน้านี้ใช้งานได้
                            _disabledButtonsCache.Add(button);
                            button.Enabled = false;
                        }

                        if (control.HasChildren)
                        {
                            DisableButtons(control.Controls);
                        }
                    }
                }

                DisableButtons(Controls);
            }
            else
            {
                // เปิดคืนเฉพาะปุ่มที่เมธอดนี้เคยปิด
                foreach (System.Windows.Forms.Button button
                         in _disabledButtonsCache)
                {
                    if (!button.IsDisposed)
                    {
                        button.Enabled = true;
                    }
                }

                _disabledButtonsCache.Clear();
            }
        }
        #endregion

        #region 21 Popup Manual Test

        private bool CheckManualConnections(bool main, bool ohm, bool relay)
        {
            bool isDebug = chkDebugWrite != null && chkDebugWrite.Checked;
            var missing = new List<string>();

            if (main && _serialPort?.IsOpen != true)
            {
                if (!isDebug || _defPortConnected)
                    missing.Add("DEF-03N");
            }
            if (ohm && _ohmPort?.IsOpen != true)
            {
                if (!isDebug || _ohmPortConnected)
                    missing.Add("Ohm Source");
            }
            if (relay && _relayPort?.IsOpen != true)
            {
                if (!isDebug || _relayPortConnected)
                    missing.Add("PH-01 (Relay / Alarm)");
            }

            if (missing.Count == 0) return true;
            MessageBox.Show(this, "กรุณาเชื่อมต่อ " + string.Join(", ", missing) + " ก่อนใช้งาน",
                "ยังไม่ได้เชื่อมต่อ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        private void btnSelectTestPopup_Click(object sender, EventArgs e)
        {
            if (!CheckManualConnections(true, true, false)) return;
            if (sender is not System.Windows.Forms.Button sourceButton)
                return;

            const int popupWidth = 330;

            Color panelColor =
                ColorTranslator.FromHtml("#142837");

            Color accentColor =
                ColorTranslator.FromHtml("#49DAF2");

            Color testButtonColor =
                ColorTranslator.FromHtml("#FFD900");

            Color selectButtonColor =
                ColorTranslator.FromHtml("#00A6C8");

            Color stopButtonColor =
                ColorTranslator.FromHtml("#D90012");



            // ─────────────────────────────────────
            // ToolStripDropDown
            // ─────────────────────────────────────

            var dropDown = CreateManualWindow();

            // ─────────────────────────────────────
            // Header
            // ─────────────────────────────────────

            var lblHeader =
                new System.Windows.Forms.Label
                {
                    Text = "Manual Test",
                    Width = popupWidth,
                    Height = 34,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoEllipsis = false,

                    Font = new Font(
                        "Segoe UI",
                        10F,
                        FontStyle.Bold),

                    BackColor = panelColor,
                    ForeColor = accentColor,
                    Cursor = Cursors.SizeAll
                };

            // กดเมาส์บนหัวข้อเพื่อเริ่มลาก
            lblHeader.MouseDown += (s, ev) =>
            {
                if (ev.Button != MouseButtons.Left)
                    return;

                _isDraggingPopup = true;

                _popupDragOffset =
                    dropDown.PointToClient(
                        System.Windows.Forms.Control.MousePosition);

                lblHeader.Capture = true;
            };

            // ลาก popup
            lblHeader.MouseMove += (s, ev) =>
            {
                if (!_isDraggingPopup ||
                    !dropDown.Visible ||
                    dropDown.IsDisposed)
                {
                    return;
                }

                Point mousePosition =
                    System.Windows.Forms.Control.MousePosition;

                Rectangle workingArea =
                    Screen.FromPoint(mousePosition).WorkingArea;

                int newX =
                    mousePosition.X - _popupDragOffset.X;

                int newY =
                    mousePosition.Y - _popupDragOffset.Y;

                // ป้องกัน popup หลุดออกนอกหน้าจอ
                newX = Math.Max(
                    workingArea.Left,
                    Math.Min(
                        newX,
                        workingArea.Right - dropDown.Width));

                newY = Math.Max(
                    workingArea.Top,
                    Math.Min(
                        newY,
                        workingArea.Bottom - dropDown.Height));

                dropDown.Location =
                    new Point(newX, newY);
            };

            // หยุดลาก
            lblHeader.MouseUp += (s, ev) =>
            {
                if (ev.Button != MouseButtons.Left)
                    return;

                _isDraggingPopup = false;
                lblHeader.Capture = false;
            };



            // ─────────────────────────────────────
            // QA CheckBox
            // ─────────────────────────────────────

            var chkQAPopup =
                new System.Windows.Forms.CheckBox
                {
                    Text = "ทดสอบสำหรับ QA",
                    Checked = _manualPopupQAChecked,
                    AutoSize = true,

                    Font = new Font(
                        "Segoe UI",
                        9F,
                        FontStyle.Regular),

                    ForeColor = Color.White,
                    BackColor = panelColor,

                    TextAlign = ContentAlignment.MiddleLeft,
                    CheckAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand
                };

            // chkQAPopup.CheckedChanged จะถูกผูกด้านล่างหลังสร้าง clb เสร็จ

            var pnlQARow =
                new System.Windows.Forms.Panel
                {
                    Width = popupWidth,
                    Height = 32,
                    BackColor = panelColor,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty
                };

            chkQAPopup.Location = new Point(
                popupWidth -
                chkQAPopup.PreferredSize.Width -
                10,
                5);

            pnlQARow.Controls.Add(chkQAPopup);



            // ─────────────────────────────────────
            // CheckedListBox
            // ─────────────────────────────────────

            var clb =
                new System.Windows.Forms.CheckedListBox
                {
                    Width = popupWidth - 8,
                    Height = 110,
                    Location = new Point(4, 0),

                    CheckOnClick = true,
                    IntegralHeight = false,
                    BorderStyle = BorderStyle.None,

                    Font = new Font(
                        "Segoe UI",
                        9.5F,
                        FontStyle.Regular),

                    BackColor = panelColor,
                    ForeColor = Color.White
                };

            clb.Items.Add(
                "PTC 2K (00)",
                !_manualPopupQAChecked && _selectedTestTypes["ptc2k"]);

            clb.Items.Add(
                "NTC 10K (01)",
                !_manualPopupQAChecked && _selectedTestTypes["ntc10ka"]);

            clb.Items.Add(
                "NTC 2K (02)",
                !_manualPopupQAChecked && _selectedTestTypes["ntc2k"]);

            clb.Items.Add(
                "NTC 10K (03)",
                !_manualPopupQAChecked && _selectedTestTypes["ntc10kb"]);

            bool isUpdatingQa = false;
            chkQAPopup.CheckedChanged += (s, ev) =>
            {
                if (isUpdatingQa) return;

                if (chkQAPopup.Checked)
                {
                    bool hasSelectedItem =
                        Enumerable
                            .Range(0, clb.Items.Count)
                            .Any(i => clb.GetItemChecked(i));

                    if (hasSelectedItem)
                    {
                        isUpdatingQa = true;
                        chkQAPopup.Checked = false;
                        isUpdatingQa = false;

                        MessageBox.Show(
                            "ไม่สามารถเลือกรายการอื่นร่วมกับโหมด QA ได้ครับ\nหากต้องการทดสอบ QA ต้องเลือกโหมด QA อย่างเดียวเท่านั้น",
                            "แจ้งเตือน",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }
                }

                _manualPopupQAChecked = chkQAPopup.Checked;
            };



            var pnlCheckedList =
                new System.Windows.Forms.Panel
                {
                    Width = popupWidth,
                    Height = 110,
                    BackColor = panelColor,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty
                };

            pnlCheckedList.Controls.Add(clb);



            // ─────────────────────────────────────
            // ปุ่ม TEST
            // ─────────────────────────────────────

            var btnTestPopup =
                new System.Windows.Forms.Button
                {
                    Text = "▶  TEST",
                    Location = new Point(4, 3),
                    Size = new Size(195, 36),

                    Font = new Font(
                        "Segoe UI",
                        9.5F,
                        FontStyle.Bold),

                    BackColor = testButtonColor,
                    ForeColor = Color.Black,

                    FlatStyle = FlatStyle.Flat,
                    UseVisualStyleBackColor = false,
                    Cursor = Cursors.Hand
                };

            btnTestPopup.FlatAppearance.BorderSize = 0;

            btnTestPopup.FlatAppearance.MouseOverBackColor =
                ColorTranslator.FromHtml("#FFE34D");

            btnTestPopup.FlatAppearance.MouseDownBackColor =
                ColorTranslator.FromHtml("#E5C300");

            // ─────────────────────────────────────
            // ปุ่มเลือกทั้งหมด
            // ─────────────────────────────────────

            var btnSelectAllPopup =
                new System.Windows.Forms.Button
                {
                    Location = new Point(204, 3),
                    Size = new Size(122, 36),

                    Font = new Font(
                        "Segoe UI",
                        9F,
                        FontStyle.Bold),

                    BackColor = selectButtonColor,
                    ForeColor = Color.White,

                    FlatStyle = FlatStyle.Flat,
                    UseVisualStyleBackColor = false,
                    Cursor = Cursors.Hand
                };

            btnSelectAllPopup.FlatAppearance.BorderSize = 0;

            btnSelectAllPopup.FlatAppearance.MouseOverBackColor =
                ColorTranslator.FromHtml("#12BDE8");

            btnSelectAllPopup.FlatAppearance.MouseDownBackColor =
                ColorTranslator.FromHtml("#0786A9");

            // ตรวจสถานะเริ่มต้น
            bool initiallyAllChecked =
                !_manualPopupQAChecked &&
                Enumerable
                    .Range(0, clb.Items.Count)
                    .All(i => clb.GetItemChecked(i));

            btnSelectAllPopup.Text =
                initiallyAllChecked
                    ? "ยกเลิกทั้งหมด"
                    : "เลือกทั้งหมด";

            btnSelectAllPopup.Click += (s, ev) =>
            {
                if (chkQAPopup.Checked)
                {
                    MessageBox.Show(
                        "ไม่สามารถเลือกรายการอื่นร่วมกับโหมด QA ได้ครับ\nหากต้องการทดสอบ QA ต้องเลือกโหมด QA อย่างเดียวเท่านั้น",
                        "แจ้งเตือน",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                bool allChecked =
                    Enumerable
                        .Range(0, clb.Items.Count)
                        .All(i => clb.GetItemChecked(i));

                bool newState = !allChecked;

                for (int i = 0; i < clb.Items.Count; i++)
                {
                    clb.SetItemChecked(i, newState);
                }

                btnSelectAllPopup.Text =
                    newState
                        ? "ยกเลิกทั้งหมด"
                        : "เลือกทั้งหมด";
            };

            _clearTestPopupSelections = () =>
            {
                if (clb.IsDisposed ||
                    btnSelectAllPopup.IsDisposed)
                {
                    return;
                }

                for (int i = 0; i < clb.Items.Count; i++)
                {
                    clb.SetItemChecked(i, false);
                }

                btnSelectAllPopup.Text = "เลือกทั้งหมด";
            };

            // อัปเดตข้อความปุ่ม เมื่อผู้ใช้ติ๊กเอง
            clb.ItemCheck += (s, ev) =>
            {
                if (chkQAPopup.Checked && ev.NewValue == CheckState.Checked)
                {
                    ev.NewValue = CheckState.Unchecked;
                    MessageBox.Show(
                        "ไม่สามารถเลือกรายการอื่นร่วมกับโหมด QA ได้ครับ\nหากต้องการทดสอบ QA ต้องเลือกโหมด QA อย่างเดียวเท่านั้น",
                        "แจ้งเตือน",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                BeginInvoke(new Action(() =>
                {
                    if (clb.IsDisposed)
                        return;

                    bool allChecked =
                        Enumerable
                            .Range(0, clb.Items.Count)
                            .All(i => clb.GetItemChecked(i));

                    btnSelectAllPopup.Text =
                        allChecked
                            ? "ยกเลิกทั้งหมด"
                            : "เลือกทั้งหมด";
                }));
            };

            // ─────────────────────────────────────
            // Click ปุ่ม TEST
            // ─────────────────────────────────────

            btnTestPopup.Click += (s, ev) =>
            {
                bool isQa = chkQAPopup.Checked;
                bool hasSelectedItem =
                    Enumerable
                        .Range(0, clb.Items.Count)
                        .Any(i => clb.GetItemChecked(i));

                // ⚠️ ถ้าผู้ใช้งานเผลอกด QA ร่วมกับตัวอื่น ให้แจ้งเตือนว่าไม่ได้
                if (isQa && hasSelectedItem)
                {
                    MessageBox.Show(
                        "ไม่สามารถเลือกรายการอื่นร่วมกับโหมด QA ได้ครับ\nหากต้องการทดสอบ QA ต้องเลือกโหมด QA อย่างเดียวเท่านั้น",
                        "แจ้งเตือน",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                if (isQa)
                {
                    // สำหรับ QA ทดสอบเฉพาะ NTC 2K เพียงชนิดเดียว 9 จุด
                    // ไม่ตั้งค่าใน _selectedTestTypes เป็น true เพื่อไม่ให้หน้าต่างเปิดมาแล้วขึ้นติ๊ก
                    _selectedTestTypes["ptc2k"] = false;
                    _selectedTestTypes["ntc10ka"] = false;
                    _selectedTestTypes["ntc2k"] = false;
                    _selectedTestTypes["ntc10kb"] = false;
                }
                else
                {
                    if (!hasSelectedItem)
                    {
                        MessageBox.Show(
                            "กรุณาเลือกอย่างน้อย 1 รายการที่จะทดสอบ",
                            "แจ้งเตือน",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }

                    _selectedTestTypes["ptc2k"] =
                        clb.GetItemChecked(0);

                    _selectedTestTypes["ntc10ka"] =
                        clb.GetItemChecked(1);

                    _selectedTestTypes["ntc2k"] =
                        clb.GetItemChecked(2);

                    _selectedTestTypes["ntc10kb"] =
                        clb.GetItemChecked(3);
                }

                _manualPopupQAChecked = isQa;

                // ไม่ปิด Popup
                btnStartTest_Click(
                    sourceButton,
                    EventArgs.Empty);
            };

            // Panel ครอบปุ่ม TEST และเลือกทั้งหมด
            var pnlTestRow =
                new System.Windows.Forms.Panel
                {
                    Width = popupWidth,
                    Height = 42,
                    BackColor = panelColor,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty
                };

            pnlTestRow.Controls.Add(btnTestPopup);
            pnlTestRow.Controls.Add(btnSelectAllPopup);



            // ─────────────────────────────────────
            // ปุ่ม Stop
            // ─────────────────────────────────────

            var btnStopPopup =
                new System.Windows.Forms.Button
                {
                    Text = "■  Stop",
                    Location = new Point(4, 3),
                    Size = new Size(322, 36),

                    Font = new Font(
                        "Segoe UI",
                        9.5F,
                        FontStyle.Bold),

                    BackColor = stopButtonColor,
                    ForeColor = Color.White,

                    FlatStyle = FlatStyle.Flat,
                    UseVisualStyleBackColor = false,
                    Cursor = Cursors.Hand
                };

            btnStopPopup.FlatAppearance.BorderSize = 0;

            btnStopPopup.FlatAppearance.MouseOverBackColor =
                ColorTranslator.FromHtml("#EF233C");

            btnStopPopup.FlatAppearance.MouseDownBackColor =
                ColorTranslator.FromHtml("#A9000E");

            btnStopPopup.Click += (s, ev) =>
            {
                btn_StopTest_Click(
                    btnStopPopup,
                    EventArgs.Empty);
            };

            var pnlStopRow =
                new System.Windows.Forms.Panel
                {
                    Width = popupWidth,
                    Height = 42,
                    BackColor = panelColor,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty
                };

            pnlStopRow.Controls.Add(btnStopPopup);

            // ─────────────────────────────────────
            // ─────────────────────────────────────

            // เพิ่ม Controls ลงใน popup
            // ─────────────────────────────────────

            ArrangeManualTest(dropDown, lblHeader, chkQAPopup, clb, btnTestPopup, btnSelectAllPopup, btnStopPopup);


            // เมื่อตัว popup ปิด
            dropDown.FormClosed += (s, ev) =>
            {
                if (chkQAPopup.Checked)
                {
                    _selectedTestTypes["ptc2k"] = false;
                    _selectedTestTypes["ntc10ka"] = false;
                    _selectedTestTypes["ntc2k"] = false;
                    _selectedTestTypes["ntc10kb"] = false;
                }
                else
                {
                    _selectedTestTypes["ptc2k"] = clb.GetItemChecked(0);
                    _selectedTestTypes["ntc10ka"] = clb.GetItemChecked(1);
                    _selectedTestTypes["ntc2k"] = clb.GetItemChecked(2);
                    _selectedTestTypes["ntc10kb"] = clb.GetItemChecked(3);
                }
                _manualPopupQAChecked = chkQAPopup.Checked;
                _isDraggingPopup = false;
                lblHeader.Capture = false;
            };

            // แสดง popup ใต้ปุ่ม
            dropDown.Show(this);
        }


        int confirmedTypeIndex = -1;
        bool isChangingType = false;
        #endregion

        #region 22 Popup Manual Input

        private void ShowManualInputPopup(System.Windows.Forms.Button sourceButton)
        {
            // ถ้ามี Manual Input popup เปิดอยู่ ให้ปิดก่อน
            if (_manualInputDropDown != null &&
                !_manualInputDropDown.IsDisposed)
            {
                _manualInputDropDown.Close();
            }

            const int popupWidth = 330;
            const int popupHeight = 244;

            Color panelColor =
                ColorTranslator.FromHtml("#142837");

            Color accentColor =
                ColorTranslator.FromHtml("#49DAF2");

            Color inputColor =
                ColorTranslator.FromHtml("#F5F6F7");



            Color sendButtonColor =
                ColorTranslator.FromHtml("#FFD900");

            // ─────────────────────────────────────
            // สร้าง ToolStripDropDown
            // ─────────────────────────────────────

            var dropDown = CreateManualWindow();

            _manualInputDropDown = dropDown;

            // ─────────────────────────────────────
            // Header (ลากย้าย popup ได้)
            // ─────────────────────────────────────

            var lblManualHeader =
                new System.Windows.Forms.Label
                {
                    Text = "Manual Input",
                    Width = popupWidth,
                    Height = 34,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoEllipsis = false,

                    Font = new Font(
                        "Segoe UI",
                        10F,
                        FontStyle.Bold),

                    BackColor = panelColor,
                    ForeColor = accentColor,
                    Cursor = Cursors.SizeAll
                };

            lblManualHeader.MouseDown += (s, ev) =>
            {
                if (ev.Button != MouseButtons.Left)
                    return;

                _isDraggingPopup = true;

                _popupDragOffset =
                    dropDown.PointToClient(
                        System.Windows.Forms.Control.MousePosition);

                lblManualHeader.Capture = true;
            };

            lblManualHeader.MouseMove += (s, ev) =>
            {
                if (!_isDraggingPopup ||
                    !dropDown.Visible ||
                    dropDown.IsDisposed)
                {
                    return;
                }

                Point mousePosition =
                    System.Windows.Forms.Control.MousePosition;

                Rectangle workingArea =
                    Screen.FromPoint(mousePosition).WorkingArea;

                int newX = mousePosition.X - _popupDragOffset.X;
                int newY = mousePosition.Y - _popupDragOffset.Y;

                newX = Math.Max(
                    workingArea.Left,
                    Math.Min(newX, workingArea.Right - dropDown.Width));

                newY = Math.Max(
                    workingArea.Top,
                    Math.Min(newY, workingArea.Bottom - dropDown.Height));

                dropDown.Location = new Point(newX, newY);
            };

            lblManualHeader.MouseUp += (s, ev) =>
            {
                if (ev.Button != MouseButtons.Left)
                    return;

                _isDraggingPopup = false;
                lblManualHeader.Capture = false;
            };



            // ─────────────────────────────────────
            // Panel หลัก
            // ─────────────────────────────────────

            var mainPanel = new MyRoundedPanel
            {
                BorderRadius = 12,
                Width = popupWidth,
                Height = popupHeight,
                BackColor = panelColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            // ─────────────────────────────────────
            // ComboBox เลือก Input Type
            // ─────────────────────────────────────

            var cboInputType = new System.Windows.Forms.ComboBox
            {
                Name = "cboManualInputType",
                Location = new Point(15, 15),
                Size = new Size(145, 32),
                DropDownWidth = 180,
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = inputColor,
                ForeColor = Color.Black
            };

            cboInputType.Items.AddRange(new object[]
            {
                "PTC 2K (00)",
                "NTC 10K (01)",
                "NTC 2K (02)",
                "NTC 10K (03)"
            });

            cboInputType.SelectedIndex = -1;
            cboInputType.Text = "";

            // ─────────────────────────────────────
            // Label กรุณาเลือก Type
            // ─────────────────────────────────────

            var lblSelectType = new System.Windows.Forms.Label
            {
                Text = "กรุณาเลือก Type",
                Location = new Point(166, 15),
                Size = new Size(150, 32),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = accentColor,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // ─────────────────────────────────────
            // Label อุณหภูมิ
            // ─────────────────────────────────────

            var lblTemperature = new System.Windows.Forms.Label
            {
                Text = "อุณหภูมิ:",
                Location = new Point(15, 70),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };

            // ─────────────────────────────────────
            // TextBox อุณหภูมิ
            // ─────────────────────────────────────

            var txtTemperature = new System.Windows.Forms.TextBox
            {
                Name = "txtManualTemperature",
                Location = new Point(125, 70),
                Size = new Size(190, 30),
                Font = new Font("Segoe UI", 10F),
                BackColor = inputColor,
                ForeColor = Color.Black,
                TextAlign = HorizontalAlignment.Left
            };

            // ─────────────────────────────────────
            // Label Resistance
            // ─────────────────────────────────────

            var lblResistance = new System.Windows.Forms.Label
            {
                Text = "Resistance:",
                Location = new Point(15, 115),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };

            // ─────────────────────────────────────
            // TextBox Resistance
            // ─────────────────────────────────────

            var txtResistance = new System.Windows.Forms.TextBox
            {
                Name = "txtManualResistance",
                Location = new Point(125, 115),
                Size = new Size(190, 30),
                Font = new Font("Segoe UI", 10F),
                BackColor = inputColor,
                ForeColor = Color.Black,
                TextAlign = HorizontalAlignment.Left
            };

            // ─────────────────────────────────────
            // ข้อความบอกสถานะ
            // ─────────────────────────────────────

            var lblManualStatus =
                new System.Windows.Forms.Label
                {
                    Text = "",
                    Location = new Point(15, 150),
                    Size = new Size(300, 30),
                    Font = new Font("Segoe UI", 8F),
                    ForeColor = Color.Orange,
                    BackColor = Color.Transparent,
                    TextAlign =
                        ContentAlignment.MiddleCenter
                };

            // ─────────────────────────────────────
            // ปุ่ม Send
            // ─────────────────────────────────────

            var btnSend =
                new System.Windows.Forms.Button
                {
                    Name = "btnManualSend",
                    Text = "▶ Send to Ohm Source",
                    Location = new Point(15, 188),
                    Size = new Size(300, 42),
                    Font = new Font(
                        "Segoe UI",
                        10F,
                        FontStyle.Bold),
                    BackColor = sendButtonColor,
                    ForeColor = Color.Black,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };

            btnSend.FlatAppearance.BorderSize = 0;
            txtTemperature.Enabled = false;
            txtResistance.Enabled = false;
            btnSend.Enabled = false;

            // ─────────────────────────────────────
            // ─────────────────────────────────────

            txtTemperature.TextChanged += (s, e) =>
            {
                if (_isUpdatingManualValues)
                    return;

                string text =
                    txtTemperature.Text.Trim();

                if (string.IsNullOrEmpty(text) ||
                    text == "-" ||
                    text == ".")
                {
                    return;
                }

                if (!TryParseManualNumber(
                        text,
                        out double temperature))
                {
                    return;
                }

                ManualRtPoint[] table =
                    GetManualRtTable(
                        cboInputType.SelectedIndex);

                if (!TryCalculateResistance(
                        table,
                        temperature,
                        out double resistance))
                {
                    lblManualStatus.Text =
                        $"Temp ต้องอยู่ระหว่าง " +
                        $"{table.Min(p => p.Temperature):0.#} ถึง " +
                        $"{table.Max(p => p.Temperature):0.#} °C";

                    lblManualStatus.ForeColor =
                        Color.Orange;

                    return;
                }

                try
                {
                    _isUpdatingManualValues = true;

                    txtResistance.Text =
                        resistance.ToString(
                            "0.###",
                            System.Globalization
                                .CultureInfo.InvariantCulture);

                    lblManualStatus.Text =
                        "คำนวณแล้ว — กด Send เพื่อส่งค่า";

                    lblManualStatus.ForeColor =
                        accentColor;
                }
                finally
                {
                    _isUpdatingManualValues = false;
                }
            };

            // ─────────────────────────────────────
            // Resistance ถูกแก้ → คำนวณ Temperature
            // ─────────────────────────────────────

            txtResistance.TextChanged += (s, e) =>
            {
                if (_isUpdatingManualValues)
                    return;

                string text =
                    txtResistance.Text.Trim();

                if (string.IsNullOrEmpty(text) ||
                    text == ".")
                {
                    return;
                }

                if (!TryParseManualNumber(
                        text,
                        out double resistance))
                {
                    return;
                }

                ManualRtPoint[] table =
                    GetManualRtTable(
                        cboInputType.SelectedIndex);

                if (!TryCalculateTemperature(
                        table,
                        resistance,
                        out double temperature))
                {
                    lblManualStatus.Text =
                        $"R ต้องอยู่ระหว่าง " +
                        $"{table.Min(p => p.Resistance):0} ถึง " +
                        $"{table.Max(p => p.Resistance):0} Ω";

                    lblManualStatus.ForeColor =
                        Color.Orange;

                    return;
                }

                try
                {
                    _isUpdatingManualValues = true;

                    txtTemperature.Text =
                        temperature.ToString(
                            "0.0",
                            System.Globalization
                                .CultureInfo.InvariantCulture);

                    lblManualStatus.Text =
                        "คำนวณแล้ว — กด Send เพื่อส่งค่า";

                    lblManualStatus.ForeColor =
                        accentColor;
                }
                finally
                {
                    _isUpdatingManualValues = false;
                }
            };

            // ─────────────────────────────────────
            // เปลี่ยน Type → ล้างค่าทั้งสองช่อง
            // ─────────────────────────────────────

            cboInputType.SelectedIndexChanged += async (s, e) =>
            {
                if (isChangingType)
                {
                    return;
                }

                int selectedType = cboInputType.SelectedIndex;

                if (selectedType < 0)
                {
                    txtTemperature.Enabled = false;
                    txtResistance.Enabled = false;
                    btnSend.Enabled = false;
                    return;
                }

                if (_isCalibrating || _modelTestAllActive || _buttonTestActive)
                {
                    MessageBox.Show(
                        "ไม่สามารถเปลี่ยน Input Type ระหว่างที่ระบบกำลังทำงาน",
                        "Manual Input",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    isChangingType = true;
                    cboInputType.SelectedIndex = confirmedTypeIndex;
                    isChangingType = false;
                    return;
                }

                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    MessageBox.Show(
                        "กรุณาเชื่อมต่อ DEF-03N ก่อนเลือก Input Type",
                        "Manual Input",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    isChangingType = true;
                    cboInputType.SelectedIndex = confirmedTypeIndex;
                    isChangingType = false;
                    return;
                }

                try
                {
                    isChangingType = true;

                    cboInputType.Enabled = false;
                    txtTemperature.Enabled = false;
                    txtResistance.Enabled = false;
                    btnSend.Enabled = false;

                    txtTemperature.Clear();
                    txtResistance.Clear();

                    lblManualStatus.Text =
                        $"กำลังส่ง Type {selectedType:00} ไปยังบอร์ด...";

                    lblManualStatus.ForeColor = Color.Orange;

                    bool writeSuccess =
                        await ExecuteWriteSingleRegisterVerifiedAsync(
                            slaveId: 1,
                            registerAddress: 2,
                            value: (short)selectedType,
                            maxAttempts: 5,
                            timeoutMs: 1000);

                    if (writeSuccess)
                    {
                        confirmedTypeIndex = selectedType;

                        txtTemperature.Enabled = true;
                        txtResistance.Enabled = true;
                        btnSend.Enabled = true;

                        lblManualStatus.Text =
                            $"เลือก Type {selectedType:00} สำเร็จ";

                        lblManualStatus.ForeColor = Color.LimeGreen;

                        txtTemperature.Focus();
                    }
                    else
                    {
                        cboInputType.SelectedIndex = confirmedTypeIndex;

                        txtTemperature.Enabled =
                            confirmedTypeIndex >= 0;

                        txtResistance.Enabled =
                            confirmedTypeIndex >= 0;

                        btnSend.Enabled =
                            confirmedTypeIndex >= 0;

                        lblManualStatus.Text =
                            "บอร์ดไม่ตอบรับการเปลี่ยน Type";

                        lblManualStatus.ForeColor = Color.Red;
                    }
                }
                finally
                {
                    cboInputType.Enabled = true;
                    isChangingType = false;
                }
            };

            // ─────────────────────────────────────
            // ปุ่ม Send เท่านั้นที่สั่ง Ohm Source
            // ─────────────────────────────────────
            btnSend.Click += (s, e) =>
            {
                string text = txtResistance.Text
                    .Replace(",", "")
                    .Trim();

                if (!double.TryParse(text, out double resistance))
                {
                    MessageBox.Show(
                        "กรุณากรอกค่า Resistance ให้ถูกต้อง",
                        "Manual Input",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    txtResistance.Focus();
                    return;
                }

                ManualRtPoint[] table =
                    GetManualRtTable(cboInputType.SelectedIndex);

                double minResistance =
                    table.Min(p => p.Resistance);

                double maxResistance =
                    table.Max(p => p.Resistance);

                if (resistance < minResistance ||
                    resistance > maxResistance)
                {
                    MessageBox.Show(
                        $"ค่า Resistance ต้องอยู่ระหว่าง " +
                        $"{minResistance:0}–{maxResistance:0} Ω",
                        "ค่าความต้านทานเกินช่วง",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                SendOhm(resistance);

                lblManualStatus.Text =
                    $"ส่ง {resistance:0.###} Ω แล้ว";

                lblManualStatus.ForeColor =
                    Color.LimeGreen;
            };

            mainPanel.Controls.Add(cboInputType);
            mainPanel.Controls.Add(lblSelectType);

            mainPanel.Controls.Add(lblTemperature);
            mainPanel.Controls.Add(txtTemperature);

            mainPanel.Controls.Add(lblResistance);
            mainPanel.Controls.Add(txtResistance);

            mainPanel.Controls.Add(btnSend);
            mainPanel.Controls.Add(lblManualStatus);

            // ─────────────────────────────────────
            // นำ Panel ใส่ ToolStripDropDown
            // ─────────────────────────────────────



            ArrangeManualInput(dropDown, lblManualHeader, cboInputType, lblSelectType, lblTemperature, txtTemperature, lblResistance, txtResistance, lblManualStatus, btnSend);

            // เมื่อตัว popup ปิด ให้ล้าง reference เท่านั้น
            // ห้าม Dispose ตรงนี้ เพราะอาจเกิด ObjectDisposedException
            dropDown.FormClosed += (s, e) =>
            {
                _isDraggingPopup = false;
                lblManualHeader.Capture = false;

                if (_manualInputDropDown == dropDown)
                {
                    _manualInputDropDown = null;
                }
            };

            // ─────────────────────────────────────
            // แสดง popup ใต้ปุ่ม
            // ─────────────────────────────────────

            dropDown.Show(this);

            cboInputType.Focus();
        }
        private void btnManualInput_Click(object sender, EventArgs e)
        {
            if (!CheckManualConnections(true, true, false)) return;
            if (sender is System.Windows.Forms.Button button)
            {
                ShowManualInputPopup(button);
            }
        }


        #endregion

        #region 23 ตารางเซนเซอร์และคำนวณ Temp กับ Resistance

        // Type 00: PTC 2K
        private readonly ManualRtPoint[] _ptc2KTable = {
            new ManualRtPoint(-40, 1120),
            new ManualRtPoint(-30, 1236),
            new ManualRtPoint(-20, 1361),
            new ManualRtPoint(-10, 1495),
            new ManualRtPoint(  0, 1638),
            new ManualRtPoint( 10, 1785),
            new ManualRtPoint( 20, 1939),
            new ManualRtPoint( 30, 2101),
            new ManualRtPoint( 40, 2283),
            new ManualRtPoint( 50, 2462),
            new ManualRtPoint( 60, 2652),
            new ManualRtPoint( 70, 2848),
            new ManualRtPoint( 80, 3054),
            new ManualRtPoint( 90, 3270),
            new ManualRtPoint(100, 3479),
            new ManualRtPoint(110, 3688),
            new ManualRtPoint(120, 3910),
            new ManualRtPoint(130, 4135)
        };

        // Type 01: NTC 10K B57621C103J62
        private readonly ManualRtPoint[] _ntc10K01Table ={
            new ManualRtPoint(-40, 233700),
            new ManualRtPoint(-30, 133700),
            new ManualRtPoint(-20,  76200),
            new ManualRtPoint(-10,  47120),
            new ManualRtPoint(  0,  30780),
            new ManualRtPoint( 10,  19470),
            new ManualRtPoint( 20,  13440),
            new ManualRtPoint( 30,   8774),
            new ManualRtPoint( 40,   6240),
            new ManualRtPoint( 50,   4398),
            new ManualRtPoint( 60,   3175),
            new ManualRtPoint( 70,   2354),
            new ManualRtPoint( 80,   1762),
            new ManualRtPoint( 90,   1326),
            new ManualRtPoint(100,   1016),
            new ManualRtPoint(110,    787),
            new ManualRtPoint(120,    622)
        };

        // Type 02: NTC 2K
        private readonly ManualRtPoint[] _ntc2KTable ={
            new ManualRtPoint(-40, 35950),
            new ManualRtPoint(-30, 21480),
            new ManualRtPoint(-20, 13212),
            new ManualRtPoint(-10,  8336),
            new ManualRtPoint(  0,  5433),
            new ManualRtPoint( 10,  3670),
            new ManualRtPoint( 20,  2505),
            new ManualRtPoint( 30,  1715),
            new ManualRtPoint( 40,  1165),
            new ManualRtPoint( 50,   826),
            new ManualRtPoint( 60,   593),
            new ManualRtPoint( 70,   433),
            new ManualRtPoint( 80,   320),
            new ManualRtPoint( 90,   238),
            new ManualRtPoint(100,   181),
            new ManualRtPoint(110,   140),
            new ManualRtPoint(120,   111),
            new ManualRtPoint(130,    87)
        };

        // Type 03: NTC 10K
        private readonly ManualRtPoint[] _ntc10K03Table ={
            new ManualRtPoint(-40, 279158),
            new ManualRtPoint(-30, 151921),
            new ManualRtPoint(-20,  86749),
            new ManualRtPoint(-10,  51342),
            new ManualRtPoint(  0,  31265),
            new ManualRtPoint( 10,  19503),
            new ManualRtPoint( 20,  12431),
            new ManualRtPoint( 30,   8083),
            new ManualRtPoint( 40,   5339),
            new ManualRtPoint( 50,   3588),
            new ManualRtPoint( 60,   2457),
            new ManualRtPoint( 70,   1715),
            new ManualRtPoint( 80,   1220),
            new ManualRtPoint( 90,    883),
            new ManualRtPoint(100,    649),
            new ManualRtPoint(110,    484),
            new ManualRtPoint(120,    367)
        };

        //เมธอดเลือกตาราง
        private ManualRtPoint[] GetManualRtTable(int selectedIndex)
        {
            switch (selectedIndex)
            {
                case 0:
                    return _ptc2KTable;

                case 1:
                    return _ntc10K01Table;

                case 2:
                    return _ntc2KTable;

                case 3:
                    return _ntc10K03Table;

                default:
                    return _ptc2KTable;
            }
        }

        //เมธอดแปลงข้อความเป็นตัวเลข
        private bool TryParseManualNumber(string text, out double value)
        {
            string cleanText = text
                .Replace(",", "")
                .Trim();

            return double.TryParse(
                cleanText,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);
        }

        //เมธอด Temperature → Resistance
        private bool TryCalculateResistance(ManualRtPoint[] table, double temperature, out double resistance)
        {
            resistance = 0;

            if (table == null || table.Length < 2)
                return false;

            double minimumTemperature =
                table.Min(p => p.Temperature);

            double maximumTemperature =
                table.Max(p => p.Temperature);

            if (temperature < minimumTemperature ||
                temperature > maximumTemperature)
            {
                return false;
            }

            // ตรวจค่าที่ตรงกับตารางพอดี
            foreach (ManualRtPoint point in table)
            {
                if (Math.Abs(
                        point.Temperature - temperature) < 0.000001)
                {
                    resistance = point.Resistance;
                    return true;
                }
            }

            // Linear interpolation
            for (int i = 0; i < table.Length - 1; i++)
            {
                ManualRtPoint point1 = table[i];
                ManualRtPoint point2 = table[i + 1];

                if (temperature >= point1.Temperature &&
                    temperature <= point2.Temperature)
                {
                    double ratio =
                        (temperature - point1.Temperature) /
                        (point2.Temperature - point1.Temperature);

                    resistance =
                        point1.Resistance +
                        ratio *
                        (point2.Resistance - point1.Resistance);

                    return true;
                }
            }

            return false;
        }

        //เมธอด Resistance → Temperature
        private bool TryCalculateTemperature(ManualRtPoint[] table, double resistance, out double temperature)
        {
            temperature = 0;

            if (table == null || table.Length < 2)
                return false;

            double minimumResistance =
                table.Min(p => p.Resistance);

            double maximumResistance =
                table.Max(p => p.Resistance);

            if (resistance < minimumResistance ||
                resistance > maximumResistance)
            {
                return false;
            }

            // ตรวจค่าที่ตรงกับตารางพอดี
            foreach (ManualRtPoint point in table)
            {
                if (Math.Abs(
                        point.Resistance - resistance) < 0.000001)
                {
                    temperature = point.Temperature;
                    return true;
                }
            }

            // Linear interpolation
            for (int i = 0; i < table.Length - 1; i++)
            {
                ManualRtPoint point1 = table[i];
                ManualRtPoint point2 = table[i + 1];

                double segmentMinimum = Math.Min(
                    point1.Resistance,
                    point2.Resistance);

                double segmentMaximum = Math.Max(
                    point1.Resistance,
                    point2.Resistance);

                if (resistance >= segmentMinimum &&
                    resistance <= segmentMaximum)
                {
                    double resistanceDifference =
                        point2.Resistance - point1.Resistance;

                    if (Math.Abs(resistanceDifference) < 0.000001)
                        return false;

                    double ratio =
                        (resistance - point1.Resistance) /
                        resistanceDifference;

                    temperature =
                        point1.Temperature +
                        ratio *
                        (point2.Temperature -
                         point1.Temperature);

                    return true;
                }
            }

            return false;
        }




        #endregion

        #region 24 ทดสอบ ADC Debug และปุ่มเครื่องมือเพิ่มเติม


        private void chkDebugWrite_CheckedChanged(object sender, EventArgs e)
        {
            _writeDebugEnabled = chkDebugWrite.Checked;


            if (_writeDebugEnabled)
            {
                LogToRx(
                    "[DEBUG WRITE] เปิดการแสดง Log การเขียน Register",
                    Color.DarkOrange);
            }
            else
            {
                LogToRx(
                    "[DEBUG WRITE] ปิดการแสดง Log การเขียน Register",
                    Color.Gray);
            }
        }

        private void LogWriteDebug(string message, Color color)
        {
            if (!_writeDebugEnabled)
            {
                return;
            }

            LogToRx(message, color);
        }

        private async void btnCheckRelay_Click(object sender, EventArgs e)
        {
            if (!CheckManualConnections(false, false, true)) return;
            if (_modelTestAllActive || _buttonTestActive) return;
            var btn = sender as System.Windows.Forms.Button;
            try
            {
                if (btn != null) btn.Enabled = false;
                await CheckRelayOutputAsync(); // ID 3, Register 2
                await CheckAlarmAsync();       // ID 3, Register 7
            }
            finally
            {
                if (btn != null) btn.Enabled = true;
            }
        }

        private void txt_Rev_Leave(object sender, EventArgs e)
        {
            SaveConfig();
        }

        private void txt_instrument_Leave(object sender, EventArgs e)
        {
            SaveConfig();
        }

        private void txt_firmware_Leave(object? sender, EventArgs e)
        {
            SaveConfig();
        }

        #endregion

        #region 25 ข้อความแนะนำการเลือก Model และ Supply

        private static void ConfigureSelectionHint(ComboBox combo, string hint)
        {
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.DrawItem += (_, e) =>
            {
                e.DrawBackground();
                string text = e.Index < 0 ? hint : combo.Items[e.Index]?.ToString() ?? "";
                Color color = e.Index < 0 ? Color.Gray : e.ForeColor;
                TextRenderer.DrawText(e.Graphics, text, combo.Font, e.Bounds, color,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                e.DrawFocusRectangle();
            };
        }
        #endregion

        #region 26 ลำดับ QC ตาม Model และการรวมผล

        private const string ModelA = ModelTestResults.ModelA;
        private const string ModelAB = ModelTestResults.ModelAB;
        private bool _modelTestAllActive;
        private CancellationTokenSource? _modelContactsCts;
        private readonly SemaphoreSlim _relayReadGate = new SemaphoreSlim(1, 1);
        private byte _pendingRelaySlaveId;
        private readonly List<ModelReportTarget> _modelReportTargets = new List<ModelReportTarget>();

        private sealed class ModelReportTarget
        {
            public string Path = "";
            public int Row;
        }

        private void InitializeModelSelection()
        {
            string previous = txt_model.Text;
            txt_model.Items.Clear();
            txt_model.Items.AddRange(new object[] { ModelA, ModelAB });
            txt_model.DropDownStyle = ComboBoxStyle.DropDownList;
            txt_model.SelectedIndex = previous == ModelA ? 0 : previous == ModelAB ? 1 : -1;
            _lastConfirmedModel = txt_model.SelectedIndex >= 0 ? txt_model.Text.Trim() : string.Empty;
            ConfigureSelectionHint(txt_model, "กรุณาเลือก โมเดล");
            txt_supply.Items.Clear();
            txt_supply.Items.AddRange(new object[] { "220 Volt", "24 Volt" });
            txt_supply.DropDownStyle = ComboBoxStyle.DropDownList;
            txt_supply.SelectedIndex = -1;
            ConfigureSelectionHint(txt_supply, "กรุณาเลือก supply");
        }

        private async void txt_model_SelectionChangeCommitted(object? sender, EventArgs e)
        {
            // ป้องกันการทำงานซ้ำ
            if (_isChangingModelRegister)
                return;

            string selectedModel = txt_model.Text.Trim();

            // ถ้าที่หน้าจอเลือก Type เดิมซ้ำอยู่แล้ว ไม่ต้องทำอะไรเลย (ข้ามทันที)
            if (!string.IsNullOrEmpty(_lastConfirmedModel) &&
                string.Equals(selectedModel, _lastConfirmedModel, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string previousModel = !string.IsNullOrEmpty(_lastConfirmedModel)
                ? _lastConfirmedModel
                : (selectedModel == ModelA ? ModelAB : ModelA);

            short requestedValue;

            if (selectedModel == ModelA)
            {
                requestedValue = 0;
            }
            else if (selectedModel == ModelAB)
            {
                requestedValue = 1;
            }
            else
            {
                return;
            }

            // ต้องเชื่อมต่อ DEF-03N ก่อน
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                MessageBox.Show(
                    "เลือก Model แล้ว แต่ยังไม่ได้เชื่อมต่อ DEF-03N\n" +
                    "จึงไม่สามารถเขียน Register 23 ได้",
                    "Model Option",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string friendlyModelName = selectedModel == ModelA ? "DEF-03N Type A" :
                                       selectedModel == ModelAB ? "DEF-03N Type A-B" : selectedModel;
            string shortTypeName = selectedModel == ModelA ? "Type A" :
                                   selectedModel == ModelAB ? "Type A-B" : selectedModel;

            List<KeyValuePair<Control, bool>>? lockedControls = null;

            try
            {
                _isChangingModelRegister = true;
                lockedControls = LockAllInteractiveControls();
                UseWaitCursor = true;

                LogToRx($"[MODEL] กำลังเปลี่ยน Model เป็น {friendlyModelName}...", Color.Blue);

                // ขั้นที่ 1: เขียนและตรวจ Write Echo
                bool writeSuccess =
                    await ExecuteWriteSingleRegisterVerifiedAsync(
                        slaveId: 1,
                        registerAddress: ModelOptionRegister,
                        value: requestedValue,
                        maxAttempts: 5,
                        timeoutMs: 1000);

                if (!writeSuccess)
                {
                    RevertModelSelection(previousModel);
                    LogToRx($"[MODEL ERROR] เปลี่ยน Model เป็น {friendlyModelName} ไม่สำเร็จ", Color.Red);
                    MessageBox.Show(
                        $"เปลี่ยนเป็น {friendlyModelName} ไม่สำเร็จ\nกรุณาตรวจสอบการเชื่อมต่อกับอุปกรณ์",
                        "Model Option",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                // ขั้นที่ 2: อ่านค่าจริงกลับมายืนยัน
                short actualValue =
                    await ReadSingleRegisterAsync(
                        ModelOptionRegister,
                        timeoutMs: 1000,
                        retries: 2,
                        slaveId: 1);

                if (actualValue != requestedValue)
                {
                    LogToRx(
                        $"[MODEL ERROR] ตรวจสอบค่า Model ไม่ถูกต้อง (อ่านได้ {actualValue})",
                        Color.Red);

                    RevertModelSelection(previousModel);
                    MessageBox.Show(
                        $"เปลี่ยนเป็น {friendlyModelName} ไม่สมบูรณ์\nกรุณาลองใหม่อีกครั้ง",
                        "Model Option",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                _lastConfirmedModel = selectedModel;
                LogToRx($"[MODEL] เปลี่ยนเป็น {shortTypeName} เรียบร้อย", Color.Green);
                LogWriteDebug($"[MODEL DEBUG] Register {ModelOptionRegister} = {actualValue}", Color.DarkGray);

                // รอ 3 วินาทีตามที่ผู้ใช้กำหนด พร้อมนับถอยหลัง
                for (int s = 3; s > 0; s--)
                {
                    LogToRx($"[MODEL] รอการเตรียมความพร้อมอุปกรณ์ {s} วินาที...", Color.DarkOrange);
                    await Task.Delay(1000);
                }

                LogToRx($"[MODEL] อุปกรณ์พร้อมทำงานแล้ว ({friendlyModelName})", Color.Green);
            }
            catch (Exception ex)
            {
                LogToRx(
                    $"[MODEL ERROR] เปลี่ยน Model เป็น {friendlyModelName} ไม่สำเร็จ: {ex.Message}",
                    Color.Red);

                RevertModelSelection(previousModel);
                MessageBox.Show(
                    $"เปลี่ยน Model ไม่สำเร็จ\n{ex.Message}",
                    "Model Option",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isChangingModelRegister = false;
                UseWaitCursor = false;
                if (lockedControls != null)
                {
                    RestoreInteractiveControls(lockedControls);
                }
            }
        }

        private void RevertModelSelection(string previousModel)
        {
            txt_model.SelectedIndex = previousModel == ModelA ? 0 : previousModel == ModelAB ? 1 : -1;
        }

        private List<KeyValuePair<Control, bool>> LockAllInteractiveControls()
        {
            var locked = new List<KeyValuePair<Control, bool>>();
            void Collect(Control parent)
            {
                foreach (Control child in parent.Controls)
                {
                    if (child is ButtonBase or ComboBox or TextBox or CheckBox or CustomTextBox or CustomComboBox)
                    {
                        locked.Add(new KeyValuePair<Control, bool>(child, child.Enabled));
                        child.Enabled = false;
                    }
                    if (child.HasChildren)
                    {
                        Collect(child);
                    }
                }
            }
            Collect(this);
            return locked;
        }

        private static void RestoreInteractiveControls(List<KeyValuePair<Control, bool>> lockedControls)
        {
            foreach (var item in lockedControls)
            {
                item.Key.Enabled = item.Value;
            }
        }

        private bool ValidateTestModel(out string model)
        {
            model = txt_model.Text.Trim();
            if (ModelTestResults.IsValidModel(model))
            {
                if (txt_supply.SelectedIndex >= 0) return true;
                MessageBox.Show("กรุณาเลือก supply ก่อนเริ่มทดสอบหรือสร้างรายงาน", "Supply", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txt_supply.Focus();
                return false;
            }
            MessageBox.Show("กรุณาเลือก Model: DEF-03N-A หรือ DEF-03N-A-B ก่อนเริ่มทดสอบหรือสร้างรายงาน",
                "Model", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        // Called inside the existing reservation/save transaction, before temperature tests.
        private void InitializeModelReportRow(IXLWorksheet sheet, string path, int row)
        {
            if (!_modelTestAllActive || _modelReportTargets.Any(t => t.Path == path && t.Row == row)) return;
            sheet.Range(row, 2, row, 21).Value = "-";
            sheet.Cell(row, "N").Value = "NOT TESTED";
            sheet.Cell(row, "O").Value = "NOT TESTED";
            sheet.Cell(row, "V").Value = "";
            sheet.Cell(row, "W").Value = "";

            IXLRange resultRange = sheet.Range(row, 2, row, 24);
            resultRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            resultRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            QcReportLayout.SetAutomatedResult(sheet, row, txt_model.Text.Trim(), null);
        }

        private void TrackModelReportRow(string path, int row)
        {
            if (_modelTestAllActive && !_modelReportTargets.Any(t => t.Path == path && t.Row == row))
                _modelReportTargets.Add(new ModelReportTarget { Path = path, Row = row });
        }

        private static string GetFirmwareRegisterName(ushort address)
        {
            switch (address)
            {
                case 0: return "PV";
                case 1: return "SV";
                case 2: return "Input";
                case 3: return "PV Adjust";
                case 4: return "PV Filter";
                case 5: return "Decimal Point";
                case 6: return "Scale C/F";
                case 7: return "Lock Key";
                case 8: return "Start Time";
                case 9: return "Function Relay";
                case 10: return "Mode Control";
                case 11: return "Position On 1";
                case 12: return "Position Off 1";
                case 13: return "Output Delay On";
                case 14: return "Output Delay Off";
                case 15: return "Function Alarm 1";
                case 16: return "Alarm Limit High 1";
                case 17: return "Alarm Limit Low 1";
                case 18: return "Hysteresis Alarm 1";
                case 19: return "Function Alarm 2";
                case 20: return "Alarm Limit High 2";
                case 21: return "Alarm Limit Low 2";
                case 22: return "Hysteresis Alarm 2";
                case 23: return "Option";
                case 24: return "Meter Command";
                case 25: return "Meter Status";
                case 26: return "ADC Value";
                case 27: return "Front Key Status";
                default: return $"Register {address}";
            }
        }

        private static string GetContactDisplayName(string name)
        {
            return name == "RELAY OUTPUT" ? "Relay Output" :
                   name == "ALARM" ? "Alarm Relay" : name;
        }

        private async Task<bool> WaitForTemperatureAsync(
            double expectedTemp,
            double tolerance = 1.0,
            int maxLoops = 20,
            CancellationToken cancellationToken = default)
        {
            LogToRx($"[RELAY SETUP] กำลังรออุณหภูมิเข้าใกล้ {expectedTemp:0.#}°C...", Color.Blue);

            short rawPv = 0;
            int stableCount = 0;
            const int requiredStableReads = 2;
            double actualPv = 0;

            for (int loop = 0; loop < maxLoops; loop++)
            {
                if (_userRequestedStop || cancellationToken.IsCancellationRequested)
                    return false;

                try
                {
                    rawPv = await ReadSingleRegisterAsync(0, timeoutMs: 1000, retries: 1, slaveId: TargetID, cancellationToken: cancellationToken);
                }
                catch { }

                double realTimePv = (_lastKnownDp == 0) ? rawPv : (rawPv / 10.0);
                _currentPv = realTimePv;
                actualPv = realTimePv;

                Control[] foundLabel0 = Controls.Find("Regis0", true);
                if (foundLabel0.Length > 0 && foundLabel0[0] is Label lbl0)
                {
                    lbl0.Text = realTimePv.ToString("F0") + "°C";
                }

                Control[] foundStatus = Controls.Find("lbl_testStatus", true);
                if (foundStatus.Length > 0)
                {
                    foundStatus[0].Text = $"🎯 เตรียมทดสอบ Relay ({expectedTemp:0.#}°C) | PV ปัจจุบัน = {realTimePv:F0}°C";
                    foundStatus[0].ForeColor = Color.DarkOrange;
                }

                if (Math.Abs(realTimePv - expectedTemp) <= tolerance)
                {
                    stableCount++;
                    if (stableCount >= requiredStableReads)
                    {
                        LogToRx($"⚡ [RELAY SETUP] อุณหภูมินิ่งเข้าเป้าแล้วที่ {realTimePv:0.#}°C", Color.Green);
                        LogWriteDebug($"[RELAY SETUP] อุณหภูมินิ่งเข้าเป้าแล้วที่รอบ {loop + 1}/{maxLoops} (PV = {realTimePv:0.#}°C)", Color.DarkCyan);
                        return true;
                    }
                }
                else
                {
                    stableCount = 0;
                }

                try
                {
                    await Task.Delay(250, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }

            LogToRx($"[RELAY SETUP] อุณหภูมิปัจจุบัน = {actualPv:0.#}°C (เป้าหมาย: {expectedTemp:0.#}°C)", Color.DarkOrange);
            return Math.Abs(actualPv - expectedTemp) <= tolerance;
        }

        private async Task<string> ReadModelContactAsync(string name, ushort address)
        {
            if (_userRequestedStop) return "CANCELLED";
            string displayName = GetContactDisplayName(name);
            LogToRx($"กำลังตรวจสอบ {displayName}...", Color.Blue);
            try
            {
                if (_relayPort == null || !_relayPort.IsOpen)
                    throw new InvalidOperationException("ยังไม่ได้เชื่อมต่อพอร์ต Relay");
                // Read sequentially; never start the alarm read while an output read is pending.
                short value = await ReadRelaySingleRegisterAsync(address,
                    timeoutMs: 1000, retries: 2, slaveId: 3,
                    cancellationToken: _modelContactsCts!.Token);
                if (_userRequestedStop) return "CANCELLED";
                string result = ModelTestResults.ContactResult(value);
                LogToRx($"ตรวจสอบ {displayName}: {(value == 1 ? "ผ่าน" : "ไม่ผ่าน")}",
                    value == 1 ? Color.Green : Color.Red);
                LogWriteDebug($"[{name}] ID 3, Register {address} = {value}: {result}",
                    value == 1 ? Color.Green : Color.Red);
                return result;
            }
            catch (Exception ex)
            {
                LogToRx($"ตรวจสอบ {displayName} ไม่สำเร็จ", Color.Red);
                LogWriteDebug($"[{name}] ตรวจสอบไม่สำเร็จ: {ex.Message}", Color.Red);
                return _userRequestedStop ? "CANCELLED" : "ERROR";
            }
        }

        private async Task<bool> WriteAndVerifyModelRegisterAsync(
            ushort address,
            short value,
            string description)
        {
            string registerName = GetFirmwareRegisterName(address);
            string action = description.IndexOf("RESET", StringComparison.OrdinalIgnoreCase) >= 0
                ? "คืนค่า"
                : "ปรับ";

            LogToRx($"กำลัง{action} {registerName} เป็น {value}...", Color.Blue);
            LogWriteDebug(
                $"[{description}] กำลังเขียน Register {address} = {value}",
                Color.Blue);

            bool writeOk = await ExecuteWriteSingleRegisterVerifiedAsync(
                slaveId: TargetID,
                registerAddress: address,
                value: value,
                maxAttempts: 5,
                timeoutMs: 1000);

            if (!writeOk)
            {
                LogToRx($"{action} {registerName} เป็น {value} ไม่สำเร็จ", Color.Red);
                LogWriteDebug(
                    $"[{description} ERROR] เขียน Register {address} = {value} ไม่สำเร็จ",
                    Color.Red);
                return false;
            }

            const int maxReadBackAttempts = 5;

            for (int attempt = 1; attempt <= maxReadBackAttempts; attempt++)
            {
                try
                {
                    await Task.Delay(100);

                    short actualValue = await ReadSingleRegisterAsync(
                        address,
                        timeoutMs: 1000,
                        retries: 0,
                        slaveId: TargetID);

                    if (actualValue == value)
                    {
                        LogToRx($"{action} {registerName} เป็น {value} สำเร็จ", Color.Green);
                        LogWriteDebug(
                            $"[{description}] Register {address} = {actualValue} สำเร็จ",
                            Color.Green);
                        return true;
                    }

                    LogWriteDebug(
                        $"[{description}] Register {address} อ่านกลับได้ {actualValue}, " +
                        $"รอค่า {value} ({attempt}/{maxReadBackAttempts})",
                        Color.Orange);
                }
                catch (Exception ex)
                {
                    LogWriteDebug(
                        $"[{description}] อ่านกลับ Register {address} ไม่สำเร็จ " +
                        $"({attempt}/{maxReadBackAttempts}): {ex.Message}",
                        Color.Orange);
                }
            }

            LogToRx($"{action} {registerName} เป็น {value} ไม่สำเร็จ", Color.Red);
            LogWriteDebug(
                $"[{description} ERROR] Register {address} ไม่เปลี่ยนเป็น {value}",
                Color.Red);
            return false;
        }

        private bool SaveModelResults(
            string relay,
            string alarm,
            bool pass,
            IReadOnlyList<bool>? buttonResults = null,
            IReadOnlyList<bool>? ledProblems = null)
        {
            if (_skipExcelWrite || _modelReportTargets.Count == 0) return false;
            bool saved = true;
            foreach (var group in _modelReportTargets.GroupBy(t => t.Path))
            {
                try
                {
                    using (var workbook = new XLWorkbook(group.Key))
                    {
                        var sheet = workbook.Worksheet(1);
                        foreach (var target in group)
                        {
                            ModelTestResults.WriteRow(sheet, target.Row, relay, alarm, pass, txt_model.Text.Trim());
                            if (buttonResults != null && ledProblems != null)
                            {
                                QcReportLayout.WriteButtons(
                                    sheet,
                                    target.Row,
                                    buttonResults,
                                    ledProblems,
                                    txt_model.Text.Trim());
                            }
                        }
                        if (!SaveWorkbookWithRetry(workbook, group.Key,
                                "บันทึกผล Test For All หลังคืนค่าอุปกรณ์"))
                            saved = false;
                    }
                }
                catch (Exception ex)
                {
                    saved = false;
                    LogToRx($"[REPORT] บันทึกผลรวมไม่สำเร็จ: {ex.Message}", Color.Red);
                }
            }
            return saved;
        }

        private async Task RunModelTestAllAsync()
        {
            if (!ValidateTestModel(out string model) || string.IsNullOrWhiteSpace(txt_SN.Text))
            {
                ShowRunOverview();
                SetRunStepLight(1, Color.Red);
                return;
            }

            DateTime inspectionStartTime = DateTime.Now;

            try
            {
                BeginAutoReportTransaction(
                    txt_templatePath.Text.Trim(),
                    txt_Lot.Text.Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"ไม่สามารถเตรียมไฟล์ผลทดสอบชั่วคราวได้\n\n{ex.Message}",
                    "เริ่มการทดสอบไม่ได้",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // This workflow is dispatched only for QC. QA keeps its original layout and tests.
            bool isQAMode = false;
            var defs = GetTestDefinitions();
            var results = new List<TestRunResult>();
            var lockedControls = new List<KeyValuePair<Control, bool>>();
            foreach (string name in new[] { "txt_name", "txt_supply", "txt_model", "txt_SN", "txt_Lot", "txt_templatePath" })
            {
                var control = Controls.Find(name, true).FirstOrDefault();
                if (control == null) continue;
                lockedControls.Add(new KeyValuePair<Control, bool>(control, control.Enabled));
                control.Enabled = false;
            }

            bool timerWasRunning = timer1.Enabled;
            _modelReportTargets.Clear();
            _modelTestAllActive = true;
            string relay = "-";
            string alarm = model == ModelAB ? "-" : "N/A";
            bool pass = false;
            bool saved = false;
            bool temperaturePass = false;
            bool relayPass = false;
            bool relayStarted = false;
            bool alarmRegisterTouched = false;
            string contactFailureDetail = string.Empty;
            _pendingAutoButtonResults = null;
            _pendingAutoLedProblems = null;
            try
            {
                for (int i = 0; i < defs.Count && !_userRequestedStop; i++)
                {
                    var def = defs[i];
                    results.Add(await RunCalibrationTestAsync(def.TestName, def.TestType,
                        def.GetTestCases(isQAMode), def.QaTab, def.QcTab,
                        null, 1.0, showCompletionPopup: false));
                    if (_userRequestedStop || !results.Last().Completed || !results.Last().IsAllPass) break;
                    if (i < defs.Count - 1) await Task.Delay(1500);
                }

                temperaturePass =
                    !_userRequestedStop &&
                    results.Count == defs.Count &&
                    results.All(r => r.Completed && r.IsAllPass);

                ShowRunOverview();
                SetRunStepLight(1, temperaturePass ? Color.LimeGreen : Color.Red);

                if (temperaturePass)
                {
                    relayStarted = true;
                    SetRunStepLight(2, Color.Gold);
                    _isCalibrating = true;
                    _modelContactsCts = new CancellationTokenSource();

                    bool contactSetupOk = true;

                    try
                    {
                        // 1. ปรับ Input (Register 2) เป็น 0 (PTC 2K) เสมอ ทั้ง Type A และ Type A-B
                        contactSetupOk = await WriteAndVerifyModelRegisterAsync(
                            address: 2,
                            value: 0,
                            description: "SET INPUT PTC 2K (INP = 00)");

                        if (!contactSetupOk)
                        {
                            contactFailureDetail = "ตั้งค่า Input: Register 2 = 0 ไม่สำเร็จ";
                            RecordRunFailure(contactFailureDetail);
                        }

                        // 2. ถ้าเป็น Type A-B ให้ตั้งค่า rL (Reg 9) = 21 และ FU2 (Reg 19) = 1
                        if (contactSetupOk && model == ModelAB && !_userRequestedStop)
                        {
                            contactSetupOk = await WriteAndVerifyModelRegisterAsync(
                                address: 9,
                                value: 21,
                                description: "A-B ALARM SETUP (rL = 21)");

                            if (!contactSetupOk)
                            {
                                contactFailureDetail = "ตั้งค่า Alarm: Register 9 = 21 ไม่สำเร็จ";
                                RecordRunFailure(contactFailureDetail);
                            }

                            if (contactSetupOk && !_userRequestedStop)
                            {
                                alarmRegisterTouched = true;
                                contactSetupOk = await WriteAndVerifyModelRegisterAsync(
                                    address: 19,
                                    value: 1,
                                    description: "A-B ALARM ENABLE (FU2 = 1)");

                                if (!contactSetupOk)
                                {
                                    contactFailureDetail = "เปิด Alarm: Register 19 = 1 ไม่สำเร็จ";
                                    RecordRunFailure(contactFailureDetail);
                                }
                            }
                        }

                        // 3. จ่าย Ohm ให้เป็นอุณหภูมิ 40 องศา (PTC 2K คือ 2283 โอห์ม) และรอให้อุณหภูมิใกล้เคียง 40°C
                        if (contactSetupOk && !_userRequestedStop)
                        {
                            const int ohm40C = 2283;
                            const double targetTemp = 40.0;
                            LogToRx($"[RELAY SETUP] กำลังจ่าย Ohm = {ohm40C}Ω (เป้าหมาย 40°C สำหรับทดสอบ Relay)...", Color.Blue);
                            SendOhm(ohm40C);

                            // รอให้อุณหภูมิเข้าใกล้ 40°C เหมือนตอนทดสอบอุณหภูมิ
                            await WaitForTemperatureAsync(targetTemp, tolerance: 1.0, maxLoops: 20, cancellationToken: _modelContactsCts.Token);
                        }

                        if (contactSetupOk && !_userRequestedStop)
                        {
                            relay = await ReadModelContactAsync("RELAY OUTPUT", RelayStatusRegister);
                            if (!_userRequestedStop) IncrementRunProgress();

                            if (model == ModelAB && !_userRequestedStop && QcReportLayout.IsPass(relay))
                            {
                                alarm = await ReadModelContactAsync("ALARM", AlarmStatusRegister);
                                if (!_userRequestedStop) IncrementRunProgress();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        contactSetupOk = false;
                        contactFailureDetail = $"ช่วงทดสอบ Relay / Alarm: {ex.Message}";
                        RecordRunFailure(contactFailureDetail);
                        LogToRx($"[RELAY / ALARM ERROR] {ex.Message}", Color.Red);
                    }

                    if (model == ModelAB && !_userRequestedStop && !QcReportLayout.IsPass(relay))
                    {
                        // ชดเชย progress ของ Alarm ที่ไม่ได้อ่านเพราะ Relay ไม่ผ่าน
                        IncrementRunProgress();
                    }
                }

                pass = ModelTestResults.OverallPass(model,
                    results.Count == defs.Count && results.All(r => r.Completed),
                    results.All(r => r.IsAllPass), _userRequestedStop, relay, alarm);
                relayPass = temperaturePass && pass;

                if (!pass && !_userRequestedStop && !_testFailureMessageShown)
                {
                    if (!temperaturePass)
                    {
                        string failedTest = results.LastOrDefault().TestName ?? "การทดสอบอุณหภูมิ";
                        ShowRunFailureMessage($"{failedTest}\nสาเหตุ: การทดสอบอุณหภูมิไม่สำเร็จ");
                    }
                    else if (!string.IsNullOrEmpty(contactFailureDetail))
                    {
                        ShowRunFailureMessage(contactFailureDetail);
                    }
                    else if (!QcReportLayout.IsPass(relay))
                    {
                        RecordRunFailure($"Relay Output: ผลที่อ่านได้ '{relay}' (ไม่ผ่านเกณฑ์)");
                        ShowRunFailureMessage($"Relay Output\nผลที่อ่านได้: {relay}");
                    }
                    else if (model == ModelAB && !QcReportLayout.IsPass(alarm))
                    {
                        RecordRunFailure($"Alarm: ผลที่อ่านได้ '{alarm}' (ไม่ผ่านเกณฑ์)");
                        ShowRunFailureMessage($"Alarm\nผลที่อ่านได้: {alarm}");
                    }
                }

                if (relayStarted)
                {
                    ShowRunOverview();
                    SetRunStepLight(2, relayPass ? Color.LimeGreen : Color.Red);
                }
            }
            catch (Exception ex)
            {
                pass = false;
                LogToRx($"[TEST ALL] {ex.Message}", Color.Red);
                if (!_userRequestedStop)
                    ShowRunFailureMessage($"Test For All\nสาเหตุ: {ex.Message}");
                ShowRunOverview();
                SetRunStepLight(relayStarted ? 2 : 1, Color.Red);
            }
            finally
            {
                if (_userRequestedStop)
                {
                    pass = false;
                    if (relay == "NOT TESTED") relay = "CANCELLED";
                    if (alarm == "NOT TESTED") alarm = "CANCELLED";
                }
                _modelContactsCts?.Dispose();
                _modelContactsCts = null;
                _modelTestAllActive = false;
                _isCalibrating = false;
                foreach (var item in lockedControls) item.Key.Enabled = item.Value;
                if (timerWasRunning && _serialPort.IsOpen) timer1.Start();
            }

            bool? inspection = pass ? null : false;
            bool cleanupOk = true;
            var cleanupFailures = new List<string>();

            try
            {
                if (pass)
                {
                    ShowRunOverview();
                    SetRunStepLight(3, Color.Gold);

                    while (!_userRequestedStop && !inspection.HasValue)
                    {
                        _buttonTestActive = true;
                        try
                        {
                            ShowButtonTestPopup(txt_SN.Text.Trim(), txt_Lot.Text.Trim(),
                                txt_templatePath.Text.Trim(), model, saveResults: true,
                                deferSaveUntilCleanup: true);
                            inspection = _lastButtonInspection;
                        }
                        finally
                        {
                            _buttonTestActive = false;
                            if (timerWasRunning && _serialPort.IsOpen) timer1.Start();
                        }

                        if (!inspection.HasValue && !_userRequestedStop)
                        {
                            ShowRunOverview();
                            SetRunStepLight(3, Color.Gold);
                            bool retryRequested = await WaitForButtonTestRetryAsync();
                            if (!retryRequested)
                                inspection = false;
                        }
                    }

                    ShowRunOverview();
                    SetRunStepLight(3, inspection == true ? Color.LimeGreen : Color.Red);

                    if (!_userRequestedStop && inspection == false)
                    {
                        RecordRunFailure("การทดสอบปุ่มกดหรือหลอด LED ไม่ผ่านเกณฑ์");
                        ShowRunFailureMessage("การทดสอบปุ่มหรือ LED");
                    }
                }
            }
            finally
            {
                // A-B: คืนค่า Register 19 = 0 เฉพาะเมื่อผลการทดสอบทั้งหมดผ่าน (PASS)
                // ถ้าไม่ผ่าน (FAIL / ERROR / Stop): ไม่ต้องคืนค่าใดๆ ทั้งสิ้น ตามคำสั่งของผู้ใช้งาน เพื่อให้ตรวจเช็คหน้างานได้
                bool testFullyPassed = pass && inspection == true && !_userRequestedStop;

                if (testFullyPassed && model == ModelAB && alarmRegisterTouched)
                {
                    try
                    {
                        bool resetAlarmOk = await WriteAndVerifyModelRegisterAsync(
                            address: 19,
                            value: 0,
                            description: "A-B ALARM RESET AFTER BUTTON TEST");

                        if (!resetAlarmOk)
                        {
                            alarm = "ERROR";
                            cleanupOk = false;
                            string failText = "คืนค่า Register 19 = 0 ไม่สำเร็จ";
                            RecordRunFailure(failText);
                            cleanupFailures.Add(failText);
                        }
                    }
                    catch (Exception ex)
                    {
                        alarm = "ERROR";
                        cleanupOk = false;
                        string failText = $"คืนค่า Register 19 = 0 ไม่สำเร็จ: {ex.Message}";
                        RecordRunFailure(failText);
                        cleanupFailures.Add(failText);
                    }
                }

                // หมายเหตุ: ไม่ต้องคืนค่า Register 2 (INP) แล้ว เพราะปรับเป็น 00 ไว้ตั้งแต่ช่วงทดสอบ Relay
                // และกรณีทดสอบไม่ผ่าน จะคงค่าทุกอย่างไว้ตามที่ผู้ใช้กำหนด
            }

            if (!cleanupOk)
            {
                pass = false;
                ShowRunFailureMessage(string.Join(Environment.NewLine, cleanupFailures));
            }

            if (_userRequestedStop)
            {
                // All point-by-point writes were made only to the staging copy.
                // Discarding it leaves the real LOT file exactly as it was.
                DiscardAutoReportTransaction();
                saved = false;
            }
            else
            {
                // PASS and FAIL are committed once, after hardware cleanup.
                saved = SaveModelResults(
                    relay,
                    alarm,
                    pass,
                    _pendingAutoButtonResults,
                    _pendingAutoLedProblems);

                if (saved)
                    saved = CommitAutoReportTransaction();

                if (!saved)
                    DiscardAutoReportTransaction(logDiscard: false);

                LogToRx(
                    saved
                        ? $"บันทึกผล S/N {txt_SN.Text.Trim()} ลง Excel หลังคืนค่าอุปกรณ์เรียบร้อย"
                        : $"บันทึกผล S/N {txt_SN.Text.Trim()} ลง Excel ไม่สำเร็จ",
                    saved ? Color.DarkGreen : Color.Red);
                LogWriteDebug(
                    saved
                        ? "[AUTO SAVE] บันทึกผลสุดท้ายและย้ายเข้าไฟล์ LOT สำเร็จ"
                        : "[AUTO SAVE] บันทึกผลสุดท้ายลงไฟล์ LOT ไม่สำเร็จ",
                    saved ? Color.DarkGreen : Color.Red);
            }

            bool? finalResult = _userRequestedStop ? null : inspection;
            if (!cleanupOk)
                finalResult = false;
            //if (inspection == true && !_userRequestedStop)
            //{
            //    SetRunStepLight(4, Color.Gold);
            //    bool parameterPass = await SetFinalParameterAsync();
            //    ShowRunOverview();
            //    SetRunStepLight(4, parameterPass ? Color.LimeGreen : Color.Red);
            //    finalResult = parameterPass;
            //}

            string overall = _userRequestedStop
                ? "CANCELLED"
                : finalResult.HasValue
                    ? (finalResult.Value ? "PASS" : "FAIL")
                    : "รอผลปุ่ม / LED";
            if (!_userRequestedStop && finalResult.HasValue)
                LogFinalResult(txt_SN.Text.Trim(), finalResult.Value);
            var statusLabel = Controls.Find("lbl_testStatus", true).FirstOrDefault();
            if (statusLabel != null)
            {
                statusLabel.Text = $"Test For All ({model}): {overall}";
                statusLabel.ForeColor = _userRequestedStop
                    ? Color.DarkOrange
                    : finalResult == true ? Color.Green : finalResult == false ? Color.Red : Color.DarkOrange;
            }

            ServerPostResult serverPostResult = ServerPostResult.Skipped;
            if (!_userRequestedStop && finalResult.HasValue && saved && sent_server.Checked)
            {
                serverPostResult = await PostQcResultAsync(
                    inspectionStartTime,
                    DateTime.Now,
                    finalResult.Value);
            }

            if (_userRequestedStop)
            {
                MessageBox.Show(
                    this,
                    "การทดสอบถูกยกเลิกโดยผู้ใช้งาน",
                    "การทดสอบถูกยกเลิก",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else if (finalResult == true && saved)
            {
                string completionText = serverPostResult switch
                {
                    ServerPostResult.Success => "ทดสอบสำเร็จ\n\nบันทึก Excel และส่ง Server สำเร็จ",
                    ServerPostResult.Timeout => "ทดสอบสำเร็จและบันทึก Excel แล้ว\n\nแต่ Server ไม่ตอบกลับภายใน 10 วินาที",
                    ServerPostResult.Failed => "ทดสอบสำเร็จและบันทึก Excel แล้ว\n\nแต่ส่งข้อมูลไป Server ไม่สำเร็จ",
                    _ => "ทดสอบสำเร็จ"
                };

                MessageBox.Show(
                    this,
                    completionText,
                    "ผลการทดสอบ",
                    MessageBoxButtons.OK,
                    serverPostResult is ServerPostResult.Timeout or ServerPostResult.Failed
                        ? MessageBoxIcon.Warning
                        : MessageBoxIcon.Information);
            }
            else if (!_testFailureMessageShown)
            {
                string failureText = _currentRunFailures.Count > 0
                    ? string.Join(Environment.NewLine, _currentRunFailures)
                    : (saved
                        ? "ไม่สามารถระบุจุดผิดพลาดได้ กรุณาตรวจสอบข้อความการทำงาน"
                        : "การบันทึกผลลง Excel ไม่สำเร็จ");

                ShowRunFailureMessage(failureText);
            }
        }

        #endregion

        #region 27 ชื่อ Model และ Supply ในหัวรายงาน

        private void UpdateModelReportHeader(IXLWorksheet sheet, bool isQa)
        {
            string model = txt_model.Text.Trim();
            if (!ModelTestResults.IsValidModel(model) || txt_supply.SelectedIndex < 0) throw new InvalidOperationException("กรุณาเลือก Model และ supply ก่อนสร้างรายงาน");

            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                isQa ? "TempleteQA.xlsx" : "DEF-03N QC Inspection.xlsx");
            // Read a snapshot even when the template is open in Excel. Never edit the template.
            using var snapshot = new MemoryStream();
            using (var source = new FileStream(templatePath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite))
                source.CopyTo(snapshot);
            snapshot.Position = 0;
            using var template = new XLWorkbook(snapshot);
            ModelTestResults.WriteHeader(sheet, template.Worksheet(1), model, !isQa);
            if (isQa)
            {
                sheet.Cell("J3").Value = GetSelectedReportModelCode();
                sheet.Cell("N10").Value = GetSelectedPowerSupplyText();
            }
            else
            {
                var modelLabelCell = FindHeaderCell(sheet, "Model:");
                if (modelLabelCell != null)
                    sheet.Cell(modelLabelCell.Address.RowNumber, modelLabelCell.Address.ColumnNumber + 1).Value = GetSelectedReportModelCode();
                else
                    sheet.Cell("J2").Value = GetSelectedReportModelCode();

                var supplyCell = FindHeaderCell(sheet, "Power Supply");
                if (supplyCell != null)
                    supplyCell.Value = $"Power Supply {GetSelectedPowerSupplyText()}";
                else
                    sheet.Cell("P8").Value = $"Power Supply {GetSelectedPowerSupplyText()}";
            }
        }
        #endregion

        #region 28 ทดสอบปุ่ม LED และบันทึกผล

        private bool _buttonTestActive;
        private bool? _lastButtonInspection;
        private bool[]? _pendingAutoButtonResults;
        private bool[]? _pendingAutoLedProblems;
        private readonly SemaphoreSlim _mainReadGate = new SemaphoreSlim(1, 1);
        private int _priorityCommandCount;
        private byte _pendingReadSlaveId = 1;

        private sealed class ButtonStepLamp : Control
        {
            public Color LampColor { get; set; } = Color.LightGray;
            public ButtonStepLamp() { DoubleBuffered = true; Size = new Size(66, 66); }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(LampColor);
                e.Graphics.FillEllipse(brush, 3, 3, Width - 6, Height - 6);
            }
        }

        private async Task<bool> WaitForButtonTestRetryAsync()
        {
            var retryTcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            _pendingRunButtonRetryTcs = retryTcs;
            _runWaitingForButtonRetry = true;
            btn_Testbtn.Enabled = true;

            LogToRx(
                "หน้าต่างทดสอบปุ่มถูกปิด กรุณากดปุ่มทดสอบปุ่มเพื่อเริ่มใหม่ หรือกด Stop เพื่อยกเลิก",
                Color.DarkOrange);
            LogWriteDebug(
                "[BUTTON TEST] Popup ถูกปิด กดปุ่มทดสอบปุ่มเพื่อเริ่มใหม่ หรือกด Stop เพื่อยกเลิก",
                Color.DarkOrange);

            try
            {
                return await retryTcs.Task;
            }
            finally
            {
                if (ReferenceEquals(_pendingRunButtonRetryTcs, retryTcs))
                    _pendingRunButtonRetryTcs = null;

                _runWaitingForButtonRetry = false;
                if (!_disconnectInProgress)
                    btn_Testbtn.Enabled = false;
            }
        }

        private void btn_Testbtn_Click(object? sender, EventArgs e)
        {
            if (_runWaitingForButtonRetry)
            {
                _pendingRunButtonRetryTcs?.TrySetResult(true);
                return;
            }

            if (!CheckManualConnections(true, false, false)) return;
            if (_isCalibrating || _modelTestAllActive || _buttonTestActive || _isWriting) return;
            if (_manualPopupQAChecked)
            {
                MessageBox.Show("การทดสอบปุ่มใช้รายงาน QC กรุณาเลือกโหมด QC ก่อนครับ");
                return;
            }
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                MessageBox.Show("กรุณาเชื่อมต่อพอร์ต DEF-03N ก่อนทดสอบปุ่ม");
                return;
            }
            string model = txt_model.Text.Trim();
            string sn = txt_SN.Text.Trim();
            string lot = txt_Lot.Text.Trim();
            string folder = txt_templatePath.Text.Trim();

            bool timerWasRunning = timer1.Enabled;
            _buttonTestActive = true;
            try { ShowButtonTestPopup(sn, lot, folder, model, saveResults: false); }
            finally
            {
                _buttonTestActive = false;
                if (timerWasRunning && _serialPort.IsOpen) timer1.Start();
                CheckSNWhileTyping();
            }
        }

        private void ShowButtonTestPopup(
            string sn,
            string lot,
            string folder,
            string model,
            bool saveResults,
            bool deferSaveUntilCleanup = false)
        {
            _lastButtonInspection = null;
            bool canSave = saveResults && sn.Length > 0 && lot.Length > 0 &&
                Directory.Exists(folder) && ModelTestResults.IsValidModel(model) &&
                txt_supply.SelectedIndex >= 0;
            var background = Color.FromArgb(7, 23, 35);
            var card = Color.FromArgb(20, 40, 55);
            var activeCard = Color.FromArgb(39, 58, 65);
            var cyan = Color.FromArgb(73, 218, 242);
            var muted = Color.FromArgb(166, 185, 199);
            var waiting = Color.FromArgb(93, 112, 126);
            using var popup = new Form
            {
                Text = "ทดสอบปุ่มและ LED",
                ClientSize = new Size(450, 620),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = background,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                AutoScaleMode = AutoScaleMode.Dpi
            };
            var title = new Label
            {
                Text = "ทดสอบการทำงานปุ่ม",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = cyan,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(12, 10, 426, 50)
            };
            popup.Controls.Add(title);
            popup.Controls.Add(new Label
            {
                Text = $"S/N: {sn}   •   {model}",
                AutoEllipsis = true,
                ForeColor = muted,
                Bounds = new Rectangle(24, 65, 402, 28)
            });
            popup.Controls.Add(new Panel { BackColor = Color.FromArgb(58, 79, 94), Bounds = new Rectangle(24, 94, 402, 1) });
            var leds = new CheckBox[3];
            string[] ledNames = { "LED OUT", "LED AL", "7-SEG" };
            for (int i = 0; i < leds.Length; i++)
            {
                leds[i] = new CheckBox { Text = ledNames[i], Bounds = new Rectangle(24 + ((i + 1) % 3) * 140, 104, 122, 30), AutoSize = false, Cursor = Cursors.Hand };
                leds[i].TabIndex = (i + 1) % 3;
                leds[i].Visible = i != 1 || model != ModelA;
                popup.Controls.Add(leds[i]);
            }
            var ledInstruction = new Label
            {
                Text = "ติ๊กเฉพาะรายการ LED / 7-SEG ที่มีปัญหา",
                ForeColor = Color.Red,
                Font = new Font("Segoe UI", 10),
                Bounds = new Rectangle(24, 140, 402, 28)
            };
            popup.Controls.Add(ledInstruction);

            bool ledInspectionAcknowledged = false;
            void UpdateLedInstructionColor()
            {
                bool hasSelectedFault = leds.Any(led => led.Visible && led.Checked);
                ledInstruction.ForeColor = ledInspectionAcknowledged || hasSelectedFault
                    ? muted
                    : Color.Red;
            }

            foreach (CheckBox led in leds)
                led.CheckedChanged += (_, _) => UpdateLedInstructionColor();

            string[] names = { "F", "ลด", "เพิ่ม" };
            short[] expected = { 3, 2, 1 };
            var lamps = new ButtonStepLamp[3];
            var rows = new Panel[3];
            var labels = new Label[3];
            for (int i = 0; i < 3; i++)
            {
                rows[i] = new MyRoundedPanel { Bounds = new Rectangle(24, 180 + i * 92, 402, 82), BackColor = card, BorderRadius = 12 };
                lamps[i] = new ButtonStepLamp { Location = new Point(17, 14), Size = new Size(54, 54), LampColor = waiting };
                labels[i] = new Label
                {
                    Text = $"กดปุ่ม {names[i]}",
                    Bounds = new Rectangle(105, 16, 280, 60),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 13),
                    ForeColor = Color.White
                };
                rows[i].Controls.Add(lamps[i]); rows[i].Controls.Add(labels[i]); popup.Controls.Add(rows[i]);
            }
            var status = new Label { Text = "กด Test เพื่อเริ่มทดสอบ", Bounds = new Rectangle(24, 462, 402, 66), ForeColor = cyan, AutoEllipsis = true };
            var test = new Button { Text = "▶ Test", Bounds = new Rectangle(24, 541, 402, 52), BackColor = Color.FromArgb(255, 217, 0), ForeColor = Color.Black, Font = new Font("Segoe UI", 16, FontStyle.Bold), FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
            test.FlatAppearance.BorderSize = 0;
            popup.Controls.AddRange(new Control[] { status, test });

            bool running = false, closeRequested = false, closeAfterCompletion = false;
            bool[] results = new bool[3];
            string reportPath = "";
            int reportRow = 0, activeStep = -1;
            CancellationTokenSource? cts = null;
            var stepClock = new Stopwatch();
            using var countdown = new System.Windows.Forms.Timer { Interval = 100 };
            countdown.Tick += (_, _) =>
            {
                if (activeStep >= 0)
                    status.Text = $"กดปุ่ม {names[activeStep]} บนเครื่อง • รอค่า {expected[activeStep]}\nเหลือ {Math.Max(0, 10 - stepClock.Elapsed.TotalSeconds):0.0} วินาที";
            };
            test.Click += async (_, _) =>
            {
                if (running) return;
                ledInspectionAcknowledged = true;
                UpdateLedInstructionColor();
                running = true; test.Enabled = false;
                foreach (CheckBox led in leds) led.Enabled = false;
                Array.Clear(results, 0, results.Length);
                foreach (var lamp in lamps) { lamp.LampColor = waiting; lamp.Invalidate(); }
                for (int i = 0; i < 3; i++) { rows[i].BackColor = card; labels[i].Text = $"กดปุ่ม {names[i]}"; }
                cts = new CancellationTokenSource();
                try
                {
                    if (canSave && !deferSaveUntilCleanup)
                    {
                        // กันผลปุ่ม/LED เดิมของ S/N ซ้ำถูกนำมาคิดระหว่างรอบ Retry
                        (reportPath, reportRow) = GetOrCreateReportFileQC(folder, lot, false);
                        using var pendingWorkbook = new XLWorkbook(reportPath);
                        var pendingSheet = pendingWorkbook.Worksheet(1);
                        QcReportLayout.Validate(pendingSheet);
                        pendingSheet.Cell(reportRow, QcReportLayout.SerialColumn).Value = sn;
                        UpdateReportHeaderInfo(pendingSheet, lot, false);

                        for (int col = 16; col <= 21; col++)
                            pendingSheet.Cell(reportRow, col).Value = "NOT TESTED";

                        QcReportLayout.RefreshInspection(pendingSheet, reportRow, model);
                        if (!SaveWorkbookWithRetry(pendingWorkbook, reportPath,
                                "เตรียมผลทดสอบปุ่มอัตโนมัติ"))
                            throw new IOException("เตรียมแถวผลทดสอบปุ่มใน Excel ไม่สำเร็จ");
                    }

                    // Let the last normal polling response finish before reading Register 27.
                    await Task.Delay(250, cts.Token);
                    for (int i = 0; i < 3; i++)
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        activeStep = i; stepClock.Restart(); countdown.Start();
                        rows[i].BackColor = activeCard;
                        lamps[i].LampColor = Color.Gold; lamps[i].Invalidate();
                        results[i] = await ButtonTestRunner.WaitForButtonAsync(expected[i],
                            token => ReadSingleRegisterAsync(27, timeoutMs: 400, retries: 0,
                                slaveId: 1, cancellationToken: token), TimeSpan.FromSeconds(10), cts.Token);

                        countdown.Stop();
                        activeStep = -1;

                        lamps[i].LampColor = results[i] ? Color.FromArgb(16, 175, 91) : Color.Firebrick;
                        lamps[i].Invalidate(); rows[i].BackColor = card;
                        labels[i].Text = $"ปุ่ม {names[i]}: {(results[i] ? "PASS" : "FAIL (หมดเวลา)")}";

                        LogToRx(
                            $"ทดสอบปุ่ม {names[i]}: {(results[i] ? "ผ่าน" : "ไม่ผ่าน")}",
                            results[i] ? Color.Green : Color.Red);
                        LogWriteDebug(
                            $"[BUTTON TEST] {names[i]} / ID 1 Reg27={expected[i]}: {(results[i] ? "PASS" : "FAIL")}",
                            results[i] ? Color.Green : Color.Red);
                        IncrementRunProgress();

                        if (_userRequestedStop)
                            throw new OperationCanceledException();
                    }
                    if (canSave && deferSaveUntilCleanup)
                    {
                        _pendingAutoButtonResults = results.ToArray();
                        _pendingAutoLedProblems = leds.Select(c => c.Checked).ToArray();
                        _lastButtonInspection = results.All(result => result) &&
                            leds.Where(led => led.Visible).All(led => !led.Checked);

                        status.Text = "ทดสอบครบแล้ว — กำลังคืนค่าอุปกรณ์ก่อนบันทึก Excel";
                        LogToRx("ทดสอบปุ่มและ LED ครบแล้ว กำลังคืนค่าอุปกรณ์", Color.Blue);
                        closeAfterCompletion = true;
                    }
                    else if (canSave)
                    {
                        (reportPath, reportRow) = GetOrCreateReportFileQC(folder, lot, false);
                        using var workbook = new XLWorkbook(reportPath);
                        var sheet = workbook.Worksheet(1);
                        QcReportLayout.Validate(sheet);
                        sheet.Cell(reportRow, QcReportLayout.SerialColumn).Value = sn;
                        QcReportLayout.WriteButtons(sheet, reportRow, results,
                            leds.Select(c => c.Checked).ToArray(), model);
                        UpdateReportHeaderInfo(sheet, lot, false);

                        bool saved = SaveWorkbookWithRetry(
                            workbook, reportPath, "บันทึกปุ่มและ LED อัตโนมัติ");
                        _lastButtonInspection = saved &&
                            QcReportLayout.RefreshInspection(sheet, reportRow, model);

                        LogToRx(
                            saved
                                ? $"บันทึกผล S/N {sn} ลงรายงานเรียบร้อย"
                                : $"บันทึกผล S/N {sn} ลงรายงานไม่สำเร็จ",
                            saved ? Color.DarkGreen : Color.Red);
                        LogWriteDebug(
                            saved
                                ? $"[BUTTON TEST] บันทึก S/N {sn} อัตโนมัติแล้ว"
                                : $"[BUTTON TEST] บันทึก S/N {sn} ไม่สำเร็จ",
                            saved ? Color.DarkGreen : Color.Red);
                        status.Text = saved
                            ? "ทดสอบครบและบันทึกผลอัตโนมัติแล้ว"
                            : "ทดสอบครบ แต่บันทึกผลไม่สำเร็จ";
                        closeAfterCompletion = true;
                    }
                    else if (saveResults)
                    {
                        _lastButtonInspection = false;
                        status.Text = "ทดสอบครบ แต่ข้อมูลรายงานไม่พร้อมสำหรับบันทึก";
                        closeAfterCompletion = true;
                    }
                    else
                    {
                        status.Text = "ทดสอบครบแล้ว — โหมดทดสอบอย่างเดียว ไม่บันทึก Excel";
                    }
                }
                catch (OperationCanceledException)
                {
                    status.Text = "ยกเลิกการทดสอบ ผลยังไม่ครบ";
                    // ปิดด้วย X ระหว่างทดสอบถือเป็น "รอเริ่มใหม่" ไม่ใช่ผล FAIL
                    _lastButtonInspection = null;
                }
                catch (Exception ex)
                {
                    status.Text = "ทดสอบไม่สำเร็จ: " + ex.Message;
                    LogToRx("ทดสอบปุ่มไม่สำเร็จ กรุณาตรวจสอบการเชื่อมต่ออุปกรณ์", Color.Red);
                    LogWriteDebug("[BUTTON TEST] " + ex.Message, Color.Red);
                    if (saveResults)
                    {
                        _lastButtonInspection = false;
                        closeAfterCompletion = true;
                        MessageBox.Show(popup, ex.Message, "ทดสอบ/บันทึกไม่สำเร็จ",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                finally
                {
                    countdown.Stop(); activeStep = -1; running = false; test.Enabled = true;
                    foreach (CheckBox led in leds) led.Enabled = true;
                    cts.Dispose(); cts = null;
                    if (closeRequested || closeAfterCompletion) popup.Close();
                }
            };
            popup.FormClosing += (_, e) =>
            {
                if (!running) return;
                closeRequested = true; e.Cancel = true; cts?.Cancel();
            };
            popup.ShowDialog(this);
        }

        private void CreateNewQcReport(string templatePath, string reportPath)
        {
            using var snapshot = new MemoryStream();
            using (var file = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) file.CopyTo(snapshot);
            snapshot.Position = 0;
            using var workbook = new XLWorkbook(snapshot);
            QcReportLayout.ClearExampleRows(workbook.Worksheet(1));
            string temporaryPath = reportPath + "." + Guid.NewGuid().ToString("N") + ".tmp.xlsx";
            try { workbook.SaveAs(temporaryPath); File.Move(temporaryPath, reportPath); }
            finally { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
        }
        #endregion

        #region 29 จัดหน้าตา Popup Manual Test และ Manual Input

        private static Form CreateManualWindow() => new Form
        {
            ClientSize = new Size(450, 620),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(7, 23, 35),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11),
            AutoScaleMode = AutoScaleMode.Dpi
        };

        private static void SetManualHeading(Form window, Label title, string hint)
        {
            window.Text = title.Text;
            title.Bounds = new Rectangle(24, 16, 402, 54);
            title.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.BackColor = window.BackColor;
            title.ForeColor = Color.FromArgb(73, 218, 242);
            window.Controls.Add(title);
            window.Controls.Add(new Label
            {
                Text = hint,
                Bounds = new Rectangle(24, 78, 402, 32),
                ForeColor = Color.FromArgb(166, 185, 199)
            });
            window.Controls.Add(new Panel
            {
                Bounds = new Rectangle(24, 114, 402, 1),
                BackColor = Color.FromArgb(58, 79, 94)
            });
        }

        private static MyRoundedPanel ManualCard(Form window, Rectangle bounds)
        {
            var card = new MyRoundedPanel
            {
                Bounds = bounds,
                BorderRadius = 12,
                BackColor = Color.FromArgb(20, 40, 55)
            };
            window.Controls.Add(card);
            return card;
        }

        private static void ArrangeManualTest(Form window, Label title, CheckBox qa,
            CheckedListBox choices, System.Windows.Forms.Button test,
            System.Windows.Forms.Button select, System.Windows.Forms.Button stop)
        {
            SetManualHeading(window, title, "เลือก Type ที่ต้องการทดสอบ");
            qa.Bounds = new Rectangle(24, 130, 402, 32);
            qa.BackColor = window.BackColor;
            qa.Font = new Font("Segoe UI", 11);
            window.Controls.Add(qa);
            var card = ManualCard(window, new Rectangle(24, 180, 402, 230));
            choices.Bounds = new Rectangle(20, 22, 362, 188);
            choices.Font = new Font("Segoe UI", 17);
            choices.BackColor = card.BackColor;
            card.Controls.Add(choices);
            select.Bounds = new Rectangle(24, 426, 402, 44);
            window.Controls.Add(select);
            window.Controls.Add(new Label
            {
                Text = "กด Test เพื่อเริ่มทดสอบ",
                ForeColor = Color.FromArgb(73, 218, 242),
                Bounds = new Rectangle(24, 488, 402, 32)
            });
            test.Bounds = new Rectangle(24, 541, 190, 52);
            test.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            stop.Bounds = new Rectangle(230, 541, 196, 52);
            stop.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            window.Controls.Add(test);
            window.Controls.Add(stop);
        }

        private static void ArrangeManualInput(Form window, Label title, ComboBox type, Label typeHint,
            Label temperatureLabel, TextBox temperature, Label resistanceLabel, TextBox resistance,
            Label status, System.Windows.Forms.Button send)
        {
            SetManualHeading(window, title, "เลือก Type และกรอกอุณหภูมิหรือ Resistance");
            var typeCard = ManualCard(window, new Rectangle(24, 140, 402, 96));
            typeHint.Bounds = new Rectangle(18, 10, 366, 28);
            type.Bounds = new Rectangle(18, 48, 366, 32);
            typeCard.Controls.Add(typeHint);
            typeCard.Controls.Add(type);
            var temperatureCard = ManualCard(window, new Rectangle(24, 250, 402, 94));
            temperatureLabel.Bounds = new Rectangle(18, 10, 366, 28);
            temperatureLabel.TextAlign = ContentAlignment.MiddleLeft;
            temperature.Bounds = new Rectangle(18, 48, 366, 30);
            temperatureCard.Controls.Add(temperatureLabel);
            temperatureCard.Controls.Add(temperature);
            var resistanceCard = ManualCard(window, new Rectangle(24, 358, 402, 94));
            resistanceLabel.Bounds = new Rectangle(18, 10, 366, 28);
            resistanceLabel.TextAlign = ContentAlignment.MiddleLeft;
            resistance.Bounds = new Rectangle(18, 48, 366, 30);
            resistanceCard.Controls.Add(resistanceLabel);
            resistanceCard.Controls.Add(resistance);
            status.Bounds = new Rectangle(24, 468, 402, 56);
            status.Font = new Font("Segoe UI", 10);
            status.TextAlign = ContentAlignment.MiddleLeft;
            window.Controls.Add(status);
            send.Bounds = new Rectangle(24, 541, 402, 52);
            send.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            window.Controls.Add(send);
        }
        #endregion


        private void btn_setparameter_Click(object sender, EventArgs e)
        {
            ShowLiveParameterPopup();
        }
    }
}
