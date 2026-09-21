using ClosedXML.Excel;

namespace TEST_DEF_03N
{
    internal static class QcReportLayout
    {
        internal const int SerialColumn = 24;
        internal const int FirstDataRow = 15;
        private const int DataRowsPerExpansion = 28;
        private const int FinalPageDataRows = 24;
        private const int RowsInsertedPerExpansion = DataRowsPerExpansion + 1;
        private const int DataRowsBeforeInsertedFooter =
            DataRowsPerExpansion - FinalPageDataRows;
        internal const string SheetProtectionPassword = "DEF03N";
        private const string StateSheet = "_QCState";

        private const int OkColumn = 22;
        private const int NgColumn = 23;
        private const int SummaryValueRowOffsetFromFooter = 3;

        internal static bool IsQcLayout(IXLWorksheet sheet)
        {
            string header = string.Join(" ",
                Enumerable.Range(12, 3)
                    .Select(r => sheet.Cell(r, SerialColumn).GetString()));

            return header.Contains("S/N", StringComparison.OrdinalIgnoreCase);
        }

        internal static void Validate(IXLWorksheet sheet)
        {
            if (!IsQcLayout(sheet))
                throw new InvalidOperationException("รายงาน QC นี้ยังเป็นรูปแบบเก่า กรุณาใช้ Template ใหม่ที่มี S/N คอลัมน์ X และไฟล์ LOT รูปแบบใหม่");
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

        internal static int GetBottomFooterRow(IXLWorksheet sheet) =>
            GetLastDataRow(sheet) + 1;

        internal static IReadOnlyList<int> GetFooterRows(IXLWorksheet sheet)
        {
            int bottomFooterRow = GetBottomFooterRow(sheet);
            var footerRows = new List<int>();

            for (int row = FirstDataRow + 1; row <= bottomFooterRow; row++)
            {
                if (IsDataRow(sheet, row - 1) && !IsDataRow(sheet, row))
                    footerRows.Add(row);
            }

            return footerRows;
        }

        private static int ExpandDataRows(IXLWorksheet sheet)
        {
            int templateRow = GetLastDataRow(sheet);
            int bottomFooterRow = templateRow + 1;
            int highestNumber = 0;

            for (int row = FirstDataRow; row <= templateRow; row++)
            {
                if (int.TryParse(
                        sheet.Cell(row, 1).GetFormattedString().Trim(),
                        out int number))
                {
                    highestNumber = Math.Max(highestNumber, number);
                }
            }

            sheet.Row(bottomFooterRow)
                .InsertRowsAbove(RowsInsertedPerExpansion);

            int insertedFooterRow =
                bottomFooterRow + DataRowsBeforeInsertedFooter;
            int movedBottomFooterRow =
                bottomFooterRow + RowsInsertedPerExpansion;

            for (int offset = 0; offset < DataRowsPerExpansion; offset++)
            {
                int newRow = bottomFooterRow + offset;
                if (offset >= DataRowsBeforeInsertedFooter)
                    newRow++; // เว้นแถว F-RD ระหว่างหน้ารายงาน

                sheet.Range(templateRow, 1, templateRow, SerialColumn)
                    .CopyTo(sheet.Cell(newRow, 1));

                sheet.Row(newRow).Height = sheet.Row(templateRow).Height;
                sheet.Range(newRow, 2, newRow, SerialColumn)
                    .Clear(XLClearOptions.Contents);

                sheet.Cell(newRow, 1).Value = highestNumber + offset + 1;
            }

            sheet.Range(
                    movedBottomFooterRow, 1,
                    movedBottomFooterRow, SerialColumn)
                .CopyTo(sheet.Cell(insertedFooterRow, 1));
            sheet.Row(insertedFooterRow).Height =
                sheet.Row(movedBottomFooterRow).Height;

            return bottomFooterRow;
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

        internal static void UpdateSummary(IXLWorksheet sheet)
        {
            if (!IsQcLayout(sheet))
                return;

            int good = 0;
            int defect = 0;
            int footerRow = GetBottomFooterRow(sheet);

            for (int row = FirstDataRow; row < footerRow; row++)
            {
                if (!IsDataRow(sheet, row))
                    continue;

                string ok = sheet.Cell(row, OkColumn).GetString().Trim();
                string ng = sheet.Cell(row, NgColumn).GetString().Trim();

                if (ok.Equals("OK", StringComparison.OrdinalIgnoreCase))
                {
                    good++;
                }
                else if (ng.Equals("NG", StringComparison.OrdinalIgnoreCase) ||
                         ng.Equals("NOT OK", StringComparison.OrdinalIgnoreCase))
                {
                    defect++;
                }
            }

            int quantity = good + defect;
            int summaryValueRow = footerRow + SummaryValueRowOffsetFromFooter;

            sheet.Cell(summaryValueRow, 2).Value = quantity;
            sheet.Cell(summaryValueRow, 4).Value = good;
            sheet.Cell(summaryValueRow, 6).Value = defect;
            sheet.Cell(summaryValueRow, 8).Value =
                quantity == 0 ? 0d : (double)good / quantity;
            sheet.Cell(summaryValueRow, 8).Style.NumberFormat.Format = "0.00%";

            // Qty is a merged B:C value area spanning two rows in the template.
            // Keep the generated count centered in both directions.
            var quantityRange = sheet.Range(
                summaryValueRow, 2,
                summaryValueRow + 1, 3);
            quantityRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            quantityRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        internal static void ProtectTestResults(IXLWorksheet sheet)
        {
            if (!IsQcLayout(sheet))
                return;

            if (sheet.Protection.IsProtected)
                sheet.Unprotect(SheetProtectionPassword);

            int signatureRow = GetBottomFooterRow(sheet) + SummaryValueRowOffsetFromFooter;

            // Leave only the three signature entry boxes editable. Their row
            // follows the footer, so this also works after each 52-row expansion.
            var signatureRanges = new[]
            {
                sheet.Range(signatureRow, 11, signatureRow, 14), // Check by: K:N
                sheet.Range(signatureRow, 15, signatureRow, 20), // Confirm by: O:T
                sheet.Range(signatureRow, 21, signatureRow, 24)  // Approved by: U:X
            };

            foreach (IXLRange signatureRange in signatureRanges)
            {
                signatureRange.Style.Protection.Locked = false;
                signatureRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                signatureRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            // Test records and the calculated summary are program-generated.
            // Protecting the worksheet prevents manual changes in Excel; the
            // application can still update these cells through ClosedXML.
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
            if (model == ModelTestResults.ModelA) sheet.Cell(row, "T").Value = "N/A";
            bool automated = sheet.Workbook.TryGetWorksheet(StateSheet, out var state) &&
                state.Cell(row, 1).GetString().Equals(sheet.Cell(row, SerialColumn).GetString().Trim(), StringComparison.OrdinalIgnoreCase) &&
                state.Cell(row, 2).GetString() == model && state.Cell(row, 3).GetString() == "PASS";

            bool contacts = IsOk(sheet.Cell(row, "N").GetString()) &&
                (model == ModelTestResults.ModelA || IsOk(sheet.Cell(row, "O").GetString()));

            bool buttonsAndLeds = Enumerable.Range(16, 6).Where(c => c != 20 || model != ModelTestResults.ModelA).All(c => IsOk(sheet.Cell(row, c).GetString()));

            bool pass = automated && contacts && buttonsAndLeds && ModelTestResults.IsValidModel(model);
            sheet.Cell(row, "V").Value = pass ? "OK" : "";

            bool failed = (state != null && state.Cell(row, 1).GetString().Equals(sheet.Cell(row, SerialColumn).GetString().Trim(), StringComparison.OrdinalIgnoreCase) && state.Cell(row, 2).GetString() == model && state.Cell(row, 3).GetString() == "FAIL") ||
                Enumerable.Range(14, 8).Where(c => (c != 15 && c != 20) || model != ModelTestResults.ModelA)
                    .Any(c => new[] { "NG", "FAIL", "NOT OK", "ERROR" }.Contains(sheet.Cell(row, c).GetString()));
            sheet.Cell(row, "W").Value = failed ? "NG" : "";

            return pass;
        }

        private static bool IsOk(string value) => value == "OK" || value == "PASS";

        internal static void WriteButtons(IXLWorksheet sheet, int row, IReadOnlyList<bool> buttons,
            IReadOnlyList<bool> ledProblems, string model)
        {
            for (int i = 0; i < 3; i++)
            {
                sheet.Cell(row, 16 + i).Value = buttons[i] ? "OK" : "NG";
                sheet.Cell(row, 19 + i).Value = i == 1 && model == ModelTestResults.ModelA ? "N/A" : ledProblems[i] ? "NG" : "OK";
            }
            RefreshInspection(sheet, row, model);
        }
    }
}
