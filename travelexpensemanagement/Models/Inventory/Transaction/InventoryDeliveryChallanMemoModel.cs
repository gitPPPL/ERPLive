namespace travelexpensemanagement.Models.Inventory.Transaction
{
    public class InventoryDeliveryChallanMemoModel
    {
        public int? COMP_CODE { get; set; }
        public int? YEAR_CODE { get; set; }
        public int? BRANCH_CODE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? EMP_CODE { get; set; }
        public string? EMP_NAME { get; set; }
        public int? VENDOR_CODE { get; set; }
        public string? VENDOR_NAME { get; set; }
        public int? TRANSPORT_CODE { get; set; }
        public string? TRANSPORT_NAME { get; set; }
        public string? THROUGH { get; set; }
        public DateTime? RETURN_DATE { get; set; }
        public string? REMARKS { get; set; }
        public int? STATUS { get; set; }
        public int? ACTIVE { get; set; }

        public string? ACTION { get; set; }

        public List<InventoryDeliveryChallanMemoItemsModel> items { get; set; } = new List<InventoryDeliveryChallanMemoItemsModel>();
    }

    public class InventoryDeliveryChallanMemoItemsModel
    {
        public int? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public int? NOS { get; set; }
        public decimal? APPROX_AMT { get; set; }
        public decimal? QTY { get; set; }
        public int? UNIT_CODE { get; set; }
        public string? UNIT_NAME { get; set; }
        public string? REMARKS { get; set; }
        public int? ACTIVE { get; set; }
    }
}
