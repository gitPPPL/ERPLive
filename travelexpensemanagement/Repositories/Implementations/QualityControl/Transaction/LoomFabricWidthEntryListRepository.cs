using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Asn1.Cms;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Transaction
{
    public class LoomFabricWidthEntryListRepository : ILoomFabricWidthEntryListRepository
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly DbHelper _dbHelper;

        public LoomFabricWidthEntryListRepository(DataBaseConnection dbcontext, GlobalVariableService globalValue, DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public async Task<(object Data, int TotalCount)> GetLoomFabricStrengthListAsync(string searchTerm, int pageNumber,int pageSize)
        {
            var userSessionDt = _globalValue.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                {"@COMP_CODE", userSessionDt.PubCompCode},
                {"@YEAR_CODE", userSessionDt.PubFYearCode},
                {"@BRANCH_CODE", userSessionDt.PubBranchCode},
                {"@V_TYPE", "LINS"},
                {"@Action", "LFSEntryList"},
                {"@SearchTerm", searchTerm ?? ""},
                {"@PageNumber", pageNumber},
                {"@PageSize", pageSize}
            };

            var pagedList = await _dbHelper.GetJsonFromProcedureAsync(
                "[dbo].[sp_GetLoomFabricEntry]",
                parameter);

            int totalCount = 0;

            if (pagedList != null && pagedList.Count > 0)
            {
                var firstRow = (IDictionary<string, object>)pagedList[0];

                if (firstRow.ContainsKey("TotalRecords"))
                {
                    totalCount = Convert.ToInt32(firstRow["TotalRecords"]);
                }
            }

            return (pagedList, totalCount);
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
            if (string.IsNullOrWhiteSpace(docId))
                return null;

            var userSession = _globalValue.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                {"@COMP_CODE", userSession.PubCompCode},
                {"@YEAR_CODE", userSession.PubFYearCode},
                {"@BRANCH_CODE", userSession.PubBranchCode},
                {"@V_TYPE", "LINS"},
                {"@V_NO", docId.Substring(4)},
                {"@Action", "EntryDetail"}
            };

            return await _dbHelper.GetJsonFromProcedureAsync(
                "[dbo].[sp_GetLoomFabricEntry]",
                parameter);
        }

        public async Task<object> ExportAllDocsAsync()
        {
            var userSession = _globalValue.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                {"@COMP_CODE", userSession.PubCompCode},
                {"@YEAR_CODE", userSession.PubFYearCode},
                {"@BRANCH_CODE", userSession.PubBranchCode},
                {"@V_TYPE", "LINS"},
                {"@Action", "Excel"}
            };

            return await _dbHelper.GetJsonFromProcedureAsync(
                "[dbo].[sp_GetLoomFabricEntry]",
                parameter);
        }

    }
}
