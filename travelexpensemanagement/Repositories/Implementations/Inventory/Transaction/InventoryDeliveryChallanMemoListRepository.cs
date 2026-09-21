using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Inventory.Transaction
{
    public class InventoryDeliveryChallanMemoListRepository : IInventoryDeliveryChallanMemoListRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly LogService.LogService _logService;
        public InventoryDeliveryChallanMemoListRepository(GlobalVariableService globalVariableService, DataBaseConnection dbConnection, LogService.LogService logService)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
            _logService = logService;
        }
        const string doctype = "GTMO";
        public async Task<RepositoryResponseList<InventoryDeliveryChallanMemoModel>> GetAllDeliveryMemoList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new List<InventoryDeliveryChallanMemoModel>();
            int totalcount = 0;
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_InventoryDeliveryChallanMemo", con))
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
                                data.Add(new InventoryDeliveryChallanMemoModel
                                {
                                    V_NO = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]),
                                    V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]),
                                    EMP_NAME = reader["EMP_NAME"]?.ToString(),
                                    VENDOR_NAME = reader["VENDOR_NAME"]?.ToString(),
                                    TRANSPORT_NAME = reader["TRANSPORT_NAME"]?.ToString(),
                                    THROUGH = reader["THROUGH"]?.ToString(),
                                    REMARKS = reader["REMARKS"]?.ToString()
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


                return new RepositoryResponseList<InventoryDeliveryChallanMemoModel> { status = true, data = data, totalCount = totalcount };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<InventoryDeliveryChallanMemoModel> { status = false, message = ex.Message };
            }
        }
        public RepositoryResponse Delete(int vNo)
        {
            if (vNo <= 0)
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
                            using (SqlCommand cmd = new SqlCommand("sp_InventoryDeliveryChallanMemo", con, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@Action", "DELETE");
                                cmd.Parameters.AddWithValue("@V_NO", vNo);
                                cmd.Parameters.AddWithValue("@V_TYPE", doctype);
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                                cmd.ExecuteNonQuery();
                            }
                            tran.Commit();
                            //Log Service
                            //_logService.InsertLog("GATE_MEMO1", "Delivery Challan Memo", "Transaction", "DELETE", doctype, vNo.ToString(), null);
                            //_logService.InsertLog("GATE_MEMO2", "Delivery Challan Memo", "Transaction", "DELETE", doctype, vNo.ToString(), null);
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
    }
}
