using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Production.LoomProcess
{
    public class LoomBreakDownEntryListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public LoomBreakDownEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "Loom Breakdown Entry";

            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Production/LoomProcess/LoombreakdownEntryList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult loadListData(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var globalVaribale= _globalVariableService.GetGlobalVariables();
            int totalCount = 0;
            List<object> list= new List<object>();

            try
            {
                using(SqlConnection con= _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd= new SqlCommand("sp_Break_Down_Loom", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVaribale.PubCompCode);  
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVaribale.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVaribale.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", "BKDN");

                    cmd.Parameters.AddWithValue("@Action", "List");
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? (object)DBNull.Value : searchTerm);

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(new
                        {
                            VNo = reader["VNo"] != DBNull.Value ? Convert.ToInt32(reader["VNo"]) : 0,
                            VType = reader["VType"]?.ToString(),
                            VDate = Convert.ToDateTime(reader["VDate"]).ToString("yyyy-MM-dd"),
                            DOC_ID = reader["DOC_ID"].ToString(),
                            SHIFT = reader["Shift"]?.ToString(),
                            ST_DATE = Convert.ToDateTime(reader["ST_DATE"]).ToString("yyyy-MM-dd"),
                            ST_TIME = reader["ST_TIME"]?.ToString(),
                            STOP_DATE = Convert.ToDateTime(reader["STOP_DATE"]).ToString("yyyy-MM-dd"),
                            STOP_TIME = reader["STOP_TIME"]?.ToString(),
                            HRS = reader["HRS"] != DBNull.Value ? Convert.ToInt32(reader["HRS"]) : 0,
                            MINT = reader["MINT"] != DBNull.Value ? Convert.ToInt32(reader["MINT"]) : 0,
                            LOOM_CODE = reader["LOOM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["LOOM_CODE"]) : 0,
                            LoomName = reader["LoomName"]?.ToString(),
                            BD_CODE = reader["BD_CODE"] != DBNull.Value ? Convert.ToInt32(reader["BD_CODE"]) : 0,
                            BDName = reader["BDName"]?.ToString(),
                            FAULT_CODE = reader["FAULT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["FAULT_CODE"]) : 0,
                            FaultName = reader["FaultName"]?.ToString(),
                            CONV_MINT = reader["CONV_MINT"] != DBNull.Value ? Convert.ToDecimal(reader["CONV_MINT"]) : 0,
                            CONV_HRS = reader["CONV_HRS"] != DBNull.Value ? Convert.ToDecimal(reader["CONV_HRS"]) : 0,
                            REMARKS = reader["REMARKS"]?.ToString(),
                            UUSER = reader["UUSER"]?.ToString(),
                            UDATE = Convert.ToDateTime(reader["UDATE"]).ToString("yyyy-MM-dd")
                        });
                    }
                    if (reader.NextResult() && reader.Read())
                    {
                        totalCount = Convert.ToInt32(reader["TotalCount"]);
                    }
                    return Json(new { success = true, data = list, totalCount });
                }
            }catch(Exception ex)
            {
                return Json(new {success= false, message= ex.Message}); 
            }
        }

        [HttpPost]
        public IActionResult DeleteData(string docId)
        {
            var globalVarible = _globalVariableService.GetGlobalVariables();
            if (string.IsNullOrEmpty(docId))
            {
                return Json(new { success = false, message = "Invalid DOC_ID" });
            }
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("sp_Break_Down_Loom", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVarible.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVarible.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVarible.PubFYearCode);
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@Action", "Delete");
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true, message = "Data Deleted Successfully !!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
