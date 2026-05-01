namespace travelexpensemanagement.Models.GateEntry
{
    public class TransportInwardModel
    {
        public string V_TYPE { get; set; }
        public int V_NO { get; set; }
        public string DOC_ID { get; set; }
        public string TRF_TYPE { get; set; }
        public int? TRF_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string V_TIME { get; set; }
        public string ITEM_TYPE { get; set; }
        public int? PARTY_CODE { get; set; }
        public string ADD1 { get; set; }
        public string ADD2 { get; set; }
        public string ADD3 { get; set; }
        public int? PARTY_CITY { get; set; }
        public string PARTY_GST { get; set; }
        public string PARTY_PINCODE { get; set; }
        public int? PARTY_ADDRESSID { get; set; }
        public string BILL_NO { get; set; }
        public DateTime? BILL_DATE { get; set; }
        public string CHALL_NO { get; set; }
        public DateTime? CHALL_DATE { get; set; }
        public string TRUCK_NO { get; set; }
        public int? TRANSPORT_CODE { get; set; }
        public string DRIVER_NAME { get; set; }
        public string DRIVER_NO { get; set; }
        public int? TRANSIT_NO { get; set; }
        public string WAYBILL_NO { get; set; }
        public decimal? BILL_AMT { get; set; }
        public string REMARKS { get; set; }
        public int? DISP_PLAN_NO { get; set; }
        public string DISP_PLAN_TYPE { get; set; }
        public string WB_TYPE { get; set; }
        public int? WB_NO { get; set; }
        public string MRN_TYPE { get; set; }
        public int? MRN_NO { get; set; }
        public string REF_TYPE { get; set; }
        public int? REF_NO { get; set; }
        public string FAPROV_STATUS { get; set; }
        public string FAPROV_REMARKS { get; set; }
        public int? STATUS { get; set; }
        public int? ACTIVE { get; set; }
        public string Remarks2 { get; set; }
        public string PARTY_NAME { get; set; }
        public string RC_NO { get; set; }
        public string DL_NO { get; set; }
        public string INSU_NO { get; set; }
        public string PAN_NO { get; set; }
        public string PURPOSE { get; set; }
        public string IMAGEPATH { get; set; }
        public string R_TIME { get; set; }
        public string OUT_TIME { get; set; }
        public DateTime? R_DATE { get; set; }
        public DateTime? OUT_DATE { get; set; }
        public string RETURN_TYPE { get; set; }
        public string QRCODE_NO { get; set; }
        public string INOUT_ACTIVE { get; set; }
        public string OUT_ALLOWED { get; set; }
        public int? OUT_ALLOWEDBY { get; set; }
        public DateTime? RETURN_DATE { get; set; }
        public string RESPONSIBLE_PERSON { get; set; }
        public DateTime? INSU_EXPDT { get; set; }
        public DateTime? DL_EXPDT { get; set; }
        public string CONTAINER_NO { get; set; }
        public string CONTAINER_SIZE { get; set; }
        public int? SHIP_PARTY { get; set; }
        public string SHIP_BILLNO { get; set; }
        public DateTime? SHIP_BILLDATE { get; set; }
        public DateTime? EWB_DATE { get; set; }
        public DateTime? EWB_EXPDATE { get; set; }
        public DateTime? PARTY_WBTIME { get; set; }
        public string EWB_INVNO { get; set; }
        public decimal? EWB_INVAMT { get; set; }
        public string PARTY_WBSLIPNO { get; set; }
        public decimal? PARTY_WBGRWT { get; set; }
        public decimal? PARTY_WBTRWT { get; set; }
        public int? PARTY_EWBCITY { get; set; }
        public string GR_NO { get; set; }
        public DateTime? GR_DATE { get; set; } 
        public string? SaveOrUpdate { get; set; }
        //==================Correction===============
        public IFormFile Attachment { get; set; }
    }
    public class RcRequest
    {
        public string? RcNumber { get; set; }
        public int? CompCode { get; set; }
        public int? BranchCode { get; set; }
        public int? YearCode { get; set; }
        public string? VType { get; set; }
        public int? VNo { get; set; }
        public string? ClientId { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string? OwnerName { get; set; }
        public string? FatherName { get; set; }
        public string? PresentAddress { get; set; }
        public string? PermanentAddress { get; set; }
        public string? MobileNumber { get; set; }
        //public string? VehicleCategory { get; set; }
        public string? vehicleCategory { get; set; }
        //public string? VehicleChasiNumber { get; set; }
        public string? vehicleChasiNumber { get; set; }
        public string? VehicleEngineNumber { get; set; }
        public string? MakerDescription { get; set; }
        public string? MakerModel { get; set; }
        //public string? BodyType { get; set; }
        public string? bodyType { get; set; }
        public string? FuelType { get; set; }
        public string? Color { get; set; }
        public string? NormsType { get; set; }
        //public DateTime? FitUpTo { get; set; }
        public DateTime? fitUpTo { get; set; }
        public string? Financer { get; set; }
        public bool? Financed { get; set; }
        public string? InsuranceCompany { get; set; }
        //public string? InsurancePolicyNumber { get; set; }
        public string? insurancePolicyNumber { get; set; }
        //public DateTime? InsuranceUpto { get; set; }
        public DateTime? insuranceUpto { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public string? ManufacturingDateFormatted { get; set; }
        public string? RegisteredAt { get; set; }
        public string? LatestBy { get; set; }
        public bool? LessInfo { get; set; }
        //public DateTime? TaxUpto { get; set; }
        public DateTime? taxUpto { get; set; }
        //public DateTime? TaxPaidUpto { get; set; }
        public string? TaxPaidUpto { get; set; }
        public string? CubicCapacity { get; set; }
        //public string? VehicleGrossWeight { get; set; }
        public decimal? vehicleGrossWeight { get; set; }
        public string? NoCylinders { get; set; }
        public string? SeatCapacity { get; set; }
        public string? SleeperCapacity { get; set; }
        public string? StandingCapacity { get; set; }
        public string? Wheelbase { get; set; }
        //public string? UnladenWeight { get; set; }
        public decimal? unladenWeight { get; set; }
        public string? VehicleCategoryDescription { get; set; }
        public string? PuccNumber { get; set; }
        //public DateTime? PuccUpto { get; set; }
        public DateTime? puccUpto { get; set; }
        public string? PermitNumber { get; set; }
        public DateTime? PermitIssueDate { get; set; }
        public DateTime? PermitValidFrom { get; set; }
        //public DateTime? PermitValidUpto { get; set; }
        public DateTime? permitValidUpto { get; set; }
        public string? PermitType { get; set; }
        public string? NationalPermitNumber { get; set; }
        public DateTime? NationalPermitUpto { get; set; }
        public string? NationalPermitIssuedBy { get; set; }
        public string? NonUseStatus { get; set; }
        public DateTime? NonUseFrom { get; set; }
        public DateTime? NonUseTo { get; set; }
        //public string? BlacklistStatus { get; set; }
        public string? blacklistStatus { get; set; }
        public string? NocDetails { get; set; }
        public string? OwnerNumber { get; set; }
        //public string? RcStatus { get; set; }
        public string? rcStatus { get; set; }
        public bool? MaskedName { get; set; }
        public string? ChallanDetails { get; set; }
        public int? UUser { get; set; }
        public DateTime? UDate { get; set; }
        public int? EUser { get; set; }
        public DateTime? EDate { get; set; }
        public string? Aed { get; set; }
        public string? Wsid { get; set; }
        public string? Lip { get; set; }
        public string? Lid { get; set; }
        public int? SrNo { get; set; }
    }
    public class vehicleInfoDb
    {
        public string? rcNumber { get; set; }
        public string? insuranceNumber { get; set; }
        public string? purpose { get; set; }
        public string? grossWt { get; set; }
        public string? bodyType { get; set; }
        public string? vehicleRemarks { get; set; }
        public DateTime? fitmentupto { get; set; }
        public DateTime? taxupto { get; set; }
        public DateTime? insuExp { get; set; }
        public int? transportCode { get; set; }
        public string? transportName { get; set; }
    }
}
public class DriverDetail
{
    public string? dLNo { get; set; }
    public string? pANNo { get; set; }
    public string? driverName { get; set; }
    public string? driverNo { get; set; }
}
public class TransportInwardListModel
{
    public string? docid { get; set; }
    public int? vno { get; set; }
    public int? dono { get; set; }
    public DateTime? vdate { get; set; }
    public string? vtime { get; set; }
    public string? partyname { get; set; }
    public string? truckno { get; set; }
    public string? transport { get; set; }
}
//public class VehicleInwardEntryDetailsModel
//{
//    public string? code { get; set; }
//    public string? uUser { get; set; }
//    public DateTime? udate { get; set; }
//    public string? euser { get; set; }
//    public DateTime? edate { get; set; }
//    public string? wsid { get; set; }
//    public string? lip { get; set; }
//    public string? lid { get; set; }
//}
