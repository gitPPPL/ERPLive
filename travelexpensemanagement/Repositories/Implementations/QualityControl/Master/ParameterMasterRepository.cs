using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.QualityControl.Master;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Master
{
    public class ParameterMasterRepository : IParameterMasterRepository
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly DbHelper _dbHelper;
        private readonly LogService.LogService _logService;

        int x;
        public ParameterMasterRepository(DataBaseConnection dbcontext, GlobalVariableService globalValue, DbHelper dbHelper, LogService.LogService logService)
        {
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _dbHelper = dbHelper;
            _logService = logService;
        }
        public RepositoryResponseData<bool> GetExistOrNotAsync(string inputData)
        {
            try
            {
                bool isExist = false;

                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QualityParameterMast_AED"))
                    {
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@AED", "Exist");
                        cmd.Parameters.AddWithValue("@Name", inputData);
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        isExist = Convert.ToInt32(result) == 1;
                    }
                }

                return new RepositoryResponseData<bool> { status = true, data = isExist };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, message = "Data check failed: " + ex.Message };
            }
        }

        public async Task<RepositoryResponseData<ParameterMasterController.ParameterModel>> GetQParameterDetailsByIdAsync(string id)
        {
            var gv = _globalValue.GetGlobalVariables();
            var data = new ParameterMasterController.ParameterModel();
            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QualityParameterMast_AED", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "GetById");
                        cmd.Parameters.AddWithValue("@companyCd", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", id);

                        await con.OpenAsync();
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                data.code = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : null;
                                data.Name = reader["NAME"]?.ToString();
                                data.ShortName = reader["SHORTNAME"]?.ToString();
                                data.QUnitCd = reader["QUNIT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["QUNIT_CODE"]) : null;
                                data.Qty = reader["QTY"] != DBNull.Value ? Convert.ToInt32(reader["QTY"]) : null;
                                data.active = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : null;
                            }
                        }
                        return new RepositoryResponseData<ParameterMasterController.ParameterModel> { status = true, data = data };
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<ParameterMasterController.ParameterModel> { status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponse> SaveQParamMastAsync(ParameterMasterController.ParameterModel model)
        {
            try
            {
                int code = 0;
                if (model == null)
                {
                    return new RepositoryResponse { status = false, message = "Data Save Failed" };
                }

                using (var con = _dbcontext.GetErpConnection())
                {
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_QualityParameterMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.code));
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@ShortName", _dbHelper.Xnull(model.ShortName));
                        cmd.Parameters.AddWithValue("@QUnitCd", _dbHelper.Xnull(model.QUnitCd));
                        cmd.Parameters.AddWithValue("@Qty", _dbHelper.Xnull(model.Qty));
                        cmd.Parameters.AddWithValue("@active", _dbHelper.Xnull(model.active));
                        cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);
                        cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);
                        cmd.Parameters.AddWithValue("@wsid", usersessionDt.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@lid", Environment.MachineName);

                        var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.ReturnValue
                        };
                        cmd.Parameters.Add(returnParam);
                        await con.OpenAsync();
                        //await cmd.ExecuteNonQueryAsync();
                        code = Convert.ToInt32(cmd.ExecuteScalar());
                        x = (int)cmd.Parameters["@ReturnVal"].Value;

                    }
                }

                if (x > 0)
                {
                    //===========log insert
                    _logService.InsertLog("QCP_MAST", "QC Parameter Master", "Master", "INSERT", "", code.ToString(), null);
                    return new RepositoryResponse { status = true, message = "Data Save Successfully" };
                }
                return new RepositoryResponse { status = false, message = "Data Save Failed" };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "Data Save Failed" };
            }
        }

        public async Task<RepositoryResponse> UpdateQParameterMastAsync(ParameterMasterController.ParameterModel model)
        {
            try
            {
                if (model == null)
                {
                    return new RepositoryResponse { status = false, message = "Data update Failed" };
                }

                using (var con = _dbcontext.GetErpConnection())
                {
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_QualityParameterMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.code));
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@ShortName", _dbHelper.Xnull(model.ShortName));
                        cmd.Parameters.AddWithValue("@QUnitCd", _dbHelper.Xnull(model.QUnitCd));
                        cmd.Parameters.AddWithValue("@Qty", _dbHelper.Xnull(model.Qty));
                        cmd.Parameters.AddWithValue("@active", _dbHelper.Xnull(model.active));
                        cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);
                        cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);
                        cmd.Parameters.AddWithValue("@wsid", usersessionDt.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@lid", Environment.MachineName);

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
                    _logService.InsertLog("QCP_MAST", "QC Parameter Master", "Master", "UPDATE", "", model.code.ToString(), null);
                    return new RepositoryResponse { status = true, message = "Data update Successfully" };
                }
                return new RepositoryResponse { status = false, message = "Data update failed" };

            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "Data update Failed" };
            }

        }
    }
}
