using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.Master.TenacityGroupMaster;

namespace travelexpensemanagement.Controllers.Production.Master
{
    public class TenacityGroupMasterListController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public TenacityGroupMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;

        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Tenacity Group Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Production/Master/TenacityGroupMasterList/Index.cshtml", model);
        }
        
        [HttpGet]
        public IActionResult GetTenacityList(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var globalVariable = _globalValue.GetGlobalVariables();
            List<TenacityGroupMasterModel> list = new List<TenacityGroupMasterModel>();
            int totalCount = 0;

            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_Tenacity_Group_Master", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@Action", "Select");
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? (object)DBNull.Value : searchTerm);
                        con.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            // First result set → Data
                            while (dr.Read())
                            {
                                list.Add(new TenacityGroupMasterModel
                                {
                                    Code = Convert.ToInt32(dr["CODE"]),
                                    Name = dr["NAME"].ToString(),
                                    Description = dr["DESCRIPTION"].ToString()
                                });
                            }

                            // Second result set → TotalCount
                            if (dr.NextResult() && dr.Read())
                            {
                                totalCount = Convert.ToInt32(dr["TotalCount"]);
                            }
                        }
                    }
                }

                return Json(new
                {
                    lists = list,
                    totalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteTenacityGroupMaster(int code)
        {
            var globalVariable = _globalValue.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    con.Open();

                    string query = @"DELETE FROM TENACITY_GRPMAST
                                     WHERE CODE=@CODE
                                     AND COMP_CODE=@COMP_CODE";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
