using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ItemMarketRateController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public ItemMarketRateController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = 1;
            ViewBag.YearCode = globalVar.PubFYearCode;
            return View("~/Views/Purchase/Transaction/ItemMarketRate/Index.cshtml");
        }

        public int GetNextV_NO(string yearCode)
        {
            string newV_NO = "00000";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                // Execute query to get PREFIXYR
                string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = '" + yearCode + "'";
                SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                // Execute query to get last V_NO
                string lastV_NO_Query = "SELECT TOP 1 V_NO FROM MARKET_RATE1 ORDER BY V_NO DESC";
                SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                string lastV_NO = lastVnoCmd.ExecuteScalar()?.ToString();

                int lastNumber = 0;
                if (!string.IsNullOrEmpty(lastV_NO) && lastV_NO.Length >= 9)
                {
                    string numericPart = lastV_NO.Substring(lastV_NO.Length - 5);
                    int.TryParse(numericPart, out lastNumber);
                }

                // Increment and format the new V_NO
                string newRunningNo = (lastNumber + 1).ToString("D5");
                newV_NO = prefixYR + newRunningNo;
            }

            return Convert.ToInt32(newV_NO);
        }


        public IActionResult GetDocumentTypeList()
        {
            string query = "select CODE,NAME from DOCTYPE_MAST where DOCTYPE = 'MarketRate'";
            var docTypeList = _dropdownService.GetDropdownList(query);
            return Json(docTypeList);
        }

        public IActionResult GetItemGroupTypeList(int cCode)
        {
            List<string> groupTypeList = new List<string>();
            string query = "SELECT DISTINCT MGROUP_TYPE FROM ITEM_MGROUP";

            using (SqlConnection connection = _dbConnection.GetErpConnection())
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    groupTypeList.Add(reader["MGROUP_TYPE"].ToString());
                }
                reader.Close();
            }


            return Json(groupTypeList);
        }

        public IActionResult GetItemList(int cCode)
        {
            string query = "Select CODE,NAME FROM ITEM_MAST WHERE COMP_CODE='" + cCode + "' ORDER BY NAME";
            var designationList = _dropdownService.GetDropdownList(query);
            return Json(designationList);
        }


        [HttpGet]
        public IActionResult GetItemMarketRateByVno(int vNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            MARKET_RATE1 header = null;
            List<MARKET_RATE2> items = new();

            try
            {
                using SqlConnection conn = _dbConnection.GetErpConnection();
                using SqlCommand cmd = new("sp_MARKET_RATE", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "SELECT");
                cmd.Parameters.AddWithValue("@SubAction", "GETALLBYVNO");
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                conn.Open();
                using SqlDataReader rdr = cmd.ExecuteReader();

                // Header (MARKET_RATE1)
                if (rdr.Read())
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

                //  Items (MARKET_RATE2)
                if (rdr.NextResult())
                {
                    while (rdr.Read())
                    {
                        items.Add(new MARKET_RATE2
                        {
                            //DOC_ID = rdr["DOC_ID"]?.ToString(),
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


                return Json(new
                {
                    success = true,
                    header,
                    items
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching quotation", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult GetItemMarketRate2ByGroupType(string groupType)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            List<MARKET_RATE2> items = new();

            try
            {
                using SqlConnection conn = _dbConnection.GetErpConnection();
                using SqlCommand cmd = new("sp_MARKET_RATE", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "LOADPREVIOUSDATA");
                cmd.Parameters.AddWithValue("@SubAction", "LOADBYGROUPTYPE");
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                cmd.Parameters.AddWithValue("@MGROUP_TYPE", groupType);

                conn.Open();
                using SqlDataReader rdr = cmd.ExecuteReader();

                //  Items (MARKET_RATE2)
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        items.Add(new MARKET_RATE2
                        {
                            //DOC_ID = rdr["DOC_ID"]?.ToString(),
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


                return Json(new
                {
                    success = true,
                    items
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching quotation", error = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SaveItemMarketRate([FromBody] ItemMarketRateWrapper data)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            int vNo = data.header.V_NO ?? GetNextV_NO(globalVar.PubFYearCode);
            string action = "INSERTANDUPDATE";
            string subAction = "";

            // Check for existing entry
            if (IsDuplicateMarketRateEntry(vNo, Convert.ToInt32(globalVar.PubCompCode), Convert.ToInt32(globalVar.PubFYearCode), 1))
            {
                subAction = "UPDATE";
            }
            else
            {
                subAction = "INSERT";
            }

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_MARKET_RATE", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var docID = data.header.V_TYPE + vNo;

                        // Header
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SubAction", subAction);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", data.header.V_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@V_DATE", data.header.V_DATE);
                        cmd.Parameters.AddWithValue("@EFF_DATE", data.header.EFF_DATE);
                        cmd.Parameters.AddWithValue("@EXP_DATE", data.header.EXP_DATE);
                        cmd.Parameters.AddWithValue("@DOC_ID", data.header.V_TYPE + data.header.V_NO ?? "");

                        cmd.Parameters.AddWithValue("@MGROUP_TYPE", data.header.MGROUP_TYPE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARKS", data.header.REMARKS ?? "");

                        // System Fields
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        // Detail (TVP)
                        DataTable dt = ConvertToMarketRate2TVP(data.lineRows, vNo, data.header.V_TYPE);
                        SqlParameter tvpParam = cmd.Parameters.AddWithValue("@MARKET_RATE2_Type", dt);
                        tvpParam.SqlDbType = SqlDbType.Structured;
                        tvpParam.TypeName = "dbo.MARKET_RATE2_Type";

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true });
            }
            catch (SqlException ex)
            {
                return Json(new { success = false, message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public DataTable ConvertToMarketRate2TVP(List<MARKET_RATE2> rows, int vNo, string vType)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            DataTable dt = new DataTable("TVP_MARKETRATE2");

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

            foreach (var item in rows)
            {
                dt.Rows.Add(
                    item.SNO ?? (object)DBNull.Value,
                    globalVar.PubCompCode,
                    1,
                    globalVar.PubFYearCode,
                    vType,
                    vNo,
                    item.V_DATE ?? (object)DBNull.Value,
                    item.V_TYPE + item.V_NO,
                    item.ITEM_CODE ?? 0,
                    item.MIN_RATE ?? 0,
                    item.MAX_RATE ?? 0,
                    item.AVG_RATE ?? 0,
                    item.REMARK ?? "",
                    globalVar.PubUserId,
                    DateTime.Now,
                    globalVar.PubUserId,
                    DateTime.Now,
                    "A",
                    globalVar.PubWorkStationID ?? "WEB",
                    globalVar.PubLocalId ?? "127.0.0.1",
                    Environment.MachineName
                );
            }

            return dt;
        }

        private bool IsDuplicateMarketRateEntry(int vNo, int compCode, int yearCode, int branchCode)
        {
            // Example logic; change according to your DB schema
            string query = "SELECT COUNT(*) FROM MARKET_RATE1 WHERE V_NO = @V_NO AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", yearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", branchCode);
                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }


    }
}
