using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Transaction;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class ManualOutEntryListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public ManualOutEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }





        public IActionResult Index()
        {
            return View("~/Views/Payroll/Transaction/ManualOutEntryList/Index.cshtml");
        }


        public IActionResult Getlist(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var PAY_INOUT = new List<PAY_INOUT>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_PAY_INOUT", conn))
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
                            PAY_INOUT.Add(new PAY_INOUT
                            {
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : null,
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_TYPE = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : null,

                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                SHIFT = reader["SHIFT"] != DBNull.Value ? reader["SHIFT"].ToString() : null,
                                EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["EMP_CODE"]) : 0,
                                EMP_NAME = reader["EMP_NAME"] != DBNull.Value ? reader["EMP_NAME"].ToString() : null,
                                DEPT_NAME = reader["DEPT_NAME"] != DBNull.Value ? reader["DEPT_NAME"].ToString() : null,
                                REMARKS = reader["REMARKS"] != DBNull.Value ? reader["REMARKS"].ToString() : null,
                                E_TIME = reader["E_TIME"] != DBNull.Value ? reader["E_TIME"].ToString() : null,
                                IN_TIME = reader["IN_TIME"] != DBNull.Value ? reader["IN_TIME"].ToString() : null,
                                GP_NO = reader["GP_NO"] != DBNull.Value ? reader["GP_NO"].ToString() : null,
                                HOD_NAME = reader["HOD_NAME"] != DBNull.Value ? reader["HOD_NAME"].ToString() : null,
                                GP_TYPE = reader["GP_TYPE"] != DBNull.Value ? reader["GP_TYPE"].ToString() : null,
                                GP_HRS = reader["GP_HRS"] != DBNull.Value ? Convert.ToInt32(reader["GP_HRS"]) : 0
                                                            
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
                return Json(new { success = false, message = "Error fetching Qualification Master", error = ex.Message });
            }

            return Json(new { success = true, lists = PAY_INOUT, totalCount });
        }

        [HttpGet]
        public IActionResult GetdataByCode(string code)
        {
            var getvariable = _globalVariableService.GetGlobalVariables();


            PAY_INOUT PAY_INOUT = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PAY_INOUT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ShowData");
                        cmd.Parameters.AddWithValue("@DOC_ID", code);
                        cmd.Parameters.AddWithValue("COMP_CODE", getvariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getvariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);



                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                PAY_INOUT = new PAY_INOUT
                                {
                                    DOC_ID = rdr["DOC_ID"] != DBNull.Value ? rdr["DOC_ID"].ToString() : null,
                                    V_TYPE = rdr["V_TYPE"] != DBNull.Value ? rdr["V_TYPE"].ToString() : null,
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                                    V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                                    SHIFT = rdr["SHIFT"] != DBNull.Value ? rdr["SHIFT"].ToString() : null,
                                    EMP_CODE = rdr["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["EMP_CODE"]) : 0,
                                    DEPT_CODE = rdr["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_CODE"]) : 0,
                                    DEPT_NAME = rdr["DEPT_NAME"] != DBNull.Value ? rdr["DEPT_NAME"].ToString() : null,
                                    EMP_NAME = rdr["EMP_NAME"] != DBNull.Value ? rdr["EMP_NAME"].ToString() : null,
                                    REMARKS = rdr["REMARKS"] != DBNull.Value ? rdr["REMARKS"].ToString() : null,
                                    E_TIME = rdr["E_TIME"] != DBNull.Value ? rdr["E_TIME"].ToString() : null,
                                    IN_TIME = rdr["IN_TIME"] != DBNull.Value ? rdr["IN_TIME"].ToString() : null,
                                    GP_NO = rdr["GP_NO"] != DBNull.Value ? rdr["GP_NO"].ToString() : null,
                                    HOD_CODE = rdr["HOD_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["HOD_CODE"]) : 0,
                                    GP_TYPE = rdr["GP_TYPE"] != DBNull.Value ? rdr["GP_TYPE"].ToString() : null,
                                    GP_HRS =  rdr["GP_HRS"] != DBNull.Value ? Convert.ToInt32(rdr["GP_HRS"]) : 0,
                                    REASON_CODE = rdr["REASON_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["REASON_CODE"]) : 0,

                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = PAY_INOUT });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching bank", error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Delete(string code )
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PAY_INOUT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@DOC_ID", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Manual Out Entry  deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Manual Out Entry .", error = ex.Message });
            }
        }







    }
}
