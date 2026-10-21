using ClosedXML.Excel;

namespace TEST_DEF_03N
{
    internal static class QcReportLayout
    {
        internal const int SerialColumn = 24;      // Column X (S/N)
        internal const int FirstDataRow = 12;       // ข้อมูลตัวแรกเริ่มที่แถว 12
        private const int DataRowsPerExpansion = 41; // 41 แถวต่อหน้า
        internal const string SheetProtectionPassword = "DEF03N";
        private const string StateSheet = "_QCState";

        internal const int OkColumn = 22;           // Column V = ปกติ
        internal const int NgColumn = 23;           // Column W = ไม่ปกติ
        internal const string DefaultTemplateFileName = "DEF-03N QC Inspection.xlsx";

        internal static bool IsQcLayout(IXLWorksheet sheet)
        {
            for (int r = 8; r <= 14; r++)
            {
                string val = sheet.Cell(r, SerialColumn).GetString().Trim();
                if (val.Contains("S/N", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        internal static void Validate(IXLWorksheet sheet)
        {
            if (!IsQcLayout(sheet))
                throw new InvalidOperationException("รายงาน QC นี้ยังเป็นรูปแบบเก่า กรุณาใช้ Template ใหม่ DEF-03N QC Inspection.xlsx ที่มี S/N คอลัมน์ X");
        }

        internal static bool IsDataRow(IXLWorksheet sheet, int row) =>
            int.TryParse(sheet.Cell(row, 1).GetFormattedString().Trim(), out int number) && number > 0;

        private static int LastUsedRow(IXLWorksheet sheet) =>
            sheet.LastRowUsed()?.RowNumber() ?? FirstDataRow;

        internal static int GetLastDataRow(IXLWorksheet sheet)
        {
            int lastDataRow = FirstDataRow - 1;

            for (int row = FirstDataRow; row <= LastUsedRow(sheet); row++)
            {
                if (IsDataRow(sheet, row))
                    lastDataRow = row;
            }

            if (lastDataRow < FirstDataRow)
                throw new InvalidOperationException("ไม่พบแถวข้อมูล QC ใน Template");

            return lastDataRow;
        }

        internal static int FindSummaryHeaderRow(IXLWorksheet sheet)
        {
            int lastUsed = sheet.LastRowUsed()?.RowNumber() ?? FirstDataRow;
            for (int r = FirstDataRow + 1; r <= lastUsed + 20; r++)
            {
                string val = sheet.Cell(r, 6).GetString().Trim(); // Column F = Q'ty
                if (val.Equals("Q'ty", StringComparison.OrdinalIgnoreCase) || val.Equals("Qty", StringComparison.OrdinalIgnoreCase))
                    return r;
                string checkBy = sheet.Cell(r, 15).GetString().Trim(); // Column O = Check by
                if (checkBy.Equals("Check by", StringComparison.OrdinalIgnoreCase))
                    return r;
            }
            return GetLastDataRow(sheet) + 2;
        }

        internal static int GetBottomFooterRow(IXLWorksheet sheet) =>
            FindSummaryHeaderRow(sheet);

        internal static IReadOnlyList<int> GetFooterRows(IXLWorksheet sheet)
        {
            int summaryHeaderRow = FindSummaryHeaderRow(sheet);
            var footerRows = new List<int>();

            // รวมแถวสรุปและช่องลายเซ็น
            for (int row = summaryHeaderRow; row <= summaryHeaderRow + 4; row++)
            {
                footerRows.Add(row);
            }

            return footerRows;
        }

        private static int ExpandDataRows(IXLWorksheet sheet)
        {
            int lastDataRow = GetLastDataRow(sheet);
            int highestNumber = 0;

            for (int row = FirstDataRow; row <= lastDataRow; row++)
            {
                if (int.TryParse(
                        sheet.Cell(row, 1).GetFormattedString().Trim(),
                        out int number))
                {
                    highestNumber = Math.Max(highestNumber, number);
                }
            }

            // แทรกแถวใหม่ DataRowsPerExpansion แถวก่อนแถวสรุปผล (ต่อท้าย lastDataRow)
            int insertAt = lastDataRow + 1;
            sheet.Row(insertAt).InsertRowsAbove(DataRowsPerExpansion);

            for (int offset = 0; offset < DataRowsPerExpansion; offset++)
            {
                int targetRow = insertAt + offset;
                sheet.Row(targetRow).Height = sheet.Row(FirstDataRow).Height;
                sheet.Range(FirstDataRow, 1, FirstDataRow, SerialColumn).CopyTo(sheet.Cell(targetRow, 1));
                sheet.Range(targetRow, 2, targetRow, SerialColumn).Clear(XLClearOptions.Contents);
                sheet.Cell(targetRow, 1).Value = highestNumber + offset + 1;
            }

            return insertAt;
        }

        internal static int FindRow(
            IXLWorksheet sheet,
            string sn,
            out bool rowsExpanded)
        {
            Validate(sheet);
            rowsExpanded = false;
            int empty = -1;

            for (int row = FirstDataRow; row <= LastUsedRow(sheet); row++)
            {
                if (!IsDataRow(sheet, row)) continue;
                string current = sheet.Cell(row, SerialColumn).GetString().Trim();
                if (current.Equals(sn.Trim(), StringComparison.OrdinalIgnoreCase)) return row;
                if (empty < 0 && current.Length == 0) empty = row;
            }

            if (empty >= 0)
                return empty;

            rowsExpanded = true;
            return ExpandDataRows(sheet);
        }

        internal static void ClearExampleRows(IXLWorksheet sheet)
        {
            Validate(sheet);

            for (int row = FirstDataRow; row <= LastUsedRow(sheet); row++)
                if (IsDataRow(sheet, row)) sheet.Range(row, 2, row, SerialColumn).Clear(XLClearOptions.Contents);
        }

        internal static void UpdateSummary(IXLWorksheet sheet, string operatorName = "")
        {
            if (!IsQcLayout(sheet))
                return;

            int good = 0;
            int defect = 0;
            int summaryHeaderRow = FindSummaryHeaderRow(sheet);

            for (int row = FirstDataRow; row < summaryHeaderRow; row++)
            {
                if (!IsDataRow(sheet, row))
                    continue;

                string ok = sheet.Cell(row, OkColumn).GetString().Trim();
                string ng = sheet.Cell(row, NgColumn).GetString().Trim();

                if (IsPass(ok))
                {
                    good++;
                }
                else if (IsFail(ng))
                {
                    defect++;
                }
            }

            int quantity = good + defect;
            int summaryValueRow = summaryHeaderRow + 1;

            // F55: Qty, H55: Good, J55: Defect, L55: % Yield
            SetSummaryValueCell(sheet, summaryValueRow, 6, quantity);
            SetSummaryValueCell(sheet, summaryValueRow, 8, good);
            SetSummaryValueCell(sheet, summaryValueRow, 10, defect);
            SetSummaryValueCell(sheet, summaryValueRow, 12, quantity == 0 ? 0d : (double)good / quantity, "0.00%");

            // O55: Check by / Operator Name
            if (!string.IsNullOrWhiteSpace(operatorName))
            {
                var opCell = sheet.Cell(summaryValueRow, 15);
                opCell.Value = operatorName.Trim();
                opCell.Style.Font.FontName = "Cordia New";
                opCell.Style.Font.FontSize = 11;
                opCell.Style.Font.Bold = true;
                opCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                opCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
        }

        private static void SetSummaryValueCell(IXLWorksheet sheet, int row, int col, object value, string? numberFormat = null)
        {
            var cell = sheet.Cell(row, col);
            cell.Value = XLCellValue.FromObject(value);
            cell.Style.Font.FontName = "Cordia New";
            cell.Style.Font.FontSize = 11;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            if (!string.IsNullOrEmpty(numberFormat))
            {
                cell.Style.NumberFormat.Format = numberFormat;
            }
        }

        internal static void ProtectTestResults(IXLWorksheet sheet)
        {
            if (!IsQcLayout(sheet))
                return;

            if (sheet.Protection.IsProtected)
                sheet.Unprotect(SheetProtectionPassword);

            int summaryHeaderRow = FindSummaryHeaderRow(sheet);
            int signatureRow = summaryHeaderRow + 1;

            // ปลดล็อคเฉพาะช่องลายเซ็นต์ Check by (O:R), Confirm by (S:V), Approved by (W:X)
            var signatureRanges = new[]
            {
                sheet.Range(signatureRow, 15, signatureRow + 1, 18),
                sheet.Range(signatureRow, 19, signatureRow + 1, 22),
                sheet.Range(signatureRow, 23, signatureRow + 1, 24)
            };

            foreach (IXLRange signatureRange in signatureRanges)
            {
                signatureRange.Style.Protection.Locked = false;
                signatureRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                signatureRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            var protection = sheet.Protect(SheetProtectionPassword);
            protection.AllowElement(XLSheetProtectionElements.EditObjects);
        }

        internal static void SetAutomatedResult(IXLWorksheet sheet, int row, string model, bool? pass)
        {
            if (!sheet.Workbook.TryGetWorksheet(StateSheet, out var state))
            {
                state = sheet.Workbook.AddWorksheet(StateSheet);
                state.Visibility = XLWorksheetVisibility.VeryHidden;
            }
            state.Cell(row, 1).Value = sheet.Cell(row, SerialColumn).GetString().Trim();
            state.Cell(row, 2).Value = model;
            state.Cell(row, 3).Value = pass.HasValue ? (pass.Value ? "PASS" : "FAIL") : "PENDING";
        }

        internal static bool RefreshInspection(IXLWorksheet sheet, int row, string model)
        {
            if (model == ModelTestResults.ModelA)
            {
                SetSymbolCell(sheet, row, 15, "N/A"); // Column O (Alarm Relay)
                SetSymbolCell(sheet, row, 20, "N/A"); // Column T (LED AL)
            }

            bool automated = sheet.Workbook.TryGetWorksheet(StateSheet, out var state) &&
                state.Cell(row, 1).GetString().Equals(sheet.Cell(row, SerialColumn).GetString().Trim(), StringComparison.OrdinalIgnoreCase) &&
                state.Cell(row, 2).GetString() == model && state.Cell(row, 3).GetString() == "PASS";

            bool contacts = IsPass(sheet.Cell(row, "N").GetString()) &&
                (model == ModelTestResults.ModelA || IsPass(sheet.Cell(row, "O").GetString()));

            bool buttonsAndLeds = Enumerable.Range(16, 6)
                .Where(c => c != 20 || model != ModelTestResults.ModelA)
                .All(c => IsPass(sheet.Cell(row, c).GetString()));

            bool pass = automated && contacts && buttonsAndLeds && ModelTestResults.IsValidModel(model);
            SetSymbolCell(sheet, row, OkColumn, pass ? "✔" : "");

            bool failed = (state != null && state.Cell(row, 1).GetString().Equals(sheet.Cell(row, SerialColumn).GetString().Trim(), StringComparison.OrdinalIgnoreCase) && state.Cell(row, 2).GetString() == model && state.Cell(row, 3).GetString() == "FAIL") ||
                Enumerable.Range(14, 8).Where(c => (c != 15 && c != 20) || model != ModelTestResults.ModelA)
                    .Any(c => IsFail(sheet.Cell(row, c).GetString()));

            SetSymbolCell(sheet, row, NgColumn, failed ? "✖" : "");

            return pass;
        }

        internal static bool IsPass(string value) =>
            value == "✔" || value.Equals("OK", StringComparison.OrdinalIgnoreCase) || value.Equals("PASS", StringComparison.OrdinalIgnoreCase);

        internal static bool IsFail(string value) =>
            value == "✖" || value == "✘" || value.Equals("NG", StringComparison.OrdinalIgnoreCase) || value.Equals("FAIL", StringComparison.OrdinalIgnoreCase) || value.Equals("NOT OK", StringComparison.OrdinalIgnoreCase) || value.Equals("ERROR", StringComparison.OrdinalIgnoreCase);

        internal static void SetSymbolCell(IXLWorksheet sheet, int row, int col, string symbol)
        {
            var cell = sheet.Cell(row, col);
            cell.Value = symbol;
            cell.Style.Font.FontName = "Segoe UI Symbol";
            cell.Style.Font.FontSize = 6;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        internal static void WriteButtons(IXLWorksheet sheet, int row, IReadOnlyList<bool> buttons,
            IReadOnlyList<bool> ledProblems, string model)
        {
            for (int i = 0; i < 3; i++)
            {
                SetSymbolCell(sheet, row, 16 + i, buttons[i] ? "✔" : "✖"); // P: ปุ่ม F, Q: ปุ่ม ลด, R: ปุ่ม เพิ่ม
                SetSymbolCell(sheet, row, 19 + i, i == 1 && model == ModelTestResults.ModelA ? "N/A" : (ledProblems[i] ? "✖" : "✔")); // S: LED OUT, T: LED AL, U: จอแสดงผล
            }
            RefreshInspection(sheet, row, model);
        }
    }
}
