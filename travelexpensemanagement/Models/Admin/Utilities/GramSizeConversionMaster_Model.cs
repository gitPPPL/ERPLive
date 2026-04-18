namespace TravelExpenseManagement.Models.Admin.Utilities
{
    public class SaveTempMasterRequest
    {
        public int? CODE { get; set; } // Nullable integer to accept null
        public int? CAT_CODE { get; set; } // Nullable integer to accept null
        public string ItemType { get; set; }
        public List<GramSizeConversionMaster_Model> tableData { get; set; } // List of rows
    }

    public class GramSizeConversionMaster_Model
    {
        public decimal? FromSize { get; set; } // Nullable decimal
        public decimal? ToSize { get; set; }   // Nullable decimal
        public decimal? Per { get; set; }      // Nullable decimal
    }

}
