using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transiction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class PurchaseBillPassEntryDirectListRepository : IPurchaseBillPassEntryDirectListRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;

        public PurchaseBillPassEntryDirectListRepository(GlobalVariableService globalVariableService, DataBaseConnection dbConnection)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
        }

        public RepositoryResponseList<PURCHASE1> GetAllPurchaseBillPassEntry(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
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
                        cmd.Parameters.AddWithValue("@COMP_CODE", globelVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globelVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globelVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@Doctype", "HighSeaPurchase");

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                purchaseBillDirect.Add(new PURCHASE1
                                {
                                    V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null,
                                    PARTY_NAME = reader["PARTY_NAME"]?.ToString(),
                                    SHIP_NAME = reader["SHIP_NAME"]?.ToString(),
                                    DEBIT_AC_NAME = reader["DEBIT_AC_NAME"].ToString(),
                                    CREDIT_AC_NAME = reader["CREDIT_AC_NAME"].ToString(),
                                    BILL_QTY = reader["BILL_QTY"] != DBNull.Value ? Convert.ToDecimal(reader["BILL_QTY"]) : 0,
                                    NAMOUNT = reader["NAMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["NAMOUNT"]) : 0,
                                    REF_TYPE = reader["REF_TYPE"]?.ToString(),
                                    REF_NO = reader["REF_NO"] != DBNull.Value ? Convert.ToInt32(reader["REF_NO"]) : 0,
                                    BILL_NO = reader["BILL_NO"]?.ToString(),
                                    BILL_DATE = reader["BILL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["BILL_DATE"]) : null,
                                    CHALL_NO = reader["CHALL_NO"]?.ToString(),
                                    CHALL_DATE = reader["CHALL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["CHALL_DATE"]) : null,
                                    DR_FROM_TPT = reader["DR_FROM_TPT"]?.ToString(),
                                    REMARKS = reader["REMARKS"]?.ToString(),
                                    STATUS = reader["STATUS"] != DBNull.Value ? Convert.ToInt32(reader["STATUS"]) : 0,
                                    TRANSPORT_NAME = reader["Transport_name"]?.ToString(),
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
                return new RepositoryResponseList<PURCHASE1> { status = false, message = "Error fetching puchase bill pass" + ex.Message };
            }

            return new RepositoryResponseList<PURCHASE1> { status = true, data = purchaseBillDirect, totalCount = totalCount };
        }

        public RepositoryResponse DeletePurchaseBillPass(int docId, string docType)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Action", "DELETE");
                            cmd.Parameters.AddWithValue("@V_NO", docId);
                            cmd.Parameters.AddWithValue("@V_Type", docType);
                            cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", getGlobalCode.PubBranchCode);
                            try
                            {
                                cmd.ExecuteNonQuery();
                                tran.Commit();
                                //_logService.InsertLog("PREQUEST1", "Purchase Request", "Transaction", "Delete", "STPI", docId.ToString(), null);
                                return new RepositoryResponse { status = true, message = " Purchase Bill Pass Entry deleted successfully." };
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                return new RepositoryResponse { status = false, message = "Error deleting Purchase Bill Pass Entry." + ex.Message };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "Error deleting Purchase Bill Pass Entry." + ex.Message };
            }
        }

        //Edit
        public PurchaseEditStatus GetPurchaseEditStatus(string vType, int vNo)
        {
            var result = new PurchaseEditStatus();
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "EditStatusBeforeEdit");
                    cmd.Parameters.AddWithValue("@v_type", vType);
                    cmd.Parameters.AddWithValue("@v_no", vNo);
                    cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@branch_code", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@year_code", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@USER_CODE", gv.PubUserId);

                    if (con.State != ConnectionState.Open)
                        con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            // Approved status
                            result.IsApproved = !dr.IsDBNull(dr.GetOrdinal("FAPROV_STATUS"))
                                                && dr["FAPROV_STATUS"].ToString() == "Approved";
                            // Approval in process
                            result.ApprovalUser = dr.IsDBNull(dr.GetOrdinal("OpenApprovalUser"))
                                                    ? "" : dr["OpenApprovalUser"].ToString();
                            result.IsApprovalInProcess = !string.IsNullOrEmpty(result.ApprovalUser);

                            // Final Approval Body
                            result.IsFinalApprovalBody = !dr.IsDBNull(dr.GetOrdinal("FinalApprovalUser")) &&
                                                            dr["FinalApprovalUser"].ToString()
                                                            .Equals("Final", StringComparison.OrdinalIgnoreCase);

                            // EA flag
                            result.EAFlag = dr.IsDBNull(dr.GetOrdinal("EAFlag")) ? "" : dr["EAFlag"].ToString();
                        }
                    }
                }
            }

            return result;
        }

        public PurchaseDeleteStatus GetPurchaseDeleteStatus(string vType, int vNo)
        {
            var result = new PurchaseDeleteStatus();
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "DeleteStatusBeforeDelete");
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                if (con.State != ConnectionState.Open)
                    con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        result.IsApprovalInProcess = Convert.ToInt32(dr["IsApprovalInProcess"]) == 1;

                        result.ExistsInLedger = !dr.IsDBNull(dr.GetOrdinal("LedgerNo"));

                        if (result.ExistsInLedger)
                        {
                            result.LedgerNo = dr["LedgerNo"].ToString();

                            result.LedgerDate = dr["LedgerDate"].ToString();
                        }
                    }
                }
            }

            return result;
        }
    }
}
