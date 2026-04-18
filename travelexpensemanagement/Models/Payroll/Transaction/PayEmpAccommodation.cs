namespace travelexpensemanagement.Models.Payroll.Transaction
{
    public class PayEmpAccommodation
    {       
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public string ? DOC_ID { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? EMP_CODE { get; set; }
        public DateTime? APPL_FROM { get; set; }
        public int? ROOM_NO { get; set; }
        public string? REFERENCE_BY { get; set; }
        public string? REMARKS { get; set; }
        public string? FAPROV_STATUS { get; set; }
        public string? FAPROV_REMARKS { get; set; }
        public string? STATUS { get; set; } 
        public DateTime? ValidUpto { get; set; }
        public string? LIVING_STATUS { get; set; }
        public string ? SaveOrUpdate { get; set; }

    }
}
