using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models; // adjust namespace
using travelexpensemanagement.Models.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction
{
    public class ApprovalService : IApprovalService
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;

        public ApprovalService(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }
        // FINAL CLEAN VERSION
        public RepositoryResponseList<GetCourierTrackingModel> GetCourierTrackingEntryList(string searchTerm, int pageNumber, int pageSize)
        {
            var response = new RepositoryResponseList<GetCourierTrackingModel>();
            try
            {
                var results = new List<GetCourierTrackingModel>();
                int totalCount = 0;
                var gv = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (var cmd = new SqlCommand("sp_InsertCourierTracking", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        con.Open();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new GetCourierTrackingModel
                                {
                                    VNo = reader["V_NO"]?.ToString() ?? "",
                                    DocType = reader["V_TYPE"]?.ToString() ?? "",
                                    DocNo = reader["DOC_ID"]?.ToString() ?? "",
                                    DocDate = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd") : "",
                                    PartyName = reader["PARTY_NAME"]?.ToString() ?? "",
                                    City = reader["CITY_NAME"]?.ToString() ?? "",
                                    CourierName = reader["COURIER_NAME"]?.ToString() ?? "",
                                    DocketNo = reader["DOCKET_NO"]?.ToString() ?? "",
                                    //ReceivedBy = reader["RECEIVED_BY"]?.ToString() ?? "",
                                    Purpose = reader["PURPOSE"]?.ToString() ?? "",
                                    Weight = reader["WEIGHT"] != DBNull.Value ? Convert.ToDecimal(reader["WEIGHT"]) : 0,
                                    Remarks = reader["REMARKS"]?.ToString() ?? ""   
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value
                                    ? Convert.ToInt32(reader["TotalCount"])
                                    : 0;
                            }
                        }
                    }
                }

                response.status = true;
                response.data = results;
                response.totalCount = totalCount;
                response.message = "Success";
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
            }

            return response;
        }
        public async Task<RepositoryResponse> DeleteCourierTrackingEntry(string vNo, string docType)
        {
            var response = new RepositoryResponse();

            try
            {
                var global = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertCourierTracking", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);

                        // IMPORTANT TYPE FIX
                        cmd.Parameters.AddWithValue("@V_NO", Convert.ToInt32(vNo));
                        cmd.Parameters.AddWithValue("@V_TYPE", docType);

                        await con.OpenAsync();
                        // READ RESULT PROPERLY
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int rows = 0;
                            if (reader.Read())
                            {
                                rows = reader["RowsAffected"] != DBNull.Value ? Convert.ToInt32(reader["RowsAffected"]) : 0;
                            }
                            response.status = rows > 0;
                            response.message = rows > 0 ? "Deleted successfully" : "Delete failed";
                        }
                    }
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