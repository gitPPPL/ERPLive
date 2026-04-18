namespace travelexpensemanagement.Models.FincialAccounting.Master
{
    public class PAYTERM_MAST
    {
        public int COMP_CODE { get; set; }
        public int CODE { get; set; }
        public string NAME { get; set; }
        public string SHORTNAME { get; set; }
        public string DUEBASEON { get; set; }
        public int DAY_PLUS { get; set; }
        public int TOLRENCEDAY { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public int SRNO { get; set; }
        public int Active { get; set; }
        public string CREDIT_TYPE { get; set; }
        public int DAY_INT { get; set; }
        public string ACTION { get; set; }
    }
    public class PaymentTermExport
    {
        public string CODE { get; set; }
        public string NAME { get; set; }
        public string SHORTNAME { get; set; }
        public string DUEBASEON { get; set; }
        public string DAY_PLUS { get; set; }
        public string TOLRENCEDAY { get; set; }
        public string DAY_INT { get; set; }
        public string CREDIT_TYPE { get; set; }
        public string Active { get; set; }
    }

}
