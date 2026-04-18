namespace travelexpensemanagement.Models.TODO
{
    public class TaskDetail_Model
    {   
        public int? V_NO { get; set; }
        public DateOnly? V_DATE { get; set; }        
        public int? USER_CODE { get; set; }
        public int? DEPT_CODE { get; set; }
        public string? DEPT_NAME { get; set; }
        public int? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public int? UOM_CODE { get; set; }
        public string? UOM_NAME { get; set; }
        public string? MAKE_NAME { get; set; }
        public int? ORDER_NO { get; set; }
        public string? REMARKS { get; set; }
        public string? REMARKS2 { get; set; }
        public string? REMARKS3 { get; set; }
        public string? REMARKS4 { get; set; }
        public DateOnly? QC_STATUSDATE { get; set; }
        public DateOnly? DEPT_STATUSDATE { get; set; }
        public int? FROM_DEPT { get; set; }
        public string? FROM_DEPTNAME { get; set; }
        public string? RFILE_PATH { get; set; }   
        public string? STATUS { get; set; }
        public DateTime? START_DATETIME { get; set; }
        public DateTime? END_DATETIME { get; set; }
        public string? PRIORITY { get; set; }
        public string? CCFILEPATH { get; set; }
        public string? BCCFILEPATH { get; set; }

    }
}
