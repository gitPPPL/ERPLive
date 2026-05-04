namespace travelexpensemanagement.Models
{
    public class UserSessionData
    {
        public string PubCompCode { get; set; }
        public string PubUserId { get; set; }
        public string PubUserName { get; set; }
        public string PubUserLevel { get; set; }
        public string PubWorkStationID { get; set; }
        public string PubLocalId { get; set; }
        public string PubFYearCode { get; set; }
        public int PubBranchCode { get; set; }
        public DateTime PubLoginDate { get; set; }
        public DateTime PubSessiontime { get; set; }
        public string? ip_address { get; set; }
        public string? client_id { get; set; }
        public string? client_secret { get; set; }
        public string? gstin { get; set; }
        public string? auth_access_type { get; set; }


    }

}
