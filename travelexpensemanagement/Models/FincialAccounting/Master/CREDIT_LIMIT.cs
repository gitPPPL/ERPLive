namespace travelexpensemanagement.Models.FincialAccounting.Master
{
    public class CREDIT_LIMIT
    {
        public int COMP_CODE { get; set; }
        public int BRANCH_CODE { get; set; }
        public int YEAR_CODE { get; set; }
        public string V_TYPE { get; set; }
        public int V_NO { get; set; }
        public string DOC_ID { get; set; }
        public DateTime V_DATE { get; set; }
        public int PARTY_CODE { get; set; }
        public int GR_CODE { get; set; }
        public decimal CR_LIMIT { get; set; }
        public int CR_DAYS { get; set; }
        public DateTime? EFF_FROM { get; set; }
        public string REMARKS { get; set; }
        public string FAPROV_STATUS { get; set; }
        public string FAPROV_REMARKS { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public int OURCR_DAYS { get; set; }
        public string ACTION { get; set; }
    }
}
