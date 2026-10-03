public class SalesReturn
{
    public FormDataModel FormData { get; set; }
    public List<RowDataModel> RowData { get; set; }
}

//public class FormDataModel
//{
//    public string? DocumentType { get; set; }
//    public string? DocumentNo { get; set; }
//    public string? DocumentDate { get; set; }
//    public string? partyCode { get; set; }
//    public string? PartyName { get; set; }
//    public string? AddressL1 { get; set; }
//    public string? AddressL2 { get; set; }
//    public string? AddressL3 { get; set; }
//    public string? Station { get; set; }
//    public string? Pincode { get; set; }
//    public string? TransactionThrough { get; set; }
//    public string? SupplyType { get; set; }
//    public string? SaleThrough { get; set; }
//    public string? Consignee { get; set; }
//    public string? consigneeName { get; set; }
//    public string? TransactionAddressL1 { get; set; }
//    public string? TransactionAddressL2 { get; set; }
//    public string? TransactionAddressL3 { get; set; }
//    public string? TransactionStation { get; set; }
//    public string? TransactionPIN { get; set; }
//    public string? FormType { get; set; }
//    public string? TaxType { get; set; }
//    public string? ProductionType { get; set; }
//    public string? ReferenceNo { get; set; }
//    public string? ReferenceDate { get; set; }
//    public string? GateNo { get; set; }
//    public string? WbNo { get; set; }
//    public string? PackNo { get; set; }
//    public string? SaudaNo { get; set; }
//    public string? SaudaRate { get; set; }
//    public bool? Pcs { get; set; }
//    public decimal? TotalAmount { get; set; }
//    public string? ACTION { get; set; }
//}

//public class RowDataModel
//{
//    public string? Code { get; set; }
//    public string? Id { get; set; }
//    public string? ProductName { get; set; }
//    public int? Nos { get; set; }
//    public decimal? GrossQuantity { get; set; }
//    public decimal? NetQuantity { get; set; }
//    public decimal? Rate { get; set; }
//    public decimal? Amount { get; set; }
//    public decimal? PackPercent { get; set; }
//    public decimal? PackAmount { get; set; }
//    public decimal? DiscPercent { get; set; }
//    public decimal? DiscAmount { get; set; }
//    public string? TaxTypes { get; set; }
//    public decimal? CgstPer { get; set; }
//    public decimal? CgstAmt { get; set; }
//    public decimal? SgstPer { get; set; }
//    public decimal? SgstAmt { get; set; }
//    public decimal? IgstPer { get; set; }
//    public decimal? IgstAmt { get; set; }
//    public decimal? CessPer { get; set; }
//    public decimal? CessAmt { get; set; }
//    public string? Remark { get; set; }
//    public string? PackNo { get; set; }
//    public string? LotNo { get; set; }
//    public string? SaudaType { get; set; }
//    public string? SaudaNo { get; set; }
//    public decimal? SaudaRate { get; set; }
//    public string? OrderType { get; set; }
//    public string? OrderNo { get; set; }
//    public decimal? OrderRate { get; set; }
//    public string? DcnType { get; set; }
//    public string? DcnNo { get; set; }
//    public string? Hsn { get; set; }
//}
public class FormDataModel
{
    public string V_TYPE { get; set; } = "";
    public int V_NO { get; set; }
    public DateTime V_DATE { get; set; }
    
    public int? FORM_CODE { get; set; }
    public int BILL_CODE { get; set; }
    public string? BILL_NAME { get; set; }
    public string? BILL_ADD1 { get; set; }
    public string? BILL_ADD2 { get; set; }
    public string? BILL_ADD3 { get; set; }
    public int? BILL_CITY { get; set; }
    public int? BILL_STATE { get; set; }
    public int? BILL_COUNTRY { get; set; }
    public string? BILL_CITYNAME { get; set; }
    public string? BILL_STATENAME { get; set; }
    public string? BILL_COUNTRYNAME { get; set; }
    public string? BILL_GST { get; set; }
    public string? BILL_PINCODE { get; set; }
    public int? BILL_ADDRESSID { get; set; }

    public int? GODOWN_CODE { get; set; }
    public int? SHIP_CODE { get; set; }
    public string? SHIP_NAME { get; set; }
    public string? SHIP_ADD1 { get; set; }
    public string? SHIP_ADD2 { get; set; }
    public string? SHIP_ADD3 { get; set; }
    public int? SHIP_CITY { get; set; }
    public int? SHIP_STATE { get; set; }
    public int? SHIP_COUNTRY { get; set; }
    public string? SHIP_CITYNAME { get; set; }
    public string? SHIP_STATENAME { get; set; }
    public string? SHIP_COUNTRYNAME { get; set; }
    public string? SHIP_GST { get; set; }
    public string? SHIP_PINCODE { get; set; }
    public int? SHIP_ADDRESSID { get; set; }

    public string? IMPORT_CURRENCY { get; set; }
    public decimal? EXRATE { get; set; }
    public int? TAX_CODE { get; set; }
    public string? PACK_TYPE { get; set; }
    public int? PACK_NO { get; set; }
    public string? ITEM_TYPE { get; set; }
    public int? AGENT_CODE { get; set; }
    public int? DEFECTIVE_GOODS { get; set; }
    public int? CAL_ONPCS { get; set; }
    public int? PRINT_DETAIL { get; set; }
    public int? TOT_NOS { get; set; }
    public decimal? TOT_GROSS { get; set; }
    public decimal? TOT_NET { get; set; }
    public decimal? WB_QTY { get; set; }
    public decimal? AMOUNT { get; set; }
    public decimal? TCS_PER { get; set; }
    public decimal? TCS_AMT { get; set; }
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
    public decimal? INSU_PER { get; set; }
    public decimal? INSU_AMT { get; set; }
    public decimal? ROUND_OFF { get; set; }
    public decimal? NAMOUNT { get; set; }
    public decimal? TDS_PER { get; set; }
    public decimal? TDS_AMT { get; set; }
    public decimal? FRT_AMT { get; set; }
    public decimal? FRT_TOPAY { get; set; }
    public string? FRT_BILLNO { get; set; }
    public string? FRT_BILLDT { get; set; }
    public DateTime? FRT_PASSDT { get; set; }
    public string? FRT_CHQ { get; set; }
    public string? FRT_REMARK { get; set; }
    public int? TRANSPORT_CODE { get; set; }
    public string? TRANSPORT_NAME { get; set; }
    public string? GR_NO { get; set; }
    public DateTime? GR_DATE { get; set; }
    public string? VEHICLE_NO { get; set; }
    public string? DRIVER_NAME { get; set; }
    public string? DRIVER_NO { get; set; }
    public int? TPT_MODE { get; set; }
    public int? TPT_DISTANCE { get; set; }
    public string? REMARK { get; set; }
    public string? WB_TYPE { get; set; }
    public int? WB_NO { get; set; }
    public string? INSU_TYPE { get; set; }
    public decimal? LOAD_PER { get; set; }
    public decimal? LOAD_AMT { get; set; }
    public string? LOAD_REM { get; set; }
    public string? LOAD_AC { get; set; }
    public decimal? WB_AMT { get; set; }
    public string? WB_AC { get; set; }
    public string? WB_REM { get; set; }
    public string? WAYBILL_NO { get; set; }
    public int? INSU_NO { get; set; }
    public string? SAUDA_TYPE { get; set; }
    public int? SAUDA_NO { get; set; }
    public decimal? SAUDA_RATE { get; set; }
    public decimal? ORD_AMT { get; set; }
    public decimal? COMM_RATE1 { get; set; }
    public decimal? COMM_RATE2 { get; set; }
    public decimal? GST_RATE { get; set; }
    public decimal? TDS_RATE { get; set; }
    public int? STATUS { get; set; }
    public string? RCM_NO { get; set; }
    public string? PAYREF_DOCID { get; set; }
    public decimal? PAY_AMT { get; set; }
    public string? GATE_TYPE { get; set; }
    public int? GATE_NO { get; set; }
    public string? REF_TYPE { get; set; }
    public int? REF_NO { get; set; }
    public DateTime? REF_DATE { get; set; }
    public string? ISSUE_TYPE { get; set; }
    public int? ISSUE_NO { get; set; }
    public string? BUYER_ORDNO { get; set; }
    public string? PLACE_RECEIPT { get; set; }
    public string? PORT_LOADING { get; set; }
    public string? PORT_DISCHARGE { get; set; }
    public string? FINAL_DEST { get; set; }
    public string? FINAL_DEST_COUNTRY { get; set; }
    public string? DELIVERY_TERMS { get; set; }
    public string? LUT_DETAIL { get; set; }
    public string? INSU_DETAIL { get; set; }
    public string? FAPROV_STATUS { get; set; }
    public string? FAPROV_REMARKS { get; set; }
    public int? COND_DATE { get; set; }
    public int? COND_MNTH { get; set; }
    public int? APPROVAL_USER { get; set; }
    public string? IRN { get; set; }
    public string? SIGNED_JSON { get; set; }
    public string? SIGNED_QR { get; set; }
    public int? EINVOICE_FLG { get; set; }
    public int? EWAYBILL_FLG { get; set; }
    public string? EWAYBILL_NO { get; set; }
    public string? EWAYBILL_JSON { get; set; }
    public string? EWAYBILL_DATE { get; set; }
    public string? EWAYBILL_VALIDDATE { get; set; }
    public decimal? CDISC_AMT { get; set; }
    public decimal? CDISC_PER { get; set; }
    public string? SUPPLY_TYPE { get; set; }
    public string? LC_NO { get; set; }
    public string? TRADE_TERM { get; set; }
    public string? DISP_PLACE { get; set; }
    public string? SHIPMENT_TYPE { get; set; }
    public string? MODEOF_PAYMENT { get; set; }
    public string? INCOTERM { get; set; }
    public decimal? FRT_TAXPER { get; set; }
    public decimal? FRT_TAXAMT { get; set; }
    public string? SB_NO { get; set; }
    public DateTime? SB_DATE { get; set; }
    public string? PORT_CODE { get; set; }
    public string? TRAN_TYPE { get; set; }
    public DateTime? DEL_DATE { get; set; }
    public decimal? FOB_VALUE { get; set; }
    public decimal? FOB_FRT { get; set; }
    public decimal? FOB_INSU { get; set; }
    public decimal? FOB_OTHER { get; set; }
    public string? BILLOF_LADING { get; set; }
    public string? EXPV_TYPE { get; set; }
    public int? EXPV_NO { get; set; }
    public string? SHIP_TYPE { get; set; }
    public string? CURRENCY { get; set; }
    public int? BANK_CODE { get; set; }
    public string? DEL_SCH { get; set; }
    public string? LUT_NO { get; set; }
    public DateTime? LUT_DATE { get; set; }
    public int? PAY_TERM { get; set; }
    public int? SOLD_BY { get; set; }
    public int? INV_STATUS { get; set; }
    public int? INSUCR_DAYS { get; set; }
    public string? CONTAINER_SIZE { get; set; }
    public string? PAYMENT_TERM { get; set; }
    public string? LICENCE_NO { get; set; }
    public string? LUL_BILLNO { get; set; }
    public string? LUL_BILLDT { get; set; }
    public DateTime? LUL_PASSDT { get; set; }
    public string? LUL_CHQ { get; set; }
    public string? LUL_REMARK { get; set; }
    public string? LICENCE_TYPE { get; set; }
    public DateTime? LICENCE_DATE { get; set; }

    public string ACTION { get; set; }
}
public class RowDataModel
{
    public int ITEM_CODE { get; set; }
    public string? ITEM_NAME { get; set; }
    public string? UNIT_NAME { get; set; }
    public int? UNIT_CODE { get; set; }
    public string? HSN_CODE { get; set; }
    public int? NOS { get; set; }
    public decimal? QTY { get; set; }
    public decimal? GROSS_QTY { get; set; }
    public decimal? GATE_QTY { get; set; }
    public decimal? RATE { get; set; }
    public decimal? FOR_RATE { get; set; }
    public decimal? AMOUNT { get; set; }
    public decimal? PACK_PER { get; set; }
    public decimal? PACK_AMT { get; set; }
    public decimal? DISC_PER { get; set; }
    public decimal? DISC_AMT { get; set; }
    public int? TAX_CODE { get; set; }
    public decimal? CGST_PER { get; set; }
    public decimal? CGST_AMT { get; set; }
    public decimal? SGST_PER { get; set; }
    public decimal? SGST_AMT { get; set; }
    public decimal? IGST_PER { get; set; }
    public decimal? IGST_AMT { get; set; }
    public decimal? CESS_PER { get; set; }
    public decimal? CESS_AMT { get; set; }
    public decimal? LAND_RATE { get; set; }
    public decimal? LAND_AMT { get; set; }
    public string? REMARK { get; set; }
    public string? PACK_TYPE { get; set; }
    public int? PACK_NO { get; set; }
    public string? ORD_TYPE { get; set; }
    public int? ORD_NO { get; set; }
    public decimal? ORD_RATE { get; set; }
    public string? SAUDA_TYPE { get; set; }
    public int? SAUDA_NO { get; set; }
    public decimal? SAUDA_RATE { get; set; }
    public string? LOT_No { get; set; }
    public int? DEPT_CODE { get; set; }
    public string? DCN_TYPE { get; set; }
    public int? DCN_NO { get; set; }
    public string? FINAL_LOCK { get; set; }
    public int? STATUS { get; set; }
    public decimal? CDISC_AMT { get; set; }
    public decimal? INSU_AMT { get; set; }
    public decimal? FRT_AMT { get; set; }
    public decimal? WBQTY { get; set; }
    public decimal? FEXCH_USD { get; set; }
    public string? ROW_ID { get; set; }
    public decimal? GATE_INQTY { get; set; }
    public string? MIS_GROUP { get; set; }
    public decimal? FREIGHT_AMT { get; set; }
    public string? PROD_DESC { get; set; }
    public string? MIS_GRP { get; set; }
}
public class SalesReturnModel
{
    public string VoucherNo { get; set; }
    public string vType { get; set; }
}

public class SaudaItemDetail
{
    public int ITEM_CODE { get; set; }
    public decimal SIZE_DIFF { get; set; }
    public decimal COLOR_DIFF { get; set; }
    public decimal GRAM_DIFF { get; set; }
    public decimal ITEM_DIFF { get; set; }
    public string ORD_TYPE { get; set; }
    public string ORD_NO { get; set; }
    public decimal ORD_RATE { get; set; }
}

public class ValidateDataRequest
{
    public string? GateDocId { get; set; }
    public string? DocumentType { get; set; }
    public int DocumentNo { get; set; }
    public int PartyCode { get; set; }
    public string? ReferenceDocId { get; set; }
    //public int ReferenceNo { get; set; }
    public string? PackingDocId { get; set; }
    public string? ProductType { get; set; }
    public string? Transport { get; set; }
    public string? GrNo { get; set; }
    public DateTime InvoiceDate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal GrossWeight { get; set; }
    public string? TruckNo { get; set; }
    public string? WbNo { get; set; }
    public List<ValidateItem> Items { get; set; } = new();
}

public class ValidateItem
{
    public int ItemCode { get; set; }
    public string? ItemName { get; set; }
    public decimal Quantity { get; set; }
}

public class SalesReturnListModel
{
    public int? V_NO { get; set; }
    public string? V_TYPE { get; set; }
    public DateTime? V_DATE { get; set; }

    public string? BILL_NAME { get; set; }
    public string? BILL_ADD1 { get; set; }
    public string? BILL_ADD2 { get; set; }
    public string? BILL_ADD3 { get; set; }
    public string? BILL_CITYNAME { get; set; }

    public string? AGENT_NAME { get; set; }

    public string? SHIP_NAME { get; set; }
    public string? SHIP_ADD1 { get; set; }
    public string? SHIP_ADD2 { get; set; }
    public string? SHIP_ADD3 { get; set; }
    public string? SHIP_CITYNAME { get; set; }

    public string? TAX_NAME { get; set; }
    public string? ITEM_TYPE { get; set; }

    public int? WB_NO { get; set; }
    public int? PACK_NO { get; set; }

    public decimal? AMOUNT { get; set; }
    public decimal? PACK_PER { get; set; }
    public decimal? PACK_AMT { get; set; }

    public decimal? NAMOUNT { get; set; }

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

    public decimal? WB_AMT { get; set; }
    public string? WB_AC { get; set; }

    public int? TOT_NOS { get; set; }
    public decimal? FRT_AMT { get; set; }
    public decimal? ROUND_OFF { get; set; }
    public decimal? TOT_NET { get; set; }

    public decimal? INSU_PER { get; set; }
    public decimal? INSU_AMT { get; set; }

    public decimal? TDS_PER { get; set; }
    public decimal? TDS_AMT { get; set; }

    public string? LOAD_REM { get; set; }
    public string? WB_REM { get; set; }

    public decimal? TOT_GROSS { get; set; }

    public string? GR_NO { get; set; }
    public DateTime? GR_DATE { get; set; }
    public string? VEHICLE_NO { get; set; }

    public int? TRANSPORT_CODE { get; set; }
    public string? TRANSPORT_NAME { get; set; }

    public string? DRIVER_NAME { get; set; }
    public string? DRIVER_NO { get; set; }

    public string? WAYBILL_NO { get; set; }
    public string? REMARK { get; set; }

    public decimal? FRT_TOPAY { get; set; }
    public decimal? WB_QTY { get; set; }

    public decimal? DISC_PER { get; set; }
    public decimal? DISC_AMT { get; set; }
}
public class SalesEditStatus
{
    public bool IsApproved { get; set; }
    public bool IsApprovalInProcess { get; set; }
    public string? ApprovalUser { get; set; }
    public bool IsFinalApprovalBody { get; set; }
    public string? EAFlag { get; set; }
    public string? FAProvStatus { get; set; }
    public bool CanEdit { get; set; } = true;
    public bool EInvoiceWarning { get; set; }
    public string? EInvoiceMessage { get; set; }
    public string? Message { get; set; }
    public string? MessageType { get; set; }
    public int Status { get; set; }
}

public class SalesDeleteStatus
{
    public bool CanDelete { get; set; } = true;
    public bool GateExists { get; set; }
    public string? GateMessage { get; set; }
    public bool LedgerExists { get; set; }
    public string? LedgerMessage { get; set; }
    public string? Message { get; set; }
}
public class SaveTransportRequest
{
    public string VType { get; set; }
    public int VNo { get; set; }

    public decimal FrtAmt { get; set; }
    public decimal TdsPer { get; set; }
    public decimal TdsAmt { get; set; }
    public decimal FrtToPay { get; set; }

    public decimal LoadPer { get; set; }
    public decimal LoadAmt { get; set; }
    public int LoadAc { get; set; }
    public string LoadRem { get; set; }

    public decimal WbAmt { get; set; }
    public int WbAc { get; set; }

    public string GrNo { get; set; }
    public DateTime? GrDate { get; set; }

    public int TransportCode { get; set; }
    public string TransportName { get; set; }
    public string VehicleNo { get; set; }
    public string DriverName { get; set; }
    public string DriverNo { get; set; }
    public string Remark { get; set; }

    public decimal InsuPer { get; set; }
    public decimal InsuAmt { get; set; }
}