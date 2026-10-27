// =========================================================================================
// [DEVELOPER GUIDE]
// ExcelReportService.cs: "บริการจัดการไฟล์รายงาน Excel (ClosedXML) สำหรับ DEF-03N"
// - ตรวจจับหมายเลข S/N ซ้ำในไฟล์รายงาน (CheckDuplicateSN)
// - ตรวจสอบว่าไฟล์ Excel เปิดค้างอยู่หรือไม่ ก่อนเริ่มทดสอบ (EnsureReportFileClosed)
// - ตรวจสอบความพร้อมของโฟลเดอร์รายงาน, S/N, และ LOT (ValidateBeforeTest)
// - นับจำนวน S/N ที่ผ่านการทดสอบแล้วในชีต (GetTestedSerialCount)
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;
using System.IO;
using ClosedXML.Excel;

namespace TEST_DEF_03N.Services
{
    public class ExcelReportService
    {
        /// <summary>
        /// ตรวจสอบว่าหมายเลข S/N ซ้ำกับข้อมูลที่มีอยู่แล้วในไฟล์ Excel ของ LOT นั้นหรือไม่
        /// </summary>
        public bool CheckDuplicateSN(string sn, string lotName, string outputFolder, bool isQaMode, out int duplicateRow)
        {
            duplicateRow = -1;
            outputFolder = QcReportLayout.NormalizeReportFolder(outputFolder);
            if (string.IsNullOrWhiteSpace(sn) ||
                string.IsNullOrWhiteSpace(outputFolder) ||
                !Directory.Exists(outputFolder) ||
                string.IsNullOrWhiteSpace(lotName))
            {
                return false;
            }

            string reportPath = QcReportLayout.GetReportFilePath(outputFolder, lotName);

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

                int lastReportRow = sheet.LastRowUsed()?.RowNumber() ?? 500;
                int startRow = isQaMode ? 15 : QcReportLayout.FirstDataRow;
                int serialCol = isQaMode ? 18 : QcReportLayout.SerialColumn;

                for (int row = startRow; row <= lastReportRow; row++)
                {
                    if (!isQaMode && !QcReportLayout.IsDataRow(sheet, row))
                        continue;

                    string existingSN = sheet.Cell(row, serialCol).GetString().Trim();
                    if (string.IsNullOrEmpty(existingSN))
                        existingSN = sheet.Cell(row, serialCol).GetFormattedString().Trim();

                    if (!string.IsNullOrEmpty(existingSN) &&
                        existingSN.Equals(sn, StringComparison.OrdinalIgnoreCase))
                    {
                        duplicateRow = row;
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ตรวจสอบว่าไฟล์รายงาน Excel ถูกเปิดค้างอยู่หรือไม่ (ต้องสามารถเปิดเขียนได้ก่อนเริ่มรัน)
        /// </summary>
        public bool EnsureReportFileClosed(string outputFolder, string lotName, out string? errorMessage)
        {
            errorMessage = null;
            outputFolder = QcReportLayout.NormalizeReportFolder(outputFolder);
            if (string.IsNullOrWhiteSpace(outputFolder) ||
                !Directory.Exists(outputFolder) ||
                string.IsNullOrWhiteSpace(lotName))
            {
                return true;
            }

            string reportPath = QcReportLayout.GetReportFilePath(outputFolder, lotName);

            if (!File.Exists(reportPath))
                return true;

            try
            {
                using var stream = new FileStream(reportPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                errorMessage = $"ไม่สามารถเริ่มการทดสอบได้ เนื่องจากไฟล์ Excel ยังเปิดอยู่\n\nไฟล์: {Path.GetFileName(reportPath)}\n\nกรุณาปิดไฟล์ Excel เดิมก่อน แล้วกด Run ใหม่อีกครั้ง";
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                errorMessage = $"ไม่สามารถเข้าถึงไฟล์รายงานได้\n\nไฟล์: {Path.GetFileName(reportPath)}\n\nกรุณาตรวจสอบสิทธิ์ของไฟล์ หรือปิด Excel แล้วลองใหม่อีกครั้ง";
                return false;
            }
        }

        /// <summary>
        /// ตรวจสอบความถูกต้องของ Input พื้นฐานก่อนเริ่มการทดสอบ
        /// </summary>
        public bool ValidateBeforeTest(string outputFolder, string sn, string lot, out string? validationError)
        {
            validationError = null;
            if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
            {
                validationError = "กรุณากดปุ่ม Save เพื่อเลือกโฟลเดอร์สำหรับเซฟไฟล์รายงานก่อนครับ!";
                return false;
            }

            if (string.IsNullOrEmpty(sn))
            {
                validationError = "กรุณากรอก S/N ก่อนเริ่มการทดสอบครับ!";
                return false;
            }

            if (string.IsNullOrEmpty(lot))
            {
                validationError = "กรุณากรอก LOT ก่อนเริ่มการทดสอบครับ!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// นับจำนวน Serial Number ที่บันทึกไปแล้วในชีต Excel
        /// </summary>
        public int GetTestedSerialCount(IXLWorksheet sheet)
        {
            int count = 0;
            int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = QcReportLayout.FirstDataRow; r <= lastRow; r++)
            {
                if (!QcReportLayout.IsDataRow(sheet, r)) continue;
                string val = sheet.Cell(r, QcReportLayout.SerialColumn).GetString().Trim();
                if (!string.IsNullOrEmpty(val)) count++;
            }
            return count;
        }
    }
}
