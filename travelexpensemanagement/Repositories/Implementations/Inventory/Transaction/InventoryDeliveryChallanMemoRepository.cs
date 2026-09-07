using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Inventory.Transaction
{
    public class InventoryDeliveryChallanMemoRepository : IInventoryDeliveryChallanMemoRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly LogService.LogService _logService;
        public InventoryDeliveryChallanMemoRepository(GlobalValidationdate globalValidationdate, GlobalVariableService globalVariableService, 
            DataBaseConnection dbConnection, LogService.LogService logService)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
            _logService = logService;
        }
        const string doctype = "GTMO";
        public RepositoryResponse SaveDeliveryChallanMemo(InventoryDeliveryChallanMemoModel model)
        {
            if (model == null || model.items == null)
            {
                return new RepositoryResponse { status = false, message = "Invalid request" };
            }
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            //Header
                            string mode = "";
                            string action = model.ACTION == "INSERT" ? "HEADERINSERT" : "UPDATE";

                            using (SqlCommand headCmd = new SqlCommand("sp_InventoryDeliveryChallanMemo", con, tran))
                            {
                                headCmd.CommandType = CommandType.StoredProcedure;

                                headCmd.Parameters.AddWithValue("@Action", action);
                                headCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                headCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                headCmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                headCmd.Parameters.AddWithValue("@DOC_ID", doctype + model.V_NO);
                                headCmd.Parameters.AddWithValue("@V_TYPE", doctype);

                                headCmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                                headCmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);
                                headCmd.Parameters.AddWithValue("@EMP_CODE", (object?)model.EMP_CODE ?? DBNull.Value);
                                headCmd.Parameters.AddWithValue("@EMP_NAME", (object?)model.EMP_NAME ?? DBNull.Value);
                                headCmd.Parameters.AddWithValue("@VENDOR_CODE", (object?)model.VENDOR_CODE ?? DBNull.Value);
                                headCmd.Parameters.AddWithValue("@VENDOR_NAME", (object?)model.VENDOR_NAME ?? DBNull.Value);
                                headCmd.Parameters.AddWithValue("@TRANSPORT_CODE", (object?)model.TRANSPORT_CODE ?? DBNull.Value);
                                headCmd.Parameters.AddWithValue("@TRANSPORT_NAME", (object?)model.TRANSPORT_NAME ?? DBNull.Value);
                                headCmd.Parameters.AddWithValue("@THROUGH", (object?)model.THROUGH ?? DBNull.Value);
                                headCmd.Parameters.AddWithValue("@RETURN_DATE", (object?)model.RETURN_DATE ?? DBNull.Value);
                                headCmd.Parameters.AddWithValue("@REMARKS", (object?)model.REMARKS ?? DBNull.Value);
                                headCmd.Parameters.AddWithValue("@STATUS", (object?)model.STATUS ?? DBNull.Value);

                                // Audit fields
                                if (model.ACTION == "INSERT")
                                {
                                    headCmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                    mode = "INSERT";
                                }
                                else
                                {
                                    headCmd.Parameters.AddWithValue("@EUSER", gv.PubUserId);
                                    mode = "UPDATE";
                                }
                                headCmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                headCmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                headCmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                headCmd.Parameters.AddWithValue("@ACTIVE", (object?)model.ACTIVE ?? DBNull.Value);

                                headCmd.ExecuteNonQuery();
                            }

                            //Footer
                            string delQry = $@"DELETE FROM GATE_MEMO2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = 
                                                @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                            using (SqlCommand cmd = new SqlCommand(delQry, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                                cmd.Parameters.AddWithValue("@V_TYPE", doctype);
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                                cmd.ExecuteNonQuery();
                            }

                            int i = 1;
                            foreach (var item in model.items)
                            {
                                using (SqlCommand itemCmd = new SqlCommand("sp_InventoryDeliveryChallanMemo", con, tran))
                                {
                                    itemCmd.CommandType = CommandType.StoredProcedure;
                                    itemCmd.Parameters.AddWithValue("@Action", "FOOTERINSERT");

                                    // Common/Header parameters
                                    itemCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                    itemCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                    itemCmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                    itemCmd.Parameters.AddWithValue("@DOC_ID", doctype + model.V_NO);
                                    itemCmd.Parameters.AddWithValue("@V_TYPE", doctype);

                                    itemCmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                                    itemCmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);

                                    // Footer/Item parameters
                                    itemCmd.Parameters.AddWithValue("@ITEM_CODE", (object?)item.ITEM_CODE ?? DBNull.Value);
                                    itemCmd.Parameters.AddWithValue("@ITEM_NAME", (object?)item.ITEM_NAME ?? DBNull.Value);
                                    itemCmd.Parameters.AddWithValue("@NOS", (object?)item.NOS ?? DBNull.Value);
                                    itemCmd.Parameters.AddWithValue("@APPROX_AMT", (object?)item.APPROX_AMT ?? DBNull.Value);
                                    itemCmd.Parameters.AddWithValue("@QTY", (object?)item.QTY ?? DBNull.Value);
                                    itemCmd.Parameters.AddWithValue("@UNIT_CODE", (object?)item.UNIT_CODE ?? DBNull.Value);
                                    itemCmd.Parameters.AddWithValue("@UNIT_NAME", (object?)item.UNIT_NAME ?? DBNull.Value);
                                    itemCmd.Parameters.AddWithValue("@REMARKS", (object?)item.REMARKS ?? DBNull.Value);
                                    itemCmd.Parameters.AddWithValue("@SNO", i++);
                                    itemCmd.Parameters.AddWithValue("@ACTIVE", (object?)item.ACTIVE ?? DBNull.Value);

                                    // Audit parameters
                                    itemCmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                    itemCmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                    itemCmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                    itemCmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                    itemCmd.ExecuteNonQuery();
                                }
                            }

                            tran.Commit();
                            //Log Service
                            //_logService.InsertLog("GATE_MEMO1", "Delivery Challan Memo", "Transaction", mode, doctype, model.V_NO.ToString(), model.V_DATE);
                            //_logService.InsertLog("GATE_MEMO2", "Delivery Challan Memo", "Transaction", mode, doctype, model.V_NO.ToString(), model.V_DATE);

                            return new RepositoryResponse { status = true, message = "Saved Successfully!" };
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            return new RepositoryResponse { status = false, message = ex.Message };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }
        public RepositoryResponseData<InventoryDeliveryChallanMemoModel> GetDataById(string docId)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new InventoryDeliveryChallanMemoModel();
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_InventoryDeliveryChallanMemo", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetById");
                        cmd.Parameters.AddWithValue("@V_NO", docId);
                        cmd.Parameters.AddWithValue("@V_TYPE", doctype);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                data.V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]);
                                data.RETURN_DATE = reader["RETURN_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["RETURN_DATE"]);
                                data.EMP_CODE = reader["EMP_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["EMP_CODE"]);
                                data.VENDOR_CODE = reader["VENDOR_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["VENDOR_CODE"]);
                                data.TRANSPORT_NAME = reader["TRANSPORT_NAME"]?.ToString();
                                data.THROUGH = reader["THROUGH"]?.ToString();
                                data.REMARKS = reader["REMARKS"]?.ToString();
                            }

                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    var item = new InventoryDeliveryChallanMemoItemsModel
                                    {
                                        ITEM_CODE = reader["ITEM_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["ITEM_CODE"]),
                                        ITEM_NAME = reader["ITEM_NAME"] == DBNull.Value ? null : reader["ITEM_NAME"].ToString(),
                                        UNIT_NAME = reader["UNIT_NAME"]?.ToString(),
                                        UNIT_CODE = reader["UNIT_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["UNIT_CODE"]),
                                        NOS = reader["NOS"] == DBNull.Value ? null : Convert.ToInt32(reader["NOS"]),
                                        QTY = reader["QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["QTY"]),
                                        APPROX_AMT = reader["APPROX_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["APPROX_AMT"]),
                                        REMARKS = reader["REMARKS"]?.ToString()
                                    };

                                    data.items.Add(item);
                                }
                            }
                        }
                    }
                }
                return new RepositoryResponseData<InventoryDeliveryChallanMemoModel> { status = true, data = data };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<InventoryDeliveryChallanMemoModel> { status = false, message = ex.Message };
            }
        }
    }
}
