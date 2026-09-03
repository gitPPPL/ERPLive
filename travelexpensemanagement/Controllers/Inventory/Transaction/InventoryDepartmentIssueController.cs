using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    [SessionAuthorize]
    public class InventoryDepartmentIssueController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IInventoryDepartmentIssueRepository _inventoryDepartmentIssueRepository;
        public string Fromname = "AdjustmentIssue";
        public InventoryDepartmentIssueController( DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService,
            DropdownService dropdownService,
            travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
            travelexpensemanagement.ModuleService.ModuleService moduleService,
            GlobalValidationdate globalValidationdate,
            IInventoryDepartmentIssueRepository inventoryDepartmentIssueRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _inventoryDepartmentIssueRepository =
                inventoryDepartmentIssueRepository;
        }

        public IActionResult Index()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;
            return View("~/Views/Inventory/Transaction/InventoryDepartmentIssue/Index.cshtml");
        }

        [HttpGet]
        public JsonResult GetVNo( string Vtype,  string Tablename = "ISSUE1")
        {
            string newV_NO = "00000";
            try
            {
                newV_NO = _globalValidationdate.GetVNo( Vtype, Tablename );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(  $"Error in GetVNo: {ex.Message}" );
                return Json(new {  error = "An error occurred while generating the V_NO." });
            }
            return Json(new  { V_NO = newV_NO });
        }

        [HttpGet]
        public JsonResult DDlVType()
        {
            var data = _inventoryDepartmentIssueRepository.DDlVType(Fromname);
            return Json(data);
        }



        [HttpGet]
        public JsonResult DDlPlaceFrom()
        {
            var data = _inventoryDepartmentIssueRepository.DDlVType(Fromname);
            return Json(data);
        }


        public JsonResult DDLSTATUS()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        public JsonResult DDLProdType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @" SELECT V_No, V_Type FROM PROD_ORDER1 WHERE V_Type IN (
                SELECT CODE FROM DOCTYPE_MAST WHERE DOCTYPE = 'ProductionOrder' AND ACTIVE = 1) AND 
                COMP_CODE = " + getdata.PubCompCode + @" AND YEAR_CODE = " + getdata.PubFYearCode + @"
                AND BRANCH_CODE = " + getdata.PubBranchCode + ";";

                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDLDO(string VType, int VNo)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"
                SELECT  a.V_TYPE,  a.V_NO FROM DO1 AS a
                LEFT JOIN City_mast AS b  ON a.SHIP_CITY = b.code  WHERE a.COMP_CODE = " + getdata.PubCompCode + @"
                AND a.BRANCH_CODE = " + getdata.PubBranchCode + @" AND CONCAT(a.V_TYPE, a.V_NO) NOT IN
                (SELECT CONCAT(PORD_Type, PORD_NO) FROM PRODUCTION1  WHERE V_Type = '" + VType + @"'  AND V_no <> " + VNo + @"  AND Branch_code = " + getdata.PubBranchCode + @" );";

                var data = _dropdownService.GetDropdownList(query);

                return Json(data);
            }
        }

        [HttpGet]
        public JsonResult DDlItemName(string V_TYPE)
        {
            var data = _inventoryDepartmentIssueRepository.DDlItemName("AdjustmentIssue", V_TYPE);
            return Json(data);
        }

        [HttpGet]
        public JsonResult CopyData(string V_TYPE)
        {
            var data = _inventoryDepartmentIssueRepository.CopyData( V_TYPE);
            return Json(data);
        }

        [HttpPost]
        public async Task<JsonResult> SavedData([FromBody] InventryDepartmentIssue_Model request)
        {
            if (request?.Header == null)
            {
                return Json(new { success = false, status = "Error", message = "Input model is null"});
            }

            var action = string.Equals( request.Header.action,  "INSERT",  StringComparison.OrdinalIgnoreCase)  ? "INSERT"  : "UPDATE";

            var result = await _inventoryDepartmentIssueRepository.SubmitRequest(  request.Header, request.Details, action);

            return Json(new {  success = result.Status == "Success",  status = result.Status,  message = result.Message });
        }

    }
}