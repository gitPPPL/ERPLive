namespace travelexpensemanagement.Models.TaskManagement_Model
{
    public class CreateTask_Model
    {
        public int? V_NO { get; set; }
        public int? REF_NO { get; set; }
        public DateTime? V_DATE { get; set; }     
        public string? DOC_ID { get; set; }
        public string? TASK_SUBJECT { get; set; }
        public int? ASSIGN_COMPANY { get; set; }
        public int? ASSIGN_PERSON_CODE { get; set; }
        public int? SUPERVISOR_CODE { get; set; }
        public string? TASK_DESC { get; set; }
        public DateTime? START_DATETIME { get; set; }
        public DateTime? END_DATETIME { get; set; }
        public string? FILE_PATH { get; set; }
        public string? PRIORITY { get; set; }
        public string? STATUS { get; set; }
        public string? FREQUENCY { get; set; }
        public string? ALERT_FOR { get; set; }
        public string? ALERTCC_FOR { get; set; }
        public string? ALERTBCC_FOR { get; set; }
        public int? SEND_MAIL { get; set; }
        public int? SEND_SMS { get; set; }
        public string? MOBILE1 { get; set; }
        public string? MOBILE2 { get; set; }
        public string? MOBILE3 { get; set; }
        public string? EMAIL1 { get; set; }
        public string? EMAIL2 { get; set; }
        public string? REMARKS { get; set; }     
        public int? CC_COMPANY { get; set; }
        public int? CC_CODE { get; set; } 
        public string? CC_REMARKS { get; set; }
        public int? BCC_COMPANY { get; set; }
        public int? BCC_CODE { get; set; }
        public string? BCC_REMARKS { get; set; }
        public string? action { get; set; }
        public string? Full_name { get; set; }
        public string? AssignedBy { get; set; }
        public string? AssignTo { get; set; }
        public string? FROM_DEPTNAME { get; set; }
        public string? DEPT_NAME { get; set; }
        public string? ASSIGN_PERSON { get; set; }
        public string? CC_PERSON { get; set; }
        public string? BCC_PERSON { get; set; }
    }
}
