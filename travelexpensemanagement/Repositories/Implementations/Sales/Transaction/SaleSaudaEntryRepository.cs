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

                    // ================================
                    // VALIDATE DATA
                    // ================================
                    var validation = await ValidateDataAsync(model, globalVariable, con);

                    if (!validation.IsValid)
                    {
                        return new { success = false, message = validation.Message };
                    }

                    // ==========================================
                    // APPROVAL STATUS
                    // ==========================================
                    var approval = await GetApprovalStatusAsync(model,globalVariable,con);

                    model.FAPROV_STATUS = approval.FaprovStatus;
                    model.FAPROV_REMARKS = approval.FaprovRemark;

                    //=========================
                    // For DSAU VType
                    //=========================
                    if (isUpdate)
                    {
                        await InsertDSAUSnapshotAsync(con, globalVariable, model.V_NO, model.DSAU_V_NO);
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

        private async Task InsertDSAUSnapshotAsync(SqlConnection con, dynamic globalVariable, int? saudaVNo,string dsauVNo)
        {
            using (SqlCommand cmd = new SqlCommand("sp_SaleSauda_Entry", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "InsertDSAUSnapshot");
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                cmd.Parameters.AddWithValue("@V_NO", (object?)saudaVNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DSAU_V_NO", dsauVNo);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task<(bool IsValid, string Message)> ValidateDataAsync(SaleSaudaEntryModel model, dynamic globalVariable, SqlConnection con)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(@"
                SELECT COUNT(1)
                FROM SUBGROUP_MAST
                WHERE COMP_CODE = @COMP_CODE
                  AND CODE = @PARTY_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE.Value);
                    int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                    if (count == 0)
                        return (false, "Invalid Party Name selected.");
                }

                // ==========================================
                // CUSTOMER COUNTRY
                // ==========================================
                int countryCode = 0;

                using (SqlCommand cmd = new SqlCommand(@"
                SELECT ISNULL(C.COUNTRY_CODE, 0)
                FROM SUBGROUP_MAST S
                LEFT JOIN CITY_MAST C
                    ON C.CODE = S.CITY_CODE
                WHERE S.COMP_CODE = @COMP_CODE
                  AND S.CODE = @PARTY_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                    cmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE.Value);

                    object? result = await cmd.ExecuteScalarAsync();

                    if (result != null && result != DBNull.Value)
                        countryCode = Convert.ToInt32(result);
                }

                // ==========================================
                // OUTSIDE INDIA + LOCAL
                // ==========================================
                if (countryCode > 1 &&
                    string.Equals(model.SHIP_TYPE?.Trim(), "LOCAL", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "It seems you are selecting out of India shipment so please select 'Export' in shipment type also fill Export details.");
                }

                if (model.ITEM_CODE.HasValue && model.ITEM_CODE.Value != 0)
                {
                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM ITEM_MAST
                    WHERE COMP_CODE = @COMP_CODE
                      AND CODE = @ITEM_CODE", con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                        cmd.Parameters.AddWithValue("@ITEM_CODE", model.ITEM_CODE.Value);

                        int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                        if (count == 0)
                            return (false, "Invalid Item Name selected.");
                    }
                }

                // ==========================================
                // TENACITY GROUP
                // ==========================================
                if (Convert.ToInt32(globalVariable.PubCompCode) == 1 && string.Equals(model.ITEM_TYPE?.Trim(), "PSF", StringComparison.OrdinalIgnoreCase))
                {
                    if (!model.TENACITY_GRPCODE.HasValue ||
                        model.TENACITY_GRPCODE.Value == 0)
                    {
                        return (false, "Please select Tenacity Group.");
                    }
                }

                // ==========================================
                // FREIGHT RATE
                // FOR / FOR-TPT
                // ==========================================
                if ((model.FRT_TERM.Trim().Equals("FOR", StringComparison.OrdinalIgnoreCase) || model.FRT_TERM.Trim().Equals("FOR-TPT", StringComparison.OrdinalIgnoreCase))
                    && (!model.FRT_RATE.HasValue || model.FRT_RATE.Value == 0))
                {
                    return (false, "Please enter Freight Rate, if you selected Freight Term 'FOR'.");
                }

                // ==========================================
                // INDIA + EXPORT
                // ==========================================
                if (countryCode == 1 && string.Equals(model.SHIP_TYPE?.Trim(), "EXPORT", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Please check, Customer Country is India but you select shipment type => Export.");
                }

                // ==========================================
                // FREIGHT MASTER
                // FOR + NON EXPORT
                // ==========================================
                if (model.FRT_TERM.Trim().Equals("FOR", StringComparison.OrdinalIgnoreCase) && !string.Equals( model.SHIP_TYPE?.Trim(),"EXPORT",StringComparison.OrdinalIgnoreCase))
                {
                    if (!model.CITY_CODE.HasValue || model.CITY_CODE.Value == 0)
                    {
                        return (false, "City is required.");
                    }

                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM FREIGHT_MAST
                    WHERE CITY_CODE = @CITY_CODE
                      AND COMP_CODE = @COMP_CODE", con))
                    {
                        cmd.Parameters.AddWithValue("@CITY_CODE",model.CITY_CODE.Value);

                        cmd.Parameters.AddWithValue("@COMP_CODE",globalVariable.PubCompCode);

                        int count = Convert.ToInt32(
                            await cmd.ExecuteScalarAsync());

                        if (count <= 0)
                        {
                            return ( false, "Please check the Freight Master. The freight master for this station/city has not been created yet. First, create the freight master for this station or city." );
                        }
                    }
                }

                // ==========================================
                // EXPORT VALIDATION
                // ==========================================
                if (!string.Equals(model.SHIP_TYPE?.Trim(), "LOCAL",StringComparison.OrdinalIgnoreCase))
                {
                    // ------------------------------------------
                    // PI REQUIRED
                    // ------------------------------------------
                    if (string.IsNullOrWhiteSpace(model.PINO))
                    {
                        return ( false, "Please Create and select Proforma Invoice no. in case of Export Invoice.");
                    }

                    // ------------------------------------------
                    // SAUDA EXPORT DETAILS
                    // ------------------------------------------
                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT TOP 1
                        POL_CODE,
                        FPOD,
                        SEAPORT_CODE,
                        FINAL_COUNTRY,
                        NOTIFY_PERSON
                    FROM SAUDA_EXPORT
                    WHERE REF_TYPE = @REF_TYPE
                      AND REF_NO = @REF_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE", con))
                    {
                        cmd.Parameters.AddWithValue( "@REF_TYPE",model.SAUDA_TYPE ?? (object)DBNull.Value);

                        cmd.Parameters.AddWithValue( "@REF_NO", model.V_NO.Value);

                        cmd.Parameters.AddWithValue("@COMP_CODE",globalVariable.PubCompCode);

                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                        using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                        if (!await reader.ReadAsync())
                        {
                            return ( false, "Please fill all the necessary details in Export detail Form.");
                        }

                        string polCode = Convert.ToString(reader["POL_CODE"]) ?? "";

                        string fpod = Convert.ToString(reader["FPOD"]) ?? "";

                        string seaPortCode = Convert.ToString(reader["SEAPORT_CODE"]) ?? "";

                        int finalCountry = 0;
                        int notifyPerson = 0;

                        if (reader["FINAL_COUNTRY"] != DBNull.Value)
                            int.TryParse(Convert.ToString(reader["FINAL_COUNTRY"]),out finalCountry);

                        if (reader["NOTIFY_PERSON"] != DBNull.Value)
                            int.TryParse(Convert.ToString(reader["NOTIFY_PERSON"]),out notifyPerson);

                        if (string.IsNullOrWhiteSpace(polCode) && string.IsNullOrWhiteSpace(fpod) && string.IsNullOrWhiteSpace(seaPortCode) &&
                            finalCountry == 0 && notifyPerson == 0)
                        {
                            return (false, "Please fill all the necessary details in Export detail Form." );
                        }
                    }
                }

                // ==========================================
                // ALL VALIDATIONS PASSED
                // ==========================================
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<object> LoadEditDataAsync(int vNo, string vType)
        {
            try
            {
                var globalVariable = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_SaleSauda_Entry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "LoadListData");
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return new { success = false, message = "Sales Sauda data not found." };
                            }

                            var data = new Dictionary<string, object?>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);

                                data[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }

                            var images = new List<Dictionary<string, object?>>();

                            if (await reader.NextResultAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var image = new Dictionary<string, object?>();

                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        string columnName = reader.GetName(i);

                                        image[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    }

                                    images.Add(image);
                                }
                            }

                            return new {success = true, data = data, images = images };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new {success = false, message = ex.Message};
            }
        }

        public async Task<object> CreateSalesOrderAsync(int saudaVNo)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using SqlTransaction tran = con.BeginTransaction();

                try
                {
                    // ==========================================
                    // 1. CHECK SALES ORDER CREATE AUTHORIZATION
                    // ==========================================

                    using (SqlCommand authCmd = new SqlCommand(@"
                    SELECT ISNULL(_ADD, 0)
                    FROM USER_MENU
                    WHERE COMP_CODE = @COMP_CODE
                      AND MENU_CODE = 77
                      AND YEAR_CODE = @YEAR_CODE
                      AND USER_CODE = @USER_CODE", con, tran))
                    {
                        authCmd.Parameters.AddWithValue("@COMP_CODE",globalVariable.PubCompCode);

                        authCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                        authCmd.Parameters.AddWithValue("@USER_CODE",globalVariable.PubUserId);

                        object? result = await authCmd.ExecuteScalarAsync();

                        int addPermission = result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);

                        if (addPermission != 1)
                        {
                            if (globalVariable.PubUserLevel != "1")
                            {
                                await tran.RollbackAsync();

                                return new
                                {
                                    success = false,
                                    message = "You are not authorised to Create Sales Order."
                                };
                            }
                        }
                    }

                    // ==========================================
                    // 2. CHECK SALES ORDER ALREADY EXISTS
                    // ==========================================
                    using (SqlCommand checkOrderCmd = new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM ORDER1
                    WHERE V_TYPE = 'SORD'
                      AND SAUDA_TYPE = 'SAUD'
                      AND SAUDA_NO = @SAUDA_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE", con, tran))
                    {
                        checkOrderCmd.Parameters.AddWithValue("@SAUDA_NO", saudaVNo);
                        checkOrderCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        checkOrderCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        checkOrderCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                        int count = Convert.ToInt32(
                            await checkOrderCmd.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            await tran.RollbackAsync();

                            return new
                            {
                                success = false,
                                message = "Order already created of this Sauda."
                            };
                        }
                    }

                    // ==========================================
                    // 3. CHECK SAUDA APPROVAL
                    // ==========================================
                    string faprovStatus = "";

                    using (SqlCommand saudaCmd = new SqlCommand(@"
                    SELECT FAPROV_STATUS
                    FROM SAUDA
                    WHERE V_TYPE = 'SAUD'
                      AND V_NO = @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE", con, tran))
                    {
                        saudaCmd.Parameters.AddWithValue("@V_NO", saudaVNo);
                        saudaCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        saudaCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        saudaCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                        object? result = await saudaCmd.ExecuteScalarAsync();

                        if (result == null || result == DBNull.Value)
                        {
                            await tran.RollbackAsync();

                            return new
                            {
                                success = false,
                                message = "Sales Sauda data not found."
                            };
                        }

                        faprovStatus = Convert.ToString(result) ?? "";
                    }

                    // ==========================================
                    // 4. APPROVAL VALIDATION
                    // ==========================================

                    if (!string.Equals(faprovStatus.Trim(), "Approved", StringComparison.OrdinalIgnoreCase))
                    {
                        await tran.RollbackAsync();

                        return new
                        {
                            success = false,
                            message = "Please check, Sauda not approved yet. so, Order can not create."
                        };
                    }

                    // ==========================================
                    // 4. LOAD SAUDA MODEL DATA
                    // ==========================================
                    SaleSaudaEntryModel? model = null;

                    using (SqlCommand loadCmd = new SqlCommand(@"
                    SELECT
                        V_NO, V_DATE, DOC_ID, STATUS, PARTY_CODE, PARTY_TO, ADD1, ADD2, ADD3, CITY_CODE,
                        ITEM_TYPE, ITEM_CODE, TENACITY_GRPCODE, TENACITY_GRP, QTY, RATE, PAYTERM_CODE,
                        REMARK, PINO, SHIP_TYPE, FAPROV_STATUS, FAPROV_REMARKS
                    FROM SAUDA
                    WHERE V_TYPE = 'SAUD'
                      AND V_NO = @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE", con, tran))
                    {
                        loadCmd.Parameters.AddWithValue("@V_NO", saudaVNo);
                        loadCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        loadCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        loadCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                        using SqlDataReader reader = await loadCmd.ExecuteReaderAsync();

                        if (await reader.ReadAsync())
                        {
                            model = new SaleSaudaEntryModel
                            {
                                V_NO = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]),

                                V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]),

                                DOC_ID = Convert.ToString(reader["DOC_ID"]),

                                STATUS = reader["STATUS"] == DBNull.Value ? null : Convert.ToInt32(reader["STATUS"]),

                                PARTY_CODE = reader["PARTY_CODE"] == DBNull.Value ? null  : Convert.ToInt32(reader["PARTY_CODE"]),

                                PARTY_TO = Convert.ToString(reader["PARTY_TO"]),

                                ADD1 = Convert.ToString(reader["ADD1"]),
                                ADD2 = Convert.ToString(reader["ADD2"]),
                                ADD3 = Convert.ToString(reader["ADD3"]),

                                CITY_CODE = reader["CITY_CODE"] == DBNull.Value ? null  : Convert.ToInt32(reader["CITY_CODE"]),

                                ITEM_TYPE = Convert.ToString(reader["ITEM_TYPE"]),

                                ITEM_CODE = reader["ITEM_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["ITEM_CODE"]),

                                TENACITY_GRPCODE =reader["TENACITY_GRPCODE"] == DBNull.Value ? null : Convert.ToInt32(reader["TENACITY_GRPCODE"]),

                                TENACITY_GRP =Convert.ToString(reader["TENACITY_GRP"]),

                                QTY = reader["QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["QTY"]),

                                RATE = reader["RATE"] == DBNull.Value? null : Convert.ToDecimal(reader["RATE"]),

                                PAYTERM_CODE =  reader["PAYTERM_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["PAYTERM_CODE"]),

                                REMARK = Convert.ToString(reader["REMARK"]),

                                PINO = Convert.ToString(reader["PINO"]),

                                SHIP_TYPE = Convert.ToString(reader["SHIP_TYPE"]),

                                FAPROV_STATUS = Convert.ToString(reader["FAPROV_STATUS"]),

                                FAPROV_REMARKS = Convert.ToString(reader["FAPROV_REMARKS"])
                            };
                        }
                    }

                    if (model == null)
                    {
                        await tran.RollbackAsync();

                        return new
                        {
                            success = false,
                            message = "Sales Sauda data not found."
                        };
                    }

                    // ==========================================
                    // 5. BILLING DETAILS
                    // ==========================================
                    int partyCode = model.PARTY_CODE ?? 0;

                    string billAdd1 = model.ADD1 ?? "";
                    string billAdd2 = model.ADD2 ?? "";
                    string billAdd3 = model.ADD3 ?? "";

                    int cityCode = model.CITY_CODE ?? 0;

                    // ==========================================
                    // 6. SHIP TO DETAILS
                    // ==========================================
                    int shipCode = 0;
                    string shipAdd1 = "";
                    string shipAdd2 = "";
                    string shipAdd3 = "";
                    int shipCity = 0;
                    string shipPincode = "";
                    string shipGst = "";

                    if (!string.IsNullOrWhiteSpace(model.PINO) &&
                        model.PINO.Trim().Length > 4)
                    {
                        using SqlCommand shipCmd = new SqlCommand(@"
                        SELECT TOP 1
                            SHIP_CODE, SHIP_ADD1, SHIP_ADD2, SHIP_ADD3, SHIP_CITY, SHIP_PINCODE, SHIP_GST
                        FROM SALE1
                        WHERE CONCAT(V_TYPE, V_NO) = @PINO
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE", con, tran);

                        shipCmd.Parameters.AddWithValue("@PINO", model.PINO.Trim());

                        shipCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                        shipCmd.Parameters.AddWithValue("@BRANCH_CODE",globalVariable.PubBranchCode);

                        using SqlDataReader reader =await shipCmd.ExecuteReaderAsync();

                        if (await reader.ReadAsync())
                        {
                            shipCode = reader["SHIP_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SHIP_CODE"]);

                            shipAdd1 = Convert.ToString(reader["SHIP_ADD1"]) ?? "";

                            shipAdd2 = Convert.ToString(reader["SHIP_ADD2"]) ?? "";

                            shipAdd3 = Convert.ToString(reader["SHIP_ADD3"]) ?? "";

                            shipCity = reader["SHIP_CITY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SHIP_CITY"]);

                            shipPincode = Convert.ToString(reader["SHIP_PINCODE"]) ?? "";

                            shipGst =Convert.ToString(reader["SHIP_GST"]) ?? "";
                        }

                        await reader.CloseAsync();

                        if (shipCode == 0)
                        {
                            shipCode = partyCode;
                            shipAdd1 = billAdd1;
                            shipAdd2 = billAdd2;
                            shipAdd3 = billAdd3;
                            shipCity = cityCode;
                        }
                    }
                    else
                    {
                        shipCode = partyCode;
                        shipAdd1 = billAdd1;
                        shipAdd2 = billAdd2;
                        shipAdd3 = billAdd3;
                        shipCity = cityCode;
                    }

                    // ==========================================
                    // 7. GENERATE SORD V_NO
                    // ==========================================
                    int sordVNo;

                    using (SqlCommand vnoCmd = new SqlCommand(@"
                    SELECT ISNULL(MAX(V_NO), 0) + 1
                    FROM ORDER1
                    WHERE V_TYPE = 'SORD'
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE", con, tran))
                    {
                        vnoCmd.Parameters.AddWithValue("@COMP_CODE",globalVariable.PubCompCode);

                        vnoCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                        vnoCmd.Parameters.AddWithValue("@YEAR_CODE",globalVariable.PubFYearCode);

                        sordVNo = Convert.ToInt32(await vnoCmd.ExecuteScalarAsync());
                    }

                    string sordDocId = "SORD" + sordVNo;

                    // ==========================================
                    // 8. INSERT ORDER1
                    // ==========================================
                    using (SqlCommand order1Cmd = new SqlCommand(@"
                    INSERT INTO ORDER1
                    (
                        DOC_ID, YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, PARTY_CODE, BILL_ADD1, BILL_ADD2, BILL_ADD3,
                        BILL_CITY, BILL_PINCODE, BILL_GST, SHIP_CODE, SHIP_ADD1, SHIP_ADD2, SHIP_ADD3, SHIP_CITY, SHIP_PINCODE, SHIP_GST,
                        SAUDA_TYPE,  SAUDA_NO,  FAPROV_STATUS, FAPROV_REMARKS, DELIVERY_PERIOD, DELIVERY_TO, PAYTERM_CODE, REMARKS,  DISC_AMT,
                        CDISC_AMT, AUTOGEN_PO,  STATUS, UUSER, UDATE, AED, WSID, LIP, LID
                    )
                    VALUES
                    (
                        @DOC_ID, @YEAR_CODE, @COMP_CODE, @BRANCH_CODE, @V_TYPE, @V_NO, GETDATE(), @PARTY_CODE, @BILL_ADD1, @BILL_ADD2, @BILL_ADD3,
                        @BILL_CITY, @BILL_PINCODE, @BILL_GST, @SHIP_CODE, @SHIP_ADD1, @SHIP_ADD2, @SHIP_ADD3, @SHIP_CITY, @SHIP_PINCODE, @SHIP_GST,
                        @SAUDA_TYPE, @SAUDA_NO, @FAPROV_STATUS, @FAPROV_REMARKS,  NULL, NULL, @PAYTERM_CODE, @REMARKS,  0, 0, 1, @STATUS, @UUSER,
                        GETDATE(), 'A', @WSID, @LIP, @LID
                    )", con, tran))
                    {
                        order1Cmd.Parameters.AddWithValue("@DOC_ID", sordDocId);
                        order1Cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        order1Cmd.Parameters.AddWithValue("@COMP_CODE",globalVariable.PubCompCode);
                        order1Cmd.Parameters.AddWithValue( "@BRANCH_CODE", globalVariable.PubBranchCode);
                        order1Cmd.Parameters.AddWithValue("@V_TYPE", "SORD");
                        order1Cmd.Parameters.AddWithValue("@V_NO", sordVNo);
                        order1Cmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);
                        order1Cmd.Parameters.AddWithValue("@BILL_ADD1", billAdd1);
                        order1Cmd.Parameters.AddWithValue("@BILL_ADD2", billAdd2);
                        order1Cmd.Parameters.AddWithValue("@BILL_ADD3", billAdd3);
                        order1Cmd.Parameters.AddWithValue("@BILL_CITY", cityCode);

                        order1Cmd.Parameters.AddWithValue("@BILL_PINCODE", "");
                        order1Cmd.Parameters.AddWithValue("@BILL_GST", "");

                        order1Cmd.Parameters.AddWithValue("@SHIP_CODE", shipCode);
                        order1Cmd.Parameters.AddWithValue("@SHIP_ADD1", shipAdd1);
                        order1Cmd.Parameters.AddWithValue("@SHIP_ADD2", shipAdd2);
                        order1Cmd.Parameters.AddWithValue("@SHIP_ADD3", shipAdd3);
                        order1Cmd.Parameters.AddWithValue("@SHIP_CITY", shipCity);
                        order1Cmd.Parameters.AddWithValue("@SHIP_PINCODE", shipPincode);
                        order1Cmd.Parameters.AddWithValue("@SHIP_GST", shipGst);

                        order1Cmd.Parameters.AddWithValue("@SAUDA_TYPE", "SAUD");
                        order1Cmd.Parameters.AddWithValue("@SAUDA_NO", saudaVNo);

                        order1Cmd.Parameters.AddWithValue("@FAPROV_STATUS", "Approved");
                        order1Cmd.Parameters.AddWithValue("@FAPROV_REMARKS","Document Approved");
                        order1Cmd.Parameters.AddWithValue("@PAYTERM_CODE", model.PAYTERM_CODE ?? 0);
                        order1Cmd.Parameters.AddWithValue("@REMARKS",model.REMARK ?? "");
                        order1Cmd.Parameters.AddWithValue("@STATUS", model.STATUS ?? 0);
                        order1Cmd.Parameters.AddWithValue("@UUSER",globalVariable.PubUserId);
                        order1Cmd.Parameters.AddWithValue( "@WSID", globalVariable.PubWorkStationID);
                        order1Cmd.Parameters.AddWithValue("@LIP",globalVariable.PubLocalId);
                        order1Cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        await order1Cmd.ExecuteNonQueryAsync();
                    }

                    // ==========================================
                    // 9. GET ITEM NAME
                    // ==========================================
                    string actualItemName = model.ITEM_TYPE ?? "";

                    using (SqlCommand itemCmd = new SqlCommand(@"
                    SELECT TOP 1 SHORTNAME
                    FROM ITEM_MAST
                    WHERE COMP_CODE = @COMP_CODE
                      AND CODE = @ITEM_CODE", con, tran))
                    {
                        itemCmd.Parameters.AddWithValue("@COMP_CODE",globalVariable.PubCompCode);

                        itemCmd.Parameters.AddWithValue("@ITEM_CODE",model.ITEM_CODE ?? 0);

                        object? result =await itemCmd.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            actualItemName =Convert.ToString(result) ?? "";
                        }
                    }

                    // ==========================================
                    // 10. INSERT ORDER2
                    // ==========================================
                    using (SqlCommand order2Cmd = new SqlCommand(@"
                        INSERT INTO ORDER2
                        (
                            DOC_ID,COMP_CODE, BRANCH_CODE,YEAR_CODE, V_NO, V_TYPE, V_DATE,ITEM_CODE, ITEM_NAME, NOS, QTY, RATE, AMOUNT, TENACITY_NAME,
                            TENACITY_CODE, TENACITY_TYPE, TENACITY_GRPCODE, DELIVERY_DATE, SAUDA_TYPE, SAUDA_NO, REMARKS, STATUS, UUSER, UDATE,
                            AED, WSID, LIP, LID,SNO
                        )
                        VALUES
                        (
                            @DOC_ID, @COMP_CODE, @BRANCH_CODE, @YEAR_CODE,  @V_NO, @V_TYPE, GETDATE(), @ITEM_CODE, @ITEM_NAME, @NOS, @QTY, @RATE,
                            @AMOUNT, @TENACITY_NAME, @TENACITY_CODE,@TENACITY_TYPE, @TENACITY_GRPCODE, NULL, @SAUDA_TYPE,@SAUDA_NO,  @REMARKS,
                            @STATUS,  @UUSER, GETDATE(), 'A', @WSID, @LIP,  @LID, @SNO
                        )", con, tran))
                    {
                        order2Cmd.Parameters.AddWithValue("@DOC_ID", sordDocId);
                        order2Cmd.Parameters.AddWithValue( "@COMP_CODE", globalVariable.PubCompCode);
                        order2Cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        order2Cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        order2Cmd.Parameters.AddWithValue("@V_NO", sordVNo);
                        order2Cmd.Parameters.AddWithValue("@V_TYPE", "SORD");

                        order2Cmd.Parameters.AddWithValue( "@ITEM_CODE",model.ITEM_CODE ?? 0);
                        order2Cmd.Parameters.AddWithValue("@ITEM_NAME",actualItemName);

                        order2Cmd.Parameters.AddWithValue("@NOS", model.NOS ?? 1);
                        order2Cmd.Parameters.AddWithValue("@QTY", model.QTY ?? 0);
                        order2Cmd.Parameters.AddWithValue("@RATE",model.RATE ?? 0);
                        order2Cmd.Parameters.AddWithValue("@AMOUNT", (model.QTY ?? 0) * (model.RATE ?? 0));
                        order2Cmd.Parameters.AddWithValue("@TENACITY_NAME","");
                        order2Cmd.Parameters.AddWithValue("@TENACITY_CODE", 0);
                        order2Cmd.Parameters.AddWithValue("@TENACITY_TYPE",model.TENACITY_GRP ?? "");

                        order2Cmd.Parameters.AddWithValue("@TENACITY_GRPCODE",model.TENACITY_GRPCODE ?? 0);

                        order2Cmd.Parameters.AddWithValue("@SAUDA_TYPE", "SAUD");
                        order2Cmd.Parameters.AddWithValue("@SAUDA_NO", saudaVNo);
                        order2Cmd.Parameters.AddWithValue("@REMARKS",model.REMARK ?? "");
                        order2Cmd.Parameters.AddWithValue("@STATUS",model.STATUS ?? 0);
                        order2Cmd.Parameters.AddWithValue("@UUSER",globalVariable.PubUserId);
                        order2Cmd.Parameters.AddWithValue("@WSID",globalVariable.PubWorkStationID);
                        order2Cmd.Parameters.AddWithValue("@LIP",globalVariable.PubLocalId);
                        order2Cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        order2Cmd.Parameters.AddWithValue("@SNO", 1);
                        await order2Cmd.ExecuteNonQueryAsync();
                    }

                    // ==========================================
                    // 11. COMMIT
                    // ==========================================
                    await tran.CommitAsync();

                    return new
                    {
                        success = true,
                        message = $"Sales Order No.: {sordDocId} generated successfully.",
                        docId = sordDocId,
                        vNo = sordVNo
                    };
                }
                catch (Exception ex)
                {
                    try
                    {
                        await tran.RollbackAsync();
                    }
                    catch
                    {
                        // Ignore rollback exception
                    }

                    return new
                    {
                        success = false,
                        message = ex.Message
                    };
                }
            }
        }

        private async Task<(string FaprovStatus, string FaprovRemark)> GetApprovalStatusAsync(SaleSaudaEntryModel model, dynamic globalVariable, SqlConnection con)
        {
            string fappStatus = "";
            string fappRemark = "";

            bool isApprovalBody = false;
            bool isFinalApprovalBody = false;

            // ==========================================
            // 1. CHECK APPROVAL STAGE
            // ==========================================
            using (SqlCommand cmd = new SqlCommand(@"
            SELECT 
                APPROV_USER,
                ISNULL(FLAG_D, '')
            FROM DOC_APPROSTAGE
            WHERE USER_CODE = @USER_CODE
              AND DOC_CODE = @DOC_CODE
              AND COMP_CODE = @COMP_CODE", con))
            {
                cmd.Parameters.AddWithValue("@USER_CODE", globalVariable.PubUserId);
                cmd.Parameters.AddWithValue("@DOC_CODE", "SAUD");
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    isApprovalBody = true;

                    string approvUser = reader["APPROV_USER"] == DBNull.Value ? "" : Convert.ToString(reader["APPROV_USER"]) ?? "";

                    string flagD = reader["FLAG_D"] == DBNull.Value ? "" : Convert.ToString(reader["FLAG_D"]) ?? "";

                    if (string.Equals(approvUser.Trim(), "FINAL", StringComparison.OrdinalIgnoreCase))
                    {
                        isFinalApprovalBody = true;
                    }

                    if (isFinalApprovalBody)
                    {
                        fappStatus = "Approved";
                        fappRemark = "Document Approved.";
                    }

                    // ==========================================
                    // 2. 10K APPROVAL
                    // ==========================================
                    if (string.Equals(flagD.Trim(), "10K", StringComparison.OrdinalIgnoreCase))
                    {
                        decimal qty = model.QTY ?? 0;
                        decimal rate = model.RATE ?? 0;

                        if (qty * rate <= 10000)
                        {
                            fappStatus = "Approved";
                            fappRemark = "Document Approved.";
                        }
                    }
                }
            }

            // ==========================================
            // 3. MARKET RATE APPROVAL
            // ==========================================
            using (SqlCommand cmd = new SqlCommand(@"
            SELECT TOP 1
                MIN_RATE,
                MAX_RATE
            FROM MARKET_RATE1 a
            LEFT JOIN MARKET_RATE2 b
                ON a.V_TYPE = b.V_TYPE
               AND a.V_NO = b.V_NO
               AND a.COMP_CODE = b.COMP_CODE
               AND a.BRANCH_CODE = b.BRANCH_CODE
               AND a.YEAR_CODE = b.YEAR_CODE
            WHERE a.COMP_CODE = @COMP_CODE
              AND a.FAPROV_STATUS = 'Approved'
              AND b.ITEM_CODE = @ITEM_CODE
              AND a.EFF_DATE >= DATEADD(DAY, -20, GETDATE())
            ORDER BY a.V_DATE DESC, a.V_NO DESC", con))
            {
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                cmd.Parameters.AddWithValue("@ITEM_CODE", model.ITEM_CODE ?? 0);
                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    decimal minRate = reader["MIN_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["MIN_RATE"]);

                    decimal maxRate = reader["MAX_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["MAX_RATE"]);

                    decimal currentRate = model.RATE ?? 0;

                    if (currentRate >= minRate && currentRate <= maxRate)
                    {
                        fappStatus = "Approved";
                        fappRemark = "Document Approved.";
                    }
                }
            }

            // ==========================================
            // 4. FINAL APPROVAL -> APPROVAL_STATUS CLOSE
            // ==========================================
            if (isFinalApprovalBody)
            {
                using (SqlCommand checkCmd = new SqlCommand(@"
                SELECT 1
                FROM APPROVAL_STATUS
                WHERE USER_CODE = @USER_CODE
                  AND V_TYPE = @V_TYPE
                  AND V_NO = @V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND YEAR_CODE = @YEAR_CODE", con))
                {
                    checkCmd.Parameters.AddWithValue("@USER_CODE", globalVariable.PubUserId);
                    checkCmd.Parameters.AddWithValue("@V_TYPE", "SAUD");
                    checkCmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                    checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    checkCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    checkCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                    object? exists = await checkCmd.ExecuteScalarAsync();

                    if (exists != null)
                    {
                        using SqlCommand updateCmd = new SqlCommand(@"
                        UPDATE APPROVAL_STATUS
                        SET STATUS = 'CLOSE',
                            CLOSE_DATE = GETDATE(),
                            APPROVAL_CODE = 8,
                            APPROVAL_REMARK = 'Approved',
                            REMARKS = 'Document Approved'
                        WHERE V_TYPE = @V_TYPE
                          AND V_NO = @V_NO
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE
                          AND YEAR_CODE = @YEAR_CODE", con);

                        updateCmd.Parameters.AddWithValue("@V_TYPE", "SAUD");
                        updateCmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                        updateCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        updateCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        updateCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                        await updateCmd.ExecuteNonQueryAsync();
                    }
                }
            }

            return (fappStatus, fappRemark);
        }

        //private async Task<(string FaprovStatus, string FaprovRemark)> GetApprovalStatusAsync(SaleSaudaEntryModel model,dynamic globalVariable,SqlConnection con)
        //{
        //    string fappStatus = "";
        //    string fappRemark = "";

        //    // ==========================================
        //    // 1. CHECK APPROVAL STAGE
        //    // ==========================================
        //    using (SqlCommand cmd = new SqlCommand(@"
        //    SELECT 
        //        APPROV_USER,
        //        ISNULL(FLAG_D, '')
        //    FROM DOC_APPROSTAGE
        //    WHERE USER_CODE = @USER_CODE
        //      AND DOC_CODE = @DOC_CODE
        //      AND COMP_CODE = @COMP_CODE", con))
        //    {
        //        cmd.Parameters.AddWithValue("@USER_CODE", globalVariable.PubUserId);
        //        cmd.Parameters.AddWithValue("@DOC_CODE", "SAUD");
        //        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

        //        using SqlDataReader reader = await cmd.ExecuteReaderAsync();

        //        if (await reader.ReadAsync())
        //        {
        //            string approvUser = reader["APPROV_USER"] == DBNull.Value ? "" : Convert.ToString(reader["APPROV_USER"]) ?? "";
        //            string flagD = reader["FLAG_D"] == DBNull.Value ? "" : Convert.ToString(reader["FLAG_D"]) ?? "";

        //            if (string.Equals(approvUser.Trim(),"FINAL", StringComparison.OrdinalIgnoreCase))
        //            {
        //                fappStatus = "Approved";
        //                fappRemark = "Document Approved.";
        //            }

        //            // ==========================================
        //            // 2. 10K APPROVAL
        //            // ==========================================
        //            if (string.Equals( flagD.Trim(), "10K", StringComparison.OrdinalIgnoreCase))
        //            {
        //                decimal qty = model.QTY ?? 0;
        //                decimal rate = model.RATE ?? 0;

        //                if (qty * rate <= 10000)
        //                {
        //                    fappStatus = "Approved";
        //                    fappRemark = "Document Approved.";
        //                }
        //            }
        //        }
        //    }

        //    // ==========================================
        //    // 3. MARKET RATE APPROVAL
        //    // ==========================================
        //    using (SqlCommand cmd = new SqlCommand(@"
        //    SELECT TOP 1
        //        MIN_RATE,
        //        MAX_RATE
        //    FROM MARKET_RATE1 a
        //    LEFT JOIN MARKET_RATE2 b
        //        ON a.V_TYPE = b.V_TYPE
        //       AND a.V_NO = b.V_NO
        //       AND a.COMP_CODE = b.COMP_CODE
        //       AND a.BRANCH_CODE = b.BRANCH_CODE
        //       AND a.YEAR_CODE = b.YEAR_CODE
        //    WHERE a.COMP_CODE = @COMP_CODE
        //      AND a.FAPROV_STATUS = 'Approved'
        //      AND b.ITEM_CODE = @ITEM_CODE
        //      AND a.EFF_DATE >= DATEADD(DAY, -20, GETDATE())
        //    ORDER BY a.V_DATE DESC, a.V_NO DESC", con))
        //    {
        //        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
        //        cmd.Parameters.AddWithValue("@ITEM_CODE", model.ITEM_CODE ?? 0);

        //        using SqlDataReader reader = await cmd.ExecuteReaderAsync();

        //        if (await reader.ReadAsync())
        //        {
        //            decimal minRate = reader["MIN_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["MIN_RATE"]);

        //            decimal maxRate = reader["MAX_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["MAX_RATE"]);

        //            decimal currentRate = model.RATE ?? 0;

        //            if (currentRate >= minRate && currentRate <= maxRate)
        //            {
        //                fappStatus = "Approved";
        //                fappRemark = "Document Approved.";
        //            }
        //        }
        //    }

        //    return (fappStatus, fappRemark);
        //}
    }
}
