namespace travelexpensemanagement.Models.Payroll.MonthlyTransaction
{
    public class TDSDeduction
    {            
        public int? EmpCode { get; set; }
        public string? EmpName { get; set; }
        public DateTime? EffDate { get; set; }
        public int? Child { get; set; }
        public decimal? IncomeInt { get; set; }
        public decimal? Rent { get; set; }
        public string? RentPan { get; set; }
        public decimal? Us24 { get; set; }
        public decimal? Us80C_EPF { get; set; }
        public decimal? Us80C_PPF { get; set; }
        public decimal? Us80C_SCSS { get; set; }
        public decimal? Us80C_NSC { get; set; }
        public decimal? Us80C_FD { get; set; }
        public decimal? Us80C_TSB { get; set; }
        public decimal? Us80C_LIP { get; set; }
        public decimal? Us80C_NPS { get; set; }
        public decimal? Us80C_HL { get; set; }
        public decimal? Us80C_SSA { get; set; }
        public decimal? Us80C_SD { get; set; }
        public decimal? Us80C_TU { get; set; }
        public decimal? Us80C_Tot { get; set; }
        public decimal? Us80CCD { get; set; }
        public decimal? Us80CCG { get; set; }
        public decimal? Us80D { get; set; }
        public decimal? Us80E { get; set; }
        public decimal? Us80G { get; set; }
        public decimal? Us10 { get; set; }
        public decimal? HraEx { get; set; }
        public decimal? ChildEdu { get; set; }
        public decimal? ChildHost { get; set; }
        public decimal? SalaryIncome { get; set; }
        public decimal? TbSalary { get; set; }
        public decimal? TaxAmt { get; set; }
        public decimal? CessAmt { get; set; }
        public decimal? TaxPay { get; set; }
        public decimal? TdsAmt { get; set; }
        public decimal? ProfTaxAmt { get; set; }
        public decimal? BalAmt { get; set; }
        public decimal? TdsMnth { get; set; }
        public decimal? TotSalary { get; set; }
        public decimal? PrevIncome { get; set; }
        public decimal? PrevTds { get; set; }
        public string? Mobile { get; set; }
        public string? Pan { get; set; }
        public decimal? Us80D_P { get; set; }
        public decimal? HomeLoan { get; set; }
        public decimal? MedicalS { get; set; }
        public decimal? MedicalP { get; set; }
        public decimal? Donation { get; set; }     
        public int? Donation100 { get; set; }
        public int? Us80G100 { get; set; }
        public string? TaxRegime { get; set; }
        public decimal? MedicalSp { get; set; }
        public decimal? Us80DSp { get; set; }
        public string? SaveOrUpdate { get; set; }
        public List<TdsDetailTable> tdsDetailTables { get; set; }

    }

    public class TdsDetailTable
    {
        public DateTime? SDATE { get; set; }
        public decimal? BASIC { get; set; }
        public decimal? HRA { get; set; }
        public decimal? CONV { get; set; }
        public decimal? ALLOW { get; set; }
        public decimal? OTHER { get; set; }
        public decimal? TOTAL { get; set; }
        public decimal? HRA_RECD { get; set; }
        public decimal? HRA_PAID { get; set; }
        public decimal? HRA_40 { get; set; }
        public decimal? HRA_EX { get; set; }

    }

}
