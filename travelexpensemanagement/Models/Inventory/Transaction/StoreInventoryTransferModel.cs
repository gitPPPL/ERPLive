namespace travelexpensemanagement.Models.Inventory.Transaction
{
    public class StoreInventoryTransferModel
    {
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? SHIFT { get; set; }
        public string? SLIP_NO { get; set; }
        public int? DEPT_CODE { get; set; }
        public int? STATUS { get; set; }
        public string? REMARKS { get; set; }
        public string? Action { get; set; }

        public List<StoreInventoryTransferFooterModel> StoreInventoryTransferFooter { get; set; } = new();
    }

    public class StoreInventoryTransferFooterModel
    {
        public int? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public int? MAKE_CODE { get; set; }
        public int? UOM_CODE { get; set; }
        public string? UOM_NAME { get; set; }
        public int? NOS { get; set; }
        public decimal? QTY { get; set; }
        public string? BIN_LOCATION { get; set; }
        public int? FROM_DEPT { get; set; }
        public int? TO_DEPT { get; set; }
        public decimal? LAND_RATE { get; set; }
        public decimal? LAND_AMT { get; set; }
        public string? KANTA_TYPE { get; set; }
        public int? KANTA_NO { get; set; }
        public string? REQ_TYPE { get; set; }
        public int? REQ_NO { get; set; }
        public string? PORD_TYPE { get; set; }
        public int? PORD_NO { get; set; }
        public string? FREMARKS { get; set; }
        public DateTime? WB_DATETIME { get; set; }
      
    }
}
