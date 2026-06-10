using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;

namespace travelexpensemanagement.Controllers.QualityControl.Reports
{
    public class QCReportController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly ILoomFabricWidthEntryRepository _loomFabricWidthEntry;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        public QCReportController(DataBaseConnection dbcontext, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, ILoomFabricWidthEntryRepository loomFabricWidthEntry, GlobalValidationdate globalValidationdate, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _loomFabricWidthEntry = loomFabricWidthEntry;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Reports/QCReport/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetdocType()
        {
            var globalVariable = _globalValue.GetGlobalVariables();
            string query = @"Select Code,Name from DOCTYPE_MAST where doctype in ('QualityControl','MaterialReceipt') order by name";
            var docType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = docType });
        }

        [HttpGet]
        public IActionResult GetItemList()
        {
            var globalVariable = _globalValue.GetGlobalVariables();
            string query = "select code, name from ITEM_MAST where comp_code=" + globalVariable.PubCompCode + " order by name";
            var itemList = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = itemList });
        }

        [HttpGet]
        public IActionResult GetPartyList()
        {
            var globalVariable = _globalValue.GetGlobalVariables();
            string query = "select code, name from SUBGROUP_MAST where comp_code=" + globalVariable.PubCompCode + " order by name";
            var partyList = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = partyList });
        }

        [HttpGet]
        public IActionResult GetItemGroup()
        {
            var globalVariable = _globalValue.GetGlobalVariables();
            string query = "select code,name from item_group where comp_code=" + globalVariable.PubCompCode + " order by name";
            var itemGroup = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = itemGroup });
        }

        [HttpGet]
        public IActionResult GetQcType()
        {
            var globalVariable = _globalValue.GetGlobalVariables();
            string query = "select code,name from qc_mast where comp_code =" + globalVariable.PubCompCode + " order by name";
            var qcType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = qcType });
        }

        [HttpGet]
        public IActionResult GetQcParam()
        {
            var globalVariable = _globalValue.GetGlobalVariables();
            string query = "select code,name from qcp_mast where comp_code =" + globalVariable.PubCompCode + " order by  name";
            var qcParam = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = qcParam });
        }

    }
}



