using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class DispatchPlanningRepository : IDispatchPlanningRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public DispatchPlanningRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
        }

        public async Task<object> SaveAndUpdateDataAsync([FromForm] DispatchPlanningModel model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            string vtype = "DISP";
            string docId = vtype + model.V_NO;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        bool isUpdate = false;

                        // Check existing voucher
                        using (SqlCommand cmdCheck = new SqlCommand("sp_Dispatch_Planning", con, transaction))
                        {
                            cmdCheck.CommandType = CommandType.StoredProcedure;

                            cmdCheck.Parameters.AddWithValue("@Action", "CheckData");
                            cmdCheck.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmdCheck.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmdCheck.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmdCheck.Parameters.AddWithValue("@V_TYPE", vtype);
                            cmdCheck.Parameters.AddWithValue("@V_NO", model.V_NO);

                            isUpdate = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync()) > 0;
                        }

                        // Delete existing voucher header/footer
                        if (isUpdate)
                        {
                            using (SqlCommand cmdDelete = new SqlCommand("sp_Dispatch_Planning", con, transaction))
                            {
                                cmdDelete.CommandType = CommandType.StoredProcedure;

                                cmdDelete.Parameters.AddWithValue("@Action", "DeleteData");
                                cmdDelete.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                cmdDelete.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                cmdDelete.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                cmdDelete.Parameters.AddWithValue("@V_TYPE", vtype);
                                cmdDelete.Parameters.AddWithValue("@V_NO", model.V_NO);

                                await cmdDelete.ExecuteNonQueryAsync();
                            }
                        }

                        // Save Footer Data
                        if (model.FooterList != null && model.FooterList.Count > 0)
                        {
                            foreach (var item in model.FooterList)
                            {
                                using (SqlCommand cmd = new SqlCommand("sp_Dispatch_Planning", con, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@Action", "InsertHeader");

                                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                                    cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                                    cmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                                    cmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);

                                    cmd.Parameters.AddWithValue("@ITEM_CODE", (object?)item.ITEM_CODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@TENACITY_CODE", (object?)item.TENACITY_CODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@STATUS_CODE", (object?)item.STATUS_CODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PLAN_QTY", (object?)item.PLAN_QTY ?? DBNull.Value);

                                    cmd.Parameters.AddWithValue("@SNO", (object?)item.SNO ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PARTY_CODE", (object?)item.PARTY_CODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PARTY_NAME", (object?)item.PARTY_NAME ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@TENACITY", (object?)item.TENACITY ?? DBNull.Value);

                                    cmd.Parameters.AddWithValue("@OPN_QTY", (object?)item.OPN_QTY ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@OPN_WGT", (object?)item.OPN_WGT ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PROD_PLANQTY", (object?)item.PROD_PLANQTY ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DISP_PLANQTY", (object?)item.DISP_PLANQTY ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@APPROX_AMT", (object?)item.APPROX_AMT ?? DBNull.Value);

                                    // Audit
                                    cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                                    cmd.Parameters.AddWithValue("@AED", isUpdate ? "E" : "A");
                                    cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                                    cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        // Save Pending Orders - ALL mode
                        // This block is intentionally outside FooterList IF.
                        if (model.CopyDataMode == "all")
                        {
                            // Delete existing pending-order details
                            string deleteQuery = @"
                            DELETE FROM DISPATCH_PLAN2
                            WHERE COMP_CODE = @COMP_CODE
                              AND BRANCH_CODE = @BRANCH_CODE
                              AND YEAR_CODE = @YEAR_CODE
                              AND V_TYPE = @V_TYPE
                              AND V_NO = @V_NO";

                            using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, con, transaction))
                            {
                                deleteCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                deleteCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                deleteCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                deleteCmd.Parameters.AddWithValue("@V_TYPE", vtype);
                                deleteCmd.Parameters.AddWithValue("@V_NO", model.V_NO);

                                await deleteCmd.ExecuteNonQueryAsync();
                            }

                            // Insert pending-order details
                            if (model.PendingOrderList != null && model.PendingOrderList.Count > 0)
                            {
                                for (int i = 0; i < model.PendingOrderList.Count; i++)
                                {
                                    var item = model.PendingOrderList[i];

                                    string insertQuery = @"
                                    INSERT INTO DISPATCH_PLAN2
                                    (
                                        YEAR_CODE, COMP_CODE, BRANCH_CODE,
                                        V_TYPE, V_NO, V_SNO, V_DATE,
                                        ITEM_CODE, TENACITY_CODE, STATUS_CODE,
                                        DOC_TYPE, DOC_NO, DOC_SNO, DOC_DATE,
                                        AC_CODE, RATE, SALE_QTY, BAL_QTY,
                                        PLAN_QTY, AMOUNT, USERID, WSID
                                    )
                                    VALUES
                                    (
                                        @YEAR_CODE, @COMP_CODE, @BRANCH_CODE,
                                        @V_TYPE, @V_NO, @V_SNO, @V_DATE,
                                        @ITEM_CODE, @TENACITY_CODE, @STATUS_CODE,
                                        @DOC_TYPE, @DOC_NO, @DOC_SNO, @DOC_DATE,
                                        @AC_CODE, @RATE, @SALE_QTY, @BAL_QTY,
                                        @PLAN_QTY, @AMOUNT, @USERID, @WSID
                                    )";

                                    using (SqlCommand cmd = new SqlCommand(insertQuery, con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                                        cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                                        cmd.Parameters.AddWithValue("@V_SNO", (object?)item.V_SNO ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);

                                        cmd.Parameters.AddWithValue("@ITEM_CODE", (object?)item.ITEM_CODE ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@TENACITY_CODE", (object?)item.TENACITY_CODE ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@STATUS_CODE", (object?)item.STATUS_CODE ?? DBNull.Value);

                                        cmd.Parameters.AddWithValue("@DOC_TYPE", (object?)item.DOC_TYPE ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@DOC_NO", (object?)item.DOC_NO ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@DOC_SNO", (object?)item.DOC_SNO ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@DOC_DATE", (object?)item.DOC_DATE ?? DBNull.Value);

                                        cmd.Parameters.AddWithValue("@AC_CODE", (object?)item.PARTY_CODE ?? DBNull.Value);

                                        cmd.Parameters.AddWithValue("@RATE", item.RATE);
                                        cmd.Parameters.AddWithValue("@SALE_QTY", item.SALE_QTY);
                                        cmd.Parameters.AddWithValue("@BAL_QTY", item.BAL_QTY);
                                        cmd.Parameters.AddWithValue("@PLAN_QTY", item.PLAN_QTY);

                                        cmd.Parameters.AddWithValue("@AMOUNT", item.RATE * item.PLAN_QTY);

                                        cmd.Parameters.AddWithValue("@USERID", globalVariable.PubUserId);
                                        cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);

                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            // Delete zero PLAN_QTY records
                            string zeroDeleteQuery = @"
                            DELETE FROM DISPATCH_PLAN2
                            WHERE COMP_CODE = @COMP_CODE
                              AND BRANCH_CODE = @BRANCH_CODE
                              AND YEAR_CODE = @YEAR_CODE
                              AND V_TYPE = @V_TYPE
                              AND V_NO = @V_NO
                              AND PLAN_QTY = 0";

                            using (SqlCommand cmd = new SqlCommand(zeroDeleteQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO);

                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        else if (model.CopyDataMode == "single")
                        {
                            if (model.PendingOrderList != null && model.PendingOrderList.Count > 0)
                            {
                                var firstItem = model.PendingOrderList.First();

                                int itemCode = firstItem.ITEM_CODE ?? 0;
                                int tenacityCode = firstItem.TENACITY_CODE ?? 0;
                                int vSno = firstItem.V_SNO;

                                // Delete existing details for current footer item + tenacity
                                string deleteQuery = @"
                                DELETE FROM DISPATCH_PLAN2
                                WHERE COMP_CODE = @COMP_CODE
                                  AND BRANCH_CODE = @BRANCH_CODE
                                  AND YEAR_CODE = @YEAR_CODE
                                  AND V_TYPE = @V_TYPE
                                  AND V_NO = @V_NO
                                  AND ITEM_CODE = @ITEM_CODE
                                  AND TENACITY_CODE = @TENACITY_CODE";

                                using (SqlCommand cmdDelete = new SqlCommand(deleteQuery, con, transaction))
                                {
                                    cmdDelete.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                    cmdDelete.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                    cmdDelete.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                    cmdDelete.Parameters.AddWithValue("@V_TYPE", vtype);
                                    cmdDelete.Parameters.AddWithValue("@V_NO", model.V_NO);
                                    cmdDelete.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                                    cmdDelete.Parameters.AddWithValue("@TENACITY_CODE", tenacityCode);

                                    await cmdDelete.ExecuteNonQueryAsync();
                                }

                                // Get selected footer Tenacity Type
                                string selectedTenacityType = "";

                                using (SqlCommand cmd = new SqlCommand(@"
                                SELECT ISNULL(TENACITY_TYPE, '')
                                FROM TENACITY_MAST
                                WHERE CODE = @CODE
                                  AND COMP_CODE = @COMP_CODE", con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@CODE", tenacityCode);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                                    selectedTenacityType = Convert.ToString(await cmd.ExecuteScalarAsync() ) ?? "";
                                }

                                decimal approxAmount = 0;

                                foreach (var item in model.PendingOrderList)
                                {
                                    // Get Party Tenacity Type
                                    string partyTenacityType = "";

                                    using (SqlCommand cmd = new SqlCommand(@"
                                    SELECT ISNULL(TENACITY_TYPE, '')
                                    FROM SUBGROUP_MAST
                                    WHERE CODE = @CODE
                                      AND COMP_CODE = @COMP_CODE", con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@CODE", item.PARTY_CODE);
                                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                                        partyTenacityType = Convert.ToString( await cmd.ExecuteScalarAsync() ) ?? "";
                                    }

                                    // VB condition:
                                    // Selected Tenacity Type = Party Tenacity Type
                                    // OR Party Tenacity Type is blank

                                    if (!string.IsNullOrWhiteSpace(partyTenacityType) && !string.Equals(selectedTenacityType.Trim(), partyTenacityType.Trim(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        continue;
                                    }

                                    string insertQuery = @"
                                    INSERT INTO DISPATCH_PLAN2
                                    (
                                        YEAR_CODE, COMP_CODE, BRANCH_CODE,
                                        V_TYPE, V_NO, V_SNO, V_DATE,
                                        ITEM_CODE, TENACITY_CODE, STATUS_CODE,
                                        DOC_TYPE, DOC_NO, DOC_SNO, DOC_DATE,
                                        AC_CODE, RATE, SALE_QTY, BAL_QTY,
                                        PLAN_QTY, AMOUNT, USERID, WSID
                                    )
                                    VALUES
                                    (
                                        @YEAR_CODE, @COMP_CODE, @BRANCH_CODE,
                                        @V_TYPE, @V_NO, @V_SNO, @V_DATE,
                                        @ITEM_CODE, @TENACITY_CODE, @STATUS_CODE,
                                        @DOC_TYPE, @DOC_NO, @DOC_SNO, @DOC_DATE,
                                        @AC_CODE, @RATE, @SALE_QTY, @BAL_QTY,
                                        @PLAN_QTY, @AMOUNT, @USERID, @WSID
                                    )";

                                    using (SqlCommand cmd = new SqlCommand(insertQuery, con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                                        cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                                        cmd.Parameters.AddWithValue("@V_SNO", vSno);
                                        cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);

                                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                                        cmd.Parameters.AddWithValue("@TENACITY_CODE", tenacityCode);
                                        cmd.Parameters.AddWithValue("@STATUS_CODE", (object?)firstItem.STATUS_CODE ?? DBNull.Value);

                                        cmd.Parameters.AddWithValue("@DOC_TYPE", (object?)item.DOC_TYPE ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@DOC_NO", (object?)item.DOC_NO ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@DOC_SNO", item.DOC_SNO);
                                        cmd.Parameters.AddWithValue("@DOC_DATE", (object?)item.DOC_DATE ?? DBNull.Value);

                                        cmd.Parameters.AddWithValue("@AC_CODE", item.PARTY_CODE);

                                        cmd.Parameters.AddWithValue("@RATE", item.RATE);
                                        cmd.Parameters.AddWithValue("@SALE_QTY", item.SALE_QTY);
                                        cmd.Parameters.AddWithValue("@BAL_QTY", item.BAL_QTY);
                                        cmd.Parameters.AddWithValue("@PLAN_QTY", item.PLAN_QTY);

                                        decimal amount = item.RATE * item.PLAN_QTY;

                                        cmd.Parameters.AddWithValue("@AMOUNT", amount);

                                        cmd.Parameters.AddWithValue("@USERID", globalVariable.PubUserId);
                                        cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);

                                        await cmd.ExecuteNonQueryAsync();

                                        approxAmount += amount;
                                    }
                                }

                                // Update footer approximate amount with matching orders only
                                using (SqlCommand cmd = new SqlCommand(@"
                                UPDATE DISPATCH_PLAN
                                SET APPROX_AMT = @APPROX_AMT
                                WHERE COMP_CODE = @COMP_CODE
                                  AND BRANCH_CODE = @BRANCH_CODE
                                  AND YEAR_CODE = @YEAR_CODE
                                  AND V_TYPE = @V_TYPE
                                  AND V_NO = @V_NO
                                  AND SNO = @V_SNO", con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@APPROX_AMT", approxAmount);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                                    cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                                    cmd.Parameters.AddWithValue("@V_SNO", vSno);

                                    await cmd.ExecuteNonQueryAsync();
                                }

                                // Delete zero quantity records
                                using (SqlCommand cmd = new SqlCommand(@"
                                DELETE FROM DISPATCH_PLAN2
                                WHERE COMP_CODE = @COMP_CODE
                                  AND BRANCH_CODE = @BRANCH_CODE
                                  AND YEAR_CODE = @YEAR_CODE
                                  AND V_TYPE = @V_TYPE
                                  AND V_NO = @V_NO
                                  AND V_SNO = @V_SNO
                                  AND PLAN_QTY = 0", con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                                    cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                                    cmd.Parameters.AddWithValue("@V_SNO", vSno);

                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        // ================= Total Plan Quantity Validation =================

                        decimal sumQty = 0;

                        if (model.FooterList != null)
                        {
                            sumQty = model.FooterList.Where(x => (x.ITEM_CODE ?? 0) > 0).Sum(x => Convert.ToDecimal(x.PLAN_QTY));
                        }

                        // Get total Plan Qty from DISPATCH_PLAN2
                        decimal dbSumQty = 0;

                        using (SqlCommand cmd = new SqlCommand(@"
                            SELECT ISNULL(SUM(PLAN_QTY), 0)
                            FROM DISPATCH_PLAN2
                            WHERE V_TYPE = @V_TYPE
                              AND V_NO = @V_NO
                              AND COMP_CODE = @COMP_CODE
                              AND BRANCH_CODE = @BRANCH_CODE
                              AND YEAR_CODE = @YEAR_CODE
                        ", con, transaction))
                        {
                            cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                            cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                            dbSumQty = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                        }

                        // Compare both quantities
                        if (dbSumQty != sumQty)
                        {
                            await transaction.RollbackAsync();

                            return new {  success = false, message = "Please check! Total Plan Qty not matched with Item wise Plan Qty."  };
                        }

                        await transaction.CommitAsync();

                        return new {success = true, message = isUpdate ? "Data updated successfully." : "Data saved successfully." };
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        return new { success = false, message = ex.Message };
                    }
                }
            }
        }

        public async Task<object> LoadEditDataAsync(int vNo, string vType)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_Dispatch_Planning", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "LoadDataOnEdit");
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        var dataList = new List<Dictionary<string, object?>>();

                        while (await reader.ReadAsync())
                        {
                            var data = new Dictionary<string, object?>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                data[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                            }

                            dataList.Add(data);
                        }

                        return dataList;
                    }
                }
            }
        }


    }
}
