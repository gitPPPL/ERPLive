using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Weighbridge.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Weighbridge.Transaction
{
    public class BigWeighbridgeRepository : IBigWeighbridgeRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly GlobalValidationdate _globalValidationdate;
        public BigWeighbridgeRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, 
            travelexpensemanagement.LogService.LogService logService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _dbHelper = dbHelper;
            _globalVariableService = globalVariableService;
            _logService = logService;
            _globalValidationdate = globalValidationdate;
        }

        public async Task<(string DocId, string VNo)> GetMaxVNoAsync(string vType)
        {
            var session = _globalVariableService.GetGlobalVariables();

            var companyCode = session.PubCompCode;
            var yearCode = session.PubFYearCode;
            var branchCode = 1;
            var tableName = "WB1";

            var yearParams = new Dictionary<string, object>
            {
                { "@YearCd", yearCode }
            };

            var vnoParams = new Dictionary<string, object>
            {
                { "@COMP_CODE", companyCode },
                { "@BRANCH_CODE", branchCode },
                { "@YEAR_CODE", yearCode },
                { "@V_TYPE", vType },
                { "@TableName", tableName }
            };

            string nextVNo = await _dbHelper.GetExecuteScalarAsync<string>(
                "sp_GetMaxVNo",
                vnoParams,
                isStoredProc: true
            );

            string year = await _dbHelper.GetExecuteScalarAsync<string>(
                "SELECT dbo.fn_GetCurrentYear(@YearCd)",
                yearParams
            );

            string docId = vType + year + nextVNo;
            string newVNo = year + nextVNo;

            return (docId, newVNo);
        }

        public async Task<object> GetGateNoAsync(string wbType)
        {
           // var WbCondtion = string.Empty;

            //if (wbType == "Raw Material")
            //{
            //    WbCondtion = " AND g.V_TYPE IN ('INRM') ";
            //}
            //else if (wbType == "Store")
            //{
            //    WbCondtion = "  AND g.V_TYPE IN ('INST') ";
            //}
            //else if (wbType == "Fuel")
            //{
            //    WbCondtion = "  AND g.V_TYPE IN ('INFU') ";
            //}
            //else if (wbType == "Sales")
            //{
            //    WbCondtion = "  AND g.V_TYPE IN ('TRGI') ";
            //}
            //else if (wbType == "Misc")
            //{
            //    WbCondtion = "  AND g.V_TYPE IN ('INMS') ";
            //}
            //else if (wbType == "JobWork")
            //{
            //    WbCondtion = "  AND g.V_TYPE IN ('INJB') ";
            //}
            //else if (wbType == "RGP")
            //{
            //    WbCondtion = "  AND g.V_TYPE IN ('INRT') ";
            //}
            //else if (wbType == "Sales Return")
            //{
            //    WbCondtion = " AND g.V_TYPE IN ('INSR') ";
            //}
            //else
            //{
            //    WbCondtion = " AND g.V_TYPE IN ( select DISTINCT CODE from DOCTYPE_MAST where DOCTYPE='GateInward' ) ";
            //}

            string WbCondtion = wbType?.Trim() switch
            {
                "Raw Material" => " AND g.V_TYPE IN ('INRM') ",
                "Store" => " AND g.V_TYPE IN ('INST') ",
                "Fuel" => " AND g.V_TYPE IN ('INFU') ",
                "Sales" => " AND g.V_TYPE IN ('TRGI') ",
                "Misc" => " AND g.V_TYPE IN ('INMS') ",
                "JobWork" => " AND g.V_TYPE IN ('INJB') ",
                "RGP" => " AND g.V_TYPE IN ('INRT') ",
                "Sales Return" => " AND g.V_TYPE IN ('INSR') ",
                _ => ""
            };

            var session = _globalVariableService.GetGlobalVariables();

            string strqry = $@"
                            SELECT V_NO, V_TYPE, TRUCK_NO, PARTY_CODE,
                                   sg.NAME AS partyName,
                                   d.NAME AS VtypeName
                            FROM GATE1 g
                            LEFT JOIN SUBGROUP_MAST sg 
                                ON g.PARTY_CODE = sg.CODE 
                                AND g.COMP_CODE = sg.COMP_CODE
                            LEFT JOIN DOCTYPE_MAST d 
                                ON g.V_TYPE = d.CODE
                            WHERE g.COMP_CODE = {session.PubCompCode}
                              AND g.YEAR_CODE = {session.PubFYearCode}
                              AND g.BRANCH_CODE ={session.PubBranchCode}
                              {WbCondtion}
                            ORDER BY V_NO DESC";

            var gateList = await _dbHelper.GetJsonDataAsync(strqry);
            return gateList;
        }

        public async Task<object> GetDocTypeAsync()
        {
            var query = @"SELECT CODE, NAME FROM DOCTYPE_MAST WHERE ISNULL(DOCTYPE, '') = 'KantaBig'";
            var result = await _dbHelper.GetJsonDataAsync(query);
            return result;
        }

        public async Task<object> GetItemListAsync()
        {
            var session = _globalVariableService.GetGlobalVariables();

            string query = $@"
            SELECT CODE, NAME, HSN_CODE, UNIT_NAME, UNIT_CODE
            FROM ITEM_MAST
            WHERE COMP_CODE = {session.PubCompCode}
            ORDER BY NAME";

            return await _dbHelper.GetJsonDataAsync(query);
        }

        public async Task<object> GetPlaceMastAsync()
        {
            var session = _globalVariableService.GetGlobalVariables();

            string query = $@"
        SELECT CODE, NAME
        FROM ITEMDEPT_MAST
        WHERE COMP_CODE = {session.PubCompCode}
          AND TRAN_TYPE = 'Production'
        ORDER BY NAME";

            return await _dbHelper.GetJsonDataAsync(query);
        }

        public async Task<object> GetPartyListAsync()
        {
            var session = _globalVariableService.GetGlobalVariables();

            string query = $@"
                SELECT CODE, NAME
                FROM SUBGROUP_MAST
                WHERE COMP_CODE = {session.PubCompCode}
                  AND ACTIVE = 1
                ORDER BY NAME";

            return await _dbHelper.GetJsonDataAsync(query);
        }

        public async Task<(object Header, object Detail)> GetWeighBridgeByIdAsync(string id)
        {
            var session = _globalVariableService.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                {"@COMP_CODE", session.PubCompCode},
                {"@YEAR_CODE", session.PubFYearCode},
                {"@BRANCH_CODE", session.PubBranchCode},
                {"@DOC_ID", id},
                {"@Action", "WBEntryHeaderData"}
            };

            var parameter1 = new Dictionary<string, object>
            {
                {"@COMP_CODE", session.PubCompCode},
                {"@YEAR_CODE", session.PubFYearCode},
                {"@BRANCH_CODE", session.PubBranchCode},
                {"@DOC_ID", id},
                {"@Action", "WBEntryDetailData"}
            };

            var header = await _dbHelper.GetJsonFromProcedureAsync(
                "[dbo].[sp_GetWBEntry]",
                parameter
            );

            var detail = await _dbHelper.GetJsonFromProcedureAsync(
                "[dbo].[sp_GetWBEntry]",
                parameter1
            );

            return (header, detail);
        }

        public async Task<(bool Status, string Message)> SaveOrUpdateWeighBridgeEntryAsync(WBEntryModel model)
        {
            if (model == null)
                return (false, "Invalid model data");

            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    if (model.SaveOrUpdate != "Save")
                    {
                        var validation = await ValidateMRNAsync(model);

                        if (!validation.Status)
                        {
                            return (false, validation.Message);
                        }
                    }

                    var session = _globalVariableService.GetGlobalVariables();

                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_WBEntry]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@Action", model.SaveOrUpdate == "Save" ? "Add" : "Edit");
                                cmd.Parameters.AddWithValue("@YEAR_CODE", session.PubFYearCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMP_CODE", session.PubCompCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", session.PubBranchCode);

                                cmd.Parameters.AddWithValue("@DOC_ID", model.DOC_ID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_SHIFT", model.V_SHIFT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WB_TYPE", model.WB_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GATE_TYPE", model.GATE_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GATE_NO", model.GATE_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_QTY", model.PARTY_QTY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GROSS_NO", model.GROSS_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TARE_NO", model.TARE_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VEHICLE_NO", model.VEHICLE_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);

                                cmd.Parameters.AddWithValue("@STATUS", model.STATUS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@STATUS_DATE", model.STATUS_DATE ?? (object)DBNull.Value);

                                cmd.Parameters.AddWithValue("@NET_WGT", model.NET_WGT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FINAL_TYPE", model.FINAL_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FINAL_REM", model.FINAL_REM ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_GROSSWT", model.PARTY_GROSSWT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_TRWT", model.PARTY_TRWT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_WBNO", model.PARTY_WBNO ?? (object)DBNull.Value);

                                cmd.Parameters.AddWithValue("@SMALL_BAG", model.SMALL_BAG ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@MEDIUM_BAG", model.MEDIUM_BAG ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LARGE_BAG", model.LARGE_BAG ?? (object)DBNull.Value);

                                cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@USER", session.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Lip", session.PubLocalId ?? (object)DBNull.Value);

                                // TVP
                                var tvp = new SqlParameter("@WB2Data", SqlDbType.Structured)
                                {
                                    TypeName = "Type_WB2",
                                    Value = ToWB2DataTable(model.WB2Data)
                                };
                                cmd.Parameters.Add(tvp);

                                // Return + Error params
                                var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                                {
                                    Direction = ParameterDirection.ReturnValue
                                };
                                cmd.Parameters.Add(returnParam);

                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 4000)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(errorParam);

                                await cmd.ExecuteNonQueryAsync();

                                string errorMsg = errorParam.Value?.ToString();
                                int returnVal = returnParam.Value != DBNull.Value ? Convert.ToInt32(returnParam.Value) : 0;

                                if (returnVal <= 0)
                                {
                                    transaction.Rollback();
                                    return (false, errorMsg ?? "Save/Update failed");
                                }

                                if (model.SaveOrUpdate != "Save")
                                {
                                    string clearGateLinkQuery = @" UPDATE GATE1 SET WB_TYPE = NULL, WB_NO = NULL WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE  AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                                      
                                    using (SqlCommand clearCmd = new SqlCommand(clearGateLinkQuery, con, transaction))
                                    {
                                        clearCmd.Parameters.AddWithValue("@V_TYPE", model.oldGateType ?? (object)DBNull.Value);
                                        clearCmd.Parameters.AddWithValue("@V_NO", model.oldGateNo ?? (object)DBNull.Value);
                                        clearCmd.Parameters.AddWithValue("@COMP_CODE", session.PubCompCode ?? (object)DBNull.Value);
                                        clearCmd.Parameters.AddWithValue("@BRANCH_CODE", session.PubBranchCode);
                                        clearCmd.Parameters.AddWithValue("@YEAR_CODE", session.PubFYearCode ?? (object)DBNull.Value);

                                        await clearCmd.ExecuteNonQueryAsync();
                                    }
                                }

                                string updateGateQuery = @"UPDATE GATE1 SET WB_TYPE = @WB_TYPE,WB_NO = @WB_NO WHERE V_TYPE = @GATE_TYPE AND V_NO = @GATE_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                                using (SqlCommand updateGateCmd = new SqlCommand(updateGateQuery, con, transaction))
                                {
                                    updateGateCmd.Parameters.AddWithValue("@WB_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                                    updateGateCmd.Parameters.AddWithValue("@WB_NO", model.V_NO ?? (object)DBNull.Value);
                                    updateGateCmd.Parameters.AddWithValue("@GATE_TYPE", model.GATE_TYPE ?? (object)DBNull.Value);
                                    updateGateCmd.Parameters.AddWithValue("@GATE_NO", model.GATE_NO ?? (object)DBNull.Value);
                                    updateGateCmd.Parameters.AddWithValue("@COMP_CODE", session.PubCompCode ?? (object)DBNull.Value);
                                    updateGateCmd.Parameters.AddWithValue("@BRANCH_CODE", session.PubBranchCode);
                                    updateGateCmd.Parameters.AddWithValue("@YEAR_CODE", session.PubFYearCode ?? (object)DBNull.Value);

                                    await updateGateCmd.ExecuteNonQueryAsync();
                                }

                                transaction.Commit();

                                string action = model.SaveOrUpdate == "Save" ? "ADD" : "UPDATE";
                                _logService.InsertLog("WB1", "BigWeighbridge", "Transaction", action, model.V_TYPE, model.V_NO.ToString(),
                                model.V_DATE);

                                _logService.InsertLog("WB2", "BigWeighbridge", "Transaction", action, model.V_TYPE, model.V_NO.ToString(),
                                 model.V_DATE);

                                if (action != "ADD")
                                {
                                    _globalValidationdate.LogInsertUpdateDelete(destinationTable: "WB1", sourceTable: "WB1", transactionType: "Transaction",
                                            codeVNo: model.V_NO.ToString(), vtype: model.V_TYPE);
                                    _globalValidationdate.LogInsertUpdateDelete(destinationTable: "WB2", sourceTable: "WB2", transactionType: "Transaction",
                                            codeVNo: model.V_NO.ToString(), vtype: model.V_TYPE);
                                }

                                return (true, "Data saved/updated successfully");
                            }
                            
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return (false, "Transaction failed: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, "Error: " + ex.Message);
            }
        }

        private DataTable ToWB2DataTable(List<TypeWB2> items)
        {
            var table = new DataTable();

            table.Columns.Add("V_SHIFT", typeof(string));
            table.Columns.Add("TYPE", typeof(string));
            table.Columns.Add("WEIGHT", typeof(decimal));
            table.Columns.Add("TARE_WGT", typeof(decimal));
            table.Columns.Add("NET_WGT", typeof(decimal));
            table.Columns.Add("WGT_DATE", typeof(DateTime));
            table.Columns.Add("WGT_TIME", typeof(string));
            table.Columns.Add("FROM_PLACE", typeof(int));
            table.Columns.Add("FROM_NAME", typeof(string));
            table.Columns.Add("TO_PLACE", typeof(int));
            table.Columns.Add("TO_NAME", typeof(string));
            table.Columns.Add("ITEM_CODE", typeof(int));
            table.Columns.Add("ITEM_NAME", typeof(string));
            table.Columns.Add("REMARKS", typeof(string));
            table.Columns.Add("STATUS", typeof(string));
            table.Columns.Add("Ref_type", typeof(string));
            table.Columns.Add("Ref_no", typeof(int));
            table.Columns.Add("SNO", typeof(int));
            table.Columns.Add("wb_time", typeof(string));
            table.Columns.Add("COND", typeof(string));
            table.Columns.Add("MOIS_PER", typeof(decimal));
            table.Columns.Add("MOIS_WT", typeof(decimal));

            foreach (var item in items ?? new List<TypeWB2>())
            {
                if (item.WEIGHT != null && item.WGT_DATE != null && item.WGT_TIME != null)
                {
                    decimal netWeight = Math.Abs(item.NET_WGT);

                    table.Rows.Add(
                        item.V_SHIFT, item.TYPE, item.WEIGHT, item.TARE_WGT, netWeight,
                        item.WGT_DATE, item.WGT_TIME, item.FROM_PLACE, item.FROM_NAME,
                        item.TO_PLACE, item.TO_NAME, item.ITEM_CODE, item.ITEM_NAME,
                        item.REMARKS, item.STATUS, item.Ref_type, item.Ref_no,
                        item.SNO, item.WGT_TIME, item.COND, item.MOIS_PER, item.MOIS_WT
                    );
                }
            }

            return table;
        }

        public async Task<(bool Status, string Message)> ValidateMRNAsync(WBEntryModel model)
        {
            var session = _globalVariableService.GetGlobalVariables();

            using (var con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (var transaction = con.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("[dbo].[sp_WBEntry]", con, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Action", "Validate_MRN");
                            cmd.Parameters.AddWithValue("@COMP_CODE", session.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", session.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", session.PubFYearCode);
                            cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                            cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);

                            var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                            {
                                Direction = ParameterDirection.ReturnValue
                            };

                            var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 5000)
                            {
                                Direction = ParameterDirection.Output
                            };

                            cmd.Parameters.Add(returnParam);
                            cmd.Parameters.Add(errorParam);

                            await cmd.ExecuteNonQueryAsync();

                            int result = Convert.ToInt32(returnParam.Value ?? 0);
                            string error = errorParam.Value?.ToString();

                            if (result <= 0)
                            {
                                transaction.Rollback();
                                return (false, error ?? "MRN validation failed");
                            }

                            transaction.Commit();
                            return (true, "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return (false, ex.Message);
                    }
                }
            }
        }

    }
}
