using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class EInvoiceCancellationApprovalController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        public EInvoiceCancellationApprovalController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
         DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
        }

        public async Task<IActionResult> Index()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            var loadGeneralSetting = await _globalVariableService.LoadGeneralSetting();
            ViewBag.LoadGeneralSetting = loadGeneralSetting;
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;

            return View("~/Views/Sales/Transaction/EInvoiceCancellationApproval/Index.cshtml");
        }
        public JsonResult GetVNo(String doctype)
        {
            string newV_NO = "00000";
            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";
                    string lastV_NO_Query = "select max(V_no) from EINVOICE_CANAPP where V_TYPE=@V_TYPE and COMP_CODE= @CompCode and BRANCH_CODE= @BRANCH_CODE and YEAR_CODE= @YearCode  ";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                    lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@V_TYPE", doctype);
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                    object result = lastVnoCmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        int lastV_NO = Convert.ToInt32(result);
                        newV_NO = (lastV_NO + 1).ToString("D5");
                    }
                    else
                    {
                        newV_NO = prefixYR + "00001";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO });
        }
        public JsonResult DDlVType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('EINVCancelApp') order by Name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlEInvoiceDocument()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select code,Name from DOCTYPE_MAST where code in ('SAGT','SASI','SAST','SART','DNSL','CRSL','ESAG','SATD') order by Name";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        public JsonResult GetData(string V_TYPE)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            var result = new List<object>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {

                    using (SqlCommand cmd = new SqlCommand("sp_EInvoiceCancellationApproval", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = Convert.ToInt32(getdata.PubCompCode);
                        cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = Convert.ToInt32(getdata.PubBranchCode);
                        cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = Convert.ToInt32(getdata.PubFYearCode);
                        cmd.Parameters.Add("@V_TYPE", SqlDbType.NVarChar, 4).Value = string.IsNullOrEmpty(V_TYPE) ? (object)DBNull.Value : V_TYPE;
                        cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 30).Value = "Showdata";
                        con.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new
                                {
                                    V_Type = reader["V_Type"]?.ToString(),
                                    V_No = reader["V_No"]?.ToString(),
                                    V_date = reader["V_date"]?.ToString(),
                                    Namount = reader["Namount"]?.ToString(),
                                    Bill_Name = reader["Bill_Name"]?.ToString(),
                                    Bill_Code = reader["BILL_CODE"]?.ToString(),
                                    BILL_PINCODE = reader["BILL_PINCODE"]?.ToString(),
                                    Bill_GST = reader["Bill_GST"]?.ToString(),
                                    FAPROV_STATUS = reader["FAPROV_STATUS"]?.ToString(),
                                    EINV = reader["EINV"]?.ToString(),
                                    EINVDate = reader["EINVDate"]?.ToString(),
                                    EWB = reader["EWB"]?.ToString(),
                                    EWBDate = reader["EWBDate"]?.ToString(),
                                    CStatus = reader["CStatus"]?.ToString(),
                                    TranType = reader["TranType"]?.ToString(),
                                    IRN = reader["IRN"]?.ToString(),
                                    SHIP_CODE = reader["SHIP_CODE"]?.ToString(),
                                    SHIP_NAME = reader["SHIP_NAME"]?.ToString(),
                                    SHIP_GST = reader["SHIP_GST"]?.ToString(),
                                    SHIP_PINCODE = reader["SHIP_PINCODE"]?.ToString(),
                                    Reason = reader["Reason"]?.ToString()
                                });
                            }
                        }
                    }
                }

                if (result.Count == 0)
                {
                    return Json(new { Success = false, Message = "No data found.", Data = result });
                }

                return Json(new { Success = true, Message = "Data loaded successfully.", Data = result });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SavedData([FromBody] EInvoiceCancellationApproval_Model request)
        {
            if (request == null || request.Details == null || !request.Details.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Input model or details are empty."
                });
            }

            string action = request.Details.FirstOrDefault()?.action ?? "INSERT";

            return SubmitRequest(request.Details, action);
        }

        private JsonResult SubmitRequest(List<EInvoiceCancellationApproval_Detail> Details, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();

                if (Details == null || Details.Count == 0)
                {
                    return Json(new { success = false, message = "No data available." });
                }

                string fappstatus = "";
                string fappRemark = "";

                bool isFinalApprovalBody = false;

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    // UPDATE VALIDATION
                    if (action == "UPDATE")
                    {
                        string sql = @" SELECT STATUS  FROM APPROVAL_STATUS  WHERE V_TYPE = @V_TYPE
                        AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE  AND BRANCH_CODE = @BRANCH_CODE
                        AND YEAR_CODE = @YEAR_CODE AND STATUS = 'OPEN' AND USER_CODE <> @USER_CODE";

                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar).Value = Details[0].V_TYPE;
                            cmd.Parameters.Add("@V_NO", SqlDbType.Int).Value = Details[0].V_NO;
                            cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = g.PubCompCode;
                            cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = g.PubBranchCode;
                            cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = g.PubFYearCode;
                            cmd.Parameters.Add("@USER_CODE", SqlDbType.Int).Value = g.PubUserId;

                            object result = cmd.ExecuteScalar();

                            if (result != null && result != DBNull.Value)
                            {
                                string query = @"
                                SELECT TOP 1 USER_NAME  FROM APPROVAL_STATUS WHERE V_TYPE = @V_TYPE  AND V_NO = @V_NO
                                AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE  AND YEAR_CODE = @YEAR_CODE
                                AND STATUS = 'OPEN' AND USER_CODE <> @USER_CODE  ORDER BY SRNO DESC";

                                using (SqlCommand cmdUser = new SqlCommand(query, conn))
                                {
                                    cmdUser.Parameters.Add("@V_TYPE", SqlDbType.VarChar).Value = Details[0].V_TYPE;
                                    cmdUser.Parameters.Add("@V_NO", SqlDbType.Int).Value = Details[0].V_NO;
                                    cmdUser.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = g.PubCompCode;
                                    cmdUser.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = g.PubBranchCode;
                                    cmdUser.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = g.PubFYearCode;
                                    cmdUser.Parameters.Add("@USER_CODE", SqlDbType.Int).Value = g.PubUserId;

                                    object userResult = cmdUser.ExecuteScalar();


                                    return Json(new { success = false, validation = true, message = "This Document Approval is in process at User: " + g.PubUserName + ", Edit not allowed." });
                                }
                            }
                        }
                    }

                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {

                            string sql = @" SELECT APPROV_USER FROM DOC_APPROSTAGE WHERE USER_CODE = @USER_CODE AND DOC_CODE = @DOC_CODE AND COMP_CODE = @COMP_CODE";

                            using (SqlCommand cmd = new SqlCommand(sql, conn, transaction))
                            {
                                cmd.Parameters.Add("@USER_CODE", SqlDbType.Int).Value = g.PubUserId;
                                cmd.Parameters.Add("@DOC_CODE", SqlDbType.VarChar).Value = Details[0].V_TYPE;
                                cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = g.PubCompCode;

                                object result = cmd.ExecuteScalar();

                                if (result != null && result != DBNull.Value)
                                {
                                    if (result.ToString() == "FINAL")
                                    {
                                        isFinalApprovalBody = true;
                                    }
                                }
                            }

                            if (isFinalApprovalBody == true)
                            {
                                fappstatus = "Approved";
                                fappRemark = "Document Approved.";
                            }

                            string deleteSql = @"DELETE FROM EINVOICE_CANAPP WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE AND 
                            YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE";

                            using (SqlCommand deleteCmd = new SqlCommand(deleteSql, conn, transaction))
                            {
                                deleteCmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = g.PubCompCode;
                                deleteCmd.Parameters.Add("@V_NO", SqlDbType.Int).Value = Details[0].V_NO;
                                deleteCmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = g.PubBranchCode;
                                deleteCmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = g.PubFYearCode;
                                deleteCmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar).Value = Details[0].V_TYPE;

                                deleteCmd.ExecuteNonQuery();
                            }

                            foreach (var detail in Details)
                            {
                                using (SqlCommand cmd = new SqlCommand("sp_EInvoiceCancellationApproval", conn, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@Action", action);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@V_TYPE", (object?)detail.V_TYPE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@V_NO", (object?)detail.V_NO ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@V_DATE", (object?)detail.V_DATE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DOC_ID", $"{detail.V_TYPE}{detail.V_NO}");
                                    cmd.Parameters.AddWithValue("@REF_TYPE", (object?)detail.REF_TYPE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@REF_NO", (object?)detail.REF_NO ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@REF_DATE", (object?)detail.REF_DATE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@INV_STATUS", (object?)detail.INV_STATUS ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EINV_STATUS", (object?)detail.EINV_STATUS ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EINV_DATE", (object?)detail.EINV_DATE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EWB_STATUS", (object?)detail.EWB_STATUS ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EWB_DATE", (object?)detail.EWB_DATE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@BILL_AMT", (object?)detail.BILL_AMT ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@BILL_CODE", (object?)detail.BILL_CODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@BILL_NAME", (object?)detail.BILL_NAME ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@BILL_GST", (object?)detail.BILL_GST ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@BILL_PINCODE", (object?)detail.BILL_PINCODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SHIP_CODE", (object?)detail.SHIP_CODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SHIP_NAME", (object?)detail.SHIP_NAME ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SHIP_GST", (object?)detail.SHIP_GST ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SHIP_PINCODE", (object?)detail.SHIP_PINCODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@REMARKS", (object?)detail.REMARKS ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@IRN", (object?)detail.IRN ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@STATUS", (object?)detail.STATUS ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@REASON", (object?)detail.REASON ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@FAPROV_STATUS", fappstatus);
                                    cmd.Parameters.AddWithValue("@FAPROV_REMARKS", fappRemark);
                                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AED", "A");
                                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                    cmd.Parameters.AddWithValue("@SNO", (object?)detail.SNO ?? DBNull.Value);

                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();

                            return Json(new { success = true, message = "Data saved successfully." });
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}