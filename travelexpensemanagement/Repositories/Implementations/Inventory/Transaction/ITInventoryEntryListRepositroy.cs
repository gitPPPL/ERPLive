using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Inventory.Transaction
{
    public class ITInventoryEntryListRepositroy : IITInventroyEntryListRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;
       
        public ITInventoryEntryListRepositroy(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _logService = logService;
        }

        public async Task<object> LoadListDataAsync(string searchTerm = "", int pageNo = 1, int pageSize = 20)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                if (pageNo < 1)
                    pageNo = 1;

                if (pageSize < 1)
                    pageSize = 20;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using SqlCommand cmd = new SqlCommand("sp_ITInventoryEntry",con);

                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE",globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE",globalVariables.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE",globalVariables.PubFYearCode);
                    cmd.Parameters.AddWithValue("@PageNo",pageNo);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@SearchTerm",searchTerm?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@Action","ListData");

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    int totalCount = 0;

                    var list = new List<object>();

                    if (await reader.ReadAsync())
                    {
                        if (reader["TotalRecords"] != DBNull.Value)
                        {
                            totalCount = Convert.ToInt32(reader["TotalRecords"]);
                        }
                    }
                    
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                docId = reader["docId"] == DBNull.Value ? "" : reader["docId"].ToString(),
                                vNo = reader["V_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["V_NO"]),
                                vDate = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
                                ipAddress = reader["IPADDRESS"] == DBNull.Value ? "" : reader["IPADDRESS"].ToString(),
                                assetCat = reader["ASSET_CAT"] == DBNull.Value ? "" : reader["ASSET_CAT"].ToString(),
                                deviceType = reader["DEVICE_TYPE"] == DBNull.Value ? "" : reader["DEVICE_TYPE"].ToString(),
                                empCode = reader["empCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["empCode"]),
                                empName = reader["empName"] == DBNull.Value ? "" : reader["empName"].ToString(),
                                assetCode = reader["ASSET_CODE"] == DBNull.Value ? "" : reader["ASSET_CODE"].ToString(),
                                deviceName = reader["DEVICE_NAME"] == DBNull.Value ? "" : reader["DEVICE_NAME"].ToString(),
                                deviceModel = reader["DEVICE_MODEL"] == DBNull.Value ? "" : reader["DEVICE_MODEL"].ToString(),
                                unitName = reader["UNIT_NAME"] == DBNull.Value ? "" : reader["UNIT_NAME"].ToString(),
                                location = reader["LOCATION"] == DBNull.Value ? "" : reader["LOCATION"].ToString(),
                                purchaseDate = reader["PURCHASE_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["PURCHASE_DATE"]).ToString("yyyy-MM-dd"),
                                purchaseFrom = reader["PURCHASE_FROM"] == DBNull.Value ? "" : reader["PURCHASE_FROM"].ToString(),
                                usedBy = reader["USED_BY"] == DBNull.Value ? "" : reader["USED_BY"].ToString(),
                                remarks = reader["REMARKS"] == DBNull.Value ? "" : reader["REMARKS"].ToString()
                            });
                        }
                    }

                    return new {success = true, data = list, pageNo = pageNo, pageSize = pageSize, totalCount = totalCount,hasMore = (pageNo * pageSize) < totalCount };
                }
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

    }
}
