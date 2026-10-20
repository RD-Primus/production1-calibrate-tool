using Newtonsoft.Json;

namespace CMATestVer1
{
    internal sealed class QcFirmware
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("version")]
        public string Version { get; set; } = "";
    }
}
