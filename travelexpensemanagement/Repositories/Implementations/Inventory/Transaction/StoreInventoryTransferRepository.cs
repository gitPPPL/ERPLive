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
    public class StoreInventoryTransferRepository : IStoreInventoryTransferRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;

        public StoreInventoryTransferRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;

        }

        public async Task<object> SaveAndUpdateDataAsync(StoreInventoryTransferModel model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            var docId = model.V_TYPE + model.V_NO;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (SqlTransaction tran = con.BeginTransaction())
                {
                    try
                    {
                        bool isUpdate;

                        using (SqlCommand checkCmd = new SqlCommand(@"
	                    SELECT COUNT(1)
	                    FROM ISSUE1
	                    WHERE YEAR_CODE = @YEAR_CODE
	                      AND COMP_CODE = @COMP_CODE
	                      AND BRANCH_CODE = @BRANCH_CODE
	                      AND DOC_ID = @DOC_ID", con, tran))
                        {
                            checkCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            checkCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            checkCmd.Parameters.AddWithValue("@DOC_ID", docId);
                            isUpdate = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                        }

                        using (SqlCommand cmd = new SqlCommand("sp_StoreInvetoryTransfer", con, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Action", isUpdate ? "UpdateHeader" : "InsertHeader");

                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", (object?)model.V_TYPE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DOC_ID", docId);
                            cmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIFT", (object?)model.SHIFT ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SLIP_NO", (object?)model.SLIP_NO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DEPT_CODE", (object?)model.DEPT_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@STATUS", (object?)model.STATUS ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@REMARKS", (object?)model.REMARKS ?? DBNull.Value);
                            if (isUpdate)
                            {
                                cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            }
                            cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                            cmd.ExecuteNonQuery();
                        }

                        if (isUpdate)
                        {
                            // ---------------------------------------------
                            // Delete old footer
                            // ---------------------------------------------
                            using (SqlCommand cmd = new SqlCommand("sp_StoreInvetoryTransfer", con, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@Action", "DeleteFooter");
                                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                cmd.Parameters.AddWithValue("@DOC_ID", docId);
                                //cmd.ExecuteNonQuery();
                                var deletedRows = Convert.ToInt32(cmd.ExecuteScalar());

                                Console.WriteLine("Deleted Footer Rows: " + deletedRows);

                            }
                        }

                        if (model.StoreInventoryTransferFooter != null && model.StoreInventoryTransferFooter.Count > 0)
                        {
                            foreach (var item in model.StoreInventoryTransferFooter)
                            {
                                using (SqlCommand cmd = new SqlCommand("sp_StoreInvetoryTransfer", con, tran))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@Action", "InsertFooter");

                                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@V_TYPE", (object?)model.V_TYPE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                                    cmd.Parameters.AddWithValue("@SHIFT", (object?)model.SHIFT ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ITEM_CODE", (object?)item.ITEM_CODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ITEM_NAME", (object?)item.ITEM_NAME ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@MAKE_CODE", (object?)item.MAKE_CODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@UOM_CODE", (object?)item.UOM_CODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@UOM_NAME", (object?)item.UOM_NAME ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@FROM_DEPT", (object?)item.FROM_DEPT ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@TO_DEPT", (object?)item.TO_DEPT ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@NOS", (object?)item.NOS ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@QTY", (object?)item.QTY ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LAND_RATE", (object?)item.LAND_RATE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LAND_AMT", (object?)item.LAND_AMT ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@KANTA_TYPE", (object?)item.KANTA_TYPE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@KANTA_NO", (object?)item.KANTA_NO ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@REQ_TYPE", (object?)item.REQ_TYPE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@REQ_NO", (object?)item.REQ_NO ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PORD_TYPE", (object?)item.PORD_TYPE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PORD_NO", (object?)item.PORD_NO ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@BIN_LOCATION", (object?)item.BIN_LOCATION ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@FREMARKS", (object?)item.FREMARKS ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@WB_DATETIME", (object?)item.WB_DATETIME ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                                    cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                                    cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                        tran.Commit();

                        return new { success = true, message = isUpdate ? "Data updated successfully." : "Data saved successfully." };
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return new { success = false, message = ex.Message };
                    }
                }

            }
        }

        public async Task<object> LoadEditDataAsync(string docId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(docId))
                {
                    return new { success = false, message = "Document Id is required." };
                }

                var globalVariables = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using SqlCommand cmd = new SqlCommand("sp_StoreInvetoryTransfer", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@Action", "LoadEditData");

                    using SqlDataReader reader = cmd.ExecuteReader();

                    // =========================
                    // HEADER
                    // =========================

                    var header = new Dictionary<string, object?>();

                    if (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            header[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                    }

                    // =========================
                    // FOOTER
                    // =========================

                    var footer = new List<Dictionary<string, object?>>();

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object?>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }

                            footer.Add(row);
                        }
                    }

                    return new { success = true, header = header, footer = footer };
                }
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        public async Task<object> GetLinkDataAsync(DateTime vDate)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                using SqlConnection con = _dbConnection.GetErpConnection();
                await con.OpenAsync();

                string query = @"
                    SELECT 
                        ISNULL(a.ITEM_CODE, '') AS ITEM_CODE,
                        b.NAME AS ITEM_NAME,
                        b.UNIT_NAME AS UOM_NAME,
                        SUM(ISNULL(a.NET_WGT, 0)) AS QTY,
                        c.NAME AS FROM_DEPTNAME,
                        d.NAME AS TO_DEPTNAME,
                        ISNULL(a.FROM_PLACE, 0) AS FROM_DEPT,
                        ISNULL(a.TO_PLACE, 0) AS TO_DEPT,
                        b.UNIT_CODE AS UOM_CODE
                    FROM WB2 a

                    LEFT JOIN ITEM_MAST b
                        ON a.ITEM_CODE = b.CODE
                        AND b.COMP_CODE = @COMP_CODE

                    LEFT JOIN ITEMDEPT_MAST c
                        ON a.FROM_PLACE = c.CODE
                        AND c.COMP_CODE = @COMP_CODE

                    LEFT JOIN ITEMDEPT_MAST d
                        ON a.TO_PLACE = d.CODE
                        AND d.COMP_CODE = @COMP_CODE

                    LEFT JOIN ITEM_MGROUP
                        ON ITEM_MGROUP.CODE = b.MGROUP_CODE
                        AND ITEM_MGROUP.COMP_CODE = @COMP_CODE

                    WHERE a.V_TYPE IN ('KINH', 'KANT')
                        AND a.TO_PLACE NOT IN (16)
                        AND a.FROM_PLACE IN (16)
                        AND ITEM_MGROUP.REPORT_TYPE IN ('Fuel')

                        AND a.WGT_DATE >= DATEADD(
                            HOUR, 8,
                            CAST(CAST(@V_DATE AS DATE) AS DATETIME)
                        )

                        AND a.WGT_DATE < DATEADD(
                            HOUR, 8,
                            DATEADD(DAY, 1, CAST(CAST(@V_DATE AS DATE) AS DATETIME))
                        )

                        AND a.COMP_CODE = @COMP_CODE
                        AND a.BRANCH_CODE = @BRANCH_CODE
                        AND a.YEAR_CODE = @YEAR_CODE

                    GROUP BY
                        ISNULL(a.ITEM_CODE, ''),
                        a.FROM_PLACE,
                        a.TO_PLACE,
                        b.NAME,
                        b.UNIT_NAME,
                        c.NAME,
                        d.NAME,
                        b.UNIT_CODE,
                        b.MGROUP_CODE

                    HAVING SUM(ISNULL(a.NET_WGT, 0)) > 0

                    ORDER BY
                        d.NAME,
                        c.NAME,
                        b.NAME";

                using SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                cmd.Parameters.AddWithValue("@V_DATE", vDate.Date);

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        itemCode = reader["ITEM_CODE"] == DBNull.Value ? "" : reader["ITEM_CODE"].ToString(),
                        itemName = reader["ITEM_NAME"] == DBNull.Value ? "" : reader["ITEM_NAME"].ToString(),
                        uomName = reader["UOM_NAME"] == DBNull.Value ? "" : reader["UOM_NAME"].ToString(),
                        qty = reader["QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QTY"]),
                        fromDeptName = reader["FROM_DEPTNAME"] == DBNull.Value ? "" : reader["FROM_DEPTNAME"].ToString(),
                        toDeptName = reader["TO_DEPTNAME"] == DBNull.Value ? "" : reader["TO_DEPTNAME"].ToString(),
                        fromDept = reader["FROM_DEPT"] == DBNull.Value ? 0 : Convert.ToInt32(reader["FROM_DEPT"]),
                        toDept = reader["TO_DEPT"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TO_DEPT"]),
                        uomCode = reader["UOM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["UOM_CODE"])
                    });
                }

                return new { success = true, data = list };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

    }
}
