using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.ModuleService;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class MiscConsumptionEntryListController : Controller
    {
        private readonly IMiscConsumptionListRepository _repo;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public MiscConsumptionEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DropdownService dropdownService, DbHelper dbHelper, IMiscConsumptionListRepository repo,
           ModuleService.ModuleService moduleService ,travelexpensemanagement.LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _repo = repo;
            _logService = logService;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Contractor Material Consumption";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/GateEntry/Transaction/MiscConsumptionEntryList/Index.cshtml", model);
        }

        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var (data, totalCount) = _repo.GetList(searchTerm, pageNumber, pageSize);

                return Json(new{ success = true,headers = data, totalCount = totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching data", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult GetDataByCode(int rowId, string vtype)
        {
            try
            {
                var data = _repo.GetDataByCode(rowId, vtype);

                return Json(new { success = true,data = new{ Header = data.Header, Details = data.Deatils }});
            }
            catch (Exception ex)
            {
                return Json(new {success = false, message = "Error fetching data", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string vNo, string docType)
        {
            try
            {
                var result = await _repo.Delete(vNo, docType);

                if (result.status)
                {
                    _logService.InsertLog("GATE1", "MiscConsumptionEntry", "Transaction", "DELETE", docType, vNo, null);

                    return Json(new { status = result.status, message = result.message });
                }

                return Json(new { success = false, message = "Delete failed" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false,   message = "Error deleting record", error = ex.Message  });
            }
        }

        public IActionResult GetPendingDocumnents(int partyId)
        {
            try
            {
                var data = _repo.GetPendingDocuments(partyId);

                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new{ success = false, message = "Error fetching pending documents", error = ex.Message });
            }
        }
    }
}
