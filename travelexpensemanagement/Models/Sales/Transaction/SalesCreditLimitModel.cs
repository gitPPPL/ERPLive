using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;

namespace travelexpensemanagement.Models.Sales.Transaction
{
    public class SalesCreditLimitModel
    {
        public int? V_NO {get; set;}
        public DateTime? V_DATE {get; set;}
        public int? PARTY_CODE {get; set;}
        public int? GR_CODE {get; set;}
        public string? GR_NAME {get; set;}
        public decimal? CR_LIMIT {get; set;}
        public int? CR_DAYS {get; set;}
        public DateTime? EFF_FROM {get; set;}
        public string? REMARKS {get; set;}
        public int? OURCR_DAYS {get; set;}
        public string? OURAPPROVAL_TYPE {get; set;}
        public string? APPROVAL_TYPE { get; set; }
        
        public string? ACTION { get; set; }

        public bool IsFinalUser { get; set; }
    }

    public class SalesCreditLimitListModel
    {
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? PARTY_NAME { get; set; }
        public decimal? CR_LIMIT { get; set; }
        public int? CR_DAYS { get; set; }
        public string? REMARKS { get; set; }
        public int? OURCR_DAYS { get; set; }
    }
}
