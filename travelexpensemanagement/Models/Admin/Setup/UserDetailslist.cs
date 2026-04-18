namespace travelexpensemanagement.Models.Admin.Setup
{
    public class UserDetailslist
    {
            public int CODE { get; set; }
            public string USER_NAME { get; set; }
            public string FULL_NAME { get; set; }
            public string DESIGNATION { get; set; }
            public string DEPARTMENT { get; set; }
            public int DEPTT_CODE { get; set; }
            public int EMP_CODE { get; set; }
            public int USER_LEVEL { get; set; }
            public string PCName1 { get; set; }
            public string PCName2 { get; set; }
            public string APP_DEVICE_ID_1 { get; set; }
            public string APP_DEVICE_ID_2 { get; set; }
            public string ACTIVE { get; set; }
            public int ALLOW_DAYS { get; set; }
            public string PASSWORD_NEVER_EXPIRED { get; set; }
            public string PASSWORD_CHANGE_ON_NEXT_LOGIN { get; set; }

    }
    public class UserExportDto
    {
        public string Code { get; set; }
        public string FullName { get; set; }
        public string Designation { get; set; }
        public string Department { get; set; }
        public string EmpCode { get; set; }
        public string PcName1 { get; set; }
        public string PcName2 { get; set; }
        public string PcName3 { get; set; }
        public string Status { get; set; } // "Active" / "Inactive"
    }
}
