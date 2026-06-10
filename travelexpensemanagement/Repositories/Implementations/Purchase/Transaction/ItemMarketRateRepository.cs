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
    public class ItemMarketRateRepository : IItemMarketRateRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        public ItemMarketRateRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, GlobalValidationdate globalValidationdate,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
        }

        public async Task<ItemMarketRateWrapper?> GetItemMarketRateByVnoAsync(int vNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            MARKET_RATE1 header = null;
            List<MARKET_RATE2> items = new();

            using SqlConnection conn = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new("sp_MARKET_RATE", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Action", "SELECT");
            cmd.Parameters.AddWithValue("@SubAction", "GETALLBYVNO");
            cmd.Parameters.AddWithValue("@V_NO", vNo);
            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

            await conn.OpenAsync();

            using var rdr = await cmd.ExecuteReaderAsync();

            if (await rdr.ReadAsync())
            {
                header = new MARKET_RATE1
                {
                    YEAR_CODE = rdr["YEAR_CODE"] as int? ?? 0,
                    COMP_CODE = rdr["COMP_CODE"] as int? ?? 0,
                    BRANCH_CODE = rdr["BRANCH_CODE"] as int? ?? 0,
                    V_TYPE = rdr["V_TYPE"]?.ToString(),
                    V_NO = rdr["V_NO"] as int? ?? 0,
                    V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                    MGROUP_TYPE = rdr["MGROUP_TYPE"]?.ToString(),
                    EFF_DATE = rdr["EFF_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EFF_DATE"]) : DateTime.MinValue,
                    EXP_DATE = rdr["EXP_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EXP_DATE"]) : DateTime.MinValue,
                    REMARKS = rdr["REMARKS"]?.ToString()
                };
            }

            if (await rdr.NextResultAsync())
            {
                while (await rdr.ReadAsync())
                {
                    items.Add(new MARKET_RATE2
                    {
                        YEAR_CODE = rdr["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["YEAR_CODE"]) : 0,
                        COMP_CODE = rdr["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COMP_CODE"]) : 0,
                        BRANCH_CODE = rdr["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BRANCH_CODE"]) : 0,
                        V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                        V_TYPE = rdr["V_TYPE"]?.ToString(),
                        V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                        ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                        MIN_RATE = rdr["MIN_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["MIN_RATE"]) : 0,
                        MAX_RATE = rdr["MAX_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["MAX_RATE"]) : 0,
                        REMARK = rdr["REMARK"]?.ToString(),
                    });
                }
            }

            return new ItemMarketRateWrapper
            {
                header = header,
                lineRows = items
            };
        }

        public async Task<(bool Success, string Message, int VNo)> SaveItemMarketRateAsync(ItemMarketRateWrapper data)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            int vNo = data.header.V_NO ?? 0;

            bool exists = vNo > 0 && IsDuplicateMarketRateEntry(
                  vNo,
                 Convert.ToInt32(globalVar.PubCompCode),
                Convert.ToInt32(globalVar.PubFYearCode),
                Convert.ToInt32(globalVar.PubBranchCode)
              );

            string subAction = exists ? "UPDATE" : "INSERT";

            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new("sp_MARKET_RATE", con);

            cmd.CommandType = CommandType.StoredProcedure;

            await con.OpenAsync();

            cmd.Parameters.AddWithValue("@Action", "INSERTANDUPDATE");
            cmd.Parameters.AddWithValue("@SubAction", subAction);

            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);

            cmd.Parameters.AddWithValue("@V_TYPE", data.header.V_TYPE ?? "");
            cmd.Parameters.AddWithValue("@V_NO", vNo);
            cmd.Parameters.AddWithValue("@V_DATE", data.header.V_DATE);
            cmd.Parameters.AddWithValue("@EFF_DATE", data.header.EFF_DATE);
            cmd.Parameters.AddWithValue("@EXP_DATE", data.header.EXP_DATE);

            cmd.Parameters.AddWithValue("@DOC_ID", (data.header.V_TYPE ?? "") + vNo);
            cmd.Parameters.AddWithValue("@MGROUP_TYPE", data.header.MGROUP_TYPE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@REMARKS", data.header.REMARKS ?? "");

            var dt = ConvertToTVP(data.lineRows, vNo, data.header.V_TYPE);

            var param = cmd.Parameters.AddWithValue("@MARKET_RATE2_Type", dt);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.MARKET_RATE2_Type";

            await cmd.ExecuteNonQueryAsync();

            string msg = subAction == "UPDATE"
                ? "Data Updated Successfully"
                : "Data Saved Successfully";

            return (true, msg, vNo);
        }

        private DataTable ConvertToTVP(List<MARKET_RATE2> rows, int vNo, string vType)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            DataTable dt = new();

            dt.Columns.Add("SRNO", typeof(int));
            dt.Columns.Add("COMP_CODE", typeof(int));
            dt.Columns.Add("BRANCH_CODE", typeof(int));
            dt.Columns.Add("YEAR_CODE", typeof(int));
            dt.Columns.Add("V_TYPE", typeof(string));
            dt.Columns.Add("V_NO", typeof(int));
            dt.Columns.Add("V_DATE", typeof(DateTime));
            dt.Columns.Add("DOC_ID", typeof(string));

            dt.Columns.Add("ITEM_CODE", typeof(int));
            dt.Columns.Add("MIN_RATE", typeof(decimal));
            dt.Columns.Add("MAX_RATE", typeof(decimal));
            dt.Columns.Add("AVG_RATE", typeof(decimal)); 
            dt.Columns.Add("REMARK", typeof(string));

            dt.Columns.Add("UUSER", typeof(int));
            dt.Columns.Add("UDATE", typeof(DateTime));
            dt.Columns.Add("EUSER", typeof(int));
            dt.Columns.Add("EDATE", typeof(DateTime));
            dt.Columns.Add("AED", typeof(string));
            dt.Columns.Add("WSID", typeof(string));
            dt.Columns.Add("LIP", typeof(string));
            dt.Columns.Add("LID", typeof(string));

            int i = 1;

            foreach (var r in rows)
            {
                dt.Rows.Add(
                    i++,
                    globalVar.PubCompCode,
                    globalVar.PubBranchCode,
                    globalVar.PubFYearCode,
                    vType,
                    vNo,
                    DateTime.Now,
                    (vType ?? "") + vNo,
                    r.ITEM_CODE,
                    r.MIN_RATE,
                    r.MAX_RATE,
                    0,
                    r.REMARK ?? "",

                    globalVar.PubUserId,
                    DateTime.Now,
                    globalVar.PubUserId,
                    DateTime.Now,
                    "A",
                    globalVar.PubWorkStationID ?? "WEB",
                    globalVar.PubLocalId,
                    Environment.MachineName
                );
            }

            return dt;
        }

        public bool IsDuplicateMarketRateEntry(int vNo, int compCode, int yearCode, int branchCode)
        {
            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new(@"
            SELECT COUNT(*) 
            FROM MARKET_RATE1 
            WHERE V_NO=@V_NO 
            AND COMP_CODE=@COMP_CODE 
            AND YEAR_CODE=@YEAR_CODE 
            AND BRANCH_CODE=@BRANCH_CODE", con);

            cmd.Parameters.AddWithValue("@V_NO", vNo);
            cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
            cmd.Parameters.AddWithValue("@YEAR_CODE", yearCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", branchCode);

            con.Open();
            return (int)cmd.ExecuteScalar() > 0;
        }
    }
}
