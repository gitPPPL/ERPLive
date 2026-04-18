namespace travelexpensemanagement.Models.FincialAccounting.Master
{
    public class GODOWN_MAST
    {
        public int COMP_CODE { get; set; }
        public int CODE { get; set; }
        public string NAME { get; set; }
        public string COMP_NAME { get; set; }
        public string ADDRESS { get; set; }
        public string ADDRESS2 { get; set; }
        public string CITY { get; set; }
        public string PINCODE { get; set; }
        public string STATE_CODE { get; set; }
        public int ACTIVE { get; set; }
        public int SNO { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public string WB_YN { get; set; }
        public string ACTION { get; set; }
    }
    public class GODOWNExport
    {
        public string CODE { get; set; }
        public string NAME { get; set; }
        public string PINCODE { get; set; }
        public string COMP_NAME { get; set; }
        public string ADDRESS { get; set; }
        public string ADDRESS2 { get; set; }
        public string CITY { get; set; }
        public string STATE_CODE { get; set; }
        public string ACTIVE { get; set; }
        public string WB_YN { get; set; }
    }

}
