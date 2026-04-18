using System.Linq.Expressions;

namespace travelexpensemanagement.Models.Payroll.MobileBillEntry
{
    public class MobileEntryModel
    {
        

        public details Details { get; set; } = new details();


        public class details
        {

            public Header header { get; set; } = new Header();
            public List<tableRow> TableRow { get; set; } = new List<tableRow>();

        }

        public class Header
        {
            public string VNo { get; set; }
            public string VType { get; set; }
            public string Vdate { get; set; }
            public decimal BillAmt { get; set; }
            public decimal CgstAmt { get; set; }
            public decimal SgstAmt { get; set; }
            public decimal IgstAmt { get; set; }
            public decimal DeductAmt { get; set; }
            public int BillDrAccount { get; set; }
            public int BillCrAccount { get; set; }
            public int CGSTAccount { get; set; }
            public int SGSTAccount { get; set; }
            public int IGSTAccount { get; set; }
            //public int IGSTAccount { get; set; }

            public string Action { get; set; }
        }


        public class tableRow
        {
            public int SrNo { get; set; }
            public int EmpCode { get; set; }
            public string EmpName { get; set; }
            public string MobNo { get; set; }
            public decimal Limit { get; set; }
            public decimal BillAmt { get; set; }
            public decimal DeductAmt { get; set; }
            public string Name { get; set; }
            public string DrAcName { get; set; }
            public string CrAcName { get; set; }
            public string Remarks { get; set; }
        }

    }

  

}
