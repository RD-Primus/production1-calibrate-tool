// =========================================================================================
// [DEVELOPER GUIDE]
// Form1.Barcode.cs: "ระบบดักจับและประมวลผลข้อความจาก Barcode Scanner / QR Code"
// - ดักจับ Event KeyDown และ TextChanged ในช่อง txt_SN, txt_Lot, txt_ECN
// - สกัดค่า Model, LOT, ECN, และ S/N จากข้อความบาร์โค้ดรวมที่คั่นด้วยเครื่องหมายจุลภาค (Comma)
// - มีระบบ Debounce Timer ป้องกันการสแกนเบิ้ล หรือการ Paste ข้อมูลด้วยเมาส์
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TEST_DEF_03N
{
    public partial class Form1
    {
        private bool _isProcessingBarcode = false;
        private bool _isFirstChar = true;
        private System.Windows.Forms.Timer? _barcodeDebounceTimer;

        private void InitializeBarcodeScannerFeature()
        {
            txt_SN.KeyDown += txt_SN_KeyDown;
            txt_SN.KeyPress += txt_SN_KeyPress;
            txt_Lot.KeyDown += txt_Lot_KeyDown;
            txt_ECN.KeyDown += txt_ECN_KeyDown;

            // Timer สำหรับกรณี Paste ข้อมูลด้วยเมาส์หรือ Ctrl+V โดยไม่ได้กด Enter
            _barcodeDebounceTimer = new System.Windows.Forms.Timer();
            _barcodeDebounceTimer.Interval = 300; // รอ 300ms หลังสแกน/วางเสร็จ
            _barcodeDebounceTimer.Tick += (s, e) =>
            {
                _barcodeDebounceTimer.Stop();
                if (_isProcessingBarcode) return;

                if (txt_SN.Text.Contains(',') && TryProcessCombinedBarcode(txt_SN.Text, txt_SN)) return;
                if (txt_Lot.Text.Contains(',') && TryProcessCombinedBarcode(txt_Lot.Text, txt_Lot)) return;
                if (txt_ECN.Text.Contains(',') && TryProcessCombinedBarcode(txt_ECN.Text, txt_ECN)) return;
            };

            txt_SN.TextChanged += OnBarcodeBoxTextChanged;
            txt_Lot.TextChanged += OnBarcodeBoxTextChanged;
            txt_ECN.TextChanged += OnBarcodeBoxTextChanged;

            // เมื่อเข้าสู่กล่องข้อความ ให้เลือกข้อความทั้งหมดเพื่อพร้อมสแกนทับ
            txt_SN.Enter += (s, e) => BeginInvoke(new Action(() => txt_SN.SelectAll()));
            txt_Lot.Enter += (s, e) => BeginInvoke(new Action(() => txt_Lot.SelectAll()));
            txt_ECN.Enter += (s, e) => BeginInvoke(new Action(() => txt_ECN.SelectAll()));
        }

        private void OnBarcodeBoxTextChanged(object? sender, EventArgs e)
        {
            if (_isProcessingBarcode) return;
            if (sender is CustomTextBox tb && tb.Text.Contains(','))
            {
                _barcodeDebounceTimer?.Stop();
                _barcodeDebounceTimer?.Start();
            }
        }

        private void txt_SN_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _barcodeDebounceTimer?.Stop();
                _isFirstChar = true;

                if (TryProcessCombinedBarcode(txt_SN.Text, txt_SN))
                    return;

                // กรณีสแกนบาร์โค้ดปกติ 1D ที่มีเฉพาะ S/N เพียงอย่างเดียว (ไม่มี comma)
                _snCheckTimer?.Stop();
                CheckSNWhileTyping();

                if (!string.IsNullOrWhiteSpace(txt_SN.Text) && !txt_SN.Text.Trim().All(char.IsDigit))
                {
                    LogToRx($"⚠️ S/N '{txt_SN.Text.Trim()}' มีตัวหนังสือ ไม่ใช่ตัวเลข!", Color.OrangeRed);
                }

                txt_SN.SelectAll();
            }
        }

        private void txt_SN_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (_isProcessingBarcode) return;
            if (_isFirstChar && e.KeyChar != (char)Keys.Back && e.KeyChar != (char)Keys.Enter)
            {
                txt_SN.Clear();
                _isFirstChar = false;
            }
        }

        private void txt_Lot_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _barcodeDebounceTimer?.Stop();
                if (TryProcessCombinedBarcode(txt_Lot.Text, txt_Lot))
                    return;

                txt_Lot.SelectAll();
            }
        }

        private void txt_ECN_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _barcodeDebounceTimer?.Stop();
                if (TryProcessCombinedBarcode(txt_ECN.Text, txt_ECN))
                    return;

                txt_ECN.SelectAll();
            }
        }

        private bool TryProcessCombinedBarcode(string rawText, CustomTextBox? sourceBox = null)
        {
            if (_isProcessingBarcode || string.IsNullOrWhiteSpace(rawText))
                return false;

            string text = rawText.Trim();
            if (!text.Contains(','))
                return false;

            string[] parts = text.Split(new[] { ',' }, StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
                return false;

            string rawModel = "";
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

            // หากไม่พบคีย์เวิร์ดเฉพาะ ให้จับคู่ตามลำดับมาตรฐาน
            // กรณี 4 ส่วนขึ้นไป: 0:Model, 1:ECN, 2:Lot, 3:SN
            if (parts.Length >= 4)
            {
                rawModel = parts[0];
                if (string.IsNullOrEmpty(rawEcn)) rawEcn = parts[1];
                if (string.IsNullOrEmpty(rawLot)) rawLot = parts[2];
                if (string.IsNullOrEmpty(rawSn)) rawSn = parts[3];
            }
            else if (parts.Length == 3)
            {
                // กรณี 3 ส่วน: 0:ECN, 1:Lot, 2:SN
                if (string.IsNullOrEmpty(rawEcn)) rawEcn = parts[0];
                if (string.IsNullOrEmpty(rawLot)) rawLot = parts[1];
                if (string.IsNullOrEmpty(rawSn)) rawSn = parts[2];
            }

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
                _snCheckTimer?.Stop();

                txt_ECN.Text = cleanEcn;
                txt_Lot.Text = cleanLot;
                txt_SN.Text = cleanSn;

                // ตรวจสอบ Model ถ้ามีการระบุใน QR Code และตรงกับ Model ที่รองรับ
                if (!string.IsNullOrWhiteSpace(rawModel))
                {
                    string m = rawModel.Trim();
                    if (m.IndexOf("A-B", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (txt_model.Items.Contains(ModelAB))
                            txt_model.SelectedItem = ModelAB;
                    }
                    else if (m.IndexOf("DEF-03N-A", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             m.IndexOf("Type A", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (txt_model.Items.Contains(ModelA))
                            txt_model.SelectedItem = ModelA;
                    }
                }

                // บันทึก config ลง path_config.txt
                SaveConfig();

                _isFirstChar = true;

                // ตรวจสอบความถูกต้องของ SN ซ้ำ และแสดงสีสถานะ
                CheckSNWhileTyping();

                if (_isDuplicateSN)
                {
                    LogToRx($"⚠️ S/N: {cleanSn} ซ้ำในระบบ! (ตำแหน่งแถว {_duplicateSNRow})", Color.Orange);
                }

                // โฟกัสกลับที่ช่อง S/N พร้อมคลุมดำ เพื่อให้พร้อมสแกนตัวถัดไปได้ทันที
                CustomTextBox targetBox = sourceBox ?? txt_SN;
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
