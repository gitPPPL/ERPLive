using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    [SessionAuthorize]                                                              
    public class LoomFabricStrengthEntryListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly ILoomFabricStrengthEntryListRepository _loomFabricStrengthEntryList;
        public LoomFabricStrengthEntryListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, ILoomFabricStrengthEntryListRepository loomFabricStrengthEntryList)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _loomFabricStrengthEntryList = loomFabricStrengthEntryList;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Loom Fabric Strength Entry";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/QualityControl/Transaction/LoomFabricStrengthEntryList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetLoomFabricStrengthList(string searchTerm = "",int pageNumber = 1,int pageSize = 10)
        {
            try
            {
                var result = await _loomFabricStrengthEntryList.GetLoomFabricStrengthListAsync(searchTerm, pageNumber, pageSize);

                return Json(new{status = true, data = result.Data, totalCount = result.TotalCount});
            }
            catch (Exception ex)
            {
                return Json(new{status = false, message = ex.Message});
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteLoomFabricStrengthEntry(string docId)
        {
            try
            {
                var result = await _loomFabricStrengthEntryList.DeleteLoomFabricStrengthEntryAsync(docId);

                return Json(new {success = result.Success, message = result.Message});
            }
            catch (Exception ex)
            {
                return Json(new {success = false, message = ex.Message});
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLoomFabricStrengthEntryDetails(string docId)
        {
            try
            {
                var data = await _loomFabricStrengthEntryList.GetLoomFabricStrengthEntryDetailsAsync(docId);

                if (data == null)
                {
                    return Json(new {status = false,message = "Invalid ID"});
                }

                return Json(new {status = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new {status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportAllDocs()
        {
            try
            {
                var data = await _loomFabricStrengthEntryList.ExportAllDocsAsync();
                return Json(new { status = true,  data = data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message});
            }
        }

    }
}
