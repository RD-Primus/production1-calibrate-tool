using Newtonsoft.Json;
using System.Collections.Generic;

namespace CMATestVer1
{
    public class QcItem
    {
        [JsonProperty("item_code")] public string ItemCode { get; set; } = "";
        [JsonProperty("test_name")] public string TestName { get; set; } = "";
        [JsonProperty("required")] public bool Required { get; set; } = false;
        [JsonProperty("measured_text")] public string MeasuredText { get; set; } = "";
        [JsonProperty("measured_value")] public double? MeasuredValue { get; set; }
        [JsonProperty("minValue")] public double? MinValue { get; set; }
        [JsonProperty("maxValue")] public double? MaxValue { get; set; }
        [JsonProperty("unit")] public string? Unit { get; set; }
        [JsonProperty("result")] public string Result { get; set; } = "";
        [JsonProperty("report_mapping")] public List<QcItemReportMapping> ReportMapping { get; set; } = new List<QcItemReportMapping>();
    }
}
