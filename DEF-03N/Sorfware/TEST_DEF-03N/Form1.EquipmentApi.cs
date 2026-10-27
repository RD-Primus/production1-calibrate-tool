// =========================================================================================
// [DEVELOPER GUIDE]
// Form1.EquipmentApi.cs: "การจัดการฐานข้อมูลเครื่องมือวัด (Equipment) และส่งข้อมูล QC API"
// - บันทึกและโหลดข้อมูลเครื่องมือวัด (DMM, Power Supply, Calibrator) ลงใน Settings
// - จัดการการแสดงผล Popup หน้าต่างแก้ไขอุปกรณ์เครื่องมือ (EquipmentPopupForm)
// - ส่ง HTTP POST พร้อมผลการทดสอบ QC ไปยังเซิร์ฟเวอร์ปลายทาง (/api/qc/result)
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using ClosedXML.Excel;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TEST_DEF_03N
{
    public partial class Form1
    {
        private const string QcResultApiUrl = "http://192.168.109.54:5088/api/qc/result";
        private static readonly HttpClient QcApiClient = new HttpClient();

        private List<EquipmentRecord> _equipmentDatabaseRecords = EquipmentRecord.CreateDefaults();
        private bool _serverPostInProgress;

        private enum ServerPostResult
        {
            Skipped,
            Success,
            Timeout,
            Failed
        }

        private void InitializeEquipmentAndApiFeature()
        {
            _equipmentDatabaseRecords = LoadSavedEquipmentRecords();
            btn_equipment.Enabled = true;
            btn_equipment.Click += BtnEquipment_Click;
        }

        private void BtnEquipment_Click(object? sender, EventArgs e)
        {
            using var popup = new EquipmentPopupForm(
                _equipmentDatabaseRecords,
                _currentFirmware,
                _currentRevision);

            if (popup.ShowDialog(this) != DialogResult.OK)
                return;

            _equipmentDatabaseRecords = popup.EquipmentRecords
                .Select(item => item.Clone())
                .ToList();

            _currentFirmware = popup.FirmwareVersion;
            _currentRevision = popup.Revision;
            _currentInstrument = GetOhmSourceInstrumentText();

            Settings1.Default.SavedEquipmentDetails =
                JsonSerializer.Serialize(_equipmentDatabaseRecords);
            Settings1.Default.Save();

            SaveConfig();
        }

        private static List<EquipmentRecord> LoadSavedEquipmentRecords()
        {
            string savedJson = Settings1.Default.SavedEquipmentDetails?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(savedJson))
                return EquipmentRecord.CreateDefaults();

            try
            {
                List<EquipmentRecord>? records =
                    JsonSerializer.Deserialize<List<EquipmentRecord>>(savedJson);

                if (records == null
                    || records.Count != 2
                    || records.Any(item => string.IsNullOrWhiteSpace(item.Name)
                                           || string.IsNullOrWhiteSpace(item.Series)
                                           || string.IsNullOrWhiteSpace(item.EquipmentId)))
                {
                    return EquipmentRecord.CreateDefaults();
                }

                if (string.IsNullOrWhiteSpace(records[0].ExpireDate))
                    records[0].ExpireDate = "3/2/27";
                if (string.IsNullOrWhiteSpace(records[1].ExpireDate))
                    records[1].ExpireDate = "-";

                return records;
            }
            catch (JsonException)
            {
                return EquipmentRecord.CreateDefaults();
            }
        }

        private List<QcEquipment> GetEquipmentCodesForPayload() =>
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
                        ExpireDate = expireDate ?? "-"
                    };
                })
                .ToList();

        private string GetSelectedApiModelCode()
        {
            string model = txt_model.Text.Trim();
            if (!ModelTestResults.IsValidModel(model) || txt_supply.SelectedIndex < 0)
                throw new InvalidOperationException("ไม่พบ Model หรือ Supply สำหรับส่ง Server");

            return model + "-" + (txt_supply.SelectedIndex == 0 ? "220" : "24");
        }

        private string GetSelectedPowerSupplyText()
        {
            return txt_supply.SelectedIndex switch
            {
                0 => "220VAC +-15% 50/60Hz",
                1 => "24VAC/VDC +-15%",
                _ => throw new InvalidOperationException(
                    "ไม่พบ Supply สำหรับเขียน Excel หรือส่ง Server")
            };
        }

        private async Task<ServerPostResult> PostQcResultAsync(
            DateTime inspectionStart,
            DateTime inspectionEnd,
            bool overallPass)
        {
            if (_serverPostInProgress)
            {
                LogToRx("ไม่สามารถส่ง Server ซ้ำได้ เนื่องจากกำลังส่งผลอยู่", Color.Orange);
                return ServerPostResult.Failed;
            }

            string token = (Settings1.Default.SavedApiToken ?? "").Trim();
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring("Bearer ".Length).Trim();

            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show(this,
                    "กรุณากำหนด Token Key ในหน้าต่าง Login ก่อนส่งข้อมูลขึ้นระบบ",
                    "ไม่พบ Token", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return ServerPostResult.Failed;
            }

            if (!TryGetQcResultApiUrl(out string qcResultApiUrl))
                return ServerPostResult.Failed;

            if (!PrepareDescriptionForPersistence(overallPass ? "PASS" : "FAIL"))
            {
                LogToRx("[SERVER] ยกเลิกการส่ง Server เนื่องจากยังไม่ได้บันทึกหมายเหตุ", Color.Orange);
                return ServerPostResult.Failed;
            }

            QcPayload payload;
            try
            {
                payload = BuildQcPayloadV2(inspectionStart, inspectionEnd, overallPass);
            }
            catch (Exception ex)
            {
                LogToRx($"[SERVER] เตรียมข้อมูลไม่สำเร็จ: {ex.Message}", Color.Red);
                return ServerPostResult.Failed;
            }

            string jsonBody = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            _serverPostInProgress = true;
            try
            {
                LogToRx($"กำลังส่งผล S/N: {payload.SerialNumber} ไป Server...", Color.Blue);

                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Post, qcResultApiUrl);
                request.Headers.TryAddWithoutValidation("X-API-KEY", token);
                if (!string.IsNullOrWhiteSpace(_operatorToken))
                {
                    request.Headers.TryAddWithoutValidation("X-OPERATOR-TOKEN", _operatorToken);
                }
                request.Content = content;

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using HttpResponseMessage response = await QcApiClient.SendAsync(request, timeout.Token);
                string responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    LogToRx($"อัปโหลด S/N: {payload.SerialNumber} เรียบร้อย", Color.Green);
                    return ServerPostResult.Success;
                }

                LogToRx(
                    $"[SERVER ERROR {(int)response.StatusCode}] {response.StatusCode}: {responseText}",
                    Color.Red);
                return ServerPostResult.Failed;
            }
            catch (OperationCanceledException)
            {
                LogToRx("[SERVER TIMEOUT] Server ไม่ตอบกลับภายใน 10 วินาที", Color.Red);
                return ServerPostResult.Timeout;
            }
            catch (Exception ex)
            {
                LogToRx($"[SERVER ERROR] {ex.Message}", Color.Red);
                return ServerPostResult.Failed;
            }
            finally
            {
                _serverPostInProgress = false;
            }
        }

        private QcPayload BuildQcPayload(
            DateTime inspectionStart,
            DateTime inspectionEnd,
            bool overallPass)
        {
            ModelReportTarget target = _modelReportTargets
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Path)
                                     && File.Exists(item.Path))
                ?? throw new InvalidOperationException("ไม่พบไฟล์ผลทดสอบสำหรับสร้างข้อมูล API");

            List<QcItem> items;
            using (var workbook = new XLWorkbook(target.Path))
            {
                IXLWorksheet sheet = workbook.Worksheet(1);
                items = BuildQcItemsFromReport(sheet, target.Row, txt_model.Text.Trim());
            }

            string modelCode = GetSelectedApiModelCode();
            const string dateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss";

            return new QcPayload
            {
                ModelCode = modelCode,
                LotNumber = txt_Lot.Text.Trim(),
                EcnNumber = txt_ECN.Text.Trim(),
                SerialNumber = txt_SN.Text.Trim(),
                StationName = "QC-STATION-01",
                OperatorUsername = string.IsNullOrWhiteSpace(_operatorUsername) ? "admin" : _operatorUsername,
                StartTime = inspectionStart.ToString(dateTimeFormat, CultureInfo.InvariantCulture),
                InspectionTime = inspectionEnd.ToString(dateTimeFormat, CultureInfo.InvariantCulture),
                TemplateName = modelCode + " QC Inspection",
                Firmware = new List<QcFirmware>
                {
                    new QcFirmware
                    {
                        Name = "DEF-03N",
                        Version = _currentFirmware.Trim()
                    }
                },
                PowerSupply = GetSelectedPowerSupplyText(),
                EquipmentCodes = GetEquipmentCodesForPayload(),
                Remark = GetDescriptionForPayload(),
                Items = items
            };
        }

        private static List<QcItem> BuildQcItemsFromReport(
            IXLWorksheet sheet,
            int row,
            string model)
        {
            var items = new List<QcItem>();
            (string Name, double Expected)[] temperatures =
            {
                ("PTC 2K -40°C", -40),
                ("PTC 2K 40°C", 40),
                ("PTC 2K 130°C", 130),
                ("NTC 10K (01) -40°C", -40),
                ("NTC 10K (01) 100°C", 100),
                ("NTC 10K (01) 120°C", 120),
                ("NTC 2K -20°C", -20),
                ("NTC 2K 40°C", 40),
                ("NTC 2K 120°C", 120),
                ("NTC 10K (03) -40°C", -40),
                ("NTC 10K (03) 60°C", 60),
                ("NTC 10K (03) 120°C", 120)
            };

            const double tolerance = 1.0;
            for (int index = 0; index < temperatures.Length; index++)
            {
                string text = sheet.Cell(row, 2 + index).GetFormattedString().Trim();
                double? value = ParseReportNumber(text);
                double min = temperatures[index].Expected - tolerance;
                double max = temperatures[index].Expected + tolerance;
                items.Add(new QcItem
                {
                    ItemCode = $"QC{index + 1:000}",
                    TestName = temperatures[index].Name,
                    MeasuredText = text,
                    MeasuredValue = value,
                    MinValue = min,
                    MaxValue = max,
                    Unit = "°C",
                    Result = value.HasValue && value.Value >= min && value.Value <= max
                        ? "PASS"
                        : "FAIL"
                });
            }

            (string Name, int Column)[] textItems =
            {
                ("Relay Output", 14),
                ("Alarm", 15),
                ("Button F", 16),
                ("Button Down", 17),
                ("Button Up", 18),
                ("LED OUT", 19),
                ("LED AL", 20),
                ("7-SEG", 21)
            };

            for (int index = 0; index < textItems.Length; index++)
            {
                string text = sheet.Cell(row, textItems[index].Column).GetString().Trim();
                bool notApplicable = text.Equals("N/A", StringComparison.OrdinalIgnoreCase);
                bool passed = notApplicable
                    || text.Equals("OK", StringComparison.OrdinalIgnoreCase)
                    || text.Equals("PASS", StringComparison.OrdinalIgnoreCase);

                items.Add(new QcItem
                {
                    ItemCode = $"QC{temperatures.Length + index + 1:000}",
                    TestName = textItems[index].Name,
                    MeasuredText = text,
                    MeasuredValue = null,
                    MinValue = null,
                    MaxValue = null,
                    Unit = null,
                    Result = passed ? "PASS" : "FAIL"
                });
            }

            return items;
        }

        private static double? ParseReportNumber(string text)
        {
            if (double.TryParse(text, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double invariantValue))
                return invariantValue;

            if (double.TryParse(text, NumberStyles.Float,
                    CultureInfo.CurrentCulture, out double currentValue))
                return currentValue;

            return null;
        }
    }
}
