namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class MARKET_RATE1
    {
        public int? COMP_CODE { get; set; }
        public int? BRANCH_CODE { get; set; }
        public int? YEAR_CODE { get; set; }
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? DOC_ID { get; set; }
        public DateTime? EFF_DATE { get; set; }
        public DateTime? EXP_DATE { get; set; }
        public string? MGROUP_TYPE { get; set; }
        public string? REMARKS { get; set; }
        public string? FAPROV_STATUS { get; set; }
        public string? FAPROV_REMARKS { get; set; }
        public int? UUSER { get; set; }
        public DateTime? UDATE { get; set; }
        public int? EUSER { get; set; }
        public DateTime? EDATE { get; set; }
        public string? AED { get; set; }
        public string? WSID { get; set; }
        public string? LIP { get; set; }
        public string? LID { get; set; }
    }
    public class ItemMarketRateWrapper
    {
        public MARKET_RATE1 header { get; set; }
        public List<MARKET_RATE2> lineRows { get; set; }
    }
    
}
