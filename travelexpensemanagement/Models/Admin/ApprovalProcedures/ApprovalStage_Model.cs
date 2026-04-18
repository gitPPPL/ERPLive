namespace travelexpensemanagement.Models.Admin.ApprovalProcedures
{
    public class ApprovalStage_Model
    {    
        public string USER_CODE { get; set; }
        public string APPROV_USER { get; set; } 
        public string FLAG_A { get; set; }
        public string FLAG_B { get; set; }
        public string FLAG_C { get; set; }
        public string FLAG_D { get; set; }
        public string FLAG_E { get; set; }
        public string DOC_CODE { get; set; }
        public string Fullname  { get; set; }
        public string Action  { get; set; }

    }
    public class Form12SaveRequest
    {
        public DateTime VDate { get; set; }
        public List<ApprovalStage_Model> Data { get; set; }
    }
}
