using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Data;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class SalesExportCostingListRepository : ISalesExportCostingListRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly LogService.LogService _logService;
        
        public SalesExportCostingListRepository(GlobalVariableService globalVariableService, DataBaseConnection dbConnection, LogService.LogService logService)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
            _logService = logService;
        }
        const string doctype = "EXPC";
        
        public RepositoryResponseList<SalesExportCostingListModel> GetAllSalesExportCostingList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new List<SalesExportCostingListModel>();
            int totalcount = 0;
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_SalesExportCosting", con))
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
                                data.Add(new SalesExportCostingListModel
                                {
                                    V_NO = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]),
                                    V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]),
                                    PARTY_NAME = reader["PARTY_NAME"]?.ToString(),
                                    DELIVERY_AT = reader["DELIVERY_AT"]?.ToString(),
                                    AGENT_NAME = reader["AGENT_NAME"]?.ToString(),
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


                return new RepositoryResponseList<SalesExportCostingListModel> { status = true, data = data, totalCount = totalcount };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<SalesExportCostingListModel> { status = false, message = ex.Message };
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
                            using (SqlCommand cmd = new SqlCommand("sp_SalesExportCosting", con, tran))
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
                            //_logService.InsertLog("COSTING_EXPORT1", "Sales Export Costing", "Transaction", "DELETE", doctype, vNo.ToString(), null);
                            //_logService.InsertLog("COSTING_EXPORT2", "Sales Export Costing", "Transaction", "DELETE", doctype, vNo.ToString(), null);
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
