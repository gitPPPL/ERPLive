namespace travelexpensemanagement.Models.Admin.SystemInitilization
{
    public class COMP_MAST
    {
        public int CODE { get; set; }
        public string NAME { get; set; }
        public string ADD1 { get; set; }
        public string ADD2 { get; set; } 
        public string ADD3 { get; set; }
        public int CITY_CODE { get; set; }
        public string PINCODE { get; set; } 
        public int STATE_CODE { get; set; }
        public int COUNTRY_CODE { get; set; }
        public string REGADD1 { get; set; }
        public string REGADD2 { get; set; }
        public string CINNO { get; set; }
        public string PHONE { get; set; }
        public string FAX { get; set; }
        public string EMAIL { get; set; }
        public string WEBSITE { get; set; }
        public string PAN { get; set; }
        public string GSTIN { get; set; }
        public string IEC { get; set; }
        public string EXCISE { get; set; }
        public string SERVICETAX { get; set; }
        public string STORE_PHONE { get; set; }
        public string STORE_EMAIL { get; set; }
        public int CURRENCY_CODE { get; set; }
        public int LANGUAGE_CODE { get; set; }
        public int? BANK_CODE { get; set; }
        public string BANK_ADD1 { get; set; }
        public string BANK_ADD2 { get; set; }
        public string BANK_IFSC { get; set; }
        public string BANK_AC { get; set; }
        public string VALUATION_METHOD { get; set; }
        public string DEPRECIATION_METHOD { get; set; }
        public string BUSINESS_TYPE { get; set; }
        public string COMPANY_TYPE { get; set; }
        public string COMP_LOGO { get; set; }

        public int ACT_CODE { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public int SRNO { get; set; }
        public int ACTIVE { get; set; }
        public byte[] logo { get; set; }
        public string SERVER_IP { get; set; }
        public string DATABASE_NAME { get; set; }
        public string MSMENO { get; set; }
        public string LUTNO { get; set; }
        public string COMP_LOGO_BASE64 { get; set; }
        public string ACTION { get; set; }
    }

    public class CompanyExportModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string PINCODE { get; set; }
        public string PAN { get; set; }
        public string GSTIN { get; set; }
        public string Status { get; set; }
    }
    public class DocDetailDto
    {
        public string Code { get; set; }  
        public string UUser { get; set; }
        public DateTime? UDATE { get; set; }
        public string EUSER { get; set; }
        public DateTime? EDATE { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
    }


}
