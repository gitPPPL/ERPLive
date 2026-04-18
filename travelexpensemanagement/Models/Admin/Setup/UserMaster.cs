namespace travelexpensemanagement.Models.Admin.Setup
{
    public class UserMaster
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public int EmpCode { get; set; }
        public string Password { get; set; }
        public int UserLevel { get; set; }
        public int? AllowDays { get; set; }
        public string PCName1 { get; set; }
        public string PCName2 { get; set; }
        public string MACID { get; set; }
        public string AppDeviceID1 { get; set; }
        public string AppDeviceID2 { get; set; }
        public string IsActive { get; set; }
        public string Department { get; set; }
        public int Dashboard { get; set; }
        public string Designation { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public bool PasswordNeverExpire { get; set; }
        public bool PasswordChangeNextLogin { get; set; }
        public bool UserAllowForTask { get; set; }
        public List<string> CompanyIds { get; set; }
        public string UserID { get; set; }
        public string DEPT_CODE { get; set; }
        public string DESG_CODE { get; set; }
    }
}
