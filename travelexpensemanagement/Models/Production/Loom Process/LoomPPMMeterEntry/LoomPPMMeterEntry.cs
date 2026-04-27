namespace travelexpensemanagement.Models.Production.Loom_Process.LoomPPMMeterEntry
{
    public class LoomPPMMeterEntry
    {
        public string? DOC_ID { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? SHIFT { get; set; }
        public int? PLACE_CODE { get; set; }
        public int? EMP_CODE { get; set; }
        public string? REMARKS { get; set; }
        public List<Item> ItemList { get; set; }
    }
    public class Item
    {
        public string? DOC_ID { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? FSHIFT { get; set; }
        public int? FPLACE_CODE { get; set; }
        public int? LOOM_CODE { get; set; }
        public string? LOOM_TYPE { get; set; }
        public int? FEMP_CODE { get; set; }
        public int? ITEM_CODE { get; set; }
        public int? PTYPE_CODE { get; set; }
        public string? PTYPE_NAME { get; set; }
        public decimal? WIDTH { get; set; }
        public decimal? GRAM { get; set; }
        public string? MESH { get; set; }
        public int? MESH_CODE { get; set; }
        public int? COLOR_CODE { get; set; }
        public string? COLOR_NAME { get; set; }
        public decimal? DNR { get; set; }
        public decimal? OPRD { get; set; }
        public decimal? CLRD { get; set; }
        public decimal? PRDN { get; set; }
        public int? PPM { get; set; }
        public string? FREMARKS { get; set; }   
        public DateTime? READING_TIME {  get; set; }
    }
}
