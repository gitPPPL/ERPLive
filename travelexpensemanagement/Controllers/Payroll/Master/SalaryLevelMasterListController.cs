using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.PayRoll;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class SalaryLevelMasterListController : Controller
    {

        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public SalaryLevelMasterListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbcontext;
            _globalVariableService = globalValue;
            _moduleService = moduleService;
        }


        public IActionResult Index()
        {

            ViewBag.CurrentMenu = "Salary Level Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Payroll/Master/SalaryLevelMasterList/Index.cshtml", model);
        }

        public IActionResult GetSalaryLevel(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var SalaryLevelList = new List<SalaryLevelModel>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_SalaryLevel", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SalaryLevelList.Add(new SalaryLevelModel
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                NAME = reader["NAME"] != DBNull.Value ? reader["NAME"].ToString() : string.Empty,
                                BASIC = reader["BASIC"] != DBNull.Value ? Convert.ToInt32(reader["BASIC"]) : 0,
                                HRA = reader["HRA"] != DBNull.Value ? Convert.ToInt32(reader["HRA"]) : 0,
                                CONV = reader["CONV"] != DBNull.Value ? Convert.ToInt32(reader["CONV"]) : 0,
                                OTHERS = reader["OTHERS"] != DBNull.Value ? Convert.ToInt32(reader["OTHERS"]) : 0,
                                TOT_AMT = reader["TOT_AMT"] != DBNull.Value ? Convert.ToInt32(reader["TOT_AMT"]) : 0,
                                GW_AMT = reader["GW_AMT"] != DBNull.Value ? Convert.ToInt32(reader["GW_AMT"]) : 0,

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
                return Json(new { success = false, message = "Error fetching categories", error = ex.Message });
            }

            return Json(new { success = true, lists = SalaryLevelList, totalCount });
        }

        [HttpGet]
        public IActionResult GetsalarylevelByCode(int code)
        {
            var getvariable = _globalVariableService.GetGlobalVariables();


            SalaryLevelModel SalaryLevelModel = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_SalaryLevel", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getvariable.PubCompCode);

                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                SalaryLevelModel = new SalaryLevelModel
                                {
                                    CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    NAME = rdr["NAME"] != DBNull.Value ? rdr["NAME"].ToString() : null,
                                    BASIC = rdr["BASIC"] != DBNull.Value ? Convert.ToInt32(rdr["BASIC"]) : 0,
                                    HRA = rdr["HRA"] != DBNull.Value ? Convert.ToInt32(rdr["HRA"]) : 0,
                                    CONV = rdr["CONV"] != DBNull.Value ? Convert.ToInt32(rdr["CONV"]) : 0,
                                    OTHERS = rdr["OTHERS"] != DBNull.Value ? Convert.ToInt32(rdr["OTHERS"]) : 0,
                                    TOT_AMT = rdr["TOT_AMT"] != DBNull.Value ? Convert.ToInt32(rdr["TOT_AMT"]) : 0,
                                    GW_AMT = rdr["GW_AMT"] != DBNull.Value ? Convert.ToInt32(rdr["GW_AMT"]) : 0,
                                    Active = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0

                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = SalaryLevelModel });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching bank", error = ex.Message });
            }
        }



        [HttpPost]
        public JsonResult Delete(int code)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_SalaryLevel", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            con.Open();
                            cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = " Salary Level Master deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting  Salary Level Master.", error = ex.Message });
            }
        }






    }
}
