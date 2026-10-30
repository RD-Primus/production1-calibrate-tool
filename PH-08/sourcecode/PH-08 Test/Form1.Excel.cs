// =========================================================================================
// [DEVELOPER GUIDE]
// Form1.Excel.cs: "ส่วนประสานงานหน้าจอ UI เกี่ยวกับการจัดการโฟลเดอร์และตั้งค่าแอพพลิเคชัน"
// - บันทึก/โหลดการตั้งค่าพาธโฟลเดอร์รายงานลงไฟล์ 'app_config.txt'
// - เปิดเลือกโฟลเดอร์ปลายทาง (btn_openfolder)
// - ประสานงานกับ ExcelReportService ในการบันทึกรายงานผลการทดสอบ
// - หากต้องการแก้ไข "สูตร ช่องตาราง หัวกระดาษ หรือ ClosedXML" -> ไปที่ Services/Reporting/ExcelReportService.cs
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ClosedXML.Excel;
using PH_08_Test.Services.Reporting;

namespace PH_08_Test
{
    public partial class Form1
    {
        public const string DefaultECN = "ECN-RD-CN-26-10-115";
        public const string DefaultFW = "Primus-PH08v300.hex";
        private static readonly string ConfigFilePath = Path.Combine(Application.StartupPath, "app_config.txt");
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        private readonly System.Windows.Forms.Timer _snCheckTimer = new System.Windows.Forms.Timer();
        private bool _isDuplicateSN = false;
        private int _duplicateSNRow = -1;
        private Color _defaultSnBackColor = Color.White;
        private Color _defaultSnForeColor = Color.FromArgb(40, 40, 40);
        private bool _hasInvalidLotChars = false;
        private bool _wasLotInvalidLogged = false;

        private void InitExcelControls()
        {
            _defaultSnBackColor = txt_SN.BackColor;
            _defaultSnForeColor = txt_SN.ForeColor;

            btn_openfolder.Click += Btn_OpenFolder_Click;

            // Set placeholder
            txt_name.PlaceholderText = "กรุณาลงชื่อ";
            txt_ECN.PlaceholderText = DefaultECN;
            txt_FW.PlaceholderText = DefaultFW;

            // Load saved configurations
            LoadAppConfig();

            _snCheckTimer.Interval = 400;
            _snCheckTimer.Tick += (s, e) =>
            {
                _snCheckTimer.Stop();
                CheckSNWhileTyping();
            };

            // Live Check S/N & LOT while typing & Auto-save changes
            txt_SN.TextChanged += (s, e) => ScheduleSnDuplicateCheck();
            txt_folder.TextChanged += (s, e) => { SaveAppConfig(); ScheduleSnDuplicateCheck(); };
            txt_ECN.TextChanged += (s, e) => SaveAppConfig();
            txt_FW.TextChanged += (s, e) => SaveAppConfig();
            txt_Rev.TextChanged += (s, e) => SaveAppConfig();
            txt_Lot.TextChanged += (s, e) => { SaveAppConfig(); CheckLotWhileTyping(); ScheduleSnDuplicateCheck(); };
            txt_name.TextChanged += (s, e) => SaveAppConfig();

            // S/N Up/Down Button Click Handlers
            btn_up.Click += (s, e) => { txt_SN.Text = IncrementSerialNumber(txt_SN.Text.Trim()); };
            btn_down.Click += (s, e) => { txt_SN.Text = DecrementSerialNumber(txt_SN.Text.Trim()); };

            // Initial check if LOT & SN are filled
            CheckLotWhileTyping();
            CheckSNWhileTyping();
        }

        private void ResetSnColorsToDefault()
        {
            txt_SN.BackColor = _defaultSnBackColor;
            txt_SN.ForeColor = _defaultSnForeColor;
        }

        private void ScheduleSnDuplicateCheck()
        {
            if (_isProcessingBarcode) return;

            ResetSnColorsToDefault();
            _isDuplicateSN = false;
            _duplicateSNRow = -1;

            _snCheckTimer.Stop();
            if (!string.IsNullOrWhiteSpace(txt_SN.Text))
                _snCheckTimer.Start();
        }

        private void Btn_OpenFolder_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            fbd.Description = "กรุณาเลือกโฟลเดอร์สำหรับบันทึกรายงานผลการทดสอบ (Excel)";
            if (!string.IsNullOrWhiteSpace(txt_folder.Text) && Directory.Exists(txt_folder.Text))
            {
                fbd.SelectedPath = txt_folder.Text;
            }

            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                txt_folder.Text = fbd.SelectedPath;
                SaveAppConfig();
                LogMessage($"📂 เลือกโฟลเดอร์บันทึกไฟล์ Excel: {fbd.SelectedPath}");
            }
        }

        private void SaveAppConfig()
        {
            try
            {
                var lines = new[]
                {
                    $"Folder={txt_folder.Text.Trim()}",
                    $"ECN={txt_ECN.Text.Trim()}",
                    $"FW={txt_FW.Text.Trim()}",
                    $"Rev={txt_Rev.Text.Trim()}",
                    $"Lot={txt_Lot.Text.Trim()}",
                    $"Name={txt_name.Text.Trim()}"
                };
                File.WriteAllLines(ConfigFilePath, lines);
            }
            catch
            {
                // Ignored
            }
        }

        private void LoadAppConfig()
        {
            string savedFolder = "";
            string savedECN = "";
            string savedFW = "";
            string savedRev = "";
            string savedLot = "";
            string savedName = "";

            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var lines = File.ReadAllLines(ConfigFilePath);
                    foreach (var rawLine in lines)
                    {
                        var line = rawLine.Trim();
                        if (string.IsNullOrEmpty(line) || !line.Contains('=')) continue;
                        var parts = line.Split('=', 2);
                        var key = parts[0].Trim();
                        var val = parts[1].Trim();

                        if (key.Equals("Folder", StringComparison.OrdinalIgnoreCase)) savedFolder = val;
                        else if (key.Equals("ECN", StringComparison.OrdinalIgnoreCase)) savedECN = val;
                        else if (key.Equals("FW", StringComparison.OrdinalIgnoreCase)) savedFW = val;
                        else if (key.Equals("Rev", StringComparison.OrdinalIgnoreCase)) savedRev = val;
                        else if (key.Equals("Lot", StringComparison.OrdinalIgnoreCase)) savedLot = val;
                        else if (key.Equals("Name", StringComparison.OrdinalIgnoreCase)) savedName = val;
                    }
                }
            }
            catch
            {
                // Ignored
            }

            // Apply Folder (Leave blank if never selected before)
            if (!string.IsNullOrWhiteSpace(savedFolder) && Directory.Exists(savedFolder))
            {
                txt_folder.Text = savedFolder;
            }
            else
            {
                txt_folder.Text = "";
            }

            // Apply ECN (Default: "ECN-RD-CN-26-10-115")
            if (savedECN == "999999" || savedECN == "123456" || string.IsNullOrWhiteSpace(savedECN))
            {
                savedECN = DefaultECN;
            }
            txt_ECN.Text = savedECN;

            // Apply FW (Default: "Primus-PH08v300.hex")
            if (savedFW == "999999" || string.IsNullOrWhiteSpace(savedFW))
            {
                savedFW = DefaultFW;
            }
            txt_FW.Text = savedFW;

            // Apply Rev (Default: "F-PD-07 Rev.6")
            txt_Rev.Text = !string.IsNullOrWhiteSpace(savedRev) ? savedRev : "F-PD-07 Rev.6";

            // Apply Lot
            if (!string.IsNullOrWhiteSpace(savedLot))
            {
                txt_Lot.Text = savedLot;
            }

            // Apply Name
            if (!string.IsNullOrWhiteSpace(savedName))
            {
                txt_name.Text = savedName;
            }
        }

        public static bool ContainsInvalidFileNameChars(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.IndexOfAny(InvalidFileNameChars) >= 0;
        }

        public void CheckLotWhileTyping()
        {
            string lot = txt_Lot.Text;
            if (ContainsInvalidFileNameChars(lot))
            {
                _hasInvalidLotChars = true;
                txt_Lot.ForeColor = Color.White;
                txt_Lot.BackColor = Color.Firebrick;

                if (!_wasLotInvalidLogged)
                {
                    LogMessage("⚠️ [LOT] ตรวจพบอักขระต้องห้าม (\\ / : * ? \" < > |) ห้ามใช้ในชื่อไฟล์", LogFailColor);
                    _wasLotInvalidLogged = true;
                }
            }
            else
            {
                _hasInvalidLotChars = false;
                _wasLotInvalidLogged = false;
                txt_Lot.ForeColor = SystemColors.WindowText;
                txt_Lot.BackColor = SystemColors.Window;
            }
        }

        public void CheckSNWhileTyping()
        {
            string sn = txt_SN.Text.Trim();
            string folder = txt_folder.Text.Trim();
            string lot = txt_Lot.Text.Trim();

            _isDuplicateSN = false;
            _duplicateSNRow = -1;

            if (string.IsNullOrWhiteSpace(sn))
            {
                ResetSnColorsToDefault();
                return;
            }

            // S/N ต้องเป็นตัวเลขเท่านั้น (ห้ามมีตัวหนังสือหรือช่องว่าง) เหมือน DEF-03N
            if (!sn.All(char.IsDigit))
            {
                txt_SN.ForeColor = Color.White;
                txt_SN.BackColor = Color.Firebrick;
                return;
            }

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder) || string.IsNullOrWhiteSpace(lot) || ContainsInvalidFileNameChars(lot))
            {
                ResetSnColorsToDefault();
                return;
            }

            _isDuplicateSN = _excelService.CheckDuplicateSN(folder, lot, sn, out _duplicateSNRow);
            if (_isDuplicateSN)
            {
                txt_SN.ForeColor = Color.White;
                txt_SN.BackColor = Color.Firebrick;
            }
            else
            {
                txt_SN.ForeColor = Color.White;
                txt_SN.BackColor = Color.SeaGreen;
            }
        }

        public bool ValidateDeviceInputsForTest()
        {
            if (string.IsNullOrWhiteSpace(txt_name.Text))
            {
                MessageBox.Show("กรุณากรอกชื่อผู้ทดสอบให้เรียบร้อยก่อนเริ่มทำการทดสอบ", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txt_name.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txt_SN.Text))
            {
                MessageBox.Show("กรุณากรอก S/N ให้เรียบร้อยก่อนเริ่มทำการทดสอบ", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txt_SN.Focus();
                return false;
            }

            string currentSN = txt_SN.Text.Trim();
            if (!currentSN.All(char.IsDigit))
            {
                MessageBox.Show(
                    "Serial Number (S/N) ต้องเป็นตัวเลขเท่านั้น (ห้ามมีตัวหนังสือหรือช่องว่าง) ครับ",
                    "แจ้งเตือน",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txt_SN.Focus();
                txt_SN.SelectAll();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txt_Lot.Text))
            {
                MessageBox.Show("กรุณากรอก LOT ให้เรียบร้อยก่อนเริ่มทำการทดสอบ", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txt_Lot.Focus();
                return false;
            }

            if (_hasInvalidLotChars || ContainsInvalidFileNameChars(txt_Lot.Text))
            {
                MessageBox.Show("ชื่อ LOT มีอักขระต้องห้ามสำหรับตั้งชื่อไฟล์ (\\ / : * ? \" < > |)\nกรุณาแก้ไขก่อนเริ่มทำการทดสอบ", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txt_Lot.Focus();
                txt_Lot.SelectAll();
                return false;
            }

            string folder = txt_folder.Text.Trim();
            if (string.IsNullOrWhiteSpace(folder))
            {
                MessageBox.Show("กรุณาเลือก Folder สำหรับบันทึกไฟล์รายงานผลการทดสอบ", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btn_openfolder.Focus();
                return false;
            }

            try
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ไม่สามารถสร้างหรือเข้าถึงโฟลเดอร์ที่ระบุได้:\n{ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // ตรวจสอบว่าไฟล์ Excel ของ LOT นี้เปิดค้างอยู่หรือไม่ ก่อนเริ่มทดสอบ
            string lot = txt_Lot.Text.Trim();
            if (!EnsureReportFileClosedBeforeRun(folder, lot))
            {
                return false;
            }

            // ตรวจซ้ำด้วย LOT/Folder ล่าสุดก่อนเริ่ม Run เสมอ
            _snCheckTimer.Stop();
            CheckSNWhileTyping();

            if (_isDuplicateSN)
            {
                string rowInfo = _duplicateSNRow > 0 ? $"\nตำแหน่งแถว: {_duplicateSNRow}" : "";
                var promptResult = MessageBox.Show(
                    $"พบ S/N '{currentSN}' อยู่ในระบบแล้ว{rowInfo}\n\nต้องการทดสอบและบันทึกทับผลเดิมหรือไม่?",
                    "ยืนยันการบันทึกทับ S/N",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (promptResult == DialogResult.No)
                {
                    txt_SN.Focus();
                    txt_SN.SelectAll();
                    return false;
                }
            }

            return true;
        }

        public bool SaveTestRecordToExcel(Ph08TestRecord record)
        {
            var ctx = new ExcelReportSaveContext
            {
                SN = txt_SN.Text.Trim(),
                Lot = txt_Lot.Text.Trim(),
                ECN = txt_ECN.Text.Trim(),
                FW = txt_FW.Text.Trim(),
                Rev = txt_Rev.Text.Trim(),
                Folder = txt_folder.Text.Trim(),
                TesterName = txt_name.Text.Trim(),
                Record = record
            };
            return _excelService.SaveTestRecord(ctx, msg => LogMessage(msg));
        }

        public string IncrementSerialNumber(string currentSN) => ExcelReportService.IncrementSerialNumber(currentSN);

        public string DecrementSerialNumber(string currentSN) => ExcelReportService.DecrementSerialNumber(currentSN);

        public bool EnsureReportFileClosedBeforeRun(string outputFolder, string lotName)
        {
            if (string.IsNullOrWhiteSpace(outputFolder) ||
                !Directory.Exists(outputFolder) ||
                string.IsNullOrWhiteSpace(lotName))
            {
                return true;
            }

            string reportPath = ExcelReportService.GetReportFilePath(outputFolder, lotName);
            if (!File.Exists(reportPath))
            {
                return true;
            }

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
                    "กรุณาปิดไฟล์ Excel เดิมก่อน แล้วกด Test ใหม่อีกครั้ง",
                    "กรุณาปิดไฟล์ Excel",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    this,
                    $"ไม่สามารถเข้าถึงไฟล์รายงานได้\n\n" +
                    $"ไฟล์: {Path.GetFileName(reportPath)}\n\n" +
                    "กรุณาตรวจสอบสิทธิ์ของไฟล์ หรือปิดโปรแกรมที่กำลังใช้งานไฟล์นี้ แล้วลองใหม่อีกครั้ง",
                    "ไม่สามารถเปิดไฟล์รายงาน",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
        }
    }
}
