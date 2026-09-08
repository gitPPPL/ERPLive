using iText.StyledXmlParser.Jsoup.Select;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using StackExchange.Redis;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Pages.Admin.SystemInitilization.DocumentTypeMasterList;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Inventory.Transaction
{
    public class InventoryDepartmentIssueRepository : IInventoryDepartmentIssueRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;

        public InventoryDepartmentIssueRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DropdownService dropdownService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
        }

        public object DDlVType(string formName)
        {
            var getData = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string vType = formName switch
                {
                    "RecdAfterProdEntry" => "'PRDR'",
                    "SFGIssueToDispatch" => "'RAID'",
                    "SFGAdjustmentIssue" => "'RMAI'",
                    "SFGAdjustmentReceived" => "'RMAR'",
                    "ChemicalIssue" => "'CMIS'",
                    "ChemicalReceived" => "'CMRC'",
                    "AdjustmentIssue" => "'STAI','STPR'",
                    "AdjustmentReceived" => "'STAR','SRCO'",
                    "RMGTOWaste" => "'RAIV','RAIT'",
                    _ => throw new ArgumentException(
                        $"Invalid Formname: {formName}",
                        nameof(formName))
                };

                string query = $@" SELECT  CODE,NAME FROM DOCTYPE_MAST WHERE CODE IN ({vType})  ORDER BY NAME";

                var data = _dropdownService.GetDropdownList(query);

                return data;
            }
        }

        public object DDlItemName(string formName, string V_TYPE)
        {
            var getData = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_InventoryDepartmentIssue", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@ACTION", SqlDbType.VarChar, 100).Value = "ItemName";
                cmd.Parameters.Add("@FormName", SqlDbType.VarChar, 100).Value = formName;
                cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar, 50).Value = V_TYPE;
                cmd.Parameters.Add("@COMP_CODE", SqlDbType.VarChar, 50).Value = getData.PubCompCode;
                cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.VarChar, 50).Value = getData.PubBranchCode;

                con.Open();

                var data = new List<object>();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        data.Add(new
                        {
                            ItemCode = reader["icode"]?.ToString(),
                            ItemName = reader["itemname"]?.ToString(),
                            unit = reader["unit"]?.ToString(),
                            ucode = reader["ucode"]?.ToString(),
                            ShortName = reader["ShortName"]?.ToString(),
                        });
                    }
                }

                return data;
            }
        }

        public object CopyData(string V_TYPE)
        {
            var getData = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_InventoryDepartmentIssue", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@ACTION", SqlDbType.VarChar, 100).Value = "CopyData";
                cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar, 50).Value = V_TYPE;
                cmd.Parameters.Add("@COMP_CODE", SqlDbType.VarChar, 50).Value = getData.PubCompCode;
                cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.VarChar, 50).Value = getData.PubBranchCode;
                cmd.Parameters.Add("@Year_Code", SqlDbType.VarChar, 50).Value = getData.PubFYearCode;

                con.Open();

                var data = new List<object>();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        data.Add(new
                        {

                            VNo = reader["VNo"] == DBNull.Value ? (long?)null : Convert.ToInt64(reader["VNo"]),
                            VDate = reader["VDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["VDate"]),
                            ItemName = reader["ItemName"] == DBNull.Value ? null : reader["ItemName"].ToString(),
                            Nos = reader["Nos"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Nos"]),
                            Qty = reader["Qty"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Qty"]),
                            Unit = reader["Unit"] == DBNull.Value ? null : reader["Unit"].ToString(),
                            Make = reader["Make"] == DBNull.Value ? null : reader["Make"].ToString(),
                            Place = reader["Place"] == DBNull.Value ? null : reader["Place"].ToString(),
                            Remarks = reader["Remarks"] == DBNull.Value ? null : reader["Remarks"].ToString(),
                            ItemCode = reader["ItemCode"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ItemCode"]),
                            UOM_CODE = reader["UOM_CODE"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["UOM_CODE"]),
                            MAKE_CODE = reader["MAKE_CODE"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["MAKE_CODE"]),
                            PlaceCode = reader["PlaceCode"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["PlaceCode"])
                        });
                    }
                }

                return data;
            }
        }


        public string GetText(string query)
        {
            try
            {
                using var con = _dbConnection.GetErpConnection();
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return reader[0].ToString();
                            }
                            else
                            {
                                return string.Empty;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetText() Error: " + ex.Message);
                return string.Empty;
            }
        }








        public async Task<(string Status, string Message)> validation( InventryDepartmentIssue_Header header,  List<InventryDepartmentIssue_Details> details, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                string docId = string.IsNullOrWhiteSpace(header.DOC_ID)
                    ? $"{header.V_TYPE}{header.V_NO}"
                    : header.DOC_ID;

                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details)
                    {
                        if (detail == null || detail.ITEM_CODE <= 0)
                            continue;

                        if (header.V_TYPE == "RMAI" || header.V_TYPE == "RAID")
                        {
                            decimal closStk = 0m;

                            string excludeDoc = $"{header.V_TYPE}{header.V_NO}{detail.SNO}";

                            string sql = $@" SELECT SUM(qty) FROM VW_STOCK_MOVEMENT WHERE comp_code = {g.PubCompCode}
                            AND branch_code = {g.PubBranchCode} AND v_date <= {(header.V_DATE)}
                            AND v_type IN ('BFOP','OO','OPRM','BFRC','RCPI','RCPT',
                            'SRPU','PRDR','SRCO','STAR','RMAR','RAIV') AND item_code = {detail.ITEM_CODE}
                            AND CONCAT(v_type,v_no,sno) <> '{excludeDoc}'";

                            closStk += await GetDecimalAsync(conn, sql);

                            sql = $@" SELECT SUM(qty) FROM VW_STOCK_MOVEMENT  WHERE comp_code = {g.PubCompCode}
                            AND branch_code = {g.PubBranchCode} AND v_date <= {(header.V_DATE)}
                            AND v_type IN ('RRET','SRET','BFIS','PRDI','SICO',
                            'RAIP','STAI','RMAI','RAIT') AND item_code = {detail.ITEM_CODE}
                            AND CONCAT(v_type,v_no,sno) <> '{excludeDoc}'";

                            closStk -= await GetDecimalAsync(conn, sql);
                                     
                            sql = $@" SELECT SUM(qty) FROM VW_STOCK_MOVEMENT  WHERE comp_code = {g.PubCompCode}
                            AND branch_code = {g.PubBranchCode}  AND v_date <= {(header.V_DATE)}
                            AND v_type IN ('JBRC','SAJR','SART')  AND mgroup_type <> 'Finish'
                            AND item_code = {detail.ITEM_CODE} AND CONCAT(v_type,v_no,sno) <> '{excludeDoc}'";

                            closStk += await GetDecimalAsync(conn, sql);
                 
                            sql = $@" SELECT SUM(qty)  FROM VW_STOCK_MOVEMENT  WHERE comp_code = {g.PubCompCode}
                            AND branch_code = {g.PubBranchCode}  AND v_date <= {(header.V_DATE)}
                            AND v_type IN ('JBIS','SAJI','ESAG','SAGT') AND mgroup_type <> 'Finish'
                            AND item_code = {detail.ITEM_CODE}  AND CONCAT(v_type,v_no,sno) <> '{excludeDoc}'";

                            closStk -= await GetDecimalAsync(conn, sql);

                            sql = $@" SELECT SUM(qty)  FROM VW_STOCK_MOVEMENT  WHERE comp_code = {g.PubCompCode}
                            AND branch_code = {g.PubBranchCode}  AND v_date <= {(header.V_DATE)}
                            AND v_type IN ('FFRC','FLRC','FPDR','FPRC','FRC')  AND mgroup_type = 'Finish'
                            AND item_code = {detail.ITEM_CODE} AND CONCAT(v_type,v_no,sno) <> '{excludeDoc}'";

                            closStk += await GetDecimalAsync(conn, sql);

                            sql = $@" SELECT SUM(qty)  FROM VW_STOCK_MOVEMENT WHERE comp_code = {g.PubCompCode}
                            AND branch_code = {g.PubBranchCode}  AND v_date <= {(header.V_DATE)}
                            AND v_type IN ('FFIS','FLIS','FPIS','FSIS')  AND mgroup_type = 'Finish'
                            AND item_code = {detail.ITEM_CODE} AND CONCAT(v_type,v_no,sno) <> '{excludeDoc}'";

                            closStk -= await GetDecimalAsync(conn, sql);

                            closStk = Math.Round(closStk, 2);
                                       

                            decimal requiredQty = Convert.ToDecimal(detail.QTY);

                            if ((closStk - requiredQty) < 0)
                            {
                                string itemName = detail.ITEM_NAME ?? "";

                                string message = $"Item : ({detail.ITEM_CODE}) {itemName}, " + $"Please Check Stock Available = {closStk}";
                                                  

                                if (g.PubUserId != "1")
                                {
                                    return ("Error", message);
                                }
                            }

                            if (action == "Insert" && closStk > 0)
                            {
                                decimal closAmt = 0m;
                                decimal balRate = 0m;

        
                                sql = $@" SELECT SUM(land_amt) FROM VW_STOCK_MOVEMENT WHERE comp_code = {g.PubCompCode}
                                AND branch_code = {g.PubBranchCode}  AND v_date <= {(header.V_DATE)} AND v_type IN
                                ('BFOP','OO','OPRM','BFRC','RCPI','RCPT', 'SRPU','PRDR','SRCO','STAR','RMAR','RAIV')
                                AND item_code = {detail.ITEM_CODE} AND CONCAT(v_type,v_no,sno) <> '{excludeDoc}'";

                                closAmt += await GetDecimalAsync(conn, sql);

                                sql = $@" SELECT SUM(land_amt)  FROM VW_STOCK_MOVEMENT  WHERE comp_code = {g.PubCompCode}
                                AND branch_code = {g.PubBranchCode} AND v_date <= {(header.V_DATE)} AND v_type IN
                                ('RRET','SRET','BFIS','PRDI','SICO',  'RAIP','STAI','RMAI','RAIT')
                                AND item_code = {detail.ITEM_CODE} AND CONCAT(v_type,v_no,sno) <> '{excludeDoc}'";

                                closAmt -= await GetDecimalAsync(conn, sql);

                                sql = $@"  SELECT SUM(land_amt)  FROM VW_STOCK_MOVEMENT
                                WHERE comp_code = {g.PubCompCode} AND branch_code = {g.PubBranchCode}
                                AND v_date <= {(header.V_DATE)} AND v_type IN ('JBRC','SAJR','SART')
                                AND mgroup_type <> 'Finish' AND item_code = {detail.ITEM_CODE} AND CONCAT(v_type,v_no,sno) <> '{excludeDoc}'";

                                closAmt += await GetDecimalAsync(conn, sql);

                                sql = $@" SELECT SUM(land_amt)  FROM VW_STOCK_MOVEMENT WHERE comp_code = {g.PubCompCode}
                                AND branch_code = {g.PubBranchCode} AND v_date <= {(header.V_DATE)}
                                AND v_type IN ('JBIS','SAJI','ESAG','SAGT') AND mgroup_type <> 'Finish'
                                AND item_code = {detail.ITEM_CODE}  AND CONCAT(v_type,v_no,sno) <> '{excludeDoc}'";

                                closAmt -= await GetDecimalAsync(conn, sql);

                                sql = $@" SELECT SUM(land_amt) FROM VW_STOCK_MOVEMENT
                                WHERE comp_code = {g.PubCompCode} AND branch_code = {g.PubBranchCode}
                                AND v_date <= {(header.V_DATE)}  AND v_type IN ('FFRC','FLRC','FPDR','FPRC','FRC')
                                AND mgroup_type = 'Finish'  AND item_code = {detail.ITEM_CODE}  AND CONCAT(v_type,v_no,sno) <> '{excludeDoc}'";

                                closAmt += await GetDecimalAsync(conn, sql);

                                sql = $@" SELECT SUM(land_amt)  FROM VW_STOCK_MOVEMENT
                                WHERE comp_code = {g.PubCompCode}  AND branch_code = {g.PubBranchCode}
                                AND v_date <= {(header.V_DATE)}  AND v_type IN ('FFIS','FLIS','FPIS','FSIS')
                                AND mgroup_type = 'Finish'  AND item_code = {detail.ITEM_CODE} AND CONCAT(v_type,v_no,sno) <> '{excludeDoc}'";

                                closAmt -= await GetDecimalAsync(conn, sql);


                                closAmt = Math.Round(closAmt, 2);
                                        
                                if (closAmt > 0 && closStk > 0)
                                {
                                    balRate = closAmt / closStk;
                                    detail.RATE = Math.Round(balRate, 2);
                                    detail.AMOUNT =  Math.Round(requiredQty * balRate, 2);
                                }
                            }
                        }

                        if (g.PubCompCode == "4"  && detail.ITEM_CODE > 0 && detail.PORD_NO > 0 && detail.EMPTY_YN == "BT")
                        {
                                string sql = $@"
                                SELECT COUNT(*)  FROM PROD_SFG2  WHERE V_TYPE = '{header.V_TYPE}'
                                AND V_DATE = '{header.V_DATE:yyyy-MM-dd}'  AND SHIFT = '{header.SHIFT}'
                                AND COMP_CODE = {g.PubCompCode} AND BRANCH_CODE = {g.PubBranchCode} AND YEAR_CODE = {g.PubFYearCode}
                                AND ITEM_CODE = {detail.ITEM_CODE} AND PORD_NO = {detail.PORD_NO} AND ISNULL(LMRC_NO, 0) = 0";

                            decimal qc = await GetDecimalAsync(conn, sql);

                            if (qc >= 2)
                            {
                                string itemName = detail.ITEM_NAME ?? "";

                                return (  "Error", $"Quality Control is not Done of Item => {itemName}" );
                            }
                        }


                        String QUERY = $@"SELECT isnull(MANAGE_TYPE,'') FROM ITEM_MAST WHERE COMP_CODE={g.PubCompCode} AND CODE={detail.ITEM_CODE}";


                        string managetype = GetText(QUERY);


                        if (managetype.ToLower() == "Batch".ToLower())
                        {

                            string query = @"
                            SELECT  ISNULL(SUM(QTY), 0) AS QTY, COUNT(1) AS NOS  FROM PROD_BATCH  WHERE V_TYPE = @V_TYPE
                            AND V_NO = @V_NO AND TO_DEPT = @TO_DEPT  AND ITEM_CODE = @ITEM_CODE AND COMP_CODE = @COMP_CODE
                            AND BRANCH_CODE = @BRANCH_CODE";

                            using var cmd = new SqlCommand(query, conn);

                            cmd.Parameters.AddWithValue("@V_TYPE", header.V_TYPE);
                            cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                            cmd.Parameters.AddWithValue("@TO_DEPT", detail.TO_DEPT);
                            cmd.Parameters.AddWithValue("@ITEM_CODE", detail.ITEM_CODE);
                            cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);

                            decimal qty = 0;
                            int nos = 0;

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    qty = reader["QTY"] != DBNull.Value ? Convert.ToDecimal(reader["QTY"]) : 0; 
                                    nos = reader["NOS"] != DBNull.Value ? Convert.ToInt32(reader["NOS"])  : 0;
                                }

                                if(nos != detail.NOS || qty != detail.QTY)
                                {
                                    return ("Error", $"Bag number OR Quantity is not matched from Batch-wise selection, Check for Item = > {detail.ITEM_NAME}");
                                }                                                             

                            }
                        }

                    }
                }

                return ("Success", "Data Save Successfully");
            }
            catch (Exception ex)
            {
                return ("Error", ex.Message);
            }
        }

        private async Task<decimal> GetDecimalAsync( DbConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            object result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                return 0m;

            return decimal.TryParse(
                Convert.ToString(result),
                out decimal value)
                ? value
                : 0m;
        }

        public async Task<(string Status, string Message)> SubmitRequest(InventryDepartmentIssue_Header header, List<InventryDepartmentIssue_Details> details, string action)
        {
            try
            {
                var validationResult = await validation(header, details, action);

                if (validationResult.Status != "Success")
                {
                    return validationResult;
                }

                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();

                await conn.OpenAsync();

                string docId = string.IsNullOrWhiteSpace(header.DOC_ID) ? $"{header.V_TYPE}{header.V_NO}" : header.DOC_ID;

                using (var cmd = new SqlCommand("sp_InventoryDepartmentIssue", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@SaveAction", "HEADER");
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;
                    cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd.Parameters.AddWithValue("@SHIFT", header.SHIFT);
                    cmd.Parameters.AddWithValue("@SLIP_NO", header.SLIP_NO);
                    cmd.Parameters.AddWithValue("@REMARKS", (object?)header.REMARKS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PORD_TYPE", header.PORD_TYPE);
                    cmd.Parameters.AddWithValue("@PORD_NO", header.PORD_NO);
                    cmd.Parameters.AddWithValue("@PLAN_TYPE", header.PLAN_TYPE);
                    cmd.Parameters.AddWithValue("@PLAN_NO", header.PLAN_NO);
                    cmd.Parameters.AddWithValue("@STATUS", header.STATUS);                            
                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    await cmd.ExecuteNonQueryAsync();
                }

                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details)
                    {
                        if (detail == null || detail.ITEM_CODE <= 0)
                            continue;                   

                        using var cmd = new SqlCommand("sp_InventoryDepartmentIssue", conn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SaveAction", "DETAILS");
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                        cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", detail.ITEM_CODE);
                        cmd.Parameters.AddWithValue("@ITEM_NAME", (object?)detail.ITEM_NAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UOM_CODE", (object?)detail.UOM_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UOM_NAME", (object?)detail.UOM_NAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LOT_NO", (object?)detail.LOT_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NOS", (object?)detail.NOS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@QTY", (object?)detail.QTY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARKS", (object?)detail.REMARKS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@RATE", (object?)detail.RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AMOUNT", (object?)detail.AMOUNT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LAND_RATE", (object?)detail.LAND_RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LAND_AMT", (object?)detail.LAND_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIFT", (object?)detail.SHIFT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PORD_TYPE", (object?)detail.PORD_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PORD_NO", (object?)detail.PORD_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FROM_DEPT", (object?)detail.FROM_DEPT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TO_DEPT", (object?)detail.TO_DEPT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EMPTY_YN", (object?)detail.EMPTY_YN ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@COSTCAT_CODE", (object?)detail.COSTCAT_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@COSTSCAT_CODE", (object?)detail.COSTSCAT_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@COSTCENTER_CODE", (object?)detail.COSTCENTER_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SNO", detail.SNO);
                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return ("Success", "Data Save Successfully");

            }
            catch (Exception ex)
            {
                return ("Error", ex.Message);
            }
        }

        public object DDlPlaceFrom(string formName)
        {
            var getData = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "";
                if (formName == "AdjustmentIssue" || formName == "AdjustmentReceived")
                {
                    query = " select code,name from ITEMDEPT_MAST where Active=1 and COMP_CODE=" + getData.PubCompCode + " and TRAN_TYPE='Store' order by name";
                }
                else
                {
                    query = "select code,name   from ITEMDEPT_MAST where Active=1 and COMP_CODE=" + getData.PubCompCode + " and TRAN_TYPE='Production' order by name";
                }

                var data = _dropdownService.GetDropdownList(query);

                return data;
            }
        }

    }
}



