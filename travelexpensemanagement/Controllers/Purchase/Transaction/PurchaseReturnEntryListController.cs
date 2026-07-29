using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseReturnEntryListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public PurchaseReturnEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
    ModuleService.ModuleService moduleService, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _logService = logService;
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



        [HttpPost]
        public async Task<IActionResult> Delete(string vNo, string docType, DateTime vDate)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            #region Check Approved Status

                            string approvalStatus = "";

                            using (SqlCommand cmd = new SqlCommand(@" SELECT ISNULL(FAPROV_STATUS,'') FROM PURCHASE1
                                WHERE V_TYPE=@VType AND V_NO=@VNo AND COMP_CODE=@CompCode AND BRANCH_CODE=@BranchCode
                                AND YEAR_CODE=@YearCode", con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@VType", docType);
                                cmd.Parameters.AddWithValue("@VNo", vNo);
                                cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                                cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

                                var result = await cmd.ExecuteScalarAsync();
                                approvalStatus = result?.ToString() ?? "";
                            }

                            if (approvalStatus == "Approved")
                            {
                                transaction.Rollback();

                                return Json(new
                                {
                                    success = false,
                                    message = "This Document has been Approved. Deletion not allowed."
                                });
                            }

                            #endregion

                            #region Approval In Process

                            bool approvalOpen = false;

                            using (SqlCommand cmd = new SqlCommand(@" SELECT COUNT(*) FROM APPROVAL_STATUS WHERE V_TYPE=@VType
                                AND V_NO=@VNo AND STATUS='OPEN' AND COMP_CODE=@CompCode AND BRANCH_CODE=@BranchCode
                                AND YEAR_CODE=@YearCode", con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@VType", docType);
                                cmd.Parameters.AddWithValue("@VNo", vNo);
                                cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                                cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

                                approvalOpen = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                            }

                            if (approvalOpen)
                            {
                                transaction.Rollback();

                                return Json(new
                                {
                                    success = false,
                                    message = "This Document Approval is in process. Deletion not allowed."
                                });
                            }

                            #endregion

                            #region Delete

                            string deleteQuery = @" DELETE FROM IMG_TABLE WHERE V_TYPE=@VType AND V_NO=@VNo AND COMP_CODE=@CompCode
                                AND BRANCH_CODE=@BranchCode AND YEAR_CODE=@YearCode;
                                DELETE FROM PURCHASE2 WHERE V_TYPE=@VType AND V_NO=@VNo AND COMP_CODE=@CompCode AND BRANCH_CODE=@BranchCode
                                AND YEAR_CODE=@YearCode;
                                DELETE FROM PURCHASE1 WHERE V_TYPE=@VType AND V_NO=@VNo AND COMP_CODE=@CompCode AND BRANCH_CODE=@BranchCode
                                AND YEAR_CODE=@YearCode;";

                            int rows;

                            using (SqlCommand cmd = new SqlCommand(deleteQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@VType", docType);
                                cmd.Parameters.AddWithValue("@VNo", vNo);
                                cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                                cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);
                                rows = await cmd.ExecuteNonQueryAsync();
                            }

                            if (rows <= 0)
                            {
                                transaction.Rollback();

                                return Json(new
                                {
                                    success = false,
                                    message = "Record not found."
                                });
                            }
                            #endregion
                            transaction.Commit();

                            #region Ledger Posting
                            //Same as VB Code
                            //await _accountPostingService.ACTPostingPurchaseReturn("LEDGER2",vDate,vDate, docType, vNo);
                            #endregion

                            #region Log Entry
                            string action = "Delete";

                            _logService.InsertLog("PURCHASE1", "purchase Return Entry", "TRANSACTION", action, docType, vNo, vDate);

                            #endregion

                            return Json(new
                            {
                                success = true,
                                message = "Record deleted successfully."
                            });
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        //[HttpPost]
        //public async Task<IActionResult> Delete(string vNo, string docType)
        //{
        //    try
        //    {
        //        var gv = _globalVariableService.GetGlobalVariables();

        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            await con.OpenAsync();

        //            using (SqlTransaction transaction = con.BeginTransaction())
        //            {
        //                try
        //                {
        //                    // PURCHASE3
        //                    var cmd3 = new SqlCommand(@"
        //                DELETE FROM PURCHASE3
        //                WHERE V_TYPE=@VType
        //                AND V_NO=@VNo
        //                AND YEAR_CODE=@YearCode
        //                AND COMP_CODE=@CompCode
        //                AND BRANCH_CODE=@BranchCode", con, transaction);

        //                    cmd3.Parameters.AddWithValue("@VType", docType);
        //                    cmd3.Parameters.AddWithValue("@VNo", vNo);
        //                    cmd3.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);
        //                    cmd3.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
        //                    cmd3.Parameters.AddWithValue("@BranchCode", 1);

        //                    await cmd3.ExecuteNonQueryAsync();

        //                    // PURCHASE2
        //                    var cmd2 = new SqlCommand(@"
        //                DELETE FROM PURCHASE2
        //                WHERE V_TYPE=@VType
        //                AND V_NO=@VNo
        //                AND YEAR_CODE=@YearCode
        //                AND COMP_CODE=@CompCode
        //                AND BRANCH_CODE=@BranchCode", con, transaction);

        //                    cmd2.Parameters.AddWithValue("@VType", docType);
        //                    cmd2.Parameters.AddWithValue("@VNo", vNo);
        //                    cmd2.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);
        //                    cmd2.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
        //                    cmd2.Parameters.AddWithValue("@BranchCode", 1);

        //                    await cmd2.ExecuteNonQueryAsync();

        //                    // PURCHASE1
        //                    var cmd1 = new SqlCommand(@"
        //                DELETE FROM PURCHASE1
        //                WHERE V_TYPE=@VType
        //                AND V_NO=@VNo
        //                AND YEAR_CODE=@YearCode
        //                AND COMP_CODE=@CompCode
        //                AND BRANCH_CODE=@BranchCode", con, transaction);

        //                    cmd1.Parameters.AddWithValue("@VType", docType);
        //                    cmd1.Parameters.AddWithValue("@VNo", vNo);
        //                    cmd1.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);
        //                    cmd1.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
        //                    cmd1.Parameters.AddWithValue("@BranchCode", 1);

        //                    int rows = await cmd1.ExecuteNonQueryAsync();

        //                    transaction.Commit();

        //                    if (rows > 0)
        //                    {
        //                        return Json(new
        //                        {
        //                            success = true,
        //                            message = "Record deleted successfully."
        //                        });
        //                    }

        //                    return Json(new
        //                    {
        //                        success = false,
        //                        message = "Record not found."
        //                    });
        //                }
        //                catch
        //                {
        //                    transaction.Rollback();
        //                    throw;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}
    }
}
