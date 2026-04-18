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



        }

        public class ItamDetails
        {
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
            public string? PRIORITY_TYPE { get; set; }
            public string? SCRAP_TYPE { get; set; }
            public string? PLACE_USE { get; set; }
            public string? WORK_TYPE { get; set; }
            public string? APROV_STATUS { get; set; }
            public string? APROV_REMARKS { get; set; }
            public int? STATUS { get; set; }

        }

        public class PurchaseDocuments
        {
            public string? FILE_NAME { get; set; }
            public string? FILE_Path { get; set; }

        }


    }
}
