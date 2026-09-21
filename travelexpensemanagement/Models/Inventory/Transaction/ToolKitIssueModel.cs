using DocumentFormat.OpenXml.Spreadsheet;

namespace travelexpensemanagement.Models.Inventory.Transaction
{
    public class ToolKitIssueModel
    {
        public string? V_TYPE {get; set;}
        public string? V_TYPE_NAME {get; set;}
        public int? V_NO {get; set;}
        public DateTime? V_DATE {get; set;}
        public int? PLACE_CODE {get; set;}
        public string? PLACE_NAME {get; set;}
        public int? ITEM_CODE {get; set;}
        public string? ITEM_NAME {get; set;}
        public decimal? QTY {get; set;}
        public decimal? RATE {get; set;}
        public decimal? AMOUNT {get; set;}
        public int? EMP_CODE {get; set;}
        public string? EMP_NAME {get; set;}
        public int? FROM_DEPT {get; set;}
        public string? FROM_DEPT_NAME {get; set;}
        public int? TO_DEPT {get; set;}
        public string? TO_DEPT_NAME {get; set;}
        public string? REMARK {get; set;}
        public decimal? RECD_QTY {get; set;}
        public decimal? DR_AMOUNT { get; set; }
        public string? UUSER { get; set; }
        public DateTime? UDATE { get; set; }
        public string? ACTION { get; set; }

        public decimal BALANCE_QTY { get; set; }
    }
}
