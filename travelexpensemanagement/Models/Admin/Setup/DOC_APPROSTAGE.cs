namespace travelexpensemanagement.Models.Admin.Setup
{
    public class DOC_APPROSTAGE
    {
        public int COMP_CODE { get; set; }
        public string DOC_CODE { get; set; }
        public int USER_CODE { get; set; }
        public string APPROV_USER { get; set; }
        public string FLAG_A { get; set; }
        public string FLAG_B { get; set; }
        public string FLAG_C { get; set; }
        public string FLAG_D { get; set; }
        public string FLAG_E { get; set; }
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

        //Additional fields
        public string ACTION { get; set; }
        public string DOC_NAME { get; set; }
        public int USER_NO { get; set; }
        public string DESIGNATION { get; set; }
        public string DEPARTMENT { get; set; }
    }

    public class SaveApprovalStageRequest
    {
        public string DocCode { get; set; }
        public List<DOC_APPROSTAGE> DocStageList { get; set; }
    }
    public class DocDetailDto
    {
        public string DOC_CODE { get; set; }
        public string UUser { get; set; }
        public DateTime? UDATE { get; set; }
        public string EUSER { get; set; }
        public DateTime? EDATE { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
    }

}
