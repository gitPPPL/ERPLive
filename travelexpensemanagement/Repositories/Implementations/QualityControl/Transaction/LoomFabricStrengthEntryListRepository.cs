using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Transaction
{
    public class LoomFabricStrengthEntryListRepository : ILoomFabricStrengthEntryListRepository
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly DbHelper _dbHelper;

        public LoomFabricStrengthEntryListRepository(DataBaseConnection dbcontext, GlobalVariableService globalValue, DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public async Task<(object Data, int TotalCount)> GetLoomFabricStrengthListAsync(string searchTerm, int pageNumber, int pageSize)
        {
            var userSession = _globalValue.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                {"@COMP_CODE", userSession.PubCompCode},
                {"@YEAR_CODE", userSession.PubFYearCode},
                {"@BRANCH_CODE", userSession.PubBranchCode},
                {"@V_TYPE", "LMQC"},
                {"@Action", "LFSEntryList"},
                {"@SearchTerm", searchTerm ?? string.Empty},
                {"@PageNumber", pageNumber},
                {"@PageSize", pageSize}
            };

            var result = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetLoomFabricEntry]",parameter);

            int totalCount = 0;

            if (result.Any())
            {
                var firstRow = (IDictionary<string, object>)result.First();

                if (firstRow.ContainsKey("TotalRecords") &&
                    firstRow["TotalRecords"] != null)
                {
                    totalCount = Convert.ToInt32(firstRow["TotalRecords"]);
                }
            }

            return (result, totalCount);
        }

        public async Task<(bool Success, string Message)> DeleteLoomFabricStrengthEntryAsync(string docId)
        {
            if (string.IsNullOrEmpty(docId))
            {
                return (false, "Invalid ID");
            }

            var userSession = _globalValue.GetGlobalVariables();

            string vType = docId.Substring(0, 4);
            int vNo = Convert.ToInt32(docId.Substring(4));

            using (var con = _dbcontext.GetErpConnection())
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_GetLoomFabricEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@Action", "Delete");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return (Convert.ToInt32(reader["Status"]) == 1, reader["Message"]?.ToString() ?? string.Empty);
                        }
                    }
                }
            }

            return (false, "Delete failed");
        }

        public async Task<object> GetLoomFabricStrengthEntryDetailsAsync(string docId)
        {
            if (string.IsNullOrEmpty(docId))
            {
                return null;
            }

            var userSession = _globalValue.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                {"@COMP_CODE", userSession.PubCompCode},
                {"@YEAR_CODE", userSession.PubFYearCode},
                {"@BRANCH_CODE", userSession.PubBranchCode},
                {"@V_TYPE", "LMQC"},
                {"@V_NO", docId.Substring(4)},
                {"@Action", "EntryDetail"}
            };

            return await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetLoomFabricEntry]", parameter);
        }

        public async Task<object> ExportAllDocsAsync()
        {
            var userSession = _globalValue.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                {"@COMP_CODE", userSession.PubCompCode},
                {"@YEAR_CODE", userSession.PubFYearCode},
                {"@BRANCH_CODE", userSession.PubBranchCode},
                {"@V_TYPE", "LMQC"},
                {"@Action", "Excel"}
            };

            return await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetLoomFabricEntry]", parameter);
        }

    }
}
