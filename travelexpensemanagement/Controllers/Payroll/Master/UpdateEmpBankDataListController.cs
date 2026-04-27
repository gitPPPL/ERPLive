using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class UpdateEmpBankDataListController : Controller
    {

        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public UpdateEmpBankDataListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbcontext;
            _globalVariableService = globalValue;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Emp Bank Data";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Payroll/Master/UpdateEmpBankDataList/Index.cshtml", model);
        }

        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var UpdateEmpBankDataModel = new List<UpdateEmpBankDataModel>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_UpdateEmpBankDataList", conn))
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
                            UpdateEmpBankDataModel.Add(new UpdateEmpBankDataModel
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["EMP_CODE"]) : 0,
                                EMP_Name = reader["EmployeeName"] != DBNull.Value ? reader["EmployeeName"].ToString() : string.Empty,
                                BANK_NAME = reader["BANK_NAME"] != DBNull.Value ? reader["BANK_NAME"].ToString() : string.Empty,
                                BRANCH = reader["BRANCH"] != DBNull.Value ? reader["BRANCH"].ToString() : string.Empty,
                                AC_NO = reader["AC_NO"] != DBNull.Value ? reader["AC_NO"].ToString() : string.Empty,
                                IFSC_CODE = reader["IFSC_CODE"] != DBNull.Value ? reader["IFSC_CODE"].ToString() : string.Empty,
                                AC_TYPE = reader["AC_TYPE"] != DBNull.Value ? reader["AC_TYPE"].ToString() : string.Empty,
                                BANK_VERIFY = reader["bank_verify"] != DBNull.Value ? reader["bank_verify"].ToString() : string.Empty,
                               
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
                return Json(new { success = false, message = "Error fetching Update Emp Bank Data", error = ex.Message });
            }

            return Json(new { success = true, lists = UpdateEmpBankDataModel, totalCount });
        }

        [HttpGet]
        public IActionResult GetDataByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            UpdateEmpBankDataModel UpdateEmpBankDataModel = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateEmpBankDataList", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                UpdateEmpBankDataModel = new UpdateEmpBankDataModel
                                {
                                    CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    EMP_CODE = rdr["Emp_code"] != DBNull.Value ? Convert.ToInt32(rdr["Emp_code"]) : 0,
                                    EMP_Name = rdr["EmployeeName"] != DBNull.Value ? Convert.ToString(rdr["EmployeeName"]) : "",
                                    BANK_NAME = rdr["BANK_NAME"] != DBNull.Value ? Convert.ToString(rdr["BANK_NAME"]) : "",
                                    BANK_CODE = rdr["BANK_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BANK_CODE"]) : 0,
                                    BRANCH = rdr["BRANCH"] != DBNull.Value ? Convert.ToString(rdr["BRANCH"]) : "",
                                    AC_NO = rdr["AC_NO"] != DBNull.Value ? Convert.ToString(rdr["AC_NO"]) : "",
                                    IFSC_CODE = rdr["IFSC_CODE"] != DBNull.Value ? Convert.ToString(rdr["IFSC_CODE"]) : "",
                                    AC_TYPE = rdr["AC_TYPE"] != DBNull.Value ? Convert.ToString(rdr["AC_TYPE"]) : "",
                                    BANK_VERIFY = rdr["bank_verify"] != DBNull.Value ? Convert.ToString(rdr["bank_verify"]) : "",
                                    FileName = rdr["FileName"] != DBNull.Value ? Convert.ToString(rdr["FileName"]) : "",
                                    Filepath = rdr["Filepath"] != DBNull.Value ? Convert.ToString(rdr["Filepath"]) : ""
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = UpdateEmpBankDataModel });
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
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateEmpBankDataList", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Update Emp Bank Data  Master deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Update Emp Bank Data Master.", error = ex.Message });
            }
        }

    }
}
