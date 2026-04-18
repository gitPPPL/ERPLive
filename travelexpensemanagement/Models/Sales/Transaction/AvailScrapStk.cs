namespace travelexpensemanagement.Models.Sales.Transaction
{
    public class AvailScrapStk
    {        
        public string? VType { get; set; }
        public int? VNo { get; set; }
        public DateTime? VDate { get; set; }
        public string? DocId { get; set; }
        public string? RequestBy { get; set; }
        public string? Remarks { get; set; }
        public int? Status { get; set; }
        public string? FaProvStatus { get; set; }
        public string? FaProvRemarks { get; set; }       
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string ? SaveOrUpdate { get; set; }
        public List<AvailScrapStk2> availScrapStk2list { get; set; }
    }

    public class AvailScrapStk2
    {        
        public int? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public int? UnitCode { get; set; }
        public int? Nos { get; set; }
        public decimal? Qty { get; set; }
        public string? Remarks { get; set; }
        public string? GivenTo { get; set; }
        public string? GivenFor { get; set; }
        public int? StatusQSisterConcern { get; set; }
        public decimal? StatusQQty { get; set; }
        public int? AfterSisterConcern { get; set; }
        public decimal? AfterQty { get; set; }
        public decimal? StatusQReuseQty { get; set; }
        public decimal? AfterReuseQty { get; set; }
        public decimal? SoldQty { get; set; }
        public decimal? HoldQty { get; set; }
        public decimal? BalQty { get; set; }
        public int? Sno { get; set; }      
        public string? ItemType { get; set; }
        public int? DeptCode { get; set; }
    }

}
