namespace travelexpensemanagement.Models.Admin.Setup
{
    public class TaxMastViewModel
    {
        public int SRNO { get; set; }
        public string NAME { get; set; }
        public string TAX_DESCRIPTION { get; set; }
        public string TAX_TYPE { get; set; }
        public decimal? CGST_PER { get; set; }
        public decimal? SGST_PER { get; set; }
        public decimal? IGST_PER { get; set; }
        public decimal? VAT_PER { get; set; }
        public decimal? TDS_PER { get; set; }
        public decimal? TCS_PER { get; set; }
        public decimal? OTH_PER { get; set; }
        public decimal? OTH_PER2 { get; set; }
        public int? PACK_ONBASIC { get; set; }
        public int? ACTIVE { get; set; }
    }
    public class TaxCodeRequest
    {
        public string NAME { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class TaxCodeListExportDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string TaxDescription { get; set; }
        public string TaxType { get; set; }
        public string CGST { get; set; }
        public string SGST { get; set; }
        public string IGST { get; set; }
        public string TDS { get; set; }
        public string TCS { get; set; }
        public string OTH { get; set; }
    }




}
