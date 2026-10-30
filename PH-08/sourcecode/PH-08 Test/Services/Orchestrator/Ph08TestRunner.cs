// =========================================================================================
// [DEVELOPER GUIDE]
// Ph08TestRunner.cs: "Main หลักคุมขั้นตอนการทดสอบทั้งหมด (Master Test Sequence)"
// - ไฟล์นี้เป็นแกนกลางในการคุม Flow 4 Phase:
//     * Phase 1: Remote RS-485 (Read System Reg 1..24, Read MapTable, Write RunFlag, Write Mapping)
//     * Phase 2: Local RS-485 (ดักรับ Modbus 0x03 จากสาย Device & ตรวจสอบ ID)
//     * Phase 3: Visual LED Validation (เปิด Dialog FormLEDTest)
//     * Phase 4: DIP Switch ID Validation (เปิด Dialog FormSwitchIDTest)
// - หากต้องการแก้ไขสเต็ป, ปรับ Register, หรือเพิ่ม Phase 5 ให้แก้ไขใน ExecuteMasterTestSequenceAsync()
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace PH_08_Test.Services.Orchestrator
{
    public interface IPh08TestHardwareProvider
    {
        Task<(bool success, byte[]? rxFrame, string message)> ReadPh08RegistersAsync(ushort startRegister, ushort quantity, int timeoutMs, CancellationToken ct);
        Task<(bool success, byte[]? echoFrame, string message)> WritePh08RegistersAsync(ushort startRegister, ushort[] values, int timeoutMs, CancellationToken ct);
        Task<(bool success, byte[]? frame, byte receivedId, string message)> WaitForDeviceTriggerAsync(byte expectedSlaveId, int timeoutMs, CancellationToken ct);
        void ResetDeviceTrigger();
        Task<(bool passed, int problemCount, string problemSummary)> ShowLedModalDialogAsync(CancellationToken ct);
        Task<(bool passed, string status)> ShowSwitchIdModalDialogAsync(CancellationToken ct);
        void UpdateLiveParameterUi(string? devAdr, string? moduleType, string? runFlag);
        void SetPollingPaused(bool paused);
    }

    public class Ph08TestContext
    {
        public byte SlaveId { get; set; } = 1;
        public string SN { get; set; } = "";
        public string Lot { get; set; } = "";
        public string ECN { get; set; } = "";
        public string FW { get; set; } = "";
        public bool IsDebugMode { get; set; } = false;
    }

    public class Ph08TestProgress
    {
        public int PhaseIndex { get; set; } // 1, 2, 3, 4
        public PhaseStatus Status { get; set; }
        public string MainMessage { get; set; } = "";
        public string? LogMessage { get; set; }
        public Color LogColor { get; set; } = Color.Empty;
        public bool IsDebugOnly { get; set; } = false;

        public static Ph08TestProgress Phase(int phase, PhaseStatus status, string msg) =>
            new Ph08TestProgress { PhaseIndex = phase, Status = status, MainMessage = msg };

        public static Ph08TestProgress Log(string log, Color color, bool debugOnly = false) =>
            new Ph08TestProgress { LogMessage = log, LogColor = color, IsDebugOnly = debugOnly };
    }

    public class Ph08TestRunner
    {
        public static readonly Color ColorNormal = Color.FromArgb(40, 40, 40);
        public static readonly Color ColorSuccess = Color.FromArgb(0, 150, 80);
        public static readonly Color ColorFail = Color.FromArgb(200, 30, 30);

        /// <summary>
        /// Main หลักที่เขียนขั้นตอนการทำงานทั้งหมดของ Master Test Sequence
        /// </summary>
        public async Task<Ph08TestRecord> ExecuteMasterTestSequenceAsync(
            Ph08TestContext context,
            IPh08TestHardwareProvider hardware,
            IProgress<Ph08TestProgress>? progress,
            CancellationToken ct)
        {
            var record = new Ph08TestRecord
            {
                Timestamp = DateTime.Now,
                Ph08SlaveId = context.SlaveId
            };

            // ========================================================
            // PHASE 1: ตรวจสอบสายและคำสั่ง PH-08 (Remote RS-485)
            // ========================================================
            hardware.SetPollingPaused(true);
            // รอ 250ms เพื่อให้เฟรม Polling ก่อนหน้าที่อาจกำลังส่งอยู่ในสาย RS-485 ส่งจบและสายเงียบสนิท 100%
            await Task.Delay(250, ct);
            ct.ThrowIfCancellationRequested();
            progress?.Report(Ph08TestProgress.Phase(1, PhaseStatus.Testing, "กำลังทดสอบ Remote line..."));
            progress?.Report(Ph08TestProgress.Log("▶ [Phase 1/4] ตรวจสอบสายและคำสั่ง PH-08 (Remote RS-485)...", ColorNormal, true));

            // 1.1 อ่าน System Block (Reg 1..24, 24 Regs -> 48 Bytes)
            progress?.Report(Ph08TestProgress.Log($"1.1 ขออ่าน System Block (Reg 1, 24 Regs) จาก PH-08 ID {context.SlaveId}...", ColorNormal, true));
            ct.ThrowIfCancellationRequested();
            var (sysOk, sysFrame, sysMsg) = await hardware.ReadPh08RegistersAsync(1, 24, 2000, ct);
            ct.ThrowIfCancellationRequested();

            if (!sysOk || sysFrame == null)
            {
                record.ReadSystemPassed = false;
                record.ReadSystemStatus = $"FAIL: อ่าน System Block ไม่สำเร็จ ({sysMsg})";
                progress?.Report(Ph08TestProgress.Phase(1, PhaseStatus.Fail, "การทดสอบ Remote line ไม่ผ่าน"));
                progress?.Report(Ph08TestProgress.Log($"❌ ไม่ผ่านที่: Phase 1 (Step 1.1 Read System Block - {sysMsg})", ColorFail));
                return record;
            }

            record.ReadSystemPassed = true;
            record.ReadSystemFrameHex = BitConverter.ToString(sysFrame).Replace("-", " ");
            record.ReadSystemStatus = $"PASS (53 Bytes, CRC OK, ID={sysFrame[0]})";
            progress?.Report(Ph08TestProgress.Log($"✔ [Step 1.1 PASS] System Read สำเร็จ: {record.ReadSystemFrameHex}", ColorSuccess, true));

            await Task.Delay(80, ct);

            // 1.2 อ่าน Mapping Table Block (Reg 1000..1063, 64 Regs -> 128 Bytes)
            progress?.Report(Ph08TestProgress.Log($"1.2 ขออ่าน MapTable Block (Reg 1000, 64 Regs) จาก PH-08 ID {context.SlaveId}...", ColorNormal, true));
            ct.ThrowIfCancellationRequested();
            var (mapReadOk, mapReadFrame, mapReadMsg) = await hardware.ReadPh08RegistersAsync(1000, 64, 2500, ct);
            ct.ThrowIfCancellationRequested();

            if (!mapReadOk || mapReadFrame == null)
            {
                record.ReadMapTablePassed = false;
                record.ReadMapTableStatus = $"FAIL: อ่าน Mapping Table Block ไม่สำเร็จ ({mapReadMsg})";
                progress?.Report(Ph08TestProgress.Phase(1, PhaseStatus.Fail, "การทดสอบ Remote line ไม่ผ่าน"));
                progress?.Report(Ph08TestProgress.Log($"❌ ไม่ผ่านที่: Phase 1 (Step 1.2 Read MapTable - {mapReadMsg})", ColorFail));
                return record;
            }

            record.ReadMapTablePassed = true;
            record.ReadMapTableFrameHex = BitConverter.ToString(mapReadFrame).Replace("-", " ");
            record.ReadMapTableStatus = $"PASS (133 Bytes, CRC OK, ID={mapReadFrame[0]})";
            progress?.Report(Ph08TestProgress.Log($"✔ [Step 1.2 PASS] MapTable Read สำเร็จ: {record.ReadMapTableFrameHex}", ColorSuccess, true));

            await Task.Delay(80, ct);

            // 1.3 สั่งเขียนเปิด RunFlag = 1 (Register 1)
            progress?.Report(Ph08TestProgress.Log($"1.3 สั่งเขียน RunFlag = 1 (Reg 1) ไปที่ PH-08 ID {context.SlaveId}...", ColorNormal, true));
            ct.ThrowIfCancellationRequested();
            var (runFlagOk, runFlagEcho, runFlagMsg) = await hardware.WritePh08RegistersAsync(1, new ushort[] { 1 }, 2000, ct);
            ct.ThrowIfCancellationRequested();

            if (!runFlagOk || runFlagEcho == null)
            {
                record.WriteRunFlagPassed = false;
                record.WriteRunFlagStatus = $"FAIL: สั่งเขียน RunFlag ไม่สำเร็จ ({runFlagMsg})";
                progress?.Report(Ph08TestProgress.Phase(1, PhaseStatus.Fail, "การทดสอบ Remote line ไม่ผ่าน"));
                progress?.Report(Ph08TestProgress.Log($"❌ ไม่ผ่านที่: Phase 1 (Step 1.3 Write RunFlag - {runFlagMsg})", ColorFail));
                return record;
            }

            record.WriteRunFlagPassed = true;
            record.WriteRunFlagEchoHex = BitConverter.ToString(runFlagEcho).Replace("-", " ");
            record.WriteRunFlagStatus = "PASS (Write Echo 0x10 OK)";
            progress?.Report(Ph08TestProgress.Log($"✔ [Step 1.3 PASS] เขียน RunFlag สำเร็จ (Echo: {record.WriteRunFlagEchoHex})", ColorSuccess, true));
            hardware.UpdateLiveParameterUi(null, null, "1");

            await Task.Delay(80, ct);

            // 1.4 สั่งเขียน Mapping Slot 1 (Reg 1001 = PH08 ID, Reg 1002 = 1)
            progress?.Report(Ph08TestProgress.Log($"1.4 สั่งเขียน Mapping Slot 1 (Reg 1001 = {context.SlaveId}, Reg 1002 = 1)...", ColorNormal, true));
            // เคลียร์ประวัติเฟรม 0x03 เก่าของสาย Device ก่อนเริ่มเขียน Mapping
            hardware.ResetDeviceTrigger();
            ct.ThrowIfCancellationRequested();
            var (mapWriteOk, mapWriteEcho, mapWriteMsg) = await hardware.WritePh08RegistersAsync(1001, new ushort[] { context.SlaveId, 1 }, 2000, ct);
            ct.ThrowIfCancellationRequested();

            if (!mapWriteOk || mapWriteEcho == null)
            {
                record.WriteMappingPassed = false;
                record.WriteMappingStatus = $"FAIL: สั่งเขียน Mapping Reg 1001/1002 ไม่สำเร็จ ({mapWriteMsg})";
                progress?.Report(Ph08TestProgress.Phase(1, PhaseStatus.Fail, "การทดสอบ Remote line ไม่ผ่าน"));
                progress?.Report(Ph08TestProgress.Log($"❌ ไม่ผ่านที่: Phase 1 (Step 1.4 Write Mapping - {mapWriteMsg})", ColorFail));
                return record;
            }

            record.WriteMappingPassed = true;
            record.WriteMappingEchoHex = BitConverter.ToString(mapWriteEcho).Replace("-", " ");
            record.WriteMappingStatus = "PASS (Write Echo 0x10 OK)";
            progress?.Report(Ph08TestProgress.Log($"✔ [Step 1.4 PASS] เขียน Mapping สำเร็จ (Echo: {record.WriteMappingEchoHex})", ColorSuccess, true));
            hardware.UpdateLiveParameterUi(context.SlaveId.ToString(), "1", null);

            // Phase 1 PASS
            progress?.Report(Ph08TestProgress.Phase(1, PhaseStatus.Pass, "การทดสอบ Remote line ผ่าน"));
            progress?.Report(Ph08TestProgress.Log("✔ [Phase 1 PASS] ตรวจสอบสายและคำสั่ง PH-08 (Remote RS-485) ผ่านสมบูรณ์ครบ 4 ขั้นตอน", ColorSuccess));

            // ========================================================
            // PHASE 2: ตรวจสอบสาย Device / Local 485 (ดักรับ Trigger Frame 0x03)
            // ========================================================
            ct.ThrowIfCancellationRequested();
            progress?.Report(Ph08TestProgress.Phase(2, PhaseStatus.Testing, "กำลังทดสอบ Local line..."));
            progress?.Report(Ph08TestProgress.Log("▶ [Phase 2/4] ตรวจสอบสาย Device (Local RS-485)...", ColorNormal, true));
            progress?.Report(Ph08TestProgress.Log($"⏳ กำลังดักรอรับเฟรม Modbus 0x03 จากสาย Device (Expected ID: {context.SlaveId}, Timeout 3.5s)...", ColorNormal, true));

            var (devOk, devFrame, devRxId, devMsg) = await hardware.WaitForDeviceTriggerAsync(context.SlaveId, 3500, ct);
            ct.ThrowIfCancellationRequested();

            if (!devOk || devFrame == null)
            {
                record.DeviceTriggerPassed = false;
                record.DeviceTriggerStatus = $"FAIL: {devMsg}";
                progress?.Report(Ph08TestProgress.Phase(2, PhaseStatus.Fail, "การทดสอบ Local line ไม่ผ่าน"));
                progress?.Report(Ph08TestProgress.Log($"❌ ไม่ผ่านที่: Phase 2 Device Local RS-485 ({devMsg})", ColorFail));
                return record;
            }

            string remoteHex = BitConverter.ToString(devFrame).Replace("-", " ");
            record.DeviceTriggerFrameHex = remoteHex;
            record.DeviceTriggerReceivedId = devRxId;

            if (devRxId != context.SlaveId)
            {
                record.DeviceTriggerPassed = false;
                record.DeviceTriggerStatus = $"FAIL: ID ไม่ตรง (Expected: {context.SlaveId}, Received: {devRxId})";
                progress?.Report(Ph08TestProgress.Phase(2, PhaseStatus.Fail, "การทดสอบ Local line ไม่ผ่าน"));
                progress?.Report(Ph08TestProgress.Log($"❌ ไม่ผ่านที่: Phase 2 Device Local RS-485 (ID ไม่ตรง) | เฟรม: {remoteHex}", ColorFail));
                return record;
            }

            // Phase 2 PASS
            record.DeviceTriggerPassed = true;
            record.DeviceTriggerStatus = "PASS (รับเฟรมคำสั่ง 0x03 และจำลอง Slave ส่ง Response 0x03 Reg0..1=1 สำเร็จ)";
            progress?.Report(Ph08TestProgress.Phase(2, PhaseStatus.Pass, "การทดสอบ Local line ผ่าน"));
            progress?.Report(Ph08TestProgress.Log($"✔ [Phase 2 PASS] ตรวจพบเฟรมคำสั่งจาก PH-08 ทางสาย Device/Local (ID: {devRxId})", ColorSuccess));

            // ========================================================
            // PHASE 3: ตรวจสอบไฟ LED บนบอร์ด (Popup FormLEDTest)
            // ========================================================
            // กลับมาสั่งอ่าน Modbus Polling ปกติ เพื่อให้ไฟ Remote RX/TX กระพริบทำงานสมบูรณ์ตลอดช่วง Phase 3 (LED Test)
            hardware.SetPollingPaused(false);

            ct.ThrowIfCancellationRequested();
            progress?.Report(Ph08TestProgress.Phase(3, PhaseStatus.Testing, "กำลังทดสอบ LED..."));
            progress?.Report(Ph08TestProgress.Log("▶ [Phase 3/4] ตรวจสอบไฟ LED บนบอร์ด...", ColorNormal, true));

            var (ledPassed, ledProblemCount, ledProblemSummary) = await hardware.ShowLedModalDialogAsync(ct);
            ct.ThrowIfCancellationRequested();

            if (!ledPassed)
            {
                record.LedTestPassed = false;
                record.LedProblemCount = ledProblemCount;
                record.LedProblemSummary = ledProblemSummary;
                progress?.Report(Ph08TestProgress.Phase(3, PhaseStatus.Fail, "การทดสอบ LED ไม่ผ่าน"));
                progress?.Report(Ph08TestProgress.Log($"❌ ไม่ผ่านที่: Phase 3 LED ({ledProblemSummary})", ColorFail));
                return record;
            }

            record.LedTestPassed = true;
            record.LedProblemCount = 0;
            record.LedProblemSummary = "";
            progress?.Report(Ph08TestProgress.Phase(3, PhaseStatus.Pass, "การทดสอบ LED ผ่าน"));
            progress?.Report(Ph08TestProgress.Log("✔ [Phase 3 PASS] ตรวจสอบไฟ LED ผ่านเรียบร้อย", ColorSuccess));

            // ========================================================
            // PHASE 4: ตรวจสอบ DIP Switch ID (Popup FormSwitchIDTest)
            // ========================================================
            // หยุด Polling ชั่วคราวเพื่อให้คำสั่งอ่าน DIP Switch ID ทำงานได้แม่นยำไม่ชนกัน
            hardware.SetPollingPaused(true);

            ct.ThrowIfCancellationRequested();
            progress?.Report(Ph08TestProgress.Phase(4, PhaseStatus.Testing, "กำลังทดสอบ Switch ID..."));
            progress?.Report(Ph08TestProgress.Log("▶ [Phase 4/4] ตรวจสอบ DIP Switch ID...", ColorNormal, true));

            var (swPassed, swStatus) = await hardware.ShowSwitchIdModalDialogAsync(ct);
            ct.ThrowIfCancellationRequested();

            if (!swPassed)
            {
                record.SwitchTestPassed = false;
                record.SwitchTestStatus = swStatus;
                progress?.Report(Ph08TestProgress.Phase(4, PhaseStatus.Fail, "การทดสอบ Switch ID ไม่ผ่าน"));
                progress?.Report(Ph08TestProgress.Log($"❌ ไม่ผ่านที่: Phase 4 DIP Switch ID ({swStatus})", ColorFail));
                return record;
            }

            record.SwitchTestPassed = true;
            record.SwitchTestStatus = swStatus;
            progress?.Report(Ph08TestProgress.Phase(4, PhaseStatus.Pass, "การทดสอบ Switch ID ผ่าน"));
            progress?.Report(Ph08TestProgress.Log("✔ [Phase 4 PASS] ทดสอบ DIP Switch ID ผ่านสมบูรณ์", ColorSuccess));

            // คืนค่า Polling ปกติ
            hardware.SetPollingPaused(false);

            return record;
        }
    }
}
