using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Weighbridge.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge;

namespace travelexpensemanagement.Repositories.Implementations.Weighbridge
{
    public class InHouseWeighbridgeEntryRepository : IInHouseWeighbridgeEntryRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DbHelper _dbHelper;
        public InHouseWeighbridgeEntryRepository(DataBaseConnection dbConnection,GlobalVariableService globalVariableService,GlobalValidationdate globalValidationdate,DbHelper dbHelper)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dbHelper = dbHelper;
        }
        public async Task<IActionResult> SaveOrUpdateInHouseWeighBridgeEntryasync(WBEntryModel model)
        {
            if (model == null)
            {
                return new JsonResult(new {  status = false,  message = "Model data is null." });
            }
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalVariableService.GetGlobalVariables();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            bool success = true;
                            string errorMessage = string.Empty;

                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_WBEntry]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue(
                                    "@Action",
                                    model.SaveOrUpdate == "Save" ? "Add" : "Edit"
                                );
           
                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
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
                                cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId ?? (object)DBNull.Value);
        
                                var tvp = new SqlParameter("@WB2Data", SqlDbType.Structured)
                                {
                                    TypeName = "Type_WB2",
                                    Value = ToWB2DataTable(model.WB2Data)
                                };

                                cmd.Parameters.Add(tvp);

                                // Return Parameter
                                var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                                {
                                    Direction = ParameterDirection.ReturnValue
                                };

                                cmd.Parameters.Add(returnParam);

                                // Output Parameter
                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 54000)
                                {
                                    Direction = ParameterDirection.Output
                                };

                                cmd.Parameters.Add(errorParam);

                                // Execute
                                await cmd.ExecuteNonQueryAsync();

                                int result = returnParam.Value != DBNull.Value
                                    ? Convert.ToInt32(returnParam.Value)
                                    : 0;

                                errorMessage = errorParam.Value?.ToString() ?? "";

                                if (result <= 0)
                                {
                                    success = false;
                                }
                            }

                            if (success)
                            {
                                await transaction.CommitAsync();

                                if (model.SaveOrUpdate == "Update")
                                {
                                    _globalValidationdate.LogInsertUpdateDelete(
                                        destinationTable: "WB1",
                                        sourceTable: "WB1",
                                        transactionType: "Transaction",
                                        codeVNo: model.V_NO.ToString(),
                                        vtype: model.V_TYPE
                                    );

                                    _globalValidationdate.LogInsertUpdateDelete(
                                        destinationTable: "WB2",
                                        sourceTable: "WB2",
                                        transactionType: "Transaction",
                                        codeVNo: model.V_NO.ToString(),
                                        vtype: model.V_TYPE
                                    );
                                }
                                return new JsonResult(new  { status = true, message = "Data saved/updated successfully." });
                            }
                            else
                            {
                                await transaction.RollbackAsync();
                                return new JsonResult(new  {  status = false,  message = "Failed to save/update data."  });
                            }
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();

                            return new JsonResult(new  { status = false, message = "Transaction failed : " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new {  status = false,  message = "Error : " + ex.Message  });
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

            int srno = 1;

            foreach (var item in items ?? new List<TypeWB2>())
            {
                table.Rows.Add(
                    item.V_SHIFT ?? (object)DBNull.Value,
                    item.TYPE ?? (object)DBNull.Value,
                    item.WEIGHT,
                    item.TARE_WGT,
                    item.NET_WGT,
                    item.WGT_DATE,
                    item.WGT_TIME ?? (object)DBNull.Value,
                    item.FROM_PLACE,
                    item.FROM_NAME ?? (object)DBNull.Value,
                    item.TO_PLACE,
                    item.TO_NAME ?? (object)DBNull.Value,
                    item.ITEM_CODE,
                    item.ITEM_NAME ?? (object)DBNull.Value,
                    item.REMARKS ?? (object)DBNull.Value,
                    item.STATUS ?? (object)DBNull.Value,
                    item.Ref_type ?? (object)DBNull.Value,
                    item.Ref_no,
                    srno,
                    item.wb_time ?? (object)DBNull.Value,
                    item.COND ?? (object)DBNull.Value,
                    item.MOIS_PER,
                    item.MOIS_WT
                );

                srno++;
            }

            return table;
        }


    }
}