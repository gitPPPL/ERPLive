using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class SaleSaudaEntryListRepository : ISaleSaudaEntryListRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public SaleSaudaEntryListRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService)
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

                    using SqlCommand cmd = new SqlCommand("sp_SaleSauda_Entry", con);

                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                    cmd.Parameters.AddWithValue("@PageNo", pageNo);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@SearchTerm", searchTerm?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@Action", "GetList");

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
                                vNo = reader["V_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["V_NO"]),
                                vDate = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
                                docId = reader["docId"] == DBNull.Value ? "" : reader["docId"].ToString(),
                                partyCode = reader["PARTY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PARTY_CODE"]),
                                customerName = reader["CustomerName"] == DBNull.Value ? "" : reader["CustomerName"].ToString(),
                                cityCode = reader["CITY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CITY_CODE"]),
                                cityName = reader["CityName"] == DBNull.Value ? "" : reader["CityName"].ToString(),
                                phone = reader["PHONE"] == DBNull.Value ? "" : reader["PHONE"].ToString(),
                                itemCode = reader["ITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ITEM_CODE"]),
                                itemName = reader["ItemName"] == DBNull.Value ? "" : reader["ItemName"].ToString(),
                                itemType = reader["ITEM_TYPE"] == DBNull.Value ? "" : reader["ITEM_TYPE"].ToString(),
                                remark = reader["REMARK"] == DBNull.Value ? "" : reader["REMARK"].ToString()
                            });
                        }
                    }

                    return new { success = true, data = list, pageNo = pageNo, pageSize = pageSize, totalCount = totalCount, hasMore = (pageNo * pageSize) < totalCount };
                }
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

    }
}   
