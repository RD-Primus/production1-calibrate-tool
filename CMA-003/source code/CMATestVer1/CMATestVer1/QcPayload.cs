using Newtonsoft.Json;
using System.Collections.Generic;

namespace CMATestVer1
{
    internal class QcPayload
    {
        [JsonProperty("schema_version")] public int SchemaVersion { get; set; } = 2;
        [JsonProperty("update_mode")] public bool UpdateMode { get; set; } = false;
        [JsonProperty("model_code")] public string ModelCode { get; set; } = "CMA-003";
        [JsonProperty("lot_number")] public string LotNumber { get; set; } = "";
        [JsonProperty("ecn_number")] public string EcnNumber { get; set; } = "";
        [JsonProperty("serial_number")] public string SerialNumber { get; set; } = "";
        [JsonProperty("station_name")] public string StationName { get; set; } = "QC-STATION-01";
        [JsonProperty("operator_username")] public string OperatorUsername { get; set; } = "";
        [JsonProperty("start_time_inspection_datetime")] public string StartTime { get; set; } = "";
        [JsonProperty("inspection_datetime")] public string InspectionTime { get; set; } = "";
        [JsonProperty("template_name")] public string TemplateName { get; set; } = "CMA-003 QC Inspection.xlsx / Sheet1";
        [JsonProperty("description")] public string Description { get; set; } = "";
        [JsonProperty("firmware")] public List<QcFirmware> Firmware { get; set; } = new List<QcFirmware>();
        [JsonProperty("firmware_main")] public string FirmwareMain { get; set; } = "";
        [JsonProperty("firmware_display")] public string FirmwareDisplay { get; set; } = "";
        [JsonProperty("powersupply")] public string PowerSupply { get; set; } = "220 VAC 50 Hz";
        [JsonProperty("equipment_code")] public List<QcEquipment> EquipmentCodes { get; set; } = new List<QcEquipment>();
        [JsonProperty("approve")] public QcApproval Approve { get; set; } = new QcApproval();
        [JsonProperty("remark")] public string Remark { get; set; } = "";
        [JsonProperty("uuid")] public string Uuid { get; set; } = "";
        [JsonProperty("inspection_status")] public string InspectionStatus { get; set; } = "final";
        [JsonProperty("product_name")] public string ProductName { get; set; } = "CMA-003 QC Inspection";
        [JsonProperty("report_serial_number")] public string ReportSerialNumber { get; set; } = "";
        [JsonProperty("jig_name")] public string JigName { get; set; } = "CMA-003-JIG-01";
        [JsonProperty("report_date")] public string ReportDate { get; set; } = "";
        [JsonProperty("overall_result")] public string OverallResult { get; set; } = "PASS";
        [JsonProperty("acceptance_checks")] public List<QcAcceptanceCheck> AcceptanceChecks { get; set; } = new List<QcAcceptanceCheck>();
        [JsonProperty("items")] public List<QcItem> Items { get; set; } = new List<QcItem>();
        [JsonProperty("report_mapping")] public QcReportMappingConfig ReportMapping { get; set; } = new QcReportMappingConfig();
        [JsonProperty("images")] public List<object> Images { get; set; } = new List<object>();
    }
}
