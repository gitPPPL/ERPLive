namespace travelexpensemanagement.Models.QualityControl.Transaction
{
    using System.Text.Json.Serialization;
    using System.Collections.Generic;

    public class SampleQCHeaderDto
    {
        [JsonPropertyName("docType")]
        public string DocType { get; set; }

        [JsonPropertyName("docNo")]
        public string? DocNo { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("qcIncharge")]
        public string QcIncharge { get; set; }

        [JsonPropertyName("chem")]
        public string Chem { get; set; }

        [JsonPropertyName("truckNo")]
        public string TruckNo { get; set; }



        [JsonPropertyName("partyCode")]
        public string PartyCode { get; set; }

        [JsonPropertyName("ContainerNo")]
        public string? ContainerNo { get; set; }

        [JsonPropertyName("transportName")]
        public string TransportName { get; set; }

        [JsonPropertyName("recdQty")]
        public decimal? RecdQty { get; set; }

        [JsonPropertyName("add1")]
        public string Add1 { get; set; }

        [JsonPropertyName("add2")]
        public string? Add2 { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; }

        [JsonPropertyName("sampleRecordedBy")]
        public string SampleRecordedBy { get; set; }

   
        [JsonPropertyName("qcInchargeName")]
        public string QcInchargeName { get; set; }

        [JsonPropertyName("chemist")]
        public string Chemist { get; set; }

        [JsonPropertyName("chemistName")]
        public string ChemistName { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }
    }


    public class SampleQCDetailDto
    {
        [JsonPropertyName("RowIndex")]
        public int? RowIndex { get; set; }

        [JsonPropertyName("QC_CODE")]
        public string? QC_CODE { get; set; }

        [JsonPropertyName("QCP_CODE")]
        public string? QCP_CODE { get; set; }

        [JsonPropertyName("Parameter")]
        public string? Parameter { get; set; }

        [JsonPropertyName("Unit")]
        public string? Unit { get; set; }          // ← fixed backtick

        [JsonPropertyName("Level")]
        public string? Level { get; set; }         // ← matches JS (instead of QCP_STD)

        [JsonPropertyName("AllowAmt")]
        public decimal? AllowAmt { get; set; }      // consider decimal?

        [JsonPropertyName("DeductAmt")]
        public decimal? DeductAmt { get; set; }     // consider decimal?

        [JsonPropertyName("DeductNarr")]
        public string? DeductNarr { get; set; }

        // Since JS uses an array, define a matching list type
        [JsonPropertyName("Items")]
        public List<SampleQCDetailItemDto> Items { get; set; } = new();
    }

    public class SampleQCDetailItemDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }


    public class SampleQCSaveRequest
    {
        [JsonPropertyName("header")]
        public SampleQCHeaderDto Header { get; set; }
        [JsonPropertyName("details")]
        public List<SampleQCDetailDto> Details { get; set; }
        //public Dictionary<string, List<string>> CombinedItems { get; set; }
    }
    public class GateSamepleQCRM
    {
        public List<Dictionary<string, object>> Header { get; set; } = new();
        public List<Dictionary<string, object>> Items { get; set; } = new();
    }

}
