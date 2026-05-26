using DocumentFormat.OpenXml.Drawing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Dynamic;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Transaction
{
    public class QCTemperatureEntryListRepository : IQCTemperatureEntryListRepository
    {
        private readonly GlobalVariableService _globalValue;
        private readonly DataBaseConnection _dbcontext;
        private readonly LogService.LogService _logService;
        public QCTemperatureEntryListRepository(GlobalVariableService globalValue, DataBaseConnection dbcontext, LogService.LogService logService)
        {
            _globalValue = globalValue;
            _dbcontext = dbcontext;
            _logService = logService;
        }

        public async Task<RepositoryResponse> Delete(string docId)
        {
            var response = new RepositoryResponse();
            try
            {
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

                        //    string[] deleteQueries = {
                        //"DELETE FROM TAPE_QUALITY1 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO",
                        //"DELETE FROM TAPE_QUALITY2 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO"

                        //};

                        //    foreach (var query in deleteQueries)
                        //    {
                            using (var cmd = new SqlCommand("sp_GetQcTempratureEntry", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@Action", "Delete");
                                cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                                cmd.Parameters.AddWithValue("@V_TYPE", VType);
                                cmd.Parameters.AddWithValue("@V_NO", VNo);

                                await cmd.ExecuteNonQueryAsync();
                            }
                            //}

                            transaction.Commit();
                            //_logService.InsertLog("TAPE_QUALITY1", "QC Temperature Entry", "Transaction", "Delete", VType, VNo.ToString(), null);
                            //_logService.InsertLog("TAPE_QUALITY2", "QC Temperature Entry", "Transaction", "Delete", VType, VNo.ToString(), null);
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

        public async Task<RepositoryResponseList<dynamic>> GetList(string searchTerm, int pageNumber, int pageSize)
        {
            var response = new RepositoryResponseList<dynamic>();
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var dataList = new List<dynamic>();
                int totalCount = 0;

                using (SqlConnection conn = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_GetQcTempratureEntry]", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add Parameters
                        cmd.Parameters.AddWithValue("@COMP_CODE", UsersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", UsersessionDt.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", UsersessionDt.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", "TAPE");
                        cmd.Parameters.AddWithValue("@Action", "QcTempratureEntryList");
                        cmd.Parameters.AddWithValue("@SearchTerm", (object)searchTerm ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        await conn.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            // --- RESULT SET 1: QCTempEntryList ---
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
                response.status = true;
                response.data = dataList;
                response.totalCount = totalCount;
                return response;
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
                return response;
            }
        }
    }
}
