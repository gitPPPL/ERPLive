namespace travelexpensemanagement.Models.Sales.Transaction
{

    public class TransportRateRequestImportModel
    {
        public int PartyCode { get; set; }
        public int CityCode { get; set; }
        public string? DeliveryLocation { get; set; }
        public string? SaleGroup { get; set; }
        public string? PinCode { get; set; }
        public string? PartyName { get; set; }
        public decimal TotalQty { get; set; }
        public int? RefNo { get; set; }
    }

    public class TransportRateRequestForQuotationModel
    {
        public string VType { get; set; } = "";
        public int VNo { get; set; }
        public DateTime VDate { get; set; }
        public string? RefType { get; set; }
        public int? RefNo { get; set; }
        public string TransportName { get; set; } = "[]";   // JSON: [{Code, Name}]
        public List<TransportRateRequestRowModel> Rows { get; set; } = new();
        public bool IsEdit { get; set; }
    }

    public class TransportRateRequestRowModel
    {
        public decimal Weight { get; set; }
        public string? ItemType { get; set; }
        public int DelCityCode { get; set; }
        public string? VehicleType { get; set; }
        public string? DeliveryPincode { get; set; }
        public string? Remarks { get; set; }
        public int? PartyCode { get; set; }
        public string? RefType { get; set; }
        public int? RefNo { get; set; }
    }

    public class TransportRateRequestForQuotationListModel
    {
        public string? VType { get; set; } = "";
        public int? VNo { get; set; }
        public DateTime? VDate { get; set; }
    }

    public class TransportEmailRequestModel
    {
        public string VType { get; set; } = "";
        public int VNo { get; set; }
        public bool SendToTransporter { get; set; }
        public string EmailId { get; set; } = "";
        public List<int> TransporterCodes { get; set; } = new();
    }

    public class TransportWhatsAppRequestModel
    {
        public string? VType { get; set; }
        public int VNo { get; set; }
        public bool SendToTransporter { get; set; }
        public string? MobileNo { get; set; }
        public List<int> TransporterCodes { get; set; } = new();
    }
}
