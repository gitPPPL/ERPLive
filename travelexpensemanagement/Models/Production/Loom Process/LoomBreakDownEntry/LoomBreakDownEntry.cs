namespace travelexpensemanagement.Models.Production.Loom_Process.LoomBreakDownEntry
{
    public class LoomBreakDownEntry
    {
        public string? DOC_ID { get; set; }
        public int? V_NO { get; set; }
        public string? V_TYPE { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? SHIFT { get; set; }
        public DateTime? STOP_DATE { get; set; }
        public string? STOP_TIME { get; set; }
        public int? LOOM_CODE { get; set; }
        public int? BD_CODE { get; set; }
        public int? FAULT_CODE { get; set; }
        public DateTime? ST_DATE { get; set; }
        public string? ST_TIME { get; set; }
        public int? HRS { get; set; }
        public int? MINT { get; set; }
        public decimal? CONV_MINT { get; set; }
        public decimal? CONV_HRS { get; set; }
        public string? REMARKS { get; set; }

    }
}
