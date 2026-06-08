using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.QualityControl.Master;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;
using static travelexpensemanagement.Controllers.QualityControl.Master.TapeAndFabricMasterController;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Master
{
    public class TapeAndFabricMasterRepository : ITapeAndFabricMasterRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly LogService.LogService _logService;
        int x;
        public TapeAndFabricMasterRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DbHelper dbHelper, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _logService = logService;
        }

        public async Task<RepositoryResponseData<bool>> GetExistOrNotAsync(string inputData)
        {
            try
            {
                bool isExist = false;

                using (var con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_TapeNFabricMast_AED"))
                    {
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@AED", "Exist");
                        cmd.Parameters.AddWithValue("@Name", inputData);
                        cmd.Parameters.AddWithValue("@CompanyCd", _globalVariableService.GetGlobalVariables().PubCompCode);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        isExist = Convert.ToInt32(result) == 1;
                    }
                }

                return new RepositoryResponseData<bool> { status = true, data = isExist };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponse> SaveTapeAndFabricAsync(TapeAndFabricMasterController.TapeNFabricModel model)
        {
            try
            {
                int code = 0;
                if (model == null)
                {
                    return new RepositoryResponse { status = false, message = "Invalid Data" };
                }

                using (var con = _dbConnection.GetErpConnection())
                {
                    var usersessionDt = _globalVariableService.GetGlobalVariables();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_TapeNFabricMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@MESH_CODE", _dbHelper.Xnull(model.MeshCode));
                        cmd.Parameters.AddWithValue("@STD_GRAM", _dbHelper.Vnull(model.StdGram));
                        cmd.Parameters.AddWithValue("@MIN_GRAM", _dbHelper.Vnull(model.MinGram));
                        cmd.Parameters.AddWithValue("@MAX_GRAM", _dbHelper.Vnull(model.MaxGram));
                        cmd.Parameters.AddWithValue("@GSM", _dbHelper.Vnull(model.Gsm));
                        cmd.Parameters.AddWithValue("@DENIER", _dbHelper.Vnull(model.Denier));
                        cmd.Parameters.AddWithValue("@UNIT_NAME", _dbHelper.Xnull(model.UnitName));
                        cmd.Parameters.AddWithValue("@COLOR_CODE", _dbHelper.Xnull(model.ColorCode));
                        cmd.Parameters.AddWithValue("@WIDTH", _dbHelper.Vnull(model.Width));
                        cmd.Parameters.AddWithValue("@GPD", _dbHelper.Vnull(model.Gpd));
                        cmd.Parameters.AddWithValue("@MIN_GPD", _dbHelper.Vnull(model.MinGpd));
                        cmd.Parameters.AddWithValue("@MAX_GPD", _dbHelper.Vnull(model.MaxGpd));
                        cmd.Parameters.AddWithValue("@STD_STRENGTH", _dbHelper.Vnull(model.StdStrength));
                        cmd.Parameters.AddWithValue("@STRENGTH_MAX", _dbHelper.Vnull(model.StrengthMax));
                        cmd.Parameters.AddWithValue("@STRENGTH_MIN", _dbHelper.Vnull(model.StrengthMin));
                        cmd.Parameters.AddWithValue("@STD_ELONG", _dbHelper.Vnull(model.StdElong));
                        cmd.Parameters.AddWithValue("@ELONG_MAX", _dbHelper.Vnull(model.ElongMax));
                        cmd.Parameters.AddWithValue("@ELONG_MIN", _dbHelper.Vnull(model.ElongMin));
                        cmd.Parameters.AddWithValue("@UNLAM_FAB", _dbHelper.Vnull(model.UnlamFab));
                        cmd.Parameters.AddWithValue("@LAM_FAB", _dbHelper.Vnull(model.LamFab));
                        cmd.Parameters.AddWithValue("@ACTIVE", _dbHelper.Xnull(model.Active));
                        cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);
                        cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);

                        var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.ReturnValue
                        };
                        cmd.Parameters.Add(returnParam);
                        await con.OpenAsync();
                        //await cmd.ExecuteNonQueryAsync();
                        code = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        x = (int)cmd.Parameters["@ReturnVal"].Value;

                    }
                }

                if (x > 0)
                {
                    //===========log insert
                    _logService.InsertLog("TAPE_NFABRIC_MAST", "Tape And Fabric Master", "Master", "INSERT", "", code.ToString(), null);
                    return new RepositoryResponse { status = true, message = "Data Save Successfully" };
                }
                return new RepositoryResponse { status = false, message = "Data Save Failed" };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "Data Save Failed" };
            }
        }

        public async Task<RepositoryResponseData<TapeAndFabricMasterController.TapeNFabricModel>> GetTapeAndFabricDetailsByIdAsync(string id)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new TapeNFabricModel();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_TapeNFabricMast_AED", con))
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
                                data.Code = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : null;
                                data.Name = reader["NAME"]?.ToString();

                                data.MeshCode = reader["MESH_CODE"] != DBNull.Value
                                    ? Convert.ToInt32(reader["MESH_CODE"])
                                    : null;

                                data.StdGram = reader["STD_GRAM"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["STD_GRAM"])
                                    : null;

                                data.MinGram = reader["MIN_GRAM"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["MIN_GRAM"])
                                    : null;

                                data.MaxGram = reader["MAX_GRAM"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["MAX_GRAM"])
                                    : null;

                                data.Gsm = reader["GSM"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["GSM"])
                                    : null;

                                data.Denier = reader["DENIER"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["DENIER"])
                                    : null;

                                data.UnitName = reader["UNIT_NAME"]?.ToString();

                                data.ColorCode = reader["COLOR_CODE"] != DBNull.Value
                                    ? Convert.ToInt32(reader["COLOR_CODE"])
                                    : null;

                                data.Width = reader["WIDTH"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["WIDTH"])
                                    : null;

                                data.Gpd = reader["GPD"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["GPD"])
                                    : null;

                                data.MinGpd = reader["MIN_GPD"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["MIN_GPD"])
                                    : null;

                                data.MaxGpd = reader["MAX_GPD"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["MAX_GPD"])
                                    : null;

                                data.StdStrength = reader["STD_STRENGTH"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["STD_STRENGTH"])
                                    : null;

                                data.StrengthMax = reader["STRENGTH_MAX"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["STRENGTH_MAX"])
                                    : null;

                                data.StrengthMin = reader["STRENGTH_MIN"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["STRENGTH_MIN"])
                                    : null;

                                data.StdElong = reader["STD_ELONG"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["STD_ELONG"])
                                    : null;

                                data.ElongMax = reader["ELONG_MAX"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["ELONG_MAX"])
                                    : null;

                                data.ElongMin = reader["ELONG_MIN"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["ELONG_MIN"])
                                    : null;

                                data.UnlamFab = reader["UNLAM_FAB"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["UNLAM_FAB"])
                                    : null;

                                data.LamFab = reader["LAM_FAB"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["LAM_FAB"])
                                    : null;

                                data.Active = reader["ACTIVE"] != DBNull.Value
                                    ? Convert.ToInt32(reader["ACTIVE"])
                                    : null;
                            }
                        }
                    }
                    return new RepositoryResponseData<TapeNFabricModel> { status = true, data = data };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<TapeNFabricModel> { status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponse> UpdateTapeAndFabricAsync(TapeNFabricModel model)
        {
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    var usersessionDt = _globalVariableService.GetGlobalVariables();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_TapeNFabricMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.Code));
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@MESH_CODE", _dbHelper.Xnull(model.MeshCode));
                        cmd.Parameters.AddWithValue("@STD_GRAM", _dbHelper.Vnull(model.StdGram));
                        cmd.Parameters.AddWithValue("@MIN_GRAM", _dbHelper.Vnull(model.MinGram));
                        cmd.Parameters.AddWithValue("@MAX_GRAM", _dbHelper.Vnull(model.MaxGram));
                        cmd.Parameters.AddWithValue("@GSM", _dbHelper.Vnull(model.Gsm));
                        cmd.Parameters.AddWithValue("@DENIER", _dbHelper.Vnull(model.Denier));
                        cmd.Parameters.AddWithValue("@UNIT_NAME", _dbHelper.Xnull(model.UnitName));
                        cmd.Parameters.AddWithValue("@COLOR_CODE", _dbHelper.Xnull(model.ColorCode));
                        cmd.Parameters.AddWithValue("@WIDTH", _dbHelper.Vnull(model.Width));
                        cmd.Parameters.AddWithValue("@GPD", _dbHelper.Vnull(model.Gpd));
                        cmd.Parameters.AddWithValue("@MIN_GPD", _dbHelper.Vnull(model.MinGpd));
                        cmd.Parameters.AddWithValue("@MAX_GPD", _dbHelper.Vnull(model.MaxGpd));
                        cmd.Parameters.AddWithValue("@STD_STRENGTH", _dbHelper.Vnull(model.StdStrength));
                        cmd.Parameters.AddWithValue("@STRENGTH_MAX", _dbHelper.Vnull(model.StrengthMax));
                        cmd.Parameters.AddWithValue("@STRENGTH_MIN", _dbHelper.Vnull(model.StrengthMin));
                        cmd.Parameters.AddWithValue("@STD_ELONG", _dbHelper.Vnull(model.StdElong));
                        cmd.Parameters.AddWithValue("@ELONG_MAX", _dbHelper.Vnull(model.ElongMax));
                        cmd.Parameters.AddWithValue("@ELONG_MIN", _dbHelper.Vnull(model.ElongMin));
                        cmd.Parameters.AddWithValue("@UNLAM_FAB", _dbHelper.Vnull(model.UnlamFab));
                        cmd.Parameters.AddWithValue("@LAM_FAB", _dbHelper.Vnull(model.LamFab));
                        cmd.Parameters.AddWithValue("@ACTIVE", _dbHelper.Xnull(model.Active));
                        cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);
                        cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);

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
                    _logService.InsertLog("TAPE_NFABRIC_MAST", "Tape And Fabric Master", "Master", "UPDATE", "", model.Code.ToString(), null);
                    return new RepositoryResponse { status = true, message = "Data update Successfully" };
                }
                return new RepositoryResponse { status = true, message = "Data update failed." };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }
    }
}
