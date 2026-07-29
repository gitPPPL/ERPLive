using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Common.DropdownService.DropdownService;
using static travelexpensemanagement.Controllers.QualityControl.Transaction.IncommingQCRMController;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseReturnEntry;


namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseReturnEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly IPurchaseReturnEntryRepository _purchaseReturnEntryRepository;
        public PurchaseReturnEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper,
        ModuleService.ModuleService moduleService, LogService.LogService logService, IPurchaseReturnEntryRepository purchaseReturnEntryRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _logService = logService;
            _purchaseReturnEntryRepository = purchaseReturnEntryRepository;
        }
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/PurchaseReturnEntry/Index.cshtml");
        }
        public JsonResult GetddlDocType()
        {
            return Json(_purchaseReturnEntryRepository.GetddlDocType());
        }
        public JsonResult GetddlRefType()
        {
            var data = _purchaseReturnEntryRepository.GetddlRefType();
            return Json(data);
        }

        [HttpPost]
        public JsonResult GetDocNo(string docType, string docName)
        {
            try
            {
                int nextNo = _purchaseReturnEntryRepository.GetDocNo(docType);

                return Json(new
                {
                    success = true,
                    nextVNo = nextNo
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        #region Ref No

        [HttpGet]
        public JsonResult GetddlRefNo(string Vtype)
        {
            return Json(_purchaseReturnEntryRepository.GetddlRefNo(Vtype));
        }
        #endregion

        #region Document Status
        [HttpGet]
        public JsonResult GetddlDocStatus()
        {
            return Json(_purchaseReturnEntryRepository.GetddlDocStatus());
        }
        #endregion

        #region Make List
        [HttpGet]
        public JsonResult GetMakeListByItem()
        {
            return Json(_purchaseReturnEntryRepository.GetMakeListByItem());
        }
        #endregion

        #region Department
        [HttpGet]
        public JsonResult GetDepartmentList()
        {
            return Json(_purchaseReturnEntryRepository.GetDepartmentList());
        }

        #endregion

        #region Return To
        [HttpGet]
        public JsonResult GetddlReturnTo()
        {
            return Json(_purchaseReturnEntryRepository.GetddlReturnTo());
        }
        #endregion

        #region Credit AC
        [HttpGet]
        public JsonResult GetddlCreditAC()
        {
            return Json(_purchaseReturnEntryRepository.GetddlCreditAC());
        }
        #endregion

        #region Debit AC
        [HttpGet]
        public JsonResult GetddlDebitAC()
        {
            return Json(_purchaseReturnEntryRepository.GetddlDebitAC());
        }
        #endregion

        #region Freight Credit AC
        [HttpGet]
        public JsonResult GetddlFreightCreditAC()
        {
            return Json(_purchaseReturnEntryRepository.GetddlFreightCreditAC());
        }
        #endregion

        #region Freight Debit AC
        [HttpGet]
        public JsonResult GetddlFreightDebitAC()
        {
            return Json(_purchaseReturnEntryRepository.GetddlFreightDebitAC());
        }
        #endregion
        [HttpPost]
        public JsonResult GetBillDetails(int code)
        {
            return Json(_purchaseReturnEntryRepository.GetBillDetails(code));
        }
        [HttpGet]
        public JsonResult GetddlCityBillDetails()
        {
            return Json(_purchaseReturnEntryRepository.GetddlCityBillDetails());
        }

        [HttpGet]
        public JsonResult GetddlstateBillDetails()
        {
            return Json(_purchaseReturnEntryRepository.GetddlstateBillDetails());
        }

        [HttpGet]
        public JsonResult GetddlCityShipDetails()
        {
            return Json(_purchaseReturnEntryRepository.GetddlCityShipDetails());
        }

        [HttpGet]
        public JsonResult GetddlstateShipDetails()
        {
            return Json(_purchaseReturnEntryRepository.GetddlstateShipDetails());
        }

        [HttpGet]
        public JsonResult GetddlShipDetails()
        {
            return Json(_purchaseReturnEntryRepository.GetddlShipDetails());
        }
        public JsonResult GetDropdown(string type, string term = "")
        {
            if (type == "TransportName")
                return Json(_purchaseReturnEntryRepository.GetTransportName(term));

            return Json(new List<DropdownModel>());
        }

        [HttpGet]
        public JsonResult SearchTransportName(string term = "")
        {
            return Json(_purchaseReturnEntryRepository.GetTransportName(term));
        }

        [HttpGet]
        public JsonResult GetddlTransportAc()
        {
            return Json(_purchaseReturnEntryRepository.GetddlTransportAc());
        }

        [HttpGet]
        public JsonResult GetItemList()
        {
            return Json(_purchaseReturnEntryRepository.GetItemList());
        }

        [HttpGet]
        public JsonResult GetHSNCode(int code)
        {
            return Json(_purchaseReturnEntryRepository.GetHSNCode(code));
        }

        [HttpGet]
        public JsonResult GetTaxTypeList()
        {
            return Json(_purchaseReturnEntryRepository.GetTaxTypeList());
        }

        [HttpGet]
        public JsonResult GetTaxTypeDetails(string code)
        {
            return Json(_purchaseReturnEntryRepository.GetTaxTypeDetails(code));
        }
        [HttpPost]
        public async Task<IActionResult> SaveAllData(
            [FromForm] string Header,
            [FromForm] List<ItemDetailModel> ItemDetails,
            [FromForm] List<AttachmentModel> Attachments)
        {
            var headerObj1 = JsonConvert.DeserializeObject<PurchaseReturnHeaderModel>(Header);

            var result = await _purchaseReturnEntryRepository.SaveAllData(
                headerObj1,
                ItemDetails,
                Attachments
            );

            return Json(result);
        }
        public static void AddParameterSafe(SqlCommand cmd, string paramName, object value)
        {
            try
            {
                cmd.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message} | Parameter: {paramName}", ex);
            }
        }
        public async Task<IActionResult> GetRefNoList(string StrVNo, string StrV_type)
        {
            try
            {
                var data = await _purchaseReturnEntryRepository
                    .GetRefNoList(StrVNo, StrV_type);

                return Json(new
                {
                    success = true,
                    data
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetAllDatadetails([FromBody] GetDetailsRequest request)
        {
            if (request == null)
                return BadRequest("Invalid Request");

            try
            {
                var result = await _purchaseReturnEntryRepository.GetAllDataDetails(request);

                return Json(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> PrintPurchaseReturnEntryReport([FromBody] PrintReportModelPurchaseReturnEntry model)
        {
            if (model == null)
                return BadRequest();

            try
            {
                var result = await _purchaseReturnEntryRepository
                    .PrintPurchaseReturnEntryReport(model);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
       
    }
}
