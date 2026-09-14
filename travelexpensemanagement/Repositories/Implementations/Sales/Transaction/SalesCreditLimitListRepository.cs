using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class SalesCreditLimitListRepository : ISalesCreditLimitListRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly LogService.LogService _logService;
        public SalesCreditLimitListRepository(GlobalVariableService globalVariableService, DataBaseConnection dbConnection, LogService.LogService logService)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
            _logService = logService;
        }
        public RepositoryResponseList<SalesCreditLimitListModel> GetAllSalesCreditLimitList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new List<SalesCreditLimitListModel>();
            int totalcount = 0;
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_SalesCreditLimit", con))
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
                                data.Add(new SalesCreditLimitListModel
                                {
                                    V_NO = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]),
                                    V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]),
                                    PARTY_NAME = reader["PARTY_NAME"]?.ToString(),
                                    CR_LIMIT = reader["CR_LIMIT"] == DBNull.Value ? null : Convert.ToDecimal(reader["CR_LIMIT"]),
                                    CR_DAYS = reader["CR_DAYS"] == DBNull.Value ? null : Convert.ToInt32(reader["CR_DAYS"]),
                                    OURCR_DAYS = reader["OURCR_DAYS"] == DBNull.Value ? null : Convert.ToInt32(reader["OURCR_DAYS"]),
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


                return new RepositoryResponseList<SalesCreditLimitListModel> { status = true, data = data, totalCount = totalcount };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<SalesCreditLimitListModel> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponse Delete(string doctype, int vNo)
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
                            using (SqlCommand cmd = new SqlCommand("sp_SalesCreditLimit", con, tran))
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
