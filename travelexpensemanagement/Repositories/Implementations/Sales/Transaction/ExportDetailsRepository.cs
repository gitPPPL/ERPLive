using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class ExportDetailsRepository : IExportDetailsRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public ExportDetailsRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _logService = logService;
        }

        public async Task<object> SaveAndUpdateDataAsync([FromForm] ExportDetailsModel model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            string vtype = "EXSD";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                SqlTransaction tran = con.BeginTransaction();
                
                try
                {
                    bool isUpdate;

                    using (SqlCommand checkCmd = new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM SAUDA_EXPORT
                    WHERE REF_TYPE = @REF_TYPE
                      AND REF_NO = @REF_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE", con, tran))
                    {
                        checkCmd.Parameters.AddWithValue("@REF_TYPE", (object?)model.REF_TYPE ?? DBNull.Value);
                        checkCmd.Parameters.AddWithValue("@REF_NO", (object?)model.REF_NO ?? DBNull.Value);
                        checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        checkCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        checkCmd.Parameters.AddWithValue("@YEAR_CODE",  globalVariable.PubFYearCode);
                             
                        isUpdate = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    }
                        
                    using (SqlCommand cmd = new SqlCommand("Sp_ExportDetails", con, tran))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", isUpdate ? "UpdateData" : "InsertData");

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                        cmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@REF_TYPE", (object?)model.REF_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REF_NO", (object?)model.REF_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@INCO_TERM", (object?)model.INCO_TERM ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EX_RATE", (object?)model.EX_RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@POL_CODE", (object?)model.POL_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@POD_CODE", (object?)model.POD_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FINAL_COUNTRY", (object?)model.FINAL_COUNTRY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BL_TYPE", (object?)model.BL_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SEAPORT_CODE", (object?)model.SEAPORT_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NOTIFY_PERSON", (object?)model.NOTIFY_PERSON ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NOTIFY_TO", (object?)model.NOTIFY_TO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@COMMISSION_AGENT", (object?)model.COMMISSION_AGENT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LC_NUMBER", (object?)model.LC_NUMBER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LC_DATE", (object?)model.LC_DATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FRT_AMT", (object?)model.FRT_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@INSU_AMT", (object?)model.INSU_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@COMM_AMT", (object?)model.COMM_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BANK_CODE", (object?)model.BANK_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BANK_BRANCH", (object?)model.BANK_BRANCH ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ACT_NUMBER", (object?)model.ACT_NUMBER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IFSC_CODE", (object?)model.IFSC_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AD_CODE", (object?)model.AD_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SWIFT_CODE", (object?)model.SWIFT_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NATURE_PACKING", (object?)model.NATURE_PACKING ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TECH_SPEC", (object?)model.TECH_SPEC ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SPL_INST", (object?)model.SPL_INST ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FILE_NAME", (object?)model.FILE_NAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@COMM_NARR", (object?)model.COMM_NARR ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FPOD", (object?)model.FPOD ?? DBNull.Value);
                        
                        cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        await cmd.ExecuteNonQueryAsync();

                        // ==========================================
                        // SAVE IMAGE
                        // ==========================================
                        if (model.Attachment != null && model.Attachment.Length > 0)
                        {
                            if (isUpdate)
                            {
                                using (SqlCommand deleteImageCmd = new SqlCommand(@"
                                DELETE FROM IMG_TABLE
                                WHERE V_TYPE = @V_TYPE
                                  AND V_NO = @V_NO  
                                  AND YEAR_CODE = @YEAR_CODE
                                  AND COMP_CODE = @COMP_CODE
                                  AND BRANCH_CODE = @BRANCH_CODE", con, tran))
                                {
                                    deleteImageCmd.Parameters.AddWithValue("@V_TYPE", vtype);
                                    deleteImageCmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                                    deleteImageCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                    deleteImageCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                    deleteImageCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                                    await deleteImageCmd.ExecuteNonQueryAsync();
                                }
                            }

                            using (var memoryStream = new MemoryStream())
                            {
                                await model.Attachment.CopyToAsync(memoryStream);

                                byte[] imageBytes = memoryStream.ToArray();

                                using (SqlCommand imageCmd = new SqlCommand("Sp_ExportDetails", con, tran))
                                {
                                    imageCmd.CommandType = CommandType.StoredProcedure;

                                    imageCmd.Parameters.AddWithValue("@Action", "InsertImage");
                                    imageCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                    imageCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                    imageCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                    imageCmd.Parameters.AddWithValue("@V_TYPE", vtype);
                                    imageCmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                                    imageCmd.Parameters.AddWithValue("@DOC_ID", vtype + model.V_NO);

                                    imageCmd.Parameters.AddWithValue("@IMG_FILE", imageBytes);
                                    imageCmd.Parameters.AddWithValue("@FILE_NAME", model.Attachment.FileName);

                                    imageCmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                                    imageCmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                                    imageCmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                                    imageCmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                    imageCmd.Parameters.AddWithValue("@FILE_TYPE", model.Attachment.ContentType);
                                    imageCmd.Parameters.AddWithValue("@FILE_DESC", "Export Details Attachment");
                                    imageCmd.Parameters.AddWithValue("@FILE_Path", DBNull.Value);

                                    await imageCmd.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        // ==========================================
                        // UPDATE SALE1 PORT DETAILS
                        // ==========================================
                        if (!string.IsNullOrWhiteSpace(model.POD_CODE) && !string.IsNullOrWhiteSpace(model.POL_CODE))
                        {
                            using (SqlCommand portCmd = new SqlCommand(@"
                            UPDATE SALE1
                            SET PORT_LOADING = @POL_CODE,
                            PORT_DISCHARGE = @POD_CODE
                            WHERE V_TYPE = 'DMEU'
                              AND V_NO = @REF_NO
                              AND COMP_CODE = @COMP_CODE
                              AND BRANCH_CODE = @BRANCH_CODE",
                                con, tran))
                            {
                                portCmd.Parameters.AddWithValue("@POL_CODE", model.POL_CODE);
                                portCmd.Parameters.AddWithValue("@POD_CODE", model.POD_CODE);
                                portCmd.Parameters.AddWithValue("@REF_NO", model.REF_NO);
                                portCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                portCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                                await portCmd.ExecuteNonQueryAsync();
                            }
                        }

                        // ==========================================
                        // UPDATE REFERENCE DOCUMENT
                        // ==========================================
                        if (string.Equals(model.REF_TYPE, "SAUD", StringComparison.OrdinalIgnoreCase))
                        {
                            // ======================================
                            // SAUDA
                            // ======================================
                            using (SqlCommand refCmd = new SqlCommand(@"
                            UPDATE SAUDA
                            SET EXPV_TYPE = 'EXSD',
                                EXPV_NO = @EXPV_NO,
                                DEL_PORT = @DEL_PORT
                            WHERE V_TYPE = @V_TYPE
                              AND V_NO = @V_NO
                              AND COMP_CODE = @COMP_CODE
                              AND BRANCH_CODE = @BRANCH_CODE",
                                con, tran))
                            {
                                refCmd.Parameters.AddWithValue("@EXPV_NO", model.V_NO);
                                refCmd.Parameters.AddWithValue("@DEL_PORT", (object?)model.POD_CODE ?? DBNull.Value);
                                refCmd.Parameters.AddWithValue("@V_TYPE", model.REF_TYPE);
                                refCmd.Parameters.AddWithValue("@V_NO", model.REF_NO);
                                refCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                refCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                await refCmd.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // ======================================
                            // SALE1
                            // ======================================
                            using (SqlCommand refCmd = new SqlCommand(@"
                            UPDATE SALE1
                            SET EXPV_TYPE = 'EXSD',
                                EXPV_NO = @EXPV_NO
                            WHERE V_TYPE = @V_TYPE
                              AND V_NO = @V_NO
                              AND COMP_CODE = @COMP_CODE
                              AND BRANCH_CODE = @BRANCH_CODE",
                                con, tran))
                            {
                                refCmd.Parameters.AddWithValue("@EXPV_NO", model.V_NO);
                                refCmd.Parameters.AddWithValue("@V_TYPE", model.REF_TYPE);
                                refCmd.Parameters.AddWithValue("@V_NO", model.REF_NO);
                                refCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                refCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                                await refCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    await tran.CommitAsync();
                    return new { success = true, message = isUpdate ? "Data updated successfully." : "Data saved successfully." };
                }
                catch (Exception ex)
                {
                    return new { success = false, message = ex.Message };
                }
            }
        }

        public async Task<object> LoadEditDataAsync(int refNo, string refType)
        {
            using var connection = _dbConnection.GetErpConnection();

            using var command = new SqlCommand("Sp_ExportDetails", connection);
            command.CommandType = CommandType.StoredProcedure;

            var global = _globalVariableService.GetGlobalVariables();

            command.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
            command.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
            command.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);

            command.Parameters.AddWithValue("@REF_TYPE", refType);
            command.Parameters.AddWithValue("@REF_NO", refNo);

            command.Parameters.AddWithValue("@Action", "LoadEditData");

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            var exportDetails = new Dictionary<string, object>();
            var images = new List<Dictionary<string, object>>();

            // ================================
            // RESULT SET 1 - EXPORT DETAILS
            // ================================
            if (await reader.ReadAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    exportDetails[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
            }

            // ================================
            // RESULT SET 2 - IMAGE DATA
            // ================================
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    var image = new Dictionary<string, object>();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        image[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }

                    images.Add(image);
                }
            }

            return new
            {
                exportDetails,
                images
            };
        }

        public async Task<object> DeleteDataAsync(int refNo, string refType, int vNo)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            string vtype = "EXSD";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using SqlTransaction tran = con.BeginTransaction();

                try
                {
                    // ==========================================
                    // DELETE EXPORT DETAILS
                    // ==========================================
                    using (SqlCommand cmd = new SqlCommand(@"
                    DELETE FROM SAUDA_EXPORT
                    WHERE V_TYPE = @V_TYPE
                      AND V_NO = @V_NO
                      AND REF_TYPE = @REF_TYPE
                      AND REF_NO = @REF_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE",
                        con, tran))
                    {
                        cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@REF_TYPE", refType);
                        cmd.Parameters.AddWithValue("@REF_NO", refNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // ==========================================
                    // DELETE ATTACHMENT
                    // ==========================================
                    using (SqlCommand imageCmd = new SqlCommand(@"
                    DELETE FROM IMG_TABLE
                    WHERE V_TYPE = @V_TYPE
                      AND V_NO = @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE",
                        con, tran))
                    {
                        imageCmd.Parameters.AddWithValue("@V_TYPE", vtype);
                        imageCmd.Parameters.AddWithValue("@V_NO", vNo);
                        imageCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        imageCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        imageCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                        await imageCmd.ExecuteNonQueryAsync();
                    }

                    // ==========================================
                    // CLEAR REFERENCE DOCUMENT
                    // ==========================================
                    if (string.Equals(refType, "SAUD", StringComparison.OrdinalIgnoreCase))
                    {
                        using (SqlCommand refCmd = new SqlCommand(@"
                        UPDATE SAUDA
                        SET EXPV_TYPE = NULL,
                            EXPV_NO = NULL
                        WHERE V_TYPE = @V_TYPE
                          AND V_NO = @V_NO
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE",
                            con, tran))
                        {
                            refCmd.Parameters.AddWithValue("@V_TYPE", refType);
                            refCmd.Parameters.AddWithValue("@V_NO", refNo);
                            refCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            refCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            await refCmd.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        using (SqlCommand refCmd = new SqlCommand(@"
                        UPDATE SALE1
                        SET EXPV_TYPE = NULL,
                            EXPV_NO = NULL
                        WHERE V_TYPE = @V_TYPE
                          AND V_NO = @V_NO
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE",
                            con, tran))
                        {
                            refCmd.Parameters.AddWithValue("@V_TYPE", refType);
                            refCmd.Parameters.AddWithValue("@V_NO", refNo);
                            refCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            refCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            await refCmd.ExecuteNonQueryAsync();
                        }
                    }

                    await tran.CommitAsync();

                    return new {success = true,message = "Export Details deleted successfully."};
                }
                catch (Exception ex)
                {
                    await tran.RollbackAsync();

                    return new
                    {
                        success = false,
                        message = ex.Message
                    };
                }
            }
        }
    }
}
