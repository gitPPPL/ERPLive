using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

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
        private readonly IPurchaseReceiptEntryListRepository _purchaseReceiptEntryListRepository;
        public PurchaseReceiptEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper,ModuleService.ModuleService moduleService, IPurchaseReceiptEntryListRepository purchaseReceiptEntryListRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _purchaseReceiptEntryListRepository = purchaseReceiptEntryListRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/PurchaseReceiptEntryList/Index.cshtml");
        }

        [HttpGet]
        public JsonResult GetPurchaseReceiptEntryList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var result = _purchaseReceiptEntryListRepository.GetPurchaseReceiptEntryList(searchTerm, pageNumber, pageSize);

            return Json(new
            {
                items = result.Items,
                totalCount = result.TotalCount
            });
        }

        [HttpPost]
        public JsonResult DeleteDocByCode(string vType, string vNo)
        {
            var result = _purchaseReceiptEntryListRepository.DeleteDocByCode(vType, vNo);

            return Json(new
            {
                success = result.Success,
                message = result.Message
            });
        }

        //[HttpGet]
        //public JsonResult GetPurchaseReceiptEntryList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        //{
        //    var results = new List<object>();
        //    int totalCount = 0;
        //    var gv = _globalVariableService.GetGlobalVariables();
        //    using (SqlConnection con = _dbConnection.GetErpConnection())
        //    {
        //        using (var cmd = new SqlCommand("InsertPurchaseReceiptHeader", con))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;

        //            // Set stored procedure parameters:
        //            cmd.Parameters.AddWithValue("@Action", "SELECT");
        //            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
        //            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
        //            cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
        //            cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? (object)DBNull.Value : searchTerm);
        //            cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
        //            cmd.Parameters.AddWithValue("@PageSize", pageSize);

        //            con.Open();

        //            using (var reader = cmd.ExecuteReader())
        //            {
        //                // Read paginated data
        //                while (reader.Read())
        //                {
        //                    results.Add(new
        //                    {
        //                        SearchCode = reader["SearchCode"] ?? "",
        //                        VNo = reader["VNo"] ?? "",
        //                        VType = reader["VType"] ?? "",
        //                        VDate = reader["VDate"] ?? "",
        //                        PartyName = reader["PartyName"] ?? "",
        //                        BillNo = reader["bill_no"] ?? "",
        //                        BillDate = reader["BILL_DATE"] ?? "",
        //                        BillAdd1 = reader["BILL_ADD1"] ?? "",
        //                        BillAdd2 = reader["BILL_ADD2"] ?? "",
        //                        BillCity = reader["BILL_CITY"] ?? "",
        //                        BillGST = reader["BILL_GST"] ?? "",
        //                        ShipTo = reader["ShipTo"] ?? "",
        //                        Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
        //                        Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0,
        //                        Remarks = reader["Remarks"] ?? "",
        //                        TransportName = reader["Transport_Name"] ?? "",
        //                        GateNo = reader["GateNo"] ?? "",
        //                        Status = reader["Status"] ?? ""
        //                    });
        //                }

        //                // Move to second result set for total count
        //                if (reader.NextResult())
        //                {
        //                    if (reader.Read())
        //                    {
        //                        totalCount = reader.GetInt32(0);
        //                    }
        //                }
        //            }
        //        }
        //    }       
        //    return Json(new { items = results, totalCount });
        //}



        //[HttpPost]
        //public JsonResult DeleteDocByCode(string vType, string vNo)
        //{
        //    try
        //    {
        //        var globalVar = _globalVariableService.GetGlobalVariables();

        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            con.Open();

        //            //==========================
        //            // Validation 1 : Purchase Invoice Exists
        //            //==========================
        //            string purchaseCheck = @"
        //            SELECT TOP 1 V_NO, V_DATE
        //            FROM PURCHASE1
        //            WHERE REF_TYPE=@VType
        //              AND REF_NO=@VNo
        //              AND COMP_CODE=@CompCode
        //              AND BRANCH_CODE=@BranchCode
        //              AND YEAR_CODE=@YearCode";

        //            using (SqlCommand cmd = new SqlCommand(purchaseCheck, con))
        //            {
        //                cmd.Parameters.AddWithValue("@VType", vType);
        //                cmd.Parameters.AddWithValue("@VNo", vNo);
        //                cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
        //                cmd.Parameters.AddWithValue("@BranchCode", globalVar.PubBranchCode);
        //                cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);

        //                using (SqlDataReader dr = cmd.ExecuteReader())
        //                {
        //                    if (dr.Read())
        //                    {
        //                        return Json(new
        //                        {
        //                            success = false,
        //                            message = $"This document exists in Purchase Invoice Serial No : {dr["V_NO"]} dated : {Convert.ToDateTime(dr["V_DATE"]):dd/MM/yyyy}"
        //                        });
        //                    }
        //                }
        //            }

        //            //==========================
        //            // Validation 2 : QC Exists
        //            //==========================
        //            string qcCheck = @"
        //            SELECT TOP 1 V_NO, V_DATE
        //            FROM QC1
        //            WHERE MRN_TYPE=@VType
        //              AND MRN_NO=@VNo
        //              AND COMP_CODE=@CompCode
        //              AND BRANCH_CODE=@BranchCode";

        //            using (SqlCommand cmd = new SqlCommand(qcCheck, con))
        //            {
        //                cmd.Parameters.AddWithValue("@VType", vType);
        //                cmd.Parameters.AddWithValue("@VNo", vNo);
        //                cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
        //                cmd.Parameters.AddWithValue("@BranchCode", globalVar.PubBranchCode);

        //                using (SqlDataReader dr = cmd.ExecuteReader())
        //                {
        //                    if (dr.Read())
        //                    {
        //                        return Json(new
        //                        {
        //                            success = false,
        //                            message = $"This document exists in QC Serial No : {dr["V_NO"]} dated : {Convert.ToDateTime(dr["V_DATE"]):dd/MM/yyyy}"
        //                        });
        //                    }
        //                }
        //            }

        //            //==========================
        //            // Validation 3 : Approval
        //            //==========================
        //            string approvalCheck = @"
        //            SELECT 1
        //            FROM APPROVAL_STATUS
        //            WHERE V_TYPE=@VType
        //              AND V_NO=@VNo
        //              AND COMP_CODE=@CompCode
        //              AND BRANCH_CODE=@BranchCode
        //              AND YEAR_CODE=@YearCode
        //              AND STATUS='OPEN'";

        //            using (SqlCommand cmd = new SqlCommand(approvalCheck, con))
        //            {
        //                cmd.Parameters.AddWithValue("@VType", vType);
        //                cmd.Parameters.AddWithValue("@VNo", vNo);
        //                cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
        //                cmd.Parameters.AddWithValue("@BranchCode", globalVar.PubBranchCode);
        //                cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);

        //                if (cmd.ExecuteScalar() != null)
        //                {
        //                    return Json(new
        //                    {
        //                        success = false,
        //                        message = "This Document Approval is in process, Deletion not allowed."
        //                    });
        //                }
        //            }

        //            using (SqlTransaction tran = con.BeginTransaction())
        //            {
        //                try
        //                {
        //                    //==========================
        //                    // Get Gate Details
        //                    //==========================
        //                    string gateType = "";
        //                    string gateNo = "";

        //                    string gateQuery = @"
        //                    SELECT TOP 1 GATE_TYPE,GATE_NO
        //                    FROM PURCHASE1
        //                    WHERE V_TYPE=@VType
        //                      AND V_NO=@VNo
        //                      AND COMP_CODE=@CompCode
        //                      AND BRANCH_CODE=@BranchCode
        //                      AND YEAR_CODE=@YearCode";

        //                    using (SqlCommand cmd = new SqlCommand(gateQuery, con, tran))
        //                    {
        //                        cmd.Parameters.AddWithValue("@VType", vType);
        //                        cmd.Parameters.AddWithValue("@VNo", vNo);
        //                        cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
        //                        cmd.Parameters.AddWithValue("@BranchCode", globalVar.PubBranchCode);
        //                        cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);

        //                        using (SqlDataReader dr = cmd.ExecuteReader())
        //                        {
        //                            if (dr.Read())
        //                            {
        //                                gateType = dr["GATE_TYPE"]?.ToString();
        //                                gateNo = dr["GATE_NO"]?.ToString();
        //                            }
        //                        }
        //                    }

        //                    //==========================
        //                    // Delete Purchase Tables
        //                    //==========================
        //                    string deleteSql = @"
        //                    DELETE FROM IMG_TABLE
        //                    WHERE V_TYPE=@VType AND V_NO=@VNo
        //                      AND COMP_CODE=@CompCode
        //                      AND BRANCH_CODE=@BranchCode
        //                      AND YEAR_CODE=@YearCode;

        //                    DELETE FROM PURCHASE2
        //                    WHERE V_TYPE=@VType AND V_NO=@VNo
        //                      AND COMP_CODE=@CompCode
        //                      AND BRANCH_CODE=@BranchCode
        //                      AND YEAR_CODE=@YearCode;

        //                    DELETE FROM PURCHASE1
        //                    WHERE V_TYPE=@VType AND V_NO=@VNo
        //                      AND COMP_CODE=@CompCode
        //                      AND BRANCH_CODE=@BranchCode
        //                      AND YEAR_CODE=@YearCode";

        //                    using (SqlCommand cmd = new SqlCommand(deleteSql, con, tran))
        //                    {
        //                        cmd.Parameters.AddWithValue("@VType", vType);
        //                        cmd.Parameters.AddWithValue("@VNo", vNo);
        //                        cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
        //                        cmd.Parameters.AddWithValue("@BranchCode", globalVar.PubBranchCode);
        //                        cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);

        //                        cmd.ExecuteNonQuery();
        //                    }

        //                    //==========================
        //                    // Update Gate1
        //                    //==========================
        //                    if (!string.IsNullOrEmpty(gateType) && !string.IsNullOrEmpty(gateNo))
        //                    {
        //                        string gateUpdate = @"
        //                        UPDATE GATE1
        //                        SET MRN_NO=NULL
        //                        WHERE V_TYPE=@GateType
        //                          AND V_NO=@GateNo
        //                          AND COMP_CODE=@CompCode
        //                          AND BRANCH_CODE=@BranchCode
        //                          AND YEAR_CODE=@YearCode";

        //                        using (SqlCommand cmd = new SqlCommand(gateUpdate, con, tran))
        //                        {
        //                            cmd.Parameters.AddWithValue("@GateType", gateType);
        //                            cmd.Parameters.AddWithValue("@GateNo", gateNo);
        //                            cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
        //                            cmd.Parameters.AddWithValue("@BranchCode", globalVar.PubBranchCode);
        //                            cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);

        //                            cmd.ExecuteNonQuery();
        //                        }
        //                    }

        //                    tran.Commit();

        //                    return Json(new
        //                    {
        //                        success = true,
        //                        message = "Document deleted successfully."
        //                    });
        //                }
        //                catch
        //                {
        //                    tran.Rollback();
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
