using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class EmployeeCategoryMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public EmployeeCategoryMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbcontext;
            _globalVariableService = globalValue;
            _moduleService = moduleService;
        }


        public IActionResult Index()
        {


            ViewBag.CurrentMenu = "Employee Category Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };



            return View("~/Views/Payroll/Master/EmployeeCategoryMasterList/Index.cshtml", model);
        }
        public IActionResult GetEmployeeCategaryList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var EmployeeCategoryList = new List<EmployeeCategoryModel>();
            int totalCount = 0;
            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_EmployeeCategory", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            EmployeeCategoryList.Add(new EmployeeCategoryModel
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                NAME = reader["NAME"] != DBNull.Value ? reader["NAME"].ToString() : string.Empty,
                                SHORTNAME = reader["SHORTNAME"] != DBNull.Value ? reader["SHORTNAME"].ToString() : string.Empty,
                                Active = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching Employee Category", error = ex.Message });
            }

            return Json(new { success = true, lists = EmployeeCategoryList, totalCount });
        }
        [HttpGet]
        public IActionResult GetEmployeecategoryByCode(int code)
        {
            var getvariable = _globalVariableService.GetGlobalVariables();


            EmployeeCategoryModel EmployeeCategoryModel = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_EmployeeCategory", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                EmployeeCategoryModel = new EmployeeCategoryModel
                                {
                                    CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    NAME = rdr["NAME"] != DBNull.Value ? rdr["NAME"].ToString() : null,
                                    SHORTNAME = rdr["SHORTNAME"] != DBNull.Value ? rdr["SHORTNAME"].ToString() : null,
                                    Active = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0

                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = EmployeeCategoryModel });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching bank", error = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult Delete(int code)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_EmployeeCategory", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Employee Category Master deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting  Employee Category Master.", error = ex.Message });
            }
        }

    }
}
