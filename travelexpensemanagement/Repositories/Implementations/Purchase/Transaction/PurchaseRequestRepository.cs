using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using StackExchange.Redis;
using System.Data;
using System.Threading.Tasks;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseRequestModel;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class PurchaseRequestRepository : IPurchaseRequestRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly LogService.LogService _logService;
        public PurchaseRequestRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _logService = logService;
        }
        public RepositoryResponseData<bool> CheckIsApprovalBody()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            int result = 0;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "IsApprovalBody");
                    cmd.Parameters.AddWithValue("@USER_CODE", getdata.PubUserId);
                    cmd.Parameters.AddWithValue("@V_TYPE", "STPI");
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);

                    con.Open();
                    object queryResult = cmd.ExecuteScalar();

                    if (queryResult != null && queryResult != DBNull.Value)
                    {
                        result = Convert.ToInt32(queryResult);
                    }
                }
            }

            return new RepositoryResponseData<bool> { data = result == 1 };
        }

        public async Task<RepositoryResponseData<bool>> CheckIsFinalApprovalBodyAsync()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                string approvUser = null;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "IsFinalApprovalBody");
                        cmd.Parameters.AddWithValue("@USER_CODE", gv.PubUserId);
                        cmd.Parameters.AddWithValue("@V_TYPE", "STPI");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        con.Open();
                        object result = await cmd.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            approvUser = result.ToString();
                        }
                    }
                }

                return new RepositoryResponseData<bool> { status = true, data = approvUser == "FINAL" };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<decimal?> GetApporxiateRate(int itemCode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            decimal? approxRate = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "GetItemApproxRate");
                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);


                    con.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        approxRate = Convert.ToDecimal(result);
                    }
                }
            }
            return new RepositoryResponseData<decimal?> { data = approxRate };
        }

        public RepositoryResponseData<decimal?> GetPendingQty(int itemCode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            decimal? PendingQty = null;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                //string query = @" SELECT sum(isnull(Qty,0)-isnull(ADJ_QTY,0)) AS RemainingQty FROM ORDER2 WHERE 
                //ITEM_CODE = @Itemcode AND Status = 1 AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode ";

                using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "GetItemPendingQty");
                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);

                    con.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        PendingQty = Convert.ToDecimal(result);
                    }
                }
            }

            return new RepositoryResponseData<decimal?> { data = PendingQty };
        }

        public RepositoryResponseData<decimal?> GetTotalQty(int itemCode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            decimal? Total_Qty = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "GetItemOpenReqQty");
                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        Total_Qty = Convert.ToDecimal(result);
                    }
                }
            }

            return new RepositoryResponseData<decimal?> { data = Total_Qty };
        }

        public RepositoryResponseData<string> GetTECH_DESC(int itemCode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            string? TECH_DESC = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "GetItemTech_Desc");
                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);

                    con.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        TECH_DESC = Convert.ToString(result);
                    }
                }
            }

            return new RepositoryResponseData<string> { data = TECH_DESC };
        }
        
        public RepositoryResponseData<decimal?> GetCurrentStock(int itemCode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            decimal? CurrentStocklist = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "GetItemCurr_Stk");
                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    //cmd.Parameters.AddWithValue("@BranchCode", 1);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        CurrentStocklist = Convert.ToDecimal(result);
                    }
                }
            }

            return new RepositoryResponseData<decimal?> { data = CurrentStocklist };
        }
        
        public RepositoryResponseData<decimal> GetAvgConsumption(int itemCode, DateTime vDate)
        {
            var globalVars = _globalVariableService.GetGlobalVariables();
            if (vDate <= DateTime.MinValue.AddDays(90))
            {
                return new RepositoryResponseData<decimal> { data = 0, message = "Invalid date provided." };
            }
            DateTime endDate = vDate;
            DateTime startDate = vDate.AddDays(-90);
            decimal avgConsumption = 0;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                //string query = @"Select isnull(sum(qty),0) from ISSUE2 where V_TYPE='SICO' and 
                //  item_code= @ItemCode and COMP_CODE=@CompCode   and BRANCH_CODE=@BranchCode   and v_date  between  @StartDate and @EndDate ";

                using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "GetItemAvg_Cons");
                    cmd.Parameters.Add("@ITEM_CODE", SqlDbType.Int).Value = itemCode;
                    cmd.Parameters.Add("@COMP_CODE", SqlDbType.VarChar).Value = globalVars.PubCompCode;
                    cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = globalVars.PubBranchCode;
                    cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;
                    cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate;

                    con.Open();

                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        avgConsumption = Convert.ToDecimal(result);

                        avgConsumption = avgConsumption / 3;

                    }
                }
            }
            return new RepositoryResponseData<decimal> { data = avgConsumption };
        }
        
        public async Task<RepositoryResponse> SaveData(PurchaseRequest_model request)
        {
            if (request?.Header == null)
                return new RepositoryResponse { status = false, message = "Input model is null" };

            var action = request.Header.action == "INSERT" ? "Insert" : "Update";
            var result = await SubmitRequest(request.Header, request.ItamDetails, request.PurchaseDocuments, action);

            return result == "Success"
                ? new RepositoryResponse { status = true }
                : new RepositoryResponse { status = false, message = result };
        }

        private async Task<string> SubmitRequest(Header header, List<ItamDetails> itamDetails, List<PurchaseRequestModel.PurchaseDocuments> purchaseDocuments, string action)
        {
            {
                try
                {
                    var g = _globalVariableService.GetGlobalVariables();

                    using (SqlConnection conn = _dbConnection.GetErpConnection())
                    {
                        conn.Open();
                        using (SqlTransaction tran = conn.BeginTransaction())
                        {
                            try
                            {
                                string deletePRequest2Sql = @"
                                    DELETE FROM PREQUEST2 
                                    WHERE COMP_CODE = @CompCode 
                                    AND V_NO = @VNo 
                                    AND BRANCH_CODE = @BranchCode 
                                    AND YEAR_CODE = @YearCode and  V_TYPE = 'STPI';";
                                using (var deletePRequest2Cmd = conn.CreateCommand())
                                {
                                    deletePRequest2Cmd.Transaction = tran;
                                    deletePRequest2Cmd.CommandText = deletePRequest2Sql;
                                    deletePRequest2Cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                                    deletePRequest2Cmd.Parameters.AddWithValue("@VNo", header.V_NO);
                                    deletePRequest2Cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                                    deletePRequest2Cmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);
                                    deletePRequest2Cmd.ExecuteNonQuery();
                                }

                                string deleteImgTableSql = @"
                                    DELETE FROM IMG_TABLE 
                                    WHERE COMP_CODE = @CompCode 
                                    AND V_NO = @VNo 
                                    AND BRANCH_CODE = @BranchCode 
                                    AND V_TYPE = @V_TYPE
                                    AND YEAR_CODE = @YearCode;";
                                using (var deleteImgTableCmd = conn.CreateCommand())
                                {
                                    deleteImgTableCmd.Transaction = tran;
                                    deleteImgTableCmd.CommandText = deleteImgTableSql;
                                    deleteImgTableCmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                                    deleteImgTableCmd.Parameters.AddWithValue("@VNo", header.V_NO);
                                    deleteImgTableCmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                                    deleteImgTableCmd.Parameters.AddWithValue("@V_TYPE", "STPI");
                                    deleteImgTableCmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);
                                    deleteImgTableCmd.ExecuteNonQuery();
                                }

                                //conn.Close();

                                //conn.Open();
                                using (var cmd = new SqlCommand("sp_PurchaseReq1", conn, tran))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@Action", action);
                                    cmd.Parameters.AddWithValue("@SaveAction", "Header");
                                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@v_NO", header.V_NO);
                                    cmd.Parameters.AddWithValue("@V_TYPE", "STPI");
                                    cmd.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                                    cmd.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE ?? "STPI") + header.V_NO);
                                    cmd.Parameters.AddWithValue("@DEPT_CODE", header.DEPT_CODE);
                                    cmd.Parameters.AddWithValue("@TARGET_DATE", header.TARGET_DATE ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@REASON", header.REASON ?? "");
                                    cmd.Parameters.AddWithValue("@PLACE_CODE", header.PLACE_CODE);
                                    cmd.Parameters.AddWithValue("@URGENT_REQUEST", header.URGENT_REQUEST);
                                    cmd.Parameters.AddWithValue("@status", header.STATUS);
                                    cmd.Parameters.AddWithValue("@OWNER_CODE", header.OWNER_CODE);
                                    cmd.Parameters.AddWithValue("@OWNER_NAME", header.OWNER_NAME);
                                    cmd.Parameters.AddWithValue("@PLAN_NO", header.PLAN_NO);
                                    cmd.Parameters.AddWithValue("@PLAN_TYPE", header.PLAN_TYPE ?? "");
                                    cmd.Parameters.AddWithValue("@REMARKS", header.REMARKS ?? "");
                                    cmd.Parameters.AddWithValue("@FAPROV_STATUS", header.FAPROV_STATUS ?? "");
                                    cmd.Parameters.AddWithValue("@FAPROV_REMARKS", header.FAPROV_REMARKS ?? "");
                                    cmd.Parameters.AddWithValue("@USER_CODE", g.PubUserId);
                                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AED", "A");
                                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                    cmd.ExecuteNonQuery();
                                }

                                /// save dateails 

                                foreach (var d in itamDetails)
                                {
                                    if (!d.ITEM_CODE.HasValue || d.ITEM_CODE == 0)
                                        continue;
                                    using var cmd2 = new SqlCommand("sp_PurchaseReq1", conn, tran) { CommandType = CommandType.StoredProcedure };
                                    cmd2.Parameters.AddWithValue("@Action", "INSERT");
                                    cmd2.Parameters.AddWithValue("@SaveAction", "table");
                                    cmd2.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                                    cmd2.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                                    cmd2.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                                    cmd2.Parameters.AddWithValue("@V_NO", header.V_NO);
                                    cmd2.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                                    cmd2.Parameters.AddWithValue("@V_TYPE", "STPI");
                                    cmd2.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE ?? "STPI") + header.V_NO);
                                    cmd2.Parameters.AddWithValue("@ITEM_CODE", d.ITEM_CODE);
                                    cmd2.Parameters.AddWithValue("@MAKE_CODE", d.MAKE_CODE);
                                    cmd2.Parameters.AddWithValue("@DEPT_CODE", header.DEPT_CODE);
                                    cmd2.Parameters.AddWithValue("@TECH_DESC", d.TECH_DESC ?? "");
                                    cmd2.Parameters.AddWithValue("@UOM_CODE", d.UOM_CODE);
                                    cmd2.Parameters.AddWithValue("@STD_REQ", d.STD_REQ);
                                    cmd2.Parameters.AddWithValue("@CUR_STK", d.CUR_STK);
                                    cmd2.Parameters.AddWithValue("@AVG_CONS", d.AVG_CONS ?? 0);
                                    cmd2.Parameters.AddWithValue("@RESERVE_QTY", d.RESERVE_QTY);
                                    cmd2.Parameters.AddWithValue("@OPEN_POQTY", d.OPEN_POQTY);
                                    cmd2.Parameters.AddWithValue("@OPEN_RQQTY", d.OPEN_RQQTY);
                                    cmd2.Parameters.AddWithValue("@USER_QTY", d.USER_QTY);
                                    cmd2.Parameters.AddWithValue("@REQ_QTY", d.REQ_QTY);
                                    cmd2.Parameters.AddWithValue("@REQ_REASON", d.REQ_REASON ?? "");
                                    cmd2.Parameters.AddWithValue("@REMARKS", d.REMARKS ?? "");
                                    cmd2.Parameters.AddWithValue("@PLACE_USE", d.PLACE_USE ?? "");
                                    cmd2.Parameters.AddWithValue("@PLACE_USECODE", d.PLACE_Code);
                                    cmd2.Parameters.AddWithValue("@APROX_RATE", d.APROX_RATE);

                                    cmd2.Parameters.AddWithValue("@PRIORITY_CODE", d.PRIORITY_CODE ?? 0);
                                    cmd2.Parameters.AddWithValue("@PRIORITY_TYPE", d.PRIORITY_TYPE ?? "");

                                    cmd2.Parameters.AddWithValue("@SCRAP_TYPE", d.SCRAP_TYPE ?? "");

                                    cmd2.Parameters.AddWithValue("@WORK_TYPECODE", d.WORK_TYPECODE ?? 0);
                                    cmd2.Parameters.AddWithValue("@WORK_TYPE", d.WORK_TYPE ?? "");

                                    cmd2.Parameters.AddWithValue("@APROV_CODE", d.APROV_CODE ?? 0);
                                    cmd2.Parameters.AddWithValue("@APROV_STATUS", d.APROV_STATUS ?? "");

                                    cmd2.Parameters.AddWithValue("@APROV_REMARKS", d.APROV_REMARKS ?? "");

                                    cmd2.Parameters.AddWithValue("@MONTHLY", d.MONTHLY ?? "");

                                    cmd2.Parameters.AddWithValue("@STATUS", d.STATUS);
                                    cmd2.Parameters.AddWithValue("@UUSER", g.PubUserId);
                                    cmd2.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                    cmd2.Parameters.AddWithValue("@EUSER", g.PubUserId);
                                    cmd2.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                    cmd2.Parameters.AddWithValue("@AED", "A");
                                    cmd2.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                                    cmd2.Parameters.AddWithValue("@LIP", g.PubLocalId);
                                    cmd2.Parameters.AddWithValue("@LID", Environment.MachineName);
                                    cmd2.ExecuteNonQuery();
                                }

                                int i = 1;
                                foreach (var Attachment in purchaseDocuments)
                                {
                                    if (string.IsNullOrWhiteSpace(Attachment.FILE_NAME))
                                        continue;

                                    byte[] fileBytes = Convert.FromBase64String(Attachment.FILE_DATA);

                                    using var cmd3 = new SqlCommand("sp_PurchaseReq1", conn, tran)
                                    {
                                        CommandType = CommandType.StoredProcedure
                                    };

                                    cmd3.Parameters.AddWithValue("@Action", "INSERT");
                                    cmd3.Parameters.AddWithValue("@SaveAction", "Documnets");
                                    cmd3.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                                    cmd3.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                                    cmd3.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                                    cmd3.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE ?? "STPI") + header.V_NO);
                                    cmd3.Parameters.AddWithValue("@V_NO", header.V_NO);
                                    cmd3.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                                    cmd3.Parameters.AddWithValue("@V_TYPE", "STPI");
                                    cmd3.Parameters.AddWithValue("@ROWID", i);

                                    cmd3.Parameters.AddWithValue("@FILE_NAME", Attachment.FILE_NAME);
                                    cmd3.Parameters.AddWithValue("@FILE_Path", Attachment.FILE_NAME);
                                    cmd3.Parameters.Add("@IMG_FILE", SqlDbType.VarBinary).Value = fileBytes;

                                    cmd3.Parameters.AddWithValue("@UUSER", g.PubUserId);
                                    cmd3.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                    cmd3.Parameters.AddWithValue("@AED", "A");
                                    cmd3.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                                    cmd3.Parameters.AddWithValue("@LIP", g.PubLocalId);
                                    cmd3.Parameters.AddWithValue("@LID", Environment.MachineName);

                                    cmd3.ExecuteNonQuery();
                                    i++;
                                }

                                tran.Commit();
                                
                                //==============Delete from Approval Status===============
                                if(header.STATUS > 1)
                                {
                                    string delApprovalRecord = $@" Delete from APPROVAL_STATUS Where V_TYPE=@V_TYPE and V_NO=@V_NO and comp_code=@comp_code and year_code=@year_code and branch_code=@branch_code;
                                                                   Delete from APPROVAL_STATUS2 Where V_TYPE=@V_TYPE and V_NO=@V_NO and comp_code=@comp_code and year_code=@year_code and branch_code=@branch_code";
                                    using (SqlCommand pubCmd = new SqlCommand(delApprovalRecord, conn))
                                    {
                                        pubCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                                        pubCmd.Parameters.AddWithValue("@V_TYPE", "STPI");
                                        pubCmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                                        pubCmd.Parameters.AddWithValue("@year_code", g.PubFYearCode);
                                        pubCmd.Parameters.AddWithValue("@branch_code", g.PubBranchCode);
                                        pubCmd.ExecuteNonQuery();
                                    }
                                }
                                //==============Delete from Approval Status===============

                                //===============Update Approval Status==================
                                var FinalApprovalBody = await CheckIsFinalApprovalBodyAsync();
                                if (FinalApprovalBody.data)
                                {
                                    using (SqlCommand cmd = new SqlCommand($@"select 1 from approval_status where user_Code=@USER_CODE  
                                                                            and V_Type='STPI' and V_No=@V_NO and COMP_CODE=@COMP_CODE and Branch_Code=@Branch_Code 
                                                                            and Year_Code=@Year_Code", conn))
                                    {
                                        //cmd.CommandType = CommandType.StoredProcedure;
                                        //cmd.Parameters.AddWithValue("@Action", "IsFinalApprovalBody");
                                        cmd.Parameters.AddWithValue("@USER_CODE", g.PubUserId);
                                        cmd.Parameters.AddWithValue("@V_TYPE", "STPI");
                                        cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                                        cmd.Parameters.AddWithValue("@Branch_Code", g.PubBranchCode);
                                        cmd.Parameters.AddWithValue("@Year_Code", g.PubFYearCode);

                                        object result = await cmd.ExecuteScalarAsync();

                                        if (result != null && result != DBNull.Value)
                                        {
                                            string updateQuery = $@" Update approval_status set STATUS='CLOSE', CLOSE_DATE=format(getdate(),'yyyy-MM-dd HH:mm'),Approval_code=8,
                                                                 Approval_remark='Approved',remarks='Document Approved' where V_Type='STPI' and V_No=@V_NO 
                                                                 and COMP_CODE=@COMP_CODE and Branch_Code=@Branch_Code and Year_Code=@Year_Code";
                                            var pubCmd = new SqlCommand(updateQuery, conn);
                                            cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                                            cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                                            cmd.Parameters.AddWithValue("@Branch_Code", g.PubBranchCode);
                                            cmd.Parameters.AddWithValue("@Year_Code", g.PubFYearCode);
                                            pubCmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                                //===============Update Approval Status==================
                                string mode = action == "insert" ? "Insert" : "Update";
                                _logService.InsertLog("PREQUEST1", "Purchase Request", "Transaction", mode, "STPI", header.V_NO.ToString(), header.V_DATE);
                                _logService.InsertLog("PREQUEST2", "Purchase Request", "Transaction", mode, "STPI", header.V_NO.ToString(), header.V_DATE);
                                return "Success";
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                return $"Error: {ex.Message}";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            }

        }

        public async Task<RepositoryResponseData<string>> GetPurchaseRequestsAsync(int itemCode, int deptCode, int vNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                string result = "";

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetPrevRequest");
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                        cmd.Parameters.AddWithValue("@DEPT_CODE", deptCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);

                        await conn.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                result = reader["DocNo"].ToString();
                            }
                        }
                    }
                }

                return new RepositoryResponseData<string> {status = true, data = result};
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<string> { status = false, message = "Error occurred while fetching data" + ex.Message};
            }
        }

        public async Task<RepositoryResponseData<bool>> GetItemMakeAsync(int itemCode, int makeCode)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                bool result = false;
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ValidateMakeCode");
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                        cmd.Parameters.AddWithValue("@MAKE_CODE", makeCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        await conn.OpenAsync();

                        var value = await cmd.ExecuteScalarAsync();

                        result = (value != null);
                    }
                }
                return new RepositoryResponseData<bool> { status = true, data = result };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponseData<bool>> CheckMonthlyReqAsync(int itemCode)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                bool exists = false;

                //string query = @"
                //SELECT 1 
                //FROM item_mast 
                //WHERE code = @ItemCode 
                //    AND active = 1 
                //    AND comp_code = @CompCode 
                //    AND planning_method = 'MRP'";

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetMonthlyReq");
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        await conn.OpenAsync();

                        var value = await cmd.ExecuteScalarAsync();

                        exists = (value != null);
                    }
                }
                return new RepositoryResponseData<bool> { status = true, data = exists };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, message = ex.Message, data = false };
            }
        }

        public async Task<RepositoryResponseData<bool>> GetMaxRequestCountAsync(int vNo, DateTime vDate)
        {
            try
            {
                int count = 0;

                var gv = _globalVariableService.GetGlobalVariables();
                var gs = await _globalVariableService.LoadGeneralSetting();
                int maxRequest = gs.pubMaxRequestInADay;
                maxRequest = 5; //Testing
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetMaxRequest");
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@V_DATE", vDate);
                        cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        await con.OpenAsync();
                        object result = await cmd.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            count = Convert.ToInt32(result);
                        }
                    }
                }

                bool isWithinLimit = count < maxRequest;

                return new RepositoryResponseData<bool> { status = true, data = isWithinLimit };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<string> GetApprovalStatus(int vNo)
        {
            string status = string.Empty;
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetApprovStatus");
                        cmd.Parameters.AddWithValue("@v_type", "STPI");
                        cmd.Parameters.AddWithValue("@v_NO", vNo);
                        cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@branch_code", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@year_code", gv.PubFYearCode);

                        con.Open();

                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            status = result.ToString().ToUpper();
                        }
                    }
                }

                return new RepositoryResponseData<string> { status = true, data = status };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<string> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<bool> ValidateDepartmentAccess(int deptCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            bool exists = false;

            try
            {

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetUserDept");
                        cmd.Parameters.AddWithValue("@USER_CODE", gv.PubUserId);
                        cmd.Parameters.AddWithValue("@DEPT_CODE", deptCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        con.Open();

                        object result = cmd.ExecuteScalar();

                        if (result != null)
                            exists = true;
                    }
                }


                return new RepositoryResponseData<bool> { status = true, data = exists };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<List<LastTenPurchaseRequestModel>> GetLastTenPurchaseRequest(List<int> itemCodes)
        {
            List<LastTenPurchaseRequestModel> list = new List<LastTenPurchaseRequestModel>();
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                string itemCodeString = string.Join(",", itemCodes);

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetLast10PR");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ItemCodeString", itemCodeString);

                        con.Open();

                        SqlDataReader dr = cmd.ExecuteReader();

                        while (dr.Read())
                        {
                            list.Add(new LastTenPurchaseRequestModel
                            {
                                ItemCode = dr["ItemCode"] != DBNull.Value ? Convert.ToInt32(dr["ItemCode"]) : 0,
                                VNo = dr["VNo"]?.ToString(),
                                VDate = dr["VDate"]?.ToString(),
                                Department = dr["Department"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                MakeName = dr["MakeName"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),
                                Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                PlaceofUse = dr["PlaceofUse"]?.ToString(),
                                TechDesc = dr["TechDesc"]?.ToString(),
                                Remarks = dr["Remarks"]?.ToString(),
                                Status = dr["Status"]?.ToString()
                            });
                        }

                        con.Close();
                    }
                }

                return new RepositoryResponseData<List<LastTenPurchaseRequestModel>> { status = true, data = list };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<LastTenPurchaseRequestModel>> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<List<LastTenConsumptionModel>> GetLastTenConsumptionDetails(List<int> itemCodes)
        {
            List<LastTenConsumptionModel> list = new List<LastTenConsumptionModel>();
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                string itemCodeString = string.Join(",", itemCodes);
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetLast10Consumption");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ItemCodeString", itemCodeString);

                        con.Open();

                        SqlDataReader dr = cmd.ExecuteReader();

                        while (dr.Read())
                        {
                            list.Add(new LastTenConsumptionModel
                            {
                                ItemCode = dr["ItemCode"] != DBNull.Value ? Convert.ToInt32(dr["ItemCode"]) : 0,
                                VNo = dr["VNo"]?.ToString(),
                                Date = dr["Date"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                Make = dr["Make"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),
                                Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                Rate = dr["Rate"] != DBNull.Value ? Convert.ToDecimal(dr["Rate"]) : 0,
                                Department = dr["Department"]?.ToString(),
                                Machine = dr["Machine"]?.ToString(),
                                Remarks = dr["Remarks"]?.ToString(),
                                Status = dr["Status"]?.ToString()
                            });
                        }

                        con.Close();
                    }
                }

                return new RepositoryResponseData<List<LastTenConsumptionModel>> { status = true, data = list };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<LastTenConsumptionModel>> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<List<LastTenPurchaseHistoryModel>> GetLastTenPurchaseHistory(List<int> itemCodes)
        {
            List<LastTenPurchaseHistoryModel> list = new List<LastTenPurchaseHistoryModel>();

            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                string itemCodeString = string.Join(",", itemCodes);

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetLast10PHistory");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ItemCodeString", itemCodeString);

                        con.Open();

                        SqlDataReader dr = cmd.ExecuteReader();

                        while (dr.Read())
                        {
                            list.Add(new LastTenPurchaseHistoryModel
                            {
                                ItemCode = dr["ItemCode"] != DBNull.Value ? Convert.ToInt32(dr["ItemCode"]) : 0,
                                VNo = dr["VNo"]?.ToString(),
                                Date = dr["Date"]?.ToString(),
                                Supplier = dr["Supplier"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                Make = dr["Make"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),
                                Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                Rate = dr["Rate"] != DBNull.Value ? Convert.ToDecimal(dr["Rate"]) : 0,
                                OthAmt = dr["OthAmt"] != DBNull.Value ? Convert.ToDecimal(dr["OthAmt"]) : 0,
                                CGSTPer = dr["CGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["CGSTPer"]) : 0,
                                SGSTPer = dr["SGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["SGSTPer"]) : 0,
                                IGSTPer = dr["IGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["IGSTPer"]) : 0,
                                PackPer = dr["PackPer"] != DBNull.Value ? Convert.ToDecimal(dr["PackPer"]) : 0,
                                DiscPer = dr["DiscPer"] != DBNull.Value ? Convert.ToDecimal(dr["DiscPer"]) : 0,
                                LDRate = dr["LDRate"] != DBNull.Value ? Convert.ToDecimal(dr["LDRate"]) : 0,
                                Remarks = dr["Remarks"]?.ToString(),
                                Status = dr["Status"]?.ToString()
                            });
                        }

                        con.Close();
                    }
                }

                return new RepositoryResponseData<List<LastTenPurchaseHistoryModel>> { status = true, data = list };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<LastTenPurchaseHistoryModel>> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<List<LastTenPurchaseHistoryModel>> GetLastTenOrderHistory(List<int> itemCodes)
        {
            List<LastTenPurchaseHistoryModel> list = new List<LastTenPurchaseHistoryModel>();

            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                string itemCodeString = string.Join(",", itemCodes);

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetLast10POrder");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ItemCodeString", itemCodeString);

                        con.Open();
                        SqlDataReader dr = cmd.ExecuteReader();

                        while (dr.Read())
                        {
                            list.Add(new LastTenPurchaseHistoryModel
                            {
                                ItemCode = dr["ItemCode"] != DBNull.Value ? Convert.ToInt32(dr["ItemCode"]) : 0,
                                VNo = dr["VNo"]?.ToString(),
                                Date = dr["Date"]?.ToString(),
                                Supplier = dr["Supplier"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                Make = dr["Make"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),
                                Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                Rate = dr["Rate"] != DBNull.Value ? Convert.ToDecimal(dr["Rate"]) : 0,
                                OthAmt = dr["OthAmt"] != DBNull.Value ? Convert.ToDecimal(dr["OthAmt"]) : 0,
                                CGSTPer = dr["CGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["CGSTPer"]) : 0,
                                SGSTPer = dr["SGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["SGSTPer"]) : 0,
                                IGSTPer = dr["IGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["IGSTPer"]) : 0,
                                PackPer = dr["PackPer"] != DBNull.Value ? Convert.ToDecimal(dr["PackPer"]) : 0,
                                DiscPer = dr["DiscPer"] != DBNull.Value ? Convert.ToDecimal(dr["DiscPer"]) : 0,
                                LDRate = dr["LDRate"] != DBNull.Value ? Convert.ToDecimal(dr["LDRate"]) : 0,
                                Remarks = dr["Remarks"]?.ToString(),
                                Status = dr["Status"]?.ToString()
                            });
                        }

                        con.Close();
                    }
                }

                return new RepositoryResponseData<List<LastTenPurchaseHistoryModel>> { status = true, data = list };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<LastTenPurchaseHistoryModel>> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<List<LastTenPurchaseRequestModel>> GetItemWisePurchaseRequest(int itemCode)
        {
            List<LastTenPurchaseRequestModel> list = new List<LastTenPurchaseRequestModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetItemPRequest");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);

                        con.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new LastTenPurchaseRequestModel
                                {
                                    ItemCode = itemCode,
                                    VNo = dr["VNo"]?.ToString(),
                                    VDate = dr["VDate"]?.ToString(),
                                    Department = dr["Department"]?.ToString(),
                                    ItemName = dr["ItemName"]?.ToString(),
                                    MakeName = dr["MakeName"]?.ToString(),
                                    Unit = dr["Unit"]?.ToString(),
                                    Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                    PlaceofUse = dr["PlaceofUse"]?.ToString(),
                                    TechDesc = dr["TechDesc"]?.ToString(),
                                    Remarks = dr["Remarks"]?.ToString(),
                                    Status = dr["Status"]?.ToString()
                                });
                            }
                        }
                    }
                }

                return new RepositoryResponseData<List<LastTenPurchaseRequestModel>> { status = true, data = list };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<LastTenPurchaseRequestModel>> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<List<LastTenConsumptionModel>> GetItemWiseConsumptionHistory(int itemCode)
        {
            List<LastTenConsumptionModel> list = new List<LastTenConsumptionModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetItemConsumption");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);

                        con.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new LastTenConsumptionModel
                                {
                                    ItemCode = itemCode,
                                    VNo = dr["VNo"]?.ToString(),
                                    Date = dr["VDate"]?.ToString(),
                                    ItemName = dr["ItemName"]?.ToString(),
                                    Make = dr["Make"]?.ToString(),
                                    Unit = dr["Unit"]?.ToString(),
                                    Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                    Rate = dr["Rate"] != DBNull.Value ? Convert.ToDecimal(dr["Rate"]) : 0,
                                    Department = dr["Department"]?.ToString(),
                                    Machine = dr["Machine"]?.ToString(),
                                    Remarks = dr["Remarks"]?.ToString(),
                                    Status = dr["Status"]?.ToString()
                                });
                            }
                        }
                    }

                    return new RepositoryResponseData<List<LastTenConsumptionModel>> { status = true, data = list };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<LastTenConsumptionModel>> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<List<LastTenPurchaseHistoryModel>> GetItemWisePurchaseOrderHistory(int itemCode)
        {
            List<LastTenPurchaseHistoryModel> list = new List<LastTenPurchaseHistoryModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetItemPOrder");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);

                        con.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new LastTenPurchaseHistoryModel
                                {
                                    ItemCode = itemCode,
                                    VNo = dr["VNo"]?.ToString(),
                                    Date = dr["VDate"]?.ToString(),
                                    Supplier = dr["Supplier"]?.ToString(),
                                    ItemName = dr["ItemName"]?.ToString(),
                                    Make = dr["Make"]?.ToString(),
                                    Unit = dr["Unit"]?.ToString(),

                                    Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                    Rate = dr["Rate"] != DBNull.Value ? Convert.ToDecimal(dr["Rate"]) : 0,
                                    OthAmt = dr["OthAmt"] != DBNull.Value ? Convert.ToDecimal(dr["OthAmt"]) : 0,

                                    CGSTPer = dr["CGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["CGSTPer"]) : 0,
                                    SGSTPer = dr["SGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["SGSTPer"]) : 0,
                                    IGSTPer = dr["IGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["IGSTPer"]) : 0,

                                    PackPer = dr["PackPer"] != DBNull.Value ? Convert.ToDecimal(dr["PackPer"]) : 0,
                                    DiscPer = dr["DiscPer"] != DBNull.Value ? Convert.ToDecimal(dr["DiscPer"]) : 0,

                                    LDRate = dr["LDRate"] != DBNull.Value ? Convert.ToDecimal(dr["LDRate"]) : 0,

                                    Remarks = dr["Remarks"]?.ToString(),
                                    Status = dr["Status"]?.ToString()
                                });
                            }
                        }
                    }
                    return new RepositoryResponseData<List<LastTenPurchaseHistoryModel>> { status = true, data = list };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<LastTenPurchaseHistoryModel>> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<List<ItemWisePurchaseQuotationHistoryModel>> GetItemWisePurchaseQuotationHistory(int itemCode)
        {
            List<ItemWisePurchaseQuotationHistoryModel> list = new List<ItemWisePurchaseQuotationHistoryModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetItemPQuotation");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);

                        con.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new ItemWisePurchaseQuotationHistoryModel
                                {
                                    ItemCode = itemCode,
                                    VNo = dr["VNo"]?.ToString(),
                                    Date = dr["VDate"]?.ToString(),
                                    Supplier = dr["Supplier"]?.ToString(),
                                    ItemName = dr["ItemName"]?.ToString(),
                                    Make = dr["Make"]?.ToString(),
                                    Unit = dr["Unit"]?.ToString(),
                                    GroupNo = dr["GroupNo"]?.ToString(),

                                    Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                    Rate = dr["Rate"] != DBNull.Value ? Convert.ToDecimal(dr["Rate"]) : 0,
                                    Freight = dr["Freight"] != DBNull.Value ? Convert.ToDecimal(dr["Freight"]) : 0,

                                    CGSTPer = dr["CGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["CGSTPer"]) : 0,
                                    SGSTPer = dr["SGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["SGSTPer"]) : 0,
                                    IGSTPer = dr["IGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["IGSTPer"]) : 0,

                                    PackPer = dr["PackPer"] != DBNull.Value ? Convert.ToDecimal(dr["PackPer"]) : 0,
                                    DiscPer = dr["DiscPer"] != DBNull.Value ? Convert.ToDecimal(dr["DiscPer"]) : 0,

                                    OthExps = dr["OthExps"] != DBNull.Value ? Convert.ToDecimal(dr["OthExps"]) : 0,
                                    LDRate = dr["LDRate"] != DBNull.Value ? Convert.ToDecimal(dr["LDRate"]) : 0,

                                    Remarks = dr["Remarks"]?.ToString(),
                                    Status = dr["Status"]?.ToString()
                                });
                            }
                        }
                    }

                    return new RepositoryResponseData<List<ItemWisePurchaseQuotationHistoryModel>> { status = true, data = list };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<ItemWisePurchaseQuotationHistoryModel>> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<List<LastTenPurchaseHistoryModel>> GetItemWisePurchaseReceiptHistory(int itemCode)
        {
            List<LastTenPurchaseHistoryModel> list = new List<LastTenPurchaseHistoryModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetItemPReceipt");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);


                        con.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new LastTenPurchaseHistoryModel
                                {
                                    ItemCode = itemCode,
                                    VNo = dr["VNo"]?.ToString(),
                                    Date = dr["VDate"]?.ToString(),
                                    Supplier = dr["Supplier"]?.ToString(),
                                    ItemName = dr["ItemName"]?.ToString(),
                                    Make = dr["Make"]?.ToString(),
                                    Unit = dr["Unit"]?.ToString(),

                                    Qty = dr["Qty"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["Qty"])
                                        : 0,

                                    Rate = dr["Rate"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["Rate"])
                                        : 0,

                                    OthAmt = dr["OthAmt"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["OthAmt"])
                                        : 0,

                                    CGSTPer = dr["CGSTPer"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["CGSTPer"])
                                        : 0,

                                    SGSTPer = dr["SGSTPer"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["SGSTPer"])
                                        : 0,

                                    IGSTPer = dr["IGSTPer"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["IGSTPer"])
                                        : 0,

                                    PackPer = dr["PackPer"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["PackPer"])
                                        : 0,

                                    DiscPer = dr["DiscPer"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["DiscPer"])
                                        : 0,

                                    LDRate = dr["LDRate"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["LDRate"])
                                        : 0,

                                    Remarks = dr["Remarks"]?.ToString(),
                                    Status = dr["Status"]?.ToString()
                                });
                            }
                        }
                    }

                    return new RepositoryResponseData<List<LastTenPurchaseHistoryModel>> { status = true, data = list };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<LastTenPurchaseHistoryModel>> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<List<LastTenPurchaseHistoryModel>> GetItemWisePurchaseHistory(int itemCode)
        {
            List<LastTenPurchaseHistoryModel> list = new List<LastTenPurchaseHistoryModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetItemPHistory");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);

                        con.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new LastTenPurchaseHistoryModel
                                {
                                    ItemCode = itemCode,
                                    VNo = dr["VNo"]?.ToString(),
                                    Date = dr["VDate"]?.ToString(),
                                    Supplier = dr["Supplier"]?.ToString(),
                                    ItemName = dr["ItemName"]?.ToString(),
                                    Make = dr["Make"]?.ToString(),
                                    Unit = dr["Unit"]?.ToString(),

                                    Qty = dr["Qty"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["Qty"])
                                        : 0,

                                    Rate = dr["Rate"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["Rate"])
                                        : 0,

                                    OthAmt = dr["OthAmt"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["OthAmt"])
                                        : 0,

                                    CGSTPer = dr["CGSTPer"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["CGSTPer"])
                                        : 0,

                                    SGSTPer = dr["SGSTPer"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["SGSTPer"])
                                        : 0,

                                    IGSTPer = dr["IGSTPer"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["IGSTPer"])
                                        : 0,

                                    PackPer = dr["PackPer"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["PackPer"])
                                        : 0,

                                    DiscPer = dr["DiscPer"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["DiscPer"])
                                        : 0,

                                    LDRate = dr["LDRate"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["LDRate"])
                                        : 0,

                                    Remarks = dr["Remarks"]?.ToString(),
                                    Status = dr["Status"]?.ToString()
                                });
                            }
                        }
                    }

                    return new RepositoryResponseData<List<LastTenPurchaseHistoryModel>> { status = true, data = list };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<LastTenPurchaseHistoryModel>> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<string> PRPrintRequest(PRPrintModel model)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using(SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            // 1. Clear temp table
                            using (SqlCommand cmdClear = new SqlCommand("sp_PurchaseReq1", con, tran))
                            {
                                cmdClear.CommandType = CommandType.StoredProcedure;
                                cmdClear.Parameters.AddWithValue("@Action", "ClearTemp_Cheq");
                                cmdClear.ExecuteNonQuery();
                            }
                            // 1. Item Process
                            foreach (var item in model.Items)
                            {
                                using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con, tran))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@Action", "ITEM_PROCESS");
                                    cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                                    cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // =========================
                            // 3. FINAL APPROVED BY STRING
                            // =========================
                            string finalApprovedBy = "";

                            string docId = "STPI" + model.VNo;

                            using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@Action", "Get_ApprovedBy");
                                cmd.Parameters.AddWithValue("@DOC_ID", docId);
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                finalApprovedBy = Convert.ToString(cmd.ExecuteScalar());
                            }
                            tran.Commit();
                            return new RepositoryResponseData<string> { status = true, message = "Processed successfully", data = finalApprovedBy };
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            return new RepositoryResponseData<string> { status = false, message = ex.Message, data = "" };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<string> { status = false, message = ex.Message };
            }
        }
        public RepositoryResponseData<(bool isExist, string userName)> CheckApprovalStatus(int vNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            bool isExist = false;
            string userName = "";
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "CheckApprovalStatus");
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@USER_CODE", gv.PubUserId);
                        var result = cmd.ExecuteScalar();
                        isExist = result != null;
                    }
                    if (isExist)
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Action", "GetPenApprovUserName");
                            cmd.Parameters.AddWithValue("@V_NO", vNo);
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                            cmd.Parameters.AddWithValue("@USER_CODE", gv.PubUserId);
                            var result = cmd.ExecuteScalar();
                            userName = result?.ToString() ?? "";
                        }
                    }
                    return new RepositoryResponseData<(bool, string)>
                    {
                        status = true,
                        data = (isExist, userName)
                    };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<(bool, string)>
                {
                    status = false,
                    message = ex.Message,
                    data = (false, "")
                };
            }
        }
    }
}
