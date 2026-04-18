namespace travelexpensemanagement.Models
{
    public class RequestforTravel
    {
        public int? FromOneWay { get; set; }
        public int? ToOneWay { get; set; }
        public DateTime? TravelDate { get; set; }
        public int? ExpenseCategoryMaster { get; set; }
        public decimal? Cost { get; set; }
        public int? TransportationModeMaster { get; set; }
        public string Purpose { get; set; }
    }

    public class TravelExpenseWrapper
    {
        public int? EmpID { get; set; }
        public string Employee { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public DateTime? TravelDate { get; set; }
        public decimal? Cost { get; set; }
        public string Purpose { get; set; }
        public string TravelType { get; set; }
        public List<RequestforTravel> TravelDetails { get; set; }
        public string RequestType { get; set; } 
    }
}
