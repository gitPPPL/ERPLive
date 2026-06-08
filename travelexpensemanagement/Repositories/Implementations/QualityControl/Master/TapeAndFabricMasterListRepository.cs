using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.QualityControl.Master;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;
using static travelexpensemanagement.Controllers.QualityControl.Master.TapeAndFabricMasterListController;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Master
{
    public class TapeAndFabricMasterListRepository : ITapeAndFabricMasterListRepository
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly DbHelper _dbHelper;
        private readonly LogService.LogService _logService;
        public TapeAndFabricMasterListRepository(DataBaseConnection dbcontext, GlobalVariableService globalValue, DbHelper dbHelper, LogService.LogService logService)
        {
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _dbHelper = dbHelper;
            _logService = logService;
        }
        public async Task<RepositoryResponse> DelTape_FabricMastAsync(int docId)
        {
            try
            {
                int x;
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_TapeNFabricMast_AED]", con))
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
                    _logService.InsertLog("TAPE_NFABRIC_MAST", "Tape And Fabric Master", "Master", "Delete", "", docId.ToString(), null);
                    return new RepositoryResponse { status = true, message = "Data delete successfully" };
                }
                return new RepositoryResponse { status = false, message = "data delete failed" };

            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "data delete failed" };
            }
        }

        public async Task<RepositoryResponseList<TapeAndFabricMasterListController.QCStandardMasterDto>> GetTape_FabricListAsync(string searchTerm, int pageNumber, int pageSize)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();

                var pagedList = new List<QCStandardMasterDto>();
                int totalCount = 0;

                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_TapeNFabricMast_AED", con))
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
                                pagedList.Add(new QCStandardMasterDto
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : null,
                                    NAME = reader["NAME"]?.ToString(),
                                    MESH_NAME = reader["MESH_NAME"]?.ToString(),

                                    STD_GRAM = reader["STD_GRAM"] != DBNull.Value ? Convert.ToDecimal(reader["STD_GRAM"]) : null,
                                    MIN_GRAM = reader["MIN_GRAM"] != DBNull.Value ? Convert.ToDecimal(reader["MIN_GRAM"]) : null,
                                    MAX_GRAM = reader["MAX_GRAM"] != DBNull.Value ? Convert.ToDecimal(reader["MAX_GRAM"]) : null,

                                    GSM = reader["GSM"] != DBNull.Value ? Convert.ToDecimal(reader["GSM"]) : null,
                                    DENIER = reader["DENIER"] != DBNull.Value ? Convert.ToDecimal(reader["DENIER"]) : null,

                                    UNIT_NAME = reader["UNIT_NAME"]?.ToString(),
                                    COLOR_NAME = reader["COLOR_NAME"]?.ToString(),

                                    WIDTH = reader["WIDTH"] != DBNull.Value ? Convert.ToDecimal(reader["WIDTH"]) : null,

                                    GPD = reader["GPD"] != DBNull.Value ? Convert.ToDecimal(reader["GPD"]) : null,
                                    MIN_GPD = reader["MIN_GPD"] != DBNull.Value ? Convert.ToDecimal(reader["MIN_GPD"]) : null,
                                    MAX_GPD = reader["MAX_GPD"] != DBNull.Value ? Convert.ToDecimal(reader["MAX_GPD"]) : null,

                                    STD_STRENGTH = reader["STD_STRENGTH"] != DBNull.Value ? Convert.ToDecimal(reader["STD_STRENGTH"]) : null,
                                    STRENGTH_MAX = reader["STRENGTH_MAX"] != DBNull.Value ? Convert.ToDecimal(reader["STRENGTH_MAX"]) : null,
                                    STRENGTH_MIN = reader["STRENGTH_MIN"] != DBNull.Value ? Convert.ToDecimal(reader["STRENGTH_MIN"]) : null,

                                    STD_ELONG = reader["STD_ELONG"] != DBNull.Value ? Convert.ToDecimal(reader["STD_ELONG"]) : null,
                                    ELONG_MAX = reader["ELONG_MAX"] != DBNull.Value ? Convert.ToDecimal(reader["ELONG_MAX"]) : null,
                                    ELONG_MIN = reader["ELONG_MIN"] != DBNull.Value ? Convert.ToDecimal(reader["ELONG_MIN"]) : null,

                                    UNLAM_FAB = reader["UNLAM_FAB"] != DBNull.Value ? Convert.ToDecimal(reader["UNLAM_FAB"]) : null,
                                    LAM_FAB = reader["LAM_FAB"] != DBNull.Value ? Convert.ToDecimal(reader["LAM_FAB"]) : null,

                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : null
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

                return new RepositoryResponseList<QCStandardMasterDto> { status = true, data = pagedList, totalCount = totalCount };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<QCStandardMasterDto>{ status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<bool> IsTapeFabricDeletableAsync(int docId)
        {
            var gv = _globalValue.GetGlobalVariables();
            bool isExists = false;
            string msg = "";
            try
            {
                //===========Check Qc Group existence in QC Master===========
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_TapeNFabricMast_AED", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "Del_CheckInItem_Mast");
                        cmd.Parameters.AddWithValue("@CODE", docId);
                        cmd.Parameters.AddWithValue("@CompanyCd", gv.PubCompCode);

                        con.Open();
                        object result = cmd.ExecuteScalar();

                        string qcTape_FabName = result?.ToString();
                        isExists = string.IsNullOrEmpty(qcTape_FabName) ? false : true;

                        msg = $"Tape And Fabric <b>{qcTape_FabName}</b> exists in Item Master and cannot be deleted.";
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
