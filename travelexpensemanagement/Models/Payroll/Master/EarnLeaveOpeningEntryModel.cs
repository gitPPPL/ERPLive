using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using System;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace travelexpensemanagement.Models
{
    public class EarnLeaveOpeningEntryModel
    {

        public string V_TYPE { get; set; }           
        public int V_NO { get; set; }                
        public DateTime? V_DATE { get; set; }         
        public string DOC_ID { get; set; }           
        public int EMP_CODE { get; set; }   
        
        public string EmpName { get; set; }

        public int LEAVE_CODE { get; set; }          
        public string LEAVE_TYPE { get; set; }        
        public int OP_DAYS { get; set; }            
        

        public string action { get; set; } 



    }
}
