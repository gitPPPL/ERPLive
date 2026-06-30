namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class PurchaseOrder
    {    
        public int? VNo { get; set; }
        public string? VType { get; set; }
        public DateTime? VDate { get; set; }
        public string? DocId { get; set; }
        public int? PlaceCode { get; set; }
        public string? WbType { get; set; }
        public int? WbNo { get; set; }
        public int? PartyCode { get; set; }
        public int? ShipCode { get; set; }
        public int? ShipFrom { get; set; }
        public string? PriceType { get; set; }
        public string? PartyRef { get; set; }
        public string? ImportCurrency { get; set; }
        public decimal? ExRate { get; set; }
        public decimal? Nos { get; set; }
        public decimal? Qty { get; set; }
        public decimal? Amount { get; set; }
        public decimal? PackAmt { get; set; }
        public decimal? DiscAmt { get; set; }
        public decimal? CgstAmt { get; set; }
        public decimal? SgstAmt { get; set; }
        public decimal? IgstAmt { get; set; }
        public decimal? OthAmt { get; set; }
        public decimal? VatAmt { get; set; }
        public decimal? CessPer { get; set; }
        public decimal? CessAmt { get; set; }
        public decimal? TcsPer { get; set; }
        public decimal? TcsAmt { get; set; }
        public decimal? NetAmt { get; set; }
        public string? DeliveryTerm { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public DateTime? ValidityDate { get; set; }
        public string? TransportTerm { get; set; }
        public int? PaytermCode { get; set; }
        public string? PaymentTerm { get; set; }
        public string? PriceTerm { get; set; }
        public string? SaudaType { get; set; }
        public int? SaudaNo { get; set; }
        public string? DeliveryPeriod { get; set; }
        public string? DeliveryTo { get; set; }
        public string? Remarks { get; set; }
        public string? PoType { get; set; }
        public string? FAProvStatus { get; set; }
        public string? FAProvRemarks { get; set; }
        public int? MailSend { get; set; }
        public decimal? CDiscAmt { get; set; }
        public int? AutoGenPo { get; set; }
        public string? PoAcceptFlg { get; set; }
        public string? PoAttachPath { get; set; }
        public DateTime? PoAttachDate { get; set; }
        public string? BillAdd1 { get; set; }
        public string? BillAdd2 { get; set; }
        public string? BillAdd3 { get; set; }
        public int? BillCity { get; set; }
        public string? BillPincode { get; set; }
        public string? BillGst { get; set; }
        public string? ShipAdd1 { get; set; }
        public string? ShipAdd2 { get; set; }
        public string? ShipAdd3 { get; set; }
        public int? ShipCity { get; set; }
        public string? ShipPincode { get; set; }
        public string? ShipGst { get; set; }

        public int? TaxCode { get; set; }
        public string? ItemType { get; set; }
        public string? SupplyType { get; set; }
        public string? TranType { get; set; }
        public int? FormCode { get; set; }
        public string? VehicleNo { get; set; }
        public string? InvType { get; set; }
        public int? InvNo { get; set; }
        public string? PartyName { get; set; }
        public string? ShipName { get; set; }
        public int? Status { get; set; }
        public string ? SaveOrUpdate { get; set; }
        public List<Order2> ItemRecords { get; set; } 
        public List<PurchaseAttachment> Attachments { get; set; } 

    }    

    public class Order2
    {       
        public int? SNO { get; set; }
        public int? PlaceCode { get; set; }
        public string? ItemName { get; set; }
        public int? ItemCode { get; set; }
        public int? MakeCode { get; set; }
        public int? NOS { get; set; }
        public decimal? Qty { get; set; }
        public decimal? AdjQty { get; set; }
        public decimal? GateQty { get; set; }
        public string? UomName { get; set; }
        public int? UomCode { get; set; }
        public decimal? Rate { get; set; }
        public decimal? ImportRate { get; set; }
        public decimal? CalcRate { get; set; }
        public decimal? Amount { get; set; }
        public decimal? PackPer { get; set; }
        public decimal? PackAmt { get; set; }
        public decimal? DiscPer { get; set; }
        public decimal? DiscAmt { get; set; }
        public int? TaxCode { get; set; }
        public decimal? CgstPer { get; set; }
        public decimal? CgstAmt { get; set; }
        public decimal? SgstPer { get; set; }
        public decimal? SgstAmt { get; set; }
        public decimal? IgstPer { get; set; }
        public decimal? IgstAmt { get; set; }
        public decimal? VatPer { get; set; }
        public decimal? VatAmt { get; set; }
        public decimal? CessPer { get; set; }
        public decimal? CessAmt { get; set; }           
        public decimal? OthAmt { get; set; } 
        public decimal? NetAmt { get; set; }
        public decimal? LandRate { get; set; }
        public int? Status { get; set; }
        public string? PlaceUse { get; set; }
        public string? DeptName { get; set; }
        public string? Remarks { get; set; }
        public int? PreorityLevel { get; set; }
        public string? PreorityRemarks { get; set; }
        public decimal? RateMonthly { get; set; }
        public decimal? RateQuarterly { get; set; }
        public decimal? RateAnnualy { get; set; }
        public decimal? RateSpecial { get; set; }
        public string? RequestType { get; set; }
        public int? RequestNo { get; set; }
        public string? ApprovalType { get; set; }
        public int? ApprovalNo { get; set; }
        public int? DeptCode { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? SaudaType { get; set; }
        public int? SaudaNo { get; set; }
        public string? DispThrough { get; set; }
        public string? DispRef { get; set; }
        public string? DispRemarks { get; set; }
        public int? TenacityGrpCode { get; set; }
        public string? TenacityType { get; set; }
        public int? TenacityCode { get; set; }
        public string? TenacityName { get; set; }
        public string? FAProvStatus { get; set; }
        public string? FAProvRemarks { get; set; }
        public int? ColorCode { get; set; }
        public int? GramCode { get; set; }
        
    }

    public class PurchaseAttachment
    {
        public string? FileName { get; set; }      
        public long? FileSize { get; set; }    
        public string? FileType { get; set; }     
        public string? FilePath { get; set; }    
        public string? FileContentBase64 { get; set; }
    }

    public class SaudaCalculationRequest
    {
        public string? Btn { get; set; }
        public int? SaudaNo { get; set; }
        public string? SaudaType { get; set; }
        public DateTime? EffectiveDate { get; set; } // Add this if needed
        public int? StateCode { get; set; }
        public int? CityCode { get; set; }        
        public List<Order2> Orders { get; set; }  // Use Order2
    }



}
