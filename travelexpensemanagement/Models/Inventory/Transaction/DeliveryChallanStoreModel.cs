namespace travelexpensemanagement.Models.Inventory.Transaction
{
    public class DeliveryChallanStoreModel
    {
        public string? V_TYPE {get; set;}
        public int? V_NO {get; set;}
        public DateTime? V_DATE {get; set;}
        public string? DOC_ID {get; set;}
        public string? BILL_NO {get; set;}
        public DateTime? BILL_DATE {get; set;}
        public string? ITEM_TYPE {get; set;}
        public int? BILL_CODE {get; set;}
        public string? BILL_NAME {get; set;}
        public string? BILL_ADD1 {get; set;}
        public string? BILL_ADD2 {get; set;}
        public string? BILL_ADD3 {get; set;}
        public int? BILL_CITY {get; set;}
        public string? BILL_GST {get; set;}
        public string? BILL_PINCODE {get; set;}
        public int? DRCR_CODE {get; set;}
        public int? BROKER_CODE {get; set;}
        public string? PARTY_NAME {get; set;}
        public int? SHIP_CODE {get; set;}
        public string? SHIP_NAME {get; set;}
        public string? SHIP_ADD1 {get; set;}
        public string? SHIP_ADD2 {get; set;}
        public string? SHIP_ADD3 {get; set;}
        public int? SHIP_CITY {get; set;}
        public string? SHIP_GST {get; set;}
        public string? SHIP_PINCODE {get; set;}
        public int? CITY_CODE {get; set;}
        public string? DOC_TYPE {get; set;}
        public int? DOC_NO {get; set;}
        public DateTime? DOC_DATE {get; set;}
        public string? DOC_NAME {get; set;}
        public decimal? TOT_NOS {get; set;}
        public decimal? TOT_GROSS {get; set;}
        public decimal? TOT_QTY {get; set;}
        public decimal? AMOUNT {get; set;}
        public decimal? DISC_PER {get; set;}
        public decimal? DISC_AMT {get; set;}
        public decimal? CESS_PER {get; set;}
        public decimal? CESS_AMT {get; set;}
        public decimal? PACK_PER {get; set;}
        public decimal? PACK_AMT {get; set;}
        public decimal? SGST_PER {get; set;}
        public decimal? IGST_PER {get; set;}
        public decimal? CGST_PER {get; set;}
        public decimal? CGST_AMT {get; set;}
        public decimal? SGST_AMT {get; set;}
        public decimal? IGST_AMT {get; set;}
        public decimal? FRT_AMT {get; set;}
        public decimal? FRT_TAXP {get; set;}
        public decimal? FRT_TAX {get; set;}
        public decimal? TCS_PER {get; set;}
        public decimal? TCS_AMT {get; set;}
        public decimal? ROUNDOFF {get; set;}
        public decimal? NAMOUNT {get; set;}
        public string? INPUT_TYPE {get; set;}
        public string? GR_NO {get; set;}
        public DateTime? GR_DATE {get; set;}
        public string? TRANSPORT_NAME {get; set;}
        public int? TRANSPORT_CODE {get; set;}
        public string? TRUCK_NO {get; set;}
        public decimal? TDS_PER {get; set;}
        public decimal? TDS_AMT {get; set;}
        public string? STATION_NAME {get; set;}
        public int? STATION_CODE {get; set;}
        public int? CONSG_ADD_ID {get; set;}
        public int? PARTY_ADD_ID {get; set;}
        public string? REMARK {get; set;}
        public string? FAPROV_STATUS {get; set;}
        public string? FAPROV_REMARKS {get; set;}
        public string? NATURE_OFWORK {get; set;}
        public string? STATUS {get; set;}
        public string? IRN {get; set;}
        public string? SIGNED_JSON {get; set;}
        public string? SIGNED_QR {get; set;}
        public int? EINVOICE_FLG {get; set;}
        public int? EWAYBILL_FLG {get; set;}
        public string? EWAYBILL_NO {get; set;}
        public string? EWAYBILL_JSON {get; set;}
        public string? EWAYBILL_DATE {get; set;}
        public int? SRNO {get; set;}
        public string? SUPPLY_TYPE {get; set;}
        public string? GSTRECO_REFTYPE {get; set;}
        public int? GSTRECO_REFNO {get; set;}
        public string? TRAN_TYPE {get; set;}
        public int? MAILSEND {get; set;}
        public DateTime? MONTH_3B {get; set;}
        public DateTime? MONTH_3BN {get; set;}
        public string? MTH_REVYN3B {get; set;}
        public string? MOVE_TYPE {get; set;}
        public string? DESP_ADDRESS {get; set;}
        public string? JW_NATURE {get; set;}
        public string? EWB_NO {get; set;}
        public int? EMP_CODE {get; set;}
        public DateTime? RET_DATE {get; set;}
        public int? DESP_FROMPARTY {get; set;}
        public int? DESP_TOPARTY {get; set;}
        public string? DESP_FROMGST {get; set;}
        public string? DESP_TOGST {get; set;}
        public int? DESP_FROMCITY {get; set;}
        public int? DESP_TOPCITY {get; set;}
        public int? TPT_DISTANCE { get; set; }
        public string? ACTION { get; set; }

        public List<DeliveryChallanStoreFooterModel> items { get; set; } = new List<DeliveryChallanStoreFooterModel>();
    }

    public class DeliveryChallanStoreFooterModel
    {
        public  string? V_TYPE {get; set;}
        public  int? V_NO {get; set;}
        public  DateTime? V_DATE {get; set;}
        public string? DOC_ID {get; set;}
        public  int? ITEM_CODE {get; set;}
        public string? ITEM_NAME {get; set;}
        public string? ITEM_UNIT {get; set;}
        public string? HSN_CODE {get; set;}
        public  int? SNO {get; set;}
        public  decimal? NOS {get; set;}
        public decimal? GROSS {get; set;}
        public decimal? QTY {get; set;}
        public decimal? RATE {get; set;}
        public decimal? AMOUNT {get; set;}
        public  decimal? DISC_PER {get; set;}
        public  decimal? CGST_PER {get; set;}
        public  decimal? SGST_PER {get; set;}
        public  decimal? IGST_PER {get; set;}
        public  decimal? DISC_AMT {get; set;}
        public  decimal? CGST_AMT {get; set;}
        public  decimal? SGST_AMT {get; set;}
        public  decimal? IGST_AMT {get; set;}
        public  decimal? PACK_AMT {get; set;}
        public  decimal? PACK_PER {get; set;}
        public  decimal? CESS_PER {get; set;}
        public  decimal? CESS_AMT {get; set;}
        public  string? REMARK {get; set;}
        public string? REF_TYPE {get; set;}
        public  int? REF_NO {get; set;}
        public string? WB_TYPE {get; set;}
        public  int? WB_NO {get; set;}
        public string? TYPE {get; set;}
        public  int? STATUS {get; set;}
        public  int? SRNO {get; set;}
        public int? TAX_CODE { get; set; }
    }

    public class DeliveryChallanStoreOrderDetails
    {
        public DateTime? V_DATE { get; set; }
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public int? PARTY_CODE { get; set; }
        public string? PARTY_NAME { get; set; }
        public string? BILL_ADD1 { get; set; }
        public string? BILL_ADD2 { get; set; }
        public string? BILL_ADD3 { get; set; }
        public int? BILL_CITY { get; set; }
        public string? CITY_NAME { get; set; }
        public string? BILL_GST { get; set; }
        public string? BILL_PINCODE { get; set; }
        public int? SHIP_FROM { get; set; }
        public string? SHIP_NAME { get; set; }
        public string? SHIP_ADD1 { get; set; }
        public string? SHIP_ADD2 { get; set; }
        public string? SHIP_ADD3 { get; set; }
        public int? SHIP_CITY { get; set; }
        public string? SHIPCITY_NAME { get; set; }
        public string? SHIP_GST { get; set; }
        public string? SHIP_PINCODE { get; set; }
        public List<DeliveryChallanStoreOrderItem> Items { get; set; } = new List<DeliveryChallanStoreOrderItem>();
    }

    public class DeliveryChallanStoreOrderItem
    {
        // From ITEM_MAST
        public int? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public int? UOM_CODE { get; set; }
        public string? UNIT { get; set; }
        public string? HSN_CODE { get; set; }
        public int? NOS { get; set; }
        public decimal? RECD_QTY { get; set; }
        public decimal? INV_QTY { get; set; }
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
        public string? REF_TYPE { get; set; }
        public int? REF_NO { get; set; }
    }

    public class DeliveryChallanStoreListModel
    {
        public string? DOC_ID { get; set; }
        public int? V_NO { get; set; }
        public string? V_TYPE { get; set; }
        public DateTime? V_DATE { get; set; }
        public DateTime? DOC_DATE { get; set; }
        public string? BILL_NO { get; set; }
        public DateTime? BILL_DATE { get; set; }
        public string? ITEM_TYPE { get; set; }
        public string? BilledTo { get; set; }
        public string? BrokerName { get; set; }
        public string? CrName { get; set; }
    }

    public class WBCheckRequest
    {
        public string VType { get; set; }
        public List<WBCheckItem> Items { get; set; }
    }

    public class WBCheckItem
    {
        public int ItemCode { get; set; }
        public string ItemName { get; set; }
        public string WbType { get; set; }
        public string WbNo { get; set; }
        public double Quantity { get; set; }
    }
}
