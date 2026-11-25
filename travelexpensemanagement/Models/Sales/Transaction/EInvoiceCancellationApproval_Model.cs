public class EInvoiceCancellationApproval_Model
{
    public List<EInvoiceCancellationApproval_Detail> Details { get; set; } = new();
}

public class EInvoiceCancellationApproval_Detail
{
    public string? V_TYPE { get; set; } = string.Empty;
    public int? V_NO { get; set; }
    public string? DOC_ID { get; set; } = string.Empty;
    public DateTime? V_DATE { get; set; }
    public string? REF_TYPE { get; set; }
    public int? REF_NO { get; set; }
    public DateTime? REF_DATE { get; set; }
    public string? INV_STATUS { get; set; }
    public string? EINV_STATUS { get; set; }
    public DateTime? EINV_DATE { get; set; }
    public string? EWB_STATUS { get; set; }
    public DateTime? EWB_DATE { get; set; }
    public decimal? BILL_AMT { get; set; }
    public int? BILL_CODE { get; set; }
    public string? BILL_NAME { get; set; }
    public string? BILL_GST { get; set; }
    public string? BILL_PINCODE { get; set; }
    public int? SHIP_CODE { get; set; }
    public string? SHIP_NAME { get; set; }
    public string? SHIP_GST { get; set; }
    public string? SHIP_PINCODE { get; set; }
    public string? BILL_TYPE { get; set; }
    public string? REMARKS { get; set; }
    public string? IRN { get; set; }
    public string? STATUS { get; set; }
    public string? REASON { get; set; }
    public string? FAPROV_STATUS { get; set; }
    public string? FAPROV_REMARKS { get; set; }
    public string? action { get; set; }
    public int? SNO { get; set; }
}