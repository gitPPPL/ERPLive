using System.ComponentModel.DataAnnotations;

namespace travelexpensemanagement.Models.Admin.Setup
{
    public class ITEM_MGROUP
    {
        [Key]
        public int COMP_CODE { get; set; }
        public int CODE { get; set; }
        public string NAME { get; set; }
        public string SHORTNAME { get; set; }
        public string MAIN_TYPE { get; set; }
        public string MGROUP_TYPE { get; set; }
        public string REPORT_TYPE { get; set; }
        public string PLANNING_METHOD { get; set; }
        public string PROCUREMENT_METHOD { get; set; }
        public string VALUATION_METHOD { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public int ACTIVE { get; set; }
        public int SRNO { get; set; }
        public string ACTION { get; set; }

    }

    public class ITEM_MGROUPListExport
    {
        public int? CODE { get; set; }
        public string NAME { get; set; }
        public string? SHORTNAME { get; set; }
        public int? MGROUP_CODE { get; set; }
        public string? MGROUP_NAME { get; set; }
        public string? GROUP_TYPE { get; set; }
        public int? ACT_CODE { get; set; }         // Renamed for clarity
        public string? ACT_NAME { get; set; }      // To hold the descriptive name
        public string? Sauda_Required { get; set; }
        public string? PRINT_NAME { get; set; }
        public string? SALE_GROUP { get; set; }
        public string? ACTIVE { get; set; }
        public string? ACTION { get; set; }
        public string? MAIN_TYPE { get; set; }
        public string? PLANNING_METHOD { get; set; }
        // etc.

    }
    public class ItemMGroupDetailDto
    {
        public string Code { get; set; }
        public string UUser { get; set; }
        public DateTime? UDATE { get; set; }
        public string EUSER { get; set; }
        public DateTime? EDATE { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
    }
}
