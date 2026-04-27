using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
namespace travelexpensemanagement.Controllers.Production.Master
{
    public class ItemStandardParameterMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariable;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public ItemStandardParameterMasterListController(DataBaseConnection dbConnection, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariable = globalVariableService;
            _moduleService = moduleService;

        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Item Standard Parameter Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Production/Master/ItemStandardParameterMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public IActionResult GetItemStandardParameterList(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var list = new List<object>();
            int totalCount = 0;
            var globalVariable = _globalVariable.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;

                        cmd.CommandText = @"SELECT COUNT(*) FROM ITEM_STDPARAM s LEFT JOIN ITEM_MAST i ON s.ITEM_CODE = i.CODE WHERE s.COMP_CODE = @COMP_CODE
                             AND (@SearchTerm = '' OR CAST(s.CODE AS NVARCHAR) LIKE '%' + @SearchTerm + '%' OR CAST(s.ITEM_CODE AS NVARCHAR) LIKE '%' + @SearchTerm + '%' OR i.SHORTNAME LIKE '%' + @SearchTerm + '%');
                             
                             SELECT s.CODE, s.ITEM_CODE, i.SHORTNAME,s.CUTTING_STD_WT, s.THREAD_STD_WT, s.PRINTING_STD_WT, s.MTR_STD_WT FROM ITEM_STDPARAM s
                             LEFT JOIN ITEM_MAST i ON s.ITEM_CODE = i.CODE WHERE s.COMP_CODE = @COMP_CODE AND (@SearchTerm = ''  OR CAST(s.CODE AS NVARCHAR) LIKE '%' + @SearchTerm + '%' 
                             OR CAST(s.ITEM_CODE AS NVARCHAR) LIKE '%' + @SearchTerm + '%' OR i.SHORTNAME LIKE '%' + @SearchTerm + '%')  ORDER BY CODE ASC
                             OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;";
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@SearchTerm", searchTerm ?? "");
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        con.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            
                            if (dr.Read())
                            {
                                totalCount = Convert.ToInt32(dr[0]);
                            }

                            dr.NextResult();

                            while (dr.Read())
                            {
                                list.Add(new
                                {
                                    code = dr["CODE"] != DBNull.Value ? Convert.ToInt32(dr["CODE"]) : 0,
                                    item_code = dr["ITEM_CODE"]?.ToString(),
                                    shortname = dr["SHORTNAME"]?.ToString(),
                                    cutting_std_wt = dr["CUTTING_STD_WT"] != DBNull.Value ? Convert.ToDecimal(dr["CUTTING_STD_WT"]) : 0,
                                    thread_std_wt = dr["THREAD_STD_WT"] != DBNull.Value ? Convert.ToDecimal(dr["THREAD_STD_WT"]) : 0,
                                    printing_std_wt = dr["PRINTING_STD_WT"] != DBNull.Value ? Convert.ToDecimal(dr["PRINTING_STD_WT"]) : 0,
                                    mtr_std_wt = dr["MTR_STD_WT"] != DBNull.Value ? Convert.ToDecimal(dr["MTR_STD_WT"]) : 0
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
                using(SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("sp_ItemStandard_ParameterMaster",con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("Action", "Delete");
                    cmd.Parameters.AddWithValue("@COMP_CODE", 1);
                    cmd.Parameters.AddWithValue("@CODE", code);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true, message="Data Deleted Successfully" });
            }
            catch(Exception e)
            {
                return Json(new { success = false, message = e.Message });
            }
            
        }
    }
}
