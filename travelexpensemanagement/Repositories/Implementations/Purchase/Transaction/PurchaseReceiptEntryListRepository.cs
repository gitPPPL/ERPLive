using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class PurchaseReceiptEntryListRepository : IPurchaseReceiptEntryListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public PurchaseReceiptEntryListRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }

        public (List<object> Items, int TotalCount) GetPurchaseReceiptEntryList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var results = new List<object>();
            int totalCount = 0;

            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("InsertPurchaseReceiptHeader", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@SearchTerm",
                        string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new
                            {
                                SearchCode = reader["SearchCode"]?.ToString() ?? "",
                                VNo = reader["VNo"]?.ToString() ?? "",
                                VType = reader["VType"]?.ToString() ?? "",
                                VDate = reader["VDate"] ?? "",
                                PartyName = reader["PartyName"]?.ToString() ?? "",
                                BillNo = reader["bill_no"]?.ToString() ?? "",
                                BillDate = reader["BILL_DATE"] ?? "",
                                BillAdd1 = reader["BILL_ADD1"]?.ToString() ?? "",
                                BillAdd2 = reader["BILL_ADD2"]?.ToString() ?? "",
                                BillCity = reader["BILL_CITY"]?.ToString() ?? "",
                                BillGST = reader["BILL_GST"]?.ToString() ?? "",
                                ShipTo = reader["ShipTo"]?.ToString() ?? "",
                                Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
                                Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0,
                                Remarks = reader["Remarks"]?.ToString() ?? "",
                                TransportName = reader["Transport_Name"]?.ToString() ?? "",
                                GateNo = reader["GateNo"]?.ToString() ?? "",
                                Status = reader["Status"]?.ToString() ?? ""
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = Convert.ToInt32(reader[0]);
                        }
                    }
                }
            }

            return (results, totalCount);
        }


        public (bool Success, string Message) DeleteDocByCode(string vType, string vNo)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    //==========================
                    // Validation 1 : Purchase Invoice Exists
                    //==========================
                    string purchaseCheck = @"
                        SELECT TOP 1 V_NO, V_DATE
                        FROM PURCHASE1
                        WHERE REF_TYPE=@VType
                          AND REF_NO=@VNo
                          AND COMP_CODE=@CompCode
                          AND BRANCH_CODE=@BranchCode
                          AND YEAR_CODE=@YearCode";

                    using (SqlCommand cmd = new SqlCommand(purchaseCheck, con))
                    {
                        cmd.Parameters.AddWithValue("@VType", vType);
                        cmd.Parameters.AddWithValue("@VNo", vNo);
                        cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", globalVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                return (false,
                                    $"This document exists in Purchase Invoice Serial No : {dr["V_NO"]} dated : {Convert.ToDateTime(dr["V_DATE"]):dd/MM/yyyy}");
                            }
                        }
                    }

                    //==========================
                    // Validation 2 : QC Exists
                    //==========================
                    string qcCheck = @"
                    SELECT TOP 1 V_NO, V_DATE
                    FROM QC1
                    WHERE MRN_TYPE=@VType
                      AND MRN_NO=@VNo
                      AND COMP_CODE=@CompCode
                      AND BRANCH_CODE=@BranchCode";

                    using (SqlCommand cmd = new SqlCommand(qcCheck, con))
                    {
                        cmd.Parameters.AddWithValue("@VType", vType);
                        cmd.Parameters.AddWithValue("@VNo", vNo);
                        cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", globalVar.PubBranchCode);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                return (false,
                                    $"This document exists in QC Serial No : {dr["V_NO"]} dated : {Convert.ToDateTime(dr["V_DATE"]):dd/MM/yyyy}");
                            }
                        }
                    }

                    //==========================
                    // Validation 3 : Approval
                    //==========================
                    string approvalCheck = @"
                    SELECT 1
                    FROM APPROVAL_STATUS
                    WHERE V_TYPE=@VType
                      AND V_NO=@VNo
                      AND COMP_CODE=@CompCode
                      AND BRANCH_CODE=@BranchCode
                      AND YEAR_CODE=@YearCode
                      AND STATUS='OPEN'";

                    using (SqlCommand cmd = new SqlCommand(approvalCheck, con))
                    {
                        cmd.Parameters.AddWithValue("@VType", vType);
                        cmd.Parameters.AddWithValue("@VNo", vNo);
                        cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", globalVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);

                        if (cmd.ExecuteScalar() != null)
                        {
                            return (false, "This Document Approval is in process, Deletion not allowed.");
                        }
                    }

                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            //==========================
                            // Get Gate Details
                            //==========================
                            string gateType = "";
                            string gateNo = "";

                            string gateQuery = @"
                            SELECT TOP 1 GATE_TYPE,GATE_NO
                            FROM PURCHASE1
                            WHERE V_TYPE=@VType
                              AND V_NO=@VNo
                              AND COMP_CODE=@CompCode
                              AND BRANCH_CODE=@BranchCode
                              AND YEAR_CODE=@YearCode";

                            using (SqlCommand cmd = new SqlCommand(gateQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@VType", vType);
                                cmd.Parameters.AddWithValue("@VNo", vNo);
                                cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                                cmd.Parameters.AddWithValue("@BranchCode", globalVar.PubBranchCode);
                                cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);

                                using (SqlDataReader dr = cmd.ExecuteReader())
                                {
                                    if (dr.Read())
                                    {
                                        gateType = dr["GATE_TYPE"]?.ToString();
                                        gateNo = dr["GATE_NO"]?.ToString();
                                    }
                                }
                            }

                            //==========================
                            // Delete Purchase Tables
                            //==========================
                            string deleteSql = @"
                            DELETE FROM IMG_TABLE
                            WHERE V_TYPE=@VType AND V_NO=@VNo
                              AND COMP_CODE=@CompCode
                              AND BRANCH_CODE=@BranchCode
                              AND YEAR_CODE=@YearCode;

                            DELETE FROM PURCHASE2
                            WHERE V_TYPE=@VType AND V_NO=@VNo
                              AND COMP_CODE=@CompCode
                              AND BRANCH_CODE=@BranchCode
                              AND YEAR_CODE=@YearCode;

                            DELETE FROM PURCHASE1
                            WHERE V_TYPE=@VType AND V_NO=@VNo
                              AND COMP_CODE=@CompCode
                              AND BRANCH_CODE=@BranchCode
                              AND YEAR_CODE=@YearCode";

                            using (SqlCommand cmd = new SqlCommand(deleteSql, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@VType", vType);
                                cmd.Parameters.AddWithValue("@VNo", vNo);
                                cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                                cmd.Parameters.AddWithValue("@BranchCode", globalVar.PubBranchCode);
                                cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);

                                cmd.ExecuteNonQuery();
                            }

                            //==========================
                            // Update Gate1
                            //==========================
                            if (!string.IsNullOrEmpty(gateType) && !string.IsNullOrEmpty(gateNo))
                            {
                                string gateUpdate = @"
                                UPDATE GATE1
                                SET MRN_NO=NULL
                                WHERE V_TYPE=@GateType
                                  AND V_NO=@GateNo
                                  AND COMP_CODE=@CompCode
                                  AND BRANCH_CODE=@BranchCode
                                  AND YEAR_CODE=@YearCode";

                                using (SqlCommand cmd = new SqlCommand(gateUpdate, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@GateType", gateType);
                                    cmd.Parameters.AddWithValue("@GateNo", gateNo);
                                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BranchCode", globalVar.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);

                                    cmd.ExecuteNonQuery();
                                }
                            }

                            tran.Commit();
                            return (true, "Document deleted successfully.");
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

    }
}
