namespace travelexpensemanagement.Models.Payroll.Monthly_Transaction
{
    public class EarnLeavePaymentEntry_Model
    {
        public string? V_TYPE { get; set; }       
        public int? V_NO { get; set; }            
        public DateTime? V_DATE { get; set; }     
        public string? DOC_ID { get; set; }       
        public string? Emp_name { get; set; }       
        public int? EMP_CODE { get; set; }   
        
        public int? LEAVE_CODE { get; set; }       
        public string? LEAVE_TYPE { get; set; }   
        public int? OP_DAYS { get; set; }          
        public int? CUR_DAYS { get; set; }        
        public int? PAY_DAYS { get; set; }        
        public int? SALARY_DAYS { get; set; }    
        public int? BAL_DAYS { get; set; }         
        public string? MNTH { get; set; }          
        public decimal? GROSS { get; set; }       
        public decimal? RATE { get; set; }        
        public decimal? AMOUNT { get; set; }     
        public string? action { get; set; }   
        
        public int? UUSER { get; set; }
        public DateTime? UDATE { get; set; }
        public int? EUSER { get; set; }
        public DateTime? EDATE { get; set; }




    }
}
