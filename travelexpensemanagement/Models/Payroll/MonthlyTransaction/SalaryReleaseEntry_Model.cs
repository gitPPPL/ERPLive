namespace travelexpensemanagement.Models.MonthlyTransaction
{
    public class SalaryReleaseEntry_Model
    {

        public string? V_TYPE { get; set; }

        public int? V_NO { get; set; }

        public DateTime? V_DATE { get; set; }


        public int? EMP_CODE { get; set; }

        public string? DOC_ID { get; set; }

        public int? SNO { get; set; }


        public decimal? AMOUNT { get; set; }

        public string? REMARK { get; set; }

        public DateTime? RELEASE_DATE { get; set; }

        public string? Emp_name { get; set; }

    }
}
