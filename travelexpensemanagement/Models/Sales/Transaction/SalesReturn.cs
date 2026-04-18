public class SalesReturn
{
    public FormDataModel FormData { get; set; }
    public List<RowDataModel> RowData { get; set; }
}

public class FormDataModel
{
    public string? DocumentType { get; set; }
    public string? DocumentNo { get; set; }
    public string? DocumentDate { get; set; }
    public string? partyCode { get; set; }
    public string? PartyName { get; set; }
    public string? AddressL1 { get; set; }
    public string? AddressL2 { get; set; }
    public string? AddressL3 { get; set; }
    public string? Station { get; set; }
    public string? Pincode { get; set; }
    public string? TransactionThrough { get; set; }
    public string? SupplyType { get; set; }
    public string? SaleThrough { get; set; }
    public string? Consignee { get; set; }
    public string? consigneeName { get; set; }
    public string? TransactionAddressL1 { get; set; }
    public string? TransactionAddressL2 { get; set; }
    public string? TransactionAddressL3 { get; set; }
    public string? TransactionStation { get; set; }
    public string? TransactionPIN { get; set; }
    public string? FormType { get; set; }
    public string? TaxType { get; set; }
    public string? ProductionType { get; set; }
    public string? ReferenceNo { get; set; }
    public string? ReferenceDate { get; set; }
    public string? GateNo { get; set; }
    public string? WbNo { get; set; }
    public string? PackNo { get; set; }
    public string? SaudaNo { get; set; }
    public string? SaudaRate { get; set; }
    public bool? Pcs { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? ACTION { get; set; }
}

public class RowDataModel
{
    public string? Code { get; set; }
    public string? Id { get; set; }
    public string? ProductName { get; set; }
    public int? Nos { get; set; }
    public decimal? GrossQuantity { get; set; }
    public decimal? NetQuantity { get; set; }
    public decimal? Rate { get; set; }
    public decimal? Amount { get; set; }
    public decimal? PackPercent { get; set; }
    public decimal? PackAmount { get; set; }
    public decimal? DiscPercent { get; set; }
    public decimal? DiscAmount { get; set; }
    public string? TaxTypes { get; set; }
    public decimal? CgstPer { get; set; }
    public decimal? CgstAmt { get; set; }
    public decimal? SgstPer { get; set; }
    public decimal? SgstAmt { get; set; }
    public decimal? IgstPer { get; set; }
    public decimal? IgstAmt { get; set; }
    public decimal? CessPer { get; set; }
    public decimal? CessAmt { get; set; }
    public string? Remark { get; set; }
    public string? PackNo { get; set; }
    public string? LotNo { get; set; }
    public string? SaudaType { get; set; }
    public string? SaudaNo { get; set; }
    public decimal? SaudaRate { get; set; }
    public string? OrderType { get; set; }
    public string? OrderNo { get; set; }
    public decimal? OrderRate { get; set; }
    public string? DcnType { get; set; }
    public string? DcnNo { get; set; }
    public string? Hsn { get; set; }
}
public class SalesReturnModel
{
    public string VoucherNo { get; set; }
    public string vType { get; set; }
}