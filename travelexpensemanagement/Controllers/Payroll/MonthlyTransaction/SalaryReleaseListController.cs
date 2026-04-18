using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.MonthlyTransaction;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{


    public class SalaryReleaseListController : Controller
    {


        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private int? userLevel;
        public SalaryReleaseListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
            ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }


        public IActionResult Index()
        {
            return View("~/Views/Payroll/MonthlyTransaction/SalaryReleaseList/Index.cshtml");
        }


        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var GatePassEntry = new List<SalaryReleaseEntry_Model>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_SalaryReleaseEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);


                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            GatePassEntry.Add(new SalaryReleaseEntry_Model
                            {
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_TYPE = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : string.Empty,
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : (DateTime?)null,
                                RELEASE_DATE = reader["RELEASE_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["RELEASE_DATE"]) : (DateTime?)null,
                                Emp_name = reader["Emp_name"] != DBNull.Value ? reader["Emp_name"].ToString() : string.Empty,
                                AMOUNT = reader["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["AMOUNT"]) : 0,
                                REMARK = reader["REMARK"] != DBNull.Value ? reader["REMARK"].ToString() : string.Empty,


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

            return Json(new { success = true, lists = GatePassEntry, totalCount });
        }

        [HttpGet]
        public IActionResult GetDataByCode(string code)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();

            var headerData = new Dictionary<string, object>();
            var detailDataList = new List<Dictionary<string, object>>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // Header
                    using (SqlCommand cmd = new SqlCommand("sp_SalaryReleaseEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "showdata");
                        cmd.Parameters.AddWithValue("@ShowActionOption", "HEADER");
                        cmd.Parameters.AddWithValue("@DOC_ID", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                headerData["DOC_ID"] = rdr["DOC_ID"] == DBNull.Value ? null : rdr["DOC_ID"];
                                headerData["V_TYPE"] = rdr["V_TYPE"] == DBNull.Value ? null : rdr["V_TYPE"];
                                headerData["V_NO"] = rdr["V_NO"] == DBNull.Value ? null : rdr["V_NO"];
                                headerData["V_DATE"] = rdr["V_DATE"] == DBNull.Value ? null : rdr["V_DATE"];
                            }
                        }
                    }

                    // Details
                    using (SqlCommand cmd2 = new SqlCommand("sp_SalaryReleaseEntry", con))
                    {
                        cmd2.CommandType = CommandType.StoredProcedure;
                        cmd2.Parameters.AddWithValue("@Action", "showdata");
                        cmd2.Parameters.AddWithValue("@ShowActionOption", "Details");
                        cmd2.Parameters.AddWithValue("@DOC_ID", code);
                        cmd2.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd2.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd2.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);

                        using (SqlDataReader rdr = cmd2.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                var detailItem = new Dictionary<string, object>
                                {

                                    ["SNO"] = rdr["SNO"] == DBNull.Value ? null : rdr["SNO"],
                                    ["EMP_CODE"] = rdr["EMP_CODE"] == DBNull.Value ? null : rdr["EMP_CODE"],
                                    ["NAME"] = rdr["NAME"] == DBNull.Value ? null : rdr["NAME"],
                                    ["AMOUNT"] = rdr["AMOUNT"] == DBNull.Value ? null : rdr["AMOUNT"],
                                    ["REMARK"] = rdr["REMARK"] == DBNull.Value ? null : rdr["REMARK"],
                                    ["RELEASE_DATE"] = rdr["RELEASE_DATE"] == DBNull.Value ? null : rdr["RELEASE_DATE"]

                                };

                                detailDataList.Add(detailItem);
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        Header = headerData,
                        Details = detailDataList
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching data", error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Delete(string code)
        {
            var globalvariable = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_SalaryReleaseEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@DOC_ID", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalvariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalvariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Salary Release Entry  deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting  Salary Release Entry .", error = ex.Message });
            }
        }






    }
}
