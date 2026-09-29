namespace travelexpensemanagement.Models.Sales.Transaction
{
    public class DeliveryOrderModel
    {
        public string? V_TYPE { get; set; }
        public DateTime? V_DATE { get; set; }
        public DateTime? DEL_DATE { get; set; }
        public int? V_NO { get; set; }
        public int? GODOWN_CODE { get; set; }
        public int? BILL_CODE { get; set; }
        public string? BILL_NAME { get; set; }
        public string? BILL_ADD1 { get; set; }
        public string? BILL_ADD2 { get; set; }
        public string? BILL_ADD3 { get; set; }
        public int? BILL_CITY { get; set; }
        public string? BILL_CITYNAME { get; set; }
        public int? BILL_STATE { get; set; }
        public string? BILL_STATENAME { get; set; }
        public int? BILL_COUNTRY { get; set; }
        public string? BILL_COUNTRYNAME { get; set; }
        public string? BILL_GST { get; set; }
        public string? BILL_PINCODE { get; set; }
        public int? INSUCR_DAYS { get; set; }
        public int? SHIP_CODE { get; set; }
        public string? SHIP_NAME { get; set; }
        public string? SHIP_ADD1 { get; set; }
        public string? SHIP_ADD2 { get; set; }
        public string? SHIP_ADD3 { get; set; }
        public int? SHIP_CITY { get; set; }
        public string? SHIP_CITYNAME { get; set; }
        public int? SHIP_STATE { get; set; }
        public string? SHIP_STATENAME { get; set; }
        public int? SHIP_COUNTRY { get; set; }
        public string? SHIP_COUNTRYNAME { get; set; }
        public string? SHIP_GST { get; set; }
        public string? SHIP_PINCODE { get; set; }
        public int? TAX_CODE { get; set; }
        public string? PACK_TYPE { get; set; }
        public int? PACK_NO { get; set; }
        public string? ITEM_TYPE { get; set; }
        public int? WB_NO { get; set; }
        public string? WB_TYPE { get; set; }
        public int? ISSUE_NO { get; set; }
        public string? ISSUE_TYPE { get; set; }
        public decimal? AMOUNT { get; set; }
        public decimal? PACK_PER { get; set; }
        public decimal? PACK_AMT { get; set; }
        public decimal? TCS_PER { get; set; }
        public decimal? TCS_AMT { get; set; }
        public decimal? CGST_PER { get; set; }
        public decimal? CGST_AMT { get; set; }
        public decimal? SGST_PER { get; set; }
        public decimal? SGST_AMT { get; set; }
        public decimal? IGST_PER { get; set; }
        public decimal? IGST_AMT { get; set; }
        public decimal? CESS_PER { get; set; }
        public decimal? CESS_AMT { get; set; }
        public decimal? LOAD_PER { get; set; }
        public decimal? LOAD_AMT { get; set; }
        public string? LOAD_AC { get; set; }
        public string? LOAD_REM { get; set; }
        public decimal? WB_AMT { get; set; }
        public string? WB_AC { get; set; }
        public int? TOT_NOS { get; set; }
        public decimal? FRT_AMT { get; set; }
        public decimal? ROUND_OFF { get; set; }
        public decimal? NAMOUNT { get; set; }
        public decimal? INSU_PER { get; set; }
        public decimal? INSU_AMT { get; set; }
        public string? TDS_PER { get; set; }
        public decimal? TDS_AMT { get; set; }
        public decimal? FRT_TAXPER { get; set; }
        public decimal? FRT_TAXAMT { get; set; }
        public string? WB_REM { get; set; }
        public decimal? TOT_GROSS { get; set; }
        public decimal? TOT_NET { get; set; }
        public string? GR_NO { get; set; }
        public DateTime? GR_DATE { get; set; }
        public string? VEHICLE_NO { get; set; }
        public int? TRANSPORT_CODE { get; set; }
        public string? TRANSPORT_NAME { get; set; }
        public string? DRIVER_NAME { get; set; }
        public string? DRIVER_NO { get; set; }
        public int? TPT_MODE { get; set; }
        public int? TPT_DISTANCE { get; set; }
        public string? WAYBILL_NO { get; set; }
        public decimal? FRT_TOPAY { get; set; }
        public string? REMARK { get; set; }
        public decimal? WB_QTY { get; set; }
        public decimal? DISC_PER { get; set; }
        public decimal? DISC_AMT { get; set; }
        public decimal? CDISC_PER { get; set; }
        public decimal? CDISC_AMT { get; set; }
        public int? AGENT_CODE { get; set; }
        public int? FORM_CODE { get; set; }
        public string? SAUDA_TYPE { get; set; }
        public int? SAUDA_NO { get; set; }
        public decimal? SAUDA_RATE { get; set; }
        public int? DEFECTIVE_GOODS { get; set; }
        public int? CAL_ONPCS { get; set; }
        public int? PRINT_DETAIL { get; set; }
        public decimal? EXRATE { get; set; }
        public string? BUYER_ORDNO { get; set; }
        public string? PLACE_RECEIPT { get; set; }
        public string? PORT_LOADING { get; set; }
        public string? PORT_DISCHARGE { get; set; }
        public string? FINAL_DEST { get; set; }
        public string? FINAL_DEST_COUNTRY { get; set; }
        public string? DELIVERY_TERMS { get; set; }
        public string? SB_NO { get; set; }
        public DateTime? SB_DATE { get; set; }
        public string? PORT_CODE { get; set; }
        public string? LUT_DETAIL { get; set; }
        public string? INSU_DETAIL { get; set; }
        public string? SUPPLY_TYPE { get; set; }
        public string? LC_NO { get; set; }
        public string? SHIPMENT_TYPE { get; set; }
        public string? TRAN_TYPE { get; set; }
        public int? STATUS { get; set; }
        public string? PAYMENT_TERM { get; set; }


        public List<DeliveryOrderFooterModel> Footer { get; set; } = new();
    }

    public class DeliveryOrderFooterModel
    {
        public string? DOC_ID { get; set; }
        public int? YEAR_CODE { get; set; }
        public int? COMP_CODE { get; set; }
        public int? BRANCH_CODE { get; set; }
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public string? UNIT_NAME { get; set; }
        public int? UNIT_CODE { get; set; }
        public string? HSN_CODE { get; set; }
        public int? NOS { get; set; }
        public decimal? GROSS_QTY { get; set; }
        public decimal? QTY { get; set; }
        public decimal? FOR_RATE { get; set; }
        public decimal? RATE { get; set; }
        public decimal? AMOUNT { get; set; }
        public decimal? PACK_PER { get; set; }
        public decimal? PACK_AMT { get; set; }
        public decimal? DISC_PER { get; set; }
        public decimal? DISC_AMT { get; set; }
        public decimal? CGST_PER { get; set; }
        public decimal? CGST_AMT { get; set; }
        public decimal? SGST_PER { get; set; }
        public decimal? SGST_AMT { get; set; }
        public decimal? IGST_PER { get; set; }
        public decimal? IGST_AMT { get; set; }
        public decimal? CESS_PER { get; set; }
        public decimal? CESS_AMT { get; set; }
        public string? REMARK { get; set; }
        public int? STATUS { get; set; }
        public string? PACK_TYPE { get; set; }
        public int? PACK_NO { get; set; }
        public string? LOT_No { get; set; }
        public string? SAUDA_TYPE { get; set; }
        public int? SAUDA_NO { get; set; }
        public decimal? SAUDA_RATE { get; set; }
        public string? ORD_TYPE { get; set; }
        public int? ORD_NO { get; set; }
        public decimal? ORD_RATE { get; set; }
        public string? DCN_TYPE { get; set; }
        public int? DCN_NO { get; set; }
        public string? ROW_ID { get; set; }
        public int? TAX_CODE { get; set; }
        public decimal? FRT_AMT { get; set; }
        public decimal? INSU_AMT { get; set; }
        public decimal? CDISC_AMT { get; set; }
        public decimal? WB_QTY { get; set; }
        public int? SNO { get; set; }
        public decimal? GATE_QTY { get; set; }
        public decimal? LAND_RATE { get; set; }
        public decimal? LAND_AMT { get; set; }
        public int? DEPT_CODE { get; set; }
        public string? FINAL_LOCK { get; set; }
        public decimal? ADJ_QTY { get; set; }
        public int? TENACITY_CODE { get; set; }

    }

    public class ReconcileDeliveryOrderRequest
    {
        public string? V_TYPE { get; set; }
        public int V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public int PARTY_CODE { get; set; }
        public string? SUPPLY_TYPE { get; set; }
        public string? PROD_TYPE { get; set; }

        public decimal SUBTOTAL { get; set; }
        public decimal NET_AMOUNT { get; set; }

        public decimal FREIGHT { get; set; }
        public decimal INSURANCE_PERCENT { get; set; }
        public decimal INSURANCE_AMOUNT { get; set; }
        public decimal TDS_PERCENT { get; set; }
        public decimal TDS_AMOUNT { get; set; }

        public string? PACK_TYPE { get; set; }
        public int PACK_NO { get; set; }
        public string? ISSUE_TYPE { get; set; }
        public int ISSUE_NO { get; set; }

        public int TAX_CODE { get; set; }

        public List<ReconcileDeliveryOrderRow> Footer { get; set; } = new();
    }

    public class ReconcileDeliveryOrderRow
    {
        public int ITEM_CODE { get; set; }
        public string? DOROW_ID { get; set; }
        public string? SAUDA_TYPE { get; set; }
        public int SAUDA_NO { get; set; }
    }
}
