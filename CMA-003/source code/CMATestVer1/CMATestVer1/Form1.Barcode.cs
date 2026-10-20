using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMATestVer1
{
    public partial class Form1
    {
        private bool _isProcessingBarcode = false;

        private System.Windows.Forms.Timer? _barcodeDebounceTimer;

        private void InitializeBarcodeScannerFeature()
        {
            txtLot.KeyDown += txtLot_KeyDown;
            txtECN.KeyDown += txtECN_KeyDown;

            // Timer สำหรับกรณี Paste ข้อมูลด้วยเมาส์หรือ Ctrl+V โดยไม่ได้กด Enter
            _barcodeDebounceTimer = new System.Windows.Forms.Timer();
            _barcodeDebounceTimer.Interval = 300; // รอ 300ms หลังสแกน/วางเสร็จ
            _barcodeDebounceTimer.Tick += (s, e) =>
            {
                _barcodeDebounceTimer.Stop();
                if (_isProcessingBarcode) return;

                if (txtSerialNumber.Text.Contains(',') && TryProcessCombinedBarcode(txtSerialNumber.Text, txtSerialNumber)) return;
                if (txtLot.Text.Contains(',') && TryProcessCombinedBarcode(txtLot.Text, txtLot)) return;
                if (txtECN.Text.Contains(',') && TryProcessCombinedBarcode(txtECN.Text, txtECN)) return;
            };

            txtSerialNumber.TextChanged += OnBarcodeBoxTextChanged;
            txtLot.TextChanged += OnBarcodeBoxTextChanged;
            txtECN.TextChanged += OnBarcodeBoxTextChanged;

            // เมื่อเข้าสู่กล่องข้อความ ให้เลือกข้อความทั้งหมดเพื่อพร้อมสแกนทับ
            txtSerialNumber.Enter += (s, e) => BeginInvoke(new Action(() => txtSerialNumber.SelectAll()));
            txtLot.Enter += (s, e) => BeginInvoke(new Action(() => txtLot.SelectAll()));
            txtECN.Enter += (s, e) => BeginInvoke(new Action(() => txtECN.SelectAll()));
        }

        private void OnBarcodeBoxTextChanged(object? sender, EventArgs e)
        {
            if (_isProcessingBarcode) return;
            if (sender is TextBox tb && tb.Text.Contains(','))
            {
                _barcodeDebounceTimer?.Stop();
                _barcodeDebounceTimer?.Start();
            }
        }

        private void txtLot_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _barcodeDebounceTimer?.Stop();
                if (TryProcessCombinedBarcode(txtLot.Text, txtLot))
                    return;

                txtLot.SelectAll();
            }
        }

        private void txtECN_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _barcodeDebounceTimer?.Stop();
                if (TryProcessCombinedBarcode(txtECN.Text, txtECN))
                    return;

                txtECN.SelectAll();
            }
        }

        private bool TryProcessCombinedBarcode(string rawText, TextBox? sourceBox = null)
        {
            if (_isProcessingBarcode || string.IsNullOrWhiteSpace(rawText))
                return false;

            string text = rawText.Trim();
            if (!text.Contains(','))
                return false;

            string[] parts = text.Split(new[] { ',' }, StringSplitOptions.TrimEntries);
            if (parts.Length < 4)
                return false;

            // Barcode CMA ตัวอย่าง: FACP1CMA030000,ECN NO:RD-CN-26-09-093,(Assy/2608/1663),69090675
            string rawEcn = "";
            string rawLot = "";
            string rawSn = "";

            // ตรวจหาด้วย Pattern ของ ECN และ LOT ก่อนเพื่อความแม่นยำสูงสุด
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i];
                if (Regex.IsMatch(p, @"^ECN\s*NO\s*:", RegexOptions.IgnoreCase) || p.StartsWith("RD-CN", StringComparison.OrdinalIgnoreCase))
                {
                    rawEcn = p;
                }
                else if (p.StartsWith("(") && p.EndsWith(")"))
                {
                    rawLot = p;
                }
            }

            // หากไม่พบคีย์เวิร์ดเฉพาะ ให้จับคู่ตามลำดับมาตรฐาน 0:Model, 1:ECN, 2:Lot, 3:SN
            if (string.IsNullOrEmpty(rawEcn) && parts.Length > 1) rawEcn = parts[1];
            if (string.IsNullOrEmpty(rawLot) && parts.Length > 2) rawLot = parts[2];
            if (string.IsNullOrEmpty(rawSn) && parts.Length > 3) rawSn = parts[3];

            // 1. ECN: ตัด "ECN NO:" หรือ "ECN NO :" ออก
            string cleanEcn = Regex.Replace(rawEcn, @"^ECN\s*NO\s*:\s*", "", RegexOptions.IgnoreCase).Trim();

            // 2. LOT: ตัดวงเล็บ () ออก
            string cleanLot = Regex.Replace(rawLot, @"[()]", "").Trim();

            // 3. S/N: ดึงค่า S/N
            string cleanSn = rawSn.Trim();

            if (string.IsNullOrWhiteSpace(cleanEcn) || string.IsNullOrWhiteSpace(cleanLot) || string.IsNullOrWhiteSpace(cleanSn))
                return false;

            _isProcessingBarcode = true;
            try
            {
                _snDebounceTimer?.Stop();

                txtECN.Text = cleanEcn;
                Properties.Settings.Default.SavedECN = cleanEcn;

                txtLot.Text = cleanLot;
                Properties.Settings.Default.SavedLot = cleanLot;

                txtSerialNumber.Text = cleanSn;

                Properties.Settings.Default.Save();

                _isFirstChar = true;

                // อัปเดต Path ไฟล์ Excel ตาม Lot ใหม่
                string safeFileName = cleanLot.Replace("/", "-");
                _excelFilePath = Path.Combine(ResultFolder ?? "", safeFileName + ".xlsx");
                txtExcelPath.Text = _excelFilePath;

                // ตรวจสอบความถูกต้องของ SN ซ้ำ และแสดงสีสถานะ (เหมือน DEF-03N)
                if (!string.IsNullOrEmpty(ResultFolder) && File.Exists(_excelFilePath) && IsSerialNumberDuplicate(cleanSn, cleanLot, out int dupRow))
                {
                    _isDuplicateSN = true;
                    _duplicateSNRow = dupRow;
                    txtSerialNumber.ForeColor = Color.White;
                    txtSerialNumber.BackColor = Color.Firebrick;
                    LogToRx($"⚠️S/N:{cleanSn} ซ้ำในระบบ! (ตำแหน่งแถว {dupRow})", Color.Orange);
                }
                else
                {
                    _isDuplicateSN = false;
                    _duplicateSNRow = -1;
                    txtSerialNumber.ForeColor = Color.White;
                    txtSerialNumber.BackColor = Color.SeaGreen;
                }

                // ไม่โฟกัสที่ปุ่ม Run แต่ให้อยู่ที่ TextBox เดิม พร้อมเลือกข้อความทั้งหมด
                TextBox targetBox = sourceBox ?? txtSerialNumber;
                targetBox.Focus();
                targetBox.SelectAll();
                return true;
            }
            finally
            {
                _isProcessingBarcode = false;
            }
        }
    }
}
