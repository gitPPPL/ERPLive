using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Transaction
{
    public class LoomFabricWidthEntryRepository : ILoomFabricWidthEntryRepository
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly DbHelper _dbHelper;

        public LoomFabricWidthEntryRepository(DataBaseConnection dbcontext, GlobalVariableService globalValue, DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public async Task<object> GetMaxVNoAsync()
        {
            var userSession = _globalValue.GetGlobalVariables();

            var companyCode = userSession.PubCompCode;
            var yearCode = userSession.PubFYearCode;
            var branchCode = userSession.PubBranchCode;
            var vType = "LINS";
            var tableName = "PROD1_QC";

            var yearParams = new Dictionary<string, object>
            {
                { "@YearCd", yearCode }
            };

            var vnoParams = new Dictionary<string, object>
            {
                { "@COMP_CODE", companyCode },
                { "@BRANCH_CODE", branchCode },
                { "@YEAR_CODE", yearCode },
                { "@V_TYPE", vType },
                { "@TableName", tableName }
            };

            string nextVNo = await _dbHelper.GetExecuteScalarAsync<string>(
                "sp_GetMaxVNo",
                vnoParams,
                isStoredProc: true);

            string year = await _dbHelper.GetExecuteScalarAsync<string>(
                "SELECT dbo.fn_GetCurrentYear(@YearCd)",
                yearParams);

            string docId = vType + year + nextVNo;
            string newVno = year + nextVNo;

            return new
            {
                DocId = docId,
                VNo = newVno
            };
        }

        public async Task<(bool Status, string Message)> SaveOrUpdateLoomFabricEntryAsync(LoomFabricEntryModel model)
        {
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
                            DataTable prod2Table = ToProd2QCDataTable(model.Prod2QCData, model.PLACE_CODE);

                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_LoomFabricEntry]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                var docId = model.VType + model.V_No;

                                cmd.Parameters.AddWithValue("@Action",
                                    model.SaveOrUpdate == "Save" ? "Add" : "Edit");

                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_No);
                                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                                cmd.Parameters.AddWithValue("@DOC_ID", docId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIFT", model.SHIFT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PLACE_CODE", model.PLACE_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EMP_CODE", model.EMP_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QCTIME", model.QCTIME ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QC_INCHARGE", model.QC_INCHARGE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHEMIST", model.CHEMIST ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QC_INCHARGENAME", model.QC_INCHARGENAME ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHEMISTNAME", model.CHEMISTNAME ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId);
                                cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);
                                cmd.Parameters.AddWithValue("@SRNO", model.SRNO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Prod2QCData", prod2Table);

                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 5000)
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

                                int returnValue = Convert.ToInt32(returnParam.Value);
                                string errorMsg = errorParam.Value?.ToString();

                                if (returnValue > 0)
                                {
                                    transaction.Commit();
                                    return (true, "Data saved/updated successfully.");
                                }

                                transaction.Rollback();
                                return (false, errorMsg ?? "Operation failed.");
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return (false, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private DataTable ToProd2QCDataTable(List<Prod2QCDetailModel> data, int? headerPlaceCode)
        {
            var table = new DataTable();

            table.Columns.Add("SNO", typeof(int));
            table.Columns.Add("PLACE_CODE", typeof(int));
            table.Columns.Add("LOOM_CODE", typeof(int));
            table.Columns.Add("EMP_CODE", typeof(int));
            table.Columns.Add("ITEM_CODE", typeof(int));
            table.Columns.Add("PTYPE_CODE", typeof(int));
            table.Columns.Add("PTYPE_NAME", typeof(string));
            table.Columns.Add("WIDTH", typeof(decimal));
            table.Columns.Add("GRAM", typeof(decimal));
            table.Columns.Add("MESH", typeof(string));
            table.Columns.Add("MESH_CODE", typeof(int));
            table.Columns.Add("COLOR_CODE", typeof(int));
            table.Columns.Add("COLOR_NAME", typeof(string));
            table.Columns.Add("RUNNO", typeof(int));
            table.Columns.Add("LOOM_TYPE", typeof(string));
            table.Columns.Add("MAKE_T", typeof(string));
            table.Columns.Add("DNR", typeof(string));
            table.Columns.Add("RESULT1", typeof(decimal));
            table.Columns.Add("REMARKS1", typeof(string));
            table.Columns.Add("RESULT2", typeof(decimal));
            table.Columns.Add("REMARKS2", typeof(string));
            table.Columns.Add("PRKG", typeof(decimal));
            table.Columns.Add("WASTE", typeof(decimal));
            table.Columns.Add("PSIZE", typeof(decimal));
            table.Columns.Add("REMARKS", typeof(string));
            table.Columns.Add("CPRDN", typeof(decimal));
            table.Columns.Add("PAISA_TYPE", typeof(string));
            table.Columns.Add("PAISA_SIZE", typeof(string));
            table.Columns.Add("PAISA_MTR", typeof(int));
            table.Columns.Add("PAISA_TYPE1", typeof(string));
            table.Columns.Add("PORD_TYPE", typeof(string));
            table.Columns.Add("PORD_NO", typeof(int));
            table.Columns.Add("COND1", typeof(short));
            table.Columns.Add("COND2", typeof(short));
            table.Columns.Add("SHIFT_SCH", typeof(string));
            table.Columns.Add("REPORT_FILTER", typeof(int));
            table.Columns.Add("TIME1_WIDTH", typeof(decimal));
            table.Columns.Add("TIME2_WIDTH", typeof(decimal));
            table.Columns.Add("TIME3_WIDTH", typeof(decimal));
            table.Columns.Add("TIME4_WIDTH", typeof(decimal));
            table.Columns.Add("TIME5_WIDTH", typeof(decimal));
            table.Columns.Add("PC_LOWMELT", typeof(decimal));
            table.Columns.Add("GLUE_CONTENT", typeof(decimal));
            table.Columns.Add("OTHERS", typeof(decimal));
            table.Columns.Add("YELLOWP", typeof(decimal));
            table.Columns.Add("BLUEP", typeof(decimal));
            table.Columns.Add("OTHERP", typeof(decimal));
            table.Columns.Add("GRADE", typeof(string));
            table.Columns.Add("YELLOW160C", typeof(decimal));
            table.Columns.Add("MOISTURE", typeof(decimal));
            table.Columns.Add("BULKDENSITY", typeof(decimal));
            table.Columns.Add("PH_FLAKES", typeof(decimal));
            table.Columns.Add("OVERSIZED", typeof(decimal));
            table.Columns.Add("SRNO", typeof(int));
            table.Columns.Add("WARP_ELONG", typeof(decimal));
            table.Columns.Add("WEFT_ELONG", typeof(decimal));
            table.Columns.Add("WARP_MESH", typeof(decimal));
            table.Columns.Add("WEFT_MESH", typeof(decimal));
            table.Columns.Add("SUPPLY_TYPE", typeof(string));
            table.Columns.Add("COLOR_TYPE", typeof(string));

            if (data == null || !data.Any())
                return table;

            foreach (var row in data)
            {
                table.Rows.Add(
                    row.SNO,
                    headerPlaceCode ?? row.PLACE_CODE,
                    row.LOOM_CODE,
                    row.EMP_CODE,
                    row.ITEM_CODE,
                    row.PTYPE_CODE,
                    row.PTYPE_NAME,
                    row.WIDTH,
                    row.GRAM,
                    row.MESH,
                    row.MESH_CODE,
                    row.COLOR_CODE,
                    row.COLOR_NAME,
                    row.RUNNO,
                    row.LOOM_TYPE,
                    row.MAKE_T,
                    row.DNR,
                    row.RESULT1,
                    row.REMARKS1,
                    row.RESULT2,
                    row.REMARKS2,
                    row.PRKG,
                    row.WASTE,
                    row.PSIZE,
                    row.REMARKS,
                    row.CPRDN,
                    row.PAISA_TYPE,
                    row.PAISA_SIZE,
                    row.PAISA_MTR,
                    row.PAISA_TYPE1,
                    row.PORD_TYPE,
                    row.PORD_NO,
                    row.COND1,
                    row.COND2,
                    row.SHIFT_SCH,
                    row.REPORT_FILTER,
                    row.TIME1_WIDTH,
                    row.TIME2_WIDTH,
                    row.TIME3_WIDTH,
                    row.TIME4_WIDTH,
                    row.TIME5_WIDTH,
                    row.PC_LOWMELT,
                    row.GLUE_CONTENT,
                    row.OTHERS,
                    row.YELLOWP,
                    row.BLUEP,
                    row.OTHERP,
                    row.GRADE,
                    row.YELLOW160C,
                    row.MOISTURE,
                    row.BULKDENSITY,
                    row.PH_FLAKES,
                    row.OVERSIZED,
                    row.SRNO,
                    row.WARP_ELONG,
                    row.WEFT_ELONG,
                    row.WARP_MESH,
                    row.WEFT_MESH,
                    row.SUPPLY_TYPE,
                    row.COLOR_TYPE
                );
            }

            return table;
        }

        public async Task<object> GetLastQCEntryAsync()
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();

                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (var cmd = new SqlCommand("sp_GetLoomFabricEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                        cmd.Parameters.AddWithValue("@Action", "LastQCEntry");

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new
                                {
                                    SHIFT = reader["SHIFT"]?.ToString(),
                                    PLACE_CODE = reader["PLACE_CODE"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["PLACE_CODE"]),
                                    EMP_CODE = reader["EMP_CODE"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["EMP_CODE"])
                                };
                            }
                        }
                    }
                }

                return null;
            }
            catch
            {
                throw;
            }
        }


    }
     
}
