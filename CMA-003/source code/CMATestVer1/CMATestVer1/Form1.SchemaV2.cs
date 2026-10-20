using System;
using System.Collections.Generic;
using System.Globalization;

namespace CMATestVer1
{
    public partial class Form1
    {
        private static List<QcAcceptanceCheck> BuildCmaAcceptanceChecks()
        {
            return new List<QcAcceptanceCheck>
            {
                new QcAcceptanceCheck
                {
                    CheckCode = "OS_270_1_4_27_PH_07_PH_01_MAGNETIC_CONTACTOR_S_T",
                    Name = "เครื่องมือ OS-270 วันหมดอายุ 1/4/27, PH-07, PH-01, Magnetic Contactor S-T",
                    Checked = true,
                    ReportMapping = new List<QcCheckReportMapping>
                    {
                        new QcCheckReportMapping
                        {
                            MappingStrategy = "worksheet_search",
                            SearchKey = "เครื่องมือ OS-270 วันหมดอายุ 1/4/27, PH-07, PH-01, Magnetic Contactor S-T",
                            MatchMode = "exact",
                            Offset = new QcOffset { X = -1, Y = 0 },
                            ValueField = "checked",
                            WriteMode = "value_map",
                            ValueMap = new Dictionary<string, string>
                            {
                                { "true", "☑" },
                                { "false", "☐" }
                            }
                        }
                    }
                },
                new QcAcceptanceCheck
                {
                    CheckCode = "HP_ALARM_WL_ALARM",
                    Name = "ไฟแสดงการทำงาน HP Alarm, WL Alarm",
                    Checked = true,
                    ReportMapping = new List<QcCheckReportMapping>
                    {
                        new QcCheckReportMapping
                        {
                            MappingStrategy = "worksheet_search",
                            SearchKey = "ไฟแสดงการทำงาน HP Alarm, WL Alarm",
                            MatchMode = "exact",
                            Offset = new QcOffset { X = -1, Y = 0 },
                            ValueField = "checked",
                            WriteMode = "value_map",
                            ValueMap = new Dictionary<string, string>
                            {
                                { "true", "☑" },
                                { "false", "☐" }
                            }
                        }
                    }
                },
                new QcAcceptanceCheck
                {
                    CheckCode = "RELAY_1_20A_220_VAC",
                    Name = "มี Relay ใช้งาน 1 ชุด ขนาด 20A /220 VAC",
                    Checked = true,
                    ReportMapping = new List<QcCheckReportMapping>
                    {
                        new QcCheckReportMapping
                        {
                            MappingStrategy = "worksheet_search",
                            SearchKey = "มี Relay ใช้งาน 1 ชุด ขนาด 20A /220 VAC",
                            MatchMode = "exact",
                            Offset = new QcOffset { X = -1, Y = 0 },
                            ValueField = "checked",
                            WriteMode = "value_map",
                            ValueMap = new Dictionary<string, string>
                            {
                                { "true", "☑" },
                                { "false", "☐" }
                            }
                        }
                    }
                },
                new QcAcceptanceCheck
                {
                    CheckCode = "ECN_NO_RD_CN_18_0023",
                    Name = "ECN NO: RD-CN-18-0023",
                    Checked = true,
                    ReportMapping = new List<QcCheckReportMapping>
                    {
                        new QcCheckReportMapping
                        {
                            MappingStrategy = "worksheet_search",
                            SearchKey = "ECN NO: RD-CN-18-0023",
                            MatchMode = "exact",
                            Offset = new QcOffset { X = -1, Y = 0 },
                            ValueField = "checked",
                            WriteMode = "value_map",
                            ValueMap = new Dictionary<string, string>
                            {
                                { "true", "☑" },
                                { "false", "☐" }
                            }
                        }
                    }
                },
                new QcAcceptanceCheck
                {
                    CheckCode = "RELAY_OUT_ON",
                    Name = "ไฟแสดงการทำงาน Relay OUT ON",
                    Checked = true,
                    ReportMapping = new List<QcCheckReportMapping>
                    {
                        new QcCheckReportMapping
                        {
                            MappingStrategy = "worksheet_search",
                            SearchKey = "ไฟแสดงการทำงาน Relay OUT ON",
                            MatchMode = "exact",
                            Offset = new QcOffset { X = -1, Y = 0 },
                            ValueField = "checked",
                            WriteMode = "value_map",
                            ValueMap = new Dictionary<string, string>
                            {
                                { "true", "☑" },
                                { "false", "☐" }
                            }
                        }
                    }
                },
                new QcAcceptanceCheck
                {
                    CheckCode = "RELAY_ALARM_2_ALARM_WL_HP_LIMIT",
                    Name = "มี Relay Alarm 2 ชุด ใช้แจ้งเตือนสัญญาณ Alarm (WL,HP),(Limit)",
                    Checked = true,
                    ReportMapping = new List<QcCheckReportMapping>
                    {
                        new QcCheckReportMapping
                        {
                            MappingStrategy = "worksheet_search",
                            SearchKey = "มี Relay  Alarm 2 ชุด ใช้แจ้งเตือนสัญญาณ Alarm (WL,HP),(Limit)",
                            MatchMode = "exact",
                            Offset = new QcOffset { X = -1, Y = 0 },
                            ValueField = "checked",
                            WriteMode = "value_map",
                            ValueMap = new Dictionary<string, string>
                            {
                                { "true", "☑" },
                                { "false", "☐" }
                            }
                        }
                    }
                },
                new QcAcceptanceCheck
                {
                    CheckCode = "AIRCONTROL_C_V1_02_D43_255_KB",
                    Name = "AirControl c v1.02.d43(255 KB)",
                    Checked = true,
                    ReportMapping = new List<QcCheckReportMapping>
                    {
                        new QcCheckReportMapping
                        {
                            MappingStrategy = "worksheet_search",
                            SearchKey = "AirControl c v1.02.d43(255 KB)",
                            MatchMode = "exact",
                            Offset = new QcOffset { X = -1, Y = 0 },
                            ValueField = "checked",
                            WriteMode = "value_map",
                            ValueMap = new Dictionary<string, string>
                            {
                                { "true", "☑" },
                                { "false", "☐" }
                            }
                        }
                    }
                },
                new QcAcceptanceCheck
                {
                    CheckCode = "DISPLAY",
                    Name = "ไฟแสดงผล Display ทำงาน, ระบบอ่านค่าได้ถูกต้อง",
                    Checked = true,
                    ReportMapping = new List<QcCheckReportMapping>
                    {
                        new QcCheckReportMapping
                        {
                            MappingStrategy = "worksheet_search",
                            SearchKey = "ไฟแสดงผล Display ทำงาน, ระบบอ่านค่าได้ถูกต้อง",
                            MatchMode = "exact",
                            Offset = new QcOffset { X = -1, Y = 0 },
                            ValueField = "checked",
                            WriteMode = "value_map",
                            ValueMap = new Dictionary<string, string>
                            {
                                { "true", "☑" },
                                { "false", "☐" }
                            }
                        }
                    }
                },
                new QcAcceptanceCheck
                {
                    CheckCode = "SUPPLY_220VAC_50HZ",
                    Name = "Supply 220VAC 50Hz",
                    Checked = true,
                    ReportMapping = new List<QcCheckReportMapping>
                    {
                        new QcCheckReportMapping
                        {
                            MappingStrategy = "worksheet_search",
                            SearchKey = "Supply 220VAC 50Hz",
                            MatchMode = "exact",
                            Offset = new QcOffset { X = -1, Y = 0 },
                            ValueField = "checked",
                            WriteMode = "value_map",
                            ValueMap = new Dictionary<string, string>
                            {
                                { "true", "☑" },
                                { "false", "☐" }
                            }
                        }
                    }
                },
                new QcAcceptanceCheck
                {
                    CheckCode = "MCU_DISPLAY_AIR_CONTROL_V1_03_HEX_84_1_KB",
                    Name = "MCU_DISPLAY_AIR_CONTROL V1.03.hex(84.1 KB)",
                    Checked = true,
                    ReportMapping = new List<QcCheckReportMapping>
                    {
                        new QcCheckReportMapping
                        {
                            MappingStrategy = "worksheet_search",
                            SearchKey = "MCU_DISPLAY_AIR_CONTROL V1.03.hex(84.1 KB)",
                            MatchMode = "exact",
                            Offset = new QcOffset { X = -1, Y = 0 },
                            ValueField = "checked",
                            WriteMode = "value_map",
                            ValueMap = new Dictionary<string, string>
                            {
                                { "true", "☑" },
                                { "false", "☐" }
                            }
                        }
                    }
                }
            };
        }

        private static QcReportMappingConfig BuildCmaReportMapping()
        {
            var itemMappings = new List<QcItemMapping>
            {
                new QcItemMapping
                {
                    ItemCode = "CMA003_INPUT_2000_OHM",
                    DisplayName = "จ่าย Input 2000 Ohm",
                    TargetColumn = "C",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = PassFailCheckmarkMap
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_WL",
                    DisplayName = "WL",
                    TargetColumn = "E",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = PassFailCheckmarkMap
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_WL_AL2",
                    DisplayName = "WL/Al2",
                    TargetColumn = "F",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = PassFailCheckmarkMap
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_HP",
                    DisplayName = "HP",
                    TargetColumn = "H",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = PassFailCheckmarkMap
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_HP_AL2",
                    DisplayName = "HP/Al2",
                    TargetColumn = "I",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = PassFailCheckmarkMap
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_ALARM1",
                    DisplayName = "Alarm1",
                    TargetColumn = "K",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = PassFailCheckmarkMap
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_FAN_HOT",
                    DisplayName = "Fan / Hot",
                    TargetColumn = "L",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = PassFailCheckmarkMap
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_FAN_COOL",
                    DisplayName = "Fan / Cool",
                    TargetColumn = "M",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = PassFailCheckmarkMap
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_COMPRESSOR",
                    DisplayName = "Compressor",
                    TargetColumn = "N",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = PassFailCheckmarkMap
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_200_99",
                    DisplayName = "จ่าย 200 / 99",
                    TargetColumn = "O",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = new Dictionary<string, string>()
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_2000_25_6",
                    DisplayName = "จ่าย 2000 / 25.6",
                    TargetColumn = "P",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = new Dictionary<string, string>()
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_8000_M6_4",
                    DisplayName = "จ่าย 8000 / -6.4",
                    TargetColumn = "Q",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = new Dictionary<string, string>()
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_2435_20",
                    DisplayName = "จ่าย 2435 / 20",
                    TargetColumn = "R",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = new Dictionary<string, string>()
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_1653_30",
                    DisplayName = "จ่าย 1653 / 30",
                    TargetColumn = "S",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = new Dictionary<string, string>()
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_1150_40",
                    DisplayName = "จ่าย 1150 / 40",
                    TargetColumn = "T",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = new Dictionary<string, string>()
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_818_50",
                    DisplayName = "จ่าย 818 / 50",
                    TargetColumn = "U",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = new Dictionary<string, string>()
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_F",
                    DisplayName = "ปุ่ม F",
                    TargetColumn = "V",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = PassFailCheckmarkMap
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_ITEM_018",
                    DisplayName = "ปุ่ม ลด",
                    TargetColumn = "W",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = PassFailCheckmarkMap
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_ITEM_019",
                    DisplayName = "ปุ่ม เพิ่ม",
                    TargetColumn = "X",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = PassFailCheckmarkMap
                },
                new QcItemMapping
                {
                    ItemCode = "CMA003_LED",
                    DisplayName = "LED ติดครบ",
                    TargetColumn = "Y",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = "measured_text",
                    EmptyValue = "N/A",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = PassFailCheckmarkMap
                }
            };

            var headerMappings = new List<QcHeaderMapping>
            {
                new QcHeaderMapping
                {
                    Field = "model_code",
                    TargetCell = "H2",
                    WriteMode = "replace_full_text",
                    Prefix = "Model : ",
                    Required = true,
                    Fallback = ""
                },
                // เขียนลงช่องว่างถัดไปทางขวา (O2) แบบ value_only โดยไม่เขียนทับข้อความ 'Lot :' ที่ช่อง N2
                new QcHeaderMapping
                {
                    Field = "lot_number",
                    TargetCell = "O2",
                    WriteMode = "value_only",
                    Prefix = "",
                    Required = true,
                    Fallback = ""
                },
                // เขียนลงช่องว่างถัดไปทางขวา (V2) แบบ value_only โดยไม่เขียนทับข้อความ 'Lot Size :' ที่ช่อง U2
                new QcHeaderMapping
                {
                    Field = "lot_size",
                    TargetCell = "V2",
                    WriteMode = "value_only",
                    Prefix = "",
                    Required = false,
                    Fallback = ""
                },
                // เขียนลงช่องว่างถัดไปทางขวา (AB2) แบบ value_only โดยไม่เขียนทับข้อความ 'Date :' ที่ช่อง AA2
                new QcHeaderMapping
                {
                    Field = "inspection_date",
                    TargetCell = "AB2",
                    WriteMode = "value_only",
                    Prefix = "",
                    Required = true,
                    Fallback = ""
                },
                new QcHeaderMapping
                {
                    Field = "equipment_codes",
                    TargetCell = "K3",
                    WriteMode = "replace_full_text",
                    Prefix = "เครื่องมือ ",
                    Required = false,
                    Fallback = ""
                },
                new QcHeaderMapping
                {
                    Field = "ecn_number",
                    TargetCell = "V4",
                    WriteMode = "replace_full_text",
                    Prefix = "ECN NO: ",
                    Required = false,
                    Fallback = ""
                },
                new QcHeaderMapping
                {
                    Field = "powersupply",
                    TargetCell = "K6",
                    WriteMode = "replace_full_text",
                    Prefix = "Supply ",
                    Required = false,
                    Fallback = ""
                },
                new QcHeaderMapping
                {
                    Field = "qty",
                    TargetCell = "J37",
                    WriteMode = "value_only",
                    Prefix = "",
                    Required = true,
                    Fallback = "0"
                },
                new QcHeaderMapping
                {
                    Field = "good",
                    TargetCell = "L37",
                    WriteMode = "value_only",
                    Prefix = "",
                    Required = true,
                    Fallback = "0"
                },
                new QcHeaderMapping
                {
                    Field = "defect",
                    TargetCell = "N37",
                    WriteMode = "value_only",
                    Prefix = "",
                    Required = true,
                    Fallback = "0"
                },
                new QcHeaderMapping
                {
                    Field = "yield",
                    TargetCell = "O37",
                    WriteMode = "value_only",
                    Prefix = "",
                    Required = true,
                    Fallback = "0"
                }
            };

            return new QcReportMappingConfig
            {
                TemplateId = "5",
                TemplateSource = "CMA-003 QC Inspection.xlsx",
                SheetName = "Sheet1",
                MappingStrategy = "server_template_settings",
                OutputMode = "server_template_settings",
                DataScope = "selected_inspections",
                Revision = "1.0.0",
                ServerTemplateSettings = new QcServerTemplateSettings
                {
                    DataRows = new List<QcDataRowRange>
                    {
                        new QcDataRowRange { From = 9, To = 34 }
                    },
                    MaxRecordsPerSheet = 26,
                    OverflowStrategy = "NEW_SHEET",
                    PageLayout = new QcPageLayout
                    {
                        FreezePanes = true,
                        HeaderMode = "REPEAT",
                        HeaderRows = new QcHeaderRows { From = 1, To = 8 }
                    },
                    SequenceColumn = "A",
                    SerialNumberColumn = "AB",
                    ResultMapping = new QcResultMapping
                    {
                        PassColumn = "Z",
                        FailColumn = "AA",
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

        private static readonly Dictionary<string, string> PassFailCheckmarkMap = new Dictionary<string, string>
        {
            { "PASS", "✔" },
            { "FAIL", "✘" },
            { "PENDING", "N/A" },
            { "SKIP", "N/A" },
            { "N/A", "N/A" }
        };

        private static QcItem CreateQcTextItem(
            string itemCode,
            string testName,
            string measuredText,
            string result,
            List<string> columnPath,
            int offsetX = 0,
            string anchor = "merge_top_left")
        {
            string displayText = string.IsNullOrWhiteSpace(measuredText) || measuredText.Trim() == "-" || measuredText.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase)
                ? "N/A"
                : measuredText.Trim();

            return new QcItem
            {
                ItemCode = itemCode,
                TestName = testName,
                Required = true,
                MeasuredText = displayText,
                MeasuredValue = null,
                MinValue = null,
                MaxValue = null,
                Unit = null,
                Result = result,
                ReportMapping = new List<QcItemReportMapping>
                {
                    new QcItemReportMapping
                    {
                        TargetType = "table_cell",
                        TableKey = "inspection_items",
                        ColumnPath = columnPath,
                        ColumnAnchor = anchor,
                        Offset = new QcOffset { X = offsetX, Y = 0 },
                        ValueField = "result",
                        WriteMode = "value_map",
                        ValueMap = PassFailCheckmarkMap
                    }
                }
            };
        }

        private static QcItem CreateQcNumericItem(
            string itemCode,
            string testName,
            double? measuredValue,
            string originalText,
            double minValue,
            double maxValue,
            List<string> columnPath,
            int offsetX = 0,
            string anchor = "merge_top_left",
            bool isPending = false)
        {
            string measuredText = measuredValue.HasValue
                ? measuredValue.Value.ToString("0.00", CultureInfo.InvariantCulture)
                : (string.IsNullOrWhiteSpace(originalText) || originalText.Trim() == "-" || originalText.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase)
                    ? "N/A"
                    : originalText.Trim());

            string result = measuredValue.HasValue
                ? (measuredValue.Value >= minValue && measuredValue.Value <= maxValue ? "PASS" : "FAIL")
                : (isPending ? "PENDING" : "SKIP");

            return new QcItem
            {
                ItemCode = itemCode,
                TestName = testName,
                Required = true,
                MeasuredText = measuredText,
                MeasuredValue = measuredValue.HasValue ? Math.Round(measuredValue.Value, 2) : null,
                MinValue = minValue,
                MaxValue = maxValue,
                Unit = null,
                Result = result,
                ReportMapping = new List<QcItemReportMapping>
                {
                    new QcItemReportMapping
                    {
                        TargetType = "table_cell",
                        TableKey = "inspection_items",
                        ColumnPath = columnPath,
                        ColumnAnchor = anchor,
                        Offset = new QcOffset { X = offsetX, Y = 0 },
                        ValueField = "measured_value",
                        WriteMode = "value",
                        NumberFormat = "0.00"
                    }
                }
            };
        }
    }
}
