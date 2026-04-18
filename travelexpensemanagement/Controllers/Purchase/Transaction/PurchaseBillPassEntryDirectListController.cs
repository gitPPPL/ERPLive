using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transiction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseBillPassEntryDirectListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public PurchaseBillPassEntryDirectListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Purchase/Transaction/PurchaseBillPassEntryDirectList/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetAllPurchaseBillPassEntryDirect(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var globelVar = _globalVariableService.GetGlobalVariables();
            var purchaseBillDirect = new List<PURCHASE1>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SubAction", "GETALLBYVNO");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        //cmd.Parameters.AddWithValue("@V_NO", DBNull.Value); 
                        cmd.Parameters.AddWithValue("@COMP_CODE", globelVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globelVar.PubFYearCode);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                purchaseBillDirect.Add(new PURCHASE1
                                {
                                    V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                    PARTY_NAME = reader["PARTY_NAME"]?.ToString(),
                                    SHIP_ADD1 = reader["SHIP_ADD1"]?.ToString(),
                                    DEBIT_AC = reader["DEBIT_AC"] != DBNull.Value ? Convert.ToInt32(reader["DEBIT_AC"]) : 0,
                                    CREDIT_AC = reader["CREDIT_AC"] != DBNull.Value ? Convert.ToInt32(reader["CREDIT_AC"]) : 0,
                                    BILL_QTY = reader["BILL_QTY"] != DBNull.Value ? Convert.ToDecimal(reader["BILL_QTY"]) : 0,
                                    AMOUNT = reader["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["AMOUNT"]) : 0,
                                    REF_TYPE = reader["REF_TYPE"]?.ToString(),
                                    REF_NO = reader["REF_NO"] != DBNull.Value ? Convert.ToInt32(reader["REF_NO"]) : 0,
                                    BILL_NO = reader["BILL_NO"]?.ToString(),
                                    BILL_DATE = reader["BILL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["BILL_DATE"]) : DateTime.MinValue,
                                    CHALL_NO = reader["CHALL_NO"]?.ToString(),
                                    CHALL_DATE = reader["CHALL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["CHALL_DATE"]) : DateTime.MinValue,
                                    DR_FROM_TPT = reader["DR_FROM_TPT"]?.ToString(),
                                    REMARKS = reader["DR_FROM_TPT"]?.ToString(),  // Consider verifying this — REMARKS might be its own column
                                    STATUS = reader["STATUS"] != DBNull.Value ? Convert.ToInt32(reader["STATUS"]) : 0
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

            return Json(new { success = true, purchaseBillDirect, totalCount });
        }


    }
}
  