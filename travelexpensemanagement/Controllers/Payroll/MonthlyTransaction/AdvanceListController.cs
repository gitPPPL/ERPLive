using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using TravelExpenseManagement.Models.Payroll.Monthly_Transaction;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class AdvanceListController : Controller
    {


        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private int? userLevel;
        public AdvanceListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Payroll/MonthlyTransaction/AdvanceList/Index.cshtml");
        }



        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var AdvanceEntry = new List<AdvanceEntry_Model>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_AdvanceEntry", conn))
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
                            AdvanceEntry.Add(new AdvanceEntry_Model
                            {
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                V_TYPE = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : string.Empty,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : (DateTime?)null,
                                EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["EMP_CODE"]) : 0,
                                Emp_name = reader["Emp_name"] != DBNull.Value ? reader["Emp_name"].ToString() : string.Empty,
                                AMOUNT = reader["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["AMOUNT"]) : 0,
                                INSTALLMENT = reader["INSTALLMENT"] != DBNull.Value ? Convert.ToDecimal(reader["INSTALLMENT"]) : 0,
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

            return Json(new { success = true, lists = AdvanceEntry, totalCount });
        }




        [HttpGet]
        public IActionResult GetdatabyCode(string code)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            AdvanceEntry_Model AdvanceEntry_Model = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_AdvanceEntry", con))
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
                                AdvanceEntry_Model = new AdvanceEntry_Model
                                {

                                    DOC_ID = rdr["DOC_ID"] != DBNull.Value ? rdr["DOC_ID"].ToString() : null,
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                                    V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                                    Emp_name = rdr["empname"] != DBNull.Value ? rdr["empname"].ToString() : null,
                                    EMP_CODE = rdr["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["EMP_CODE"]) : 0,
                                    AMOUNT = rdr["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["AMOUNT"]) : 0,
                                    INSTALLMENT = rdr["INSTALLMENT"] != DBNull.Value ? Convert.ToDecimal(rdr["INSTALLMENT"]) : 0,
                                    REMARK = rdr["REMARK"] != DBNull.Value ? rdr["REMARK"].ToString() : null,




                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = AdvanceEntry_Model });
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
                    using (SqlCommand cmd = new SqlCommand("sp_AdvanceEntry", con))
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

                return Json(new { success = true, message = " Advance Entry deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting  Advance Entry.", error = ex.Message });
            }
        }






    }
}
