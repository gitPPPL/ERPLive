namespace travelexpensemanagement.Models.Admin.Setup
{
    public class BS_MSCH_MAST
    {
        public int COMP_CODE { get; set; }
        public int CODE { get; set; }
        public string NAME { get; set; }
        public int SORT_SRNO { get; set; }
        public string SCH_NO { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public int ACTIVE { get; set; }
        public int SRNO { get; set; }
        public string ACTION { get; set; }

    }

    public class BSMScheduleExport
    {
        public string CODE { get; set; }
        public string NAME { get; set; }
        public int SORT_SRNO { get; set; }
        public string SCH_NO { get; set; }
        public string ACTIVE { get; set; }
    }

}
