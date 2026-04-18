using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.ModuleService;
using travelexpensemanagement.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class EarnLeaveOpeningEntryListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
                public EarnLeaveOpeningEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
            ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;

        }

        public IActionResult Index()
        {
            return View("~/Views/Payroll/Master/EarnLeaveOpeningEntryList/Index.cshtml");
        }

        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var EarnLeaveOpeningEntryModel = new List<EarnLeaveOpeningEntryModel>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_EarnLeaveOpeningEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                  
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            EarnLeaveOpeningEntryModel.Add(new EarnLeaveOpeningEntryModel
                            {

                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                V_TYPE = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : string.Empty,
                                EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["EMP_CODE"]) : 0,
                                EmpName = reader["NAME"] != DBNull.Value ? reader["NAME"].ToString() : string.Empty,
                                LEAVE_TYPE = reader["LEAVE_TYPE"] != DBNull.Value ? reader["LEAVE_TYPE"].ToString() : string.Empty,
                                OP_DAYS = reader["OP_DAYS"] != DBNull.Value ? Convert.ToInt32(reader["OP_DAYS"]) : 0,

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
                return Json(new { success = false, message = "Error fetching Earn Leave Opening Entry", error = ex.Message });
            }

            return Json(new { success = true, lists = EarnLeaveOpeningEntryModel, totalCount });
        }

        [HttpGet]
        public IActionResult GetDataByCode(string code)
        {
            var getcode = _globalVariableService.GetGlobalVariables();

            EarnLeaveOpeningEntryModel EarnLeaveOpeningEntryModel = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_EarnLeaveOpeningEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@DOC_ID", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getcode.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE",getcode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                EarnLeaveOpeningEntryModel = new EarnLeaveOpeningEntryModel
                                {
                                    DOC_ID = rdr["DOC_ID"] != DBNull.Value ? rdr["DOC_ID"].ToString() : string.Empty,
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                                    V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                                    V_TYPE = rdr["V_TYPE"] != DBNull.Value ? rdr["V_TYPE"].ToString() : string.Empty,
                                    EMP_CODE = rdr["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["EMP_CODE"]) : 0,
                                    EmpName = rdr["NAME"] != DBNull.Value ? rdr["NAME"].ToString() : string.Empty,
                                    LEAVE_CODE = rdr["LEAVE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["LEAVE_CODE"]) : 0,
                                    LEAVE_TYPE = rdr["LEAVE_TYPE"] != DBNull.Value ? rdr["LEAVE_TYPE"].ToString() : string.Empty,
                                    OP_DAYS = rdr["OP_DAYS"] != DBNull.Value ? Convert.ToInt32(rdr["OP_DAYS"]) : 0

                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = EarnLeaveOpeningEntryModel });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching bank", error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Delete(string code)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_EarnLeaveOpeningEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@DOC_ID", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = " Earn Leave Opening Entry deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Earn Leave Opening Entry.", error = ex.Message });
            }
        }

    }
}
