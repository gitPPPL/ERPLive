namespace travelexpensemanagement.Models.Admin.Utilities
{
    public class AcOpeningBalEntry
    {               
       public string? partyCode { get; set; }
        public List<ledger2Model> ledger2 { get; set; }
                       

    }

    public class ledger2Model
    {
        public string? V_TYPE { get; set; }
        public string? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? DOC_ID { get; set; }
        public int? SNO { get; set; }
        public int? DR_CODE { get; set; }
        public int? CR_CODE { get; set; }
        public decimal? AMT { get; set; }
        public string? NARRATION { get; set; }
        public string? CHQ_NO { get; set; }
        public DateTime? CHQ_DATE { get; set; }
        public DateTime? CLG_DATE { get; set; }
        public string? RTGS_TYPE { get; set; }
        public string? RTGS_NO { get; set; }
        public string? BILL_NO { get; set; }
        public DateTime? BILL_DATE { get; set; }
        public string? HOLD_TYPE { get; set; }
        public DateTime? HOLD_DATE { get; set; }
        public int? EMP_CODE { get; set; }
        public int? SRNO { get; set; }
        public decimal? USD_AMT { get; set; }
        public decimal? USD_RATE { get; set; }
        public string? FEXCH_BANKUSD { get; set; }
    }

}
