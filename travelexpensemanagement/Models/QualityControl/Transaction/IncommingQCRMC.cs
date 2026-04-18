using System.Text.Json.Serialization;

namespace travelexpensemanagement.Models.QualityControl.Transaction
{
    public class IncommingQCRMC
    {
    }

    public class IncommingQCRMHeaderDto
    {
        [JsonPropertyName("docType")]
        public string DocType { get; set; }

        [JsonPropertyName("docNo")]
        public string? DocNo { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("gateNo")]
        public string GateNo { get; set; }

        [JsonPropertyName("qcIncharge")]
        public string QcIncharge { get; set; }

        [JsonPropertyName("chem")]
        public string Chem { get; set; }

        [JsonPropertyName("partyName")]
        public string PartyName { get; set; }

        [JsonPropertyName("transport")]
        public string Transport { get; set; }

        [JsonPropertyName("truckNo")]
        public string TruckNo { get; set; }

        [JsonPropertyName("containerNo")]
        public string ContainerNo { get; set; }

        [JsonPropertyName("invoiceQty")]
        public decimal? InvoiceQty { get; set; }

        [JsonPropertyName("recordedQty")]
        public decimal? RecordedQty { get; set; }

        [JsonPropertyName("purType")]
        public string PurType { get; set; }

        [JsonPropertyName("shortage")]
        public decimal? Shortage { get; set; }

        [JsonPropertyName("billNo")]
        public int? BillNo { get; set; }

        [JsonPropertyName("billDate")]
        public string BillDate { get; set; }

        [JsonPropertyName("billDateChecked")]
        public bool BillDateChecked { get; set; }

        [JsonPropertyName("wastage")]
        public decimal? Wastage { get; set; }

        [JsonPropertyName("gateDate")]
        public string GateDate { get; set; }

        [JsonPropertyName("gateDateChecked")]
        public bool GateDateChecked { get; set; }

        [JsonPropertyName("bales")]
        public int? Bales { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; }

        [JsonPropertyName("MRNDate")]
        public string MRNDate { get; set; }

        [JsonPropertyName("DeductAmount")]
        public string DeductAmount { get; set; }

        [JsonPropertyName("Narration")]
        public string Narration { get; set; }

        public string? ACTION { get; set; }
    }
    public class IncommingDetailDto
    {
        public int RowIndex { get; set; }
        public string QC_CODE { get; set; }
        public string QCP_CODE { get; set; }
        public string Parameter { get; set; }
        public string Unit { get; set; }
        public string QCP_STD { get; set; }
        public string AllowAmt { get; set; }
        public string DeductAmt { get; set; }
        public string DeductNarr { get; set; }
        public List<Dictionary<string, string>> Items { get; set; }
    }
    public class IncommingSaveRequest
    {
        public IncommingQCRMHeaderDto Header { get; set; }
        public List<IncommingDetailDto> Details { get; set; }
        //public Dictionary<string, List<string>> CombinedItems { get; set; }
    }
    public class GateIncommingQCRM
    {
        public List<Dictionary<string, object>> Header { get; set; } = new();
        public List<Dictionary<string, object>> Items { get; set; } = new();
    }
}
