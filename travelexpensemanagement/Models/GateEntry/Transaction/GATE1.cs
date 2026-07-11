using travelexpensemanagement.Controllers.GateEntry.Transaction;

namespace travelexpensemanagement.Models.GateEntry.Transaction
{
    public class GATE1
    {
        public int YEAR_CODE { get; set; }
        public int COMP_CODE { get; set; }
        public int BRANCH_CODE { get; set; }
        public string V_TYPE { get; set; }
        public int V_NO { get; set; }
        public string DOC_ID { get; set; }
        public string TRF_TYPE { get; set; }
        public int TRF_NO { get; set; }
        public DateTime V_DATE { get; set; }
        public string V_TIME { get; set; }
        public string ITEM_TYPE { get; set; }
        public int PARTY_CODE { get; set; }
        public string ADD1 { get; set; }
        public string ADD2 { get; set; }
        public string ADD3 { get; set; }
        public int PARTY_CITY { get; set; }
        public string PARTY_GST { get; set; }
        public string PARTY_PINCODE { get; set; }
        public int PARTY_ADDRESSID { get; set; }
        public string BILL_NO { get; set; }
        public DateTime BILL_DATE { get; set; }
        public string CHALL_NO { get; set; }
        public DateTime CHALL_DATE { get; set; }
        public string TRUCK_NO { get; set; }
        public int TRANSPORT_CODE { get; set; }
        public string DRIVER_NAME { get; set; }
        public string DRIVER_NO { get; set; }
        public int TRANSIT_NO { get; set; }
        public string WAYBILL_NO { get; set; }
        public decimal BILL_AMT { get; set; }
        public string REMARKS { get; set; }
        public int DISP_PLAN_NO { get; set; }
        public string DISP_PLAN_TYPE { get; set; }
        public string WB_TYPE { get; set; }
        public int WB_NO { get; set; }
        public string MRN_TYPE { get; set; }
        public int MRN_NO { get; set; }
        public string REF_TYPE { get; set; }
        public int REF_NO { get; set; }
        public string FAPROV_STATUS { get; set; }
        public string FAPROV_REMARKS { get; set; }
        public int STATUS { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public int SRNO { get; set; }
        public int ACTIVE { get; set; }
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
        public DateTime R_DATE { get; set; }
        public DateTime OUT_DATE { get; set; }
        public string RETURN_TYPE { get; set; }
        public string QRCODE_NO { get; set; }
        public string INOUT_ACTIVE { get; set; }
        public string OUT_ALLOWED { get; set; }
        public int OUT_ALLOWEDBY { get; set; }
        public DateTime RETURN_DATE { get; set; }
        public string RESPONSIBLE_PERSON { get; set; }
        public DateTime INSU_EXPDT { get; set; }
        public DateTime DL_EXPDT { get; set; }
        public string CONTAINER_NO { get; set; }
        public string CONTAINER_SIZE { get; set; }
        public int SHIP_PARTY { get; set; }
        public string SHIP_BILLNO { get; set; }
        public DateTime SHIP_BILLDATE { get; set; }
        public DateTime EWB_DATE { get; set; }
        public DateTime EWB_EXPDATE { get; set; }
        public DateTime PARTY_WBTIME { get; set; }
        public string EWB_INVNO { get; set; }
        public decimal EWB_INVAMT { get; set; }
        public string PARTY_WBSLIPNO { get; set; }
        public decimal PARTY_WBGRWT { get; set; }
        public decimal PARTY_WBTRWT { get; set; }
        public int PARTY_EWBCITY { get; set; }
        public string GR_NO { get; set; }
        public DateTime GR_DATE { get; set; }

    }
    public class TruckOutRecord
    {
        public int V_NO { get; set; }
        public string OUT_DATE { get; set; }  
        public string OUT_TIME { get; set; }  
        public string OUT_ALLOWED { get; set; }  
        public string DOC_ID { get; set; }
        public string remarks { get; set; }
    }

    public class TruckOutSaveRequest
    {
        public List<TruckOutRecord> Records { get; set; }
    }

}
