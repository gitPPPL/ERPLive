using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    //[SessionAuthorize]
    public class AssetsMasterListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;


        public AssetsMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Assets Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); 

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Setup/AssetsMasterList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetAllAssets(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var assetList = new List<AssetModel>();
            int totalCount = 0;
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_InsertAssetMaster", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Select");
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@AC_NAME", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            assetList.Add(new AssetModel
                            {
                                AC_CODE = reader["AC_CODE"] != DBNull.Value ? Convert.ToInt32(reader["AC_CODE"]) : 0,
                                AC_NAME = reader["AC_NAME"]?.ToString(),
                                OP_AMT = reader["OP_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["OP_AMT"]) : 0,
                                DEP_AMT = reader["DEP_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["DEP_AMT"]) : 0,
                                DEP_RATE = reader["DEP_RATE"] != DBNull.Value ? Convert.ToDecimal(reader["DEP_RATE"]) : 0,
                                LIFE = reader["LIFE"] != DBNull.Value ? Convert.ToInt32(reader["LIFE"]) : 0,
                                SHIFT_CALC = reader["SHIFT_CALC"] != DBNull.Value ? Convert.ToInt32(reader["SHIFT_CALC"]) : 0,
                                SRNO = reader["SRNO"] != DBNull.Value ? Convert.ToInt32(reader["SRNO"]) : 0
                            });
                        }

                        if (reader.NextResult() && reader.Read() && reader["TotalCount"] != DBNull.Value)
                        {
                            totalCount = Convert.ToInt32(reader["TotalCount"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }

            return Json(new { groups = assetList, totalCount });
        }


    }
}
