using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.PayRoll;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class AttendenceEntryListController : Controller
    {


        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private int? userLevel;
        public AttendenceEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }



        public IActionResult Index()
        {
            return View("~/Views/Payroll/Transaction/AttendenceEntryList/Index.cshtml");
        }


        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var AttendenceEntry = new List<AttendenceEntry>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_AttendencesEntry", conn))
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
                            AttendenceEntry.Add(new AttendenceEntry
                            {
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_TYPE = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : string.Empty,
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : (DateTime?)null,
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

            return Json(new { success = true, lists = AttendenceEntry, totalCount });
        }



        [HttpPost]
        public JsonResult Delete(string code)
        {
            var globalvariable = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_AttendencesEntry", con))
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

                return Json(new { success = true, message = "Attendences Entry deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Attendences Entry.", error = ex.Message });
            }
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
                    using (SqlCommand cmd = new SqlCommand("sp_AttendencesEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "showdata");
                        cmd.Parameters.AddWithValue("@ShowActionOption", "HEADER");
                        cmd.Parameters.AddWithValue("@DOC_ID", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", "ATIN");

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
                    using (SqlCommand cmd2 = new SqlCommand("sp_AttendencesEntry", con))
                    {
                        cmd2.CommandType = CommandType.StoredProcedure;
                        cmd2.Parameters.AddWithValue("@Action", "showdata");
                        cmd2.Parameters.AddWithValue("@ShowActionOption", "Details");
                        cmd2.Parameters.AddWithValue("@DOC_ID", code);
                        cmd2.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd2.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd2.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd2.Parameters.AddWithValue("@V_TYPE", "ATIN");

                        using (SqlDataReader rdr = cmd2.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                var detailItem = new Dictionary<string, object>
                                {
                                    ["DOC_ID"] = rdr["DOC_ID"] == DBNull.Value ? null : rdr["DOC_ID"],
                                    ["V_TYPE"] = rdr["V_TYPE"] == DBNull.Value ? null : rdr["V_TYPE"],
                                    ["V_NO"] = rdr["V_NO"] == DBNull.Value ? null : rdr["V_NO"],
                                    ["V_DATE"] = rdr["V_DATE"] == DBNull.Value ? null : rdr["V_DATE"],
                                    ["SNO"] = rdr["SNO"] == DBNull.Value ? null : rdr["SNO"],
                                    ["dept"] = rdr["dept"] == DBNull.Value ? null : rdr["dept"],
                                    ["EMP_CODE"] = rdr["EMP_CODE"] == DBNull.Value ? null : rdr["EMP_CODE"],
                                    ["emp"] = rdr["emp"] == DBNull.Value ? null : rdr["emp"],
                                    ["desg"] = rdr["desg"] == DBNull.Value ? null : rdr["desg"],
                                    ["STATUS"] = rdr["STATUS"] == DBNull.Value ? null : rdr["STATUS"],
                                    ["SHIFT"] = rdr["SHIFT"] == DBNull.Value ? null : rdr["SHIFT"],
                                    ["REMARK"] = rdr["REMARK"] == DBNull.Value ? null : rdr["REMARK"],
                                    ["OFFDAY"] = rdr["OFFDAY"] == DBNull.Value ? null : rdr["OFFDAY"]
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


    }
}
