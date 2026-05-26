using Microsoft.Data.SqlClient;
using System.Data;
using System.Dynamic;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;
using static travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction.IQCTemperatureEntryRepository;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Transaction
{
    public class QCTemperatureEntryRepository : IQCTemperatureEntryRepository
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly DbHelper _dbHelper;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly LogService.LogService _logService;
        public QCTemperatureEntryRepository(DataBaseConnection dbcontext, GlobalVariableService globalValue, DbHelper dbHelper, GlobalValidationdate globalValidationdate, LogService.LogService logService)
        {
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _dbHelper = dbHelper;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
        }

        public async Task<RepositoryResponseData<QCTempEntryDto>> GetById(string id)
        {
            var response = new RepositoryResponseData<QCTempEntryDto>();
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var VNo = id.Substring(4);
                var VType = id.Substring(0, 4);

                var parameter = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@V_TYPE", VType},
                    {"@V_NO",  VNo },
                    {"@Action", "QcTempratureHeaderData"}
                };
                var parameter1 = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@V_TYPE", VType},
                    {"@V_NO",  VNo },
                    {"@Action", "QcTempratureDetailData"}
                };

                var headerlist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetQcTempratureEntry]", parameter);
                var detaillist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetQcTempratureEntry]", parameter1);
                response.status = true;
                response.data = new QCTempEntryDto()
                {
                    Header = headerlist.ToList(),
                    Detail = detaillist.ToList()
                };
                return response;
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = "Data load failed";
                return response;
            }
        }

        public async Task<RepositoryResponseData<bool>> getExist(DateTime V_DATE, DateTime V_TIME, string SHIFT, int plantCode, int VNo)
        {
            var response = new RepositoryResponseData<bool>();
            try
            {
                bool isExist = false;

                using (var con = _dbcontext.GetErpConnection())
                {
                    var loginDatail = _globalValue.GetGlobalVariables();
                    string sqlqry = "";
                    if (VNo > 0)
                        sqlqry = @$"and V_NO != {VNo}";

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = @$"
                         SELECT CASE 
                        WHEN EXISTS (
                        SELECT 1 
                         FROM TAPE_QUALITY1 
                        WHERE V_DATE=@VDate and FORMAT(V_TIME, 'hh:mm')=@V_time
                        and SHIFT=@shift and DEPT_CODE=@plantCode
                        and COMP_CODE=@CompCode and YEAR_CODE=@YearCode and BRANCH_CODE=@BRANCH_CODE and V_TYPE=@V_type {sqlqry}
                        ) 
                        THEN 1 ELSE 0 
                        END";

                        string vdate = (V_DATE).ToString("dd-MMM-yyyy");
                        string vtime = (V_TIME).ToString("hh:mm");

                        cmd.Parameters.AddWithValue("@VDate", vdate);
                        cmd.Parameters.AddWithValue("@V_time", vtime);
                        cmd.Parameters.AddWithValue("@shift", SHIFT);
                        cmd.Parameters.AddWithValue("@plantCode", plantCode);
                        cmd.Parameters.AddWithValue("@CompCode", loginDatail.PubCompCode);
                        cmd.Parameters.AddWithValue("@YearCode", loginDatail.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", loginDatail.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_type", "TAPE");

                        await con.OpenAsync();
                        var result = await cmd.ExecuteScalarAsync();
                        isExist = Convert.ToInt32(result) == 1;
                    }
                }
                response.status = true;
                response.data = isExist;
                return response;
                //return Json(new { status = true, exists = isExist });
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = "Data check failed: " + ex.Message;
                return response;
                //return Json(new { status = false, message = "Data check failed: " + ex.Message });
            }
        }

        public async Task<RepositoryResponseData<List<testParamDto>>> FillDataByLineNo(int deptCode)
        {
            var response = new RepositoryResponseData<List<testParamDto>>();
            var testParameters = new List<testParamDto>();

            try
            {
                var gv = _globalValue.GetGlobalVariables();

                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetQcTempratureEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetTestParameterDetails");
                        cmd.Parameters.AddWithValue("@DEPT_CODE", deptCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                        await con.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                testParameters.Add(new testParamDto
                                {
                                    ROOM_CODE = reader["ROOM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ROOM_CODE"]) : 0,
                                    RoomName = reader["ROOM_NAME"]?.ToString(),
                                    TYPE = reader["TYPE"]?.ToString()
                                });
                            }
                        }
                    }
                }

                response.status = true;
                response.data = testParameters;
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = "Failed to fetch test parameter details: " + ex.Message;
            }

            return response;
        }

        public async Task<RepositoryResponseData<dynamic>> ImportDataByReading(int timeInterval, string type, string shift, int deptCode, string vType)
        {
            var gv = _globalValue.GetGlobalVariables();
            var dataList = new List<dynamic>();
            var response = new RepositoryResponseData<dynamic>();
            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetQcTempratureEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ImportReadingData");
                        cmd.Parameters.AddWithValue("@TYPE", type ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIFT", shift ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DEPT_CODE", deptCode != 0 ? (object)deptCode : DBNull.Value);
                        cmd.Parameters.AddWithValue("@TimeInterval", timeInterval != 0 ? (object)timeInterval : DBNull.Value);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        await con.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new ExpandoObject() as IDictionary<string, object>;
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row.Add(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i));
                                }
                                dataList.Add(row);
                            }
                        }
                        response.status = true;
                        response.data = dataList;
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
                return response;
            }
        }

        public async Task<RepositoryResponse> saveOrUpdate(QcTemperature model)
        {
            var response = new RepositoryResponse();
            string mode = "";
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();

                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_TapeQuality]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@TapeQuality2", QcTempDataTable(model.TapeQualitys));
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                                cmd.Parameters.AddWithValue("@V_TIME", model.V_TIME == default ? (object)DBNull.Value : model.V_TIME);
                                cmd.Parameters.AddWithValue("@INCH_CODE", model.INCH_CODE == 0 ? (object)DBNull.Value : model.INCH_CODE);
                                cmd.Parameters.AddWithValue("@OPERATORE_CODE", model.OPERATORE_CODE == 0 ? (object)DBNull.Value : model.OPERATORE_CODE);
                                cmd.Parameters.AddWithValue("@SUP_CODE", model.SUP_CODE == 0 ? (object)DBNull.Value : model.SUP_CODE);
                                cmd.Parameters.AddWithValue("@DEPT_CODE", model.DEPT_CODE == 0 ? (object)DBNull.Value : model.DEPT_CODE);
                                cmd.Parameters.AddWithValue("@SHIFT", string.IsNullOrEmpty(model.SHIFT) ? (object)DBNull.Value : model.SHIFT);
                                cmd.Parameters.AddWithValue("@DENIER", model.DENIER == 0 ? (object)DBNull.Value : model.DENIER);
                                cmd.Parameters.AddWithValue("@REMARK", string.IsNullOrEmpty(model.REMARK) ? (object)DBNull.Value : model.REMARK);
                                cmd.Parameters.AddWithValue("@Action", model.SaveOrUpdate == "Save" ? "Add" : "Edit");
                                cmd.Parameters.AddWithValue("@UUSER", usersessionDt.PubUserId);
                                cmd.Parameters.AddWithValue("@WSID", Environment.MachineName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);

                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, -1)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(errorParam);

                                var returnParam = new SqlParameter
                                {
                                    Direction = ParameterDirection.ReturnValue,
                                    SqlDbType = SqlDbType.Int
                                };
                                cmd.Parameters.Add(returnParam);

                                await cmd.ExecuteNonQueryAsync();

                                var returnValue = (int)returnParam.Value;
                                string errorMsg = errorParam.Value?.ToString();

                                if (returnValue > 0)
                                {
                                    transaction.Commit();
                                    if(model.SaveOrUpdate == "Save")
                                    {
                                        mode = "Insert";
                                    }
                                    else
                                    {
                                        mode = "Update";
                                        //_globalValidationdate.LogInsertUpdateDelete(destinationTable: "TAPE_QUALITY1", sourceTable: "TAPE_QUALITY1", transactionType: "Transaction",
                                        //codeVNo: model.V_NO.ToString(), vtype: model.V_TYPE);
                                        //_globalValidationdate.LogInsertUpdateDelete(destinationTable: "TAPE_QUALITY2", sourceTable: "TAPE_QUALITY2", transactionType: "Transaction",
                                        //codeVNo: model.V_NO.ToString(), vtype: model.V_TYPE);
                                    }
                                    //_logService.InsertLog("TAPE_QUALITY1", "QC Temperature Entry", "Transaction", mode, model.V_TYPE, model.V_NO.ToString(), model.V_DATE);
                                    //_logService.InsertLog("TAPE_QUALITY2", "QC Temperature Entry", "Transaction", mode, model.V_TYPE, model.V_NO.ToString(), model.V_DATE);
                                    response.status = true;
                                    response.message = "Data saved/updated successfully.";  
                                    return response;
                                }
                                else
                                {
                                    transaction.Rollback();
                                    response.status = false;
                                    response.message = errorMsg ?? "Operation failed.";
                                    return response;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            response.status = false;
                            response.message = "Transaction failed: " + ex.Message;
                            return response;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = "Unexpected error: " + ex.Message;
                return response;
            }
        }

        private DataTable QcTempDataTable(List<TapeQuality2> data)
        {
            var table = new DataTable();
            table.Columns.Add("SNO", typeof(int));
            table.Columns.Add("TYPE", typeof(string));
            table.Columns.Add("V_DATE", typeof(DateTime));
            table.Columns.Add("ROOM_CODE", typeof(int));
            table.Columns.Add("TEMP_READ", typeof(decimal));
            table.Columns.Add("TEMP_REM", typeof(string));
            table.Columns.Add("SPEED_CODE", typeof(int));
            table.Columns.Add("SPEED_READ", typeof(decimal));
            table.Columns.Add("SPEED_READ2", typeof(string));
            table.Columns.Add("WINDER_CODE", typeof(int));
            table.Columns.Add("WIDTH_MM", typeof(decimal));
            table.Columns.Add("DENIER", typeof(decimal));
            table.Columns.Add("BREAKING_LOAD", typeof(decimal));
            table.Columns.Add("TENACITY", typeof(decimal));
            table.Columns.Add("ELONGATION", typeof(decimal));
            table.Columns.Add("MAT_CODE", typeof(int));
            table.Columns.Add("GRADE", typeof(string));
            table.Columns.Add("NO_OF_BAGS", typeof(int));
            table.Columns.Add("MAT_PER", typeof(decimal));
            table.Columns.Add("TIME_TAKEN", typeof(DateTime));

            foreach (var row in data)
            {
                table.Rows.Add(
                    row.SNO,
                    row.TYPE ?? (object)DBNull.Value,
                    row.V_DATE == default ? (object)DBNull.Value : row.V_DATE,
                    row.ROOM_CODE,
                    row.TEMP_READ,
                    row.TEMP_REM ?? (object)DBNull.Value,
                    row.SPEED_CODE,
                    row.SPEED_READ,
                    row.SPEED_READ2 ?? (object)DBNull.Value,
                    row.WINDER_CODE,
                    row.WIDTH_MM,
                    row.DENIER,
                    row.BREAKING_LOAD,
                    row.TENACITY,
                    row.ELONGATION,
                    row.MAT_CODE,
                    row.GRADE ?? (object)DBNull.Value,
                    row.NO_OF_BAGS,
                    row.MAT_PER,
                    row.TIME_TAKEN
                );
            }

            return table;
        }
    }
}
