using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction;

namespace travelexpensemanagement.Controllers.Weighbridge.Transaction
{
    [SessionAuthorize]
    public class StoreWeighbridgeEntryListController : Controller
    {

        private readonly ModuleService.ModuleService _moduleService;

        private readonly IStoreWeighbridgeEntryListRepository _storeWbListRepository;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;

        public StoreWeighbridgeEntryListController(ModuleService.ModuleService moduleService, IStoreWeighbridgeEntryListRepository storeWbListRepository, GlobalVariableService globalVariableService
            , GlobalValidationdate globalValidationdate)
        {
            _moduleService = moduleService;
            _storeWbListRepository = storeWbListRepository;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Store Weighbridge Entry";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Weighbridge/Transaction/StoreWeighbridgeEntryList/Index.cshtml", model);

        }

        [HttpGet]
        public async Task<IActionResult> GetStoreWBridgeList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var result = await _storeWbListRepository.GetList(searchTerm, pageNumber, pageSize);
            if (!result.status)
            {
                return Json(new { status = result.status, message = result.message });
            }
            return Json(new { status = result.status, data = result.data, totalCount = result.totalCount});
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStoreWBridgeEntry(string docId, bool flag)
        {
            if (string.IsNullOrEmpty(docId))
            {
                return Json(new { status = false, message = "Invalid ID" });
            }
            var result = await _storeWbListRepository.DeleteStoreWb(docId);
            return Json(new { success = result.status, message = result.message });
        }
        [HttpPost]
        public async Task<IActionResult> ValidateDeleteStoreWb(string docId)
        {
            if (string.IsNullOrEmpty(docId))
            {
                return Json(new { status = false, message = "Invalid ID" });
            }
            var result = await _storeWbListRepository.ValidateDeleteStoreWb(docId);
            if (result.data != null)
            {
                return Json(new { success = result.status, message = result.message, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }
        [HttpGet]
        public async Task<IActionResult> GetStoreWBridgeEntryDetails(string docid)
        {
            if (string.IsNullOrEmpty(docid))
            {
                return Json(new { status = false, message = "Invalid ID" });
            }
            var result = await _storeWbListRepository.StoreWBDetails(docid);
            if (!result.status)
            {
                return Json(new { status = result.status, message = result.message });
            }
            return Json(new { status = result.status, data = result.data});
        }

        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
        {
            { "@YEAR_CODE", gv.PubFYearCode },
            { "@COMP_CODE", gv.PubCompCode },
            { "@BRANCH_CODE", gv.PubBranchCode },
            { "@DOCTYPE",  "KantaStore"},
            { "@Action", "Store_Wb_Excel" }
        };

                var fileBytes = _globalValidationdate.ExportToExcel("sp_GetWBEntry", "Store WeighBridge", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"StoreWeighBridge_{DateTime.Now:ddMMyyyy}.xlsx"
                );
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
