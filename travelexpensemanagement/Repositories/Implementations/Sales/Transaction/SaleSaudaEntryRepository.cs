using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class SaleSaudaEntryRepository : ISaleSaudaEntryRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public SaleSaudaEntryRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
        }

        public async Task<object> SaveAndUpdateDataAsync([FromForm] SaleSaudaEntryModel model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            string vtype = "SAUD";
            string docId = vtype + model.V_NO;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                try
                {
                    bool isUpdate;

                    using (SqlCommand checkCmd = new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM SAUDA
                    WHERE YEAR_CODE = @YEAR_CODE
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND V_TYPE = @V_TYPE
                      AND V_NO = @V_NO", con))
                    {
                        checkCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        checkCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        checkCmd.Parameters.AddWithValue("@V_TYPE", vtype);
                        checkCmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);

                        isUpdate = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    }

                    using (SqlCommand cmd = new SqlCommand("sp_SaleSauda_Entry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", isUpdate ? "UpdateData" : "InsertData");

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                        cmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@STATUS",(object?)model.STATUS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PARTY_CODE",(object?)model.PARTY_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PARTY_TO",(object?)model.PARTY_TO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ADD1",(object?)model.ADD1 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ADD2", (object?)model.ADD2 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ADD3",(object?)model.ADD3 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CITY_CODE",(object?)model.CITY_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PHONE",(object?)model.PHONE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ITEM_TYPE",(object?)model.ITEM_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SAUDA_TYPE",(object?)model.SAUDA_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ITEM_CODE",(object?)model.ITEM_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TENACITY_GRPCODE",(object?)model.TENACITY_GRPCODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TENACITY_GRP",(object?)model.TENACITY_GRP ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TRUCK_NO", (object?)model.TRUCK_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@QTY",(object?)model.QTY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@RATE",(object?)model.RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CURRENCY",(object?)model.CURRENCY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DISC_PER",(object?)model.DISC_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CDISC_PER",(object?)model.CDISC_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DISC_TYPE",(object?)model.DISC_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FRT_TERM",(object?)model.FRT_TERM ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TAX_TERM",(object?)model.TAX_TERM ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FRT_RATE",(object?)model.FRT_RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TAX_RATE",(object?)model.TAX_RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NET_RATE",(object?)model.NET_RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PAYTERM_CODE",(object?)model.PAYTERM_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CD_DAYS",(object?)model.CD_DAYS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DEFECTIVE_GOODS",(object?)model.DEFECTIVE_GOODS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ITEM_REMARKS",(object?)model.ITEM_REMARKS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DEL_TERM",(object?)model.DEL_TERM ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DELIVERY_DAYS",(object?)model.DELIVERY_DAYS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARK",(object?)model.REMARK ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DEAL_THROUGH",(object?)model.DEAL_THROUGH ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PINO",(object?)model.PINO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@OFFERNO",(object?)model.OFFERNO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BROKER_RATE",(object?)model.BROKER_RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FLAKES_SIZE",(object?)model.FLAKES_SIZE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FLAKES_SIZEMAX",(object?)model.FLAKES_SIZEMAX ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FLAKES_PVCPPM",(object?)model.FLAKES_PVCPPM ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FLAKES_PPMALL",(object?)model.FLAKES_PPMALL ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@GRADE",(object?)model.GRADE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WASTE_PER",(object?)model.WASTE_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FLAKES_USETYPE",(object?)model.FLAKES_USETYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DEL_STATION",(object?)model.DEL_STATION ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REF_ASTYPE",(object?)model.REF_ASTYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DEL_PORT",(object?)model.DEL_PORT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SIZE",(object?)model.SIZE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@INCOTERM",(object?)model.INCOTERM ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ATTACHMENT_PATH",(object?)model.ATTACHMENT_PATH ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REF_ASNO",(object?)model.REF_ASNO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@STUFFING_WT",(object?)model.STUFFING_WT ?? DBNull.Value);
                        
                        cmd.Parameters.AddWithValue("@FAPROV_STATUS",(object?)model.FAPROV_STATUS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS",(object?)model.FAPROV_REMARKS ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@SHIP_TYPE",(object?)model.SHIP_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NOS",(object?)model.NOS ?? DBNull.Value);
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
                                WHERE DOC_ID = @DOC_ID
                                  AND YEAR_CODE = @YEAR_CODE
                                  AND COMP_CODE = @COMP_CODE
                                  AND BRANCH_CODE = @BRANCH_CODE", con))
                                {
                                    deleteImageCmd.Parameters.AddWithValue("@DOC_ID", docId);
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

                                using (SqlCommand imageCmd = new SqlCommand("sp_SaleSauda_Entry", con))
                                {
                                    imageCmd.CommandType = CommandType.StoredProcedure;

                                    imageCmd.Parameters.AddWithValue("@Action", "InsertImage");
                                    
                                    imageCmd.Parameters.AddWithValue("@DOC_ID", docId);
                                    imageCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                    imageCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                    imageCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                    imageCmd.Parameters.AddWithValue("@V_TYPE", vtype);
                                    imageCmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                                    imageCmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);

                                    imageCmd.Parameters.AddWithValue("@IMG_FILE", imageBytes);
                                    imageCmd.Parameters.AddWithValue("@FILE_NAME", model.Attachment.FileName);

                                    imageCmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                                    imageCmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                                    imageCmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                                    imageCmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                    imageCmd.Parameters.AddWithValue("@FILE_TYPE", model.Attachment.ContentType);
                                    imageCmd.Parameters.AddWithValue("@FILE_DESC", "Sales Sauda Attachment");
                                    imageCmd.Parameters.AddWithValue("@FILE_Path", DBNull.Value);
                                    
                                    await imageCmd.ExecuteNonQueryAsync();
                                }
                            }
                        }
                    }

                    return new { success = true, message = isUpdate ? "Data updated successfully." : "Data saved successfully." };
                }
                catch (Exception ex)
                {
                    return new { success = false, message = ex.Message };
                }
            }
        }

    }
}
