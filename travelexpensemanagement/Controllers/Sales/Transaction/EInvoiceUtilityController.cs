using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class EInvoiceUtilityController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly IEInvoiceUtilityRepository _eInvoiceUtilityRepository;
        public EInvoiceUtilityController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DbHelper dbHelper, IEInvoiceUtilityRepository eInvoiceUtilityRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _eInvoiceUtilityRepository = eInvoiceUtilityRepository;
        }
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/EInvoiceUtility/Index.cshtml");
        }

        public async Task<IActionResult> GetDropdown(string type, string data = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = "";

            switch (type.ToLower())
            {
                case "doctype":
                    qry = $@"Select code as value, Name as text from DOCTYPE_MAST where code in ('SAGT','SASI','SAST','SART','DNSL','CRSL','ESAG','SATD') order by Name";
                    break;
                default:
                    return Json(new { success = false, message = "Invalid dropdown type." });
            }

            var result = await _dbHelper.GetJsonDataAsync(qry);
            return Json(result);
        }

        //==========================Get EInvoice List==========================
        [HttpGet]
        public IActionResult GetEInvoiceList(string vType)
        {
            var res = _eInvoiceUtilityRepository.GetEInvoiceList(vType);
            return Json(new { status = res.status, message = res.message, data = res.data });
        }

        //==========================Get EInvoice Status==========================
        [HttpGet]
        public async Task<IActionResult> GetInvoiceStatus(string vType, long vNo)
        {
            var res = await _eInvoiceUtilityRepository.GetInvoiceStatus(vType, vNo);
            return Json(new { status = res.status, message = res.message, data = res.data });
        }

        //==========================Get Signed JSON==========================
        [HttpGet]
        public async Task<IActionResult> GetSignedJson(string vType, long vNo)
        {
            var res = await _eInvoiceUtilityRepository.GetSignedJson(vType, vNo);
            return Json(new { status = res.status, message = res.message, data = res.data });
        }

        //==========================Get EWayBill No==========================
        [HttpGet]
        public async Task<IActionResult> GetEWayBillNo(string vType, long vNo)
        {
            var res = await _eInvoiceUtilityRepository.GetEWayBillNo(vType, vNo);
            return Json(new { status = res.status, message = res.message, data = res.data });
        }

        //==========================Get IRN==========================
        [HttpGet]
        public async Task<IActionResult> GetIRN(string vType, long vNo)
        {
            var res = await _eInvoiceUtilityRepository.GetIRN(vType, vNo);
            return Json(new { status = res.status, message = res.message, data = res.data });
        }

        //=============================API Credentials========================
        [HttpGet]
        public async Task<IActionResult> GetApiMode()
        {
            var res = await _eInvoiceUtilityRepository.GetApiMode();

            if (!res.status || res.data == null)
                return Json(new { success = false, message = res.message });

            var gv = _globalVariableService.GetGlobalVariables();
            var gs = await _globalVariableService.LoadGeneralSetting();
            var datasource = _dbConnection.GetErpConnection();

            return Json(new
            {
                success = true,
                isLive = res.data.isLive,
                gstin = res.data.gstin,
                compcode = gv.PubCompCode,
                datasource = datasource.DataSource,
                username = gs.PubEinvUName
            });
        }

        //===========================E-Invoice Button Click===========================
        [HttpPost]
        public async Task<IActionResult> EInvoice(int vNo, string vType, bool cessNAValue)
        {
            var res = await _eInvoiceUtilityRepository.EInvoice(vNo, vType, cessNAValue);

            return Json(new
            {
                success = res.status,
                message = res.message,
                irn = res.irn,
                requestJson = res.requestJson,
                signedJson = res.signedJson,
                irnGenerated = res.irnGenerated,
                einvoiceGenerated = res.einvoiceGenerated,
                authenticated = res.authenticated,
                qrGenerated = res.qrGenerated
            });
        }

        //===========================Generate IRN Button Click===========================
        [HttpPost]
        public async Task<IActionResult> GenerateIRNFromJson(int vNo, string vType, string jsonText)
        {
            var res = await _eInvoiceUtilityRepository.GenerateIRNFromJson(vNo, vType, jsonText);

            return Json(new
            {
                success = res.Success,
                message = res.Message,
                irn = res.Irn,
                signedJson = res.SignedJson,
                irnGenerated = res.IrnGenerated,
                einvoiceGenerated = res.EinvoiceGenerated,
                authenticated = res.Authenticated,
                qrGenerated = res.QrGenerated
            });
        }

        //===========================Create JSON only===========================
        [HttpPost]
        public async Task<IActionResult> CreateJsonOnly(int vNo, string vType, bool cessNAValue)
        {
            var res = await _eInvoiceUtilityRepository.CreateJsonOnly(vNo, vType, cessNAValue);

            return Json(new { success = res.status, message = res.message, requestJson = res.data });
        }

        //===========================Create QR from Signed JSON===========================
        [HttpPost]
        public async Task<IActionResult> CreateQrFromSignedJson(int vNo, string vType, string signedJson)
        {
            var res = await _eInvoiceUtilityRepository.CreateQrFromSignedJson(vNo, vType, signedJson);

            return Json(new { success = res.status, message = res.message, irn = res.data });
        }

        //===========================Create EWB JSON button===========================
        [HttpPost]
        public async Task<IActionResult> CreateEwbJsonOnly(int vNo, string vType)
        {
            var res = await _eInvoiceUtilityRepository.CreateEwbJsonOnly(vNo, vType);

            return Json(new { success = res.status, message = res.message, requestJson = res.data });
        }

        //===========================Generate EWayBill button===========================
        [HttpPost]
        public async Task<IActionResult> EWayBill(int vNo, string vType, bool useEwbJson, string jsonText, bool cbEwayBill, bool takeConfirmation = false)
        {
            var res = await _eInvoiceUtilityRepository.EWayBill(vNo, vType, useEwbJson, jsonText, cbEwayBill, takeConfirmation);
            var d = res.data;

            return Json(new
            {
                success = res.status,
                message = res.message,
                continueEwaybillQuestion = d?.continueEwaybillQuestion ?? false,
                ewbGenerated = d?.ewbGenerated ?? false,
                ewbNo = d?.ewbNo,
                ewbDate = d?.ewbDate,
                validUpto = d?.validUpto,
                requestJson = d?.requestJson,
                signedJson = d?.signedJson
            });
        }

        //===========================Cancel IRN===========================
        [HttpPost]
        public async Task<IActionResult> CancelIRN(int vNo, string vType, string irn, bool confirmed)
        {
            var res = await _eInvoiceUtilityRepository.CancelIRN(vNo, vType, irn, confirmed);

            return Json(new
            {
                success = res.status,
                message = res.message,
                needConfirm = res.data?.needConfirm ?? false,
                irnCancelled = res.data?.irnCancelled ?? false
            });
        }

        //===========================Cancel E-Waybill===========================
        [HttpPost]
        public async Task<IActionResult> CancelEwayBill(int vNo, string vType, string ewbNo)
        {
            var res = await _eInvoiceUtilityRepository.CancelEwayBill(vNo, vType, ewbNo);

            return Json(new
            {
                success = res.status,
                message = res.message,
                ewbCancelled = res.data?.ewbCancelled ?? false
            });
        }

        //===========================Authenticate button===========================
        [HttpPost]
        public async Task<IActionResult> Authenticate()
        {
            var res = await _eInvoiceUtilityRepository.Authenticate();

            return Json(new { success = res.status, message = res.message });
        }
    }
}
