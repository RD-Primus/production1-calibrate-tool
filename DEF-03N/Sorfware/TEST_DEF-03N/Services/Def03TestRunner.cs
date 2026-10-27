// =========================================================================================
// [DEVELOPER GUIDE]
// Def03TestRunner.cs: "★ Main Orchestrator - ผู้บัญชาการขั้นตอนการทดสอบ DEF-03N ทั้งหมด"
// - ควบคุมลำดับการทดสอบทั้งหมดจากบนลงล่างอย่างชัดเจน (Master Test Sequence):
//     * Step 1: ตรวจสอบความพร้อมของพอร์ต (DEF Serial, Ohm Port, Relay Port) และข้อมูล Input (S/N, LOT, Model)
//     * Step 2: ดำเนินการ Calibration (หากเลือกโหมด Calibrate)
//     * Step 3: ดำเนินการทดสอบ QC ตาม Model (Model A หรือ Model AB) รวมถึงการทดสอบ Relay, Alarm, และ Temperature
//     * Step 4: ประสานงานกับ ExcelReportService เพื่อบันทึกผลลงรายงาน
//     * Step 5: ประสานงานกับ ServerApiService เพื่อส่งผลขึ้นระบบ QC Server
// - ส่งสถานะความคืบหน้าผ่าน IProgress<Def03TestProgress> เพื่อให้อัปเดต UI ได้อย่างปลอดภัย
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;
using System.Threading;
using System.Threading.Tasks;

namespace TEST_DEF_03N.Services
{
    public class Def03TestContext
    {
        public string Model { get; set; } = "";
        public string Supply { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string LotNumber { get; set; } = "";
        public string EcnNumber { get; set; } = "";
        public string OperatorName { get; set; } = "";
        public string TemplatePath { get; set; } = "";
        public bool DoCalibration { get; set; }
        public bool DoTestForAll { get; set; }
        public bool IsQaMode { get; set; }
    }

    public class Def03TestProgress
    {
        public int StepNumber { get; }
        public string Message { get; }
        public bool IsSuccess { get; }
        public bool IsCompleted { get; }

        public Def03TestProgress(int stepNumber, string message, bool isSuccess = true, bool isCompleted = false)
        {
            StepNumber = stepNumber;
            Message = message;
            IsSuccess = isSuccess;
            IsCompleted = isCompleted;
        }
    }

    public class Def03TestResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = "";
        public bool CalibrationPassed { get; set; }
        public bool ModelQcPassed { get; set; }
        public bool ExcelSaved { get; set; }
        public bool ServerUploaded { get; set; }
    }

    /// <summary>
    /// Interface สำหรับให้ Form1 ส่งต่อการควบคุมการสื่อสารและการทดสอบให้กับ Orchestrator
    /// </summary>
    public interface IDef03TestDelegate
    {
        bool CheckAllPortsOpen();
        Task<bool> ExecuteCalibrationAsync(CancellationToken ct);
        Task<bool> ExecuteModelQcAsync(CancellationToken ct);
        bool SaveExcelReport();
        Task<bool> UploadToServerAsync();
    }

    public class Def03TestRunner
    {
        private readonly ExcelReportService _excelService;

        public Def03TestRunner(ExcelReportService? excelService = null)
        {
            _excelService = excelService ?? new ExcelReportService();
        }

        /// <summary>
        /// =========================================================================================
        /// ★ MASTER TEST SEQUENCE: ลำดับขั้นตอนการทดสอบหลัก เรียงจากบนลงล่าง
        /// =========================================================================
        /// </summary>
        public async Task<Def03TestResult> ExecuteMasterSequenceAsync(
            Def03TestContext context,
            IDef03TestDelegate testDelegate,
            IProgress<Def03TestProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new Def03TestResult();

            try
            {
                // [STEP 1] : ตรวจสอบความพร้อมของพอร์ตและการตั้งค่า
                progress?.Report(new Def03TestProgress(1, "กำลังตรวจสอบความพร้อมของอุปกรณ์และพอร์ตเชื่อมต่อ..."));
                if (!testDelegate.CheckAllPortsOpen())
                {
                    result.IsSuccess = false;
                    result.Message = "พอร์ตเชื่อมต่อ (DEF / Ohm / Relay) ไม่พร้อมใช้งาน";
                    progress?.Report(new Def03TestProgress(1, result.Message, isSuccess: false, isCompleted: true));
                    return result;
                }

                // ตรวจสอบความถูกต้องของ Input และไฟล์ Excel ปลายทาง
                if (!context.IsQaMode)
                {
                    if (!_excelService.ValidateBeforeTest(context.TemplatePath, context.SerialNumber, context.LotNumber, out string? valErr))
                    {
                        result.IsSuccess = false;
                        result.Message = valErr ?? "ข้อมูลไม่ครบถ้วน";
                        progress?.Report(new Def03TestProgress(1, result.Message, isSuccess: false, isCompleted: true));
                        return result;
                    }

                    if (!_excelService.EnsureReportFileClosed(context.TemplatePath, context.LotNumber, out string? fileErr))
                    {
                        result.IsSuccess = false;
                        result.Message = fileErr ?? "ไฟล์รายงานเปิดค้างอยู่";
                        progress?.Report(new Def03TestProgress(1, result.Message, isSuccess: false, isCompleted: true));
                        return result;
                    }
                }

                // [STEP 2] : ทำการ Calibration (หากเลือกไว้)
                if (context.DoCalibration)
                {
                    progress?.Report(new Def03TestProgress(2, "กำลังดำเนินการ Calibration..."));
                    bool calOk = await testDelegate.ExecuteCalibrationAsync(cancellationToken);
                    result.CalibrationPassed = calOk;

                    if (!calOk)
                    {
                        result.IsSuccess = false;
                        result.Message = "Calibration ล้มเหลวหรือไม่สมบูรณ์";
                        progress?.Report(new Def03TestProgress(2, result.Message, isSuccess: false, isCompleted: true));
                        return result;
                    }
                    progress?.Report(new Def03TestProgress(2, "Calibration ผ่านเรียบร้อยแล้ว"));
                }

                // [STEP 3] : ทำการทดสอบ QC ตาม Model (หากเลือกไว้)
                if (context.DoTestForAll)
                {
                    progress?.Report(new Def03TestProgress(3, $"กำลังดำเนินการทดสอบ QC สำหรับ {context.Model} ({context.Supply})..."));
                    bool qcOk = await testDelegate.ExecuteModelQcAsync(cancellationToken);
                    result.ModelQcPassed = qcOk;

                    if (!qcOk)
                    {
                        result.IsSuccess = false;
                        result.Message = "การทดสอบ QC ไม่ผ่านเกณฑ์";
                        progress?.Report(new Def03TestProgress(3, result.Message, isSuccess: false, isCompleted: true));
                        return result;
                    }
                    progress?.Report(new Def03TestProgress(3, "การทดสอบ QC ผ่านเกณฑ์ทั้งหมด"));
                }

                // [STEP 4] : บันทึกรายงาน Excel
                progress?.Report(new Def03TestProgress(4, "กำลังบันทึกผลการทดสอบลงไฟล์ Excel..."));
                bool excelOk = testDelegate.SaveExcelReport();
                result.ExcelSaved = excelOk;

                // [STEP 5] : ส่งผลขึ้นระบบ QC Server
                progress?.Report(new Def03TestProgress(5, "กำลังส่งผลการทดสอบขึ้นระบบ QC Server..."));
                bool serverOk = await testDelegate.UploadToServerAsync();
                result.ServerUploaded = serverOk;

                result.IsSuccess = true;
                result.Message = "ผ่านการทดสอบสมบูรณ์ทุกขั้นตอน";
                progress?.Report(new Def03TestProgress(5, result.Message, isSuccess: true, isCompleted: true));
                return result;
            }
            catch (OperationCanceledException)
            {
                result.IsSuccess = false;
                result.Message = "การทดสอบถูกยกเลิกโดยผู้ใช้งาน";
                progress?.Report(new Def03TestProgress(0, result.Message, isSuccess: false, isCompleted: true));
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"เกิดข้อผิดพลาดในการทดสอบ: {ex.Message}";
                progress?.Report(new Def03TestProgress(0, result.Message, isSuccess: false, isCompleted: true));
                return result;
            }
        }
    }
}
