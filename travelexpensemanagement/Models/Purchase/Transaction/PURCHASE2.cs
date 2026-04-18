using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Models.Purchase.Transiction
{
    public class PURCHASE2
    {
        public int? YEAR_CODE { get; set; }
        public int? COMP_CODE { get; set; }
        public int? BRANCH_CODE { get; set; }
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? DOC_ID { get; set; } 
        public int? SNO { get; set; }
        public int? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public int? MAKE_CODE { get; set; }
        public string? HSN_CODE { get; set; }
        public string? RCM_YN { get; set; }
        public string? INPUT_YN { get; set; }
        public int? UOM_CODE { get; set; }
        public string? UOM_NAME { get; set; }
        public int? DEPT_CODE { get; set; }
        public int? NOS { get; set; }
        public decimal? PLUS_MINUSQTY { get; set; }
        public decimal? WB_QTY { get; set; }
        public decimal? RECD_QTY { get; set; }
        public decimal? BILL_QTY { get; set; }
        public decimal? USD_RATE { get; set; }
        public decimal? EXCH_RATE { get; set; }
        public decimal? RATE { get; set; }
        public decimal? AMOUNT { get; set; }
        public decimal? DISC_PER { get; set; }
        public decimal? DISC_AMT { get; set; }
        public decimal? PACK_PER { get; set; }
        public decimal? PACK_AMT { get; set; }
        public int? TAX_CODE { get; set; }
        public decimal? CGST_PER { get; set; }
        public decimal? CGST_AMT { get; set; }
        public decimal? SGST_PER { get; set; }
        public decimal? SGST_AMT { get; set; }
        public decimal? IGST_PER { get; set; }
        public decimal? IGST_AMT { get; set; }
        public decimal? CESS_PER { get; set; }
        public decimal? CESS_AMT { get; set; }
        public decimal? VAT_PER { get; set; }
        public decimal? VAT_AMT { get; set; }
        public decimal? OTH_AMT { get; set; }
        public decimal? NET_AMT { get; set; }
        public decimal? LAND_RATE { get; set; }
        public decimal? LAND_AMT { get; set; }
        public decimal? POLAND_RATE { get; set; }
        public decimal? PO_RATE { get; set; }
        public string? BIN_LOCATION { get; set; }
        public int? BIN_CODE { get; set; }
        public string? PO_TYPE { get; set; }
        public int? PO_NO { get; set; }
        public string? SAUDA_TYPE { get; set; }
        public int? SAUDA_NO { get; set; }
        public string? KANTA_TYPE { get; set; }
        public int? KANTA_NO { get; set; }
        public string? REQ_TYPE { get; set; }
        public int? REQ_NO { get; set; }
        public string? GATE_TYPE { get; set; }
        public int? GATE_NO { get; set; }
        public string? REF_TYPE { get; set; }
        public int? REF_NO { get; set; }
        public string? QC_TYPE { get; set; }
        public int? QC_NO { get; set; }
        public string? PASS_TYPE { get; set; }
        public int? PASS_NO { get; set; }
        public string? EMPTY_YN { get; set; }
        public int? MACH_CODE { get; set; }
        public string? REMARKS { get; set; }
        public decimal? RATE_MONTHLY { get; set; }
        public decimal? RATE_QUARTERLY { get; set; }
        public decimal? RATE_ANNUALY { get; set; }
        public decimal? RATE_SPECIAL { get; set; }
        public string? FINAL_LOCK { get; set; }
        public int? UUSER { get; set; }
        public DateTime? UDATE { get; set; }
        public int? EUSER { get; set; }
        public DateTime? EDATE { get; set; }
        public string? AED { get; set; }
        public string? WSID { get; set; }
        public string? LIP { get; set; }
        public string? LID { get; set; }
    }
    public class PurchaseWrapper
    {
        public PURCHASE1 header { get; set; }
        public List<PURCHASE2> lineRows { get; set; }
        public List<PURCHASE3> Attachement { get; set; }
    }   
}
