using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.OtherUnused;
using travelexpensemanagement.Repositories.Interfaces.Sales.OtherUnused;

namespace travelexpensemanagement.Repositories.Implementations.Sales.OtherUnused
{
    public class PackingSlipEntryListRepository : IPackingSlipEntryListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly LogService.LogService _logService;
        public PackingSlipEntryListRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _logService = logService;
        }
        public RepositoryResponseList<PackingSlipListModel> GetPackingSlipList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new List<PackingSlipListModel>();
            int totalcount = 0;
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("Sp_PackingSlipEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetAll");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                data.Add(new PackingSlipListModel
                                {
                                    V_NO = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]),
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]),
                                    PARTY = reader["PARTY"]?.ToString(),
                                    PLACE = reader["PLACE"]?.ToString(),
                                    SHIFT = reader["SHIFT"]?.ToString(),
                                    SLIP_NO = reader["SLIP_NO"]?.ToString(),
                                    PORD_TYPE = reader["PORD_TYPE"]?.ToString(),
                                    REMARKS = reader["REMARKS"]?.ToString(),
                                    STATUS = reader["STATUS"]?.ToString()
                                });
                            }

                            if (reader.NextResult())
                            {
                                if (reader.Read())
                                {
                                    totalcount = reader["TotalCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalCount"]);
                                }
                            }
                        }
                    }
                }


                return new RepositoryResponseList<PackingSlipListModel> { status = true, data = data, totalCount = totalcount };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<PackingSlipListModel> { status = false, message = ex.Message };
            }
        }
        public async Task<RepositoryResponse> Delete(int vNo, string docType)
        {
            try
            {
                if (string.IsNullOrEmpty(docType) || vNo <= 0)
                {
                    return new RepositoryResponse { status = false, message = "Invalid ID" };
                }

                var userSession = _globalVariableService.GetGlobalVariables();

                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {

                            string[] deleteQueries = {
                        "DELETE FROM PPRODUCTION1 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO",
                        "DELETE FROM PPRODUCTION2 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO",
                        };
                            foreach (var query in deleteQueries)
                            {
                                using (var cmd = new SqlCommand(query, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@V_TYPE", docType);
                                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            //_logService.InsertLog("PPRODUCTION1", "Packing Slip Entry", "Other Unused", "Delete", docType, vNo.ToString(), null);
                            //_logService.InsertLog("PPRODUCTION2", "Packing Slip Entry", "Other Unused", "Delete", docType, vNo.ToString(), null);

                            return new RepositoryResponse { status = true, message = "Data deleted successfully" };
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return new RepositoryResponse { status = false, message = $"Delete failed: {ex.Message}" };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }
    }
}
