namespace travelexpensemanagement.Models.Payroll.Transaction
{
    public class PayGateInOut
    {       
        public string VType { get; set; }
        public int VNo { get; set; }
        public DateTime VDate { get; set; }
        public string DocId { get; set; }
        public string Shift { get; set; }
        public string EmpCode { get; set; }
        public string EmpName { get; set; }
        public string DeptCode { get; set; }
        public string DeptName { get; set; }
        public string Remarks { get; set; }
        public DateTime EDate { get; set; }
        public string ETime { get; set; }
        public string InTime { get; set; }
        public string GpNo { get; set; }
        public decimal DeduHrs { get; set; }
        public string HodCode { get; set; }
        public string HodName { get; set; }
        public string ReasonCode { get; set; }
        public string GpType { get; set; }
        public decimal GpHrs { get; set; }
        public decimal LateHrs { get; set; }
        public decimal SleepHrs { get; set; }
        public string WorkplacePlace { get; set; }
        public string WorkplaceCode { get; set; }
        public bool Approve { get; set; }
        public string? SaveOrUpdate { get; set; }


    }
}
