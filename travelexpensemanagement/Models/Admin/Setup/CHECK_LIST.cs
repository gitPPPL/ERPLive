namespace travelexpensemanagement.Models.Admin.Setup
{
    public class CHECK_LIST
    {
        public int COMP_CODE { get; set; }
        public int CODE { get; set; }
        public string NATURE { get; set; }
        public string CHECKLIST_NAME { get; set; }
        public string TASK_NAME { get; set; }
        public int? RESPONSIBLE_USER { get; set; }
        public int? APPROVAL_USER { get; set; }
        public DateTime? DUE_DATE { get; set; }
        public int? FREQUENCY_CODE { get; set; }
        public string FREQUENCY { get; set; }
        public int ALERT_DAYS { get; set; }
        public int ALERT_DAYS2 { get; set; }
        public string REMARKS { get; set; }
        public string STATUS { get; set; }
        public string FINAL_STATUS { get; set; }
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
    public class ChecklistExportDTO
    {
        public int CODE { get; set; }
        public string NATURE { get; set; }
        public string CHECKLIST_NAME { get; set; }
        public string TASK_NAME { get; set; }
        public string RESPONSIBLE_USER { get; set; }
        public string APPROVAL_USER { get; set; }
        public DateTime? DUE_DATE { get; set; }
        public string FREQUENCY { get; set; }
        public string REMARKS { get; set; }
        public string STATUS { get; set; }
    }

    public class ChecklistExportDocdetails
    {
        public int CODE { get; set; }
        public string UUser { get; set; }
        public DateTime? UDATE { get; set; }
        public string EUSER { get; set; }
        public DateTime? EDATE { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
    }

}
