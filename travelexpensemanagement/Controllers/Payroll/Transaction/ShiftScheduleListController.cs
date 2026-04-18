using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class ShiftScheduleListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private int? userLevel;
        public ShiftScheduleListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }


        public IActionResult Index()
        {
            return View("~/Views/Payroll/Transaction/ShiftScheduleList/Index.cshtml");
        }


        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var ShiftSchedule_Model = new List<ShiftSchedule_Model>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_ShiftSchedule", conn))
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
                            ShiftSchedule_Model.Add(new ShiftSchedule_Model
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

            return Json(new { success = true, lists = ShiftSchedule_Model, totalCount });
        }

        [HttpPost]
        public JsonResult Delete(string code)
        {
            var globalvariable = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ShiftSchedule", con))
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

                return Json(new { success = true, message = "Shift Schedule deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Shift Schedule.", error = ex.Message });
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
                    using (SqlCommand cmd = new SqlCommand("sp_ShiftSchedule", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "showdata");
                        cmd.Parameters.AddWithValue("@searchOption", "HEADER");
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
                    using (SqlCommand cmd2 = new SqlCommand("sp_ShiftSchedule", con))
                    {
                        cmd2.CommandType = CommandType.StoredProcedure;
                        cmd2.Parameters.AddWithValue("@Action", "showdata");
                        cmd2.Parameters.AddWithValue("@searchOption", "Details");
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

                                    ["V_TYPE"] = rdr["V_TYPE"] == DBNull.Value ? null : rdr["V_TYPE"],
                                    ["DOCTYPE_NAME"] = rdr["DOCTYPE_NAME"] == DBNull.Value ? null : rdr["DOCTYPE_NAME"],
                                    ["V_NO"] = rdr["V_NO"] == DBNull.Value ? null : rdr["V_NO"],
                                    ["V_DATE"] = rdr["V_DATE"] == DBNull.Value ? null : rdr["V_DATE"],
                                    ["DOC_ID"] = rdr["DOC_ID"] == DBNull.Value ? null : rdr["DOC_ID"],
                                    ["EMP_CODE"] = rdr["EMP_CODE"] == DBNull.Value ? null : rdr["EMP_CODE"],
                                    ["EMP_NAME"] = rdr["EMP_NAME"] == DBNull.Value ? null : rdr["EMP_NAME"],
                                    ["SR"] = rdr["SR"] == DBNull.Value ? null : rdr["SR"],
                                    ["S1"] = rdr["S1"] == DBNull.Value ? null : rdr["S1"],
                                    ["S2"] = rdr["S2"] == DBNull.Value ? null : rdr["S2"],
                                    ["S3"] = rdr["S3"] == DBNull.Value ? null : rdr["S3"],
                                    ["S4"] = rdr["S4"] == DBNull.Value ? null : rdr["S4"],
                                    ["S5"] = rdr["S5"] == DBNull.Value ? null : rdr["S5"],
                                    ["S6"] = rdr["S6"] == DBNull.Value ? null : rdr["S6"],
                                    ["S7"] = rdr["S7"] == DBNull.Value ? null : rdr["S7"],
                                    ["S8"] = rdr["S8"] == DBNull.Value ? null : rdr["S8"],
                                    ["S9"] = rdr["S9"] == DBNull.Value ? null : rdr["S9"],
                                    ["S10"] = rdr["S10"] == DBNull.Value ? null : rdr["S10"],
                                    ["S11"] = rdr["S11"] == DBNull.Value ? null : rdr["S11"],
                                    ["S12"] = rdr["S12"] == DBNull.Value ? null : rdr["S12"],
                                    ["S13"] = rdr["S13"] == DBNull.Value ? null : rdr["S13"],
                                    ["S14"] = rdr["S14"] == DBNull.Value ? null : rdr["S14"],
                                    ["S15"] = rdr["S15"] == DBNull.Value ? null : rdr["S15"],
                                    ["S16"] = rdr["S16"] == DBNull.Value ? null : rdr["S16"],
                                    ["S17"] = rdr["S17"] == DBNull.Value ? null : rdr["S17"],
                                    ["S18"] = rdr["S18"] == DBNull.Value ? null : rdr["S18"],
                                    ["S19"] = rdr["S19"] == DBNull.Value ? null : rdr["S19"],
                                    ["S20"] = rdr["S20"] == DBNull.Value ? null : rdr["S20"],
                                    ["S21"] = rdr["S21"] == DBNull.Value ? null : rdr["S21"],
                                    ["S22"] = rdr["S22"] == DBNull.Value ? null : rdr["S22"],
                                    ["S23"] = rdr["S23"] == DBNull.Value ? null : rdr["S23"],
                                    ["S24"] = rdr["S24"] == DBNull.Value ? null : rdr["S24"],
                                    ["S25"] = rdr["S25"] == DBNull.Value ? null : rdr["S25"],
                                    ["S26"] = rdr["S26"] == DBNull.Value ? null : rdr["S26"],
                                    ["S27"] = rdr["S27"] == DBNull.Value ? null : rdr["S27"],
                                    ["S28"] = rdr["S28"] == DBNull.Value ? null : rdr["S28"],
                                    ["S29"] = rdr["S29"] == DBNull.Value ? null : rdr["S29"],
                                    ["S30"] = rdr["S30"] == DBNull.Value ? null : rdr["S30"],
                                    ["S31"] = rdr["S31"] == DBNull.Value ? null : rdr["S31"],
                                    ["off1"] = rdr["off1"] == DBNull.Value ? null : rdr["off1"],
                                    ["off2"] = rdr["off2"] == DBNull.Value ? null : rdr["off2"],
                                    ["off3"] = rdr["off3"] == DBNull.Value ? null : rdr["off3"],
                                    ["off4"] = rdr["off4"] == DBNull.Value ? null : rdr["off4"],
                                    ["off5"] = rdr["off5"] == DBNull.Value ? null : rdr["off5"],
                                    ["UUSER"] = rdr["UUSER"] == DBNull.Value ? null : rdr["UUSER"],
                                    ["UDATE"] = rdr["UDATE"] == DBNull.Value ? null : rdr["UDATE"],


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
