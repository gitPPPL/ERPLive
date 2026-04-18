namespace travelexpensemanagement.Models.QualityControl.Master
{
    public class QCDISC_MAST
    {
        public int COMP_CODE { get; set; }
        public string V_TYPE { get; set; }
        public int ITEM_CODE { get; set; }
        public string ITEM_NAME { get; set; }
        public int QCP_CODE { get; set; }
        public decimal QCP_DIFF { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public int SRNO { get; set; }
        public int ACTIVE { get; set; }
        public string ACTION { get; set; }
    }

    public class ImportQcDiscDto
    {
        public int ItemCode { get; set; }
        //public string SubmitAction { get; set; }
        public List<QCDISC_MAST> QcDiscList { get; set; }
    }
}
