
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
namespace travelexpensemanagement.Controllers.PlantMaintenance.Master
{
    public class FrequencyMasterListController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariable;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public FrequencyMasterListController(DataBaseConnection dbConnection, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariable = globalVariableService;
            _moduleService = moduleService;

        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Frequency Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/PlantMaintenance/Master/FrequencyMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult loadListData(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var globalVariable=_globalVariable.GetGlobalVariables();
            List<object> list = new List<object>();
            int totalCount = 0;

            using (SqlConnection con= _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand("dbo.SP_PM_FREQUENCYMASTER", con);

                cmd.Parameters.AddWithValue("@Action", "ShowData");
                cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();

                SqlDataReader reader= cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new
                    {
                        code = Convert.ToInt32(reader["CODE"]),
                        name = reader["NAME"]?.ToString(),
                        shortName = reader["SHORTNAME"]?.ToString(),
                        active = Convert.ToInt32(reader["ACTIVE"])
                    });
                }
                if (reader.NextResult() && reader.Read())
                {
                    totalCount = Convert.ToInt32(reader["TotalCount"]);
                }
            }
            return Json(new { success = true, data = list, totalCount });
        }

        [HttpPost]
        public IActionResult DeleteFrequencyMaster(int code)
        {
            try
            {
                using(SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("SP_PM_FREQUENCYMASTER", con);

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CODE", code);
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
