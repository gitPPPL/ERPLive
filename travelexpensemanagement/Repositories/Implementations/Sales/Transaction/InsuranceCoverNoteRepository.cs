using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Metadata;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class InsuranceCoverNoteRepository : IInsuranceCoverNoteRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public InsuranceCoverNoteRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _logService = logService;
        }

        public async Task<object> SaveAndUpdateDataAsync(InsuranceCoverNoteModel model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            string vtype = "INSU";
            string docId = vtype + model.VNo;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                try
                {
                    var validationMessage = await ValidateInsuranceCoverNoteAsync(model, con, globalVariable);

                    if (validationMessage != null)
                    {
                        return new {success = false, message = validationMessage};
                    }

                    bool isUpdate;

                    // ==========================================
                    // CHECK EXISTING RECORD
                    // ==========================================
                    using (SqlCommand checkCmd = new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM SALE3
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
                        checkCmd.Parameters.AddWithValue("@V_NO", (object?)model.VNo ?? DBNull.Value);

                        isUpdate = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    }

                    // ==========================================
                    // CLEAR OLD INSURANCE MAPPING
                    // ==========================================
                    using (SqlCommand clearCmd = new SqlCommand(@"
                    UPDATE SALE1
                    SET INSU_NO = NULL
                    WHERE COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE
                      AND INSU_NO = @V_NO", con))
                    {
                        clearCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        clearCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        clearCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        clearCmd.Parameters.AddWithValue("@V_NO", (object?)model.VNo ?? DBNull.Value);

                        await clearCmd.ExecuteNonQueryAsync();
                    }

                    // ==========================================
                    // GET ITEM NAME FROM SALE2
                    // ==========================================
                    string? itemName = null;

                    using (SqlCommand itemCmd = new SqlCommand(@"
                    SELECT TOP 1 LEFT(ITEM_NAME, 50)
                    FROM SALE2
                    WHERE V_TYPE = @BILL_TYPE
                      AND V_NO BETWEEN @F_BILL_NO AND @T_BILL_NO
                      AND COMP_CODE = @COMP_CODE", con))
                    {
                        itemCmd.Parameters.AddWithValue("@BILL_TYPE", (object?)model.BillType ?? DBNull.Value);
                        itemCmd.Parameters.AddWithValue("@F_BILL_NO", (object?)model.FBillNo ?? DBNull.Value);
                        itemCmd.Parameters.AddWithValue("@T_BILL_NO", (object?)model.TBillNo ?? DBNull.Value);
                        itemCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                        var result = await itemCmd.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            itemName = result.ToString();
                        }
                    }

                    // ==========================================
                    // SAVE / UPDATE SALE3
                    // ==========================================
                    using (SqlCommand cmd = new SqlCommand("sp_Sale3", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action",isUpdate ? "Update" : "Add");

                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        cmd.Parameters.AddWithValue("@INSU_CODE", (object?)model.InsuCode ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                        cmd.Parameters.AddWithValue("@V_NO", (object?)model.VNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_DATE", (object?)model.VDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_TYPE", (object?)model.BillType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@F_BILL_NO", (object?)model.FBillNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@T_BILL_NO", (object?)model.TBillNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@USER", globalVariable.PubUserId);
                        cmd.Parameters.AddWithValue("@SRNO", (object?)model.Srno ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ITEM_NAME", (object?)itemName ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // ==========================================
                    // ASSIGN INSURANCE NO TO SELECTED BILLS
                    // ==========================================
                    using (SqlCommand updateCmd = new SqlCommand(@"
                    UPDATE SALE1
                    SET INSU_NO = @V_NO
                    WHERE COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE
                      AND V_NO BETWEEN @F_BILL_NO AND @T_BILL_NO
                      AND V_TYPE = @BILL_TYPE", con))
                    {
                        updateCmd.Parameters.AddWithValue("@V_NO", model.VNo);
                        updateCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        updateCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        updateCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        updateCmd.Parameters.AddWithValue("@F_BILL_NO", model.FBillNo);
                        updateCmd.Parameters.AddWithValue("@T_BILL_NO", model.TBillNo);
                        updateCmd.Parameters.AddWithValue("@BILL_TYPE", model.BillType);

                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    return new {success = true, message = isUpdate ? "Data updated successfully." : "Data saved successfully." };
                }
                catch (Exception ex)
                {
                    return new { success = false, message = ex.Message };
                }
            }
        }

        public async Task<object> LoadEditDataAsync(int vNo, string vType)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_Sale3", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "LoadEditData");
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var data = new Dictionary<string, object?>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                data[reader.GetName(i)] =await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                            }

                            return data;
                        }
                    }
                }
            }

            return new { success = false, message = "Record not found." };
        }

        private async Task<string?> ValidateInsuranceCoverNoteAsync(InsuranceCoverNoteModel model, SqlConnection con,dynamic globalVariable)
        {
          
            if (model.VDate == null || string.IsNullOrWhiteSpace(model.VDate.ToString()))
            {
                return "Voucher Date can't blank";
            }

            if (Convert.ToInt32(model.TBillNo) < Convert.ToInt32(model.FBillNo))
            {
                return "To Bill No. Can't be less than From bill no.";
            }

            // Existing record validation
            using (SqlCommand cmd = new SqlCommand(@"
            SELECT COUNT(1)
            FROM SALE3
            WHERE F_BILL_NO >= @F_BILL_NO
              AND T_BILL_NO <= @T_BILL_NO
              AND V_TYPE = 'INSU'
              AND BILL_TYPE = @BILL_TYPE
              AND COMP_CODE = @COMP_CODE
              AND BRANCH_CODE = @BRANCH_CODE
              AND YEAR_CODE = @YEAR_CODE
              AND V_NO <> @V_NO", con))
            {
                cmd.Parameters.AddWithValue("@F_BILL_NO", Convert.ToInt32(model.FBillNo));
                cmd.Parameters.AddWithValue("@T_BILL_NO", Convert.ToInt32(model.TBillNo));
                cmd.Parameters.AddWithValue("@BILL_TYPE", (object?)model.BillType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                cmd.Parameters.AddWithValue("@V_NO", model.VNo);

                int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                if (count > 0)
                {
                    return "From bill no. or to bill no. already exist";
                }
            }

            return null;
        }

    }

}
