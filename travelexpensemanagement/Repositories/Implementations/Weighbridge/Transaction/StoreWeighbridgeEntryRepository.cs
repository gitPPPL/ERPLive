using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.Weighbridge.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Weighbridge.Transaction
{
    public class StoreWeighbridgeEntryRepository : IStoreWeighbridgeEntryRepository
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly DbHelper _dbHelper;
        private readonly LogService.LogService _logService;
        private readonly GlobalValidationdate _globalValidationdate;
        public StoreWeighbridgeEntryRepository(DataBaseConnection dbcontext, GlobalVariableService globalValue, 
            DbHelper dbHelper, LogService.LogService logService, GlobalValidationdate globalValidationdate)
        {
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _dbHelper = dbHelper;
            _logService = logService;
            _globalValidationdate = globalValidationdate;
        }

        public async Task<RepositoryResponseData<WeighBridgeEntryDto>> getStoreWbById(string id)
        {
            var response = new RepositoryResponseData<WeighBridgeEntryDto>();

            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@DOC_ID", id},
                    {"@Action", "WBEntryHeaderData"}
                };
                var parameter1 = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@DOC_ID", id},
                    {"@Action", "WBEntryDetailData"}
                };

                var headerlist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetWBEntry]", parameter);
                var detaillist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetWBEntry]", parameter1);

                response.status = true;
                response.data = new WeighBridgeEntryDto
                {
                    Header = headerlist.ToList(),
                    Detail = detaillist.ToList()
                };
                return response;
                //return Json(new { status = true, header = headerlist, detail = detaillist });

            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = "data load failed";
                return response;
                //return Json(new { status = false, message = "data load failed" });
            }
        }

        public async Task<RepositoryResponse> saveOrUpdate(WBEntryModel model)
        {
            var response = new RepositoryResponse();
            if (model == null)
            {
                response.status = false;
                response.message = "Data save failed.";
                return response;
                //return Json(new { status = false, message = "Data save failed." });
            }
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();

                    using (var transaction = con.BeginTransaction())
                    {
                        bool success = true;
                        string mode = "";
                        try
                        {
                            //var wgtDt = Convert.ToDateTime(model.V_DATE).ToString("dd-MMM-yyyy");
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_WBEntry]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                //cmd.Parameters.AddWithValue("@Action", model.SaveOrUpdate == "Save" ? "Add" : "Edit");
                                if (model.SaveOrUpdate == "Save")
                                {
                                    cmd.Parameters.AddWithValue("@Action", "Add");
                                    mode = "Insert";
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@Action", "Edit");
                                    mode = "Update";
                                }

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
                                var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                                {
                                    Direction = ParameterDirection.ReturnValue
                                };
                                cmd.Parameters.Add(returnParam);
                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 54000)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(errorParam);
                                await cmd.ExecuteNonQueryAsync();

                                string errorMessage = errorParam.Value?.ToString();
                                if ((int)returnParam.Value <= 0)
                                    success = false;
                            }
                            
                            if (success)
                            {
                                transaction.Commit();
                                //=========================================Uncomment after final===============================
                                //_logService.InsertLog("WB1", "Store WeighBridge Entry", "Transaction", mode, model.V_TYPE, model.V_NO.ToString(), model.V_DATE);
                                //_logService.InsertLog("WB2", "Store WeighBridge Entry", "Transaction", mode, model.V_TYPE, model.V_NO.ToString(), model.V_DATE);
                                //if(mode != "Insert")
                                //{
                                //    _globalValidationdate.LogInsertUpdateDelete(destinationTable: "WB1", sourceTable: "WB1", transactionType: "Transaction",
                                //            codeVNo: model.V_NO.ToString(), vtype: model.V_TYPE);
                                //    _globalValidationdate.LogInsertUpdateDelete(destinationTable: "WB2", sourceTable: "WB2", transactionType: "Transaction",
                                //            codeVNo: model.V_NO.ToString(), vtype: model.V_TYPE);
                                //}
                                //=================================================
                            }
                            else
                                transaction.Rollback();

                            response.status = success;
                            response.message = success ? "Data save/update successfully." : "Failed to save or update some entry details.";
                            return response;
                            //return Json(new
                            //{
                            //    status = success,
                            //    message = success ? "Data save/update successfully." : "Failed to save or update some entry details."
                            //});
                        }
                        catch (Exception ex)
                        {
                            transaction?.Rollback();
                            response.status = false;
                            response.message = "Transaction failed: " + ex.Message;
                            return response;
                            //return Json(new { status = false, message = "Transaction failed: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = "Error: " + ex.Message;
                return response;
                //return Json(new { status = false, message = "Error: " + ex.Message });
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
                if (item.WEIGHT > 0)
                {
                    table.Rows.Add(
                                       item.V_SHIFT, item.TYPE, item.WEIGHT, item.TARE_WGT, item.NET_WGT,
                                       item.WGT_DATE, item.WGT_TIME, item.FROM_PLACE, item.FROM_NAME,
                                       item.TO_PLACE, item.TO_NAME, item.ITEM_CODE, item.ITEM_NAME,
                                       item.REMARKS, item.STATUS, item.Ref_type, item.Ref_no,
                                       srno, item.wb_time, item.COND, item.MOIS_PER, item.MOIS_WT
                    );
                    srno++;
                }

            }

            return table;
        }
    }
}
