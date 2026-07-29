using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces;

namespace travelexpensemanagement.Controllers
{
    public class ApprovalController : Controller
    {
        private readonly IApprovalService _approvalService;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly DropdownService _dropdownService;

        public ApprovalController(IApprovalService approvalService, GlobalVariableService globalVariableService, DataBaseConnection dbConnection, DropdownService dropdownService)
        {
            _approvalService = approvalService;
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
            _dropdownService = dropdownService;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CheckApprovalStatus(string v_type,int v_no, string tableName)
        {
            string status = await _approvalService.GetApprovalStatus(v_type, v_no, tableName);

            return Json(new { success = true, message = status });
        }
        public JsonResult DDlSendTo(string v_type)
        {
            //v_type = "DCHL";
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT  a.USER_CODE, b.FULL_NAME FROM DOC_APPROSTAGE a LEFT JOIN CONDATABASE.dbo.USER_MAST b " +
                " ON a.USER_CODE = b.CODE WHERE b.Active = 1  AND a.User_Code <> " + getdata.PubUserId + " " +
                " AND a.DOC_CODE = '" + v_type + "'    AND a.comp_code = " + getdata.PubCompCode + "  " +
                " ORDER BY a.SRNO;";
                var DDlSendTo = _dropdownService.GetDropdownList(query);
                return Json(DDlSendTo);
            }
        }
        public JsonResult DDlApprovalRemark()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select DISTINCT  CODE,NAME from APPROVAL_RMKS Order by code";
                var DDlApprovalRemark = _dropdownService.GetDropdownList(query);
                return Json(DDlApprovalRemark);
            }
        }
        public JsonResult DDlForwordTo(string v_type, int v_no)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"
                    SELECT a.USER_CODE AS Value, b.FULL_NAME AS Text
                    FROM DOC_APPROSTAGE a
                    LEFT JOIN CONDATABASE.dbo.USER_MAST b
                    ON a.USER_CODE = b.CODE
                    LEFT JOIN CONDATABASE.dbo.SUBUSER_MAST c
                    ON b.CODE = c.USER_CODE
                    AND c.COMP_CODE = @CompCode
                    WHERE a.USER_CODE <> @UserCode
                    AND a.DOC_CODE = @VType
                    AND a.COMP_CODE = @CompCode and  b.FULL_NAME <> '' ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@UserCode", getdata.PubUserId);
                    cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                    cmd.Parameters.AddWithValue("@FYearCode", getdata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@VNo", v_no);
                    cmd.Parameters.AddWithValue("@VType", v_type);

                    con.Open();

                    var list = new List<object>();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new
                            {
                                value = dr["Value"].ToString(),
                                text = dr["Text"].ToString()
                            });
                        }
                    }
                    return Json(list);
                }
            }
        }

        public JsonResult DDlAPPStatus()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            string approvUser = "";
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string approvQuery = @"SELECT TOP 1 APPROV_USER FROM DOC_APPROSTAGE WHERE USER_CODE = @UserCode";
                using (SqlCommand cmd = new SqlCommand(approvQuery, con))
                {
                    cmd.Parameters.AddWithValue("@UserCode", getdata.PubUserId);
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    approvUser = result?.ToString()?.Trim() ?? "";
                    con.Close();
                }
            }
            string query;
            if (approvUser.Equals("FINAL", StringComparison.OrdinalIgnoreCase))
            {
                query = @"SELECT Code, Name FROM DOCSTATUS_MAST WHERE V_TYPE = 'Approval' ORDER BY Code";
            }
            else
            {
                query = @"SELECT Code, Name FROM DOCSTATUS_MAST WHERE V_TYPE = 'Approval' AND Code <> 8 ORDER BY Code";
            }
            var ddlAppStatus = _dropdownService.GetDropdownList(query);
            return Json(ddlAppStatus);
        }

        [HttpPost]
        public async Task<IActionResult> CheckPendingUser(int vNo, string vType)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    // 1. Existing Pending User Check
                    using (SqlCommand cmd = new SqlCommand(@"SELECT TOP 1 USER_CODE FROM APPROVAL_STATUS
                        WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND STATUS = 'Open'
                        AND USER_CODE <> @CURRENT_USER", con))
                    {
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@CURRENT_USER", gv.PubUserId);
                        var pendingUser = await cmd.ExecuteScalarAsync();

                        if (pendingUser != null)
                        {
                            return Json(new { success = false, userCode = pendingUser.ToString() });
                        }
                    }
                    // 2. Approval_Code = 8 Check
                    using (SqlCommand cmd = new SqlCommand(@"SELECT COUNT(*) FROM APPROVAL_STATUS WHERE V_NO = @V_NO
                    AND V_TYPE = @V_TYPE AND USER_CODE = @CURRENT_USER AND Approval_Code = 5", con))
                    {
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@CURRENT_USER", gv.PubUserId);
                        int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        return Json(new
                        {
                            success = true,
                            approvalCode8 = count > 0
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new {  success = false,  message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendForApproval([FromBody] SendApprovalModel model)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    // 1. GET MENU DETAILS FROM MENU_MAST
                    int menuCode = 0;
                    string webFormMainName = "";

                    using (SqlCommand menuCmd = new SqlCommand(
                        @"SELECT TOP 1 Code, WebFORM_MainName  FROM MENU_MAST
                        WHERE LTRIM(RTRIM(WebFORM_MainName)) = @FormName", con))
                    {
                        menuCmd.Parameters.AddWithValue("@FormName", model.FromName);

                        using (SqlDataReader dr = await menuCmd.ExecuteReaderAsync())
                        {
                            if (await dr.ReadAsync())
                            {
                                menuCode = Convert.ToInt32(dr["Code"]);
                                webFormMainName = Convert.ToString(dr["WebFORM_MainName"]);
                            }
                            else
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = "Menu not found in MENU_MAST"
                                });
                            }
                        }
                    }
                    // 2. GET DOCUMENT DETAILS
                    string doc_id = model.DocType + model.DocNo;
                    int originCode = 0;
                    DateTime? sendDate = null;
                    DateTime? originDate = null;
                    string docName = "";

                    string query = $@"
                        SELECT 
                        G.V_TYPE,
                        G.V_NO,
                        G.DOC_ID,
                        G.UUSER AS ORIGIN_CODE,
                        G.UDATE AS ORIGIN_DATE,
                        G.EDATE AS SEND_DATE,
                        ISNULL(D.NAME,'') AS DOC_NAME
                        FROM {model.tableName} G
                        LEFT JOIN DOCTYPE_MAST D ON D.CODE = G.V_TYPE
                        WHERE G.V_NO = @V_NO
                        AND G.V_TYPE = @V_TYPE
                        AND G.YEAR_CODE = @YEAR_CODE
                        AND G.BRANCH_CODE = @BRANCH_CODE
                        AND G.COMP_CODE = @COMP_CODE";

                    using (SqlCommand fetchCmd = new SqlCommand(query, con))
                    {
                        fetchCmd.Parameters.AddWithValue("@V_NO", model.DocNo);
                        fetchCmd.Parameters.AddWithValue("@V_TYPE", model.DocType);
                        fetchCmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        fetchCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        fetchCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        using (SqlDataReader dr = await fetchCmd.ExecuteReaderAsync())
                        {
                            if (await dr.ReadAsync())
                            {
                                originCode = Convert.ToInt32(dr["ORIGIN_CODE"]);

                                if (dr["ORIGIN_DATE"] != DBNull.Value)
                                    originDate = Convert.ToDateTime(dr["ORIGIN_DATE"]);

                                if (dr["SEND_DATE"] != DBNull.Value)
                                    sendDate = Convert.ToDateTime(dr["SEND_DATE"]);

                                docName = dr["DOC_NAME"].ToString();
                            }
                            else
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = "Document not found"
                                });
                            }
                        }
                    }
                    // 3. CALL STORED PROCEDURE
                    using (SqlCommand cmd = new SqlCommand("sp_SendForApproval", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        // From MENU_MAST
                        cmd.Parameters.AddWithValue("@MENU_CODE", menuCode);
                        cmd.Parameters.AddWithValue("@FORM_NAME", webFormMainName);

                        cmd.Parameters.AddWithValue("@DEPARTMENT", "");
                        cmd.Parameters.AddWithValue("@ORIGIN_CODE", originCode);
                        cmd.Parameters.AddWithValue("@ORIGIN_NAME", "");
                        cmd.Parameters.AddWithValue("@ORIGIN_DATE", (object)originDate ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@SEND_CODE", gv.PubUserId);
                        cmd.Parameters.AddWithValue("@SEND_NAME", gv.PubUserName);

                        cmd.Parameters.AddWithValue("@USER_CODE", Convert.ToInt32(model.SendTo));
                        cmd.Parameters.AddWithValue("@USER_NAME", "");

                        cmd.Parameters.AddWithValue("@DOC_NAME", docName);
                        cmd.Parameters.AddWithValue("@DOC_ID", doc_id);

                        cmd.Parameters.AddWithValue("@V_TYPE", model.DocType);
                        cmd.Parameters.AddWithValue("@V_NO", model.DocNo);

                        cmd.Parameters.Add("@V_DATE", SqlDbType.DateTime)
                            .Value = (object)sendDate ?? DBNull.Value;

                        cmd.Parameters.Add("@SEND_DATE", SqlDbType.DateTime)
                            .Value = DateTime.Now;

                        cmd.Parameters.AddWithValue("@STATUS", "Open");
                        cmd.Parameters.AddWithValue("@APPROVAL_CODE", 0);
                        cmd.Parameters.AddWithValue("@APPROVAL_REMARK", model.Remarks ?? "");
                        cmd.Parameters.AddWithValue("@PREORITY_CODE", 1);
                        cmd.Parameters.AddWithValue("@PREORITY", "Normal");
                        cmd.Parameters.AddWithValue("@NEW_MODIFY", "N");
                        cmd.Parameters.AddWithValue("@MSG_STS", 0);
                        cmd.Parameters.AddWithValue("@REQUESTID", 0);

                        cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new
                {
                    success = true,
                    message = "Approval sent successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        //[HttpPost]
        //public async Task<IActionResult> SendForApproval([FromBody] SendApprovalModel model)
        //{
        //    try
        //    {
        //        var gv = _globalVariableService.GetGlobalVariables();
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            await con.OpenAsync();

        //            string doc_id = model.DocType + model.DocNo;
        //            int originCode = 0;
        //            DateTime? sendDate = null;
        //            DateTime? originDate = null;
        //            string docName = "";

        //            string query = $@"SELECT G.V_TYPE, G.V_NO, G.DOC_ID, G.UUSER AS ORIGIN_CODE, G.UDATE AS ORIGIN_DATE,
        //               G.EDATE AS SEND_DATE, ISNULL(D.NAME,'') AS DOC_NAME FROM {model.tableName} G LEFT JOIN DOCTYPE_MAST D ON D.CODE = G.V_TYPE
        //               WHERE G.V_NO = @V_NO AND G.V_TYPE = @V_TYPE AND G.YEAR_CODE = @YEAR_CODE AND G.BRANCH_CODE = @BRANCH_CODE
        //               AND G.COMP_CODE = @COMP_CODE";

        //            using (SqlCommand fetchCmd = new SqlCommand(query, con))
        //            {
        //                fetchCmd.Parameters.AddWithValue("@V_NO", model.DocNo);
        //                fetchCmd.Parameters.AddWithValue("@V_TYPE", model.DocType);
        //                fetchCmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
        //                fetchCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
        //                fetchCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

        //                using (SqlDataReader dr = await fetchCmd.ExecuteReaderAsync())
        //                {
        //                    if (await dr.ReadAsync())
        //                    {
        //                        originCode = Convert.ToInt32(dr["ORIGIN_CODE"]);
        //                        if (dr["ORIGIN_DATE"] != DBNull.Value)
        //                            originDate = Convert.ToDateTime(dr["ORIGIN_DATE"]);
        //                        if (dr["SEND_DATE"] != DBNull.Value)
        //                            sendDate = Convert.ToDateTime(dr["SEND_DATE"]);
        //                        docName = dr["DOC_NAME"].ToString();
        //                    }
        //                    else
        //                    {
        //                        return Json(new
        //                        {
        //                            success = false,
        //                            message = "Document not found"
        //                        });
        //                    }
        //                }
        //            }
        //            // 3. CALL STORED PROCEDURE
        //            using (SqlCommand cmd = new SqlCommand("sp_SendForApproval", con))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;
        //                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
        //                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
        //                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
        //                cmd.Parameters.AddWithValue("@MENU_CODE", 1);
        //                cmd.Parameters.AddWithValue("@DEPARTMENT", "");
        //                cmd.Parameters.AddWithValue("@ORIGIN_CODE", originCode);
        //                cmd.Parameters.AddWithValue("@ORIGIN_NAME", "");
        //                cmd.Parameters.AddWithValue("@ORIGIN_DATE", (object)originDate ?? DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SEND_CODE", gv.PubUserId);
        //                cmd.Parameters.AddWithValue("@SEND_NAME", gv.PubUserName);
        //                cmd.Parameters.AddWithValue("@USER_CODE", Convert.ToInt32(model.SendTo));
        //                cmd.Parameters.AddWithValue("@USER_NAME", "");

        //                cmd.Parameters.AddWithValue("@FORM_NAME", "Gate Entry");
        //                cmd.Parameters.AddWithValue("@DOC_NAME", docName);
        //                cmd.Parameters.AddWithValue("@DOC_ID", doc_id);

        //                cmd.Parameters.AddWithValue("@V_TYPE", model.DocType);
        //                cmd.Parameters.AddWithValue("@V_NO", model.DocNo);

        //                cmd.Parameters.Add("@V_DATE", SqlDbType.DateTime).Value = (object)sendDate ?? DBNull.Value;
        //                cmd.Parameters.Add("@SEND_DATE", SqlDbType.DateTime).Value = DateTime.Now;

        //                cmd.Parameters.AddWithValue("@STATUS", "Open");
        //                cmd.Parameters.AddWithValue("@APPROVAL_CODE", 0);
        //                cmd.Parameters.AddWithValue("@APPROVAL_REMARK", model.Remarks);
        //                cmd.Parameters.AddWithValue("@PREORITY_CODE", 1);
        //                cmd.Parameters.AddWithValue("@PREORITY", "Normal");
        //                cmd.Parameters.AddWithValue("@NEW_MODIFY", "N");
        //                cmd.Parameters.AddWithValue("@MSG_STS", 0);
        //                cmd.Parameters.AddWithValue("@REQUESTID", 0);

        //                cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
        //                cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
        //                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

        //                await cmd.ExecuteNonQueryAsync();
        //            }
        //        }

        //        return Json(new
        //        {
        //            success = true,
        //            message = "Approval sent successfully"
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> SubmitApproval([FromBody] ApprovalModel model)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_SubmitApproval", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                        cmd.Parameters.AddWithValue("@TableName", model.TableName);
                        cmd.Parameters.AddWithValue("@ApprovalStatus", model.ApprovalStatus ?? 0);
                        cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? "");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ForwardTo", model.ForwardTo ?? 0);
                        cmd.Parameters.AddWithValue("@UserCode", gv.PubUserId);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return Json(new
                                {
                                    success = Convert.ToBoolean(reader["Success"]),
                                    message = reader["Message"].ToString()
                                });
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = false,
                    message = "No response received from procedure."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        //[HttpPost]
        //public async Task<IActionResult> SubmitApproval([FromBody] ApprovalModel model)
        //{
        //    try
        //    {
        //        var gv = _globalVariableService.GetGlobalVariables();
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            await con.OpenAsync();
        //            using (SqlTransaction tran = con.BeginTransaction())
        //            {
        //                try
        //                {
        //                    // 1. Update APPROVAL_STATUS
        //                    string approvalQuery = @"
        //                UPDATE APPROVAL_STATUS
        //                SET STATUS = 'CLOSE',
        //                    APPROVAL_CODE = @ApprovalCode,
        //                   remarks = @Remarks,
        //                   close_date= @close_date
        //                WHERE V_NO = @V_NO
        //                  AND V_TYPE = @V_TYPE
        //                  AND COMP_CODE = @COMP_CODE
        //                  AND YEAR_CODE = @YEAR_CODE
        //                  AND BRANCH_CODE = @BRANCH_CODE
        //                  AND STATUS = 'Open'";

        //                    using (SqlCommand cmd = new SqlCommand(approvalQuery, con, tran))
        //                    {
        //                        cmd.Parameters.AddWithValue("@ApprovalCode", model.ApprovalStatus ?? 0);
        //                        cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? "");
        //                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
        //                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
        //                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
        //                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
        //                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
        //                        cmd.Parameters.AddWithValue("@close_date", DateTime.Now);

        //                        await cmd.ExecuteNonQueryAsync();
        //                    }

        //                    // 2. Update document table (GATE1)
        //                    string gateQuery = $@"
        //                UPDATE {model.TableName}
        //                SET FAPROV_STATUS = 'Approved',
        //                FAPROV_REMARKS = @FAPROV_REMARKS
        //                WHERE V_NO = @V_NO
        //                  AND V_TYPE = @V_TYPE
        //                  AND COMP_CODE = @COMP_CODE
        //                  AND YEAR_CODE = @YEAR_CODE
        //                  AND BRANCH_CODE = @BRANCH_CODE";

        //                    using (SqlCommand cmd = new SqlCommand(gateQuery, con, tran))
        //                    {
        //                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
        //                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
        //                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
        //                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
        //                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
        //                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", model.Remarks);

        //                        await cmd.ExecuteNonQueryAsync();
        //                    }

        //                    tran.Commit();

        //                    return Json(new
        //                    {
        //                        success = true,
        //                        message = "Approval submitted successfully."
        //                    });
        //                }
        //                catch
        //                {
        //                    tran.Rollback();
        //                    throw;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}



    }
    public class ApprovalModel
    {
        public string? V_TYPE { get; set; }

        public int? V_NO { get; set; }

        public string? TableName { get; set; }

        public int? ApprovalStatus { get; set; }

        public int? ForwardTo { get; set; }

        public string? Remarks { get; set; }
    }

    public class SendApprovalModel
    {
        public string? DocType { get; set; }
        public int? DocNo { get; set; }
        public string? SendTo { get; set; }
        public string? Remarks { get; set; }
        public string? tableName { get; set; }
        public string? FromName { get; set; }
    }
}