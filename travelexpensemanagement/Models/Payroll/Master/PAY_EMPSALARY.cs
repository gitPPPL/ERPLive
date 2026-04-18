namespace travelexpensemanagement.Models.Payroll.Master
{
    public class PAY_EMPSALARY
    {
        public int COMP_CODE { get; set; }
        public DateTime EFF_DATE { get; set; }
        
        public int EMP_CODE { get; set; }
        public string M_TYPE { get; set; }
        public string CODE { get; set; }
        public string EMP_NAME { get; set; }
        public decimal BASIC { get; set; }
        public decimal HRA { get; set; }
        public decimal SPL_ALLOW { get; set; }
        public decimal SPL_ALLOW2 { get; set; }
        public decimal OTHERS { get; set; }
        public decimal CONVEYANCE { get; set; }
        public decimal UNIFORM { get; set; }
        public decimal SECURITY { get; set; }
        public decimal COMMUITY { get; set; }
        public decimal INSURANCE { get; set; }
        public decimal MOBILE_ALLOW { get; set; }
        public decimal TOT_SALARY { get; set; }
        public decimal GW_SALARY { get; set; }
        public int DUTY { get; set; }
        public int GRADE_CODE { get; set; }
        public int DESG_CODE { get; set; }
        public string DESG_NAME { get; set; }
        public string DEPT_NAME { get; set; }
        public string IN_TIME { get; set; }
        public string OUT_TIME { get; set; }
        public string FAPROV_STATUS { get; set; }
        public string FAPROV_REMARKS { get; set; }
        public decimal OLD_GW_SALARY { get; set; }
        public int ACTIVE { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public string UPDATE_FLG { get; set; }
        public decimal VPF { get; set; }
        public string ACTION{ get; set; }
        public string SEARCH_CODE { get; set; }
    }
}
