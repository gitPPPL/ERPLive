
namespace travelexpensemanagement.Models.PlantMaintenance.Transaction
{
    public class MaintenanceStatus
    {
        public int? V_NO { get; set; }
        public string? DOC_ID { get; set; }
        public string? V_TYPE { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? PLAN_TYPE { get; set; }
        public int? PLAN_NO { get; set; }
        public int? PLAN_CODE { get; set; }
        public int? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public string? PLAN_NAME { get; set; }
        public int? PLACE_CODE { get; set; }
        public string? PLACE_NAME { get; set; }
        public int? DEPT_CODE { get; set; }
        public string? DEPT_NAME { get; set; }
        public DateTime? S_DATE { get; set; }
        public DateTime? E_DATE { get; set; }
        public string? REF_TYPE { get; set; }
        public int? REF_NO { get; set; }
        public int? CLDEPT_CODE { get; set; }
        public string? CLOSE_REMARKS { get; set; }

        public List<Activity> ActivityList { get; set; }
        public List<Spares> SparesList { get; set; }
        public List<FollowResource> FollowResource { get; set; }

    }
    public class Activity
    {
        public int? V_NO { get; set; }
        public string? V_TYPE { get; set; }
        public string? DOC_ID { get; set; }
        public int? ACT_CODE { get; set; }
        public string? ACT_NAME { get; set; }
        public int? CAT_CODE { get; set; }
        public string? CAT_NAME { get; set; }
        public int? CHK_CODE { get; set; }
        public string? CHK_NAME { get; set; }
        public string? FREQUENCY { get; set; }
        public DateTime? AS_DATE { get; set; }
        public DateTime? AE_DATE { get; set; }
        public string? STATUS { get; set; }
        public string? AREMARKS { get; set; }

    }
    public class Spares
    {
        public int? V_NO { get; set; }
        public string? V_TYPE { get; set; }
        public string? DOC_ID { get; set; }
        public int? SITEM_CODE { get; set; }
        public string? SITEM_NAME { get; set; }
        public decimal? QUANTITY { get; set; }
        public string? SREMARKS { get; set; }

    }
    public class FollowResource
    {
        public int? V_NO { get; set; }
        public string? V_TYPE { get; set; }
        public string? DOC_ID { get; set; }
        public int? EMP_CODE { get; set; }
        public string? EMP_NAME { get; set; }
        public DateTime? FS_DATE { get; set; }
        public DateTime? FE_DATE { get; set; }
        public string? HOUR { get; set; }
        public string? FREMARKS { get; set; }

    }
}
