using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sale.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Reports
{
    public class PerformaRegisterReportController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public PerformaRegisterReportController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
       travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
       ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }

        public async Task<IActionResult> Index()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            var loadGeneralSetting = await _globalVariableService.LoadGeneralSetting();

            ViewBag.LoadGeneralSetting = loadGeneralSetting;

            string databaseName;

            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }

            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;

            return View("~/Views/Sales/Reports/PerformaRegisterReport/Index.cshtml");
        }

        public JsonResult DDlDocumentType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
               string query = "Select Code,Name from DOCTYPE_MAST where doctype in ('SalesInvoice','EComSalesInvoice','TradingSale','EComSalesReturn','SalesReturn') or code in ('CRSL','DNSL') order by name;";
                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlItemType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
               string query = @$"select DISTINCT ITEM_TYPE as 'No'  , ITEM_TYPE as 'Type'  from sale1 where comp_code={getdata.PubCompCode} AND YEAR_CODE={getdata.PubFYearCode} AND BRANCH_CODE={getdata.PubBranchCode} and ITEM_TYPE != '' order by ITEM_TYPE;";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }      
        public JsonResult OSGroup()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
               string query = @$"select code, name  from ACOS_MAST where comp_code={getdata.PubCompCode} order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlTransport()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
               string query = @$"select code, name  from TRANSPORT_MAST where comp_code= {getdata.PubCompCode} order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }

        public JsonResult DDlACGroup()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
               string query = @$"select code, name  from MGROUP_MAST where comp_code={getdata.PubCompCode} and nature in ('Customer') order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlSupplyType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
               string query = @$"select code, name  from MGROUP_MAST where comp_code={getdata.PubCompCode} and nature in ('Customer') order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlCountry()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
               string query = @$"select code, name  from COUNTRY_MAST order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }

        public JsonResult DDlItem()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
               string query = @$"select code, shortname  from ITEM_MAST where comp_code={getdata.PubCompCode} and shortname <>'' order by shortname";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlPartyName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
               string query = @$"select code, name  from SUBGROUP_MAST where comp_code={getdata.PubCompCode} order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }

    }
}













