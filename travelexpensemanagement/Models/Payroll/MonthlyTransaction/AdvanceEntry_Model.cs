using System;
using System.ComponentModel.DataAnnotations;

namespace TravelExpenseManagement.Models.Payroll.Monthly_Transaction
{
    public class AdvanceEntry_Model
    {
   
        public string? V_TYPE { get; set; } 
    
        public int? V_NO { get; set; }  
        public DateTime? V_DATE { get; set; } 
        public string? DOC_ID { get; set; }  
        public int? EMP_CODE { get; set; }  
        public int? PAY_DAY { get; set; } 
        public decimal? GROSS { get; set; } 
        public decimal? RATE { get; set; } 
        public string? TYPE { get; set; }  
        public decimal? AMOUNT { get; set; } 
        public decimal? INSTALLMENT { get; set; } 
        public string? REF_TYPE { get; set; } 
        public int? REF_NO { get; set; }  
        public string? REMARK { get; set; }  
        public string? FINAL { get; set; }  

        public string? Emp_name { get; set; }
        public string? action { get; set; }



    }
}
