namespace travelexpensemanagement.Models.FincialAccounting.Master
{
    public class PORT_MAST
    {
        public int CODE { get; set; }
        public string PORTCODE { get; set; }
        public string NAME { get; set; }
        public string STATE { get; set; }
        public string PORT_TYPE { get; set; }
        public int ACTIVE { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public string ACTION { get; set; }
    }
    public class PortExport
    {
        public string CODE { get; set; }
        public string NAME { get; set; }
        public string PORTCODE { get; set; }
        public string ACTIVE { get; set; }
    }

}
