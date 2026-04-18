namespace travelexpensemanagement.Models
{
    public class TravelRequestModel
    {
        public int SRNO { get; set; }
        public string EmployeeName { get; set; }
        public string TravelFrom { get; set; }
        public string TravelTo { get; set; }
        //public string TravelDate { get; set; }
        public DateTime? TravelDate { get; set; }
        public decimal TotalCost { get; set; }
        public string Purpose { get; set; }
        public string TravelType { get; set; }
        public string Journeytype { get; set; }
    }

    public class JourneyDetailsModel
    {
        public int Code { get; set; }
        public string TravelFrom { get; set; }
        public string TravelTo { get; set; }
        public string TravelDate { get; set; }
        public string TotalCost { get; set; }
        public string Purpose { get; set; }
        public string TravelType { get; set; }
        public string Journeytype { get; set; }
    }

    public class ApprovalViewModel
    {
        public int RequestId { get; set; }
        public string User { get; set; }
        public string Document { get; set; }
        public string Remark { get; set; }
        public string SendUserName { get; set; }
    }





}
