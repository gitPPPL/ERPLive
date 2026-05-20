using Azure;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Data;
using System.Dynamic;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Weighbridge.Transaction
{
    public class StoreWeighbridgeEntryListRepository : IStoreWeighbridgeEntryListRepository
    {
        private readonly GlobalVariableService _globalValue;
        private readonly DataBaseConnection _dbcontext;
        private readonly DbHelper _dbHelper;
        private readonly LogService.LogService _logService;
        public StoreWeighbridgeEntryListRepository(GlobalVariableService globalValue, DataBaseConnection dbcontext, DbHelper dbHelper, LogService.LogService logService)
        {
            _globalValue = globalValue;
            _dbcontext = dbcontext;
            _dbHelper = dbHelper;
            _logService = logService;
        }

        public async Task<RepositoryResponse> DeleteStoreWb(string docId)
        {
            var response = new RepositoryResponse();
            try
            {
                if (string.IsNullOrEmpty(docId))
                {
                    response.status = false;
                    response.message = "Invalid ID";
                    return response;
                }

                var userSession = _globalValue.GetGlobalVariables();
                string VType = docId.Substring(0, 4);
                string VNo = docId.Substring(4);
                

                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            string[] deleteQueries = {
                            "DELETE FROM wb1 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO",
                            "DELETE FROM wb2 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO"

                            };

                            foreach (var query in deleteQueries)
                            {
                                using (var cmd = new SqlCommand(query, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@V_TYPE", VType);
                                    cmd.Parameters.AddWithValue("@V_NO", VNo);

                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                            
                            transaction.Commit();
                            //=========================================Log Insert
                            _logService.InsertLog("WB1", "Store WeighBridge Entry", "Transaction", "Delete", VType, VNo, null);
                            _logService.InsertLog("WB2", "Store WeighBridge Entry", "Transaction", "Delete", VType, VNo, null);
                            response.status = true;
                            response.message = "Data deleted successfully";
                            return response;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            response.status = false;
                            response.message = $"Delete failed: {ex.Message}";
                            return response;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
                return response;
            }
        }

        public async Task<RepositoryResponseList<ExpandoObject>> ExportAllDocs()
        {
            var response = new RepositoryResponseList<ExpandoObject>();
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE", 1},
                    {"@DOCTYPE",  "KantaStore"},
                    {"@Action", "Excel" }
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetWBEntry]", parameter);

                response.status = true;
                response.data = dataList.ToList();
                return response;
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
                return response;
            }
        }

        public async Task<RepositoryResponseList<dynamic>> GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var response = new RepositoryResponseList<dynamic>();
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var dataList = new List<dynamic>();
                int totalCount = 0;

                using (SqlConnection conn = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_GetWBEntry]", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add Parameters
                        cmd.Parameters.AddWithValue("@COMP_CODE", UsersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", UsersessionDt.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@DOCTYPE", "KantaStore");
                        cmd.Parameters.AddWithValue("@Action", "WBEntryList");
                        cmd.Parameters.AddWithValue("@SearchTerm", (object)searchTerm ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        await conn.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            // --- RESULT SET 1: WBEntryList ---
                            while (await reader.ReadAsync())
                            {
                                var row = new ExpandoObject() as IDictionary<string, object>;
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row.Add(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i));
                                }
                                dataList.Add(row);
                            }

                            // --- RESULT SET 2: TotalCount ---
                            if (await reader.NextResultAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    totalCount = Convert.ToInt32(reader["TotalCount"]);
                                }
                            }
                        }
                    }
                }
                response.data = dataList;
                response.status = true;
                response.totalCount = totalCount;

                return response;
            }
            catch (Exception ex)
            {
                response.message = ex.Message;
                response.status = false;
                return response;
            }
        }

        public async Task<RepositoryResponseList<ExpandoObject>> StoreWBDetails(string docid)
        {
            var response = new RepositoryResponseList<ExpandoObject>();
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                if (string.IsNullOrEmpty(docid))
                {
                    response.status = false;
                    response.message = "Invalid ID";
                    return response;
                }
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE", docid.Substring(0, 4) },
                    {"@V_NO", docid.Substring(4) },
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetWBEntry]", parameter);

                response.status = true;
                response.data = entryDetailList.ToList();
                return response;
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
                return response;
            }
        }

        public async Task<RepositoryResponseData<string>> ValidateDeleteStoreWb(string docId)
        {
            var response = new RepositoryResponseData<string>();
            string gateType = "";
            int gateNo = 0;
            try
            {
                var userSession = _globalValue.GetGlobalVariables();

                string VType = docId.Substring(0, 4);
                string VNo = docId.Substring(4);

                if(VType == "KSIN")
                {
                    using (var con = _dbcontext.GetErpConnection())
                    {
                        await con.OpenAsync();
                        try
                        {
                            using (SqlTransaction tran = con.BeginTransaction())
                            {
                                string getGateNo = @"select GATE_NO, GATE_TYPE from WB1 where V_TYPE=@VType and V_NO=@VNo and COMP_CODE=@COMP_CODE and BRANCH_CODE=@BRANCH_CODE and YEAR_CODE=@YEAR_CODE";
                                using (SqlCommand cmd = new SqlCommand(getGateNo, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@VType", VType);
                                    cmd.Parameters.AddWithValue("@VNo", VNo);
                                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                                    {
                                        if (await reader.ReadAsync())
                                        {
                                            gateNo = reader["GATE_NO"] != DBNull.Value ? Convert.ToInt32(reader["GATE_NO"]) : 0;
                                            gateType = reader["GATE_TYPE"]?.ToString();
                                        }
                                    }
                                }
                                using (SqlCommand cmd = new SqlCommand("sp_GetWBEntry", con, tran))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@Action", "ValidatePurchase2_ForDelete_Store_WbDetails");
                                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@NewGateType", gateType);
                                    cmd.Parameters.AddWithValue("@NewGateNo", gateNo);

                                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                                    {
                                        if (await reader.ReadAsync())
                                        {
                                            response.status = true;
                                            response.data = "Exists";

                                            response.message = string.Format(
                                                "This document exists in Purchase receipt Serial No: <span class='highlight-serial'><b>{0}</b></span> <br>Dated: <b>{1}</b>",
                                                reader["KANTA_NO"],
                                                reader["V_DATE"]
                                            );
                                        }
                                        else
                                        {
                                            response.status = true;
                                            response.data = "NotExists";
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            response.status = false;
                            response.message = ex.Message;
                        }
                    }
                }
                else
                {
                    response.status = true;
                    response.data = "";
                }
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
            }

            return response;
        }
    }
}
