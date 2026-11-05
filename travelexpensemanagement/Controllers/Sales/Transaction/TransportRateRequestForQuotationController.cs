using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    [SessionAuthorize]
    public class TransportRateRequestForQuotationController : Controller
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly ITransportRateRequestForQuotationRepository _repository;
        private readonly DataBaseConnection _dbConnection;
        public TransportRateRequestForQuotationController(GlobalVariableService globalVariableService, DbHelper dbHelper, GlobalValidationdate globalValidationdate,
            ITransportRateRequestForQuotationRepository repository, DataBaseConnection dbConnection)
        {
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _globalValidationdate = globalValidationdate;
            _repository = repository;
            _dbConnection = dbConnection;
        }

        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/TransportRateRequestForQuotation/Index.cshtml");
        }

        public async Task<IActionResult> GetDropdown(string type)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = "";

            switch (type.ToLower())
            {
                case "doctype":
                    qry = "Select Name as text, Code as value from DOCTYPE_MAST where DOCTYPE in ('TransportRequestEntry') order by Name";
                    break;

                case "refno":
                    qry = $@"Select DISTINCT V_NO as text, V_NO as value, V_date from DISPATCH_PLAN where comp_code = {gv.PubCompCode} and branch_code = {gv.PubBranchCode} and 
                            year_code = {gv.PubFYearCode} ORDER BY V_NO";
                    break;

                case "itemtype":
                    qry = $@"SELECT DISTINCT SALE_GROUP AS value, SALE_GROUP AS text FROM ITEM_GROUP WHERE COMP_CODE = {gv.PubCompCode} ORDER BY SALE_GROUP";
                    break;

                case "location":
                    qry = @"SELECT B.CODE AS value, B.NAME AS text FROM CITY_MAST B WHERE B.ACTIVE = 1 ORDER BY B.NAME";
                    break;

                case "vehicletype":
                    qry = @"SELECT V.VehicleType AS value, V.VehicleType AS text FROM (
                            SELECT 'Pickup' AS VehicleType
                            UNION ALL SELECT 'Mini Truck'
                            UNION ALL SELECT 'LCV (3–6 ton)'
                            UNION ALL SELECT 'MCV (7–11 ton)'
                            UNION ALL SELECT 'HCV (12+ ton)'
                            UNION ALL SELECT 'Trailer'
                            UNION ALL SELECT 'Container'
                            UNION ALL SELECT 'Tanker'
                            UNION ALL SELECT 'Tempo'
                        ) AS V
                        ORDER BY V.VehicleType";
                    break;

                case "transporter":
                    qry = $"SELECT CODE AS value, NAME AS text FROM TRANSPORT_MAST WHERE COMP_CODE = {gv.PubCompCode} AND SALE_GROUP = 'SALE' ORDER BY NAME";
                    break;

                default:
                    return Json(new { success = false, message = "Invalid dropdown type." });
            }

            var result = await _dbHelper.GetJsonDataAsync(qry);
            return Json(result);
        }

        // ==========V_NO=========
        [HttpGet]
        public JsonResult GetVNo(string vType)
        {
            var result = _globalValidationdate.GetVNo(vType, "RFQ1");
            return Json(new { status = true, V_NO = result });
        }

        // ==========IMPORT DISPATCH PLANNING=========
        [HttpGet]
        public async Task<IActionResult> ImportDispatch(int refNo)
        {
            if (refNo <= 0)
                return Json(new { status = false, message = "Please select Ref No." });

            var result = await _repository.ImportDispatch(refNo);
            return Json(result);
        }

        // ==========SAVE=========
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] TransportRateRequestForQuotationModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.VType) || model.VNo <= 0 || model.Rows == null || model.Rows.Count == 0)
                return Json(new { status = false, message = "Invalid data." });

            var result = await _repository.Save(model);
            return Json(new { status = result.status, message = result.message });
        }

        // ========== GET EXISTING RFQ ==========
        [HttpGet]
        public async Task<IActionResult> GetById(string vType, int vNo)
        {
            if (string.IsNullOrWhiteSpace(vType) || vNo <= 0)
                return Json(new { status = false, message = "Invalid document details." });

            var result = await _repository.GetById(vType, vNo);

            return Json(new { status = result.status, message = result.message, data = result.data });
        }

        [HttpGet]
        public async Task<JsonResult> getGlobalValues()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                var gs = await _globalVariableService.LoadGeneralSetting();
                using var erpCon = _dbConnection.GetErpConnection();

                string databaseName;
                using (var connection = _dbConnection.GetErpConnection())
                {
                    databaseName = connection.Database; // Get the database name
                }

                var response = new
                {
                    compCode = gv.PubCompCode,
                    yearCode = gv.PubFYearCode,
                    branchCode = gv.PubBranchCode,
                    add1 = gv.Address1,
                    add2 = gv.Address2,
                    companyName = gv.CompanyName,
                    db = databaseName,
                    phone = gv.Phone,
                    gst = gv.gstin,
                    pan = gv.PAN,
                    email = gv.Email,
                };

                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== MAIL ==========
        [HttpPost]
        public async Task<IActionResult> SendEmail([FromBody] TransportEmailRequestModel model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid request." });

            if (model.SendToTransporter && !model.TransporterCodes.Any())
                return Json(new { success = false, message = "Please select at least one transporter." });

            if (!model.SendToTransporter && string.IsNullOrWhiteSpace(model.EmailId))
                return Json(new { success = false, message = "Enter email to send." });

            if (!model.SendToTransporter)
            {
                try
                {
                    _ = new System.Net.Mail.MailAddress(model.EmailId);
                }
                catch
                {
                    return Json(new { success = false, message = "Please enter a valid email address." });
                }
            }

            var result = await _repository.SendTransportRequestEmail(model);

            return Json(new { success = result.status, message = result.message });
        }

        // ========== WHATSAPP ==========
        [HttpPost]
        public async Task<IActionResult> SendWhatsApp([FromBody] TransportWhatsAppRequestModel model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid request." });

            model.TransporterCodes ??= new List<int>();
            string mobileNo = (model.MobileNo ?? "").Trim();

            if (model.SendToTransporter && !model.TransporterCodes.Any())
                return Json(new { success = false, message = "Please select at least one transporter." });

            if (!model.SendToTransporter && mobileNo == "")
                return Json(new { success = false, message = "Enter mobile no. to send whatsapp." });

            var result = await _repository.SendTransportRequestWhatsApp(model);

            return Json(new { success = result.status, message = result.message });
        }

        // ========== PINCODE ==========
        [HttpGet]
        public async Task<IActionResult> GetCityPincode(int cityCode)
        {
            string qry = $@"select ZIPCODE from CITY_MAST where code = {cityCode} and ACTIVE = 1";
            var pincode = await _dbHelper.ExecuteScalarAsync(qry);
            return Json(pincode);
        }
    }
}
