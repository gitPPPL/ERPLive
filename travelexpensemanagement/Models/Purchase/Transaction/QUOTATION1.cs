using System.Text.Json.Serialization;

namespace travelexpensemanagement.Models.Purchase.Transiction
{
    public class QUOTATION1
    {
        public int? YEAR_CODE { get; set; }
        public int? COMP_CODE { get; set; }
        public int? BRANCH_CODE { get; set; }
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? PARTY_CODE { get; set; }
        public string? PARTY_NAME { get; set; }
        public int? OLD_NO { get; set; }
        public string? QUOTE_NO { get; set; }
        public DateTime? QUOTE_DATE { get; set; }
        public string? CONT_PERSON { get; set; }
        public DateTime? VALID_DATE { get; set; }
        public string? REMARKS { get; set; }
        public string? PRICE_TYPE { get; set; }
        public int? STATUS { get; set; }
        public string? STATUS_NAME { get; set; }
        public decimal? QTY { get; set; }
        public decimal? AMOUNT { get; set; }
        public decimal? FREIGHT_AMT { get; set; }
        public int? GROUP_NO { get; set; }
        public string? DELIVERY_TERM { get; set; }
        public string? FREIGHT_TERM { get; set; }
        public int? PAYTERM_CODE { get; set; }
        public string? PAYMENT_TERM { get; set; }
        public decimal? PACK_AMT { get; set; }
        public decimal? DISC_AMT { get; set; }
        public decimal? CGST_AMT { get; set; }
        public decimal? SGST_AMT { get; set; }
        public decimal? IGST_AMT { get; set; }
        public decimal? VAT_AMT { get; set; }
        public decimal? OTH_AMT { get; set; }
        public decimal? CESS_AMT { get; set; }
        public decimal? NET_AMT { get; set; }
        public decimal? BULK_QTY { get; set; }
        public decimal? BULK_DISCAMT { get; set; }
        public string? DOC_ID { get; set; }
        public string? FAPROV_STATUS { get; set; }
        public string? FAPROV_REMARKS { get; set; }
        public int? MAILSEND { get; set; }
        public int? UUSER { get; set; }
        public DateTime? UDATE { get; set; }
        public int? EUSER { get; set; }
        public DateTime? EDATE { get; set; }
        public string? AED { get; set; }
        public string? WSID { get; set; }
        public string? LIP { get; set; }
        public string? LID { get; set; }
        public int? SRNO { get; set; }
        public string? ACTION { get; set; }
        public List<QUOTATION2>? QUOT2 { get; set; }
        public string? IMPORT_CURRENCY { get; set; }
        public decimal? EXRATE { get; set; }
        // public List<QUOTATION3>? QUOT3 { get; set; }
    }

    public class QuotationWrapper
    {
        public QUOTATION1 header { get; set; }
        public List<QUOTATION2> lineRows { get; set; }
        public List<QUOTATION3> Attachement { get; set; }
    }
}
