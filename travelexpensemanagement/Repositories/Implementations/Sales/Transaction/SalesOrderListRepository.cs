using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class SalesOrderListRepository : ISalesOrderListRepository
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        public SalesOrderListRepository(DataBaseConnection dbcontext, GlobalVariableService globalValue)
        {
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public RepositoryResponseList<SalesOrderListModel> GetSalesOrderList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var gv = _globalValue.GetGlobalVariables();
            var data = new List<SalesOrderListModel>();
            int totalcount = 0;
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_SalesOrder", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SalesOrderList");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", "SORD");

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                data.Add(new SalesOrderListModel
                                {
                                    V_NO = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]),
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]),
                                    PartyName = reader["PartyName"]?.ToString(),
                                    DELIVERY_PERIOD = reader["DELIVERY_PERIOD"]?.ToString(),
                                    DELIVERY_TO = reader["DELIVERY_TO"]?.ToString(),
                                    SAUDA_NO = reader["SAUDA_NO"]?.ToString(),
                                    DOC_ID = reader["DOC_ID"]?.ToString()
                                });
                            }

                            if (reader.NextResult())
                            {
                                if (reader.Read())
                                {
                                    totalcount = reader["TotalCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalCount"]);
                                }
                            }
                        }
                    }
                }


                return new RepositoryResponseList<SalesOrderListModel> { status = true, data = data, totalCount = totalcount };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<SalesOrderListModel> { status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponse> DeleteSalesOrderEntry(int vNo, string docType)
        {
            try
            {
                if (string.IsNullOrEmpty(docType) || vNo <= 0)
                {
                    return new RepositoryResponse { status = false, message = "Invalid ID" };
                }

                var userSession = _globalValue.GetGlobalVariables();

                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {

                            string[] deleteQueries = {
                        "DELETE FROM ORDER1 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO",
                        "DELETE FROM ORDER2 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO",
                        //"DELETE FROM ORDER3 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO"
                        "DELETE FROM ORDER_DELPLAN WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO"
                        };
                            foreach (var query in deleteQueries)
                            {
                                using (var cmd = new SqlCommand(query, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@V_TYPE", docType);
                                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            return new RepositoryResponse { status = true, message = "Data deleted successfully" };
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return new RepositoryResponse { status = false, message = $"Delete failed: {ex.Message}" };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<PurchaseEditStatus> GetSalesEditStatus(string vType, int vNo)
        {
            try
            {
                var result = new PurchaseEditStatus();
                var gv = _globalValue.GetGlobalVariables();

                using (SqlConnection con = _dbcontext.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_SalesOrder", con))
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
                            result.ApprovalUser = dr["OpenApprovalUser"] == DBNull.Value ? "" : dr["OpenApprovalUser"].ToString();
                            result.IsApprovalInProcess = dr["status"] != DBNull.Value && dr["status"].ToString().ToUpper() == "OPEN";
                        }

                        // Second query - Approval Body
                        if (dr.NextResult() && dr.Read())
                        {
                            result.IsFinalApprovalBody = dr["APPROV_USER"] != DBNull.Value;
                        }
                    }
                }

                return new RepositoryResponseData<PurchaseEditStatus> { status = true, data = result };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<PurchaseEditStatus> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<object> CheckSaleInvoiceBeforeDelete(string vType, int vNo)
        {
            try
            {
                var gv = _globalValue.GetGlobalVariables();

                string qry = @"SELECT TOP 1 v_no, FORMAT(v_date, 'dd/MM/yyyy') AS v_date FROM SALE2 WHERE Status <> 2 AND ORD_TYPE = @ORD_TYPE AND ORD_NO = @ORD_NO
                                AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";

                using (SqlConnection con = _dbcontext.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(qry, con))
                {
                    cmd.Parameters.AddWithValue("@ORD_TYPE", vType);
                    cmd.Parameters.AddWithValue("@ORD_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                    if (con.State != ConnectionState.Open)
                        con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            return new RepositoryResponseData<object> { status = true,
                                data = new
                                {
                                    exists = true,
                                    billNo = dr["v_no"].ToString(),
                                    billDate = dr["v_date"].ToString()
                                }
                            };
                        }
                    }
                }

                return new RepositoryResponseData<object>
                {
                    status = true,
                    data = new
                    {
                        exists = false,
                        billNo = (string)null,
                        billDate = (string)null
                    }
                };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<object> { status = false, message = ex.Message };
            }
        }
    }
}
