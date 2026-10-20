using ClosedXML.Excel;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CMATestVer1
{
    public partial class Form1
    {
        private const int HeaderScanMaxRow = 11;
        private const int HeaderScanMaxCol = 35;

        /// <summary>
        /// สแกนหา Cell ในส่วน Header (แถว 1 ถึง 11) ที่ตรงกับเงื่อนไข
        /// </summary>
        private static IXLCell? FindHeaderCell(IXLWorksheet sheet, Func<string, bool> predicate, int maxRow = HeaderScanMaxRow, int maxCol = HeaderScanMaxCol)
        {
            for (int r = 1; r <= maxRow; r++)
            {
                for (int c = 1; c <= maxCol; c++)
                {
                    var cell = sheet.Cell(r, c);
                    string text = cell.GetString().Trim();
                    if (!string.IsNullOrEmpty(text) && predicate(text))
                    {
                        return cell;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// หา Cell ถัดไปทางขวาของ Label โดยตรวจสอบการ Merge เซลล์ให้อัตโนมัติ
        /// หาก Label มีการ Merge หลายคอลัมน์ จะข้ามไปยังคอลัมน์แรกถัดจากขอบเขตที่ Merge
        /// </summary>
        private static IXLCell GetCellAfterLabel(IXLWorksheet sheet, IXLCell labelCell)
        {
            int row = labelCell.Address.RowNumber;
            int col = labelCell.Address.ColumnNumber;

            foreach (var mr in sheet.MergedRanges)
            {
                if (row >= mr.RangeAddress.FirstAddress.RowNumber &&
                    row <= mr.RangeAddress.LastAddress.RowNumber &&
                    col >= mr.RangeAddress.FirstAddress.ColumnNumber &&
                    col <= mr.RangeAddress.LastAddress.ColumnNumber)
                {
                    return sheet.Cell(row, mr.RangeAddress.LastAddress.ColumnNumber + 1);
                }
            }

            return sheet.Cell(row, col + 1);
        }

        #region ค้นหาตำแหน่ง Cell สำหรับแต่ละฟิลด์

        private static IXLCell? FindLotCell(IXLWorksheet sheet)
        {
            var labelCell = FindHeaderCell(sheet, text =>
                Regex.IsMatch(text, @"^Lot\s*[:：]?", RegexOptions.IgnoreCase) &&
                !text.Contains("size", StringComparison.OrdinalIgnoreCase));

            return labelCell != null ? GetCellAfterLabel(sheet, labelCell) : null;
        }

        private static IXLCell? FindDateCell(IXLWorksheet sheet)
        {
            var labelCell = FindHeaderCell(sheet, text =>
                Regex.IsMatch(text, @"^(Date|วันที่)\s*[:：]?", RegexOptions.IgnoreCase));

            return labelCell != null ? GetCellAfterLabel(sheet, labelCell) : null;
        }

        private static IXLCell? FindLotSizeCell(IXLWorksheet sheet)
        {
            var labelCell = FindHeaderCell(sheet, text =>
                Regex.IsMatch(text, @"^Lot\s*Size\s*[:：]?", RegexOptions.IgnoreCase));

            return labelCell != null ? GetCellAfterLabel(sheet, labelCell) : null;
        }

        #endregion

        #region การเขียนค่าลง Header

        private static void WriteLotToHeader(IXLWorksheet sheet, string lotNumber)
        {
            var targetCell = FindLotCell(sheet) ?? sheet.Cell("P3");
            targetCell.Value = lotNumber;
        }

        private static void WriteDateToHeader(IXLWorksheet sheet, string thaiDateStr)
        {
            var targetCell = FindDateCell(sheet) ?? sheet.Cell("AB3");
            targetCell.Value = thaiDateStr;
        }

        private static void WriteLotSizeToHeader(IXLWorksheet sheet, object count)
        {
            var targetCell = FindLotSizeCell(sheet) ?? sheet.Cell("U3");
            targetCell.Value = XLCellValue.FromObject(count);
        }

        private static void WriteEcnToHeader(IXLWorksheet sheet, string ecnValue)
        {
            string formattedText = $"ECN NO: {ecnValue}".Trim();
            var cell = FindHeaderCell(sheet, text => text.Contains("ECN", StringComparison.OrdinalIgnoreCase));

            if (cell != null)
            {
                string text = cell.GetString().Trim();
                if (Regex.IsMatch(text, @"^ECN(\s*NO)?\s*[:：]?$", RegexOptions.IgnoreCase))
                {
                    GetCellAfterLabel(sheet, cell).Value = ecnValue;
                }
                else
                {
                    cell.Value = formattedText;
                }
            }
            else
            {
                sheet.Cell("V6").Value = formattedText;
            }
        }

        private static void WriteFirmwareToHeader(IXLWorksheet sheet, string firmwareValue)
        {
            var cell = FindHeaderCell(sheet, text =>
                text.Contains("AirControl", StringComparison.OrdinalIgnoreCase) ||
                ((text.Contains("Firmware", StringComparison.OrdinalIgnoreCase) || text.Contains("FW", StringComparison.OrdinalIgnoreCase)) &&
                 !text.Contains("display", StringComparison.OrdinalIgnoreCase)));

            if (cell != null)
            {
                string text = cell.GetString().Trim();
                if (Regex.IsMatch(text, @"^(Main\s*)?(Firmware|FW)\s*[:：]?$", RegexOptions.IgnoreCase))
                {
                    GetCellAfterLabel(sheet, cell).Value = firmwareValue;
                }
                else
                {
                    cell.Value = firmwareValue;
                }
            }
            else
            {
                sheet.Cell("V7").Value = firmwareValue;
            }
        }

        private static void WriteDisplayFirmwareToHeader(IXLWorksheet sheet, string displayFirmwareValue)
        {
            var cell = FindHeaderCell(sheet, text =>
                text.Contains("MCU_DISPLAY", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("DISPLAY_AIR_CONTROL", StringComparison.OrdinalIgnoreCase) ||
                (text.Contains("display", StringComparison.OrdinalIgnoreCase) &&
                 (text.Contains("firmware", StringComparison.OrdinalIgnoreCase) || text.Contains("hex", StringComparison.OrdinalIgnoreCase) || text.Contains("fw", StringComparison.OrdinalIgnoreCase))));

            if (cell != null)
            {
                string text = cell.GetString().Trim();
                if (Regex.IsMatch(text, @"^Display\s*(Firmware|FW)\s*[:：]?$", RegexOptions.IgnoreCase))
                {
                    GetCellAfterLabel(sheet, cell).Value = displayFirmwareValue;
                }
                else
                {
                    cell.Value = displayFirmwareValue;
                }
            }
            else
            {
                sheet.Cell("V8").Value = displayFirmwareValue;
            }
        }

        private static void WriteEquipmentExpiryToHeader(IXLWorksheet sheet, string equipmentText)
        {
            var cell = FindHeaderCell(sheet, text =>
                text.Contains("เครื่องมือ", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("วันหมดอายุ", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Equipment", StringComparison.OrdinalIgnoreCase));

            if (cell != null)
            {
                cell.Value = equipmentText;
            }
            else
            {
                sheet.Cell("K5").Value = equipmentText;
            }
        }

        #endregion

        #region การอ่านค่าจาก Header

        private static string ReadLotFromHeader(IXLWorksheet sheet)
        {
            var targetCell = FindLotCell(sheet) ?? sheet.Cell("P3");
            return targetCell.GetString().Trim();
        }

        private static string ReadLotSizeFromHeader(IXLWorksheet sheet)
        {
            var targetCell = FindLotSizeCell(sheet) ?? sheet.Cell("U3");
            return targetCell.GetString().Trim();
        }

        private static string ReadDateFromHeader(IXLWorksheet sheet)
        {
            var targetCell = FindDateCell(sheet) ?? sheet.Cell("AB3");
            return targetCell.GetString().Trim();
        }

        #endregion
    }
}
