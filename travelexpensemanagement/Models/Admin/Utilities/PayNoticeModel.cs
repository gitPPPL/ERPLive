namespace travelexpensemanagement.Models.Payroll.MonthlyTransaction
{
    public class PayNoticeModel1
    {
        public int? V_NO { get; set; }
        public DateTime? DocDate { get; set; }
        public int? EMP_CODE { get; set; }
        public string? EmployeeName { get; set; }
        public int? Dep_ID { get; set; }
        public int? Des_ID { get; set; }
        public DateTime? ResignationDate { get; set; }
        public DateTime? NoticePeriodStartDate { get; set; }
        public DateTime? NoticePeriodEndDate { get; set; }
        public int? TotalNoticePeriod { get; set; }
        public int? DaysServed { get; set; }
        public int? DaysNotServed { get; set; }
        public decimal? NoticePayAmount { get; set; }
        public string PaymentType { get; set; }
        public string Type { get; set; }
        public decimal? GrossSalaryPerDay { get; set; }
        public decimal? TotalPayableAmount { get; set; }
        public int? Paid { get; set; } 
        public string PreparedBy { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string Remarks { get; set; }
        public string Action { get; set; }
    }
}
