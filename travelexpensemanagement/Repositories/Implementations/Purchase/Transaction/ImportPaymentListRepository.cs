using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class ImportPaymentListRepository : IImportPaymentListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public ImportPaymentListRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _logService = logService;
        }

        public async Task<(List<object> Data, int TotalCount)> LoadListDataAsync(string searchTerm = "", int pageNumber = 1,int pageSize = 10)
        {

            var globalData = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_ImportPaymentEntry", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@COMP_CODE", globalData.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalData.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalData.PubFYearCode);
                cmd.Parameters.AddWithValue("@Action", "LoadListData");
                cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm.Trim());
                cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                await con.OpenAsync();

                var data = new List<object>();
                int totalCount = 0;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        totalCount = reader["TotalRecords"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalRecords"]);

                        data.Add(new
                        {
                            vNo = reader["V_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["V_NO"]),
                            vType = reader["V_TYPE"] == DBNull.Value ? "" : reader["V_TYPE"].ToString(),
                            vDate = reader["V_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["V_DATE"]),
                            docId = reader["DOC_ID"] == DBNull.Value ? "" : reader["DOC_ID"].ToString(),
                        });
                    }
                }
                return (data, totalCount);
            }
            
        }

        public async Task<(bool Success, string Message)> DeleteImportPaymentEntryAsync(string docId)
        {
            try
            {
                var globalData = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_ImportPaymentEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalData.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalData.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalData.PubFYearCode);
                    cmd.Parameters.AddWithValue("@Action", "DeleteRecord");
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);

                    await con.OpenAsync();
                    cmd.ExecuteNonQuery();
                }

                return (true, "Import Payment Entry deleted successfully.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }

        }
    }
}
