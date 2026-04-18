using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace travelexpensemanagement.Models.QualityControl.Transaction
{
    public class IncommingQC
    {
        [JsonPropertyName("header")]
        public QCHeader Header { get; set; }

        [JsonPropertyName("items")]
        public List<QCItem> Items { get; set; } = new List<QCItem>();
    }

    public class QCHeader
    {
        [JsonPropertyName("DocType")]
        public string DocType { get; set; }

        [JsonPropertyName("V_TYPE")]
        public string V_TYPE { get; set; }

        [JsonPropertyName("v_NO")]
        public string v_NO { get; set; }

        [JsonPropertyName("DocNo")]
        public string DocNo { get; set; }

        [JsonPropertyName("DocDate")]
        public DateTime DocDate { get; set; }

        [JsonPropertyName("MRNNo")]
        public string MRNNo { get; set; }

        [JsonPropertyName("QCIncharge")]
        public string QCIncharge { get; set; }

        [JsonPropertyName("Chemist")]
        public string Chemist { get; set; }

        [JsonPropertyName("PartyCode")]
        public int PartyCode { get; set; }

        [JsonPropertyName("Transport")]
        public string Transport { get; set; }

        [JsonPropertyName("InvoiceQty")]
        public decimal InvoiceQty { get; set; }

        [JsonPropertyName("RecordedQty")]
        public decimal RecordedQty { get; set; }

        [JsonPropertyName("PurchaseType")]
        public string PurchaseType { get; set; }

        [JsonPropertyName("Wastage")]
        public decimal Wastage { get; set; }

        [JsonPropertyName("MRNDate")]
        public DateTime MRNDate { get; set; }

        [JsonPropertyName("Bales")]
        public decimal Bales { get; set; }

        [JsonPropertyName("BillNo")]
        public string BillNo { get; set; }

        [JsonPropertyName("BillDate")]
        public DateTime BillDate { get; set; }

        [JsonPropertyName("TruckNo")]
        public string TruckNo { get; set; }

        [JsonPropertyName("Shortage")]
        public decimal Shortage { get; set; }

        [JsonPropertyName("DeductionAmount")]
        public decimal DeductionAmount { get; set; }

        [JsonPropertyName("DeductionNarration")]
        public string DeductionNarration { get; set; }

        [JsonPropertyName("Remarks")]
        public string Remarks { get; set; }

        [JsonPropertyName("ACTION")]
        public string ACTION { get; set; }
    }

    public class QCItem
    {
        [JsonPropertyName("iteM_CODE")]
        public int ItemCode { get; set; }

        [JsonPropertyName("particulaR_NAME")]
        public string ParticularName { get; set; }

        [JsonPropertyName("uniT_NAME")]
        public string UnitName { get; set; }

        [JsonPropertyName("stD_LEVEL")]
        public decimal StdLevel { get; set; }

        [JsonPropertyName("result")]
        public string Result { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; }

        [JsonPropertyName("deduction_amt")]
        public decimal DeductionAmt { get; set; }

        [JsonPropertyName("allow_amt")]
        public decimal AllowAmt { get; set; }

        [JsonPropertyName("deduction_narration")]
        public string DeductionNarration { get; set; }

        [JsonPropertyName("qcP_CODE")]
        public int QCPCode { get; set; }

        [JsonPropertyName("nos")]
        public int Nos { get; set; }
    }
}
