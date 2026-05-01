using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Production.Master
{
    public class ChemicalRecipeMasterListController : Controller
    {

        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public ChemicalRecipeMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;

        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Chemical Recipe Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Production/Master/ChemicalRecipeMasterList/Index.cshtml", model);
        }
        
        [HttpGet]
        public IActionResult loadListData(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var globalVariable = _globalValue.GetGlobalVariables();
            int totalCount = 0;
            List<object> list = new List<object>();

            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("sp_CHEMICAL_RECIPE_MASTER", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@Action", "List");

                    con.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(new
                        {
                            DocId = reader["DOC_ID"]?.ToString(),
                            SNO = reader["SNO"] != DBNull.Value ? Convert.ToInt32(reader["SNO"]) : 0,
                            VNo = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                            VType = reader["V_TYPE"]?.ToString(),
                            DeptName = reader["DEPT_NAME"]?.ToString(),
                            //ITEM_NAME = reader["ITEM_NAMES"]?.ToString(),
                            //PER = reader["PERS"]?.ToString(),
                            //ItemCode = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : 0,
                            DeptCode = reader["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["DEPT_CODE"]) : 0
                        });
                    }
                    if (reader.NextResult() && reader.Read())
                    {
                        totalCount = Convert.ToInt32(reader["TotalCount"]);
                    }

                    return Json(new { success = true, data = list, totalCount });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteChemicalRecipe(string docId)
        {
            var globalVar = _globalValue.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_CHEMICAL_RECIPE_MASTER", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                    cmd.Parameters.AddWithValue("@DOC_ID", docId); 
                    cmd.Parameters.AddWithValue("@Action", "Delete");
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Record deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
