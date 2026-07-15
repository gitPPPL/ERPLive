namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class IndentStatusUpdateModel
    {
        public class StorePurchaseOrderStatusModel
        {
            public string? VType { get; set; }
            public int? VNo { get; set; }
            public DateTime? VDate { get; set; }

            public int? PartyCode { get; set; }
            public string? PartyName { get; set; }

            public int? ItemCode { get; set; }
            public string? ItemName { get; set; }

            public decimal? Qty { get; set; }
            public decimal? RecdQty { get; set; }
            public decimal? BalQty { get; set; }

            public string? DispThrough { get; set; }
            public string? DispRef { get; set; }
            public string? DispRemarks { get; set; }
            public string? SNO { get; set; }
        }

        public class IndentStatusUpdateSaveModel
        {
            public int? VNo { get; set; }
            public DateTime? VDate { get; set; }

            public int? ItemCode { get; set; }
            public int? Sno { get; set; }

            public string? DispThrough { get; set; }
            public string? DispRef { get; set; }
            public string? DispRemarks { get; set; }
        }

    }
}
