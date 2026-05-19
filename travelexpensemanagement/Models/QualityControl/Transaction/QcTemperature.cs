namespace travelexpensemanagement.Models.QualityControl.Transaction
{
    public class QcTemperature
    {
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public DateTime? V_TIME { get; set; }
        public int? INCH_CODE { get; set; }
        public int? OPERATORE_CODE { get; set; }
        public int? SUP_CODE { get; set; }
        public int? DEPT_CODE { get; set; }
        public string? SHIFT { get; set; }
        public decimal? DENIER { get; set; }
        public string? REMARK { get; set; }
        public string? SaveOrUpdate { get; set; }

        public List<TapeQuality2>? TapeQualitys { get; set; }
    }

    public class TapeQuality2
    {
        public int? SNO { get; set; }
        public string? TYPE { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? ROOM_CODE { get; set; }
        public decimal? TEMP_READ { get; set; }
        public string? TEMP_REM { get; set; }
        public int? SPEED_CODE { get; set; }
        public decimal? SPEED_READ { get; set; }
        public string? SPEED_READ2 { get; set; }
        public int? WINDER_CODE { get; set; }
        public decimal? WIDTH_MM { get; set; }
        public decimal? DENIER { get; set; }
        public decimal? BREAKING_LOAD { get; set; }
        public decimal? TENACITY { get; set; }
        public decimal? ELONGATION { get; set; }
        public int? MAT_CODE { get; set; }
        public string? GRADE { get; set; }
        public int? NO_OF_BAGS { get; set; }
        public decimal? MAT_PER { get; set; }
        public DateTime? TIME_TAKEN { get; set; }
    }

}