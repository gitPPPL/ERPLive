namespace travelexpensemanagement.Models.PayRoll
{
    public class SalaryLevelModel
    {
        public int? CODE { get; set; }

        public string? NAME { get; set; } = string.Empty;

        public decimal? BASIC { get; set; }

        public decimal? HRA { get; set; }

        public decimal? CONV { get; set; }

        public decimal? OTHERS { get; set; }

        public decimal? TOT_AMT { get; set; }

        public decimal? GW_AMT { get; set; }

        public int? Active { get; set; } 

        public String? Action { get; set; } 

    }
}
