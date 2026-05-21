using Microsoft.Data.SqlClient;
using System.Data;
using System.Dynamic;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Weighbridge.Transaction
{
    public class BigWeighbridgeListRepository: IBigWeighbridgeListRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public BigWeighbridgeListRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _dbHelper = dbHelper;
            _globalVariableService = globalVariableService;
        }

        public async Task<(object Data, int TotalCount)> GetBigWBridgeListAsync(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var session = _globalVariableService.GetGlobalVariables();

            var dataList = new List<dynamic>();
            int totalCount = 0;

            // Validation
            if (pageNumber < 1)
                pageNumber = 1;

            if (pageSize < 1)
                pageSize = 10;

            // Calculate offset for SQL pagination
            int offset = (pageNumber - 1) * pageSize;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("[dbo].[sp_GetWBEntry]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Parameters
                    cmd.Parameters.AddWithValue("@COMP_CODE", session.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", session.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", session.PubBranchCode);
                    cmd.Parameters.AddWithValue("@DOCTYPE", "KantaBig");
                    cmd.Parameters.AddWithValue("@Action", "WBEntryList");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm.Trim());
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        // First result set: Data
                        while (await reader.ReadAsync())
                        {
                            var row = new ExpandoObject() as IDictionary<string, object>;

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row.Add(
                                    reader.GetName(i),
                                    reader.IsDBNull(i) ? null : reader.GetValue(i)
                                );
                            }

                            dataList.Add(row);
                        }

                        // Second result set: TotalCount
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

            return (dataList, totalCount);
        }

        public async Task<(bool Status, string Message, string Data)> CheckDeleteBigWBridgeEntryAsync(string docid)
        {
            if (string.IsNullOrWhiteSpace(docid))
                return (false, "Invalid ID", null);

            try
            {
                var session = _globalVariableService.GetGlobalVariables();

                string vType = docid.Substring(0, 4);
                int vNo = Convert.ToInt32(docid.Substring(4));

                string warningMessage = "";

                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    // ======================================================
                    // 1. CHECK PURCHASE2
                    // ======================================================
                    using (var cmd = new SqlCommand("sp_GetWBEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action",
                            "ValidatePurchase2_ForDelete_WbDetails");
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", session.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", session.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", session.PubFYearCode);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                warningMessage +=
                                    $"This document exists in Purchase Receipt Serial No: " +
                                    $"{reader["V_NO"]} dated: " +
                                    $"{Convert.ToDateTime(reader["V_DATE"]):dd-MM-yyyy}<br>";
                            }
                        }
                    }

                    // ======================================================
                    // 2. CHECK GATE1
                    // ======================================================
                    using (var cmd = new SqlCommand("sp_GetWBEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action",
                            "ValidateGate1_ForDelete_WbDetails");
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", session.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", session.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", session.PubFYearCode);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                warningMessage +=
                                    $"This document exists in Gate Inward Serial No: " +
                                    $"{reader["V_NO"]} dated: " +
                                    $"{Convert.ToDateTime(reader["V_DATE"]):dd-MM-yyyy}<br>";
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(warningMessage))
                    return (true, warningMessage.Trim(), "Exists");

                return (true, "", "NotExists");
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Status, string Message)>DeleteBigWBridgeEntryAsync(string docid)
        {
            if (string.IsNullOrWhiteSpace(docid))
                return (false, "Invalid ID");

            try
            {
                var session = _globalVariableService.GetGlobalVariables();

                string vType = docid.Substring(0, 4);
                int vNo = Convert.ToInt32(docid.Substring(4));

                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (var cmd = new SqlCommand("sp_GetWBEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DeleteBigWeighbridge");
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", session.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", session.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", session.PubFYearCode);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                bool status =
                                    Convert.ToInt32(reader["Status"]) == 1;

                                string message =
                                    reader["Message"]?.ToString();

                                return (status, message);
                            }
                        }
                    }
                }

                return (false, "No response from stored procedure.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<object> GetBigWBridgeEntryDetailsAsync(string docid)
        {
            if (string.IsNullOrWhiteSpace(docid))
                return null;

            var session = _globalVariableService.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                { "@COMP_CODE", session.PubCompCode },
                { "@YEAR_CODE", session.PubFYearCode },
                { "@BRANCH_CODE", 1 },
                { "@V_TYPE", docid.Substring(0, 4) },
                { "@V_NO", docid.Substring(4) },
                { "@Action", "EntryDetail" }
            };

            var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync(
                "[dbo].[sp_GetWBEntry]",
                parameter
            );

            return entryDetailList;
        }

        public async Task<object> ExportAllDocsAsync()
        {
            var session = _globalVariableService.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                { "@COMP_CODE", session.PubCompCode },
                { "@YEAR_CODE", session.PubFYearCode },
                { "@BRANCH_CODE", 1 },
                { "@DOCTYPE", "KantaBig" },
                { "@Action", "Excel" }
            };

            var dataList = await _dbHelper.GetJsonFromProcedureAsync(
                "[dbo].[sp_GetWBEntry]",
                parameter
            );

            return dataList;
        }

    }
}
