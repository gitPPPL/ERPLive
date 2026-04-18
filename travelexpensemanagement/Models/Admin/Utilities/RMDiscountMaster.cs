namespace travelexpensemanagement.Models.Admin.Utilities
{
    //public class RMDiscountMaster
    //{
    //    public int? SaudaItem { get; set; }
    //    public int? ItemCode { get; set; }
    //    public DateTime EffectiveDate { get; set; }
    //    public DateTime? LastEffectiveDate { get; set; }
    //    public decimal Rate { get; set; }
    //    public decimal? AbovePercentage { get; set; }
    //    public decimal? AboveAmount { get; set; }
    //    public string? Remarks { get; set; }
    //    public string? ACTION { get; set; }
    //}
    public class RMDiscountMaster
    {
        public int? Code { get; set; }  
        public string? DType { get; set; }
        public int? SaudaItem { get; set; }
        public int? ItemCode { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? LastEffectiveDate { get; set; }
        public decimal Rate { get; set; }
        public decimal? AbovePercentage { get; set; }
        public decimal? AboveAmount { get; set; }
        public string? Remarks { get; set; }
        public string? ACTION { get; set; }
    }
    public class RMDiscountModel
    {
        public int Code { get; set; }
        public int Sauda_Item { get; set; }
        public int Item_Code { get; set; }
        public string Eff_Date { get; set; }
        public decimal Rate { get; set; }
        public decimal Above_Per { get; set; }
        public decimal Above_Amt { get; set; }
        public string Remarks { get; set; }
    }

}
