using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.Master.TenacityMaster;

namespace travelexpensemanagement.Controllers.Production.Master
{
    public class TenacityMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public TenacityMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;

        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Tenacity Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Production/Master/TenacityMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public IActionResult GetTenacityList(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var globalVariable = _globalValue.GetGlobalVariables();
            List<TenacityMaster> list = new List<TenacityMaster>();
            int totalCount = 0;

            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                con.Open();
                string countQuery = @"SELECT COUNT(*) 
                              FROM TENACITY_MAST
                              WHERE COMP_CODE = @COMP_CODE
                              AND ISNULL(AED,'A') <> 'D'
                              AND (@SearchTerm = '' OR NAME LIKE '%' + @SearchTerm + '%')";

                using (SqlCommand countCmd = new SqlCommand(countQuery, con))
                {
                    countCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    countCmd.Parameters.AddWithValue("@SearchTerm", searchTerm ?? "");

                    totalCount = (int)countCmd.ExecuteScalar();
                }
                string query = @"SELECT CODE, NAME, TENACITY_TYPE, 
                         MIN_STD, MAX_STD, TENACITY_CAT, ACTIVE
                         FROM TENACITY_MAST
                         WHERE COMP_CODE = @COMP_CODE
                         AND ISNULL(AED,'A') <> 'D'
                         AND (@SearchTerm = '' OR NAME LIKE '%' + @SearchTerm + '%')
                         ORDER BY CODE DESC
                         OFFSET (@PageNumber - 1) * @PageSize ROWS
                         FETCH NEXT @PageSize ROWS ONLY";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@SearchTerm", searchTerm ?? "");
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new TenacityMaster
                            {
                                Code = Convert.ToInt32(dr["CODE"]),
                                Name = dr["NAME"]?.ToString(),
                                TENACITY_TYPE = dr["TENACITY_TYPE"]?.ToString(),
                                TENACITY_CAT = dr["TENACITY_CAT"]?.ToString(),
                                MIN_STD = dr["MIN_STD"] != DBNull.Value ? Convert.ToDecimal(dr["MIN_STD"]) : 0,
                                MAX_STD = dr["MAX_STD"] != DBNull.Value ? Convert.ToDecimal(dr["MAX_STD"]) : 0,
                                Active = dr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(dr["ACTIVE"]) : 0
                            });
                        }
                    }
                }
            }

            return Json(new
            {
                success = true,
                data = list,
                totalCount = totalCount
            });
        }
        [HttpPost]
        public IActionResult DeleteTenacityMaster(int code)
        {
            var globalVarriable = _globalValue.GetGlobalVariables();
            try
            {
                using(SqlConnection con = _dbcontext.GetErpConnection())
                {
                    con.Open();
                    // soft Delete
                    string query = @"UPDATE TENACITY_MAST SET AED = 'D' WHERE CODE = @Code AND COMP_CODE = @CompCode";
                    
                    using(SqlCommand cmd= new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Code", code);
                        cmd.Parameters.AddWithValue("@CompCode", globalVarriable.PubCompCode);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                            return Json(new { success = true, message = "Record deleted successfully" });
                        else
                            return Json(new { success = false, message = "Record not found" });
                    }                                
                }
            } catch(Exception ex)
            {
                return Json(new {success=false, message=ex.Message});   
            }
        }
    }
}
