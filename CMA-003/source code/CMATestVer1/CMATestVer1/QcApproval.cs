using Newtonsoft.Json;

namespace CMATestVer1
{
    internal sealed class QcApproval
    {
        [JsonProperty("tester")]
        public QcApprovalTester Tester { get; set; } = new QcApprovalTester();

        [JsonProperty("confirm")]
        public QcApprovalOfficer Confirm { get; set; } = new QcApprovalOfficer();

        [JsonProperty("approver")]
        public QcApprovalOfficer Approver { get; set; } = new QcApprovalOfficer();
    }

    internal sealed class QcApprovalTester
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("datetime")]
        public string DateTime { get; set; } = "";
    }

    internal sealed class QcApprovalOfficer
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("datetime")]
        public string? DateTime { get; set; }
    }
}
