using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventroy.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Inventory.Transaction
{
    public class InventoryConsumptionEntryRepository : IInventoryConsumptionEntryRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;

        public InventoryConsumptionEntryRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _dropdownService = dropdownService;
            _logService = logService;
        }

        public async Task<object> SaveAndUpdateDataAsync(InventoryConsumptionEntryModel model)
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
                        if (!ValidateConsumptionStock(con, tran, model, out string validationMessage))
                        {
                            tran.Rollback();
                            return new { success = false, message = validationMessage };
                        }

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

                        using (SqlCommand cmd = new SqlCommand("sp_ConsumptionEntry", con, tran))
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
                            cmd.Parameters.AddWithValue("@PLACE_CODE", (object?)model.PLACE_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EMP_CODE", (object?)model.EMP_CODE ?? DBNull.Value);
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
                            using (SqlCommand cmd = new SqlCommand("sp_ConsumptionEntry", con, tran))
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

                        if (model.InventryConsumptionFooter != null && model.InventryConsumptionFooter.Count > 0)
                        {
                            foreach (var item in model.InventryConsumptionFooter)
                            {
                                using (SqlCommand cmd = new SqlCommand("sp_ConsumptionEntry", con, tran))
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
                                    cmd.Parameters.AddWithValue("@TO_DEPT", (object?)item.TO_DEPT ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@NOS", (object?)item.NOS ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@QTY", (object?)item.QTY ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@RATE", (object?)item.RATE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AMOUNT", (object?)item.AMOUNT ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LAND_RATE", (object?)item.LAND_RATE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LAND_AMT", (object?)item.LAND_AMT ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@KANTA_TYPE", (object?)item.KANTA_TYPE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@KANTA_NO", (object?)item.KANTA_NO ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@REQ_TYPE", (object?)item.REQ_TYPE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@REQ_NO", (object?)item.REQ_NO ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@MACH_CODE", (object?)item.MACH_CODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@FREMARKS", (object?)item.FREMARKS ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@COSTCAT_CODE", (object?)item.COSTCAT_CODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@COSTSCAT_CODE", (object?)item.COSTSCAT_CODE ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@COSTCENTER_CODE", (object?)item.COSTCENTER_CODE ?? DBNull.Value);
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

        private bool ValidateConsumptionStock(SqlConnection con, SqlTransaction transaction, InventoryConsumptionEntryModel model, out string message)
        {
            message = "";

            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                foreach (var item in model.InventryConsumptionFooter)
                {
                    if (!item.ITEM_CODE.HasValue || item.ITEM_CODE.Value <= 0)
                        continue;

                    int itemCode = item.ITEM_CODE.Value;

                    if (model.V_TYPE == "SICO")
                    {
                        int requestNo = item.REQ_NO ?? 0;

                        if (requestNo == 0)
                        {
                            // VB:
                            // If pubUserLevel <> 1 Then Return False

                            if (globalVariables.PubUserLevel != "1")
                            {
                                message =
                                    $"Request Number missing in Row number=>{model.InventryConsumptionFooter.IndexOf(item) + 1}, " +
                                    $"for item=>{item.ITEM_NAME}";

                                return false;
                            }
                        }
                    }

                    // ==========================================
                    // Current Stock
                    // ==========================================

                    decimal currentStock = 0;

                    string stockQuery = @"
                        SELECT ISNULL(SUM(stock_qty), 0)
                        FROM
                        (
                            SELECT SUM(purchase2.recd_qty) AS stock_qty
                            FROM purchase2
                            LEFT JOIN doctype_mast 
                                ON doctype_mast.code = purchase2.v_type
                            WHERE purchase2.comp_code = @COMP_CODE
                              AND purchase2.branch_code = @BRANCH_CODE
                              AND purchase2.v_date <= @V_DATE
                              AND purchase2.item_code = @ITEM_CODE
                              AND doctype_mast.doctype IN ('MaterialReceipt','JobReceived')

                            UNION ALL

                            SELECT -SUM(purchase2.recd_qty)
                            FROM purchase2
                            LEFT JOIN doctype_mast 
                                ON doctype_mast.code = purchase2.v_type
                            WHERE purchase2.comp_code = @COMP_CODE
                              AND purchase2.branch_code = @BRANCH_CODE
                              AND purchase2.v_date <= @V_DATE
                              AND purchase2.item_code = @ITEM_CODE
                              AND doctype_mast.doctype IN ('PurchaseReturn','JobIssue')

                            UNION ALL

                            SELECT SUM(issue2.qty)
                            FROM issue2
                            LEFT JOIN doctype_mast 
                                ON doctype_mast.code = issue2.v_type
                            WHERE issue2.comp_code = @COMP_CODE
                              AND issue2.branch_code = @BRANCH_CODE
                              AND issue2.v_date <= @V_DATE
                              AND issue2.item_code = @ITEM_CODE
                              AND doctype_mast.doctype IN
                                  ('PlantReturn','OpeningStock','ProductionReceived','AdjustmentReceived')

                            UNION ALL

                            SELECT -SUM(issue2.qty)
                            FROM issue2
                            LEFT JOIN doctype_mast 
                                ON doctype_mast.code = issue2.v_type
                            WHERE issue2.comp_code = @COMP_CODE
                              AND issue2.branch_code = @BRANCH_CODE
                              AND issue2.v_date <= @V_DATE
                              AND issue2.item_code = @ITEM_CODE
                              AND doctype_mast.doctype IN
                                  ('GoodsIssue','ProductionIssue','AdjustmentIssue',
                                   'DispatchIssue','MoistureIssue')
                              AND issue2.v_no NOT IN (@V_NO)

                            UNION ALL

                            SELECT -SUM(sale2.qty)
                            FROM sale2
                            LEFT JOIN doctype_mast 
                                ON doctype_mast.code = sale2.v_type
                            WHERE sale2.comp_code = @COMP_CODE
                              AND sale2.branch_code = @BRANCH_CODE
                              AND sale2.v_date <= @V_DATE
                              AND sale2.item_code = @ITEM_CODE
                              AND doctype_mast.doctype IN ('SalesInvoice','JobIssue')
                              AND sale2.status <> 2

                            UNION ALL

                            SELECT SUM(sale2.qty)
                            FROM sale2
                            LEFT JOIN doctype_mast 
                                ON doctype_mast.code = sale2.v_type
                            WHERE sale2.comp_code = @COMP_CODE
                              AND sale2.branch_code = @BRANCH_CODE
                              AND sale2.v_date <= @V_DATE
                              AND sale2.item_code = @ITEM_CODE
                              AND doctype_mast.doctype IN ('SalesReturn')
                        ) X";

                    using (SqlCommand cmd = new SqlCommand(stockQuery, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? 0);

                        var result = cmd.ExecuteScalar();

                        if (result != DBNull.Value && result != null)
                        {
                            currentStock = Convert.ToDecimal(result);
                        }
                    }

                    currentStock = Math.Round(currentStock, 2);

                    decimal issueQty = item.QTY ?? 0;

                    // ==========================================
                    // Stock Validation
                    // ==========================================

                    if ((currentStock - issueQty) < 0)
                    {
                        //message = $"Current Stock = ({currentStock}) is less than issue qty. " + $"Please check for item name {item.ITEM_NAME}({itemCode})";
                        message =
                        $"Item: {item.ITEM_NAME} ({itemCode}), " +
                        $"Current Stock: {currentStock}, " +
                        $"Issue Qty: {issueQty}";

                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                message = "Stock validation failed: " + ex.Message;
                return false;
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

                    using SqlCommand cmd = new SqlCommand("sp_ConsumptionEntry", con);
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

        public async Task<object> GetCapitalItemDataAsync(DateTime vDate)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    string query = @"
                    SELECT
                        DOC_ID,
                        V_DATE AS [Date],
                        ITEM_NAME AS [ItemName],
                        NET_WGT AS [Qty],
                        TO_NAME AS [ToPlace],
                        WEIGHT AS [GrossWt],
                        TARE_WGT AS [TareWt],
                        Remarks
                    FROM WB2
                    WHERE V_TYPE = 'KSOT'
                      AND COMP_CODE = @CompCode
                      AND BRANCH_CODE = @BranchCode
                      AND YEAR_CODE = @YearCode
                      AND V_DATE = @VDate
                    ORDER BY V_DATE DESC, V_NO DESC";

                    using SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@CompCode", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", globalVariables.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YearCode", globalVariables.PubFYearCode);
                    cmd.Parameters.AddWithValue("@VDate", vDate.Date);

                    using SqlDataReader reader = cmd.ExecuteReader();

                    var result = new List<object>();

                    while (reader.Read())
                    {
                        result.Add(new
                        {
                            docId = reader["DOC_ID"]?.ToString(),
                            date = reader["Date"] == DBNull.Value ? "" : Convert.ToDateTime(reader["Date"]).ToString("dd-MM-yyyy"),
                            itemName = reader["ItemName"]?.ToString(),
                            qty = reader["Qty"] == DBNull.Value ? "" : reader["Qty"],
                            toPlace = reader["ToPlace"]?.ToString(),
                            grossWt = reader["GrossWt"] == DBNull.Value ? "" : reader["GrossWt"],
                            tareWt = reader["TareWt"] == DBNull.Value ? "" : reader["TareWt"],
                            remarks = reader["Remarks"]?.ToString()
                        });
                    }

                    return (result);
                }
            }
            catch (Exception ex)
            {
                return  new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }

        public async Task<object> GetCopyFromDataAsync(string vType, int placeCode,DateTime vDate)
        {
            try
            {
                if (vType == "BFIS")
                {
                    return new { success = true, data = new List<object>() };
                }

                if (placeCode <= 0)
                {
                    return new { success = false, message = "Please select Place first." };
                }

                var globalVariables = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    string query = @"
                        SELECT 
                            a.DOC_ID AS ReqID,
                            a.ITEM_CODE AS ICode,
                            a.ITEM_NAME AS ItemName,
                            c.UNIT_NAME AS Unit,
                            a.Nos,
                            a.Qty,
                            d.NAME AS Department,
                            e.NAME AS Machine,
                            a.TO_DEPT,
                            a.MACH_CODE,
                            a.MAKE_CODE,
                            a.V_TYPE AS ReqType,
                            a.V_NO AS ReqNo,
                            c.UNIT_CODE AS Ucode,
                            m.NAME AS Make,
                            a.Remarks,
                            i.Remarks AS Remarks2,
                            i.EMP_CODE,
                            f.NAME AS empname,
                            i.Place_code,
                            c1.Name AS CostCat,
                            c2.Name AS CostsubCat,
                            c3.Name AS Costcenter,
                            c.COSTCAT_CODE,
                            c.COSTSCAT_CODE,
                            c.COSTCENTER_CODE
                        FROM ISSUE2 a

                        LEFT JOIN ISSUE1 i 
                            ON a.V_TYPE = i.V_TYPE
                            AND a.V_NO = i.V_NO
                            AND a.COMP_CODE = i.COMP_CODE
                            AND a.BRANCH_CODE = i.BRANCH_CODE
                            AND a.YEAR_CODE = i.YEAR_CODE

                        LEFT JOIN ISSUE2 b 
                            ON a.ITEM_CODE = b.ITEM_CODE
                            AND a.V_TYPE = b.REQ_TYPE
                            AND a.V_NO = b.REQ_NO
                            AND a.COMP_CODE = b.COMP_CODE
                            AND a.BRANCH_CODE = b.BRANCH_CODE
                            AND b.V_TYPE = 'SICO'

                        LEFT JOIN ITEM_MAST c 
                            ON a.ITEM_CODE = c.CODE
                            AND a.COMP_CODE = c.COMP_CODE

                        LEFT JOIN ITEMMAKE_MAST m 
                            ON a.MAKE_CODE = m.CODE
                            AND a.COMP_CODE = m.COMP_CODE

                        LEFT JOIN ITEMDEPT_MAST d 
                            ON a.TO_DEPT = d.CODE
                            AND a.COMP_CODE = d.COMP_CODE
                            AND d.TRAN_TYPE = 'STORE'

                        LEFT JOIN MACHINE_MAST e 
                            ON a.MACH_CODE = e.CODE
                            AND a.COMP_CODE = e.COMP_CODE

                        LEFT JOIN EMP_MAST f 
                            ON i.EMP_CODE = f.CODE
                            AND i.COMP_CODE = f.COMP_CODE

                        LEFT JOIN CostCat_Mast c1 
                            ON c.COSTCAT_CODE = c1.CODE
                            AND c.COMP_CODE = c1.COMP_CODE

                        LEFT JOIN CostSubCat_Mast c2 
                            ON c.COSTSCAT_CODE = c2.CODE
                            AND c.COMP_CODE = c2.COMP_CODE

                        LEFT JOIN COSTCENTER_MAST c3 
                            ON c.COSTCENTER_CODE = c3.CODE
                            AND c.COMP_CODE = c3.COMP_CODE

                        WHERE a.V_TYPE = 'IRST'
                            AND ISNULL(b.REQ_NO, 0) = 0
                            AND CAST(a.V_DATE AS DATE) = @V_DATE
                            AND i.STATUS = 1
                            AND a.COMP_CODE = @COMP_CODE
                            AND a.BRANCH_CODE = @BRANCH_CODE
                            AND a.YEAR_CODE = @YEAR_CODE

                        UNION ALL
          
                        SELECT 
                            a.DOC_ID AS ReqID,
                            a.ITEM_CODE AS ICode,
                            a.ITEM_NAME AS ItemName,
                            c.UNIT_NAME AS Unit,
                            a.NOS,
                            a.QTY,
                            d.NAME AS Department,
                            e.NAME AS Machine,
                            a.TO_DEPT,
                            a.MACH_CODE,
                            a.MAKE_CODE,
                            a.V_TYPE AS ReqType,
                            a.V_NO AS ReqNo,
                            c.UNIT_CODE AS Ucode,
                            m.NAME AS Make,
                            a.Remarks,
                            i.Remarks AS Remarks2,
                            i.EMP_CODE,
                            f.NAME AS empname,
                            i.Place_code,
                            c1.Name AS CostCat,
                            c2.Name AS CostsubCat,
                            c3.Name AS Costcenter,
                            c.COSTCAT_CODE,
                            c.COSTSCAT_CODE,
                            c.COSTCENTER_CODE
                        FROM TRF_REQUEST2 a

                        LEFT JOIN TRF_REQUEST2 b 
                            ON a.ITEM_CODE = b.ITEM_CODE
                            AND a.V_TYPE = b.REQ_TYPE
                            AND a.V_NO = b.REQ_NO
                            AND a.COMP_CODE = b.COMP_CODE
                            AND a.BRANCH_CODE = b.BRANCH_CODE
                            AND b.V_TYPE = 'SICO'

                        LEFT JOIN TRF_REQUEST1 i 
                            ON a.V_TYPE = i.V_TYPE
                            AND a.V_NO = i.V_NO
                            AND a.COMP_CODE = i.COMP_CODE
                            AND a.BRANCH_CODE = i.BRANCH_CODE
                            AND a.YEAR_CODE = i.YEAR_CODE

                        LEFT JOIN ITEM_MAST c 
                            ON a.ITEM_CODE = c.CODE
                            AND a.COMP_CODE = c.COMP_CODE

                        LEFT JOIN ITEMMAKE_MAST m 
                            ON a.MAKE_CODE = m.CODE
                            AND a.COMP_CODE = m.COMP_CODE

                        LEFT JOIN ITEMDEPT_MAST d 
                            ON a.TO_DEPT = d.CODE
                            AND a.COMP_CODE = d.COMP_CODE
                            AND d.TRAN_TYPE = 'STORE'

                        LEFT JOIN MACHINE_MAST e 
                            ON a.MACH_CODE = e.CODE
                            AND a.COMP_CODE = e.COMP_CODE

                        LEFT JOIN EMP_MAST f 
                            ON i.EMP_CODE = f.CODE
                            AND i.COMP_CODE = f.COMP_CODE

                        LEFT JOIN CostCat_Mast c1 
                            ON c.COSTCAT_CODE = c1.CODE
                            AND c.COMP_CODE = c1.COMP_CODE

                        LEFT JOIN CostSubCat_Mast c2 
                            ON c.COSTSCAT_CODE = c2.CODE
                            AND c.COMP_CODE = c2.COMP_CODE

                        LEFT JOIN COSTCENTER_MAST c3 
                            ON c.COSTCENTER_CODE = c3.CODE
                            AND c.COMP_CODE = c3.COMP_CODE

                        WHERE a.V_TYPE = 'ITST'
                            AND b.REQ_TYPE IS NULL
                            AND CAST(a.V_DATE AS DATE) = @V_DATE
                            AND i.STATUS = 1
                            AND a.COMP_CODE = @COMP_CODE
                            AND a.BRANCH_CODE = @BRANCH_CODE
                            AND a.YEAR_CODE = @YEAR_CODE

                        ORDER BY ReqType, ReqID;
                    ";

                    using SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.Add("@V_DATE", SqlDbType.Date).Value = vDate.Date;
                    cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = globalVariables.PubCompCode;
                    cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = globalVariables.PubBranchCode;
                    cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = globalVariables.PubFYearCode;

                    var result = new List<Dictionary<string, object>>();

                    using SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }

                        result.Add(row);
                    }

                    return new { success = true, data = result };
                }
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

    }
}
