using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Implementations.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class LoomFabricWidthEntryListController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly ILoomFabricWidthEntryListRepository _loomFabricWidthEntryList;
        public LoomFabricWidthEntryListController(DataBaseConnection dbcontext, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, ILoomFabricWidthEntryListRepository loomFabricWidthEntryList)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _loomFabricWidthEntryList = loomFabricWidthEntryList;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Loom Fabric Width Entry";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();
            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/QualityControl/Transaction/LoomFabricWidthEntryList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetLoomFabricStrengthList(string searchTerm = "",int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var result = await _loomFabricWidthEntryList.GetLoomFabricStrengthListAsync(searchTerm, pageNumber, pageSize);

                return Json(new
                {
                    status = true,
                    data = result.Data,
                    totalCount = result.TotalCount
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteLoomFabricStrengthEntry(string docId)
        {
            try
            {
                var result = await _loomFabricWidthEntryList.DeleteLoomFabricStrengthEntryAsync(docId);

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLoomFabricStrengthEntryDetails(string docid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(docid))
                {
                    return Json(new
                    {
                        status = false,
                        message = "Invalid ID"
                    });
                }

                var data = await _loomFabricWidthEntryList
                    .GetLoomFabricStrengthEntryDetailsAsync(docid);

                return Json(new
                {
                    status = true,
                    data = data
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportAllDocs()
        {
            try
            {
                var data = await _loomFabricWidthEntryList
                    .ExportAllDocsAsync();

                return Json(new
                {
                    status = true,
                    data = data
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }
    }
}
