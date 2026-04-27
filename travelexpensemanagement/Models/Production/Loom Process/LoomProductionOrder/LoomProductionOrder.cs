using Microsoft.Identity.Client;

namespace travelexpensemanagement.Models.Production.Loom_Process.LoomProductionOrder
{
    public class LoomProductionOrder
    {
        public string? DOC_ID { get; set; }
        public string? V_TYPE { get; set; }
        public int ? V_NO { get; set; } 
        public DateTime? V_DATE { get; set; }
        public DateTime? EFF_DATE { get; set; }
        public DateTime? COMP_DATE { get; set; }
        public decimal? PROD_QTY { get; set; }
        public int? ITEM_CODE { get; set; }
        public decimal? APPROX_MTR { get; set; }
        public decimal? APPROX_KG { get; set; }
        public int? NO_OF_LOOM { get; set; }
        public string? REMARKS { get; set; }

        public List<Item> ItemList { get; set; }
    }
    public class Item
    {
        public string? DOC_ID { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? FITEM_CODE { get; set; }
        public string? FITEM_NAME { get; set; }
        public int? COLOR_CODE { get; set; }
        public string? MITEM_NAME { get; set; }
        public int? MITEM_CODE { get; set; }
        public int? LOOM_CODE { get; set; }
        public DateTime? FEFF_DATE { get; set; }
        public string? EFF_SHIFT { get; set; }
        public int? SIZE_CODE { get; set; }
        public int? PTYPE_CODE { get; set; }
        public decimal? GRAM_CODE { get; set; }
        public int? MESH_CODE { get; set; }
        public string? STATUS { get; set; }

    }
}
