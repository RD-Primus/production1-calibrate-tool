using ClosedXML.Excel;
using System.Globalization;

namespace TEST_DEF_03N
{
    public partial class Form1
    {
        private QcPayload BuildQcPayloadV2(
            DateTime inspectionStart,
            DateTime inspectionEnd,
            bool overallPass)
        {
            ModelReportTarget target = _modelReportTargets
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Path)
                                     && File.Exists(item.Path))
                ?? throw new InvalidOperationException("ไม่พบไฟล์ผลทดสอบสำหรับสร้างข้อมูล API");

            List<QcItem> items;
            int testedSNCount;
            string baseModel = txt_model.Text.Trim();

            using (var workbook = new XLWorkbook(target.Path))
            {
                IXLWorksheet sheet = workbook.Worksheet(1);
                items = BuildQcItemsFromReportV2(sheet, target.Row, baseModel, overallPass);
                testedSNCount = GetTestedSerialCount(sheet);
            }

            string modelCode = GetSelectedApiModelCode();
            string powerSupplyText = GetSelectedPowerSupplyText();
            const string dateTimeFormat = "yyyy-MM-dd'T'HH:mm:sszzz";

            string operatorUser = !string.IsNullOrWhiteSpace(txt_name.Text) ? txt_name.Text.Trim() : (string.IsNullOrWhiteSpace(_operatorUsername) ? "admin" : _operatorUsername);
            string sn = txt_SN.Text.Trim();

            return new QcPayload
            {
                SchemaVersion = 2,
                UpdateMode = false,
                ModelCode = modelCode,
                LotNumber = txt_Lot.Text.Trim(),
                EcnNumber = txt_ECN.Text.Trim(),
                SerialNumber = sn,
                StationName = "QC-STATION-01",
                OperatorUsername = operatorUser,
                StartTime = inspectionStart.ToString(dateTimeFormat, CultureInfo.InvariantCulture),
                InspectionTime = inspectionEnd.ToString(dateTimeFormat, CultureInfo.InvariantCulture),
                TemplateName = "DEF-03N QC Inspection.xlsx",
                Description = $"{modelCode} Functional and Temperature Inspection",
                Firmware = new List<QcFirmware>
                {
                    new QcFirmware
                    {
                        Name = "DEF-03N",
                        Version = _currentFirmware.Trim()
                    }
                },
                PowerSupply = powerSupplyText,
                EquipmentCodes = GetEquipmentCodesForPayloadV2(),
                Approve = new QcApproval
                {
                    Tester = new QcApprovalParty
                    {
                        Name = operatorUser,
                        Datetime = inspectionEnd.ToString(dateTimeFormat, CultureInfo.InvariantCulture)
                    },
                    Confirm = new QcApprovalParty
                    {
                        Name = null,
                        Datetime = null
                    },
                    Approver = new QcApprovalParty
                    {
                        Name = null,
                        Datetime = null
                    }
                },
                Remark = GetDescriptionForPayload(),
                Uuid = Guid.NewGuid().ToString(),
                InspectionStatus = (overallPass && items.All(i => i.Result == "PASS" || i.Result == "SKIP")) ? "final" : (items.Any(i => i.Result == "FAIL") ? "final" : "in_progress"),
                ProductName = "Mini Temperature Control NTC/PTC Input",
                ReportSerialNumber = sn,
                JigName = "DEF03N-FUNCTIONAL-JIG-01",
                ReportDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                OverallResult = (overallPass && items.All(i => i.Result == "PASS" || i.Result == "SKIP")) ? "PASS" : (items.Any(i => i.Result == "FAIL") ? "FAIL" : "PENDING"),
                LotSize = testedSNCount > 0 ? testedSNCount : 1,
                AcceptanceChecks = BuildAcceptanceChecks(baseModel, powerSupplyText),
                Items = items,
                ReportMapping = BuildRootReportMapping(baseModel),
                Images = new List<object>()
            };
        }

        private List<QcEquipment> GetEquipmentCodesForPayloadV2() =>
            _equipmentDatabaseRecords
                .Where(item => !string.IsNullOrWhiteSpace(item.Name)
                            && !string.IsNullOrWhiteSpace(item.Series)
                            && !string.IsNullOrWhiteSpace(item.EquipmentId))
                .Select(item =>
                {
                    string equipmentId = item.EquipmentId.Trim();
                    string? expireDate = (!string.IsNullOrWhiteSpace(item.ExpireDate) && item.ExpireDate.Trim() != "-")
                        ? item.ExpireDate.Trim()
                        : null;

                    if (expireDate != null && !equipmentId.Contains("หมดอายุ", StringComparison.OrdinalIgnoreCase))
                    {
                        equipmentId = $"{equipmentId} หมดอายุ {expireDate}";
                    }

                    return new QcEquipment
                    {
                        Name = item.Name.Trim(),
                        Series = item.Series.Trim(),
                        EquipmentId = equipmentId,
                        ExpireDate = expireDate
                    };
                })
                .ToList();

        private static int GetTestedSerialCount(IXLWorksheet sheet)
        {
            int count = 0;
            int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 500;
            for (int r = QcReportLayout.FirstDataRow; r <= lastRow; r++)
            {
                if (QcReportLayout.IsDataRow(sheet, r) &&
                    !sheet.Cell(r, QcReportLayout.SerialColumn).IsEmpty())
                {
                    count++;
                }
            }
            return count;
        }

        private static List<QcAcceptanceCheck> BuildAcceptanceChecks(string model, string powerSupply)
        {
            bool isModelA = string.Equals(model, ModelTestResults.ModelA, StringComparison.OrdinalIgnoreCase);

            var checks = new List<QcAcceptanceCheck>
            {
                // ตรวจรับ 4 ข้อพื้นฐานตามที่ Server Template ตรวจพบ (ทั้ง Model A และ Model A-B)
                CreateCheck("PTC_2K_PTC_10K_NTC", "รับสัญญาณอินพุตจาก PTC 2K, PTC 10K, NTC", true, "รับสัญญาณอินพุตจาก PTC 2K, PTC 10K, NTC"),
                CreateCheck("7_SEGMENT_LED_3", "แสดงผลด้วย 7-segment LED สีแดง 3 หลัก", true, "แสดงผลด้วย 7-segment LED สีแดง 3 หลัก"),
                CreateCheck("ACCEPTANCE_006", "สามารถตั้งจุดทศนิยมได้", true, "สามารถตั้งจุดทศนิยมได้"),
                CreateCheck("NTC_2K_1", "สาย NTC 2K(1 เส้น)", true, "สาย NTC 2K(1 เส้น)")
            };

            // ตรวจรับ 3 ข้อเฉพาะ Model A-B (ถ้าเป็น Model A จะไม่ส่ง 3 ข้อนี้ เพื่อไม่ให้ติด PENDING และคงเป็น ☐ ตามเดิม)
            if (!isModelA)
            {
                checks.Add(CreateCheck("MAINRELAY_1_CONTROL_ALARM", "มี MainRelay 1 ชุดสามารถเลือกได้ว่าจะทำงานเป็น Control/Alarm", true, "มี MainRelay 1 ชุดสามารถเลือกได้ว่าจะทำงานเป็น Control/Alarm"));
                checks.Add(CreateCheck("RELAY_OUTPUT_HEATING_COOLING_HYSTERESIS_UNIT", "Relay Output เลือกทำงานได้ทั้ง Heating และ Cooling, Hysteresis ตั้งทำให้เป็นค่า Unit หรือเวลาก็ได้", true, "Relay Output เลือกทำงานได้ทั้ง Heating และ Cooling, Hysteresis ตั้งทำให้เป็นค่า Unit หรือเวลาก็ได้"));
                checks.Add(CreateCheck("HYSTERESIS_TIME_0_99_59_MIN", "มี Hysteresis Time โดยเริ่มทำงาน 0-99.59 min", true, "มี Hysteresis Time โดยเริ่มทำงาน 0-99.59 min"));
            }

            return checks;
        }

        private static QcAcceptanceCheck CreateCheck(string code, string name, bool isChecked, string searchKey)
        {
            return new QcAcceptanceCheck
            {
                CheckCode = code,
                Name = name,
                Checked = isChecked,
                ReportMapping = new List<QcCheckReportMapping>
                {
                    new QcCheckReportMapping
                    {
                        MappingStrategy = "worksheet_search",
                        SearchKey = searchKey,
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
            };
        }

        private static readonly Dictionary<string, string> PassFailCheckmarkMap = new Dictionary<string, string>
        {
            { "PASS", "✔" },
            { "FAIL", "✘" },
            { "PENDING", "-" },
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
            string anchor = "merge_top_left",
            bool required = true)
        {
            string displayText = measuredText;
            if (result == "SKIP")
            {
                displayText = "N/A";
            }
            else if (result == "PENDING")
            {
                displayText = "-";
            }
            else if (string.IsNullOrWhiteSpace(displayText))
            {
                displayText = result == "PASS" ? "✔" : "✘";
            }

            return new QcItem
            {
                ItemCode = itemCode,
                TestName = testName,
                Required = required,
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
            bool isPending = false,
            bool overallPass = true)
        {
            string measuredText;
            string result;

            if (measuredValue.HasValue)
            {
                measuredText = measuredValue.Value.ToString("0.00", CultureInfo.InvariantCulture);
                result = (measuredValue.Value >= minValue && measuredValue.Value <= maxValue) ? "PASS" : "FAIL";
            }
            else if (isPending || originalText.Trim() == "-")
            {
                measuredText = "-";
                result = "PENDING";
            }
            else
            {
                measuredText = string.IsNullOrWhiteSpace(originalText) ? "N/A" : originalText.Trim();
                result = "SKIP";
            }

            return new QcItem
            {
                ItemCode = itemCode,
                TestName = testName,
                Required = true,
                MeasuredText = measuredText,
                MeasuredValue = measuredValue.HasValue ? Math.Round(measuredValue.Value, 2) : null,
                MinValue = minValue,
                MaxValue = maxValue,
                Unit = "°C",
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

        private static List<QcItem> BuildQcItemsFromReportV2(
            IXLWorksheet sheet,
            int row,
            string model,
            bool overallPass = true)
        {
            var items = new List<QcItem>();
            bool isModelA = string.Equals(model, ModelTestResults.ModelA, StringComparison.OrdinalIgnoreCase);

            (string Code, string Name, string TopCol, string SubCol, double Expected, int ExcelCol)[] temperatures =
            {
                ("PTC_2K_C_00_M40", "PTC 2K (°C) 00 / -40", "PTC 2K (°C) 00", "-40", -40, 2),
                ("PTC_2K_C_00_40", "PTC 2K (°C) 00 / 40", "PTC 2K (°C) 00", "40", 40, 3),
                ("PTC_2K_C_00_130", "PTC 2K (°C) 00 / 130", "PTC 2K (°C) 00", "130", 130, 4),

                ("NTC_10K_C_01_M40", "NTC 10K (°C) 01 / -40", "NTC 10K (°C) 01", "-40", -40, 5),
                ("NTC_10K_C_01_100", "NTC 10K (°C) 01 / 100", "NTC 10K (°C) 01", "100", 100, 6),
                ("NTC_10K_C_01_120", "NTC 10K (°C) 01 / 120", "NTC 10K (°C) 01", "120", 120, 7),

                ("NTC_2K_C_02_M20", "NTC 2K (°C) 02 / -20", "NTC 2K (°C) 02", "-20", -20, 8),
                ("NTC_2K_C_02_40", "NTC 2K (°C) 02 / 40", "NTC 2K (°C) 02", "40", 40, 9),
                ("NTC_2K_C_02_120", "NTC 2K (°C) 02 / 120", "NTC 2K (°C) 02", "120", 120, 10),

                ("NTC_2K_C_03_M40", "nTC 2K (°C) 03 / -40", "nTC 2K (°C) 03", "-40", -40, 11),
                ("NTC_2K_C_03_60", "nTC 2K (°C) 03 / 60", "nTC 2K (°C) 03", "60", 60, 12),
                ("NTC_2K_C_03_120", "nTC 2K (°C) 03 / 120", "nTC 2K (°C) 03", "120", 120, 13)
            };

            const double tolerance = 1.0;
            foreach (var t in temperatures)
            {
                string text = sheet.Cell(row, t.ExcelCol).GetFormattedString().Trim();
                double? value = ParseReportNumber(text);
                double min = t.Expected - tolerance;
                double max = t.Expected + tolerance;
                bool isPending = text == "-" || string.IsNullOrWhiteSpace(text);

                items.Add(CreateQcNumericItem(
                    t.Code,
                    t.Name,
                    value,
                    text,
                    min,
                    max,
                    new List<string> { t.TopCol, t.SubCol },
                    isPending: isPending,
                    overallPass: overallPass
                ));
            }

            (string Code, string Name, string ColHeader, int Col, bool IsAlarmRelated)[] funcItems =
            {
                ("OUTPUT_RELAY", "Output Relay", "Output\nRelay", 14, false),
                ("ALARM_RELAY", "Alarm Relay", "Alarm\nRelay", 15, true),
                ("F", "ปุ่ม F", "ปุ่ม\nF", 16, false),
                ("ITEM_016", "ปุ่ม ลด", "ปุ่ม\nลด", 17, false),
                ("ITEM_017", "ปุ่ม เพิ่ม", "ปุ่ม\nเพิ่ม", 18, false),
                ("LED_OUT", "LED OUT", "LED\nOUT", 19, false),
                ("LED_AL", "LED AL", "LED\nAL", 20, true),
                ("ITEM_020", "จอ แสดง ผล", "จอ\nแสดง\nผล", 21, false)
            };

            foreach (var f in funcItems)
            {
                string text = sheet.Cell(row, f.Col).GetString().Trim();
                bool notApplicable = (isModelA && f.IsAlarmRelated) || text.Equals("N/A", StringComparison.OrdinalIgnoreCase);
                string itemResult;
                string displayText;

                if (notApplicable)
                {
                    itemResult = "SKIP";
                    displayText = "N/A";
                }
                else if (text.Equals("OK", StringComparison.OrdinalIgnoreCase) || text.Equals("PASS", StringComparison.OrdinalIgnoreCase) || text == "✔" || text == "\u2714")
                {
                    itemResult = "PASS";
                    displayText = "✔";
                }
                else if (text == "-" || text.Equals("PENDING", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(text))
                {
                    itemResult = "PENDING";
                    displayText = "-";
                }
                else
                {
                    itemResult = "FAIL";
                    displayText = "✘";
                }

                items.Add(CreateQcTextItem(
                    f.Code,
                    f.Name,
                    displayText,
                    itemResult,
                    new List<string> { f.ColHeader },
                    required: !notApplicable
                ));
            }

            return items;
        }

        private static QcReportMappingConfig BuildRootReportMapping(string model)
        {
            var funcValueMap = new Dictionary<string, string>
            {
                { "FAIL", "✘" },
                { "PASS", "✔" },
                { "PENDING", "-" },
                { "SKIP", "N/A" }
            };

            var emptyValueMap = new Dictionary<string, string>();

            var itemMappings = new List<QcItemMapping>
            {
                // PTC 2K (00)
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "QC001" },
                    ItemCode = "PTC_2K_C_00_M40",
                    DisplayName = "PTC 2K (°C) 00 / -40",
                    TargetColumn = "B",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = emptyValueMap
                },
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "QC002" },
                    ItemCode = "PTC_2K_C_00_40",
                    DisplayName = "PTC 2K (°C) 00 / 40",
                    TargetColumn = "C",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = emptyValueMap
                },
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "QC003" },
                    ItemCode = "PTC_2K_C_00_130",
                    DisplayName = "PTC 2K (°C) 00 / 130",
                    TargetColumn = "D",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = emptyValueMap
                },

                // NTC 10K (01)
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "QC004" },
                    ItemCode = "NTC_10K_C_01_M40",
                    DisplayName = "NTC 10K (°C) 01 / -40",
                    TargetColumn = "E",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = emptyValueMap
                },
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "QC005" },
                    ItemCode = "NTC_10K_C_01_100",
                    DisplayName = "NTC 10K (°C) 01 / 100",
                    TargetColumn = "F",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = emptyValueMap
                },
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "QC006" },
                    ItemCode = "NTC_10K_C_01_120",
                    DisplayName = "NTC 10K (°C) 01 / 120",
                    TargetColumn = "G",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = emptyValueMap
                },

                // NTC 2K (02)
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "QC007" },
                    ItemCode = "NTC_2K_C_02_M20",
                    DisplayName = "NTC 2K (°C) 02 / -20",
                    TargetColumn = "H",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = emptyValueMap
                },
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "QC008" },
                    ItemCode = "NTC_2K_C_02_40",
                    DisplayName = "NTC 2K (°C) 02 / 40",
                    TargetColumn = "I",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = emptyValueMap
                },
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "QC009" },
                    ItemCode = "NTC_2K_C_02_120",
                    DisplayName = "NTC 2K (°C) 02 / 120",
                    TargetColumn = "J",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = emptyValueMap
                },

                // nTC 2K (03)
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "QC010" },
                    ItemCode = "NTC_2K_C_03_M40",
                    DisplayName = "nTC 2K (°C) 03 / -40",
                    TargetColumn = "K",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = emptyValueMap
                },
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "QC011" },
                    ItemCode = "NTC_2K_C_03_60",
                    DisplayName = "nTC 2K (°C) 03 / 60",
                    TargetColumn = "L",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = emptyValueMap
                },
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "QC012" },
                    ItemCode = "NTC_2K_C_03_120",
                    DisplayName = "nTC 2K (°C) 03 / 120",
                    TargetColumn = "M",
                    RowOffset = 0,
                    SourceField = "measured_value",
                    FallbackField = "measured_text",
                    EmptyValue = "",
                    NumberFormat = "0.00",
                    Required = true,
                    ValueMap = emptyValueMap
                },

                // Functional items
                new QcItemMapping
                {
                    AcceptedItemCodes = new List<string> { "QC013" },
                    ItemCode = "OUTPUT_RELAY",
                    DisplayName = "Output Relay",
                    TargetColumn = "N",
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
                    AcceptedItemCodes = new List<string> { "QC014" },
                    ItemCode = "ALARM_RELAY",
                    DisplayName = "Alarm Relay",
                    TargetColumn = "O",
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
                    AcceptedItemCodes = new List<string> { "QC015" },
                    ItemCode = "F",
                    DisplayName = "ปุ่ม F",
                    TargetColumn = "P",
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
                    AcceptedItemCodes = new List<string> { "QC016" },
                    ItemCode = "ITEM_016",
                    DisplayName = "ปุ่ม ลด",
                    TargetColumn = "Q",
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
                    AcceptedItemCodes = new List<string> { "QC017" },
                    ItemCode = "ITEM_017",
                    DisplayName = "ปุ่ม เพิ่ม",
                    TargetColumn = "R",
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
                    AcceptedItemCodes = new List<string> { "QC018" },
                    ItemCode = "LED_OUT",
                    DisplayName = "LED OUT",
                    TargetColumn = "S",
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
                    AcceptedItemCodes = new List<string> { "QC019" },
                    ItemCode = "LED_AL",
                    DisplayName = "LED AL",
                    TargetColumn = "T",
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
                    AcceptedItemCodes = new List<string> { "QC020" },
                    ItemCode = "ITEM_020",
                    DisplayName = "จอ แสดง ผล",
                    TargetColumn = "U",
                    RowOffset = 0,
                    SourceField = "result",
                    FallbackField = null,
                    EmptyValue = "",
                    NumberFormat = "@",
                    Required = true,
                    ValueMap = funcValueMap
                }
            };

            bool isModelA = string.Equals(model, ModelTestResults.ModelA, StringComparison.OrdinalIgnoreCase);
            string relayMark = !isModelA ? "☑" : "☐";

            var headerMappings = new List<QcHeaderMapping>
            {
                new QcHeaderMapping
                {
                    Field = "model_code",
                    TargetCell = "I2",
                    WriteMode = "replace_full_text",
                    Prefix = "Model: ",
                    Fallback = "",
                    Required = true
                },
                new QcHeaderMapping
                {
                    Field = "lot_number",
                    TargetCell = "N2",
                    WriteMode = "value_only",
                    Prefix = "",
                    Fallback = "",
                    Required = true
                },
                new QcHeaderMapping
                {
                    Field = "lot_size",
                    TargetCell = "S2",
                    WriteMode = "value_only",
                    Prefix = "",
                    Fallback = "",
                    Required = false // ฟิลด์นี้ไม่บังคับว่าต้องมีข้อมูล (Optional)
                },
                new QcHeaderMapping
                {
                    Field = "inspection_date",
                    TargetCell = "X2",
                    WriteMode = "value_only",
                    Prefix = "",
                    Fallback = "",
                    Required = true
                },
                new QcHeaderMapping
                {
                    Field = "equipment_codes",
                    TargetCell = "L3",
                    WriteMode = "replace_full_text",
                    Prefix = "เครื่องมือ: ",
                    Fallback = "",
                    Required = false
                },
                new QcHeaderMapping
                {
                    Field = "ecn_number",
                    TargetCell = "L6",
                    WriteMode = "replace_full_text",
                    Prefix = "ECN NO: ",
                    Fallback = "",
                    Required = false
                },
                new QcHeaderMapping
                {
                    Field = "firmware",
                    TargetCell = "L7",
                    WriteMode = "replace_full_text",
                    Prefix = "FW ",
                    Fallback = "",
                    Required = false
                },
                new QcHeaderMapping
                {
                    Field = "powersupply",
                    TargetCell = "L8",
                    WriteMode = "replace_full_text",
                    Prefix = "Power Supply ",
                    Fallback = "",
                    Required = false
                },

                // เกณฑ์การยอมรับ 11 ข้อ (Checkboxes บน Server Template)
                // คอลัมน์ซ้าย (A4 - A8)
                new QcHeaderMapping { Field = "PTC_2K_PTC_10K_NTC", TargetCell = "A4", WriteMode = "value_only", Fallback = "☑", Required = false },
                new QcHeaderMapping { Field = "7_SEGMENT_LED_3", TargetCell = "A5", WriteMode = "value_only", Fallback = "☑", Required = false },
                new QcHeaderMapping { Field = "ACCEPTANCE_006", TargetCell = "A6", WriteMode = "value_only", Fallback = "☑", Required = false },
                new QcHeaderMapping { Field = "NTC_2K_1", TargetCell = "A7", WriteMode = "value_only", Fallback = "☑", Required = false },
                new QcHeaderMapping { Field = "MAINRELAY_1_CONTROL_ALARM", TargetCell = "A8", WriteMode = "value_only", Fallback = relayMark, Required = false },

                // คอลัมน์ขวา (K3 - K8)
                // 4 ช่อง Checkbox ฝั่งขวา (เครื่องมือ K3, ECN NO K6, FW K7, Power Supply K8) ติ๊กถูกเสมอ
                new QcHeaderMapping { Field = "chk_equipment", TargetCell = "K3", WriteMode = "value_only", Fallback = "☑", Required = false },
                new QcHeaderMapping { Field = "chk_ecn", TargetCell = "K6", WriteMode = "value_only", Fallback = "☑", Required = false },
                new QcHeaderMapping { Field = "chk_fw", TargetCell = "K7", WriteMode = "value_only", Fallback = "☑", Required = false },
                new QcHeaderMapping { Field = "chk_supply", TargetCell = "K8", WriteMode = "value_only", Fallback = "☑", Required = false },
                // 2 ช่อง Checkbox ฝั่งขวา (Relay Output K4, Hysteresis K5) เฉพาะรุ่น A-B
                new QcHeaderMapping { Field = "RELAY_OUTPUT_HEATING_COOLING_HYSTERESIS_UNIT", TargetCell = "K4", WriteMode = "value_only", Fallback = relayMark, Required = false },
                new QcHeaderMapping { Field = "HYSTERESIS_TIME_0_99_59_MIN", TargetCell = "K5", WriteMode = "value_only", Fallback = relayMark, Required = false },
                new QcHeaderMapping
                {
                    Field = "qty",
                    TargetCell = "F55",
                    WriteMode = "value_only",
                    Prefix = "",
                    Fallback = "0",
                    Required = true
                },
                new QcHeaderMapping
                {
                    Field = "good",
                    TargetCell = "H55",
                    WriteMode = "value_only",
                    Prefix = "",
                    Fallback = "0",
                    Required = true
                },
                new QcHeaderMapping
                {
                    Field = "defect",
                    TargetCell = "J55",
                    WriteMode = "value_only",
                    Prefix = "",
                    Fallback = "0",
                    Required = true
                },
                new QcHeaderMapping
                {
                    Field = "yield",
                    TargetCell = "L55",
                    WriteMode = "value_only",
                    Prefix = "",
                    Fallback = "0",
                    Required = true
                }
            };

            return new QcReportMappingConfig
            {
                TemplateId = "DEF03N_F_PD07_R6",
                TemplateSource = "DEF-03N QC Inspection.xlsx",
                SheetName = "Sheet1",
                MappingStrategy = "server_template_settings",
                OutputMode = "server_template_settings",
                DataScope = "selected_inspections",
                Revision = "1.0.0",
                ServerTemplateSettings = new QcServerTemplateSettings
                {
                    DataRows = new List<QcDataRowRange>
                    {
                        new QcDataRowRange { From = 12, To = 52 }
                    },
                    MaxRecordsPerSheet = 41,
                    OverflowStrategy = "NEW_SHEET",
                    PageLayout = new QcPageLayout
                    {
                        FreezePanes = true,
                        HeaderMode = "REPEAT",
                        HeaderRows = new QcHeaderRows { From = 1, To = 11 }
                    },
                    SequenceColumn = "A",
                    SerialNumberColumn = "X",
                    ResultMapping = new QcResultMapping
                    {
                        PassColumn = "V",
                        FailColumn = "W",
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
    }
}
