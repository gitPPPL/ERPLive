using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Inventory.Transaction
{
    public class StoreInventoryTransferListRepository : IStoreInventoryTransferListRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        public StoreInventoryTransferListRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, travelexpensemanagement.LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _logService = logService;
        }

        public async Task<object> LoadListDataAsync(string searchTerm = "", int pageNo = 1, int pageSize = 20)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();
                int totalCount = 0;
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using SqlCommand cmd = new SqlCommand("sp_StoreInvetoryTransfer", con);

                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", "STRF");
                    cmd.Parameters.AddWithValue("@PageNo", pageNo);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@SearchTerm", searchTerm ?? "");
                    cmd.Parameters.AddWithValue("@Action", "ListData");
                    using SqlDataReader reader = cmd.ExecuteReader();

                    var list = new List<object>();

                    while (reader.Read())
                    {
                        if (totalCount == 0 && reader["TotalCount"] != DBNull.Value)
                        {
                            totalCount = Convert.ToInt32(reader["TotalCount"]);
                        }

                        list.Add(new
                        {
                            vType = reader["V_TYPE"] == DBNull.Value ? "" : reader["V_TYPE"].ToString(),
                            vNo = reader["V_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["V_NO"]),
                            vDate = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
                            deptCode = reader["deptCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["deptCode"]),
                            DeptName = reader["DeptName"] == DBNull.Value ? "" : reader["DeptName"].ToString(),
                            SHIFT = reader["SHIFT"] == DBNull.Value ? "" : reader["SHIFT"].ToString(),
                            remarks = reader["remarks"] == DBNull.Value ? "" : reader["remarks"].ToString(),
                            SLIP_NO = reader["SLIP_NO"] == DBNull.Value ? "" : reader["SLIP_NO"].ToString(),
                            docId = reader["docId"] == DBNull.Value ? "" : reader["docId"].ToString()

                        });
                    }

                    return new { success = true, data = list, pageNo = pageNo, pageSize = pageSize, totalCount = totalCount, hasMore = (pageNo * pageSize) < totalCount };
                }
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        public async Task<object> DeleteDataAsync(string docId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(docId))
                {
                    return new { success = false, message = "Document ID is required." };
                }

                var globalVariables = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("sp_StoreInvetoryTransfer", con, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@Action", "DeleteData");

                                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                                cmd.Parameters.AddWithValue("@DOC_ID", docId);

                                cmd.ExecuteNonQuery();
                            }

                            tran.Commit();

                            return new { success = true, message = "Data deleted successfully." };
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();

                            return new { success = false, message = ex.Message };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }

        }

        
    }
}
