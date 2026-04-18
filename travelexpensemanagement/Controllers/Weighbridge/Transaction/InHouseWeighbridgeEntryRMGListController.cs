using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Weighbridge.Transaction
{
    public class InHouseWeighbridgeEntryRMGListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public InHouseWeighbridgeEntryRMGListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "In House Weighbridge Entry RMG";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Weighbridge/Transaction/InHouseWeighbridgeEntryRMGList/Index.cshtml", model);
        }
    }
}
