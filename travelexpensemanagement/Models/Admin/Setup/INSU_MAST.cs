using System.ComponentModel.DataAnnotations;

namespace travelexpensemanagement.Models.Admin.Setup
{
    public class INSU_MAST
    {
        [Key]
        public int COMP_CODE { get; set; }
        public int CODE { get; set; }
        public string NAME { get; set; }
        public string DESCRIPTION { get; set; }
        public string COMP_NAME { get; set; }
        public string COMP_ADD { get; set; }
        public decimal? POLICY_AMT { get; set; }
        public DateTime? ENTRY_DATE { get; set; }
        public DateTime? EFF_DATE { get; set; }
        public DateTime? EXP_DATE { get; set; }
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
    public class INSU_MASTExportDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CompanyName { get; set; }
        public bool Active { get; set; }
    }
    public class INSU_MASTDetailDto
    {
        public string DOC_CODE { get; set; }
        public string UUser { get; set; }
        public DateTime? UDATE { get; set; }
        public string EUSER { get; set; }
        public DateTime? EDATE { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
    }

}
