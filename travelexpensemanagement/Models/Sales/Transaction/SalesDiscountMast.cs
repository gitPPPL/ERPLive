namespace travelexpensemanagement.Models.Sales.Transaction
{
    public class SalesDiscountMast
    {
        //public int? COMP_CODE { get; set; }
        //public string? V_TYPE { get; set; }
        public int? CODE { get; set; }
        public string? NAME { get; set; }       
        public bool? ACTIVE { get; set; }       
        public string? SaveOrUpdate { get; set; }
        public List<SalesDiscountMastDetail>? salesDiscountMastDetails { get; set; }

    }

    public class SalesDiscountMastDetail
    {
        public int? ITEM_CODE { get; set; }
        public decimal? ITEM_DIFF { get; set; }
        public int? GRAM_CODE { get; set; }
        public decimal? GRAM_DIFF { get; set; }
        public int? COLOR_CODE { get; set; }
        public decimal? COLOR_DIFF { get; set; }
        public int? SIZE_CODE { get; set; }
        public decimal? SIZE_DIFF { get; set; }
        public int? MESH_CODE { get; set; }
        public decimal? MESH_DIFF { get; set; }
    }

}
