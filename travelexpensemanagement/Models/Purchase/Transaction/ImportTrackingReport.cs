namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class ImportTrackingReport
    {
        public int? COMP_CODE { get; set; }
        public int? BRANCH_CODE { get; set; }
        public int? YEAR_CODE { get; set; }

        public string V_TYPE { get; set; }
        public int? V_NO { get; set; }

        public string SAUDA_TYPE { get; set; }
        public int? SAUDA_NO { get; set; }

        public DateTime? V_DATE { get; set; }

        public int? PARTY_CODE { get; set; }
        public string PARTY_NAME { get; set; }

        public int? CITY_CODE { get; set; }
        public string CITY_NAME { get; set; }

        public string BILL_NO { get; set; }
        public DateTime? BILL_DATE { get; set; }

        public decimal? BILL_AMT { get; set; }

        public string TRUCK_NO { get; set; }

        public int? SEND_BY { get; set; }
        public int? SEND_TO { get; set; }

        public DateTime? SEND_DATE { get; set; }
        public DateTime? RECD_DATE { get; set; }

        public int? SEND_FLAG { get; set; }
        public int? RECD_FLAG { get; set; }

        public string REMARKS { get; set; }
    }
}