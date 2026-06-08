using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.QualityControl.Master;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Master
{
    public class QCMasterListRepository : IQCMasterListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly LogService.LogService _logService;

        public QCMasterListRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _logService = logService;
        }

        public async Task<RepositoryResponseList<QCMasterList>> GetQCMasterListAsync(string searchTerm, int pageNumber, int pageSize)
        {
            var response = new RepositoryResponseList<QCMasterList>();
            var qcMasterList = new List<QCMasterList>();
            int totalCount = 0;

            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("Insert_QC_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "Select");
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@NAME", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);

                    conn.Open();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            qcMasterList.Add(new QCMasterList
                            {
                                Code = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                Name = reader["NAME"]?.ToString(),
                                ShortName = reader["SHORTNAME"]?.ToString(),
                                QCGroup = reader["QCGROUP_CODE"]?.ToString(),
                                MaxPPM = reader["PPM"] != DBNull.Value ? Convert.ToString(reader["PPM"]) : null,
                                ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                            });
                        }

                        if (reader.NextResult() && await reader.ReadAsync())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }

                response.status = true;
                response.data = qcMasterList;
                response.totalCount = totalCount;
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
            }

            return response;
        }

        public async Task<RepositoryResponse> DeleteQcMasterAsync(int docId)
        {
            var response = new RepositoryResponse();

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            ExecuteDelete(conn, tran, "Insert_QC_MAST2", docId);
                            ExecuteDelete(conn, tran, "Insert_QC_MAST1", docId);
                            ExecuteDelete(conn, tran, "Insert_QC_MAST", docId);

                            tran.Commit();

                            //===========log insert
                            _logService.InsertLog("QC_MAST2", "QC Master", "Master", "DELETE", "", docId.ToString(), null);
                            _logService.InsertLog("QC_MAST1", "QC Master", "Master", "DELETE", "", docId.ToString(), null);
                            _logService.InsertLog("QC_MAST", "QC Master", "Master", "DELETE", "", docId.ToString(), null);

                            response.status = true;
                            response.message = "QC deleted successfully.";
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            response.status = false;
                            response.message = ex.Message;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
            }

            return response;
        }

        public async Task<RepositoryResponseData<bool>> IsQcDeletableAsync(int docId)
        {
            var response = new RepositoryResponseData<bool>();
            var globalVar = _globalVariableService.GetGlobalVariables();

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("Insert_QC_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "Del_CheckInQc2");
                    cmd.Parameters.AddWithValue("@CODE", docId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                    conn.Open();
                    var result = await cmd.ExecuteScalarAsync();
                    response.data = !string.IsNullOrEmpty(result?.ToString());
                    response.message = response.data == true ? $"QC Name <b>{result}</b> exists in QC2 and cannot be deleted." : "QC is deletable.";
                    response.status = true;
                }
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
            }

            return response;
        }

        private void ExecuteDelete(SqlConnection conn, SqlTransaction tran, string sp, int code)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            using (var cmd = new SqlCommand(sp, conn, tran))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "Delete");
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
