namespace travelexpensemanagement.Models.Admin.Setup
{
    public class EmailSettingModel
    {
        public string Date { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public string SmtpServer { get; set; }
        public string SmtpPort { get; set; }
        public string SmtpUssl { get; set; }
        public string Document { get; set; }
        public string DocumentCode { get; set; }
        public string Insnerttype { get; set; }
        public string code { get; set; }
        public string compCode { get; set; }

    }

    public class EmailSettingModel1
    {
        public List<EmailSettingModel> Items { get; set; }
    }
    public class EmailSettingModelList
    {
        public string UserID { get; set; }
        public string VType { get; set; }
        public string WebPassword { get; set; }
        public string SmtpServer { get; set; }
        public string SmtpPort { get; set; }
        public string SmtpUssl { get; set; }
    }

 



}
