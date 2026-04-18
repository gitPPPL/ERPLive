namespace travelexpensemanagement.Models.FincialAccounting.Master
{
    public class COSTCAT_MAST
    {
        public int COMP_CODE { get; set; }
        public int CODE { get; set; }
        public string NAME { get; set; }
        public string COSTCODE { get; set; }
        public string COSTTYPE { get; set; }
        public int ACTIVE { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public string ACTION { get; set; }
    }

    public class CostCatExpert
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string CostCode { get; set; }
        public string CostType { get; set; }
        public string Status { get; set; }
    }

}
