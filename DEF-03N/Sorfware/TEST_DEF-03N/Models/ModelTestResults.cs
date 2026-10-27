using ClosedXML.Excel;

namespace TEST_DEF_03N
{
    internal static class ModelTestResults
    {
        internal const string ModelA = "DEF-03N-A";
        internal const string ModelAB = "DEF-03N-A-B";

        internal static bool IsValidModel(string model) => model == ModelA || model == ModelAB;
        internal static string ContactResult(short value) => value == 1 ? "✔" : "✖";

        internal static void WriteHeader(IXLWorksheet sheet, IXLWorksheet template, string model, bool qc = true)
        {
            if (!IsValidModel(model)) return;

            const string Checked = "☑";
            const string Unchecked = "☐";

            if (qc)
            {
                // ตรวจสอบว่าเป็น Template ใหม่ DEF-03N QC Inspection หรือไม่
                bool isNewTemplate = sheet.Cell(10, 24).GetString().Trim().Contains("S/N", StringComparison.OrdinalIgnoreCase)
                    || sheet.Cell(9, 1).GetString().Trim().Contains("No", StringComparison.OrdinalIgnoreCase);

                if (isNewTemplate)
                {
                    bool isModelA = model == ModelA;

                    var acceptanceCriteria = new (string Keyword, bool CheckForModelA)[]
                    {
                        ("รับสัญญาณอินพุตจาก PTC 2K, PTC 10K, NTC", true),
                        ("แสดงผลด้วย 7-segment LED สีแดง 3 หลัก", true),
                        ("สามารถตั้งจุดทศนิยมได้", true),
                        ("สาย NTC 2K", true),
                        ("เครื่องมือ", true),
                        ("ECN NO", true),
                        ("FW", true),
                        ("Power Supply", true),
                        ("Relay Output เลือกทำงานได้ทั้ง Heating และ Cooling", false), // Type A ไม่ติ๊ก
                        ("มี Hysteresis Time โดยเริ่มทำงาน 0-99.59 min", false),       // Type A ไม่ติ๊ก
                        ("มี MainRelay 1 ชุดสามารถเลือกได้ว่าจะทำงานเป็น Control/Alarm", false) // Type A ไม่ติ๊ก
                    };

                    foreach (var (keyword, checkForModelA) in acceptanceCriteria)
                    {
                        bool shouldCheck = !isModelA || checkForModelA; // ถ้าไม่ใช่ Model A (เช่น Model A-B) ติ๊กทั้งหมด
                        string mark = shouldCheck ? Checked : Unchecked;

                        var cell = FindCellByKeyword(sheet, 3, 10, keyword);
                        if (cell != null && cell.Address.ColumnNumber > 1)
                        {
                            sheet.Cell(cell.Address.RowNumber, cell.Address.ColumnNumber - 1).Value = mark;
                        }
                    }

                    var modelLabelCell = FindCellByKeyword(sheet, 1, 3, "Model:");
                    if (modelLabelCell != null)
                        sheet.Cell(modelLabelCell.Address.RowNumber, modelLabelCell.Address.ColumnNumber + 1).Value = model;
                    else
                        sheet.Cell("J2").Value = model;
                }
                else
                {
                    // Template เดิม
                    string[] alwaysChecked = { "A6", "A7", "A8", "A9", "M5", "M6", "M9", "M10" };
                    foreach (string address in alwaysChecked)
                        sheet.Cell(address).Value = Checked;

                    string[] modelDependent = { "A10", "M7", "M8" };
                    foreach (string address in modelDependent)
                        sheet.Cell(address).Value = model == ModelA ? Unchecked : Checked;

                    if (sheet.Cell("I3").GetString().TrimStart().StartsWith("Model:", StringComparison.OrdinalIgnoreCase))
                        sheet.Cell("I3").Value = "Model:";
                    sheet.Cell("J3").Value = model;
                }
            }
            else
            {
                // QA sheet
                foreach (string address in new[] { "A10", "K6", "K7" })
                    sheet.Cell(address).Value = model == ModelA ? Unchecked : template.Cell(address).Value;
            }
        }

        private static IXLCell? FindCellByKeyword(IXLWorksheet sheet, int startRow, int endRow, string keyword)
        {
            for (int r = startRow; r <= endRow; r++)
            {
                for (int c = 1; c <= 24; c++)
                {
                    string text = sheet.Cell(r, c).GetString().Trim();
                    if (!string.IsNullOrEmpty(text) && text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return sheet.Cell(r, c);
                    }
                }
            }
            return null;
        }

        internal static bool OverallPass(string model, bool temperaturesComplete,
            bool temperaturesPass, bool cancelled, string relay, string alarm)
            => IsValidModel(model) && temperaturesComplete && temperaturesPass && !cancelled &&
               QcReportLayout.IsPass(relay) && (model == ModelA || QcReportLayout.IsPass(alarm));

        internal static void WriteRow(IXLWorksheet sheet, int row, string relay, string alarm, bool pass, string model)
        {
            QcReportLayout.SetSymbolCell(sheet, row, 14, relay); // Column N (Output Relay)
            QcReportLayout.SetSymbolCell(sheet, row, 15, model == ModelA ? "N/A" : alarm); // Column O (Alarm Relay)
            QcReportLayout.SetAutomatedResult(sheet, row, model, pass);
            QcReportLayout.RefreshInspection(sheet, row, model);
        }
    }
}
