using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class ItemMarketRateListRepository : IItemMarketRateListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public ItemMarketRateListRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }

        public (List<MARKET_RATE1> itemRates, int totalCount) GetAllItemRateList(string searchTerm, int pageNumber, int pageSize)
        {
            var globelVar = _globalVariableService.GetGlobalVariables();

            var itemRates = new List<MARKET_RATE1>();
            int totalCount = 0;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_MARKET_RATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SubAction", "GETALLBYVNO");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globelVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globelVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globelVar.PubBranchCode);

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            itemRates.Add(new MARKET_RATE1
                            {
                                V_TYPE = reader["V_TYPE"]?.ToString(),
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                BRANCH_CODE = reader["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(reader["BRANCH_CODE"]) : 0,
                                YEAR_CODE = reader["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(reader["YEAR_CODE"]) : 0,
                                DOC_ID = reader["DOC_ID"]?.ToString(),
                                REMARKS = reader["REMARKS"]?.ToString(),
                                MGROUP_TYPE = reader["MGROUP_TYPE"]?.ToString(),
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                EFF_DATE = reader["EFF_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["EFF_DATE"]) : DateTime.MinValue,
                                EXP_DATE = reader["EXP_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["EXP_DATE"]) : DateTime.MinValue
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }
            }

            return (itemRates, totalCount);
        }

        public bool DeleteItemMarketRateByCode(int code, string vType, int compCode, int branchCode, int yearCode)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_MARKET_RATE", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "DELETE");
                    cmd.Parameters.AddWithValue("@SubAction", "DELETEFROMBOTH");
                    cmd.Parameters.AddWithValue("@V_NO", code);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", branchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", yearCode);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    return true;
                }
            }
        }
    }
}
