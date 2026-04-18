namespace travelexpensemanagement.Models.Payroll.Transaction
{
    public class PayEmpStatus
    {      
        public string V_TYPE { get; set; }
        public int V_NO { get; set; }
        public string DOC_ID { get; set; }
        public DateTime V_DATE { get; set; }
        public string? SHIFT { get; set; }

        public string SaveOrUpdate { get; set; }
        public List<PayEmpStatusDetail> payEmpStatusDetails { get; set; }

    }

    public class PayEmpStatusDetail
    {       
        public int? EMP_CODE { get; set; }
        public string? EMP_NAME { get; set; }
        public int? DEPT_CODE { get; set; }
        public int? DESG_CODE { get; set; }
        public int? TDEPT_CODE { get; set; }
        public DateTime? IN_DATE { get; set; }
        public string? IN_TIME { get; set; }
        public string? STATUS { get; set; }
        public string? REMARKS { get; set; }
    }


}
