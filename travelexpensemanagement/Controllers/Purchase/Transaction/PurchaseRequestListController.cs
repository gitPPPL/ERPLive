using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    [SessionAuthorize]
    public class PurchaseRequestListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly ModuleService.ModuleService _moduleService;
        private readonly IPurchaseRequestListRepository _IPRListRepository;
        private readonly DbHelper _dbHelper;
        public PurchaseRequestListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
          ModuleService.ModuleService moduleService, IPurchaseRequestListRepository IPRRepository, DbHelper dbHelper)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _IPRListRepository = IPRRepository;
            _dbHelper = dbHelper;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Purchase Request";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();
            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel,
            };

            return View("~/Views/Purchase/Transaction/PurchaseRequestList/Index.cshtml", model);
        }
        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {   
            var result = _IPRListRepository.GetPurchaseRequestList(searchTerm, pageNumber, pageSize);
            if(result.data != null)
            {
                return Json(new { success = result.status, lists = result.data, totalCount = result.totalCount});
            }
            return Json(new { success = result.status, message = result.message});
        }
        [HttpGet]
        public IActionResult GetDataByCode(int code)
        {
            if(code <= 0)
            {
                return Json(new { success = false, message = "Invalid Id!"});
            }
            var result = _IPRListRepository.GetPurchaseRequestByCode(code);
            if(result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }
        [HttpGet]
        public async Task<IActionResult> GetDataCopyForm()
        {
            var result = await _IPRListRepository.GetDataCopyFormAsync();
            if(result.data != null)
            {
                return Json(new {success = result.status, message = result.message, data = result.data});
            }
            return Json(new {success = result.status, message = result.message});
        }
        [HttpGet]
        public async Task<IActionResult> GetDataMonthlyRequirement(int Deptid)
        {
            var result = await _IPRListRepository.GetMonthlyRequirementAsync(Deptid);
            if (result.data != null)
            {
                return Json(new { success = result.status, message = result.message, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }
        [HttpPost]
        public JsonResult Delete(int docId)
        {
            if (docId <= 0)
            {
                return Json(new { success = false, message = "Invalid Id!" });
            }
            var result = _IPRListRepository.DeletePurchaseRequest(docId);
            return Json(new { success = result.status, message = result.message});
        }

        [HttpGet]
        public async Task<JsonResult> PREntryDetails(string vNo)
        {
            try
            {
                var usersession = _globalVariableService.GetGlobalVariables();
                if (string.IsNullOrEmpty(vNo))
                {
                    return Json(new {status = false , message = "Invalid ID" });
                }
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@V_TYPE", "STPI"},
                    {"@V_NO", vNo },
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseReq1]", parameter);
                return Json(new { status = true, data = entryDetailList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult CheckApprovalStatusForDelete(int docId)
        {
            if (docId <= 0)
                return Json(new { success = false, message = "Invalid VNo" });

            var result = _IPRListRepository.CheckApprovalStatusForDelete(docId);
            return Json(new { success = result.status, isOpen = result.data, message = result.message });
        }
    }
}
