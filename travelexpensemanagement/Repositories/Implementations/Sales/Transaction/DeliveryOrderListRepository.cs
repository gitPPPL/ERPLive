using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class DeliveryOrderListRepository : IDeliveryOrderListRepositroy
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public DeliveryOrderListRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService)
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

                    using SqlCommand cmd = new SqlCommand("sp_DeliveryOrder", con);

                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", "DOGT");
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
                                vType = reader["V_TYPE"] == DBNull.Value ? "" : reader["V_TYPE"].ToString(),
                                vNo = reader["V_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["V_NO"]),
                                vDate = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
                                delDate = reader["DEL_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["DEL_DATE"]).ToString("yyyy-MM-dd"),
                                billName = reader["BILL_NAME"] == DBNull.Value ? "" : reader["BILL_NAME"].ToString(),
                                billAdd1 = reader["BILL_ADD1"] == DBNull.Value ? "" : reader["BILL_ADD1"].ToString(),
                                billAdd2 = reader["BILL_ADD2"] == DBNull.Value ? "" : reader["BILL_ADD2"].ToString(),
                                billAdd3 = reader["BILL_ADD3"] == DBNull.Value ? "" : reader["BILL_ADD3"].ToString(),
                                billCity = reader["BILL_CITY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["BILL_CITY"]),
                                agentCode = reader["AGENT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["AGENT_CODE"]),
                                shipName = reader["SHIP_NAME"] == DBNull.Value ? "" : reader["SHIP_NAME"].ToString(),
                                shipAdd1 = reader["SHIP_ADD1"] == DBNull.Value ? "" : reader["SHIP_ADD1"].ToString(),
                                shipAdd2 = reader["SHIP_ADD2"] == DBNull.Value ? "" : reader["SHIP_ADD2"].ToString(),
                                shipAdd3 = reader["SHIP_ADD3"] == DBNull.Value ? "" : reader["SHIP_ADD3"].ToString(),
                                shipCity = reader["SHIP_CITY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SHIP_CITY"]),
                                taxCode = reader["TAX_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TAX_CODE"]),
                                itemType = reader["ITEM_TYPE"] == DBNull.Value ? "" : reader["ITEM_TYPE"].ToString(),
                                amount = reader["AMOUNT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["AMOUNT"]),
                                packPer = reader["PACK_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PACK_PER"]),
                                packAmt = reader["PACK_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PACK_AMT"]),
                                totNet = reader["TOT_NET"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TOT_NET"])
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
