namespace travelexpensemanagement.Models.Payroll.MonthlyTransaction
{
    public class PayEmployeeIncrement
    {        
        public string? Code { get; set; }
        public DateTime? VDate { get; set; }
        public string? VNo { get; set; }
        public string? MType {get; set; }
        public string? Title { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Name { get; set; }
        public DateTime? JoinDate { get; set; }
        public DateTime? ResignDate { get; set; }
        public DateTime? PermanentDate { get; set; }
        public DateTime? EffDate { get; set; }
        public string? DesgCode { get; set; }
        public string? DeptCode { get; set; }
        public string? PlaceCode { get; set; }
        public string? GradeCode { get; set; }
        public bool? PfAppl { get; set; }
        public string? PfNo { get; set; }
        public DateTime? PfDate { get; set; }
        public bool? EsiAppl { get; set; }
        public string? EsiNo { get; set; }
        public DateTime? EsiDate { get; set; }
        public string? AcNo { get; set; }
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? IfscCode { get; set; }
        public string? Branch { get; set; }
        public string? AcType { get; set; }
        public bool? BankVerify { get; set; }
        public decimal? Basic { get; set; }
        public decimal? Hra { get; set; }
        public decimal? SplAllow { get; set; }
        public decimal? SplAllow2 { get; set; }
        public decimal? Others { get; set; }
        public decimal? Conveyance { get; set; }
        public decimal? Uniform { get; set; }
        public decimal? Security { get; set; }
        public decimal? Community { get; set; }
        public decimal? Insurance { get; set; }
        public decimal? MobileAllow { get; set; }
        public decimal? TotSalary { get; set; }
        public decimal? GwSalary { get; set; }
        public decimal? Duty { get; set; }
        public string? BasicData { get; set; }
        public string? SalaryData { get; set; }
        public string? BankData { get; set; }
        public string? FaProvStatus { get; set; }
        public string? FaProvRemarks { get; set; }
        public bool? Active { get; set; }
        public string? SaveOrUpdate { get; set; }

    }
}
