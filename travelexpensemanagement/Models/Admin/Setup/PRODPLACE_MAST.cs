using System.ComponentModel.DataAnnotations;

namespace travelexpensemanagement.Models.Admin.Setup
{
    // Ensure there is only one definition of the PRODPLACE_MAST class in this namespace.
    public class PRODPLACE_MAST
    {

        public int COMP_CODE { get; set; }
        public int CODE { get; set; }
        public string NAME { get; set; }
        public string SHORTNAME { get; set; }
        public int PLACE_CODE { get; set; }
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

    public class ProdPlaceExportModel
    {
        public string CODE { get; set; }
        public string NAME { get; set; }
        public string SHORTNAME { get; set; }
        public string PLACE_CODE { get; set; }
    }

}
