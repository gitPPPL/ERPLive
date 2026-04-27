
namespace travelexpensemanagement.Models.PlantMaintenance.Master.MaintenancePlanMaster
{
    public class MaintenancePlanMaster
    {
        public int? CODE { get; set; }
        public int? SRNO { get; set; }
        public string? PLAN_NAME { get;set; }
        public string? M_NAME { get; set; }
        public int? M_CODE { get; set; }
        public string? PLACE_NAME { get; set; }
        public int? PLACE_CODE { get; set; }
        public string? SECTION_NAME { get; set; }
        public int? SECTION_CODE { get; set; }
        public string? FREQUENCY { get; set; }
        public int? FREQUENCY_CODE { get; set; }
        public int? CAT_CODE { get; set; }
        public string? CAT_NAME { get; set; }
        public int? DUE_DAYS { get; set; }
        public DateTime? DUE_DATE { get; set; }
        public int? ACTIVE {  get; set; }
        public List<PMActivityMaster> Details { get; set; }
        public List<PMSpareMaster> Details1 { get; set; }

    }

    public class PMActivityMaster
    {
        public int? CODE { get; set; }
        public string? ACTIVITY_NAME { get; set; }
        public int? ACTIVITY_CODE { get; set; }
        public string? ACTIVITY_REMARKS { get; set; }
    }
    public class PMSpareMaster
    {
        public int? CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public int? ITEM_CODE { get; set; } 
        public string? SPARE_REMARKS { get; set; }
        public decimal? QUANTITY { get; set; }
    }
}
