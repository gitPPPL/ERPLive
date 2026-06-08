using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.QualityControl.Master;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;
using static travelexpensemanagement.Controllers.QualityControl.Master.ParameterMasterListController;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Master
{
    public class ParameterMasterListRepository : IParameterMasterListRepository
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly DbHelper _dbHelper;
        private readonly LogService.LogService _logService;
        public ParameterMasterListRepository(DataBaseConnection dbcontext, GlobalVariableService globalValue, DbHelper dbHelper, LogService.LogService logService)
        {
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _dbHelper = dbHelper;
            _logService = logService;
        }
        public async Task<RepositoryResponse> DelQParamMastAsync(int docId)
        {
            try
            {
                int x;
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_QualityParameterMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "D");
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(docId));
                        var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.ReturnValue
                        };
                        cmd.Parameters.Add(returnParam);
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                        x = (int)cmd.Parameters["@ReturnVal"].Value;
                    }
                }
                if (x > 0)
                {
                    //===========log insert
                    _logService.InsertLog("QCP_MAST", "QC Parameter Master", "Master", "DELETE", "", docId.ToString(), null);
                    return new RepositoryResponse { status = true, message = "Data delete successfully" };
                }
                return new RepositoryResponse { status = false, message = "data delete failed" };

            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "data delete failed" };
            }
        }

        public async Task<RepositoryResponseList<ParameterMasterListController.QCprameterDto>> GetQualityParamListAsync(string searchTerm, int pageNumber, int pageSize)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var pagedList = new List<QCprameterDto>();
                int totalCount = 0;

                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QualityParameterMast_AED", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "GET");
                        cmd.Parameters.AddWithValue("@companyCd", UsersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@SearchTerm", (object)searchTerm ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        await con.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            // --- RESULT SET 1: QualityParamList ---
                            while (await reader.ReadAsync())
                            {
                                pagedList.Add(new QCprameterDto
                                {
                                    CODE = reader["Code"] != DBNull.Value ? Convert.ToInt32(reader["Code"]) : null,
                                    NAME = reader["Name"]?.ToString(),
                                    SHORTNAME = reader["ShortName"]?.ToString(),
                                    QUNIT = reader["Unit"]?.ToString(),
                                    qty = reader["Qty"] != DBNull.Value ? Convert.ToInt32(reader["Qty"]) : null,
                                    ACTIVE = reader["Active"] != DBNull.Value ? Convert.ToInt32(reader["Active"]) : null,
                                });
                            }

                            // --- RESULT SET 2: TotalCount ---
                            if (await reader.NextResultAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    totalCount = (int)reader["TotalCount"];
                                }
                            }
                        }
                    }
                }

                return new RepositoryResponseList<QCprameterDto> { status = true, data = pagedList, totalCount = totalCount };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<QCprameterDto> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<bool> IsQcParamDeletableAsync(int docId)
        {
            var gv = _globalValue.GetGlobalVariables();
            bool isExists = false;
            string msg = "";
            try
            {
                //===========Check Qc Group existence in QC Master===========
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QualityParameterMast_AED", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "Del_CheckInQcMast1");
                        cmd.Parameters.AddWithValue("@Code", docId);
                        cmd.Parameters.AddWithValue("@companyCd", gv.PubCompCode);

                        con.Open();
                        object result = cmd.ExecuteScalar();

                        string qcParamName = result?.ToString();
                        isExists = string.IsNullOrEmpty(qcParamName) ? false : true;

                        msg = $"QC Parameter <b>{qcParamName}</b> exists in QC Master and cannot be deleted.";
                    }
                    return new RepositoryResponseData<bool> { status = true, message = msg, data = isExists };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, message = ex.Message };
            }
        }
    }
}
