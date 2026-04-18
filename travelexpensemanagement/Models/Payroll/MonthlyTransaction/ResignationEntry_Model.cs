using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace travelexpensemanagement.Models.Payroll.Monthly_Transaction
{
    public class ResignationEntry_Model
    {      
        public string? V_TYPE { get; set; }

        public int? V_NO { get; set; } 

        public string? DOC_ID { get; set; } 

        public DateTime? V_DATE { get; set; } 

        public int? EMP_CODE { get; set; }

        public String? Emp_name { get; set; }

        public int? status { get; set; }

        public DateTime? RESIGN_DATE { get; set; } 

        public DateTime? LAST_WORK_DATE { get; set; }
   
        public DateTime? RELIEVING_DATE { get; set; }

        public string? RESIGN_REASON { get; set; } 

        public string? REMARKS { get; set; }

        public string? ATTACH1 { get; set; }

        public string? ATTACH2 { get; set; }

        public string? ATTACH3 { get; set; }
        public string? action { get; set; }





    }
}
