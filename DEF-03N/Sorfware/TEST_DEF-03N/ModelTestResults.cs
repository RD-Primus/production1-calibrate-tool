using ClosedXML.Excel;

namespace TEST_DEF_03N
{
    internal static class ModelTestResults
    {
        internal const string ModelA = "DEF-03N-A";
        internal const string ModelAB = "DEF-03N-A-B";

        internal static bool IsValidModel(string model) => model == ModelA || model == ModelAB;
        internal static string ContactResult(short value) => value == 1 ? "OK" : "NG";

        internal static void WriteHeader(IXLWorksheet sheet, IXLWorksheet template, string model, bool qc = true)
        {
            if (!IsValidModel(model)) return;

            const string Checked = "☑";
            const string Unchecked = "☐";

            if (qc)
            {
                // เช็คตลอดไม่ว่า Model ไหน
                string[] alwaysChecked = { "A6", "A7", "A8", "A9", "M5", "M6", "M9", "M10" };
                foreach (string address in alwaysChecked)
                    sheet.Cell(address).Value = Checked;

                // เช็คเฉพาะ Model A-B เท่านั้น
                string[] modelDependent = { "A10", "M7", "M8" };
                foreach (string address in modelDependent)
                    sheet.Cell(address).Value = model == ModelA ? Unchecked : Checked;
            }
            else
            {
                // ⚠️ ยังไม่ทราบ layout ของ QA sheet — คงพฤติกรรมเดิมไว้ก่อน
                foreach (string address in new[] { "A10", "K6", "K7" })
                    sheet.Cell(address).Value = model == ModelA ? Unchecked : template.Cell(address).Value;
            }

            // The supplied template embeds the previous model in I3; J3 is the model field now.
            if (sheet.Cell("I3").GetString().TrimStart().StartsWith("Model:",
                System.StringComparison.OrdinalIgnoreCase))
                sheet.Cell("I3").Value = "Model:";
            sheet.Cell("J3").Value = model;
        }

        internal static bool OverallPass(string model, bool temperaturesComplete,
            bool temperaturesPass, bool cancelled, string relay, string alarm)
            => IsValidModel(model) && temperaturesComplete && temperaturesPass && !cancelled &&
               relay == "OK" && (model == ModelA || alarm == "OK");

        internal static void WriteRow(IXLWorksheet sheet, int row, string relay, string alarm, bool pass, string model)
        {
            sheet.Cell(row, "N").Value = relay;
            sheet.Cell(row, "O").Value = alarm;
            QcReportLayout.SetAutomatedResult(sheet, row, model, pass);
            QcReportLayout.RefreshInspection(sheet, row, model);
        }
    }
}
