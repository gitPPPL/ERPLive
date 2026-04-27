using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Production.BagsProcess
{
    public class BOMMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Common.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public BOMMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
     ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Production/BagsProcess/BOMMaster/Index.cshtml");
        }

        [HttpGet]
        public IActionResult CustomerNameDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = @" Select code, name from SUBGROUP_MAST where COMP_CODE =" + getData.PubCompCode + "and active = 1";
            var customer = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = customer });
        }

        [HttpGet]
        public IActionResult BagTypeDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = @"select code , name from FIBCBAG_MAST where COMP_CODE =" + getData.PubCompCode + "and active = 1";
            var bagType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = bagType });
        }

        [HttpGet]
        public IActionResult ColorDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = @"select code ,name from COLOR_MAST where COMP_CODE = " + getData.PubCompCode;
            var color = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = color });
        }

        [HttpGet]
        public IActionResult ItemDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = @"select code , name , ShortName from ITEM_MAST where COMP_CODE =" + getData.PubCompCode;
            var item = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = item });
        }
        
        [HttpGet]
        public IActionResult LinerTypeDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = @"select code , name from FIBCLINER_MAST where COMP_CODE =" + getData.PubCompCode + "and ACTIVE =1 ";
            var linerType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = linerType });
        }

        [HttpGet]
        public IActionResult ComponentName()
        {
            var getData= _globalVariableService.GetGlobalVariables();
            string query = @"select code , name from FIBCCOMP_MAST where COMP_CODE ="+ getData.PubCompCode + "and ACTIVE = 1";
            var componentType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = componentType });
        }

    }
}
