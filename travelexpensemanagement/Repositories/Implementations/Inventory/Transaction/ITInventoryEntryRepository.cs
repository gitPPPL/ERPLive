using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Inventory.Transaction
{
    public class ITInventoryEntryRepository : IITInventoryEntryRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
         
        public ITInventoryEntryRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
        }

        public async Task<object> SaveAndUpdateDataAsync(ITInventoryEntryModel model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            string vtype = "ITIV";
            string docId = vtype + model.V_NO;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using SqlTransaction transaction = con.BeginTransaction();

                try
                {

                    var validation = await ValidateDataAsync(model, con, transaction);

                    if (!validation.IsValid)
                    {
                        await transaction.RollbackAsync();

                        return new
                        {
                            success = false,
                            message = validation.Message
                        };
                    }

                    bool isUpdate;

                    using (SqlCommand checkCmd = new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM IT_INVENTORY
                    WHERE YEAR_CODE = @YEAR_CODE
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND V_TYPE = @V_TYPE
                      AND V_NO = @V_NO", con, transaction))
                    {
                        checkCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        checkCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        checkCmd.Parameters.AddWithValue("@V_TYPE", vtype);
                        checkCmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);

                        isUpdate = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    }

                    if (isUpdate)
                    {
                        using (SqlCommand deleteCmd = new SqlCommand("sp_ITInventoryEntry", con, transaction))
                        {
                            deleteCmd.CommandType = CommandType.StoredProcedure;

                            deleteCmd.Parameters.AddWithValue("@Action", "DeleteExisting");
                            deleteCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            deleteCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            deleteCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            deleteCmd.Parameters.AddWithValue("@DOC_ID", docId);

                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                    }

                    foreach (var device in model.Devices)
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_ITInventoryEntry", con, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Action", "Insert");

                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                            cmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DOC_ID", docId);
                            cmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@UNIT_NAME", (object?)model.UNIT_NAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EMP_CODE", (object?)model.EMP_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DEPT", (object?)model.DEPT ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DESG", (object?)model.DESG ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@LOCATION", (object?)model.LOCATION ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IPADDRESS", (object?)model.IPADDRESS ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@USED_BY", (object?)model.USED_BY ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EMAIL", (object?)model.EMAIL ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DS_USERNAME", (object?)model.DS_USERNAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DS_PASSWORD", EncodePassword(model.DS_PASSWORD));
                            cmd.Parameters.AddWithValue("@SERVER_IP", (object?)model.SERVER_IP ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SERVER_USERNAME", (object?)model.SERVER_USERNAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@CLOUD_USERNAME", (object?)model.CLOUD_USERNAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@CLOUD_PASSWORD", EncodePassword(model.CLOUD_PASSWORD));
                            cmd.Parameters.AddWithValue("@VPN_USERNAME", (object?)model.VPN_USERNAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@VPN_PASSWORD", EncodePassword(model.VPN_PASSWORD));
                            cmd.Parameters.AddWithValue("@EMAIL_PASS", EncodePassword(model.EMAIL_PASS));
                            cmd.Parameters.AddWithValue("@MOB_NO", (object?)model.MOB_NO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@INTERCOM", (object?)model.INTERCOM ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EMP_STATUS", (object?)model.EMP_STATUS ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EMAILLIC_TYPE", (object?)model.EMAILLIC_TYPE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@WINLIC_KEY", (object?)model.WINLIC_KEY ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@WINDOWLIC_TYPE", (object?)model.WINDOWLIC_TYPE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@OFFICELIC_TYPE", (object?)model.OFFICELIC_TYPE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@REMARKS", (object?)model.REMARKS ?? DBNull.Value);
                            
                            // =========================
                            // DEVICE
                            // =========================
                            cmd.Parameters.AddWithValue("@ASSET_CODE", (object?)device.ASSET_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ASSET_SRNO", (object?)device.ASSET_SRNO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ASSET_CAT", (object?)device.ASSET_CAT ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ASSET_TYPE", (object?)device.ASSET_TYPE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SERIAL_NO", (object?)device.SERIAL_NO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DEVICE_TYPE", (object?)device.DEVICE_TYPE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DEVICE_MODEL", (object?)device.DEVICE_MODEL ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DEVICE_NAME", (object?)device.DEVICE_NAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PURCHASE_DATE", (object?)device.PURCHASE_DATE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PURCHASE_FROM", (object?)device.PURCHASE_FROM ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@WARRANTY_STATUS", (object?)device.WARRANTY_STATUS ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DEVICE_STATUS", (object?)device.DEVICE_STATUS ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@REASON", (object?)device.REASON ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ISSUE_DATE", (object?)device.ISSUE_DATE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RETURN_DATE", (object?)device.RETURN_DATE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@QTY", (object?)device.QTY ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PURPOSE", (object?)device.PURPOSE ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();

                    return new { success = true, message = isUpdate ? "Data updated successfully." : "Data saved successfully." };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    return new { success = false, message = ex.Message};
                }
            }
        }

        private async Task<(bool IsValid, string Message)> ValidateDataAsync(ITInventoryEntryModel model, SqlConnection con,SqlTransaction transaction)
        {
            try
            {
                // ==============================
                // Voucher No Validation
                // ==============================
                if (model.V_NO == null || model.V_NO <= 0)
                {
                    return (false, "Invalid Voucher No. Record not saved.");
                }

                // ==============================
                // Device List Empty Validation
                // ==============================
                if (model.Devices == null || !model.Devices.Any())
                {
                    return (false, "Grid is empty.");
                }

                // ==============================
                // Duplicate Asset Code in Grid
                // ==============================
                var duplicateAsset = model.Devices.Where(x => !string.IsNullOrWhiteSpace(x.ASSET_CODE)).GroupBy(x => x.ASSET_CODE.Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);

                if (duplicateAsset != null)
                {
                    return (false,$"Duplicate asset code found: {duplicateAsset.Key}");
                }

                // ==============================
                // Asset Code Already Exists
                // ==============================
                foreach (var device in model.Devices)
                {
                    if (string.IsNullOrWhiteSpace(device.ASSET_CODE))
                        continue;

                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT TOP 1 V_NO
                    FROM IT_INVENTORY
                    WHERE V_NO <> @V_NO
                      AND ASSET_CODE = @ASSET_CODE
                      AND COMP_CODE = @COMP_CODE ", con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                        cmd.Parameters.AddWithValue("@ASSET_CODE",device.ASSET_CODE.Trim());
                        cmd.Parameters.AddWithValue("@COMP_CODE",_globalVariableService.GetGlobalVariables().PubCompCode);

                        var result = await cmd.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            return (false,$"Asset code => '{device.ASSET_CODE}' already exists in VNo={result}.");
                        }
                    }
                }

                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private string EncodePassword(string? password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            return new string(
                password.Select(c => (char)(255 - c)).ToArray()
            );
        }

        public async Task<object> LoadEditDataAsync(string docId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(docId))
                {
                    return new
                    {
                        success = false,
                        message = "Document Id is required."
                    };
                }

                var globalVariables = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using SqlCommand cmd = new SqlCommand("sp_ITInventoryEntry", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@Action", "LoadEditData");

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    var header = new Dictionary<string, object?>();

                    var devices = new List<Dictionary<string, object?>>();

                    bool firstRow = true;

                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);

                            object? value = null;

                            if (!reader.IsDBNull(i))
                            {
                                value = reader.GetValue(i);

                                // ==============================
                                // DECODE PASSWORD
                                // ==============================

                                if (columnName == "EMAIL_PASS" ||
                                    columnName == "DS_PASSWORD" ||
                                    columnName == "CLOUD_PASSWORD" ||
                                    columnName == "VPN_PASSWORD")
                                {
                                    value = DecodePassword(value.ToString());
                                }
                            }

                            row[columnName] = value;
                        }

                        // ==========================================
                        // FIRST ROW = HEADER
                        // ==========================================

                        if (firstRow)
                        {
                            header = new Dictionary<string, object?>(row);

                            firstRow = false;
                        }

                        // ==========================================
                        // EVERY ROW = DEVICE
                        // ==========================================

                        var device = new Dictionary<string, object?>
                        {
                            ["ASSET_CODE"] = row.ContainsKey("ASSET_CODE") ? row["ASSET_CODE"] : null,
                            ["ASSET_SRNO"] = row.ContainsKey("ASSET_SRNO") ? row["ASSET_SRNO"] : null,
                            ["ASSET_CAT"] = row.ContainsKey("ASSET_CAT") ? row["ASSET_CAT"] : null,
                            ["ASSET_TYPE"] = row.ContainsKey("ASSET_TYPE") ? row["ASSET_TYPE"] : null,
                            ["SERIAL_NO"] = row.ContainsKey("SERIAL_NO") ? row["SERIAL_NO"] : null,
                            ["DEVICE_TYPE"] = row.ContainsKey("DEVICE_TYPE") ? row["DEVICE_TYPE"] : null,
                            ["DEVICE_MODEL"] = row.ContainsKey("DEVICE_MODEL") ? row["DEVICE_MODEL"] : null,
                            ["DEVICE_NAME"] = row.ContainsKey("DEVICE_NAME") ? row["DEVICE_NAME"] : null,
                            ["PURCHASE_DATE"] = row.ContainsKey("PURCHASE_DATE") ? row["PURCHASE_DATE"] : null,
                            ["PURCHASE_FROM"] = row.ContainsKey("PURCHASE_FROM") ? row["PURCHASE_FROM"] : null,
                            ["WARRANTY_STATUS"] = row.ContainsKey("WARRANTY_STATUS") ? row["WARRANTY_STATUS"] : null,
                            ["DEVICE_STATUS"] = row.ContainsKey("DEVICE_STATUS") ? row["DEVICE_STATUS"] : null,
                            ["REASON"] = row.ContainsKey("REASON") ? row["REASON"] : null,
                            ["ISSUE_DATE"] = row.ContainsKey("ISSUE_DATE") ? row["ISSUE_DATE"] : null,
                            ["RETURN_DATE"] = row.ContainsKey("RETURN_DATE") ? row["RETURN_DATE"] : null,
                            ["QTY"] = row.ContainsKey("QTY") ? row["QTY"] : null,
                            ["PURPOSE"] = row.ContainsKey("PURPOSE") ? row["PURPOSE"] : null
                        };

                        devices.Add(device);
                    }

                    return new
                    {
                        success = true,
                        header = header,
                        devices = devices
                    };
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }

        private string DecodePassword(string? password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            return new string(
                password.Select(c => (char)(255 - c)).ToArray()
            );
        }

    }
}
