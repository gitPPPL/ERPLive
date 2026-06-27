using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class QuotationRateApproval
    {
        public int? V_NO { get; set; }
        public string? V_DOCID { get; set; }
        public DateTime? V_DATE { get; set; }
        public string ? V_type { get; set; }
        public int? status { get; set; }    
        public int? groupCd { get; set; }
        public string? sortType { get; set; }
        public string? SaveOrUpdate { get; set; }
        public List<QuotationRateApprovalDetail> quotationRateApprovalDetail { get; set; }
        public List<QuotatRateApprovalAttachment> quotatRateApprovalAttachment { get; set; }
    }

    public class QuotatRateApprovalAttachment
    {
        public string FileName { get; set; }      // File display name or input name
        public long? FileSize { get; set; }       // Size in bytes, nullable because it can be missing
        public string FileType { get; set; }      // MIME type (e.g. "image/jpeg")
        public string FilePath { get; set; }      // P
        public string FileContentBase64 { get; set; }
    }
    //public class QuotatRateApprovalAttachment
    //{
    //    public IFormFile File { get; set; }
    //    public string FileName { get; set; }
    //}

    public class QuotationRateApprovalDetail
    {
        public int? PARTY_CODE { get; set; }
        public int? ITEM_CODE { get; set; }
        public int? MAKE_CODE { get; set; }
        public string TECH_DESC { get; set; }
        public int? UOM_CODE { get; set; }
        public int? REF_NO { get; set; }
        public DateTime? REF_DATE { get; set; }
        public string REF_TYPE { get; set; }
        public string REF_DOCID { get; set; }

        public decimal? QTY { get; set; }
        public decimal? RATE { get; set; }
        public decimal? AMOUNT { get; set; }
        public decimal? PACK_PER { get; set; }
        public decimal? PACK_AMT { get; set; }
        public decimal? DISC_PER { get; set; }
        public decimal? DISC_AMT { get; set; }
        public decimal? FREIGHT { get; set; }

        public int? TAX_CODE { get; set; }
        public decimal? CGST_PER { get; set; }
        public decimal? CGST_AMT { get; set; }
        public decimal? SGST_PER { get; set; }
        public decimal? SGST_AMT { get; set; }
        public decimal? IGST_PER { get; set; }
        public decimal? IGST_AMT { get; set; }
        public decimal? VAT_PER { get; set; }
        public decimal? VAT_AMT { get; set; }
        public decimal? CESS_PER { get; set; }
        public decimal? CESS_AMT { get; set; }

        public decimal? OTH_EXPS { get; set; }
        public decimal? LD_RATE { get; set; }
        public decimal? NET_AMT { get; set; }
        public decimal? BULK_QTY { get; set; }
        public decimal? BULK_RATE { get; set; }
        public decimal? BULK_DISC_PER { get; set; }
        public decimal? BULK_DISC_AMT { get; set; }

        public string WARRANTY { get; set; }
        public int? LEADTIME_DAYS { get; set; }
        public string PURCHASER_REMARKS { get; set; }
        public int? PREORITY_LEVEL { get; set; }

        public decimal? RATE_MONTHLY { get; set; }
        public decimal? RATE_QUARTERLY { get; set; }
        public decimal? RATE_ANNUALY { get; set; }
        public decimal? RATE_SPECIAL { get; set; }

        public string REQ_TYPE { get; set; }
        public int? REQ_NO { get; set; }
        public int? STATUS { get; set; }
        public int? APROV_CODE { get; set; }
        public string APROV_STATUS { get; set; }
        public string APROV_REMARKS { get; set; }
        public string FAPROV_STATUS { get; set; }
        public string FAPROV_REMARKS { get; set; }

        public string PACK_UR { get; set; }
        public string DISC_UR { get; set; }
        public string FREIGHT_UR { get; set; }
        public string CGST_UR { get; set; }
        public string SGST_UR { get; set; }
        public string IGST_UR { get; set; }
        public string OTHEXP_UR { get; set; }
        public string BULKDISC_UR { get; set; }

        public int? AUTOPO_FLG { get; set; }
        public string DOC_ID { get; set; }
    }

    public class FilterItemload
    {
        public DateTime? VDate { get; set; }
        public string? FromDt { get; set; }
        public string? ToDt { get; set; }
        public int? groupCode { get; set; }
        public string? SortBy { get; set; }
        public List<string>? VendorList { get; set; } 
        public List<string>? ItemList { get; set; } 
    }

     

}