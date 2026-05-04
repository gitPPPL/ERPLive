using System.ComponentModel.DataAnnotations;

namespace travelexpensemanagement.Models.GateEntry
{

    public class InwardEntryModel
    {
        public InwardEntry_Header Header { get; set; }
        public List<Details> Deatils { get; set; }
    }
    public class InwardEntry_Header
    {
        public string? action { get; set; }  
        public string? Add1 { get; set; }
        public string? Add2 { get; set; }
        public string? Add3 { get; set; }
        public int? ACTIVE { get; set; }
        public string? BILL_NO { get; set; }
        public DateTime? BILL_DATE { get; set; }
        public string? CHALL_NO { get; set; }
        public DateTime? CHALL_DATE { get; set; }
        public string? City { get; set; }
        public string? CONTAINER_NO { get; set; }
        public string? CONTAINER_SIZE { get; set; }
        public string? DL_NO { get; set; }
        public DateTime? DL_EXPDT { get; set; }
        public string? DOC_ID { get; set; }
        public string? DRIVER_NAME { get; set; }
        public string? DRIVER_NO { get; set; }
        public string? EWB_INVNO { get; set; }
        public decimal? EWB_INVAMT { get; set; }
        public DateTime? EWB_DATE { get; set; }
        public DateTime? EWB_EXPDATE { get; set; }
        public string? FAPROV_STATUS { get; set; }
        public DateTime? GR_DATE { get; set; }
        public string? GR_NO { get; set; }
        public string? INSU_NO { get; set; }
        public DateTime? INSU_EXPDT { get; set; }
        public string? OUT_TIME { get; set; }
        public DateTime? OUT_DATE { get; set; }
        public string? PARTY_GST { get; set; }
        public string? PARTY_NAME { get; set; }
        public string? PARTY_PINCODE { get; set; }
        public string? PARTY_WBSLIPNO { get; set; }
        public decimal? PARTY_WBGRWT { get; set; }
        public decimal? PARTY_WBTRWT { get; set; }
        public DateTime? PARTY_WBTIME { get; set; }
        public int? PARTY_ADDRESSID { get; set; }
        public int? PARTY_CITY { get; set; }
        public int? PARTY_CODE { get; set; }
        public int? PARTY_EWBCITY { get; set; }
        public string? RC_NO { get; set; }
        public string? R_TIME { get; set; }
        public DateTime? R_DATE { get; set; }
        public string? REMARKS { get; set; }
        public string? RETURN_TYPE { get; set; }
        public int? SHIP_PARTY { get; set; }
        public string? SHIP_BILLNO { get; set; }
        public DateTime? SHIP_BILLDATE { get; set; }
        public string? State { get; set; }
        public int?  STATUS { get; set; }
        public string? T_name { get; set; }
        public string? TRUCK_NO { get; set; }
        public int? TRANSIT_NO { get; set; }
        public int? TRANSPORT_CODE { get; set; }
        public string? V_TIME { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? V_NO { get; set; }
        public string? V_TYPE { get; set; }
        public int? DISP_PLAN_NO { get; set; }
        public string? DISP_PLAN_TYPE { get; set; }
        public decimal? BILL_AMT { get; set; }
        public string? WAYBILL_NO { get;  set; }
        public string? VtypeCode { get;  set; }
        public string? PAN_NO { get;  set; }
         public string? ShipAddress { get;  set; }
         public string? Remarks2 { get;  set; }




    }
    public class Details
    {
        public string? VtypeCode { get; set; }
        public int? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public int? DEPT_CODE { get; set; }
        public int? NOS { get; set; }
        public decimal?   QTY { get; set; }
        public int? UOM_CODE { get; set; }
        public string? UOM_NAME { get; set; }
        public string? EMPTY { get; set; }
        public string? REMARKS { get; set; }
        public string? REF_TYPE { get; set; }
        public int? REF_NO { get; set; }
        public string? MRN_TYPE { get; set; }
        public int? MRN_NO { get; set; }          
        public string? STATUS { get; set; }
        public decimal? ADJ_QTY { get; set; }
        public decimal? BALANCEQTY { get; set; }          
        public int? SRNO { get; set; }         
        public decimal? SHIP_RATE { get; set; }
        public string? Department { get; set; }
        public string? Unit { get; set; }

     }





}