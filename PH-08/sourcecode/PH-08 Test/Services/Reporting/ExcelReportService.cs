// =========================================================================================
// [DEVELOPER GUIDE]
// ExcelReportService.cs: "บริการจัดการไฟล์รายงาน Excel (ClosedXML)"
// - บันทึกผลการทดสอบลงไฟล์เทมเพลต PH-08 QC Inspection.xlsx ({lot}.xlsx)
// - ตรวจจับหมายเลข S/N ซ้ำ (CheckDuplicateSN)
// - อัปเดตข้อมูลหัวกระดาษ (Lot, ECN, FW, Rev, Date)
// - จัดการต่อหน้าอัตโนมัติ (Pagination / Auto-expand Rows) เมื่อข้อมูลเกิน 46 แถว/หน้า
// - สรุปยอดรวมท้ายตาราง (Actual, Good, Defect, % Yield, Operator)
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace PH_08_Test.Services.Reporting
{
    public class ExcelReportSaveContext
    {
        public string SN { get; set; } = "";
        public string Lot { get; set; } = "";
        public string ECN { get; set; } = "";
        public string FW { get; set; } = "";
        public string Rev { get; set; } = "";
        public string Folder { get; set; } = "";
        public string TesterName { get; set; } = "";
        public Ph08TestRecord Record { get; set; } = new Ph08TestRecord();
    }

    public class ExcelReportService
    {
        private const int FirstDataRow = 7;
        private const int LastColumn = 16;
        private const int FinalPageDataRows = 46;

        public struct ExcelTableColumnMap
        {
            public int StartDataRow;
            public int ColNo;
            public int ColRemote;
            public int ColLocal;
            public int ColLED;
            public int ColSwitchID;
            public int ColPass;
            public int ColFail;
            public int ColSN;
        }

        public string FindTemplateFilePath()
        {
            string appDir = Application.StartupPath;
            string t1 = Path.Combine(appDir, "PH-08 QC Inspection.xlsx");
            if (File.Exists(t1)) return t1;

            string t2 = Path.Combine(appDir, "templet ph-08.xlsx");
            if (File.Exists(t2)) return t2;

            try
            {
                var dir = new DirectoryInfo(appDir);
                for (int i = 0; i < 4 && dir != null; i++)
                {
                    string candidate1 = Path.Combine(dir.FullName, "PH-08 QC Inspection.xlsx");
                    if (File.Exists(candidate1)) return candidate1;

                    string candidate2 = Path.Combine(dir.FullName, "templet ph-08.xlsx");
                    if (File.Exists(candidate2)) return candidate2;

                    dir = dir.Parent;
                }
            }
            catch { }

            return "";
        }

        public static string GetSafeLotFileName(string lotName)
        {
            if (string.IsNullOrWhiteSpace(lotName)) return string.Empty;
            string safe = lotName.Replace("/", "-").Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (c != '-') safe = safe.Replace(c, '-');
            }
            return safe;
        }

        public static string GetReportFilePath(string outputFolder, string lotName)
        {
            string folder = string.IsNullOrWhiteSpace(outputFolder)
                ? AppDomain.CurrentDomain.BaseDirectory
                : outputFolder.Trim().Trim('"', '\'');
            string safeLot = GetSafeLotFileName(lotName);
            return Path.Combine(folder, $"{safeLot}.xlsx");
        }

        public bool CheckDuplicateSN(string folder, string lot, string sn)
        {
            return CheckDuplicateSN(folder, lot, sn, out _);
        }

        public bool CheckDuplicateSN(string folder, string lot, string sn, out int duplicateRow)
        {
            duplicateRow = -1;
            if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(lot) || string.IsNullOrWhiteSpace(sn))
                return false;

            string reportPath = GetReportFilePath(folder, lot);
            if (!File.Exists(reportPath))
                return false;

            try
            {
                using var snapshot = new MemoryStream();
                using (var source = new FileStream(reportPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    source.CopyTo(snapshot);
                }
                snapshot.Position = 0;
                using var workbook = new XLWorkbook(snapshot);
                var sheet = workbook.Worksheet(1);
                var colMap = FindTableColumns(sheet);

                if (colMap.ColSN > 0 && colMap.StartDataRow > 0)
                {
                    int lastUsed = sheet.LastRowUsed()?.RowNumber() ?? 100;
                    for (int r = colMap.StartDataRow; r <= lastUsed; r++)
                    {
                        if (!IsDataRow(sheet, r, colMap)) continue;

                        string existingSN = sheet.Cell(r, colMap.ColSN).GetString().Trim();
                        if (IsValidSerialNumber(existingSN) && existingSN.Equals(sn, StringComparison.OrdinalIgnoreCase))
                        {
                            duplicateRow = r;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Ignored
            }

            return false;
        }

        public int GetTestedSerialCount(string folder, string lot)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder) || string.IsNullOrWhiteSpace(lot))
                return 0;

            string reportPath = GetReportFilePath(folder, lot);
            if (!File.Exists(reportPath))
                return 0;

            try
            {
                using var snapshot = new MemoryStream();
                using (var source = new FileStream(reportPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    source.CopyTo(snapshot);
                }
                snapshot.Position = 0;
                using var workbook = new XLWorkbook(snapshot);
                var sheet = workbook.Worksheet(1);
                var colMap = FindTableColumns(sheet);

                int count = 0;
                if (colMap.ColSN > 0 && colMap.StartDataRow > 0)
                {
                    int lastUsed = sheet.LastRowUsed()?.RowNumber() ?? 100;
                    for (int r = colMap.StartDataRow; r <= lastUsed; r++)
                    {
                        if (!IsDataRow(sheet, r, colMap)) continue;
                        string existingSN = sheet.Cell(r, colMap.ColSN).GetString().Trim();
                        if (IsValidSerialNumber(existingSN))
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
            catch
            {
                return 0;
            }
        }

        public bool SaveTestRecord(ExcelReportSaveContext ctx, Action<string>? logAction = null)
        {
            string sn = ctx.SN.Trim();
            string lot = ctx.Lot.Trim();
            string ecn = ctx.ECN.Trim();
            string fw = ctx.FW.Trim();
            string rev = ctx.Rev.Trim();
            string folder = ctx.Folder.Trim();
            var record = ctx.Record;

            if (string.IsNullOrWhiteSpace(sn) || string.IsNullOrWhiteSpace(lot) || string.IsNullOrWhiteSpace(folder))
            {
                logAction?.Invoke("✖ [Excel] ข้อมูลไม่ครบถ้วน ไม่สามารถบันทึกรายงาน Excel ได้");
                return false;
            }

            try
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string destFilePath = GetReportFilePath(folder, lot);
                string fileName = Path.GetFileName(destFilePath);

                // Check template file
                if (!File.Exists(destFilePath))
                {
                    string templatePath = FindTemplateFilePath();
                    if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                    {
                        MessageBox.Show("ไม่พบไฟล์ต้นแบบ 'PH-08 QC Inspection.xlsx' ในระบบ\nกรุณาตรวจสอบไฟล์เทมเพลตในโฟลเดอร์โปรแกรม", "ไม่พบเทมเพลต", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        logAction?.Invoke("✖ [Excel] ไม่พบไฟล์เทมเพลต 'PH-08 QC Inspection.xlsx'");
                        return false;
                    }

                    File.Copy(templatePath, destFilePath, true);
                    logAction?.Invoke($"📄 สร้างไฟล์รายงานใหม่: {fileName}");
                }

                using var workbook = new XLWorkbook(destFilePath);
                var sheet = workbook.Worksheet(1);

                // ปลดล็อคแผ่นงานชั่วคราวเพื่อเขียนข้อมูล
                UnprotectSheetSafely(sheet);

                // 1. Update Header by searching cell names
                UpdateExcelHeaderInfo(sheet, lot, ecn, fw, rev);

                // 2. Find Table Columns
                var colMap = FindTableColumns(sheet);
                if (colMap.ColSN == -1 || colMap.StartDataRow == -1)
                {
                    MessageBox.Show("ไม่พบคอลัมน์ตารางที่ต้องการในไฟล์เทมเพลต", "ข้อผิดพลาดเทมเพลต", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // 3. Find Target Row (Auto-expands to next page if full)
                int targetRow = FindTargetRow(sheet, colMap, sn, out int duplicateRow);

                // 4. Sequence number
                int sequenceNumber = 1;
                for (int r = colMap.StartDataRow; r < targetRow; r++)
                {
                    if (IsDataRow(sheet, r, colMap))
                    {
                        sequenceNumber++;
                    }
                }
                if (colMap.ColNo > 0)
                {
                    var noCell = sheet.Cell(targetRow, colMap.ColNo);
                    noCell.Value = sequenceNumber;
                    noCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    noCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                bool p1Ran = !string.IsNullOrEmpty(record.ReadSystemStatus);
                bool p2Ran = !string.IsNullOrEmpty(record.DeviceTriggerStatus);
                bool p3Ran = record.LedTestPassed || !string.IsNullOrEmpty(record.LedProblemSummary);
                bool p4Ran = !string.IsNullOrEmpty(record.SwitchTestStatus);

                // Write ColRemote (Col B)
                if (colMap.ColRemote > 0)
                {
                    var c = sheet.Cell(targetRow, colMap.ColRemote);
                    c.Value = !p1Ran ? "-" : (record.Phase1_PH08Passed ? "✔" : "✘");
                    c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // Write ColLocal (Col D)
                if (colMap.ColLocal > 0)
                {
                    var c = sheet.Cell(targetRow, colMap.ColLocal);
                    c.Value = !p2Ran ? "-" : (record.Phase2_DevicePassed ? "✔" : "✘");
                    c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // Write ColLED (Col F)
                if (colMap.ColLED > 0)
                {
                    var c = sheet.Cell(targetRow, colMap.ColLED);
                    c.Value = !p3Ran ? "-" : (record.Phase3_LEDPassed ? "✔" : "✘");
                    c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // Write ColSwitchID (Col J)
                if (colMap.ColSwitchID > 0)
                {
                    var c = sheet.Cell(targetRow, colMap.ColSwitchID);
                    c.Value = !p4Ran ? "-" : (record.Phase4_SwitchPassed ? "✔" : "✘");
                    c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // Total Pass/Fail Columns
                bool overall = record.OverallPassed;
                if (colMap.ColPass > 0)
                {
                    var c = sheet.Cell(targetRow, colMap.ColPass);
                    c.Value = overall ? "✔" : "";
                    c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                if (colMap.ColFail > 0)
                {
                    var c = sheet.Cell(targetRow, colMap.ColFail);
                    c.Value = overall ? "" : "✔";
                    c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // S/N Column
                if (colMap.ColSN > 0)
                {
                    var c = sheet.Cell(targetRow, colMap.ColSN);
                    c.Value = sn;
                    c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // 5. Update summary
                int summaryRow = FindSummaryHeaderRow(sheet, colMap.StartDataRow);
                if (summaryRow > 0)
                {
                    UpdateSummaryCalculation(sheet, colMap, summaryRow, ctx.TesterName);
                }

                // 6. ล็อคแผ่นงานป้องกันการแก้ไขผลการทดสอบ
                ProtectSheet(sheet);

                workbook.Save();
                if (duplicateRow != -1)
                {
                    logAction?.Invoke($"📝 บันทึกทับผลเดิมที่แถว {targetRow} ในไฟล์ {fileName} เรียบร้อยแล้ว");
                }
                else
                {
                    logAction?.Invoke($"💾 บันทึกผลการทดสอบลงแถวที่ {targetRow} ในไฟล์ {fileName} เรียบร้อยแล้ว");
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการบันทึกไฟล์ Excel:\n{ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                logAction?.Invoke($"✖ [Excel Error] {ex.Message}");
                return false;
            }
        }

        public void UpdateExcelHeaderInfo(IXLWorksheet sheet, string lot, string ecn, string fw, string rev)
        {
            var now = DateTime.Now;
            int buddhistYear2Digit = (now.Year + 543) % 100;
            string thaiDateStr = $"{now.Day:00}/{now.Month:00}/{buddhistYear2Digit:00}";

            for (int r = 1; r <= 10; r++)
            {
                for (int c = 1; c <= 20; c++)
                {
                    string cellText = sheet.Cell(r, c).GetString().Trim();
                    if (string.IsNullOrEmpty(cellText)) continue;

                    if (cellText.StartsWith("Lot", StringComparison.OrdinalIgnoreCase) &&
                        !cellText.StartsWith("Lot Size", StringComparison.OrdinalIgnoreCase))
                    {
                        sheet.Cell(r, c).Value = "Lot :";
                        sheet.Cell(r, c + 1).Value = lot;
                    }
                    else if (cellText.StartsWith("FW", StringComparison.OrdinalIgnoreCase))
                    {
                        sheet.Cell(r, c).Value = string.IsNullOrWhiteSpace(fw) ? "FW:" : $"FW: {fw}";
                    }
                    else if (cellText.StartsWith("ECN", StringComparison.OrdinalIgnoreCase))
                    {
                        sheet.Cell(r, c).Value = string.IsNullOrWhiteSpace(ecn) ? "ECN NO:" : $"ECN NO: {ecn}";
                    }
                    else if (cellText.StartsWith("Rev", StringComparison.OrdinalIgnoreCase) ||
                             cellText.StartsWith("Revision", StringComparison.OrdinalIgnoreCase))
                    {
                        sheet.Cell(r, c).Value = string.IsNullOrWhiteSpace(rev) ? "Rev:" : $"Rev: {rev}";
                    }
                    else if (cellText.StartsWith("Date", StringComparison.OrdinalIgnoreCase))
                    {
                        sheet.Cell(r, c).Value = $"Date : {thaiDateStr}";
                    }
                }
            }
        }

        public ExcelTableColumnMap FindTableColumns(IXLWorksheet sheet)
        {
            var map = new ExcelTableColumnMap
            {
                StartDataRow = -1,
                ColNo = -1,
                ColRemote = -1,
                ColLocal = -1,
                ColLED = -1,
                ColSwitchID = -1,
                ColPass = -1,
                ColFail = -1,
                ColSN = -1
            };

            int maxHeaderRow = -1;

            for (int r = 4; r <= 8; r++)
            {
                for (int c = 1; c <= 20; c++)
                {
                    string text = sheet.Cell(r, c).GetString().Trim();
                    if (string.IsNullOrEmpty(text)) continue;

                    if (text.Equals("No", StringComparison.OrdinalIgnoreCase) || text.Equals("No.", StringComparison.OrdinalIgnoreCase))
                    {
                        map.ColNo = c;
                        if (r > maxHeaderRow) maxHeaderRow = r;
                    }
                    else if (text.Contains("Remote", StringComparison.OrdinalIgnoreCase))
                    {
                        map.ColRemote = c;
                        if (r > maxHeaderRow) maxHeaderRow = r;
                    }
                    else if (text.Contains("Local", StringComparison.OrdinalIgnoreCase))
                    {
                        map.ColLocal = c;
                        if (r > maxHeaderRow) maxHeaderRow = r;
                    }
                    else if (text.Contains("ไฟแสดงผล") || text.Contains("LED", StringComparison.OrdinalIgnoreCase))
                    {
                        map.ColLED = c;
                        if (r > maxHeaderRow) maxHeaderRow = r;
                    }
                    else if (text.Contains("สลับเปลี่ยน ID") || text.Contains("Switch", StringComparison.OrdinalIgnoreCase))
                    {
                        map.ColSwitchID = c;
                        if (r > maxHeaderRow) maxHeaderRow = r;
                    }
                    else if (text.Equals("ปกติ", StringComparison.OrdinalIgnoreCase))
                    {
                        map.ColPass = c;
                        if (r > maxHeaderRow) maxHeaderRow = r;
                    }
                    else if (text.Equals("ไม่ปกติ", StringComparison.OrdinalIgnoreCase))
                    {
                        map.ColFail = c;
                        if (r > maxHeaderRow) maxHeaderRow = r;
                    }
                    else if (text.Equals("S/N", StringComparison.OrdinalIgnoreCase) || text.Equals("SN", StringComparison.OrdinalIgnoreCase))
                    {
                        map.ColSN = c;
                        if (r > maxHeaderRow) maxHeaderRow = r;
                    }
                }
            }

            if (maxHeaderRow > 0)
            {
                map.StartDataRow = maxHeaderRow + 1;
            }

            if (map.ColNo == -1) map.ColNo = 1;
            if (map.ColRemote == -1) map.ColRemote = 2;
            if (map.ColLocal == -1) map.ColLocal = 4;
            if (map.ColLED == -1) map.ColLED = 6;
            if (map.ColSwitchID == -1) map.ColSwitchID = 10;
            if (map.ColPass == -1) map.ColPass = 13;
            if (map.ColFail == -1) map.ColFail = 14;
            if (map.ColSN == -1) map.ColSN = 16;
            if (map.StartDataRow == -1) map.StartDataRow = 7;

            return map;
        }

        public static bool IsRevisionRow(IXLWorksheet sheet, int row)
        {
            for (int c = 1; c <= 20; c++)
            {
                string text = sheet.Cell(row, c).GetString().Trim();
                if (text.StartsWith("F-PD", StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith("Rev", StringComparison.OrdinalIgnoreCase) ||
                    Regex.IsMatch(text, @"^F-.*\bRev\.?", RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsSummaryRow(IXLWorksheet sheet, int row)
        {
            for (int c = 1; c <= 10; c++)
            {
                string text = sheet.Cell(row, c).GetString().Trim();
                if (text.StartsWith("Q'ty", StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith("Qty", StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith("Good", StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith("Defect", StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith("Operator", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsDataRow(IXLWorksheet sheet, int row, ExcelTableColumnMap colMap)
        {
            if (row < colMap.StartDataRow) return false;
            if (IsRevisionRow(sheet, row)) return false;
            if (IsSummaryRow(sheet, row)) return false;
            return true;
        }

        public static int FindFinalRevisionRow(IXLWorksheet sheet, int summaryHeaderRow)
        {
            for (int r = summaryHeaderRow - 1; r >= FirstDataRow; r--)
            {
                if (IsRevisionRow(sheet, r))
                {
                    return r;
                }
            }
            return summaryHeaderRow > FirstDataRow ? summaryHeaderRow - 1 : FirstDataRow + 46;
        }

        public static int ExpandDataRows(IXLWorksheet sheet, ExcelTableColumnMap colMap, int finalRevisionRow)
        {
            int templateRow = finalRevisionRow;

            int highestNumber = 0;
            for (int r = colMap.StartDataRow; r <= finalRevisionRow; r++)
            {
                if (!IsRevisionRow(sheet, r) && !IsSummaryRow(sheet, r))
                {
                    highestNumber++;
                }
            }

            int insertAt = finalRevisionRow + 1;
            sheet.Row(insertAt).InsertRowsAbove(FinalPageDataRows);

            for (int i = 0; i < FinalPageDataRows; i++)
            {
                int targetRow = insertAt + i;
                sheet.Row(targetRow).Height = sheet.Row(templateRow).Height;
                sheet.Range(templateRow, 1, templateRow, LastColumn).CopyTo(sheet.Cell(targetRow, 1));
                sheet.Range(targetRow, 1, targetRow, LastColumn).Clear(XLClearOptions.Contents);
                sheet.Cell(targetRow, colMap.ColNo).Value = highestNumber + i + 1;
            }

            return insertAt;
        }

        public static int FindTargetRow(IXLWorksheet sheet, ExcelTableColumnMap colMap, string sn, out int duplicateRow)
        {
            duplicateRow = -1;
            int summaryRow = FindSummaryHeaderRow(sheet, colMap.StartDataRow);
            if (summaryRow <= 0) summaryRow = sheet.LastRowUsed()?.RowNumber() ?? 54;

            int finalRevisionRow = FindFinalRevisionRow(sheet, summaryRow);

            int emptyRow = -1;
            for (int r = colMap.StartDataRow; r <= finalRevisionRow; r++)
            {
                if (IsRevisionRow(sheet, r) || IsSummaryRow(sheet, r)) continue;

                string existingSN = sheet.Cell(r, colMap.ColSN).GetString().Trim();
                if (IsValidSerialNumber(existingSN))
                {
                    if (existingSN.Equals(sn, StringComparison.OrdinalIgnoreCase))
                    {
                        duplicateRow = r;
                        return r;
                    }
                }
                else if (emptyRow == -1)
                {
                    emptyRow = r;
                }
            }

            if (emptyRow != -1)
            {
                return emptyRow;
            }

            return ExpandDataRows(sheet, colMap, finalRevisionRow);
        }

        public static int FindSummaryHeaderRow(IXLWorksheet sheet, int startRow)
        {
            int lastUsed = sheet.LastRowUsed()?.RowNumber() ?? 200;
            for (int r = startRow; r <= lastUsed + 20; r++)
            {
                for (int c = 1; c <= 10; c++)
                {
                    string text = sheet.Cell(r, c).GetString().Trim();
                    if (text.StartsWith("Q'ty", StringComparison.OrdinalIgnoreCase) ||
                        text.StartsWith("Qty", StringComparison.OrdinalIgnoreCase))
                    {
                        return r;
                    }
                }
            }
            return -1;
        }

        public static void UpdateSummaryCalculation(IXLWorksheet sheet, ExcelTableColumnMap colMap, int summaryHeaderRow, string testerName)
        {
            if (summaryHeaderRow <= 0) return;

            int actualCount = 0;
            int goodCount = 0;
            int defectCount = 0;

            for (int r = colMap.StartDataRow; r < summaryHeaderRow; r++)
            {
                if (!IsDataRow(sheet, r, colMap)) continue;

                string sn = sheet.Cell(r, colMap.ColSN).GetString().Trim();
                if (!IsValidSerialNumber(sn)) continue;

                actualCount++;
                string passVal = sheet.Cell(r, colMap.ColPass).GetString().Trim();
                string failVal = sheet.Cell(r, colMap.ColFail).GetString().Trim();

                if (passVal == "✔" || passVal.Equals("OK", StringComparison.OrdinalIgnoreCase) || passVal.Equals("PASS", StringComparison.OrdinalIgnoreCase))
                {
                    goodCount++;
                }
                else if (failVal == "✔" || failVal == "✘" || failVal == "✖" || failVal.Equals("OK", StringComparison.OrdinalIgnoreCase) || failVal.Equals("FAIL", StringComparison.OrdinalIgnoreCase) || failVal.Equals("NG", StringComparison.OrdinalIgnoreCase))
                {
                    defectCount++;
                }
            }

            int valueRow = summaryHeaderRow + 1;

            for (int c = 1; c <= 20; c++)
            {
                string header = sheet.Cell(summaryHeaderRow, c).GetString().Trim();
                if (header.StartsWith("Q'ty", StringComparison.OrdinalIgnoreCase) || header.StartsWith("Qty", StringComparison.OrdinalIgnoreCase))
                {
                    sheet.Cell(valueRow, c).Value = actualCount;
                }
                else if (header.Equals("Good", StringComparison.OrdinalIgnoreCase))
                {
                    sheet.Cell(valueRow, c).Value = goodCount;
                }
                else if (header.Equals("Defect", StringComparison.OrdinalIgnoreCase))
                {
                    sheet.Cell(valueRow, c).Value = defectCount;
                }
                else if (header.Contains("Yield", StringComparison.OrdinalIgnoreCase) || header.Contains("Tield", StringComparison.OrdinalIgnoreCase))
                {
                    double yieldPercent = actualCount > 0 ? Math.Round((double)goodCount / actualCount * 100.0, 2) : 0.0;
                    sheet.Cell(valueRow, c).Value = $"{yieldPercent:0.00}%";
                }
                else if (header.Equals("Check by", StringComparison.OrdinalIgnoreCase) || header.Equals("Operator", StringComparison.OrdinalIgnoreCase))
                {
                    sheet.Cell(valueRow, c).Value = testerName.Trim();
                }
            }

            for (int r = 1; r <= 5; r++)
            {
                for (int c = 1; c <= 20; c++)
                {
                    string text = sheet.Cell(r, c).GetString().Trim();
                    if (text.StartsWith("Lot Size", StringComparison.OrdinalIgnoreCase))
                    {
                        sheet.Cell(r, c).Value = "Lot Size :";
                        sheet.Cell(r, c + 1).Value = actualCount;
                    }
                }
            }
        }

        public static bool IsValidSerialNumber(string sn)
        {
            if (string.IsNullOrWhiteSpace(sn)) return false;
            string trimmed = sn.Trim();
            if (trimmed.Equals("S/N", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("SN", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("Serial No", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("Serial Number", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("F-PD", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Rev", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public static string IncrementSerialNumber(string currentSN)
        {
            if (string.IsNullOrWhiteSpace(currentSN)) return currentSN;

            var match = Regex.Match(currentSN, @"^(.*?)(\d+)$");
            if (match.Success)
            {
                string prefix = match.Groups[1].Value;
                string numStr = match.Groups[2].Value;

                if (long.TryParse(numStr, out long num))
                {
                    long nextNum = num + 1;
                    string nextNumStr = nextNum.ToString().PadLeft(numStr.Length, '0');
                    return prefix + nextNumStr;
                }
            }

            return currentSN;
        }

        public static string DecrementSerialNumber(string currentSN)
        {
            if (string.IsNullOrWhiteSpace(currentSN)) return currentSN;

            var match = Regex.Match(currentSN, @"^(.*?)(\d+)$");
            if (match.Success)
            {
                string prefix = match.Groups[1].Value;
                string numStr = match.Groups[2].Value;

                if (long.TryParse(numStr, out long num) && num > 0)
                {
                    long prevNum = num - 1;
                    string prevNumStr = prevNum.ToString().PadLeft(numStr.Length, '0');
                    return prefix + prevNumStr;
                }
            }

            return currentSN;
        }

        #region Sheet Protection (การล็อคแผ่นงานป้องกันการแก้ไข)

        public const string SheetProtectionPassword = "PH08";

        public static void UnprotectSheetSafely(IXLWorksheet sheet)
        {
            try
            {
                if (sheet.Protection.IsProtected)
                {
                    sheet.Unprotect(SheetProtectionPassword);
                }
            }
            catch
            {
                try { sheet.Unprotect(); } catch { }
            }
        }

        public static void ProtectSheet(IXLWorksheet sheet)
        {
            try
            {
                UnprotectSheetSafely(sheet);
                var protection = sheet.Protect(SheetProtectionPassword);
                protection.AllowElement(XLSheetProtectionElements.SelectLockedCells);
                protection.AllowElement(XLSheetProtectionElements.SelectUnlockedCells);
                protection.AllowElement(XLSheetProtectionElements.EditObjects);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to protect sheet: {ex.Message}");
            }
        }

        #endregion
    }
}
