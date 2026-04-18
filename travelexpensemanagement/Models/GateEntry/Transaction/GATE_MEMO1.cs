namespace travelexpensemanagement.Models.GateEntry.Transaction
{
    public class GATE_MEMO1
    {
        public int COMP_CODE { get; set; }
        public int BRANCH_CODE { get; set; }
        public int YEAR_CODE { get; set; }
        public string DOC_ID { get; set; }
        public int V_NO { get; set; }
        public string V_TYPE { get; set; } 
        public DateTime V_DATE { get; set; }
        public int EMP_CODE { get; set; }
        public string EMP_NAME { get; set; }
        public int VENDOR_CODE { get; set; }
        public string VENDOR_NAME { get; set; }
        public int TRANSPORT_CODE { get; set; }
        public string TRANSPORT_NAME { get; set; }
        public string THROUGH { get; set; }
        public DateTime RETURN_DATE { get; set; }
        public string REMARKS { get; set; }
        public int STATUS { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public int SRNO { get; set; }
        public int ACTIVE { get; set; }
    }
    public class DeliveryChallanMemoWrapper
    {
        public GATE_MEMO1 Header { get; set; }
        public List<GATE_MEMO2> Items { get; set; }
    }
}
