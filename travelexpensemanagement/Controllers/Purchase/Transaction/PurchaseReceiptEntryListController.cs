using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseReceiptEntryListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public PurchaseReceiptEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Purchase/Transaction/PurchaseReceiptEntryList/Index.cshtml");
        }

        [HttpGet]
        public JsonResult GetPurchaseReceiptEntryList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var results = new List<object>();
            int totalCount = 0;
            var gv = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand("InsertPurchaseReceiptHeader", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Set stored procedure parameters:
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
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

        [HttpPost]
        public JsonResult DeleteDocByCode(string vType, string vNo)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    string sql = "DELETE FROM PURCHASE1 WHERE V_TYPE = @VType AND V_NO = @VNo";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@VType", vType);
                        cmd.Parameters.AddWithValue("@VNo", vNo);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true, message = "Document deleted successfully." });
                        }
                        else
                        {
                            return Json(new { success = false, message = "No matching record found to delete." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Optional: Log ex.Message or ex.ToString()
                return Json(new { success = false, message = "Error deleting document." });
            }
        }



    }
}
