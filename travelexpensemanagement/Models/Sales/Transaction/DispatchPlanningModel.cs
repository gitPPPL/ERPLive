namespace travelexpensemanagement.Models.Sales.Transaction
{
    public class DispatchPlanningModel
    {
        // Common Fields
        public int? COMP_CODE { get; set; }
        public int? YEAR_CODE { get; set; }
        public int? BRANCH_CODE { get; set; }
        public string? DOC_ID { get; set; }
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? ITEM_CODE { get; set; }
        public int? TENACITY_CODE { get; set; }
        public int? STATUS_CODE { get; set; }
        public decimal? PLAN_QTY { get; set; }

        // Dispatch Plan Header Fields
        public int? SNO { get; set; }
        public string? PARTY_CODE { get; set; }
        public string? PARTY_NAME { get; set; }
        public string? TENACITY { get; set; }
        public decimal? OPN_QTY { get; set; }
        public decimal? OPN_WGT { get; set; }
        public decimal? PROD_PLANQTY { get; set; }
        public decimal? DISP_PLANQTY { get; set; }
        public decimal? APPROX_AMT { get; set; }
        public int? SRNO { get; set; }

        // Audit Fields
        public int? UUSER { get; set; }
        public DateTime? UDATE { get; set; }
        public int? EUSER { get; set; }
        public DateTime? EDATE { get; set; }
        public string? AED { get; set; }
        public string? WSID { get; set; }
        public string? LIP { get; set; }
        public string? LID { get; set; }

        // Dispatch Plan Footer Fields
        public int? DOC_NO { get; set; }
        public int? DOC_SNO { get; set; }
        public DateTime? DOC_DATE { get; set; }
        public int? AC_CODE { get; set; }
        public decimal? RATE { get; set; }
        public decimal? SALE_QTY { get; set; }
        public decimal? BAL_QTY { get; set; }
        public decimal? AMOUNT { get; set; }

        // Dispatch Plan Footer List
        public List<DispatchPlanningModel>? FooterList { get; set; }

        public string? CopyDataMode { get; set; }

        public List<DispatchPendingOrderModel> PendingOrderList { get; set; } = new();
    }

    public class DispatchPendingOrderModel
    {
        public string? DOC_TYPE { get; set; }
        public string? DOC_NO { get; set; }
        public int DOC_SNO { get; set; }
        public DateTime? DOC_DATE { get; set; }

        public int? ITEM_CODE { get; set; }
        public int? TENACITY_CODE { get; set; }
        public int? STATUS_CODE { get; set; }
        public int? PARTY_CODE { get; set; }

        public decimal RATE { get; set; }
        public decimal SALE_QTY { get; set; }
        public decimal BAL_QTY { get; set; }
        public decimal PLAN_QTY { get; set; }

        public int V_SNO { get; set; }

        public int? SelectedItemCode { get; set; }
        public int? SelectedTenacityCode { get; set; }
    }

}
