using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseReturnEntryListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public PurchaseReturnEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Purchase/Transaction/PurchaseReturnEntryList/Index.cshtml");
        }

        [HttpGet]
        public JsonResult GetPurchaseReceiptEntryList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var results = new List<object>();
            int totalCount = 0;
            var gv = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                //using (var cmd = new SqlCommand("InsertPurchaseReturnHeader", con))
                using (var cmd = new SqlCommand("InsertPurchaseReturnEntryHeader", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Set stored procedure parameters:
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    //cmd.Parameters.AddWithValue("@COMP_CODE", 1);
                    //cmd.Parameters.AddWithValue("@YEAR_CODE", 8);
                    //cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        // Read paginated data
                        while (reader.Read())
                        {
                            results.Add(new
                            {
                                SearchCode = reader["SearchCode"] ?? "",
                                VNo = reader["VNo"] ?? "",
                                VType = reader["VType"] ?? "",
                                VDate = reader["VDate"] ?? "",
                                PartyName = reader["PartyName"] ?? "",
                                BillNo = reader["bill_no"] ?? "",
                                BillDate = reader["BILL_DATE"] ?? "",
                                BillAdd1 = reader["BILL_ADD1"] ?? "",
                                BillAdd2 = reader["BILL_ADD2"] ?? "",
                                BillCity = reader["BILL_CITY"] ?? "",
                                BillGST = reader["BILL_GST"] ?? "",
                                ShipTo = reader["ShipTo"] ?? "",
                                Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
                                Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0,
                                Remarks = reader["Remarks"] ?? "",
                                TransportName = reader["Transport_Name"] ?? "",
                                GateNo = reader["GateNo"] ?? "",
                                Status = reader["Status"] ?? ""
                            });
                        }

                        // Move to second result set for total count
                        if (reader.NextResult())
                        {
                            if (reader.Read())
                            {
                                totalCount = reader.GetInt32(0);
                            }
                        }
                    }
                }
            }
            return Json(new { items = results, totalCount });
        }
    }
}
