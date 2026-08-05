using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PrintingRequestionEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        public PrintingRequestionEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _logService = logService; ;
        }
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/PrintingRequestionEntry/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetDocNo()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                string query = @"SELECT ISNULL(MAX(V_NO), 0) + 1 AS NextVNo FROM PENDING_ORDERSAUDA WHERE COMP_CODE = @CompCode
                AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode";
                var parameters = new[]
                {
                    new SqlParameter("@CompCode", gv.PubCompCode),
                    new SqlParameter("@BranchCode", gv.PubBranchCode),
                    new SqlParameter("@YearCode", gv.PubFYearCode)
                };
                int nextVNo = 1;
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddRange(parameters);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            nextVNo = Convert.ToInt32(result);
                        }
                    }
                }
                return Json(new
                {
                    success = true,
                    nextVNo = nextVNo
                });
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
        [HttpGet]
        public JsonResult GetPlace(string search = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT CODE,NAME FROM PLACE_MAST WHERE COMP_CODE={gv.PubCompCode}";
            if (!string.IsNullOrWhiteSpace(search))
            {
                query += $" AND NAME LIKE '{search}%'";
            }
            query += " ORDER BY NAME";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        [HttpGet]
        public JsonResult GetDocType()
        {
            string query = @"SELECT Code, Name FROM DOCTYPE_MAST WHERE DOCTYPE='PrintingRequisition'";
            var data = _dropdownService.GetDropdownList(query);
            return Json(data);
        }
        [HttpGet]
        public JsonResult GetDepartment(string search = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT DISTINCT b.CODE,b.NAME  FROM USER_DEPT a LEFT JOIN ITEMDEPT_MAST b
            ON a.DEPT_CODE=b.CODE AND a.COMP_CODE=b.COMP_CODE WHERE a.USER_CODE=1  AND a.COMP_CODE={gv.PubCompCode}
            AND b.TRAN_TYPE='Store'";
            if (!string.IsNullOrWhiteSpace(search))
            {
                query += $" AND b.NAME LIKE '{search}%'";
            }
            query += " ORDER BY b.NAME";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }

        [HttpGet]
        public JsonResult GetRequestBy(string search = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT b.CODE,b.FULL_NAME AS NAME FROM SUBUSER_MAST a  LEFT JOIN USER_MAST b
            ON b.CODE=a.USER_CODE WHERE a.COMP_CODE={gv.PubCompCode}";
            if (!string.IsNullOrWhiteSpace(search))
            {
                query += $" AND b.FULL_NAME LIKE '{search}%'";
            }
            query += "  AND ISNULL(b.CODE, '') <> '' AND ISNULL(b.FULL_NAME, '') <> '' ORDER BY b.FULL_NAME";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        [HttpGet]
        public JsonResult GetStatus()
        {
            string query = @"SELECT CODE,NAME FROM DOCSTATUS_MAST WHERE V_TYPE='Document'   ORDER BY CODE";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        [HttpGet]
        public JsonResult GetItem()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT Code, Name FROM ITEM_MAST WHERE COMP_CODE = {gv.PubCompCode} AND ACTIVE = 1 ORDER BY Name";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        [HttpGet]
        public JsonResult GetMake(int itemCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT a.MAKE_CODE AS Code,  b.NAME AS Name FROM ITEM_MAKE a LEFT JOIN ITEMMAKE_MAST b
            ON a.MAKE_CODE = b.CODE AND b.COMP_CODE = {gv.PubCompCode} WHERE a.ITEM_CODE = {itemCode}  AND a.COMP_CODE = {gv.PubCompCode} ORDER BY b.NAME";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        public JsonResult GetPriority()
        {
            string query = @"SELECT CODE AS Value, NAME AS Text  FROM DOCSTATUS_MAST
            WHERE V_TYPE='Preority' ORDER BY CODE";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }

        public JsonResult GetWorkType()
        {
            string query = @"SELECT CODE AS Value, NAME AS Text FROM DOCSTATUS_MAST
             WHERE V_TYPE='WorkType' ORDER BY NAME";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
    }
}
