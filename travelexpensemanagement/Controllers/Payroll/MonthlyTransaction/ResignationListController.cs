using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Monthly_Transaction;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class ResignationListController : Controller
    {
                private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private int? userLevel;
        public ResignationListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Payroll/MonthlyTransaction/ResignationList/Index.cshtml");
        }

        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var ResignationEntry = new List<ResignationEntry_Model>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_PAY_RESIGN", conn))
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
                            ResignationEntry.Add(new ResignationEntry_Model
                            {
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                                            V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : (DateTime?)null,
                                EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["EMP_CODE"]) : 0,
                                Emp_name = reader["Emp_name"] != DBNull.Value ? reader["Emp_name"].ToString() : string.Empty,
                                RESIGN_REASON = reader["RESIGN_REASON"] != DBNull.Value ? reader["RESIGN_REASON"].ToString() : string.Empty,
                                status = reader["status"] != DBNull.Value ? Convert.ToInt32(reader["status"]) : 0,
                                RESIGN_DATE = reader["RESIGN_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["RESIGN_DATE"]) : (DateTime?)null,
                              
                                RELIEVING_DATE = reader["RELIEVING_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["RELIEVING_DATE"]) : (DateTime?)null,
                                LAST_WORK_DATE = reader["LAST_WORK_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["LAST_WORK_DATE"]) : (DateTime?)null,
                                REMARKS = reader["REMARKS"] != DBNull.Value ? reader["REMARKS"].ToString() : string.Empty,
                                ATTACH1 = reader["ATTACH1"] != DBNull.Value ? reader["ATTACH1"].ToString() : string.Empty,
                                ATTACH2 = reader["ATTACH2"] != DBNull.Value ? reader["ATTACH2"].ToString() : string.Empty,
                                ATTACH3 = reader["ATTACH3"] != DBNull.Value ? reader["ATTACH3"].ToString() : string.Empty

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

            return Json(new { success = true, lists = ResignationEntry, totalCount });
        }


        [HttpGet]
        public IActionResult GetdatabyCode(string code)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            ResignationEntry_Model ResignationEntry_Model = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PAY_RESIGN", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "showdata");
                        cmd.Parameters.AddWithValue("@DOC_ID", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);

                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                ResignationEntry_Model = new ResignationEntry_Model
                                {
                                    DOC_ID = rdr["DOC_ID"] != DBNull.Value ? rdr["DOC_ID"].ToString() : null,
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                                    V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                                    EMP_CODE = rdr["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["EMP_CODE"]) : 0,
                                    status = rdr["status"] != DBNull.Value ? Convert.ToInt32(rdr["status"]) : 0,
                                    RESIGN_DATE = rdr["RESIGN_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["RESIGN_DATE"]) : DateTime.MinValue,
                                    RELIEVING_DATE = rdr["RELIEVING_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["RELIEVING_DATE"]) : DateTime.MinValue,
                                    LAST_WORK_DATE = rdr["LAST_WORK_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["LAST_WORK_DATE"]) : DateTime.MinValue,
                                    RESIGN_REASON = rdr["RESIGN_REASON"] != DBNull.Value ? rdr["RESIGN_REASON"].ToString() : null,
                                    REMARKS = rdr["REMARKS"] != DBNull.Value ? rdr["REMARKS"].ToString() : null,
                                    ATTACH1 = rdr["ATTACH1"] != DBNull.Value ? rdr["ATTACH1"].ToString() : null,
                                    ATTACH2 = rdr["ATTACH2"] != DBNull.Value ? rdr["ATTACH2"].ToString() : null,
                                    ATTACH3 = rdr["ATTACH3"] != DBNull.Value ? rdr["ATTACH3"].ToString() : null



                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = ResignationEntry_Model });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching bank", error = ex.Message });
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
                    using (SqlCommand cmd = new SqlCommand("sp_PAY_RESIGN", con))
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

                return Json(new { success = true, message = " Resignation  Entry deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting  Resignation  Entry.", error = ex.Message });
            }
        }











    }
}
