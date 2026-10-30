// =========================================================================================
// [DEVELOPER GUIDE]
// Form1.cs: ทำหน้าที่เป็น "ศูนย์บัญชาการ UI (Control Center / View Controller)"
// - รวมการทำงานของหน้าจอ เข้ากับ Service ต่างๆ
// - หากต้องการแก้ไข "ขั้นตอนการทดสอบ (Master Sequence)" -> ไปที่ Services/Orchestrator/Ph08TestRunner.cs
// - หากต้องการแก้ไข "การบันทึกรายงาน Excel"             -> ไปที่ Services/Reporting/ExcelReportService.cs
// - หากต้องการแก้ไข "การสแกนบาร์โค้ด / QR"             -> ไปที่ Services/Barcode/BarcodeParser.cs
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using PH_08_Test.Services.Orchestrator;
using PH_08_Test.Services.Reporting;

namespace PH_08_Test
{
    public partial class Form1 : Form, IPh08TestHardwareProvider
    {
        private readonly ExcelReportService _excelService = new ExcelReportService();
        private readonly Ph08TestRunner _testRunner = new Ph08TestRunner();

        public Form1()
        {
            InitializeComponent();
            InitManualPopupControls();
            InitPH08Controls();
            InitDeviceControls();
            InitTestControls();
            InitExcelControls();
            InitBarcodeControls();
            InitLoginControls();
            InitDebugControls();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitAutoScale();
            InitManualPopup();
        }
    }
}
