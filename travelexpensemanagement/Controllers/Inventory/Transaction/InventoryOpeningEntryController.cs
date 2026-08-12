using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{

    [SessionAuthorize]
    public class InventoryOpeningEntryController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public InventoryOpeningEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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

            return View("~/Views/Inventory/Transaction/InventoryOpeningEntry/Index.cshtml");
        }

        public JsonResult GetVNo(string Vtype, string Tablename = "ISSUE1")
        {
            string newV_NO = "00000";
            try
            {
                newV_NO = _globalValidationdate.GetVNo(Vtype, Tablename);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO });
        }

        public JsonResult DDlVType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code,name from DOCTYPE_MAST  where doctype='OpeningStock' order by  name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlItemName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code ,name from ITEM_MAST where comp_code=" + getdata.PubCompCode + " and active=1 order by name  ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        public JsonResult DDlUnit()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select distinct unit_code,unit_name from ITEM_MAST where comp_code=" + getdata.PubCompCode + " and active=1 order by unit_name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDLItemmake()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code ,name from ITEMMAKE_MAST where comp_code=" + getdata.PubCompCode + " and active=1  order by name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDLItemDapt()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code ,name from ITEMDEPT_MAST where comp_code=" + getdata.PubCompCode + " and active=1  order by name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }


        public JsonResult GetDataByItemcode(int ItemCode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var data = new object();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string sql = @" SELECT unit_code,  unit_name  FROM ITEM_MAST WHERE comp_code = @CompCode AND code = @ItemCode  AND active = 1";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@ItemCode", ItemCode);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            data = new
                            {
                                unit_code = reader["unit_code"],
                                unit_name = reader["unit_name"]
                            };
                        }
                    }
                }
            }

            return Json(new
            {
                Data = data,
                Status = true
            });
        }







    }
}
