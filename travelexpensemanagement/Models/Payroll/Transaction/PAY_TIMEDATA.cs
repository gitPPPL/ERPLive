namespace travelexpensemanagement.Models.Payroll.Transaction
{
    public class PAY_TIMEDATA
    {
        public int YEAR_CODE { get; set; }
        public int COMP_CODE { get; set; }
        public int BRANCH_CODE { get; set; }
        public string V_TYPE { get; set; }
        public int V_NO { get; set; }
        public DateTime V_DATE { get; set; }
        public string DOC_ID { get; set; }
        public int EMP_CODE { get; set; }
        public string EMP_NAME { get; set; }
        public string DEPT { get; set; }
        public int MAC_CODE { get; set; }
        public string IN_TIME { get; set; }
        public string OUT_TIME { get; set; }
        public string DATA { get; set; }
        public string REMARKS { get; set; }
        public int LATE_MNT { get; set; }
        public int LATE_HRS { get; set; }
        public int LATE_TOT { get; set; }
        public decimal DEDU_HRS { get; set; }
        public int NOT_IN_PUNCH { get; set; }
        public int NOT_OUT_PUNCH { get; set; }
        public string STATUS { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }

    }
}
