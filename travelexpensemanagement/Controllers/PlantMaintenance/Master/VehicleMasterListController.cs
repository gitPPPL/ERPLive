using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.PlantMaintenance.Master
{
    public class VehicleMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariable;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public VehicleMasterListController(DataBaseConnection dbConnection, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariable = globalVariableService;
            _moduleService = moduleService;

        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Vehicle Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/PlantMaintenance/Master/VehicleMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public IActionResult LoadListData(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var globalVariable = _globalVariable.GetGlobalVariables();
            var list = new List<object>();
            int totalCount = 0;
            try
            {
                using(SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("SP_VEHICLE_MASTER", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@Action", "ShowData");
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    con.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(new
                        {
                            code = reader["CODE"],
                            vehicleName = reader["VEHICLE_NAME"]?.ToString(),
                            VehicleCategory = reader["VEHICLE_CATEGORY"]?.ToString(),
                            RegistrationNo = reader["VEHICLE_REGNO"]?.ToString(),
                            Make = reader["MAKE_NAME"]?.ToString(),
                            Color = reader["COLOR_NAME"]?.ToString(),
                            Model = reader["MODEL"]?.ToString(),
                            ChassisNo = reader["CHASSIS_NO"]?.ToString(),
                            EngineNo = reader["ENGINE_NO"]?.ToString(),
                            FuelType = reader["FUEL_TYPE"]?.ToString(),
                            RoadTaxDate = reader["ROADTAX_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(reader["ROADTAX_DATE"]).ToString("dd-MM-yyyy")
                        });
                    }
                    if (reader.NextResult() && reader.Read())
                    {
                        totalCount = Convert.ToInt32(reader["TotalCount"]);
                    }
                }

                return Json(new { success = true, data = list, totalCount });
            }
            catch(Exception ex)
            {
                return Json(new { success = false, message = "Failed to load List" });
            }
        }
        [HttpPost]
        public IActionResult DeleteData(int code)
        {
            var globalVariable = _globalVariable.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("SP_VEHICLE_MASTER", con);

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@Action", "Delete");
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true, message = "Record deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
