namespace travelexpensemanagement.Models.Production.Master.ItemStandardParameterMaster
{
    public class ItemStandardParameterMaster
    {
        public int? CODE { get; set; }
        public int? ITEM_CODE { get; set; }
        public int? MESH_CODE { get; set; }
        public int? COLOR_CODE { get; set; }
        public decimal? CUTTING_STD_WT { get; set; }
        public decimal? THREAD_STD_WT { get; set; }
        public decimal? PRINTING_STD_WT { get; set; }
        public string? TOP_S {  get; set; }
        public decimal? MINSTD_WT { get; set; }
        public decimal? MAXSTD_WT { get;  set; }
        public string? DENIER { get; set; }
        public string? LINER_MICRONE { get; set; }
        public decimal? CAPACITY { get; set; }
        public decimal? LINER_WT { get; set; }
        public string? PRINTING_TYPE { get; set; }
        public string? BAG_TYPE {  get; set; }
        public string? BOTTOM_S { get; set; }
        public decimal? GMG_REQ { get; set; }
        public int? PACKING {  get; set; }  
        public decimal? GRAM_WITHLAM { get; set; }
        public string? BAG_SIZE { get; set; }
        public decimal? WIDTH { get; set; }
        public string? LINER_SIZE { get; set; }
        public decimal? GSM {  get; set; }
        public decimal? BAG_WT { get; set; }
        public int? NOS {  get; set; }  
        public string? BALING_INST { get; set; }
        public string? LABELING_INST { get; set; }
        public string? WEIGHING_INST { get; set; }
        public string? LINER { get; set; }
        public string? Action { get; set; }

        public List<ItemStandardParameterDetailModel> Details { get; set; }
    }
    public class ItemStandardParameterDetailModel
    {
        public int CODE { get; set; }
        public int SUB_ITEM { get; set; }
        public decimal STD_WT { get; set; }
        public int SRNO { get; set; }
    }
}
