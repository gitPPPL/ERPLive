using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
namespace travelexpensemanagement.Controllers.PlantMaintenance.Transaction
{
    public class MaintenanceStatusListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public MaintenanceStatusListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "Maintenance Status List";

            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/PlantMaintenance/Transaction/MaintenanceStatusList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult LoadListData(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var global = _globalVariableService.GetGlobalVariables();
            int totalCount = 0;
            List<object> list = new List<object>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("Sp_Maintenance_Status", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@Action", "GetList");

                    con.Open();

                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        list.Add(new
                        {
                            docId = reader["DOC_ID"].ToString(),
                            followUpNo = reader["followUpNo"].ToString(),
                            Date = Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                            planNo = reader["planNo"].ToString(),
                            planName= reader["planName"].ToString(),
                            EquipmentName = reader["EquipmentName"].ToString(),
                            place = reader["place"].ToString(),
                            section = reader["section"].ToString(),
                            startDate = reader["startDate"].ToString(),
                            endDate = reader["endDate"].ToString()
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
                return Json(new {success= false, message= ex.Message});
            }
        }
        [HttpPost]
        public IActionResult DeleteMaintenanceStatus(string docId)
        {
            var globalVarible = _globalVariableService.GetGlobalVariables();
            try
            {
                using(SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("Sp_Maintenance_Status", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();

                    cmd.Parameters.AddWithValue("@COMP_CODE",globalVarible.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVarible.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVarible.PubFYearCode);
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@Action", "Delete");
                    cmd.ExecuteNonQuery();
                }
                return Json(new {success= true, message= "Data Deleted Successfully !!"});
                
            }
            catch (Exception ex)
            {
                return Json(new {success= false, message= ex.Message});
            }
        }
    }
}
