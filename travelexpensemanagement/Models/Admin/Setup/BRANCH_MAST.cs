using System.ComponentModel.DataAnnotations;

namespace travelexpensemanagement.Models.Admin.Setup
{
    public class BRANCH_MAST
    {
        [Key]
        public int CODE { get; set; }
        public string NAME { get; set; }
        public string LOCATION { get; set; }
        public int ACTIVE { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public int SRNO { get; set; }
        public string ACTION { get; set; }

    }
}
