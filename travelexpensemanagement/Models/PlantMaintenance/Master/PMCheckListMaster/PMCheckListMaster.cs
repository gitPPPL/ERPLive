
namespace travelexpensemanagement.Models.PlantMaintenance.Master.PMCheckListMaster
{
    public class PMCheckListMaster
    {
        public int? CODE { get; set; }
        public int? SNO { get; set; }
        public string? CHECKLIST_NAME {  get; set; }
        public string? CHECKLIST_TYPE {  get; set; }
        public int? CATEGORY_CODE { get; set; }
        public int? ACTIVITY_CODE { get; set; }
        public string? Action { get; set; }

        public List<PMCheckListMasterModel> Details { get; set; }
    }
    public class PMCheckListMasterModel
    {
        public int? CODE { get; set; }
        public int? SNO { get; set; }
        public int? PARAMETER_CODE { get; set; }
        public string? PARAMETER_NAME { get; set; }
        public string? REMARKS { get; set; }
    }
}
