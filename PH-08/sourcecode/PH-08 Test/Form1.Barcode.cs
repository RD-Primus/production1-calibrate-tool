// =========================================================================================
// [DEVELOPER GUIDE]
// Form1.Barcode.cs: "ส่วนควบคุมการรับค่าจากบาร์โค้ดสแกนเนอร์บนหน้าจอ UI"
// - ดักจับ Event KeyDown / KeyPress เมื่อยิง QR/Barcode ลงในช่อง txt_SN, txt_Lot, txt_ECN
// - ส่งข้อความดิบไปให้ BarcodeParser.TryParse(...) ประมวลผลและแยกข้อมูล (รูปแบบเดียวกับ DEF-03N)
// - หากต้องการแก้ไข "Pattern หรือรูปแบบข้อความใน QR" -> ไปที่ Services/Barcode/BarcodeParser.cs
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PH_08_Test.Services.Barcode;

namespace PH_08_Test
{
    public partial class Form1
    {
        private bool _isProcessingBarcode = false;
        private bool _isFirstChar = true;
        private System.Windows.Forms.Timer? _barcodeDebounceTimer;

        private void InitBarcodeControls()
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
                    LogMessage($"⚠️ S/N '{txt_SN.Text.Trim()}' มีตัวหนังสือ ไม่ใช่ตัวเลข!", Color.OrangeRed);
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

            if (!BarcodeParser.TryParse(rawText, out var parsed))
                return false;

            string cleanEcn = parsed.Ecn;
            string cleanLot = parsed.Lot;
            string cleanSn = parsed.Sn;

            if (string.IsNullOrWhiteSpace(cleanEcn) || string.IsNullOrWhiteSpace(cleanLot) || string.IsNullOrWhiteSpace(cleanSn))
                return false;

            _isProcessingBarcode = true;
            try
            {
                _snCheckTimer?.Stop();

                txt_ECN.Text = cleanEcn;
                txt_Lot.Text = cleanLot;
                txt_SN.Text = cleanSn;

                // บันทึก config ลง app_config.txt
                SaveAppConfig();

                _isFirstChar = true;

                // ตรวจสอบความถูกต้องของ SN ซ้ำ และแสดงสีสถานะ
                CheckSNWhileTyping();

                if (_isDuplicateSN)
                {
                    LogMessage($"⚠️ S/N: {cleanSn} ซ้ำในระบบ! (ตำแหน่งแถว {_duplicateSNRow})", Color.Orange);
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
