using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Monthly_Transaction;
namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class EarnLeavePaymentListController : Controller
    {


        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private int? userLevel;
        public EarnLeavePaymentListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }


        public IActionResult Index()
        {
            return View("~/Views/Payroll/MonthlyTransaction/EarnLeavePaymentList/Index.cshtml");
        }



        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var EarnLeavePaymentEntr = new List<EarnLeavePaymentEntry_Model>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_PAY_LEAVEBAL", conn))
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
                            EarnLeavePaymentEntr.Add(new EarnLeavePaymentEntry_Model
                            {
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                V_DATE =  reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : (DateTime?)null,
                                V_TYPE = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : string.Empty,
                                EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["EMP_CODE"]) : 0,
                                Emp_name = reader["Emp_name"] != DBNull.Value ? reader["Emp_name"].ToString() : string.Empty,
                                BAL_DAYS = reader["BAL_DAYS"] != DBNull.Value ? Convert.ToInt32(reader["BAL_DAYS"]) : 0,
                                PAY_DAYS = reader["PAY_DAYS"] != DBNull.Value ? Convert.ToInt32(reader["PAY_DAYS"]) : 0,
                                GROSS = reader["GROSS"] != DBNull.Value ? Convert.ToInt32(reader["GROSS"]) : 0,
                                RATE = reader["RATE"] != DBNull.Value ? Convert.ToInt32(reader["RATE"]) : 0,
                                AMOUNT = reader["AMOUNT"] != DBNull.Value ? Convert.ToInt32(reader["AMOUNT"]) : 0,
                                UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
                                UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : (DateTime?)null,
                                EUSER = reader["EUSER"] != DBNull.Value ? Convert.ToInt32(reader["EUSER"]) : 0,
                                EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : (DateTime?)null,


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

            return Json(new { success = true, lists = EarnLeavePaymentEntr, totalCount });
        }

        [HttpGet]
        public IActionResult GetdatabyCode(string code)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            EarnLeavePaymentEntry_Model EarnLeavePaymentEntry_Model = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PAY_LEAVEBAL", con))
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
                                EarnLeavePaymentEntry_Model = new EarnLeavePaymentEntry_Model
                                {

                                    DOC_ID = rdr["DOC_ID"] != DBNull.Value ? rdr["DOC_ID"].ToString() : null,
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                                    V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                                    Emp_name = rdr["empname"] != DBNull.Value ? rdr["empname"].ToString() : null,
                                    EMP_CODE = rdr["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["EMP_CODE"]) : 0,
                                    BAL_DAYS = rdr["BAL_DAYS"] != DBNull.Value ? Convert.ToInt32(rdr["BAL_DAYS"]) : 0,
                                    PAY_DAYS = rdr["PAY_DAYS"] != DBNull.Value ? Convert.ToInt32(rdr["PAY_DAYS"]) : 0,
                                    GROSS = rdr["GROSS"] != DBNull.Value ? Convert.ToDecimal(rdr["GROSS"]) : 0,
                                    RATE = rdr["RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE"]) : 0,
                                    AMOUNT = rdr["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["AMOUNT"]) : 0,
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = EarnLeavePaymentEntry_Model });
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
                    using (SqlCommand cmd = new SqlCommand("sp_PAY_LEAVEBAL", con))
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

                return Json(new { success = true, message = " Earn Leave Payment Entry deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting  Earn Leave Payment Entry.", error = ex.Message });
            }
        }



    }
}
