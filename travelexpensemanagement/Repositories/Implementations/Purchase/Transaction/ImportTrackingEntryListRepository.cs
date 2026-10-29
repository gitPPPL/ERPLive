using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class ImportTrackingEntryListRepository : IImportTrackingEntryListRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly LogService.LogService _logService;
        public ImportTrackingEntryListRepository(GlobalVariableService globalVariableService, DataBaseConnection dbConnection, LogService.LogService logService)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
            _logService = logService;
        }

        public RepositoryResponseList<ImportTrackingEntryListModel> GetAllImportTrackingList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var globelVar = _globalVariableService.GetGlobalVariables();
            var importList = new List<ImportTrackingEntryListModel>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ImportTrackingEntry", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globelVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globelVar.PubBranchCode);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                importList.Add(new ImportTrackingEntryListModel
                                {
                                    V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                    V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null,
                                    SAUDA_NO = reader["SAUDA_NO"] != DBNull.Value ? Convert.ToInt32(reader["SAUDA_NO"]) : 0,
                                    SAUDA_DATE = reader["SAUDA_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["SAUDA_DATE"]) : null,
                                    V_TYPE = reader["V_TYPE"].ToString(),
                                    SUPPLIER = reader["SUPPLIER_NAME"].ToString(),
                                    LOCATION = reader["DEL_LOCATION"].ToString(),
                                    INVOICE_NO = reader["SUPPLIER_INVNO"].ToString()
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
                return new RepositoryResponseList<ImportTrackingEntryListModel> { status = false, message = "Error fetching puchase bill pass" + ex.Message };
            }

            return new RepositoryResponseList<ImportTrackingEntryListModel> { status = true, data = importList, totalCount = totalCount };
        }

        public RepositoryResponse DeleteImportTrackingEntry(int docId, string docType)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_ImportTrackingEntry", con, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Action", "DELETE");
                            cmd.Parameters.AddWithValue("@V_NO", docId);
                            cmd.Parameters.AddWithValue("@V_Type", docType);
                            cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", getGlobalCode.PubBranchCode);
                            try
                            {
                                cmd.ExecuteNonQuery();
                                tran.Commit();
                                //_logService.InsertLog("EXIM1", "Import Tracking Entry", "Transaction", "Delete", docType, docId.ToString(), null);
                                //_logService.InsertLog("EXIM2", "Import Tracking Entry", "Transaction", "Delete", docType, docId.ToString(), null);
                                //_logService.InsertLog("EXIM3", "Import Tracking Entry", "Transaction", "Delete", docType, docId.ToString(), null);
                                //_logService.InsertLog("IMG_TABLE", "Import Tracking Entry", "Transaction", "Delete", docType, docId.ToString(), null);
                                return new RepositoryResponse { status = true, message = "Import Tracking Entry deleted successfully." };
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                return new RepositoryResponse { status = false, message = "Error deleting Import Tracking Entry." + ex.Message };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "Error deleting Import Tracking Entry." + ex.Message };
            }
        }
        
    }
}
