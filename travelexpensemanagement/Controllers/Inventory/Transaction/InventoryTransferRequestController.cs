using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class InventoryTransferRequestController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;



        public InventoryTransferRequestController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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

            return View("~/Views/Inventory/Transaction/InventoryTransferRequest/Index.cshtml");
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
                string query = "Select Code,Name from DOCTYPE_MAST where DOCTYPE IN ('InventoryRequest') ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        public JsonResult DDlStatus()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        public JsonResult DDlPlace()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE,NAME FROM PLACE_MAST where comp_code=" + getdata.PubCompCode + " Order by NAME ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        public JsonResult DDlHOD()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select distinct b.code,EMP_NAME from PAYGATE_HOD a left join EMP_MAST b on  a.EMP_CODE=b.CODE and a.COMP_CODE=b.COMP_CODE where b.RESIGN_DATE is null and a.COMP_CODE=" + getdata.PubCompCode + " order by  EMP_NAME ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlDeptName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT distinct b.CODE,b.NAME FROM USER_DEPT a left join ITEMDEPT_MAST b on a.DEPT_CODE=b.CODE and a.comp_code=b.COMP_CODE " +
                "where a.user_code= "+  getdata.PubUserId +" and a.comp_code=" + getdata.PubCompCode + " and b.TRAN_TYPE='Store' order by  b.NAME  ";
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
                string sql = @"Select  c.NAME 'unit_name',c.CODE 'unit_code' from item_mast a 
                left join Item_Mgroup b on a.Mgroup_code=b.code  and a.comp_code=b.comp_code
                left outer join ITEMUNIT_MAST c on a.UNIT_CODE=c.CODE and c.comp_code=@CompCode
                where a.comp_code=@CompCode and a.active=1 and b.Mgroup_type in ('Store','Fuel')  and  a.CODE = @ItemCode   group by  c.NAME ,c.CODE  order by
                c.NAME ";

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
