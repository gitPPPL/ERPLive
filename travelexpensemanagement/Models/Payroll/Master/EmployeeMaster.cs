namespace travelexpensemanagement.Models.Payroll.Master
{
    public class EmployeeMaster
    {
        public List<EmployeeExperience> Experiences { get; set; }
        public List<EmployeeFamily> Family { get; set; }
        public List<EmployeeQualification> Qualifications { get; set; }
        public List<EmployeeRelative> Relatives { get; set; }
        public List<EmployeeAttachment> Attachments { get; set; }

        // Scalar parameters
        public int? CompCode { get; set; }
        public string EmpId { get; set; }
        public int? Code { get; set; }
        public int? MacCode { get; set; }
        public string MType { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Name { get; set; }
        public string FatherName { get; set; }
        public int? RejoinCode { get; set; }
        public string PAdd1 { get; set; }
        public string PAdd2 { get; set; }
        public string PAdd3 { get; set; }
        public int? PCityCode { get; set; }
        public string TAdd1 { get; set; }
        public string TAdd2 { get; set; }
        public string TAdd3 { get; set; }
        public int? TCityCode { get; set; }
        public string Sex { get; set; }
        public int? Age { get; set; }
        public DateTime? Dob { get; set; }
        public DateTime? Dom { get; set; }
        public int? DesgCode { get; set; }
        public int? DeptCode { get; set; }
        public int? PlaceCode { get; set; }
        public int? IncPlaceCode { get; set; }
        public string Type { get; set; }
        public int? CaderCode { get; set; }
        public int? CatCode { get; set; }
        public int? GradeCode { get; set; }
        public DateTime? JoinDate { get; set; }
        public DateTime? ResignDate { get; set; }
        public DateTime? PermanentDate { get; set; }
        public string RisZone { get; set; }
        public string PfAppl { get; set; }
        public string PfNo { get; set; }
        public DateTime? PfDate { get; set; }
        public string EsiAppl { get; set; }
        public string EsiNo { get; set; }
        public DateTime? EsiDate { get; set; }
        public string OffDay { get; set; }
        public string Shift { get; set; }
        public string RefBy { get; set; }
        public string Spouse { get; set; }
        public string Contact { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Uan { get; set; }
        public string Pan { get; set; }
        public string Aadar { get; set; }
        public string VotorId { get; set; }
        public string Dl { get; set; }
        public string AcNo { get; set; }
        public int? BankCode { get; set; }
        public string BankName { get; set; }
        public string IfscCode { get; set; }
        public string Branch { get; set; }
        public string AcType { get; set; }
        public string BankVerify { get; set; }
        public string Remarks { get; set; }
        public string LeaveAppl { get; set; }
        public string HolidayAppl { get; set; }
        public string BonusAppl { get; set; }
        public string BloodGroup { get; set; }
        public string Religion { get; set; }
        public string CardNo { get; set; }
        public int? CtcProdInc { get; set; }
        public int? CtcOtherAllow { get; set; }
        public int? CtcMiscBenefits { get; set; }
        public int? CtcCarBenefits { get; set; }
        public int? CtcRentReimb { get; set; }
        public int? CtcMobExps { get; set; }
        public int? CtcTeaExps { get; set; }
        public string FaProvStatus { get; set; }
        public string FaProvRemarks { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public decimal? A { get; set; }
        public decimal? B { get; set; }
        public decimal? C { get; set; }
        public decimal? D { get; set; }
        public decimal? E { get; set; }
        public DateTime? D1 { get; set; }
        public DateTime? D2 { get; set; }
        public DateTime? D3 { get; set; }
        public string JobRes { get; set; }
        public int? Experience { get; set; }
 
        public string ImagePath { get; set; }
        public string ImageByteBase64 { get; set; }  // temporary input
        public byte[] ImageByte { get; set; }        // final byte[]
        public string Abry { get; set; }
        public string GratuityAppl { get; set; }
        public DateTime? RetirementDate { get; set; }
        public DateTime? SalaryEffDate { get; set; }
        public string HraDeduct { get; set; }
        public string FixedSalary { get; set; }
        public string EmpType { get; set; }
        public string Fitment { get; set; }
        public string Pmrpy { get; set; }
        public string BankLoan { get; set; }
        public string CardIssue { get; set; }
        public string QuarterNo { get; set; }
        public string LineNo { get; set; }
        public string ColonyAppl { get; set; }
        public string RoomNo { get; set; }
        public string PpfOnFullBasic { get; set; }
        public string MStatus { get; set; }
        public string VpfAppl { get; set; }
        public DateTime? VpfDate { get; set; }
        public DateTime? RetrDate { get; set; }
        public int ? active { get; set; }
        public string? SaveOrUpdate { get; set; }
    }

    public class EmployeeAttachment
    {
        public int? AttachId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileBase64 { get; set; }
        public DateTime? AttachDate { get; set; }
        public byte[] FileRData { get; set; }
    }
    public class EmployeeExperience
    {
        public int? SeqNo { get; set; }
        public string Employer { get; set; }
        public string Address { get; set; }
        public DateTime? Doj { get; set; }
        public DateTime? Dor { get; set; }
        public string Period { get; set; }
        public decimal? LastSalary { get; set; }
        public string Dept { get; set; }
        public string Desg { get; set; }
        public string JobProfile { get; set; }
    }
    public class EmployeeFamily
    {
        public int? SeqNo { get; set; }
        public string Member { get; set; }
        public string Age { get; set; }
        public string Contact { get; set; }
        public string Relation { get; set; }
        public string Minor { get; set; }
        public string Nominee { get; set; }
        public decimal? Share { get; set; }
        public string Address { get; set; }
        public string Remarks { get; set; }
    }
    public class EmployeeQualification
    {
        public int? SeqNo { get; set; }
        public string Degree { get; set; }
        public string Board { get; set; }
        public string Year { get; set; }
        public string Marks { get; set; }
        public string Remarks { get; set; }
    }
    public class EmployeeRelative
    {
        public int? SeqNo { get; set; }
        public string Name { get; set; }
        public int? DeptCode { get; set; }
        public string Contact { get; set; }
        public string Relation { get; set; }
        public string Address { get; set; }
        public string Remarks { get; set; }
       
    }


}
