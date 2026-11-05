using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class TransportRateRequestForQuotationListRepository : ITransportRateRequestForQuotationListRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly LogService.LogService _logService;
        public TransportRateRequestForQuotationListRepository(GlobalVariableService globalVariableService, DataBaseConnection dbConnection, LogService.LogService logService)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
            _logService = logService;
        }

        public RepositoryResponseList<TransportRateRequestForQuotationListModel> GetAllTransportRateQuotationList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var globelVar = _globalVariableService.GetGlobalVariables();
            var transportRateQuotationList = new List<TransportRateRequestForQuotationListModel>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_TransportRateRequestForQuotation", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globelVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globelVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globelVar.PubFYearCode);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                transportRateQuotationList.Add(new TransportRateRequestForQuotationListModel
                                {
                                    VNo = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                    VType = reader["V_TYPE"]?.ToString(),
                                    VDate = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null,
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
                return new RepositoryResponseList<TransportRateRequestForQuotationListModel> { status = false, message = "Error fetching puchase bill pass" + ex.Message };
            }

            return new RepositoryResponseList<TransportRateRequestForQuotationListModel> { status = true, data = transportRateQuotationList, totalCount = totalCount };
        }

        public RepositoryResponse DeleteTransportRateQuotationEntry(int docId, string docType)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_TransportRateRequestForQuotation", con, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Action", "DELETE");
                            cmd.Parameters.AddWithValue("@V_NO", docId);
                            cmd.Parameters.AddWithValue("@V_Type", docType);
                            cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", getGlobalCode.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                            try
                            {
                                cmd.ExecuteNonQuery();
                                tran.Commit();
                                //_logService.InsertLog("RFQ1", "Transport Rate Quotation", "Transaction", "Delete", docType, docId.ToString(), null);
                                //_logService.InsertLog("RFQ2", "Transport Rate Quotation", "Transaction", "Delete", docType, docId.ToString(), null);
                                return new RepositoryResponse { status = true, message = "Transport Rate Quotation deleted successfully." };
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                return new RepositoryResponse { status = false, message = "Error deleting Transport Rate Quotation." + ex.Message };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "Error deleting Transport Rate Quotation." + ex.Message };
            }
        }
    }
}
