namespace travelexpensemanagement.Models
{
    public class PendingforApprovalList
    {
        public int? RequestId { get; set; }  
        public int SrNo { get; set; }
        public string UserName { get; set; }
        public string Remarks { get; set; }
        public string Status { get; set; }
        public DateTime? SendDate { get; set; }
    }

}
