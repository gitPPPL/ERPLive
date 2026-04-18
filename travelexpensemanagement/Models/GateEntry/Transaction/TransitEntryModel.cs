using System;

namespace travelexpensemanagement.Models
{
    public class TransitEntryModel
    {
          public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public string? DOC_ID { get; set; }
        public string? FORM_NO { get; set; }
        public DateTime? FORM_DATE { get; set; }
        public DateTime? EXPIRY_DATE { get; set; }
        public int? PARTY_CODE { get; set; }
        public string? PARTY_GSTIN { get; set; }
        public string? BILL_NO { get; set; }
        public DateTime? BILL_DATE { get; set; }
        public string? GR_NO { get; set; }
        public DateTime? GR_DATE { get; set; }
        public string? TRUCK_NO { get; set; }
        public string? TRANSPORT { get; set; }
        public string? PO_STATUS { get; set; }
        public string? ORD_TYPE { get; set; }
        public int? ORD_NO { get; set; }
        public int? HSN_CODE { get; set; }
        public string? ITEM_DESC { get; set; }
        public decimal? NOS { get; set; }
        public decimal? BILL_AMT { get; set; }
        public decimal? SGST_AMT { get; set; }
        public decimal? CGST_AMT { get; set; }
        public decimal? IGST_AMT { get; set; }
        public decimal? CESS_AMT { get; set; }
        public decimal? CESS_NONADVOLAMT { get; set; }
        public decimal? OTHER_AMT { get; set; }
        public decimal? TOTAL_AMT { get; set; }
        public string? GATE_TYPE { get; set; }
        public int? GATE_NO { get; set; }
        public DateTime? GATE_DATE { get; set; }
        public int? STATUS { get; set; }
        public string? OTHER_GSTIN { get; set; }
        public DateTime? ARRIVAL_DATE { get; set; }

        public string? action { get; set; }
        public string? Doctype_Name { get; set; }
        public string? partyname { get; set; }


        public int? UUSER { get; set; }


        public DateTime? UDATE { get; set; }




    }
}
