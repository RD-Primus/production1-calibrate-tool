using ClosedXML.Excel;

namespace CMATestVer1
{
    public partial class Form1
    {
        private void InitializeOperatorNameFeature()
        {
            txt_name.PlaceholderText = "กรุณาลงชื่อ";
            txt_name.Text = Properties.Settings.Default.SavedOperatorName?.Trim() ?? string.Empty;
            txt_name.TextChanged += TxtName_TextChanged;
        }

        private void TxtName_TextChanged(object? sender, EventArgs e)
        {
            Properties.Settings.Default.SavedOperatorName = txt_name.Text.Trim();
            Properties.Settings.Default.Save();
        }

        private void WriteOperatorNameToSummary(IXLWorksheet sheet, int summaryHeaderRow)
        {
            const int lastReportColumn = 28;
            int checkByColumn = 0;

            for (int column = 1; column <= lastReportColumn; column++)
            {
                string heading = sheet.Cell(summaryHeaderRow, column).GetString().Trim();
                if (heading.Equals("Check by", StringComparison.OrdinalIgnoreCase))
                {
                    checkByColumn = column;
                    break;
                }
            }

            if (checkByColumn == 0)
            {
                throw new InvalidOperationException(
                    "ไม่พบช่อง Check by ในส่วน Summary ของไฟล์ Excel");
            }

            var cell = sheet.Cell(summaryHeaderRow + 1, checkByColumn);
            cell.Value = txt_name.Text.Trim();
            cell.Style.Font.FontName = "Cordia New";
            cell.Style.Font.FontSize = 11;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
    }
}
