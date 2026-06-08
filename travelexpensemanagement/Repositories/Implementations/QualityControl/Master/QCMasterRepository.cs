using Microsoft.Data.SqlClient; 
using System.Data;
using System.Diagnostics;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.QualityControl.Master;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Transaction
{
    public class QCMasterRepository : IQCMasterRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly LogService.LogService _logService;
        public QCMasterRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _logService = logService;
        }

        public async Task<RepositoryResponseData<bool>> GetExistOrNotAsync(string inputData)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "Exist");
                        cmd.Parameters.AddWithValue("@Name", inputData);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        await con.OpenAsync();
                        var result = await cmd.ExecuteScalarAsync();
                        bool isExist = Convert.ToInt32(result) == 1;

                        return new RepositoryResponseData<bool> { status = true, data = isExist };
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, message = ex.Message, data = false };
            }
        }

        private int GetLastCode()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            int maxCode = 0;

            using (var conn = _dbConnection.GetErpConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand("Insert_QC_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "GetMaxCode");
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                    var result = cmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out int val))
                    {
                        maxCode = val;
                    }
                }
            }
            return maxCode;
        }

        public async Task<RepositoryResponse> InsertDataQcMasterAsync(QCMaster model)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        int code = GetLastCode();
                        if (code <= 0)
                        {
                            return new RepositoryResponse { status = false, message = "Invalid Code generated!" };
                        }

                        // Insert into QC_MAST (Header)
                        using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST", con, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@CODE", code);
                            cmd.Parameters.AddWithValue("@NAME", model.Name ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHORTNAME", model.ShortName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@QCGROUP_CODE", model.QCGroup);
                            cmd.Parameters.AddWithValue("@ACTIVE", model.active);
                            cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                            cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                            cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                            cmd.Parameters.AddWithValue("@AED", "A");
                            cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PPM", model.MaxPPM);
                            cmd.Parameters.AddWithValue("@Action", "Insert");

                            await cmd.ExecuteNonQueryAsync();
                        }


                        foreach (var d in model.Details)
                        {
                            Debug.WriteLine($"Parameter={d.Parameter}");
                        }

                        // Insert into QC_MAST1 (Details)
                        int srno = 1;
                        foreach (var detail in model.Details)
                        {
                            using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST1", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", code);
                                cmd.Parameters.AddWithValue("@QCP_CODE", detail.Parameter);
                                cmd.Parameters.AddWithValue("@QCP_UNIT", detail.Unit);
                                cmd.Parameters.AddWithValue("@QCP_STD", detail.StdResult);
                                cmd.Parameters.AddWithValue("@DEDUCT_QTY", detail.DeductQty ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEDUCT_TYPE", detail.DeductType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARKS", detail.Remarks ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SRNO", srno);
                                cmd.Parameters.AddWithValue("@MOBILE_APP", "NO");
                                cmd.Parameters.AddWithValue("@PPM_YN", detail.Ppm == "YES" ? "YES" : "NO");
                                cmd.Parameters.AddWithValue("@BASE_PRICE", detail.BasePrice);
                                cmd.Parameters.AddWithValue("@Action", "Insert");

                                await cmd.ExecuteNonQueryAsync();
                                srno++;
                            }
                        }

                        transaction.Commit();
                        //===========log insert
                        _logService.InsertLog("QC_MAST1", "QC Master", "Master", "INSERT", "", code.ToString(), null);
                        _logService.InsertLog("QC_MAST", "QC Master", "Master", "INSERT", "", code.ToString(), null);

                        return new RepositoryResponse { status = true, message = "Insert successful." };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return new RepositoryResponse { status = false, message = $"Transaction failed: {ex.Message}" };
                    }
                }
            }
        }

        public async Task<RepositoryResponse> UpdateDataQcMasterAsync(QCMaster model)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // Update QC_MAST (Header)
                        using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST", con, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@CODE", model.code);
                            cmd.Parameters.AddWithValue("@NAME", model.Name ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHORTNAME", model.ShortName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@QCGROUP_CODE", model.QCGroup);
                            cmd.Parameters.AddWithValue("@ACTIVE", model.active);
                            cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                            cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                            cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                            cmd.Parameters.AddWithValue("@AED", "A");
                            cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PPM", model.MaxPPM);
                            cmd.Parameters.AddWithValue("@Action", "Update");

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Delete existing details
                        using (SqlCommand deleteCmd = new SqlCommand("Insert_QC_MAST1", con, transaction))
                        {
                            deleteCmd.CommandType = CommandType.StoredProcedure;
                            deleteCmd.Parameters.AddWithValue("@Action", "Delete");
                            deleteCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            deleteCmd.Parameters.AddWithValue("@CODE", model.code);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }

                        // Re-Insert child items into QC_MAST1
                        int srno = 1;
                        foreach (var detail in model.Details)
                        {
                            using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST1", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", model.code);
                                cmd.Parameters.AddWithValue("@QCP_CODE", detail.Parameter);
                                cmd.Parameters.AddWithValue("@QCP_UNIT", detail.Unit);
                                cmd.Parameters.AddWithValue("@QCP_STD", detail.StdResult);
                                cmd.Parameters.AddWithValue("@DEDUCT_QTY", detail.DeductQty ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEDUCT_TYPE", detail.DeductType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARKS", detail.Remarks ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SRNO", srno);
                                cmd.Parameters.AddWithValue("@MOBILE_APP", "NO");
                                cmd.Parameters.AddWithValue("@PPM_YN", detail.Ppm == "YES" ? "YES" : "NO");
                                cmd.Parameters.AddWithValue("@BASE_PRICE", detail.BasePrice);
                                cmd.Parameters.AddWithValue("@Action", "Insert");

                                await cmd.ExecuteNonQueryAsync();
                                srno++;
                            }
                        }

                        transaction.Commit();
                        
                        //===========log insert
                        _logService.InsertLog("QC_MAST1", "QC Master", "Master", "UPDATE", "", model.code.ToString(), null);
                        _logService.InsertLog("QC_MAST", "QC Master", "Master", "UPDATE", "", model.code.ToString(), null);

                        return new RepositoryResponse { status = true, message = "Update successful." };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return new RepositoryResponse {status = false, message = $"Transaction failed: {ex.Message}" };
                    }
                }
            }
        }

        public async Task<RepositoryResponse> SaveDeductRatesAsync(List<DeductRateModel> rates)
        {
            if (rates == null || !rates.Any())
            {
                return new RepositoryResponse {status = false, message = "No rate data provided." };
            }
            var gv = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        int code = Convert.ToInt32(rates.First().Code);
                        int qcpCode = Convert.ToInt32(rates.First().nextQcpCode);

                        using (SqlCommand deleteCmd = new SqlCommand("Insert_QC_MAST2", con, transaction))
                        {
                            deleteCmd.CommandType = CommandType.StoredProcedure;
                            deleteCmd.Parameters.AddWithValue("@Action", "Delete_For_Insert");
                            deleteCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            deleteCmd.Parameters.AddWithValue("@CODE", code);
                            deleteCmd.Parameters.AddWithValue("@QCP_CODE", qcpCode);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }

                        int srno = 1;
                        foreach (var rate in rates)
                        {
                            using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST2", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", rate.Code);
                                cmd.Parameters.AddWithValue("@QCP_CODE", rate.nextQcpCode);
                                cmd.Parameters.AddWithValue("@FROM_RESULT", rate.From);
                                cmd.Parameters.AddWithValue("@TO_RESULT", rate.To);
                                cmd.Parameters.AddWithValue("@DEDUCT_TYPE", rate.ded_type ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEDUCT_RATE", rate.Rate);
                                cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SRNO", srno);
                                cmd.Parameters.AddWithValue("@DED_TYPE", rate.Type ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Action", "Insert");

                                await cmd.ExecuteNonQueryAsync();
                                srno++;
                            }
                        }

                        transaction.Commit();
                        //===========log insert
                        _logService.InsertLog("QC_MAST2", "QC Master", "Master", "INSERT", "", rates.First().Code.ToString(), null);


                        return new RepositoryResponse {status = true, message = "Rates saved successfully." };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return new RepositoryResponse {status = false, message = $"Error: {ex.Message}" };
                    }
                }
            }
        }

        public async Task<RepositoryResponseData<List<DeductRateModelList>>> CheckDeductRatesAsync(CheckDeductRateRequest request)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                var deductRates = new List<DeductRateModelList>();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST2", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GET");
                        cmd.Parameters.AddWithValue("@CODE", request.Code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@QCP_CODE", request.ParameterId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                deductRates.Add(new DeductRateModelList
                                {
                                    FromResult = reader["FROM_RESULT"] != DBNull.Value ? Convert.ToDecimal(reader["FROM_RESULT"]) : (decimal?)null,
                                    ToResult = reader["TO_RESULT"] != DBNull.Value ? Convert.ToDecimal(reader["TO_RESULT"]) : (decimal?)null,
                                    DeductType = reader["DEDUCT_TYPE"]?.ToString(),
                                    DeductRate = reader["DEDUCT_RATE"] != DBNull.Value ? Convert.ToDecimal(reader["DEDUCT_RATE"]) : (decimal?)null,
                                });
                            }
                        }
                        return new RepositoryResponseData<List<DeductRateModelList>> {status = true, data = deductRates };
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<DeductRateModelList>> {status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponseData<QCMaster>> GetQCMasterListByCodeAsync(int code)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                var qcMaster = new QCMaster { Details = new List<DetailModel>() };

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    // Step 1: Read Parent Header Master
                    using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetById");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            if (await rdr.ReadAsync())
                            {
                                qcMaster.Name = rdr["NAME"]?.ToString();
                                qcMaster.ShortName = rdr["SHORTNAME"]?.ToString();
                                qcMaster.QCGroup = rdr["QCGROUP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["QCGROUP_CODE"]) : 0;
                                qcMaster.MaxPPM = rdr["PPM"] != DBNull.Value ? Convert.ToDecimal(rdr["PPM"]) : 0;
                                qcMaster.active = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(qcMaster.Name))
                    {
                        return new RepositoryResponseData<QCMaster> {status = false, message = "No record found." };
                    }

                    // Step 2: Read Child Details
                    using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetById");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                qcMaster.Details.Add(new DetailModel
                                {
                                    Code = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    ParameterValue = rdr["QCP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["QCP_CODE"]) : 0,
                                    Unit = rdr["QCP_UNIT"] != DBNull.Value ? Convert.ToInt32(rdr["QCP_UNIT"]) : 0,
                                    StdResult = rdr["QCP_STD"] != DBNull.Value ? Convert.ToDecimal(rdr["QCP_STD"]) : 0,
                                    DeductQty = rdr["DEDUCT_QTY"]?.ToString(),
                                    DeductType = rdr["DEDUCT_TYPE"]?.ToString(),
                                    Remarks = rdr["REMARKS"]?.ToString(),
                                    Ppm = rdr["PPM_YN"]?.ToString(),
                                    BasePrice = rdr["BASE_PRICE"] != DBNull.Value ? Convert.ToDecimal(rdr["BASE_PRICE"]) : 0
                                });
                            }
                        }
                    }
                }

                return new RepositoryResponseData<QCMaster> {status = true, data = qcMaster };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<QCMaster> {status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponseData<bool>> CheckDeductRateExistAsync(int code, int qcpCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST2", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "IsExists");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", code);
                        cmd.Parameters.AddWithValue("@QCP_CODE", qcpCode);

                        await con.OpenAsync();
                        var result = await cmd.ExecuteScalarAsync();
                        bool exists = result != null;

                        return new RepositoryResponseData<bool> { status = true, data = exists };
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, message = ex.Message, data = false };
            }
        }
    }
}