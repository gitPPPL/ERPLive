using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public class DeliveryChallanStoreListRepository : IDeliveryChallanStoreListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        public DeliveryChallanStoreListRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DbHelper dbHelper)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
        }
        public RepositoryResponseList<DeliveryChallanStoreListModel> GetAllDeliveryChallanList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new List<DeliveryChallanStoreListModel>();
            int totalcount = 0;
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_DeliveryChallanStore", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                data.Add(new DeliveryChallanStoreListModel
                                {
                                    DOC_ID = reader["DOC_ID"]?.ToString(),
                                    V_NO = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]),
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]),
                                    DOC_DATE = reader["DOC_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["DOC_DATE"]),
                                    BILL_NO = reader["BILL_NO"]?.ToString(),
                                    BILL_DATE = reader["BILL_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["BILL_DATE"]),
                                    ITEM_TYPE = reader["ITEM_TYPE"]?.ToString(),
                                    BilledTo = reader["BilledTo"]?.ToString(),
                                    BrokerName = reader["BrokerName"]?.ToString(),
                                    CrName = reader["CrName"]?.ToString()
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
                return new RepositoryResponseList<DeliveryChallanStoreListModel> { status = true, data = data, totalCount = totalcount };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<DeliveryChallanStoreListModel> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponse Delete(int vNo, string docType)
        {
            if (vNo <= 0 || string.IsNullOrEmpty(docType))
            {
                return new RepositoryResponse { status = false, message = "Invalid Id!" };
            }
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("sp_DeliveryChallanStore", con, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@Action", "DELETE");
                                cmd.Parameters.AddWithValue("@V_NO", vNo);
                                cmd.Parameters.AddWithValue("@V_TYPE", docType);
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                                cmd.ExecuteNonQuery();
                            }
                            tran.Commit();
                            return new RepositoryResponse { status = true, message = "Deleted successfully!" };
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            return new RepositoryResponse { status = false, message = ex.Message };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponseData<bool>> GetApprovalStatus(int vNo, string vType)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                string qry = $@"select 1 from APPROVAL_STATUS where v_type= {vType} and V_NO={vNo} and comp_code={gv.PubCompCode} and 
                                branch_code={gv.PubBranchCode} and year_code={gv.PubFYearCode} and status='OPEN' and user_code<>{gv.PubUserId}";

                bool exists = await _dbHelper.GetExecuteScalarAsync<int>(qry) == 1;
                return new RepositoryResponseData<bool> { status = true, data = exists };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, message = ex.Message };

            }
        }
    }
}
