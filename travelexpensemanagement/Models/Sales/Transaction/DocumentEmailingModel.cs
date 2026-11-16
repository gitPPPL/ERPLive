namespace travelexpensemanagement.Models.Sales.Transaction
{
    public class DocumentEmailingModel
    {
    }
    public class MailRowDto
    {
        public string VType { get; set; }
        public string VNo { get; set; }
        public string VDate { get; set; }
        public string Name { get; set; }
        public string InvNo { get; set; }
        public double Amt { get; set; }
        public double DrAmt { get; set; }
        public string Email { get; set; }
        public string PCode { get; set; }
    }
    public class SendMailRequest
    {
        public string MailType { get; set; }
        public string Message { get; set; }
        public string ToDate { get; set; }   // yyyy-MM-dd
        public List<MailRowDto> Rows { get; set; } = new();
    }
}
