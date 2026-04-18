using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace travelexpensemanagement.Models.Admin.Utilities
{
    public class MeshConversion
    {
        [JsonPropertyName("MESH_NAME")]
        public string MESH_NAME { get; set; }

        [JsonPropertyName("MESH_CODE")]
        public int MESH_CODE { get; set; }

        [JsonPropertyName("RUN_NO")]
        public int RUN_NO { get; set; }

        [JsonPropertyName("Records")]
        public List<MeshConversionRecord> Records { get; set; } = new();
    }

    public class MeshConversionRecord
    {
        [JsonPropertyName("BaseProduction")]
        public decimal? BaseProduction { get; set; }

        [JsonPropertyName("PRODUCTION")]
        public decimal? Production { get; set; }

        [JsonPropertyName("PER")]
        public decimal? Per { get; set; }
    }

    public class GetRequestMesh
    {
        public string Sno { get; set; }
        public string MeshName { get; set; }
        public int RunNo { get; set; }
    }
}
