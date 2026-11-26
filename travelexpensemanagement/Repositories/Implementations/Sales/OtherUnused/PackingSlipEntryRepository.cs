using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.OtherUnused;
using travelexpensemanagement.Repositories.Interfaces.Sales.OtherUnused;

namespace travelexpensemanagement.Repositories.Implementations.Sales.OtherUnused
{
    public class PackingSlipEntryRepository : IPackingSlipEntryRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly LogService.LogService _logService;
        public PackingSlipEntryRepository(GlobalVariableService globalVariableService, DbHelper dbHelper, DataBaseConnection dbConnection, 
            LogService.LogService logService)
        {
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _logService = logService;
        }

        public async Task<RepositoryResponseData<object>> GetDOData(string docId)
        {
            if (string.IsNullOrEmpty(docId))
                return new RepositoryResponseData<object> { status = false, message = "Invalid DO." };

            var gv = _globalVariableService.GetGlobalVariables();

            var qry = $@"SELECT a.BILL_CODE, a.BILL_NAME, i.CODE AS ITEM_CODE, i.SHORTNAME AS ITEM_NAME
                 FROM DO1 a
                 LEFT JOIN ITEM_MAST i ON i.COMP_CODE = a.COMP_CODE AND i.CODE = (SELECT TOP 1 ITEM_CODE FROM DO2 WHERE V_TYPE = a.V_TYPE AND V_NO = a.V_NO
                          AND COMP_CODE = a.COMP_CODE)
                 WHERE a.DOC_ID = '{docId}' AND a.COMP_CODE = {gv.PubCompCode} AND a.BRANCH_CODE = {gv.PubBranchCode}";

            var result = await _dbHelper.GetJsonDataAsync(qry);
            return new RepositoryResponseData<object> { status = true, data = result };
        }

        public RepositoryResponseData<object> GetItemRowData(string vType, int itemCode, int rowIndex, string prevBaleNo, int placeCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            double Val(string s)
            {
                var m = System.Text.RegularExpressions.Regex.Match((s ?? "").Trim(), @"^[+-]?(\d+\.?\d*|\.\d+)");
                return m.Success ? double.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
            }

            // VB IsNumeric(): poori string number ho to true
            bool VbIsNumeric(string s)
            {
                return double.TryParse((s ?? "").Trim(),
                    System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
                    System.Globalization.CultureInfo.InvariantCulture, out _);
            }

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            // Bale No
            double baleNo;
            //if (rowIndex == 0)
            //{
            //    const string baleNOSql = @"SELECT TOP 1 ISNULL(BALE_NO,'') FROM PPRODUCTION2 WHERE ITEM_CODE=@ITEM_CODE AND V_TYPE=@V_TYPE AND 
            //                                PLACE_CODE=@PLACE_CODE AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE
            //                                ORDER BY V_DATE DESC, V_NO DESC, SNO DESC";

            //    using var cmdBale = new SqlCommand(baleNOSql, con);
            //    cmdBale.Parameters.AddWithValue("@ITEM_CODE", itemCode);
            //    cmdBale.Parameters.AddWithValue("@V_TYPE", vType);
            //    cmdBale.Parameters.AddWithValue("@PLACE_CODE", placeCode);
            //    cmdBale.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            //    cmdBale.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
            //    cmdBale.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

            //    string mno = Convert.ToString(cmdBale.ExecuteScalar()) ?? "";
            //    baleNo = VbIsNumeric(mno) ? Val(mno) + 1 : Val(mno);
            //}
            //else
            //{
            //    baleNo = Val(prevBaleNo) + 1;
            //}

            // Modified after discussion with Amar Sir

            if (rowIndex == 0)
            {
                string baleNOSql = "";

                if (gv.PubCompCode == "1")
                {
                    baleNOSql = @"SELECT TOP 1 ISNULL(BALE_NO,'') FROM PPRODUCTION2
                      WHERE V_TYPE=@V_TYPE AND COMP_CODE=@COMP_CODE
                      AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE
                      ORDER BY V_DATE DESC, V_NO DESC, SNO DESC";
                }
                else if (gv.PubCompCode == "2" && !vType.Equals("FSIS", StringComparison.OrdinalIgnoreCase))
                {
                    baleNOSql = @"SELECT TOP 1 ISNULL(BALE_NO,'') FROM PPRODUCTION2
                      WHERE ITEM_CODE=@ITEM_CODE AND V_TYPE=@V_TYPE
                      AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE
                      AND YEAR_CODE=@YEAR_CODE
                      ORDER BY V_DATE DESC, V_NO DESC, SNO DESC";
                }
                else
                {
                    baleNOSql = @"SELECT TOP 1 ISNULL(BALE_NO,'') FROM PPRODUCTION2
                      WHERE ITEM_CODE=@ITEM_CODE AND V_TYPE=@V_TYPE
                      AND PLACE_CODE=@PLACE_CODE AND COMP_CODE=@COMP_CODE
                      AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE
                      ORDER BY V_DATE DESC, V_NO DESC, SNO DESC";
                }

                using var cmdBale = new SqlCommand(baleNOSql, con);

                cmdBale.Parameters.Add("@ITEM_CODE", SqlDbType.Int).Value = itemCode;
                cmdBale.Parameters.Add("@V_TYPE", SqlDbType.VarChar).Value = vType;
                cmdBale.Parameters.Add("@PLACE_CODE", SqlDbType.Int).Value = placeCode;
                cmdBale.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = gv.PubCompCode;
                cmdBale.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = gv.PubBranchCode;
                cmdBale.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = gv.PubFYearCode;

                string mno = Convert.ToString(cmdBale.ExecuteScalar()) ?? "";
                baleNo = VbIsNumeric(mno) ? Val(mno) + 1 : Val(mno);
            }
            else
            {
                baleNo = Val(prevBaleNo) + 1;
            }

            // Item Size (INCH)
            object itemSize = null;

            const string unitSql = @"SELECT ISNULL(INCH,0) FROM ITEM_MAST WHERE ACTIVE=1 AND COMP_CODE=@COMP_CODE AND CODE=@ITEM_CODE";

            using (var cmdItem = new SqlCommand(unitSql, con))
            {
                cmdItem.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmdItem.Parameters.AddWithValue("@ITEM_CODE", itemCode);

                using var rd = cmdItem.ExecuteReader();
                if (rd.Read())
                    itemSize = rd.GetValue(0);
            }

            return new RepositoryResponseData<object>
            {
                status = true,
                data = new { baleNo, itemSize }
            };
        }

        public async Task<PackingValidationResponse> ValidateData(PackingSlipValidationRequest data)
        {
            try
            {
                if (data == null)
                    return new PackingValidationResponse { status = false, message = "Invalid validation request." };

                var gv = _globalVariableService.GetGlobalVariables();

                var warnings = new List<string>();

                await using var connection = _dbConnection.GetErpConnection();
                await connection.OpenAsync();

                if ((gv.PubCompCode == "1" || gv.PubCompCode == "4") && data.VType.Equals("FFIS", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(data.MachineType))
                {
                    return new PackingValidationResponse { status = false, field = "MachineType", message = "Issue/Machine type not selected." };
                }

                const string invoiceSql = @"SELECT TOP 1 DOC_ID FROM SALE2 WHERE PACK_TYPE = @V_TYPE AND PACK_NO = @V_NO AND COMP_CODE = @COMP_CODE 
                                    AND BRANCH_CODE = @BRANCH_CODE";

                await using (var cmd = new SqlCommand(invoiceSql, connection))
                {
                    cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar).Value = data.VType;
                    cmd.Parameters.Add("@V_NO", SqlDbType.Int).Value = data.VNo;
                    cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = gv.PubCompCode;
                    cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = gv.PubBranchCode;

                    var invoiceNo = Convert.ToString(await cmd.ExecuteScalarAsync());

                    if (!string.IsNullOrWhiteSpace(invoiceNo))
                        warnings.Add($"Packing Slip exist in invoice no. {invoiceNo}");
                }

                int validRowCount = 0;
                var itemCodes = new HashSet<int>();
                var duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < (data.Rows?.Count ?? 0); i++)
                {
                    var row = data.Rows[i];

                    if (row.ItemCode <= 0)
                    {
                        if (!string.IsNullOrWhiteSpace(row.ItemName))
                            return new PackingValidationResponse { status = false, field = "ItemCode", rowIndex = i, message = "Item code not valid." };

                        continue;
                    }

                    validRowCount++;
                    itemCodes.Add(row.ItemCode);

                    string baleNo = (row.BaleNo ?? string.Empty).Trim();
                    string lotNo = (row.LotNo ?? string.Empty).Trim();


                    if (gv.PubCompCode == "1")
                    {
                        const string sql = @"SELECT TOP 1 CONCAT(V_TYPE, V_NO) FROM PPRODUCTION2 WHERE V_TYPE = @V_TYPE AND V_NO <> @V_NO AND BALE_NO = @BALE_NO
                                        AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE 
                                        ORDER BY V_DATE DESC, V_NO DESC";

                        await using var cmd = new SqlCommand(sql, connection);
                        cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar).Value = data.VType;
                        cmd.Parameters.Add("@V_NO", SqlDbType.Int).Value = data.VNo;
                        cmd.Parameters.Add("@BALE_NO", SqlDbType.VarChar).Value = baleNo;
                        cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = gv.PubCompCode;
                        cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = gv.PubBranchCode;
                        cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = gv.PubFYearCode;

                        var previousDoc = Convert.ToString(await cmd.ExecuteScalarAsync());

                        if (!string.IsNullOrWhiteSpace(previousDoc))
                            return new PackingValidationResponse { status = false, field = "BaleNo", rowIndex = i, message = $"Roll no.=>{baleNo} already exists in previous DocID=> {previousDoc}" };
                    }
                    else if (gv.PubCompCode == "2" && !data.VType.Equals("FSIS", StringComparison.OrdinalIgnoreCase))
                    {
                        const string sql = @"SELECT COUNT(1) FROM PPRODUCTION2 WHERE V_TYPE = @V_TYPE AND V_NO <> @V_NO AND ITEM_CODE = @ITEM_CODE
                                    AND BALE_NO = @BALE_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";

                        await using var cmd = new SqlCommand(sql, connection);
                        cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar).Value = data.VType;
                        cmd.Parameters.Add("@V_NO", SqlDbType.Int).Value = data.VNo;
                        cmd.Parameters.Add("@ITEM_CODE", SqlDbType.Int).Value = row.ItemCode;
                        cmd.Parameters.Add("@BALE_NO", SqlDbType.VarChar).Value = baleNo;
                        cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = gv.PubCompCode;
                        cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = gv.PubBranchCode;
                        cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = gv.PubFYearCode;

                        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0)
                            return new PackingValidationResponse
                            {
                                status = false,
                                field = "BaleNo",
                                rowIndex = i,
                                message = $"Item and roll already exists in previous record.\nItem : {row.ItemName} ({row.ItemCode})\nRoll/Bale/Bag No : {baleNo}"
                            };
                    }
                    else if (gv.PubCompCode == "4")
                    {
                        const string sql = @"SELECT COUNT(1) FROM PPRODUCTION2 WHERE V_TYPE = @V_TYPE AND V_NO <> @V_NO AND LOT_NO = @LOT_NO AND BALE_NO = @BALE_NO
                                    AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";

                        await using var cmd = new SqlCommand(sql, connection);
                        cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar).Value = data.VType;
                        cmd.Parameters.Add("@V_NO", SqlDbType.Int).Value = data.VNo;
                        cmd.Parameters.Add("@LOT_NO", SqlDbType.VarChar).Value = lotNo;
                        cmd.Parameters.Add("@BALE_NO", SqlDbType.VarChar).Value = baleNo;
                        cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = gv.PubCompCode;
                        cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = gv.PubBranchCode;
                        cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = gv.PubFYearCode;

                        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0)
                            return new PackingValidationResponse
                            {
                                status = false,
                                field = "BaleNo",
                                rowIndex = i,
                                message = $"Lot and Roll No already exists in previous record.\nRoll/Bale/Bag No : {baleNo}\nLot/Batch No : {lotNo}"
                            };
                    }

                    if (!data.VType.Equals("FSIS", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(baleNo))
                        return new PackingValidationResponse { status = false, field = "BaleNo", rowIndex = i, message = "Bale No is blank." };

                    if (row.NetWeight == 0)
                        return new PackingValidationResponse { status = false, field = "NetWeight", rowIndex = i, message = "Net Weight is 0." };

                    if (row.Tenacity == 0)
                    {
                        if (gv.PubCompCode == "1")
                            return new PackingValidationResponse { status = false, field = "Tenacity", rowIndex = i, message = "Tenacity is blank." };

                        if (gv.PubCompCode == "4")
                            return new PackingValidationResponse { status = false, field = "AshRange", rowIndex = i, message = "Ash Range is blank." };
                    }

                    if (gv.PubCompCode != "2" && row.Status == 0)
                        return new PackingValidationResponse { status = false, field = "Status", rowIndex = i, message = "Status is blank." };
                }

                if (!string.IsNullOrWhiteSpace(data.DOId) && itemCodes.Count > 0)
                {
                    string doId = data.DOId.Trim();

                    if (doId.Length != 13)
                        return new PackingValidationResponse { status = false, message = "Invalid Delivery Order No." };

                    string doType = doId.Substring(0, 4);
                    if (!int.TryParse(doId.Substring(4, 9), out int doNo))
                        return new PackingValidationResponse { status = false, message = "Invalid Delivery Order No." };

                    var parameters = itemCodes.Select((_, i) => $"@Item{i}").ToArray();
                    string itemParameterList = string.Join(",", parameters);

                    string checkDoSql = $@"SELECT COUNT(DISTINCT ITEM_CODE) FROM DO2 WHERE ITEM_CODE IN ({itemParameterList}) AND V_TYPE = @DOType AND V_NO = @DONo
                                            AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode";

                    await using (var cmd = new SqlCommand(checkDoSql, connection))
                    {
                        int index = 0;
                        foreach (int itemCode in itemCodes)
                            cmd.Parameters.Add(parameters[index++], SqlDbType.Int).Value = itemCode;

                        cmd.Parameters.Add("@DOType", SqlDbType.VarChar).Value = doType;
                        cmd.Parameters.Add("@DONo", SqlDbType.Int).Value = doNo;
                        cmd.Parameters.Add("@CompCode", SqlDbType.Int).Value = gv.PubCompCode;
                        cmd.Parameters.Add("@BranchCode", SqlDbType.Int).Value = gv.PubBranchCode;

                        int matchedItems = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                        if (matchedItems == 0)
                            warnings.Add("Items must be same as per Delivery Order.");
                    }
                }

                if (validRowCount == 0 && gv.PubCompCode != "2")
                    return new PackingValidationResponse { status = false, message = "No Record in grid to save." };

                return new PackingValidationResponse
                {
                    status = true,
                    message = warnings.Count > 0 ? string.Join("\n", warnings) : "Validation successful.",
                    warnings = warnings
                };
            }
            catch (Exception ex)
            {
                return new PackingValidationResponse { status = false, message = "Validation failed: " + ex.Message };
            }
        }

        public RepositoryResponse SaveData(PackingSlipEntryModel model)
        {
            if (model == null || model.Items == null)
                return new RepositoryResponse { status = false, message = "Invalid data." };

            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using var con = _dbConnection.GetErpConnection();
                con.Open();
                using var transaction = con.BeginTransaction();
                try
                {
                    // Delete from Footer
                    const string deleteFooterSql = @"DELETE FROM PPRODUCTION2 WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE 
                                                AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE";
                    using (var cmd = new SqlCommand(deleteFooterSql, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }


                    // Header Save/Update
                    using (var cmd = new SqlCommand("Sp_PackingSlipEntry", con, transaction))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        if (model.ACTION == "UPDATE")
                        {
                            cmd.Parameters.AddWithValue("@ACTION", "Update");
                            cmd.Parameters.AddWithValue("@EUSER", gv.PubUserId);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@ACTION", "InsertIntoHeader");
                            cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                        }

                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DOC_ID", (model.V_TYPE ?? "") + (model.V_NO?.ToString() ?? ""));
                        cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PLACE_CODE", model.PLACE_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PORD_TYPE", model.PORD_TYPE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PORD_NO", model.PORD_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@REF_TYPE", model.REF_TYPE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@REF_NO", model.REF_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIFT", model.SHIFT ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SLIP_NO", model.SLIP_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MAC_TYPE", model.MAC_TYPE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MAC_CODE", model.MAC_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@GODOWN_CODE", model.GODOWN_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@STATUS", model.STATUS ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.ExecuteNonQuery();
                    }

                    // Row Save/Update
                    int i = 1;
                    foreach (var row in model.Items)
                    {
                        using (var cmd = new SqlCommand("Sp_PackingSlipEntry", con, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            if (model.ACTION == "UPDATE")
                            {
                                cmd.Parameters.AddWithValue("@EUSER", gv.PubUserId);
                                cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@AED", 'E');
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@AED", 'A');
                            }
                            cmd.Parameters.AddWithValue("@ACTION", "InsertIntoFooter");

                            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DOC_ID", (model.V_TYPE ?? "") + (model.V_NO?.ToString() ?? ""));
                            cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIFT", model.SHIFT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SNO", i++);
                            cmd.Parameters.AddWithValue("@PLACE_CODE", row.PLACE_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PORD_TYPE", model.PORD_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PORD_NO", model.PORD_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ITEM_CODE", row.ITEM_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ITEM_NAME", row.ITEM_NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ITEM_SIZE", row.ITEM_SIZE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@NOS", row.NOS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@WASTE", row.WASTE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BALE_NO", row.BALE_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LOT_NO", row.LOT_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@GROSS_QTY", row.GROSS_QTY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TARE_QTY", row.TARE_QTY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@QTY", row.QTY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MTR", row.MTR ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@AVG_QTY", row.AVG_QTY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@GRAM", row.GRAM ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PACK_QTY", row.PACK_QTY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@REMARKS", row.REMARKS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TENACITY_CODE", row.TENACITY_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@STATUS_CODE", row.STATUS_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@UOM_CODE", row.UOM_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@UOM_NAME", row.UOM_NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MAKE_CODE", row.MAKE_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@FROM_DEPT", row.FROM_DEPT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TO_DEPT", row.TO_DEPT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MAC_TYPE", row.MAC_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MAC_CODE", row.MAC_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LAND_RATE", row.LAND_RATE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LAND_AMT", row.LAND_AMT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@REF_TYPE", row.REF_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@REF_NO", row.REF_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LMRC_TYPE", row.LMRC_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LMRC_NO", row.LMRC_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmd.Parameters.AddWithValue("@CREEL_NO", row.CREEL_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@QC_SAMPLE", row.QC_SAMPLE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@flg", row.flg ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LOG_FLG", row.LOG_FLG ?? (object)DBNull.Value);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();

                    //_logService.InsertLog("PPRODUCTION1", "Packing Slip Entry", "Other Unused", model.ACTION == "UPDATE" ? "UPDATE" : "ADD", model.V_TYPE, model.V_NO.ToString(), null);
                    //_logService.InsertLog("PPRODUCTION2", "Packing Slip Entry", "Other Unused", model.ACTION == "UPDATE" ? "UPDATE" : "ADD", model.V_TYPE, model.V_NO.ToString(), null);

                    return new RepositoryResponse { status = true, message = model.ACTION == "UPDATE" ? "Data updated successfully." : "Data saved successfully." };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new RepositoryResponse { status = false, message = "Error saving data: " + ex.Message };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<PackingSlipEntryModel> GetById(int id, string vtype)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(vtype))
            {
                return new RepositoryResponseData<PackingSlipEntryModel> { status = false, message = "Invalid request." };
            }

            var gv = _globalVariableService.GetGlobalVariables();
            var data = new PackingSlipEntryModel();

            try
            {
                using var con = _dbConnection.GetErpConnection();
                con.Open();

                using (var cmd = new SqlCommand("Sp_PackingSlipEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ACTION", "GetById");
                    cmd.Parameters.AddWithValue("@V_NO", id);
                    cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            data.V_NO = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]);
                            data.V_TYPE = reader["V_TYPE"]?.ToString();
                            data.V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]);
                            data.STATUS = reader["STATUS"] == DBNull.Value ? null : Convert.ToInt32(reader["STATUS"]);
                            data.MAC_CODE = reader["MAC_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["MAC_CODE"]);
                            data.SHIFT = reader["SHIFT"]?.ToString();
                            data.GODOWN_CODE = reader["GODOWN_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["GODOWN_CODE"]);
                            data.PORD_NO = reader["PORD_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["PORD_NO"]);
                            data.PORD_TYPE = reader["PORD_TYPE"]?.ToString();
                            data.PARTY_CODE = reader["PARTY_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["PARTY_CODE"]);
                            data.REMARKS = reader["REMARKS"]?.ToString();
                            data.PLACE_CODE = reader["PLACE_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["PLACE_CODE"]);
                        }

                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                data.Items.Add(new PackingSlipEntryRowModel
                                {
                                    ITEM_CODE = reader["ITEM_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["ITEM_CODE"]),
                                    BALE_NO = reader["BALE_NO"]?.ToString(),
                                    LOT_NO = reader["LOT_NO"]?.ToString(),
                                    GROSS_QTY = reader["GROSS_QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["GROSS_QTY"]),
                                    TARE_QTY = reader["TARE_QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["TARE_QTY"]),
                                    QTY = reader["QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["QTY"]),
                                    TENACITY_CODE = reader["TENACITY_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["TENACITY_CODE"]),
                                    STATUS_CODE = reader["STATUS_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["STATUS_CODE"]),
                                    REMARKS = reader["REMARKS"]?.ToString(),
                                    REF_NO = reader["REF_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["REF_NO"]),
                                    REF_TYPE = reader["REF_TYPE"]?.ToString(),
                                    LMRC_NO = reader["LMRC_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["LMRC_NO"]),
                                    LMRC_TYPE = reader["LMRC_TYPE"]?.ToString(),
                                    MAC_CODE = reader["MAC_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["MAC_CODE"]),
                                    MAC_TYPE = reader["MAC_TYPE"]?.ToString(),
                                    PLACE_CODE = reader["PLACE_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["PLACE_CODE"]),
                                    ITEM_SIZE = reader["ITEM_SIZE"] == DBNull.Value ? null : Convert.ToDecimal(reader["ITEM_SIZE"])
                                });
                            }
                        }
                    }
                }

                return new RepositoryResponseData<PackingSlipEntryModel> { status = true, data = data };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<PackingSlipEntryModel> { status = false, message = "Error retrieving data: " + ex.Message };
            }
        }

        public async Task<RepositoryResponseData<List<CopyFromItemModel>>> GetCopyFromList(int? itemCode, int? macCode, DateTime vDate)
        {
            var list = new List<CopyFromItemModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using var con = _dbConnection.GetErpConnection();
                await con.OpenAsync();

                using var cmd = new SqlCommand("Sp_PackingSlipEntry", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ACTION", "CopyFrom");
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode ?? 0);
                cmd.Parameters.AddWithValue("@MAC_CODE", macCode ?? 0);
                cmd.Parameters.AddWithValue("@V_DATE", vDate);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new CopyFromItemModel
                    {
                        BALE_NO = reader["BALE_NO"]?.ToString(),
                        V_NO = reader["V_NO"]?.ToString(),
                        V_TYPE = reader["V_TYPE"]?.ToString(),
                        V_DATE = reader["V_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(reader["V_DATE"]).ToString("dd/MM/yyyy"),
                        ITEM_CODE = reader["ITEM_CODE"]?.ToString(),
                        ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
                        UOM_NAME = reader["UOM_NAME"]?.ToString(),
                        LOT_NO = reader["LOT_NO"]?.ToString(),
                        GROSS_QTY = reader["GROSS_QTY"]?.ToString(),
                        TARE_QTY = reader["TARE_QTY"]?.ToString(),
                        QTY = reader["QTY"]?.ToString(),
                        MTR = reader["MTR"]?.ToString(),
                        AVG_QTY = reader["AVG_QTY"]?.ToString(),
                        GRAM = reader["GRAM"]?.ToString(),
                        TENACITY = reader["Tenacity"]?.ToString(),
                        STATUS = reader["Status"]?.ToString(),
                        REMARKS = reader["Remarks"]?.ToString(),
                        LMRC_TYPE = reader["LMRC_TYPE"]?.ToString(),
                        LMRC_NO = reader["LMRC_NO"]?.ToString(),
                        MAC_TYPE = reader["MAC_TYPE"]?.ToString(),
                        MAC_CODE = reader["MAC_CODE"]?.ToString(),
                        PLACE_CODE = reader["PLACE_CODE"]?.ToString(),
                        UNIT_CODE = reader["UNIT_CODE"]?.ToString(),
                        TENACITY_CODE = reader["TENACITY_CODE"]?.ToString(),
                        STATUS_CODE = reader["STATUS_CODE"]?.ToString(),
                        INCH = reader["INCH"]?.ToString()
                    });
                }

                return new RepositoryResponseData<List<CopyFromItemModel>> { status = true, data = list };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<CopyFromItemModel>> { status = false, message = ex.Message };
            }
        }
    }
}
