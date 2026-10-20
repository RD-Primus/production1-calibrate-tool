using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CMATestVer1
{
    public static class CmaReportLayout
    {
        internal const int FirstTestRow = 9;
        internal const int LastResultColumn = 28;
        internal const int TestsPerPage = 31;
        internal const int SummaryPageTests = 26;

        private const int TemplatePageStartRow = 9;

        private static readonly int[] VerticallyMergedTestColumns = { 2, 4, 7, 10 };

        public static int AddFullPageCapacity(IXLWorksheet sheet, int summaryHeaderRow)
        {
            var currentPageTestRows = GetCurrentPageTestRows(sheet, summaryHeaderRow);
            int lastNumber = int.Parse(sheet.Cell(currentPageTestRows.Last(), 1).GetString().Trim());

            // 1. ถ้าหน้าสุดท้ายปัจจุบันยังไม่เต็ม 31 แถว (เช่น มี 26 แถว)
            // ให้ขยายหน้าแรก/หน้าปัจจุบันให้เต็ม 31 แถว (เพิ่ม 5 แถว) และสร้างหน้าใหม่สำหรับ Summary (26 แถว) ต่อท้าย
            if (currentPageTestRows.Count < TestsPerPage)
            {
                int missingRows = TestsPerPage - currentPageTestRows.Count;
                int firstExpandedRow = FillCurrentPage(sheet, currentPageTestRows, TestsPerPage);

                int nextNumber = lastNumber + missingRows + 1;
                InsertSummaryPage(sheet, firstExpandedRow + missingRows, nextNumber);

                return firstExpandedRow;
            }

            // 2. ถ้าหน้าปัจจุบันเต็ม 31 แถวอยู่แล้ว ให้เพิ่มหน้า Summary ใหม่ขนาด 26 แถว ต่อท้ายหน้าปัจจุบัน
            int insertPos = currentPageTestRows[^1] + 1;
            return InsertSummaryPage(sheet, insertPos, lastNumber + 1);
        }

        public static void EnsureSummaryPageCapacity(
            IXLWorksheet sheet,
            int summaryHeaderRow)
        {
            var currentPageTestRows = GetCurrentPageTestRows(sheet, summaryHeaderRow);

            if (currentPageTestRows.Count == SummaryPageTests || currentPageTestRows.Count == TestsPerPage)
                return;

            if (currentPageTestRows.Count < SummaryPageTests)
            {
                FillCurrentPage(sheet, currentPageTestRows, SummaryPageTests);
                return;
            }

            if (currentPageTestRows.Count > SummaryPageTests && currentPageTestRows.Count < TestsPerPage)
            {
                FillCurrentPage(sheet, currentPageTestRows, TestsPerPage);
            }
        }

        private static List<int> GetCurrentPageTestRows(IXLWorksheet sheet, int summaryHeaderRow)
        {
            var allTestRows = Enumerable.Range(FirstTestRow, summaryHeaderRow - FirstTestRow)
                .Where(row => IsTestRow(sheet, row))
                .ToList();

            if (allTestRows.Count == 0)
                throw new InvalidOperationException("ไม่พบแถวการทดสอบในไฟล์ Excel");

            int fullPages = allTestRows.Count / TestsPerPage;
            int lastPageStartIndex = fullPages * TestsPerPage;
            if (lastPageStartIndex >= allTestRows.Count)
            {
                lastPageStartIndex = Math.Max(0, (fullPages - 1) * TestsPerPage);
            }

            return allTestRows.Skip(lastPageStartIndex).ToList();
        }

        private static int FillCurrentPage(
            IXLWorksheet sheet,
            IReadOnlyList<int> currentPageTestRows,
            int targetCapacity)
        {
            int pageStartRow = currentPageTestRows[0];
            int pageLastRow = currentPageTestRows[^1];
            int currentCount = currentPageTestRows.Count;
            int missingRows = targetCapacity - currentCount;
            if (missingRows <= 0) return pageStartRow;

            int lastNumber = int.Parse(sheet.Cell(pageLastRow, 1).GetString().Trim());
            var labels = CaptureMergedLabels(sheet, pageStartRow);

            UnmergePageLabels(sheet, pageStartRow, pageLastRow);

            int insertAt = pageLastRow + 1;
            sheet.Row(insertAt).InsertRowsAbove(missingRows);

            for (int offset = 0; offset < missingRows; offset++)
            {
                int targetRow = insertAt + offset;
                CopyRowFormat(sheet, TemplatePageStartRow, targetRow);
                ClearRowContents(sheet, targetRow);
                SetValueCell(sheet, targetRow, 1, lastNumber + 1 + offset);
            }

            int newPageLastRow = insertAt + missingRows - 1;
            MergePageLabels(sheet, pageStartRow, newPageLastRow, labels);

            return insertAt;
        }

        private static int InsertSummaryPage(
            IXLWorksheet sheet,
            int summaryHeaderRow,
            int nextNumber)
        {
            int insertAt = summaryHeaderRow;
            var labels = CaptureMergedLabels(sheet, TemplatePageStartRow);

            sheet.Row(insertAt).InsertRowsAbove(SummaryPageTests);

            for (int offset = 0; offset < SummaryPageTests; offset++)
            {
                int targetRow = insertAt + offset;
                CopyRowFormat(sheet, TemplatePageStartRow, targetRow);
                ClearRowContents(sheet, targetRow);
                SetValueCell(sheet, targetRow, 1, nextNumber + offset);
            }

            int pageLastRow = insertAt + SummaryPageTests - 1;
            MergePageLabels(sheet, insertAt, pageLastRow, labels);

            return insertAt;
        }

        private static Dictionary<int, XLCellValue> CaptureMergedLabels(
            IXLWorksheet sheet,
            int pageStartRow)
        {
            return VerticallyMergedTestColumns.ToDictionary(
                column => column,
                column => sheet.Cell(pageStartRow, column).Value);
        }

        private static void UnmergePageLabels(
            IXLWorksheet sheet,
            int pageStartRow,
            int pageLastRow)
        {
            foreach (int column in VerticallyMergedTestColumns)
                sheet.Range(pageStartRow, column, pageLastRow, column).Unmerge();
        }

        private static void MergePageLabels(
            IXLWorksheet sheet,
            int pageStartRow,
            int pageLastRow,
            IReadOnlyDictionary<int, XLCellValue> labels)
        {
            foreach (int column in VerticallyMergedTestColumns)
            {
                sheet.Range(pageStartRow, column, pageLastRow, column).Merge();
                sheet.Cell(pageStartRow, column).Value = labels[column];
                sheet.Cell(pageStartRow, column).Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
                sheet.Cell(pageStartRow, column).Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;
                sheet.Cell(pageStartRow, column).Style.Font.FontName = "Cordia New";
                sheet.Cell(pageStartRow, column).Style.Font.FontSize = 10;
            }
        }

        private static void CopyRowFormat(
            IXLWorksheet sheet,
            int sourceRow,
            int targetRow)
        {
            sheet.Row(targetRow).Height = sheet.Row(sourceRow).Height;

            for (int column = 1; column <= LastResultColumn; column++)
                sheet.Cell(targetRow, column).Style = sheet.Cell(sourceRow, column).Style;
        }

        private static void ClearRowContents(IXLWorksheet sheet, int row)
        {
            sheet.Range(row, 1, row, LastResultColumn)
                .Clear(XLClearOptions.Contents);
        }

        private static bool IsTestRow(IXLWorksheet sheet, int row)
        {
            return int.TryParse(sheet.Cell(row, 1).GetString().Trim(), out _);
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
    }
}
