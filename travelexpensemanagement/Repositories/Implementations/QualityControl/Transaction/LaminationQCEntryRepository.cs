using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.QualityControl.Transaction;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction.ILaminationQCEntryRepository;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Transaction
{
    public class LaminationQCEntryRepository : ILaminationQCEntryRepository
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly LogService.LogService _logService;
        public LaminationQCEntryRepository(DataBaseConnection dbcontext, GlobalVariableService globalValue, LogService.LogService logService)
        {
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _logService = logService;
        }
        public RepositoryResponseData<int> ProcessTenacityDataAsync(TenacityRequest request)
        {
            var response = new RepositoryResponseData<int>();
            var gv = _globalValue.GetGlobalVariables();
            try
            {
                int tenaMaxcode;

                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    con.Open();

                    // Check if the record exists
                    string checkQuery = "SELECT 1 FROM TENACITY_MAST WHERE NAME = @Name AND COMP_CODE = @COMP_CODE";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@Name", request.StrName);
                        checkCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        bool exists = checkCmd.ExecuteScalar() != null;

                        if (!exists)
                        {
                            // Get the next max code
                            string maxCodeQuery = "SELECT ISNULL(MAX(CODE), 0) + 1 FROM TENACITY_MAST WHERE COMP_CODE = @COMP_CODE";
                            using (SqlCommand maxCodeCmd = new SqlCommand(maxCodeQuery, con))
                            {
                                maxCodeCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                tenaMaxcode = Convert.ToInt32(maxCodeCmd.ExecuteScalar());
                            }

                            // Insert the new record
                            string insertQuery = @"
                        INSERT INTO TENACITY_MAST (CODE, COMP_CODE, NAME, TENACITY_TYPE, MIN_STD, MAX_STD, TENACITY_CAT, TENACITY_CATCODE, ACTIVE, UUSER, UDATE, AED, WSID, LIP, LID)
                        VALUES (@CODE, @COMP_CODE, @NAME, 'NA', @MIN_STD, @MAX_STD, '', 0, 1, @UUSER, GETDATE(), 'A', @WSID, @LIP, @LID)";
                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, con))
                            {
                                insertCmd.Parameters.AddWithValue("@CODE", tenaMaxcode);
                                insertCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                insertCmd.Parameters.AddWithValue("@NAME", request.StrName);
                                insertCmd.Parameters.AddWithValue("@MIN_STD", request.WarpWay);
                                insertCmd.Parameters.AddWithValue("@MAX_STD", request.WeftWay);
                                insertCmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                insertCmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                insertCmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                insertCmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                insertCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Get the existing code
                            string getCodeQuery = "SELECT ISNULL(MAX(CODE), 0) FROM TENACITY_MAST WHERE COMP_CODE = @COMP_CODE AND NAME = @Name";
                            using (SqlCommand getCodeCmd = new SqlCommand(getCodeQuery, con))
                            {
                                getCodeCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                getCodeCmd.Parameters.AddWithValue("@Name", request.StrName);
                                tenaMaxcode = Convert.ToInt32(getCodeCmd.ExecuteScalar());
                            }
                        }
                    }
                }
                response.status = true;
                response.data = tenaMaxcode;
                return response;
            }
            catch (Exception ex)
            {
                response.status = true;
                response.message = ex.Message;
                return response;
            }
        }

        public async Task<RepositoryResponse> UpdateLaminationAsync(LaminationUpdateModel model)
        {
            var response = new RepositoryResponse();
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    var userSession = _globalValue.GetGlobalVariables();

                    foreach (var detail in model.LaminationDetails)
                    {
                        var docid = detail.Docid ?? "000000";
                        var vno = docid.Length >= 5 ? docid.Substring(4) : "0";
                        var vtype = docid.Length >= 4 ? docid.Substring(0, 4) : "0000";

                        using (SqlCommand cmd = new SqlCommand("sp_UpdateLamination", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Action", "Update");
                            cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                            cmd.Parameters.AddWithValue("@V_NO", vno);
                            cmd.Parameters.AddWithValue("@V_TYPE", vtype);

                            // Optional parameters
                            cmd.Parameters.AddWithValue("@NWARPWAY_RES", (object?)detail.NWARPWAY_RES ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@WARPWAY_RES", (object?)detail.WARPWAY_RES ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@NWEFTWAY_RES", (object?)detail.NWEFTWAY_RES ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@WEFTWAY_RES", (object?)detail.WEFTWAY_RES ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ELONG_WARP", (object?)detail.ELONG_WARP ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ELONG_WEFT", (object?)detail.ELONG_WEFT ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@QC_REMARKS", (object?)detail.QC_REMARKS ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@STATUS_CODE_A", (object?)detail.STATUS_CODE_A ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TENA_CODE_A", (object?)detail.TENA_CODE_A ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@LAMSUP_CODE", (object?)detail.LAMSUP_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@LAMSUP_NAME", (object?)detail.LAMSUP_NAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@LAMOP_CODE", (object?)detail.LAMOP_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@LAMOP_NAME", (object?)detail.LAMOP_NAME ?? DBNull.Value);
                            //cmd.Parameters.AddWithValue("@QCUSER", (object?)detail.QCUSER ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@QCUSER", userSession.PubUserId);


                            var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 5000)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(errorParam);

                            SqlParameter returnValue = new SqlParameter("@ReturnVal", SqlDbType.Int)
                            {
                                Direction = ParameterDirection.ReturnValue
                            };
                            cmd.Parameters.Add(returnValue);

                            if (con.State != ConnectionState.Open)
                                await con.OpenAsync();

                            await cmd.ExecuteNonQueryAsync();

                            int result = (int)returnValue.Value;
                            string error = errorParam.Value?.ToString();

                            if (result != 1)
                            {
                                // Optional: collect all errors and return them together
                                //return BadRequest(new { success = false, message = error ?? "Unknown error during update." });
                                response.status = false;
                                response.message = error ?? "Unknown error during update.";
                                return response;
                            }
                        }
                    }
                    response.status = true;
                    response.message = "All lamination records updated successfully.";
                    return response;
                    //return Json(new { status = true, message = "All lamination records updated successfully." });
                }
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
                return response;
                //return Json(new { status = false, message = ex.Message });
            }
        }

    }
}
