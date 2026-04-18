namespace travelexpensemanagement.Models.Admin.Setup
{
    public class UserMenuDetail
    {
        public int Code { get; set; }
        public string Module { get; set; }
        public string DISPLAY_NAME { get; set; }
        public int Access { get; set; }
        public int Add { get; set; }
        public int Edit { get; set; }
        public int Delete { get; set; }
        public int Print { get; set; }
        public int Export { get; set; }
        public int Mail { get; set; }
        public int Approval { get; set; }
        public int DocDetail { get; set; }
        public int MODULE_CODE { get; set; }
        public string Name { get; set; }
        public string Menu_Type { get; set; }
        public string FULL_NAME { get; set; }
        public string UCode { get; set; }
        public string UserID { get; set; }
    }

    public class UserModuleSubmission
    {
        public int UserID { get; set; }
        public string PubFYearCode { get; set; }
        public int CopyOtherUser { get; set; }
        public int copyPermission { get; set; }
        public List<string> DepartmentCheckCodes { get; set; }
        public List<UserMenuDetail> UserMenuDetail { get; set; }
    }




}
