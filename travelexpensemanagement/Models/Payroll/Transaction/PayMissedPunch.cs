namespace travelexpensemanagement.Models.Payroll.Transaction
{
    public class PayMissedPunch
    {
        public List<PayMissedPunchDetail> PayMissedPunchDetails { get; set; }
        public string V_TYPE { get; set; }
        public int V_NO { get; set; }
        public string DOC_ID { get; set; } 
        public string SaveOrUpdate { get; set; }
        
    }

    public class PayMissedPunchDetail
    {
        public int? SNO { get; set; }
        public string? EMP_CODE { get; set; }
        public string? EMP_NAME { get; set; }
        public string? DEPT_CODE { get; set; }
        public string? DEPT_NAME { get; set; }
        public string? OUT_TYPE { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? SHIFT { get; set; }
        public TimeSpan? IN_TIME { get; set; }
        public TimeSpan? OUT_TIME { get; set; }
        public string? REMARKS { get; set; }
        public string? FAPROV_STATUS { get; set; }
        public string? FAPROV_REMARKS { get; set; }
    }
}
