using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace travelexpensemanagement.Models.Admin.Utilities
{
    public class MakeConversionRequest
    {
        [JsonPropertyName("MAKE_TYPE")]
        public string MAKE_TYPE { get; set; }

        [JsonPropertyName("RUN_NO")]
        public string RUN_NO { get; set; }

        [JsonPropertyName("Records")]
        public List<MakeConversionRecord> Records { get; set; } = new();
    }

    public class MakeConversionRecord
    {
        [JsonPropertyName("Production")]
        public string Production { get; set; }

        [JsonPropertyName("Per")]
        public string Per { get; set; }

        [JsonPropertyName("BaseProduction")]
        public string BaseProduction { get; set; }

        [JsonPropertyName("Flg")]
        public int Flg { get; set; }
    }
    public class GetRequest
    {
        public string Sno { get; set; }
        public string MakeType { get; set; }
        public int RunNo { get; set; }
    }

}
