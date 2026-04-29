namespace travelexpensemanagement.Models.GateEntry.Transaction
{
    public class CourierTrackingModel
    {
        public string DocType { get; set; }
        public DateTime? DocDate { get; set; }
        public string DocNo { get; set; }
        public string PartyName { get; set; }
        public string City { get; set; }
        public string CourierName { get; set; }
        public string DocketNo { get; set; }
        public string ReceivedBy { get; set; }
        public string Purpose { get; set; }
        public string Weight { get; set; }
        public string Remarks { get; set; }
        public string V_No { get; set; }
        public string? ACTION { get; set; }
    }

    public class GetCourierTrackingModel
    {
        public string DocType { get; set; }
        public string? DocDate { get; set; }
        public string DocNo { get; set; }
        public string PartyName { get; set; }
        public string City { get; set; }
        public string CourierName { get; set; }
        public string DocketNo { get; set; }
        public string ReceivedBy { get; set; }
        public string Purpose { get; set; }
        public string Weight { get; set; }
        public string Remarks { get; set; }
        public string VType { get; set; }
        public string? ACTION { get; set; }
    }

}
