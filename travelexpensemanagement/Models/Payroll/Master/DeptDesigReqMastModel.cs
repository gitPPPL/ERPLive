namespace travelexpensemanagement.Models.DeptDesigReqMastModel
{
    public class DeptDesigReqMastModel
    {

        public int? CODE { get; set; }
        public int DEPT_CODE { get; set; }
        public int PLACE_CODE { get; set; }
        public int DESG_CODE { get; set; }
        public int SHIFT_A { get; set; }
        public int SHIFT_B { get; set; }
        public int SHIFT_C { get; set; }
        public int SHIFT_G { get; set; }
    
        public int ACTIVE { get; set; }
     
        public String action { get; set; }

        public string DeptName { get; set; }

        public string Desgn { get; set; }

        public string Place { get; set; }   
    }
}
