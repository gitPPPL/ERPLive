namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class PurchaseRequestModel
    {


        public class PurchaseRequest_model
        {
            public Header Header { get; set; }
            public List<ItamDetails> ItamDetails { get; set; }
            public List<PurchaseDocuments> PurchaseDocuments { get; set; }

        }


        public class Header
        {
            public int? V_NO { get; set; }

            public string? V_TYPE { get; set; }

            public DateTime? V_DATE { get; set; }

            public int? PLACE_CODE { get; set; }

            public string? PlaceName { get; set; }

            public string? DOC_ID { get; set; }

            public int? OWNER_CODE { get; set; }

            public string? OWNER_NAME { get; set; }

            public int? DEPT_CODE { get; set; }

            public string? DEPT_NAME { get; set; }

            public int? STATUS { get; set; }

            public DateTime? VALID_DATE { get; set; }

            public DateTime? TARGET_DATE { get; set; }

            public string? REASON { get; set; }

            public string? REMARKS { get; set; }

            public string? action { get; set; }

            public int? URGENT_REQUEST { get; set; }

            public int? PLAN_NO { get; set; }

            public string? PLAN_TYPE { get; set; }
            public string? FAPROV_STATUS { get; set; }
            public string? FAPROV_REMARKS { get; set; }



        }

        public class ItamDetails
        {
            public bool isPOGenerated { get; set; } = false;
            public int? ITEM_CODE { get; set; }
            public string? ItemName { get; set; }
            public int? MAKE_CODE { get; set; }

            public String? Make { get; set; }

            public string? TECH_DESC { get; set; }
            public int? UOM_CODE { get; set; }
            public decimal? STD_REQ { get; set; }
            public decimal? CUR_STK { get; set; }
            public decimal? AVG_CONS { get; set; }
            public decimal? RESERVE_QTY { get; set; }
            public decimal? OPEN_POQTY { get; set; }
            public decimal? OPEN_RQQTY { get; set; }
            public decimal? USER_QTY { get; set; }
            public decimal? REQ_QTY { get; set; }
            public string? REQ_REASON { get; set; }
            public string? REMARKS { get; set; }
            public decimal? APROX_RATE { get; set; }
            public int? PRIORITY_CODE { get; set; }
            public string? PRIORITY_TYPE { get; set; }
            public string? SCRAP_TYPE { get; set; }
            public int? PLACE_Code { get; set; }
            public string? PLACE_USE { get; set; }
            public int? WORK_TYPECODE { get; set; }
            public string? WORK_TYPE { get; set; }
            public int? APROV_CODE { get; set; }
            public string? APROV_STATUS { get; set; }
            public string? APROV_REMARKS { get; set; }
            public int? STATUS { get; set; }
            public string? MONTHLY { get; set; }

        }

        public class PurchaseDocuments
        {
            public string? FILE_NAME { get; set; }
            public string? FILE_Path { get; set; }
            public string FILE_DATA { get; set; }

        }

        public class LastTenPurchaseRequestModel
        {
            public int ItemCode { get; set; }
            public string VNo { get; set; }
            public string VDate { get; set; }
            public string Department { get; set; }
            public string ItemName { get; set; }
            public string MakeName { get; set; }
            public string Unit { get; set; }
            public decimal Qty { get; set; }
            public string PlaceofUse { get; set; }
            public string TechDesc { get; set; }
            public string Remarks { get; set; }
            public string Status { get; set; }
        }
        public class LastTenConsumptionModel
        {
            public int ItemCode { get; set; }
            public string VNo { get; set; }
            public string Date { get; set; }
            public string ItemName { get; set; }
            public string Make { get; set; }
            public string Unit { get; set; }
            public decimal Qty { get; set; }
            public decimal Rate { get; set; }
            public string Department { get; set; }
            public string Machine { get; set; }
            public string Remarks { get; set; }
            public string Status { get; set; }
        }

        public class LastTenPurchaseHistoryModel
        {
            public int ItemCode { get; set; }
            public string VNo { get; set; }
            public string Date { get; set; }
            public string Supplier { get; set; }
            public string ItemName { get; set; }
            public string Make { get; set; }
            public string Unit { get; set; }
            public decimal Qty { get; set; }
            public decimal Rate { get; set; }
            public decimal OthAmt { get; set; }
            public decimal CGSTPer { get; set; }
            public decimal SGSTPer { get; set; }
            public decimal IGSTPer { get; set; }
            public decimal PackPer { get; set; }
            public decimal DiscPer { get; set; }
            public decimal LDRate { get; set; }
            public string Remarks { get; set; }
            public string Status { get; set; }
        }

        public class ItemWisePurchaseQuotationHistoryModel
        {
            public int ItemCode { get; set; }
            public string VNo { get; set; }
            public string Date { get; set; }
            public string Supplier { get; set; }
            public string ItemName { get; set; }
            public string Make { get; set; }
            public string Unit { get; set; }
            public string GroupNo { get; set; }

            public decimal Qty { get; set; }
            public decimal Rate { get; set; }
            public decimal Freight { get; set; }

            public decimal CGSTPer { get; set; }
            public decimal SGSTPer { get; set; }
            public decimal IGSTPer { get; set; }

            public decimal PackPer { get; set; }
            public decimal DiscPer { get; set; }

            public decimal OthExps { get; set; }
            public decimal LDRate { get; set; }

            public string Remarks { get; set; }
            public string Status { get; set; }
        }

    }
}
