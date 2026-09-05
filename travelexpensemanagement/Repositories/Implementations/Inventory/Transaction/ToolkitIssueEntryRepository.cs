using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Inventory.Transaction
{
    public class ToolkitIssueEntryRepository : IToolkitIssueEntryRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly LogService.LogService _logService;
        public ToolkitIssueEntryRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DbHelper dbHelper, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _logService = logService;
        }


        public RepositoryResponse SaveOrUpdate([FromBody] ToolKitIssueModel model)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            if (model == null)
            {
                return new RepositoryResponse { status = false, message = "Invalid request!" };
            }
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_ToolKitIssueEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", model.ACTION);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DOC_ID", string.IsNullOrEmpty(model.V_TYPE?.ToString()) && 
                                                                string.IsNullOrEmpty(model.V_NO?.ToString()) 
                                                                ? (object)DBNull.Value
                                                                : model.V_TYPE?.ToString() + model.V_NO?.ToString());
                        cmd.Parameters.AddWithValue("@PLACE_CODE", model.PLACE_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", model.ITEM_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@QTY", model.QTY ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RATE", model.RATE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@AMOUNT", model.AMOUNT ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EMP_CODE", model.EMP_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FROM_DEPT", model.FROM_DEPT ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TO_DEPT", model.TO_DEPT ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARK", model.REMARK ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RECD_QTY", model.RECD_QTY ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DR_AMOUNT", model.DR_AMOUNT ?? (object)DBNull.Value);

                        if (model.ACTION == "INSERT")
                        {
                            cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@EUSER", gv.PubUserId);
                        }

                        cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.ExecuteNonQuery();

                        //string formName = model.V_TYPE == "TOIS" ? "Toolkit Issue" : "Toolkit Received";
                        //_logService.InsertLog("STOOL", formName, "Transaction", model.ACTION ?? "", model.V_TYPE ?? "", model.V_NO.ToString() ?? "", model.V_DATE);
                    }
                }
                return new RepositoryResponse { status = true, message = "Saved Successfully!" };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponseData<ToolKitIssueModel>> GetDataByIdAsync(string vType, string vNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new ToolKitIssueModel();
            decimal balanceQty = 0;
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_ToolKitIssueEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GETBYID");
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                data.V_TYPE = reader["V_TYPE"]?.ToString();
                                data.V_NO = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]);
                                data.V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]);
                                data.PLACE_CODE = reader["PLACE_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["PLACE_CODE"]);
                                data.ITEM_CODE = reader["ITEM_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["ITEM_CODE"]);
                                data.QTY = reader["QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["QTY"]);
                                data.RATE = reader["RATE"] == DBNull.Value ? null : Convert.ToDecimal(reader["RATE"]);
                                data.AMOUNT = reader["AMOUNT"] == DBNull.Value ? null : Convert.ToDecimal(reader["AMOUNT"]);
                                data.EMP_CODE = reader["EMP_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["EMP_CODE"]);
                                data.FROM_DEPT = reader["FROM_DEPT"] == DBNull.Value ? null : Convert.ToInt32(reader["FROM_DEPT"]);
                                data.TO_DEPT = reader["TO_DEPT"] == DBNull.Value ? null : Convert.ToInt32(reader["TO_DEPT"]);
                                data.DR_AMOUNT = reader["DR_AMOUNT"] == DBNull.Value ? null : Convert.ToDecimal(reader["DR_AMOUNT"]);
                                data.REMARK = reader["REMARK"]?.ToString();
                                data.ITEM_NAME = reader["ITEM_NAME"]?.ToString();
                            }
                        }

                        if (string.Equals(vType, "TORC", StringComparison.OrdinalIgnoreCase))
                        {
                            string qry1 = $@"select ISNULL(SUM(qty), 0) from STOOL where v_type='TOIS'  and EMP_CODE = {data.EMP_CODE} AND ITEM_CODE = {data.ITEM_CODE}
                                            AND COMP_CODE = {gv.PubCompCode} AND BRANCH_CODE = {gv.PubBranchCode} AND  YEAR_CODE = {gv.PubFYearCode}";

                            decimal totalIssueAmt = await _dbHelper.GetExecuteScalarAsync<decimal>(qry1);

                            string qry2 = $@"select ISNULL(SUM(qty), 0) from STOOL where v_type='TORC'  and EMP_CODE = {data.EMP_CODE} AND ITEM_CODE = {data.ITEM_CODE} 
                                            AND COMP_CODE = {gv.PubCompCode} AND BRANCH_CODE = {gv.PubBranchCode} AND  YEAR_CODE = {gv.PubFYearCode}";

                            decimal totalRecdAmt = await _dbHelper.GetExecuteScalarAsync<decimal>(qry2);

                            balanceQty = totalIssueAmt - totalRecdAmt;
                            data.BALANCE_QTY = balanceQty;
                        }

                    }
                }
                return new RepositoryResponseData<ToolKitIssueModel> { status = true, data = data };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<ToolKitIssueModel> { status = true, message = ex.Message };
            }
        }
        
        public async Task<RepositoryResponse> PrepareToolKitBalReportAsync(DateTime fromDate, DateTime toDate)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            //Delete From temp_inv1
                            string delQry = $@"delete from temp_inv1  where wsid='{gv.PubWorkStationID}' and userid={gv.PubUserId}";
                            await _dbHelper.ExecuteQueryAsynctran(delQry, new List<SqlParameter>(), tran);

                            //Insert into temp_inv1 with TOIS vType
                            string insertTOISQry = $@"insert into temp_inv1 (wsid, userid, comp_code, doc_id, party_code, item_code, open_qty) 
                                            select '{gv.PubWorkStationID}', {gv.PubUserId}, {gv.PubCompCode}, '1', emp_code,
                                            item_code, sum(qty) 
                                            from stool where comp_code={gv.PubCompCode} and branch_code= {gv.PubBranchCode} and  
                                            v_type='TOIS' and v_date < '{toDate:yyyyMMdd}' group by emp_code,item_code";
                            await _dbHelper.ExecuteQueryAsynctran(insertTOISQry, new List<SqlParameter>(), tran);

                            //Insert into temp_inv1 with TORC vType
                            string insertTORCQry = $@"insert into temp_inv1 (wsid, userid, comp_code, doc_id, party_code, item_code, open_qty) 
                                            select '{gv.PubWorkStationID}', {gv.PubUserId}, {gv.PubCompCode}, '1', emp_code,
                                            item_code, -sum(qty) 
                                            from stool where comp_code={gv.PubCompCode} and branch_code= {gv.PubBranchCode} and  
                                            v_type='TORC' and v_date < '{toDate:yyyyMMdd}' group by emp_code,item_code";
                            await _dbHelper.ExecuteQueryAsynctran(insertTORCQry, new List<SqlParameter>(), tran);

                            //Insert into temp_inv1 with TORC vType and date betweeb from date and to date
                            string insertTORCBwFromAndToDateQry = $@"insert into temp_inv1 (wsid, userid, comp_code, doc_id, party_code, 
                                            item_code, recd_qty) 
                                            select '{gv.PubWorkStationID}', {gv.PubUserId}, {gv.PubCompCode}, '1', emp_code, item_code, 
                                            sum(qty) from stool where comp_code={gv.PubCompCode} and branch_code= {gv.PubBranchCode} and
                                            v_type='TORC' and v_date between '{fromDate:yyyyMMdd}' and '{toDate:yyyyMMdd}' group by emp_code,item_code";
                            await _dbHelper.ExecuteQueryAsynctran(insertTORCBwFromAndToDateQry, new List<SqlParameter>(), tran);

                            //Insert into temp_inv1 with TOIS vType and date betweeb from date and to date
                            string insertTOISBwFromAndToDateQry = $@"insert into temp_inv1 (wsid, userid, comp_code, doc_id, party_code, 
                                            item_code, issu_qty) 
                                            select '{gv.PubWorkStationID}', {gv.PubUserId}, {gv.PubCompCode}, '1', emp_code, item_code, 
                                            sum(qty) from stool where comp_code={gv.PubCompCode} and branch_code= {gv.PubBranchCode} and
                                            v_type='TOIS' and v_date between '{fromDate:yyyyMMdd}' and '{toDate:yyyyMMdd}' group by emp_code,item_code";
                            await _dbHelper.ExecuteQueryAsynctran(insertTOISBwFromAndToDateQry, new List<SqlParameter>(), tran);

                            //Insert into temp_inv1 from temp_inv1
                            string insertFromTEMP_INV1ToTEMP_INV1Qry = $@"insert into temp_inv1 (wsid, userid, comp_code, doc_id, party_code, 
                                            item_code, open_qty, recd_qty, issu_qty) 
                                            select wsid, userid, comp_code, '2', party_code, item_code, sum(open_qty), sum(recd_qty), 
                                            sum(issu_qty) from temp_inv1 
                                            where wsid='{gv.PubWorkStationID}' and userid= {gv.PubUserId} and comp_code= {gv.PubCompCode} 
                                            and doc_id='1' group by wsid,userid,comp_code,item_code,party_code";
                            await _dbHelper.ExecuteQueryAsynctran(insertFromTEMP_INV1ToTEMP_INV1Qry, new List<SqlParameter>(), tran);

                            //Delete From temp_inv1 with docid 1
                            string deleteByDocIdQry = $@"delete from temp_inv1 where wsid='{gv.PubWorkStationID}' and userid= {gv.PubUserId} 
                                                and comp_code= {gv.PubCompCode} and doc_id='1'";
                            await _dbHelper.ExecuteQueryAsynctran(deleteByDocIdQry, new List<SqlParameter>(), tran);

                            tran.Commit();
                            return new RepositoryResponse { status = true, message = "Inserted Successfully!" };
                        }
                        catch (Exception ex)
                        {
                            return new RepositoryResponse { status = false, message = ex.Message };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponse  { status = false, message = ex.Message };
            }
        }
    }
}
