using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.MonthlyTransaction;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class NoticePeriodPaymentEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public NoticePeriodPaymentEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
            ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/MonthlyTransaction/NoticePeriodPaymentEntry/Index.cshtml");
        }
        public JsonResult GetDocType()
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                string query = @" SELECT ISNULL(MAX(V_no), 0) + 1 AS NextVNo FROM pay_notice WHERE V_TYPE = 'PNOT'  
                AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode";

                var parameters = new[]
                {
                    new SqlParameter("@CompCode", globalVar.PubCompCode),
                    new SqlParameter("@BranchCode", 1),
                    new SqlParameter("@YearCode", globalVar.PubFYearCode),
                };

                int nextVNo = 1;
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddRange(parameters);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            nextVNo = Convert.ToInt32(result);
                        }
                    }
                }
                return Json(new { success = true, nextVNo = nextVNo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public JsonResult GetddlEmpName()
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                string query = $@" SELECT CAST(CODE AS VARCHAR(10)) AS CODE, CAST(CODE AS VARCHAR(10)) + ' | ' + NAME AS NAME FROM EMP_MAST 
                WHERE RESIGN_DATE IS NULL AND JOIN_DATE IS NOT NULL AND ACTIVE = 1 AND COMP_CODE = {globalVar.PubCompCode} ORDER BY NAME;";
                var resultList = _dropdownService.GetDropdownList(query);
                return Json(resultList);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public JsonResult GetddlDepartment()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"Select code, Name From DEPT_MAST where Comp_code = '{globalVar.PubCompCode}'";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        public JsonResult GetddlDesignation()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"Select Code, Name From DESG_MAST where Comp_code = '{globalVar.PubCompCode}'";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        //[HttpPost]
        //public JsonResult SavePayNotice([FromBody] PayNoticeModel model)
        //{
        //    var globalVar = _globalVariableService.GetGlobalVariables();
        //    try
        //    {
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            con.Open();
        //            using (SqlCommand cmd = new SqlCommand("sp_InsertPAYNOTICE", con))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = globalVar.PubCompCode;
        //                cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = 1;
        //                cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = globalVar.PubFYearCode;
        //                cmd.Parameters.Add("@V_TYPE", SqlDbType.NVarChar, 4).Value = "PNOT";
        //                cmd.Parameters.Add("@V_NO", SqlDbType.Int).Value = (object)model.V_NO ?? DBNull.Value;
        //                cmd.Parameters.Add("@DocDate", SqlDbType.Date).Value = (object)model.DocDate ?? DBNull.Value;
        //                cmd.Parameters.Add("@EMP_CODE", SqlDbType.Int).Value = (object)model.EMP_CODE ?? DBNull.Value;
        //                cmd.Parameters.Add("@EmployeeName", SqlDbType.NVarChar, 100).Value = model.EmployeeName ?? "";
        //                cmd.Parameters.Add("@Dep_ID", SqlDbType.Int).Value = (object)model.Dep_ID ?? DBNull.Value;
        //                cmd.Parameters.Add("@Des_ID", SqlDbType.Int).Value = (object)model.Des_ID ?? DBNull.Value;
        //                cmd.Parameters.Add("@ResignationDate", SqlDbType.Date).Value = (object)model.ResignationDate ?? DBNull.Value;
        //                cmd.Parameters.Add("@NoticePeriodStartDate", SqlDbType.Date).Value = (object)model.NoticePeriodStartDate ?? DBNull.Value;
        //                cmd.Parameters.Add("@NoticePeriodEndDate", SqlDbType.Date).Value = (object)model.NoticePeriodEndDate ?? DBNull.Value;
        //                cmd.Parameters.Add("@TotalNoticePeriod", SqlDbType.Int).Value = (object)model.TotalNoticePeriod ?? DBNull.Value;
        //                cmd.Parameters.Add("@DaysServed", SqlDbType.Int).Value = (object)model.DaysServed ?? DBNull.Value;
        //                cmd.Parameters.Add("@DaysNotServed", SqlDbType.Int).Value = (object)model.DaysNotServed ?? DBNull.Value;
        //                cmd.Parameters.Add("@NoticePayAmount", SqlDbType.Decimal).Value = (object)model.NoticePayAmount ?? DBNull.Value;
        //                cmd.Parameters.Add("@PaymentType", SqlDbType.NVarChar, 20).Value = model.PaymentType ?? "";
        //                cmd.Parameters.Add("@Type", SqlDbType.NVarChar, 20).Value = model.Type ?? "";
        //                cmd.Parameters.Add("@GrossSalaryPerDay", SqlDbType.Decimal).Value = (object)model.GrossSalaryPerDay ?? DBNull.Value;
        //                cmd.Parameters.Add("@TotalPayableAmount", SqlDbType.Decimal).Value = (object)model.TotalPayableAmount ?? DBNull.Value;
        //                cmd.Parameters.Add("@Paid", SqlDbType.Int).Value = (object)model.Paid ?? DBNull.Value;
        //                cmd.Parameters.Add("@PreparedBy", SqlDbType.NVarChar, 100).Value = model.PreparedBy ?? "";
        //                cmd.Parameters.Add("@ApprovedBy", SqlDbType.NVarChar, 100).Value = model.ApprovedBy ?? "";
        //                cmd.Parameters.Add("@ApprovalDate", SqlDbType.Date).Value = (object)model.ApprovalDate ?? DBNull.Value;
        //                cmd.Parameters.Add("@Remarks", SqlDbType.NVarChar, -1).Value = model.Remarks ?? "";
        //                cmd.Parameters.Add("@UUSER", SqlDbType.Int).Value = globalVar.PubUserId;
        //                cmd.Parameters.Add("@UDATE", SqlDbType.DateTime).Value = DateTime.Now;
        //                cmd.Parameters.Add("@EUSER", SqlDbType.Int).Value = DBNull.Value;
        //                cmd.Parameters.Add("@EDATE", SqlDbType.DateTime).Value = DBNull.Value;
        //                cmd.Parameters.Add("@AED", SqlDbType.NVarChar, 1).Value = "A";
        //                cmd.Parameters.Add("@WSID", SqlDbType.NVarChar, 30).Value = globalVar.PubWorkStationID.ToString();
        //                cmd.Parameters.Add("@LIP", SqlDbType.NVarChar, 20).Value = globalVar.PubLocalId ?? "";
        //                cmd.Parameters.Add("@LID", SqlDbType.NVarChar, 20).Value = Environment.MachineName ?? "";
        //                cmd.Parameters.Add("@Action", SqlDbType.VarChar, 50).Value = "INSERT";
        //                using (SqlDataReader reader = cmd.ExecuteReader())
        //                {
        //                    if (reader.Read())
        //                    {
        //                        bool success = reader.GetInt32(reader.GetOrdinal("Success")) == 1;
        //                        string message = reader.GetString(reader.GetOrdinal("Message"));
        //                        return Json(new { success, message });
        //                    }
        //                    else
        //                    {
        //                        return Json(new { success = false, message = "No response from database." });
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}

        [HttpPost]
        public IActionResult SavePayNotice([FromBody] PayNoticeModel model)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_InsertPAYNOTICE", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", "PNOT");
                        cmd.Parameters.AddWithValue("@V_NO", (object)model.V_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DocDate", (object)model.DocDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EMP_CODE", (object)model.EMP_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EmployeeName", model.EmployeeName ?? "");
                        cmd.Parameters.AddWithValue("@Dep_ID", (object)model.Dep_ID ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Des_ID", (object)model.Des_ID ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ResignationDate", (object)model.ResignationDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NoticePeriodStartDate", (object)model.NoticePeriodStartDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NoticePeriodEndDate", (object)model.NoticePeriodEndDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TotalNoticePeriod", (object)model.TotalNoticePeriod ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DaysServed", (object)model.DaysServed ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DaysNotServed", (object)model.DaysNotServed ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NoticePayAmount", (object)model.NoticePayAmount ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PaymentType", model.PaymentType ?? "");
                        cmd.Parameters.AddWithValue("@Payment_Mode", model.PaymentMode ?? "");

                        cmd.Parameters.AddWithValue("@Type", model.Type ?? "");
                        cmd.Parameters.AddWithValue("@GrossSalaryPerDay", (object)model.GrossSalaryPerDay ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TotalPayableAmount", (object)model.TotalPayableAmount ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Paid", (object)model.Paid ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PreparedBy", model.PreparedBy ?? "");
                        cmd.Parameters.AddWithValue("@ApprovedBy", model.ApprovedBy ?? "");
                        cmd.Parameters.AddWithValue("@ApprovalDate", (object)model.ApprovalDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? "");
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID.ToString());
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "");
                        cmd.Parameters.AddWithValue("@Action", model.Action);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bool success = reader.GetInt32(reader.GetOrdinal("Success")) == 1;
                                string message = reader.GetString(reader.GetOrdinal("Message"));
                                return Json(new { success, message });
                            }
                            else
                            {
                                return Json(new { success = false, message = "No response from database." });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult GetNoticeDetails(string searchCode, string v_TYPE, string v_NO)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();
                    string query = @"SELECT TOP 1 
                    COMP_CODE, BRANCH_CODE, YEAR_CODE, V_TYPE, V_NO, DocDate,    
                    EMP_CODE, EmployeeName, Dep_ID, Des_ID,    
                    ResignationDate, NoticePeriodStartDate, NoticePeriodEndDate, TotalNoticePeriod,    
                    DaysServed, DaysNotServed, NoticePayAmount, PaymentType, Type, GrossSalaryPerDay,    
                    TotalPayableAmount, Paid, PreparedBy, ApprovedBy, ApprovalDate, Remarks,
                    UUSER, UDATE, EUSER, EDATE, AED, WSID, LIP, LID, Payment_Mode
                    FROM PAY_NOTICE  WHERE V_TYPE = @v_TYPE AND V_NO = @v_NO AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        //cmd.Parameters.AddWithValue("@searchCode", searchCode);
                        cmd.Parameters.AddWithValue("@v_TYPE", v_TYPE);
                        cmd.Parameters.AddWithValue("@v_NO", v_NO);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var record = new
                                {
                                    COMP_CODE = reader["COMP_CODE"].ToString(),
                                    BRANCH_CODE = reader["BRANCH_CODE"].ToString(),
                                    YEAR_CODE = reader["YEAR_CODE"].ToString(),
                                    V_TYPE = reader["V_TYPE"].ToString(),
                                    V_NO = reader["V_NO"].ToString(),
                                    DocDate = reader["DocDate"].ToString(),
                                    EMP_CODE = reader["EMP_CODE"].ToString(),
                                    EmployeeName = reader["EmployeeName"].ToString(),
                                    Dep_ID = reader["Dep_ID"].ToString(),
                                    Des_ID = reader["Des_ID"].ToString(),
                                    ResignationDate = reader["ResignationDate"].ToString(),
                                    NoticePeriodStartDate = reader["NoticePeriodStartDate"].ToString(),
                                    NoticePeriodEndDate = reader["NoticePeriodEndDate"].ToString(),
                                    TotalNoticePeriod = reader["TotalNoticePeriod"].ToString(),
                                    DaysServed = reader["DaysServed"].ToString(),
                                    DaysNotServed = reader["DaysNotServed"].ToString(),
                                    NoticePayAmount = reader["NoticePayAmount"].ToString(),
                                    PaymentType = reader["PaymentType"].ToString(),
                                    Type = reader["Type"].ToString(),
                                    GrossSalaryPerDay = reader["GrossSalaryPerDay"].ToString(),
                                    TotalPayableAmount = reader["TotalPayableAmount"].ToString(),
                                    Paid = reader["Paid"].ToString(),
                                    PreparedBy = reader["PreparedBy"].ToString(),
                                    ApprovedBy = reader["ApprovedBy"].ToString(),
                                    ApprovalDate = reader["ApprovalDate"].ToString(),
                                    Remarks = reader["Remarks"].ToString(),
                                    UUSER = reader["UUSER"].ToString(),
                                    UDATE = reader["UDATE"].ToString(),
                                    EUSER = reader["EUSER"].ToString(),
                                    EDATE = reader["EDATE"].ToString(),
                                    AED = reader["AED"].ToString(),
                                    WSID = reader["WSID"].ToString(),
                                    LIP = reader["LIP"].ToString(),
                                    LID = reader["LID"].ToString(),
                                    PaymentMode = reader["Payment_Mode"].ToString()
                                };

                                return Json(record);
                            }
                            else
                            {
                                return Json(new { message = "No record found." });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { message = "Error: " + ex.Message });
            }
        }


    }
}
