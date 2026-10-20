using Newtonsoft.Json;
using System.Globalization;
using System.Text;

namespace CMATestVer1
{
    public partial class Form1
    {
        private async void sendTosever_Click(object sender, EventArgs e)
        {
            await SendSampleResultToServerAsync();
        }

        private async Task SendSampleResultToServerAsync()
        {
            if (!TryGetQcResultApiUrl(out string apiUrl))
                return;

            string apiKey = txt_token.Text.Trim();
            if (apiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                apiKey = apiKey.Substring("Bearer ".Length).Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                BigMessageBox.Show(
                    "กรุณากรอก Device Token (API Key) ในช่อง Token ก่อนส่งข้อมูลตัวอย่าง!",
                    "Warning",
                    MessageBoxIcon.Warning,
                    MessageBoxButtons.OK,
                    fontSize: 14f);
                txt_token.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_operatorToken))
            {
                BigMessageBox.Show(
                    "กรุณากดปุ่ม Login เพื่อเข้าสู่ระบบก่อนส่งข้อมูลตัวอย่าง!",
                    "Warning",
                    MessageBoxIcon.Warning,
                    MessageBoxButtons.OK,
                    fontSize: 14f);
                button1.Focus();
                return;
            }

            DateTime now = DateTime.Now;
            string sampleSerialNumber = $"TEST{now:yyyyMMddHHmmss}";
            string sampleLotNumber = string.IsNullOrWhiteSpace(txtLot.Text)
                ? "TEST_LOT"
                : txtLot.Text.Trim();

            DialogResult confirm = BigMessageBox.Show(
                $"ต้องการส่งข้อมูลตัวอย่างไปยัง Server หรือไม่?\n\nS/N: {sampleSerialNumber}\nLOT: {sampleLotNumber}",
                "Confirm Sample Post",
                MessageBoxIcon.Question,
                MessageBoxButtons.YesNo,
                fontSize: 14f);

            if (confirm != DialogResult.Yes)
                return;

            QcPayload payload = BuildSampleQcPayload(sampleSerialNumber, sampleLotNumber, now);
            string jsonBody = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.Indented
            });

            sendTosever.Enabled = false;

            try
            {
                lblResultStatus.Text = "⏳ กำลังส่งข้อมูลตัวอย่างไป Server...";
                lblResultStatus.ForeColor = Color.Orange;
                LogToRx($"[SAMPLE POST] กำลังส่ง S/N: {sampleSerialNumber}", Color.Blue);
                RxBox.AppendText($"\r\n[SAMPLE REQUEST]\r\n{jsonBody}\r\n");

                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Headers.TryAddWithoutValidation("X-API-KEY", apiKey);
                request.Headers.TryAddWithoutValidation("X-OPERATOR-TOKEN", _operatorToken);
                request.Content = content;
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using HttpResponseMessage response = await client.SendAsync(request, timeout.Token);
                string responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    lblResultStatus.Text = $"✅ ส่งข้อมูลตัวอย่าง S/N: {sampleSerialNumber} สำเร็จ";
                    lblResultStatus.ForeColor = Color.Green;
                    RxBox.AppendText($"\r\n[SAMPLE RESPONSE SUCCESS]\r\n{responseText}\r\n");

                    BigMessageBox.Show(
                        $"ส่งข้อมูลตัวอย่างสำเร็จ!\n\nS/N: {sampleSerialNumber}",
                        "Sample Post Success",
                        MessageBoxIcon.Information,
                        MessageBoxButtons.OK,
                        fontSize: 14f);
                }
                else
                {
                    lblResultStatus.Text = $"❌ ส่งข้อมูลตัวอย่างไม่สำเร็จ: {(int)response.StatusCode} {response.StatusCode}";
                    lblResultStatus.ForeColor = Color.Red;
                    RxBox.AppendText($"\r\n[SAMPLE RESPONSE ERROR {(int)response.StatusCode}]\r\n{responseText}\r\n");

                    BigMessageBox.Show(
                        $"Server ไม่รับข้อมูลตัวอย่าง\n\nStatus: {(int)response.StatusCode} {response.StatusCode}\nตรวจรายละเอียดเพิ่มเติมในกล่องข้อความ",
                        "Sample Post Failed",
                        MessageBoxIcon.Error,
                        MessageBoxButtons.OK,
                        fontSize: 14f);
                }
            }
            catch (OperationCanceledException)
            {
                lblResultStatus.Text = "⚠ ส่งข้อมูลตัวอย่างไม่สำเร็จ: Server ตอบกลับช้าเกิน 10 วินาที";
                lblResultStatus.ForeColor = Color.DarkRed;
                LogToRx("[SAMPLE POST TIMEOUT] Server ไม่ตอบกลับภายใน 10 วินาที", Color.Red);

                BigMessageBox.Show(
                    "ไม่สามารถส่งข้อมูลตัวอย่างได้ เนื่องจาก Server ไม่ตอบกลับภายใน 10 วินาที",
                    "Server Timeout",
                    MessageBoxIcon.Error,
                    MessageBoxButtons.OK,
                    fontSize: 14f);
            }
            catch (Exception ex)
            {
                lblResultStatus.Text = "⚠ ไม่สามารถเชื่อมต่อ Server สำหรับส่งข้อมูลตัวอย่างได้";
                lblResultStatus.ForeColor = Color.DarkRed;
                LogToRx($"[SAMPLE POST EXCEPTION] {ex.Message}", Color.Red);

                BigMessageBox.Show(
                    $"ไม่สามารถเชื่อมต่อ Server ได้\n\n{ex.Message}",
                    "Sample Post Error",
                    MessageBoxIcon.Error,
                    MessageBoxButtons.OK,
                    fontSize: 14f);
            }
            finally
            {
                sendTosever.Enabled = true;
            }
        }

        private QcPayload BuildSampleQcPayload(string serialNumber, string lotNumber, DateTime inspectionTime)
        {
            DateTime inspectionStart = inspectionTime.AddMinutes(-1);
            string startIso = new DateTimeOffset(inspectionStart).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
            string endIso = new DateTimeOffset(inspectionTime).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
            string reportDate = inspectionTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            return new QcPayload
            {
                SchemaVersion = 2,
                UpdateMode = false,
                ModelCode = "CMA-003",
                LotNumber = lotNumber,
                EcnNumber = txtECN.Text.Trim(),
                SerialNumber = serialNumber,
                StationName = "QC-STATION-01",
                OperatorUsername = string.IsNullOrWhiteSpace(_operatorUsername) ? "admin" : _operatorUsername,
                StartTime = startIso,
                InspectionTime = endIso,
                TemplateName = "CMA-003 QC Inspection.xlsx / Sheet1",
                Description = DescriptionPassText,
                Firmware = new List<QcFirmware>
                {
                    new QcFirmware { Name = "AirControl", Version = _firmwareValue },
                    new QcFirmware { Name = "MCU_DISPLAY_AIR_CONTROL", Version = _displayFirmwareValue }
                },
                FirmwareMain = _firmwareValue,
                FirmwareDisplay = _displayFirmwareValue,
                PowerSupply = "220 VAC 50 Hz",
                EquipmentCodes = GetEquipmentCodesForPayload(),
                Approve = BuildQcApproval(inspectionTime),
                Remark = "SAMPLE DATA - API CONNECTION TEST - DO NOT USE FOR PRODUCTION",
                Uuid = Guid.NewGuid().ToString(),
                InspectionStatus = "final",
                ProductName = "CMA-003 QC Inspection",
                ReportSerialNumber = serialNumber,
                JigName = "CMA-003-JIG-01",
                ReportDate = reportDate,
                OverallResult = "PASS",
                AcceptanceChecks = BuildCmaAcceptanceChecks(),
                Items = new List<QcItem>
                {
                    CreateQcTextItem("CMA003_INPUT_2000_OHM", "จ่าย Input 2000 Ohm", "OK", "PASS", new List<string> { "จ่าย Input 2000 Ohm" }, offsetX: 1),
                    CreateQcTextItem("CMA003_WL", "WL", "OK", "PASS", new List<string> { "WL" }, offsetX: 1),
                    CreateQcTextItem("CMA003_WL_AL2", "WL/Al2", "OK", "PASS", new List<string> { "WL/Al2" }, offsetX: 0),
                    CreateQcTextItem("CMA003_HP", "HP", "OK", "PASS", new List<string> { "HP" }, offsetX: 1),
                    CreateQcTextItem("CMA003_HP_AL2", "HP/Al2", "OK", "PASS", new List<string> { "HP/Al2" }, offsetX: 0),
                    CreateQcTextItem("CMA003_ALARM1", "Alarm1", "OK", "PASS", new List<string> { "Alarm1" }, offsetX: 1),

                    CreateQcTextItem("CMA003_FAN_HOT", "Fan / Hot", "OK", "PASS", new List<string> { "Fan", "Hot" }, offsetX: 0),
                    CreateQcTextItem("CMA003_FAN_COOL", "Fan / Cool", "OK", "PASS", new List<string> { "Fan", "Cool" }, offsetX: 0),
                    CreateQcTextItem("CMA003_COMPRESSOR", "Compressor", "OK", "PASS", new List<string> { "Compressor" }, offsetX: 0),

                    CreateQcNumericItem("CMA003_200_99", "จ่าย 200 / 99", 99.0, "PASS", 98.0, 100.0, new List<string> { "จ่าย 200", "99" }),
                    CreateQcNumericItem("CMA003_2000_25_6", "จ่าย 2000 / 25.6", 25.6, "PASS", 24.6, 26.6, new List<string> { "จ่าย 2000", "25.6" }),
                    CreateQcNumericItem("CMA003_8000_M6_4", "จ่าย 8000 / -6.4", -6.4, "PASS", -7.4, -5.4, new List<string> { "จ่าย 8000", "-6.4" }),

                    CreateQcNumericItem("CMA003_2435_20", "จ่าย 2435 / 20", 20.0, "PASS", 19.0, 21.0, new List<string> { "จ่าย 2435", "20" }),
                    CreateQcNumericItem("CMA003_1653_30", "จ่าย 1653 / 30", 30.0, "PASS", 29.0, 31.0, new List<string> { "จ่าย 1653", "30" }),
                    CreateQcNumericItem("CMA003_1150_40", "จ่าย 1150 / 40", 40.0, "PASS", 39.0, 41.0, new List<string> { "จ่าย 1150", "40" }),
                    CreateQcNumericItem("CMA003_818_50", "จ่าย 818 / 50", 50.0, "PASS", 49.0, 51.0, new List<string> { "จ่าย 818", "50" }),

                    CreateQcTextItem("CMA003_F", "ปุ่ม F", "OK", "PASS", new List<string> { "ปุ่ม F" }, offsetX: 0),
                    CreateQcTextItem("CMA003_ITEM_018", "ปุ่ม ลด", "OK", "PASS", new List<string> { "ปุ่ม ลด" }, offsetX: 0),
                    CreateQcTextItem("CMA003_ITEM_019", "ปุ่ม เพิ่ม", "OK", "PASS", new List<string> { "ปุ่ม เพิ่ม" }, offsetX: 0),
                    CreateQcTextItem("CMA003_LED", "LED ติดครบ", "OK", "PASS", new List<string> { "LED ติดครบ" }, offsetX: 0)
                },
                ReportMapping = BuildCmaReportMapping(),
                Images = new List<object>()
            };
        }
    }
}
