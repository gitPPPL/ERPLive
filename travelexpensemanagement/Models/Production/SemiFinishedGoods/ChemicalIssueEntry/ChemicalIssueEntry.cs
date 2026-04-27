namespace travelexpensemanagement.Models.Production.SemiFinishedGoods.ChemicalIssueEntry
{
    public class ChemicalIssueEntry
    {
        public string? DOC_ID { get; set; }
        public int? V_NO { get; set; }
        public string? V_TYPE { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? SHIFT { get; set; }
        public string? SLIP_NO { get; set; }
        public string? PORD_TYPE { get; set; }
        public int? PORD_NO { get; set; }
        public int? PLACE_CODE { get; set; }
        public string? REMARKS { get; set; }
        public int? STATUS { get; set; }
        public decimal? AMOUNT { get; set; }
        public string? PLAN_TYPE { get; set; }
        public int? PLAN_NO { get; set; }
        public string? Action { get; set; }

        public List<Item> ItemList { get; set; }
    }
    public class Item
    {
        public string? DOC_ID { get; set; }
        public int? V_NO { get; set; }
        public string? V_TYPE { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public int? UOM_CODE { get; set; }
        public string? UOM_NAME { get; set; }
        public string? LOT_NO { get; set; }
        public int? NOS { get; set; }
        public decimal? QTY { get; set; }
        public int? FROM_DEPT { get; set; }
        public string? IREMARKS { get; set; }
        public decimal? RATE { get; set; }
        public decimal? IAMOUNT { get; set; }
        public decimal? LAND_RATE { get; set; }
        public decimal? LAND_AMT { get; set; }
        public string? IPORD_TYPE { get; set; }
        public int? IPORD_NO { get; set; }
        public string? Action { get; set; }

    }
}
