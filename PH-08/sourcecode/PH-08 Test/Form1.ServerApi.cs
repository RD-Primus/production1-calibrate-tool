// =========================================================================================
// [DEVELOPER GUIDE]
// Form1.ServerApi.cs: "การส่งข้อมูลและผลการทดสอบ QC ไปยัง Backend Server ผ่าน HTTP REST API"
// - รองรับการส่งผลการทดสอบ (POST /qc-report หรือ endpoint ที่กำหนด)
// - มีระบบ Auto-retry, Timeout, และส่งสถานะกลับมาอัปเดต Badge บนหน้าจอ
// - รวบรวมข้อมูล Payload จาก Ph08TestRecord และหน้าจอ Form1
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using PH_08_Test.Services.Reporting;

namespace PH_08_Test
{
    public partial class Form1
    {
        private static readonly HttpClient ServerApiClient = new HttpClient();
        private bool _serverPostInProgress = false;

        public enum ServerPostResult
        {
            Skipped,
            Success,
            Timeout,
            Failed
        }

        private static readonly Dictionary<string, string> PassFailCheckmarkMap = new Dictionary<string, string>
        {
            { "PASS", "✔" },
            { "FAIL", "✘" },
            { "PENDING", "-" },
            { "SKIP", "N/A" }
        };

        private static Dictionary<string, string> CreateStandardValueMap() => PassFailCheckmarkMap;

        private QcPayloadV2 BuildQcPayloadV2(Ph08TestRecord record, DateTime startTime, DateTime endTime)
        {
            string sn = txt_SN.Text.Trim();
            string lot = txt_Lot.Text.Trim();
            string ecn = txt_ECN.Text.Trim();
            string fw = txt_FW.Text.Trim();
            string operatorUser = !string.IsNullOrWhiteSpace(_operatorEmail)
                ? _operatorEmail
                : (!string.IsNullOrWhiteSpace(_operatorName) ? _operatorName : "operator");

            const string timeFormat = "yyyy-MM-dd'T'HH:mm:sszzz";
            DateTimeOffset startOffset = new DateTimeOffset(startTime);
            DateTimeOffset endOffset = new DateTimeOffset(endTime);
            string startStr = startOffset.ToString(timeFormat);
            string endStr = endOffset.ToString(timeFormat);
            string reportDate = endTime.ToString("yyyy-MM-dd");
            string fileName = !string.IsNullOrWhiteSpace(lot) ? $"{ExcelReportService.GetSafeLotFileName(lot)}.xlsx" : "PH-08 QC Inspection.xlsx";

            int testedCount = GetTestedSerialCount();
            int lotSize = testedCount > 0 ? testedCount : 1;

            bool overall = record.OverallPassed;
            string remark = overall
                ? "Final assembly functional inspection completed: PASS"
                : BuildFailRemark(record);

            bool p1Ran = !string.IsNullOrEmpty(record.ReadSystemStatus);
            string p1Result = !p1Ran ? "PENDING" : (record.Phase1_PH08Passed ? "PASS" : "FAIL");
            string p1Text = p1Result == "PASS" ? "✔" : (p1Result == "FAIL" ? "✘" : "-");

            bool p2Ran = !string.IsNullOrEmpty(record.DeviceTriggerStatus);
            string p2Result = !p2Ran ? "PENDING" : (record.Phase2_DevicePassed ? "PASS" : "FAIL");
            string p2Text = p2Result == "PASS" ? "✔" : (p2Result == "FAIL" ? "✘" : "-");

            bool p3Ran = record.LedTestPassed || !string.IsNullOrEmpty(record.LedProblemSummary);
            string p3Result = !p3Ran ? "PENDING" : (record.Phase3_LEDPassed ? "PASS" : "FAIL");
            string p3Text = p3Result == "PASS" ? "✔" : (p3Result == "FAIL" ? "✘" : "-");

            bool p4Ran = !string.IsNullOrEmpty(record.SwitchTestStatus);
            string p4Result = !p4Ran ? "PENDING" : (record.Phase4_SwitchPassed ? "PASS" : "FAIL");
            string p4Text = p4Result == "PASS" ? "✔" : (p4Result == "FAIL" ? "✘" : "-");

            var payload = new QcPayloadV2
            {
                SchemaVersion = 2,
                UpdateMode = false,
                ModelCode = "PH-08",
                LotNumber = lot,
                EcnNumber = ecn,
                SerialNumber = sn,
                StationName = "QC-STATION-01",
                OperatorUsername = operatorUser,
                StartTime = startStr,
                InspectionTime = endStr,
                TemplateName = "PH-08 QC Inspection.xlsx",
                Description = "PH-08 I/O MAPPER Functional and Communication Inspection",
                Firmware = new List<QcFirmware>
                {
                    new QcFirmware { Name = "PH-08", Version = fw }
                },
                PowerSupply = "9-30 VDC",
                EquipmentCode = new List<QcEquipment>
                {
                    new QcEquipment
                    {
                        Name = "DC Power Supply",
                        Series = "9-30VDC",
                        EquipmentId = "PS-01"
                    }
                },
                Approve = new QcApprove
                {
                    Tester = new QcSignature
                    {
                        Name = !string.IsNullOrWhiteSpace(_operatorName) ? _operatorName : operatorUser,
                        Datetime = endStr
                    },
                    Confirm = new QcSignature { Name = null, Datetime = null },
                    Approver = new QcSignature { Name = null, Datetime = null }
                },
                Remark = remark,
                Uuid = Guid.NewGuid().ToString(),
                InspectionStatus = "final",
                ProductName = "I/O MAPPER",
                ReportSerialNumber = sn,
                JigName = "PH08-FUNCTIONAL-JIG-01",
                ReportDate = reportDate,
                OverallResult = overall ? "PASS" : "FAIL",
                LotSize = lotSize,
                AcceptanceChecks = new List<QcAcceptanceCheck>
                {
                    new QcAcceptanceCheck
                    {
                        CheckCode = "POWER_SUPPLY",
                        Name = "Power Supply 9-30 VDC",
                        Checked = true,
                        ReportMapping = new List<QcCheckReportMapping>
                        {
                            new QcCheckReportMapping
                            {
                                TargetType = "cell",
                                SearchKey = "Power Supply",
                                MatchMode = "contains",
                                Offset = new QcOffset { X = -1, Y = 0 },
                                ValueField = "checked",
                                WriteMode = "value_map",
                                ValueMap = new Dictionary<string, string> { { "true", "☑" }, { "false", "☐" } }
                            }
                        }
                    },
                    new QcAcceptanceCheck
                    {
                        CheckCode = "FW",
                        Name = "Firmware Version",
                        Checked = true,
                        ReportMapping = new List<QcCheckReportMapping>
                        {
                            new QcCheckReportMapping
                            {
                                TargetType = "cell",
                                SearchKey = "FW:",
                                MatchMode = "contains",
                                Offset = new QcOffset { X = -1, Y = 0 },
                                ValueField = "checked",
                                WriteMode = "value_map",
                                ValueMap = new Dictionary<string, string> { { "true", "☑" }, { "false", "☐" } }
                            }
                        }
                    },
                    new QcAcceptanceCheck
                    {
                        CheckCode = "ECN",
                        Name = "ECN NO",
                        Checked = true,
                        ReportMapping = new List<QcCheckReportMapping>
                        {
                            new QcCheckReportMapping
                            {
                                TargetType = "cell",
                                SearchKey = "ECN",
                                MatchMode = "contains",
                                Offset = new QcOffset { X = -1, Y = 0 },
                                ValueField = "checked",
                                WriteMode = "value_map",
                                ValueMap = new Dictionary<string, string> { { "true", "☑" }, { "false", "☐" } }
                            }
                        }
                    }
                },
                Items = new List<QcItem>
                {
                    new QcItem
                    {
                        ItemCode = "REMOTE_LINE",
                        TestName = "สายสื่อสาร Remote",
                        Required = true,
                        MeasuredText = p1Text,
                        MeasuredValue = null,
                        MinValue = null,
                        MaxValue = null,
                        Unit = null,
                        Result = p1Result,
                        ReportMapping = new List<QcItemReportMapping>
                        {
                            new QcItemReportMapping
                            {
                                TargetType = "table_cell",
                                TableKey = "inspection_items",
                                ColumnPath = new List<string> { "สายสื่อสาร Remote" },
                                ColumnAnchor = "merge_top_left",
                                Offset = new QcOffset { X = 0, Y = 0 },
                                ValueField = "result",
                                WriteMode = "value_map",
                                ValueMap = CreateStandardValueMap()
                            }
                        }
                    },
                    new QcItem
                    {
                        ItemCode = "LOCAL_LINE",
                        TestName = "สายสื่อสาร Local",
                        Required = true,
                        MeasuredText = p2Text,
                        MeasuredValue = null,
                        MinValue = null,
                        MaxValue = null,
                        Unit = null,
                        Result = p2Result,
                        ReportMapping = new List<QcItemReportMapping>
                        {
                            new QcItemReportMapping
                            {
                                TargetType = "table_cell",
                                TableKey = "inspection_items",
                                ColumnPath = new List<string> { "สายสื่อสาร Local" },
                                ColumnAnchor = "merge_top_left",
                                Offset = new QcOffset { X = 0, Y = 0 },
                                ValueField = "result",
                                WriteMode = "value_map",
                                ValueMap = CreateStandardValueMap()
                            }
                        }
                    },
                    new QcItem
                    {
                        ItemCode = "LED_DISPLAY",
                        TestName = "ไฟแสดงผล",
                        Required = true,
                        MeasuredText = p3Text,
                        MeasuredValue = null,
                        MinValue = null,
                        MaxValue = null,
                        Unit = null,
                        Result = p3Result,
                        ReportMapping = new List<QcItemReportMapping>
                        {
                            new QcItemReportMapping
                            {
                                TargetType = "table_cell",
                                TableKey = "inspection_items",
                                ColumnPath = new List<string> { "ไฟแสดงผล" },
                                ColumnAnchor = "merge_top_left",
                                Offset = new QcOffset { X = 0, Y = 0 },
                                ValueField = "result",
                                WriteMode = "value_map",
                                ValueMap = CreateStandardValueMap()
                            }
                        }
                    },
                    new QcItem
                    {
                        ItemCode = "SWITCH_ID",
                        TestName = "สลับเปลี่ยน ID",
                        Required = true,
                        MeasuredText = p4Text,
                        MeasuredValue = null,
                        MinValue = null,
                        MaxValue = null,
                        Unit = null,
                        Result = p4Result,
                        ReportMapping = new List<QcItemReportMapping>
                        {
                            new QcItemReportMapping
                            {
                                TargetType = "table_cell",
                                TableKey = "inspection_items",
                                ColumnPath = new List<string> { "สลับเปลี่ยน ID" },
                                ColumnAnchor = "merge_top_left",
                                Offset = new QcOffset { X = 0, Y = 0 },
                                ValueField = "result",
                                WriteMode = "value_map",
                                ValueMap = CreateStandardValueMap()
                            }
                        }
                    }
                },
                ReportMapping = BuildReportMappingConfig()
            };

            return payload;
        }

        private QcReportMappingConfig BuildReportMappingConfig()
        {
            var funcValueMap = new Dictionary<string, string>
            {
                { "PASS", "✔" },
                { "FAIL", "✘" },
                { "PENDING", "-" },
                { "SKIP", "N/A" }
            };

            var itemMappings = new List<QcItemMapping>
            {
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "REMOTE_LINE" },
                    ItemCode = "REMOTE_LINE",
                    DisplayName = "สายสื่อสาร Remote",
                    TargetColumn = "B",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = null,
                    EmptyValue = "",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = funcValueMap
                },
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "LOCAL_LINE" },
                    ItemCode = "LOCAL_LINE",
                    DisplayName = "สายสื่อสาร Local",
                    TargetColumn = "D",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = null,
                    EmptyValue = "",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = funcValueMap
                },
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "LED_DISPLAY" },
                    ItemCode = "LED_DISPLAY",
                    DisplayName = "ไฟแสดงผล",
                    TargetColumn = "F",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = null,
                    EmptyValue = "",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = funcValueMap
                },
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "SWITCH_ID" },
                    ItemCode = "SWITCH_ID",
                    DisplayName = "สลับเปลี่ยน ID",
                    TargetColumn = "J",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = null,
                    EmptyValue = "",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = funcValueMap
                }
            };

            var headerMappings = new List<QcHeaderMapping>
            {
                // Row 2: Model, Lot, Lot Size, Date
                new QcHeaderMapping
                {
                    Field = "model_code",
                    TargetCell = "D2",
                    WriteMode = "replace_full_text",
                    Prefix = "Model : ",
                    Fallback = "",
                    Required = true
                },
                new QcHeaderMapping
                {
                    Field = "lot_number",
                    TargetCell = "H2",
                    WriteMode = "replace_full_text",
                    Prefix = "Lot : ",
                    Fallback = "",
                    Required = true
                },
                new QcHeaderMapping
                {
                    Field = "lot_size",
                    TargetCell = "L2",
                    WriteMode = "replace_full_text",
                    Prefix = "Lot Size : ",
                    Fallback = "",
                    Required = false
                },
                new QcHeaderMapping
                {
                    Field = "inspection_date",
                    TargetCell = "P2",
                    WriteMode = "replace_full_text",
                    Prefix = "Date : ",
                    Fallback = "",
                    Required = true
                },

                // Row 4: Power Supply, FW, ECN
                new QcHeaderMapping
                {
                    Field = "powersupply_check",
                    TargetCell = "A4",
                    WriteMode = "value_only",
                    Fallback = "☑",
                    Required = false
                },
                new QcHeaderMapping
                {
                    Field = "powersupply",
                    TargetCell = "B4",
                    WriteMode = "replace_full_text",
                    Prefix = "Power Supply ",
                    Fallback = "",
                    Required = false
                },
                new QcHeaderMapping
                {
                    Field = "fw_check",
                    TargetCell = "E4",
                    WriteMode = "value_only",
                    Fallback = "☑",
                    Required = false
                },
                new QcHeaderMapping
                {
                    Field = "firmware",
                    TargetCell = "F4",
                    WriteMode = "replace_full_text",
                    Prefix = "FW: ",
                    Fallback = "",
                    Required = false
                },
                new QcHeaderMapping
                {
                    Field = "ecn_check",
                    TargetCell = "M4",
                    WriteMode = "value_only",
                    Fallback = "☑",
                    Required = false
                },
                new QcHeaderMapping
                {
                    Field = "ecn_number",
                    TargetCell = "N4",
                    WriteMode = "replace_full_text",
                    Prefix = "ECN NO: ",
                    Fallback = "",
                    Required = false
                },

                // Row 55: Summary (Q'ty, Good, Defect, % Tield)
                new QcHeaderMapping
                {
                    Field = "qty",
                    TargetCell = "C55",
                    WriteMode = "value_only",
                    Prefix = "",
                    Fallback = "0",
                    Required = true
                },
                new QcHeaderMapping
                {
                    Field = "good",
                    TargetCell = "D55",
                    WriteMode = "value_only",
                    Prefix = "",
                    Fallback = "0",
                    Required = true
                },
                new QcHeaderMapping
                {
                    Field = "defect",
                    TargetCell = "E55",
                    WriteMode = "value_only",
                    Prefix = "",
                    Fallback = "0",
                    Required = true
                },
                new QcHeaderMapping
                {
                    Field = "yield",
                    TargetCell = "F55",
                    WriteMode = "value_only",
                    Prefix = "",
                    Fallback = "0",
                    Required = true
                }
            };

            return new QcReportMappingConfig
            {
                TemplateId = "PH08_F_PD07_R7",
                TemplateSource = "PH-08 QC Inspection.xlsx",
                SheetName = "Sheet1",
                MappingStrategy = "server_template_settings",
                OutputMode = "server_template_settings",
                DataScope = "selected_inspections",
                Revision = !string.IsNullOrWhiteSpace(txt_Rev.Text) ? txt_Rev.Text.Trim() : "F-PD-07 Rev.6",
                ServerTemplateSettings = new QcServerTemplateSettings
                {
                    DataRows = new List<QcDataRowRange>
                    {
                        new QcDataRowRange { From = 7, To = 52 }
                    },
                    MaxRecordsPerSheet = 46,
                    OverflowStrategy = "NEW_SHEET",
                    PageLayout = new QcPageLayout
                    {
                        FreezePanes = true,
                        HeaderMode = "REPEAT",
                        HeaderRows = new QcHeaderRows { From = 1, To = 6 }
                    },
                    SequenceColumn = "A",
                    SerialNumberColumn = "P",
                    ResultMapping = new QcResultMapping
                    {
                        PassColumn = "M",
                        FailColumn = "N",
                        Mark = "✔",
                        SourceField = "overall_result",
                        UnselectedValue = ""
                    },
                    GroupBy = new List<string> { "model_code", "lot_number", "inspection_date" },
                    SortBy = new List<string> { "inspection_datetime", "id" },
                    Timezone = "Asia/Bangkok",
                    FileNamePattern = "{inspection_date}_{lot_size}_{model_code}_{lot_number}_{document_code}-R{revision}.xlsx",
                    HeaderMappings = headerMappings,
                    ItemMappings = itemMappings
                }
            };
        }

        private int GetTestedSerialCount()
        {
            string folder = txt_folder.Text.Trim();
            string lot = txt_Lot.Text.Trim();
            return _excelService.GetTestedSerialCount(folder, lot);
        }

        private static string BuildFailRemark(Ph08TestRecord record)
        {
            var failedPhases = new List<string>();
            if (!record.Phase1_PH08Passed) failedPhases.Add("สายสื่อสาร Remote");
            if (!record.Phase2_DevicePassed) failedPhases.Add("สายสื่อสาร Local");
            if (!record.Phase3_LEDPassed) failedPhases.Add("ไฟแสดงผล (LED)");
            if (!record.Phase4_SwitchPassed) failedPhases.Add("สลับเปลี่ยน ID (DIP Switch)");

            string reasonText = failedPhases.Count > 0 ? string.Join(", ", failedPhases) : "Unknown Error";
            return $"Final assembly functional inspection completed: FAIL - ไม่ผ่านการทดสอบ: {reasonText}";
        }

        public async Task<ServerPostResult> PostQcResultAsync(Ph08TestRecord record, DateTime startTime, DateTime endTime)
        {
            if (_serverPostInProgress)
            {
                LogMessage("⏳ [SERVER] กำลังส่งข้อมูลก่อนหน้าอยู่ กรุณารอสักครู่...", Color.DarkOrange);
                return ServerPostResult.Skipped;
            }

            if (string.IsNullOrWhiteSpace(_serverIp))
            {
                LogMessage("✖ [SERVER] ไม่พบการกำหนด Server IP กรุณา Login เพื่อกำหนด Server ก่อน", LogFailColor);
                return ServerPostResult.Failed;
            }

            int port = _serverPort > 0 ? _serverPort : 8080;
            string qcResultApiUrl = new UriBuilder("http", _serverIp, port, "/api/qc/result").Uri.AbsoluteUri;

            QcPayloadV2 payload;
            try
            {
                payload = BuildQcPayloadV2(record, startTime, endTime);
            }
            catch (Exception ex)
            {
                LogMessage($"✖ [SERVER] เตรียมข้อมูลไม่สำเร็จ: {ex.Message}", LogFailColor);
                return ServerPostResult.Failed;
            }

            string jsonBody = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            _serverPostInProgress = true;
            try
            {
                LogMessage($"📡 [SERVER] กำลังส่งผล S/N: {payload.SerialNumber} ไปยัง Server ({_serverIp}:{port})...", Color.DodgerBlue);

                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Post, qcResultApiUrl);

                if (!string.IsNullOrWhiteSpace(_serverTokenKey))
                {
                    request.Headers.TryAddWithoutValidation("X-API-KEY", _serverTokenKey);
                }

                if (!string.IsNullOrWhiteSpace(_operatorToken))
                {
                    request.Headers.TryAddWithoutValidation("X-OPERATOR-TOKEN", _operatorToken);
                }

                request.Content = content;

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using HttpResponseMessage response = await ServerApiClient.SendAsync(request, timeout.Token);
                string responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    LogMessage($"✔ [SERVER] ส่งข้อมูล S/N: {payload.SerialNumber} ไปยัง Server สำเร็จ", LogSuccessColor);
                    return ServerPostResult.Success;
                }
                else
                {
                    LogMessage($"✖ [SERVER ERROR {(int)response.StatusCode}] {response.StatusCode}: {responseText}", LogFailColor);
                    return ServerPostResult.Failed;
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("✖ [SERVER TIMEOUT] Server ไม่ตอบกลับภายใน 10 วินาที", LogFailColor);
                return ServerPostResult.Timeout;
            }
            catch (Exception ex)
            {
                LogMessage($"✖ [SERVER ERROR] เชื่อมต่อ Server ไม่ได้: {ex.Message}", LogFailColor);
                return ServerPostResult.Failed;
            }
            finally
            {
                _serverPostInProgress = false;
            }
        }
    }
}
