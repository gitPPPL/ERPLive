using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ItemMarketRateListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public ItemMarketRateListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
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
            return View("~/Views/Purchase/Transaction/ItemMarketRateList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetAllItemRateList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var globelVar = _globalVariableService.GetGlobalVariables();
            var itemRates = new List<MARKET_RATE1>();
            int totalCount = 0;

            try
            {
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
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

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
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching quotations", error = ex.Message });
            }

            return Json(new { success = true, itemRates, totalCount });
        }

        [HttpPost]
        public JsonResult DeleteItemMarketRateByCode(int code, string vType, int compCode, int branchCode, int yearCode)
        {
            try
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
                    }
                }

                return Json(new { success = true, message = "Item Market Rate deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
 