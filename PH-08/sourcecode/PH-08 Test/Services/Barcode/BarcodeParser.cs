// =========================================================================================
// [DEVELOPER GUIDE]
// BarcodeParser.cs: "ระบบแยกข้อความและตรวจสอบรูปแบบ QR Code / Barcode"
// - แกะสตริงดิบที่ได้จากการยิงบาร์โค้ด เช่น "PH-08,ECN:...,(LOT123),SN123456" หรือ "ECN:...,(LOT123),SN123456"
// - สกัดค่า Model, ECN, Lot, Serial Number (S/N) รูปแบบเดียวกับ DEF-03N
// - ตัดวงเล็บครอบและ Prefix ทั่วไป (เช่น "S/N:", "LOT:", "ECN NO:")
// - ทำงานแบบ Pure Logic (ไม่มี UI Dependencies) สามารถนำไปเขียน Unit Test ได้ทันที
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;
using System.Text.RegularExpressions;

namespace PH_08_Test.Services.Barcode
{
    public class BarcodeParseResult
    {
        public bool IsSuccess { get; set; }
        public string Model { get; set; } = "";
        public string Ecn { get; set; } = "";
        public string Lot { get; set; } = "";
        public string Sn { get; set; } = "";
    }

    public static class BarcodeParser
    {
        public static bool TryParse(string rawText, out BarcodeParseResult result)
        {
            result = new BarcodeParseResult();
            if (string.IsNullOrWhiteSpace(rawText))
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

            // ตรวจหาด้วย Pattern ของ ECN และ LOT ก่อนเพื่อความแม่นยำสูงสุด (เหมือน DEF-03N)
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i];
                if (Regex.IsMatch(p, @"^ECN\s*NO\s*:", RegexOptions.IgnoreCase) ||
                    p.StartsWith("RD-CN", StringComparison.OrdinalIgnoreCase) ||
                    p.StartsWith("ECN", StringComparison.OrdinalIgnoreCase))
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
                // กรณี 3 ส่วน: 0:ECN, 1:Lot, 2:SN (เหมือน DEF-03N)
                if (string.IsNullOrEmpty(rawEcn)) rawEcn = parts[0];
                if (string.IsNullOrEmpty(rawLot)) rawLot = parts[1];
                if (string.IsNullOrEmpty(rawSn)) rawSn = parts[2];
            }

            // 1. ECN: ตัด "ECN NO:" หรือ "ECN NO :" ออก
            string cleanEcn = Regex.Replace(rawEcn, @"^ECN\s*NO\s*:\s*", "", RegexOptions.IgnoreCase).Trim();
            cleanEcn = Regex.Replace(cleanEcn, @"^ECN\s*:\s*", "", RegexOptions.IgnoreCase).Trim();

            // 2. LOT: ตัดวงเล็บ () ออก
            string cleanLot = Regex.Replace(rawLot, @"[()]", "").Trim();
            cleanLot = Regex.Replace(cleanLot, @"^LOT\s*:\s*", "", RegexOptions.IgnoreCase).Trim();

            // 3. S/N: ดึงค่า S/N
            string cleanSn = Regex.Replace(rawSn.Trim(), @"^(S/N|SN)\s*:\s*", "", RegexOptions.IgnoreCase).Trim();

            result.Model = CleanValue(rawModel);
            result.Ecn = cleanEcn;
            result.Lot = cleanLot;
            result.Sn = cleanSn;
            result.IsSuccess = !string.IsNullOrWhiteSpace(result.Ecn) &&
                               !string.IsNullOrWhiteSpace(result.Lot) &&
                               !string.IsNullOrWhiteSpace(result.Sn);

            return result.IsSuccess;
        }

        public static string CleanValue(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return "";
            string res = val.Trim();

            // ลบวงเล็บครอบ เช่น (123) -> 123
            res = Regex.Replace(res, @"[()]", "").Trim();

            // ลบ Prefix ทั่วไป
            res = Regex.Replace(res, @"^(S/N|SN)\s*:\s*", "", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"^LOT\s*:\s*", "", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"^ECN\s*NO\s*:\s*", "", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"^ECN\s*:\s*", "", RegexOptions.IgnoreCase);

            return res.Trim();
        }
    }
}
