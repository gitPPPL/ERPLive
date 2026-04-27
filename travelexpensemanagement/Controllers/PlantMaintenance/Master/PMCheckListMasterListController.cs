using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
namespace travelexpensemanagement.Controllers.PlantMaintenance.Master
{
    public class PMCheckListMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariable;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public PMCheckListMasterListController(DataBaseConnection dbConnection, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariable = globalVariableService;
            _moduleService = moduleService;

        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "CheckList Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/PlantMaintenance/Master/PMCheckListMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult loadListData(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var globalVariable = _globalVariable.GetGlobalVariables();
            List<object> list = new List<object>();
            int totalCount = 0;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("Sp_PMCheckList_Master", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@Action", "ShowData");
                    cmd.Parameters.AddWithValue("@TYPE", "PMCL");
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    con.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(new
                        {
                            code = Convert.ToInt32(reader["CODE"]),
                            type = reader["TYPE"]?.ToString(),
                            checkListType = reader["CheckListType"]?.ToString(),
                            checkListName = reader["CheckListName"]?.ToString(),
                            activity = reader["Activity"]?.ToString(),
                            category = reader["Category"]?.ToString(),
                            parameterCode = Convert.ToInt32(reader["PARAMETER_CODE"]),
                            parameterName = reader["PARAMETER_NAME"]?.ToString(),
                            remarks = reader["REMARKS"]?.ToString()
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
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult DeleteCheckListMaster(int code)
        {
            var globalVariable = _globalVariable.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("Sp_PMCheckList_Master", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
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

