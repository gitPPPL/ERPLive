namespace travelexpensemanagement.Models.Sales.Transaction
{
    public class ExportDetailsModel
    {
     
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public string? REF_TYPE { get; set; }
        public int? REF_NO { get; set; }
        public string? INCO_TERM { get; set; }
        public decimal? EX_RATE { get; set; }
        public string? POL_CODE { get; set; }
        public string? POD_CODE { get; set; }
        public int? FINAL_COUNTRY { get; set; }
        public string? BL_TYPE { get; set; }
        public string? SEAPORT_CODE { get; set; }
        public int? NOTIFY_PERSON { get; set; }
        public int? NOTIFY_TO { get; set; }
        public string? COMMISSION_AGENT { get; set; }
        public string? LC_NUMBER { get; set; }
        public DateTime? LC_DATE { get; set; }
        public decimal? FRT_AMT { get; set; }
        public decimal? INSU_AMT { get; set; }
        public decimal? COMM_AMT { get; set; }
        public int? BANK_CODE { get; set; }
        public string? BANK_BRANCH { get; set; }
        public string? BANK_ADD { get; set; }
        public string? ACT_NUMBER { get; set; }
        public string? IFSC_CODE { get; set; }
        public string? AD_CODE { get; set; }
        public string? SWIFT_CODE { get; set; }
        public string? NATURE_PACKING { get; set; }
        public string? TECH_SPEC { get; set; }
        public string? SPL_INST { get; set; }
        public string? FILE_NAME { get; set; }
        public string? COMM_NARR { get; set; }
        public string? FPOD { get; set; }

        public byte[]? IMG_FILE { get; set; }
        public string? FILE_NAME1 { get; set; }
        public string? FILE_TYPE { get; set; }
        public string? FILE_DESC { get; set; }
        public string? FILE_Path { get; set; }
        public int? SRNO { get; set; }

        public IFormFile? Attachment { get; set; }

    }
}
