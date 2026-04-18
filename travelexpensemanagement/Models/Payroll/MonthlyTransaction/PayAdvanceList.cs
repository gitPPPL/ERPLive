namespace travelexpensemanagement.Models.Payroll.MonthlyTransaction
{
    public class PayAdvanceList
    {
        public List<PayAdvanceListDT> payAdvanceLists { get; set; }
        public string VType { get; set; }
        public int? VNo { get; set; }
        public DateTime? VDate { get; set; }   
        public string SaveOrUpdate { get; set; }
    }

    public class PayAdvanceListDT
    {
        public int? EmpCode { get; set; }
        public int? SNo { get; set; }
        public decimal? WDay { get; set; }
        public decimal? Gross { get; set; }
        public decimal? Wages { get; set; }
        public decimal? AdvNamT { get; set; }
        public decimal? SancAmt { get; set; }
        public string? Remark { get; set; }
        public decimal? BankCh { get; set; }
        public decimal? Rate { get; set; }
        public int? PerFlg { get; set; }
    }

}

 