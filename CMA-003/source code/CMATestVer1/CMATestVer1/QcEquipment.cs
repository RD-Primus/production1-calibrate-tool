using Newtonsoft.Json;

namespace CMATestVer1
{
    internal sealed class QcEquipment
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("series")]
        public string Series { get; set; } = "";

        [JsonProperty("equipment_id")]
        public string EquipmentId { get; set; } = "";

        [JsonProperty("expire_date", NullValueHandling = NullValueHandling.Ignore)]
        public string? ExpireDate { get; set; }

        [JsonIgnore]
        public string? Expire
        {
            get => ExpireDate;
            set => ExpireDate = value;
        }
    }
}
