namespace travelexpensemanagement.Models
{
    public class VehicleGatePassModel
    {
        public string GateNo { get; set; }
        public DateTime GateDate { get; set; }
        public string WbNo { get; set; }
        public DateTime WbDate { get; set; }
        public string WbType { get; set; }
        public string PartyName { get; set; }
        public string VehicleNo { get; set; }
        public string BillNo { get; set; }
        public decimal? WbQty { get; set; }
        public string FinalRemarks { get; set; }
        public string OutAllowed { get; set; }

        public string? Wbslip { get; set; }     
        public DateTime? GatOut { get; set; }  




    }

}
