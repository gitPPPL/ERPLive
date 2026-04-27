using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Purchase.Transaction;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.PlantMaintenance.Master
{
    public class MaintenancePlanMasterListController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariable;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public MaintenancePlanMasterListController(DataBaseConnection dbConnection, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariable = globalVariableService;
            _moduleService = moduleService;

        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "MaintenancePlan Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/PlantMaintenance/Master/MaintenancePlanMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult loadListData(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var globalVariable= _globalVariable.GetGlobalVariables();
            List<object> list = new List<object>();
            int totalCount = 0;

            try
            {
                 using(SqlConnection con= _dbConnection.GetErpConnection())
                 {
                    SqlCommand cmd = new SqlCommand("Sp_MaintenancePlan_Master ", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    
                    cmd.Parameters.AddWithValue("@COMP_CODE",globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@Action", "ShowData");
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);

                    SqlDataReader reader= cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        list.Add(new
                        {
                            code = Convert.ToInt32(reader["CODE"]),
                            name = reader["PLAN_NAME"]?.ToString(),
                            machine = reader["M_NAME"]?.ToString(),
                            category = reader["CAT_NAME"]?.ToString(),
                            place = reader["PLACE_NAME"]?.ToString(),
                            section = reader["SECTION_NAME"]?.ToString()
                        });
                    }
                    if (reader.NextResult() && reader.Read())
                    {
                        totalCount = Convert.ToInt32(reader["TotalCount"]);
                    }

                 }
                return Json(new { success = true, data = list, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new {success=false, message=ex.Message});
            }
        }
        [HttpPost]
        public IActionResult DeleteMaintenancePlanMaster(int code)
        {
            var globalVariable = _globalVariable.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("Sp_MaintenancePlan_Master", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@Action", "Delete");
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true, message = "Data Deleted Successfully!!" });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
