using Microsoft.AspNetCore.Http;

namespace travelexpensemanagement.Models.Sales.Transaction
{
    public class EInvoiceUtilityModel
    {
        public string VType { get; set; }
        public long VNo { get; set; }
        public string VDate { get; set; }            // dd/MM/yyyy
        public decimal NetAmount { get; set; }
        public string ApprovalStatus { get; set; }
        public string BillName { get; set; }
        public string BillGst { get; set; }
        public string BillPincode { get; set; }
        public string ShipName { get; set; }
        public string ShipGst { get; set; }
        public string ShipPincode { get; set; }
        public string Irn { get; set; }
        public string EInvoiceStatus { get; set; }   // "Generated" or ""
        public string EwbStatus { get; set; }        // "Generated" or ""
        public string CancelStatus { get; set; }     // "OK" / "Cancelled" / ""
        public string TranType { get; set; }
    }

    public class EInvoiceStatusModel
    {
        public string ApprovalStatus { get; set; } = "";
        public bool IsCancelled { get; set; }        // STATUS = 2
        public bool IsEInvoiceGenerated { get; set; } // IRN <> ''
        public bool IsEwbGenerated { get; set; }      // EWAYBILL_NO <> ''
        public string Irn { get; set; } = "";
        public string EwaybillNo { get; set; } = "";
    }

    public class Creds
    {
        public bool IsLive { get; set; }

        public string Ip { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClientId { get; set; }
        public string ClientSid { get; set; }
        public string Gstin { get; set; }

        public string SellerGSTIN { get; set; }
        public string SellerPIN { get; set; }
        public string SellerSTCD { get; set; }
        public string SellerLoc { get; set; }
        public string SellerMob { get; set; }
    }

    public class MasterGstAuthResponse
    {
        public MasterGstAuthData data { get; set; }
        public string irp { get; set; }
        public string status_cd { get; set; }
        public string status_desc { get; set; }
    }

    public class MasterGstAuthData
    {
        public string UserName { get; set; }
        public string TokenExpiry { get; set; }
        public string Sek { get; set; }
        public string ClientId { get; set; }
        public string AuthToken { get; set; }
    }

    public class IrnResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Irn { get; set; } = "";
        public string SignedQr { get; set; } = "";
        public string SignedJson { get; set; } = "";
        public bool IrnGenerated { get; set; }
        public bool EinvoiceGenerated { get; set; }
        public bool Authenticated { get; set; }
        public bool QrGenerated { get; set; }
    }

    public class ApiModeModel
    {
        public bool isLive { get; set; }
        public string? gstin { get; set; }
    }

    public class EInvoiceResultModel
    {
        public bool status { get; set; } = false;
        public string? message { get; set; }
        public string? irn { get; set; }
        public string? requestJson { get; set; }
        public string? signedJson { get; set; }
        public bool irnGenerated { get; set; } = false;
        public bool einvoiceGenerated { get; set; } = false;
        public bool authenticated { get; set; } = false;
        public bool qrGenerated { get; set; } = false;
    }
    public class EwayBillResultModel
    {
        public bool success { get; set; } = false;
        public string? message { get; set; }
        public string? ewbNo { get; set; }
        public string? ewbDate { get; set; }
        public string? validUpto { get; set; }
        public string? requestJson { get; set; }
        public string? signedJson { get; set; }
        public bool ewbGenerated { get; set; }
        public bool continueEwaybillQuestion { get; set; }
    }
    public class EInvoiceJsonResult
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public string? jsonText { get; set; }
    }
    public class MasterGstAuthResult
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public string? response { get; set; }
        public string? authToken { get; set; }
        public string? tokenExpiry { get; set; }
        public string? sek { get; set; }
        public string? clientId { get; set; }
    }

    public class CancelResultModel
    {
        public bool needConfirm { get; set; }
        public bool irnCancelled { get; set; }
        public bool ewbCancelled { get; set; }
    }
}
