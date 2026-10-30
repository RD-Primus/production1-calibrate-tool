// =========================================================================================
// [DEVELOPER GUIDE]
// Ph08TestRecord.cs: "โมเดลโครงสร้างข้อมูลบันทึกผลการทดสอบครบวงจรของ PH-08"
// - รวบรวมผลลัพธ์ของแต่ละ Phase:
//     * Phase 1: PH-08 Port Validation (Read System, Read Map, Write Run, Write Map)
//     * Phase 2: Device Port Trigger Validation (รออ่านเฟรม Function 0x03)
//     * Phase 3: LED Visual Validation (ผลการตรวจสอบไฟทั้ง 6 ดวง)
//     * Phase 4: DIP Switch ID Validation (การทดสอบสับสวิตช์ Step 1 & 2 ครบ 8 bits)
// - มี Property 'OverallPassed' ประเมินผลรวม (ต้องผ่านทุก Phase ถึงจะ Pass)
// - ใช้ส่งต่อระหว่าง Ph08TestRunner -> Form1 UI -> ExcelReportService -> QC Server API
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;

namespace PH_08_Test
{
    public class Ph08TestRecord
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public byte Ph08SlaveId { get; set; }

        // Phase 1: PH-08 Port Validation
        public bool ReadSystemPassed { get; set; }
        public string? ReadSystemFrameHex { get; set; }
        public string ReadSystemStatus { get; set; } = "";

        public bool ReadMapTablePassed { get; set; }
        public string? ReadMapTableFrameHex { get; set; }
        public string ReadMapTableStatus { get; set; } = "";

        public bool WriteRunFlagPassed { get; set; }
        public string? WriteRunFlagEchoHex { get; set; }
        public string WriteRunFlagStatus { get; set; } = "";

        public bool WriteMappingPassed { get; set; }
        public string? WriteMappingEchoHex { get; set; }
        public string WriteMappingStatus { get; set; } = "";

        public bool Phase1_PH08Passed => ReadSystemPassed && ReadMapTablePassed && WriteRunFlagPassed && WriteMappingPassed;

        // Phase 2: Device (Local 485) Trigger Validation
        public bool DeviceTriggerPassed { get; set; }
        public string? DeviceTriggerFrameHex { get; set; }
        public byte? DeviceTriggerReceivedId { get; set; }
        public string DeviceTriggerStatus { get; set; } = "";

        public bool Phase2_DevicePassed => DeviceTriggerPassed;

        // Phase 3: LED Validation
        public bool LedTestPassed { get; set; }
        public int LedProblemCount { get; set; }
        public string LedProblemSummary { get; set; } = "";
        public bool Phase3_LEDPassed => LedTestPassed;

        // Phase 4: DIP Switch ID Validation
        public bool SwitchTestPassed { get; set; }
        public string SwitchTestStatus { get; set; } = "";
        public bool Phase4_SwitchPassed => SwitchTestPassed;

        // Overall
        public bool OverallPassed => Phase1_PH08Passed && Phase2_DevicePassed && Phase3_LEDPassed && Phase4_SwitchPassed;
    }

    public enum PhaseStatus
    {
        Idle,
        Testing,
        Pass,
        Fail
    }
}
