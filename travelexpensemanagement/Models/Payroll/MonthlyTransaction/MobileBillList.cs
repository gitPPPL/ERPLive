namespace travelexpensemanagement.Models.Payroll.MobileBillEntry
{
    public class MobileBillList
    {

        public string DocNo { get; set; }
        public string DocId { get; set; }

        public string DocDate { get; set; }
        public decimal BillAmount { get; set; }
        public decimal DeductAmount { get; set; }
        public string Dr_bill_name { get; set; }
        public string Cr_bill_name { get; set; }

    }
}
