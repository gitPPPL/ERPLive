using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.Master.LotMaster;
namespace travelexpensemanagement.Controllers.Production.Master
{
    public class LotMasterListController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariable;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public LotMasterListController(DataBaseConnection dbConnection, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariable = globalVariableService;
            _moduleService = moduleService;

        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Lot Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Production/Master/LotMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public IActionResult LoadData(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var list = new List<LotMaster>();
            int totalCount = 0;

            var globalVariable = _globalVariable.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // Total Count
                    string countQuery = @"SELECT COUNT(*) FROM LOT_MAST WHERE COMP_CODE = @COMP_CODE AND (@SearchTerm = '' OR NAME LIKE '%' + @SearchTerm + '%'
                                         OR SHORTNAME LIKE '%' + @SearchTerm + '%')";
                    using (SqlCommand countCmd = new SqlCommand(countQuery, con))
                    {
                        countCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        countCmd.Parameters.AddWithValue("@SearchTerm", searchTerm ?? "");

                        totalCount = Convert.ToInt32(countCmd.ExecuteScalar());
                    }

                    // Pagination Query
                    string query = @"SELECT CODE, NAME, SHORTNAME FROM LOT_MAST WHERE COMP_CODE = @COMP_CODE
                                    AND (@SearchTerm = '' 
                                         OR NAME LIKE '%' + @SearchTerm + '%'
                                         OR SHORTNAME LIKE '%' + @SearchTerm + '%')
                                    ORDER BY CODE DESC
                                    OFFSET @Offset ROWS
                                    FETCH NEXT @PageSize ROWS ONLY";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@SearchTerm", searchTerm ?? "");
                        cmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new LotMaster
                                {
                                    CODE = Convert.ToInt32(dr["CODE"]),
                                    Name = dr["NAME"].ToString(),
                                    ShortName = dr["SHORTNAME"].ToString()
                                });
                            }
                        }
                    }
                }
                return Json(new { success = true, data = list, totalCount = totalCount });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }); 
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
                    con.Open();
                    string query = @"Delete from LOT_MAST where CODE=@CODE AND COMP_CODE=@COMP_CODE";
                    SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true, message = "Data Deleted Successfully" });
            }
            catch (Exception e)
            {
                return Json(new { success = false, message = e.Message });
            }
        }
    }
}
