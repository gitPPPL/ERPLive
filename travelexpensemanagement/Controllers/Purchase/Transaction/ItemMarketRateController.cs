using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    [SessionAuthorize]
    public class ItemMarketRateController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IItemMarketRateRepository _itemMarketRateRepository;
        public ItemMarketRateController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, GlobalValidationdate globalValidationdate,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService, IItemMarketRateRepository itemMarketRate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _itemMarketRateRepository = itemMarketRate;
        }

        public IActionResult Index()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = globalVar.PubBranchCode;
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

        public IActionResult GetItemList(int cCode, string groupType)
        {
            string query = @"
                            SELECT a.CODE, a.NAME
                            FROM ITEM_MAST a
                            LEFT JOIN ITEM_MGROUP b
                                ON a.MGROUP_CODE = b.CODE
                               AND a.COMP_CODE = b.COMP_CODE
                            WHERE a.COMP_CODE = " + cCode + @"
                              AND b.MGROUP_TYPE = '" + groupType + @"'
                            ORDER BY a.NAME";

            var itemList = _dropdownService.GetDropdownList(query);
            return Json(itemList);
        }

        [HttpGet]
        public async Task<IActionResult> GetItemMarketRateByVno(int vNo)
        {
            try
            {
                var result = await _itemMarketRateRepository.GetItemMarketRateByVnoAsync(vNo);

                if (result == null)
                {
                    return Json(new { success = false, message = "No data found" });
                }

                return Json(new
                {
                    success = true,
                    header = result.header,
                    items = result.lineRows
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error fetching data",
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveItemMarketRate([FromBody] ItemMarketRateWrapper data)
        {
            try
            {
                if (data == null || data.header == null)
                {
                    return Json(new { success = false, message = "Invalid request data" });
                }

                var result = await _itemMarketRateRepository.SaveItemMarketRateAsync(data);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    vNo = result.VNo
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error while saving data",
                    error = ex.Message
                });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            var global = _globalVariableService.GetGlobalVariables();
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("MARKET_RATE1", vdate, vtype, vno);
            return Ok(result);
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
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                cmd.Parameters.AddWithValue("@MGROUP_TYPE", groupType);
                cmd.Parameters.AddWithValue("@V_TYPE", "MRAT");

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

    }
}
