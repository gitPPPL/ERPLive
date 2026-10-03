using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class SalesReturnListRepository : ISalesReturnListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly LogService.LogService _logService;
        public SalesReturnListRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _logService = logService;
        }
        public RepositoryResponseList<SalesReturnListModel> GetSalesReturnList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new List<SalesReturnListModel>();
            int totalcount = 0;
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_SalesReturn", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "Select");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        //cmd.Parameters.AddWithValue("@V_TYPE", "SORD");

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                data.Add(new SalesReturnListModel
                                {
                                    V_NO = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]),
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]),

                                    BILL_NAME = reader["BILL_NAME"]?.ToString(),
                                    BILL_ADD1 = reader["BILL_ADD1"]?.ToString(),
                                    BILL_ADD2 = reader["BILL_ADD2"]?.ToString(),
                                    BILL_ADD3 = reader["BILL_ADD3"]?.ToString(),
                                    BILL_CITYNAME = reader["BILL_CITYNAME"]?.ToString(),

                                    AGENT_NAME = reader["AGENT_NAME"]?.ToString(),

                                    SHIP_NAME = reader["SHIP_NAME"]?.ToString(),
                                    SHIP_ADD1 = reader["SHIP_ADD1"]?.ToString(),
                                    SHIP_ADD2 = reader["SHIP_ADD2"]?.ToString(),
                                    SHIP_ADD3 = reader["SHIP_ADD3"]?.ToString(),
                                    SHIP_CITYNAME = reader["SHIP_CITYNAME"]?.ToString(),

                                    TAX_NAME = reader["TAX_NAME"]?.ToString(),
                                    ITEM_TYPE = reader["ITEM_TYPE"]?.ToString(),

                                    WB_NO = reader["WB_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["WB_NO"]),
                                    PACK_NO = reader["PACK_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["PACK_NO"]),

                                    AMOUNT = reader["AMOUNT"] == DBNull.Value ? null : Convert.ToDecimal(reader["AMOUNT"]),
                                    PACK_PER = reader["PACK_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["PACK_PER"]),
                                    PACK_AMT = reader["PACK_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["PACK_AMT"]),

                                    NAMOUNT = reader["NAMOUNT"] == DBNull.Value ? null : Convert.ToDecimal(reader["NAMOUNT"]),

                                    CGST_PER = reader["CGST_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["CGST_PER"]),
                                    CGST_AMT = reader["CGST_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["CGST_AMT"]),
                                    SGST_PER = reader["SGST_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["SGST_PER"]),
                                    SGST_AMT = reader["SGST_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["SGST_AMT"]),
                                    IGST_PER = reader["IGST_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["IGST_PER"]),
                                    IGST_AMT = reader["IGST_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["IGST_AMT"]),
                                    CESS_PER = reader["CESS_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["CESS_PER"]),
                                    CESS_AMT = reader["CESS_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["CESS_AMT"]),

                                    LOAD_PER = reader["LOAD_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["LOAD_PER"]),
                                    LOAD_AMT = reader["LOAD_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["LOAD_AMT"]),
                                    LOAD_AC = reader["LOAD_AC"]?.ToString(),

                                    WB_AMT = reader["WB_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["WB_AMT"]),
                                    WB_AC = reader["WB_AC"]?.ToString(),

                                    TOT_NOS = reader["TOT_NOS"] == DBNull.Value ? null : Convert.ToInt32(reader["TOT_NOS"]),
                                    FRT_AMT = reader["FRT_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["FRT_AMT"]),
                                    ROUND_OFF = reader["ROUND_OFF"] == DBNull.Value ? null : Convert.ToDecimal(reader["ROUND_OFF"]),
                                    TOT_NET = reader["TOT_NET"] == DBNull.Value ? null : Convert.ToDecimal(reader["TOT_NET"]),

                                    INSU_PER = reader["INSU_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["INSU_PER"]),
                                    INSU_AMT = reader["INSU_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["INSU_AMT"]),

                                    TDS_PER = reader["TDS_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["TDS_PER"]),
                                    TDS_AMT = reader["TDS_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["TDS_AMT"]),

                                    LOAD_REM = reader["LOAD_REM"]?.ToString(),
                                    WB_REM = reader["WB_REM"]?.ToString(),

                                    TOT_GROSS = reader["TOT_GROSS"] == DBNull.Value ? null : Convert.ToDecimal(reader["TOT_GROSS"]),

                                    GR_NO = reader["GR_NO"]?.ToString(),
                                    GR_DATE = reader["GR_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["GR_DATE"]),
                                    VEHICLE_NO = reader["VEHICLE_NO"]?.ToString(),

                                    TRANSPORT_CODE = reader["TRANSPORT_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["TRANSPORT_CODE"]),
                                    TRANSPORT_NAME = reader["TRANSPORT_NAME"]?.ToString(),

                                    DRIVER_NAME = reader["DRIVER_NAME"]?.ToString(),
                                    DRIVER_NO = reader["DRIVER_NO"]?.ToString(),

                                    WAYBILL_NO = reader["WAYBILL_NO"]?.ToString(),
                                    REMARK = reader["REMARK"]?.ToString(),

                                    FRT_TOPAY = reader["FRT_TOPAY"] == DBNull.Value ? null : Convert.ToDecimal(reader["FRT_TOPAY"]),
                                    WB_QTY = reader["WB_QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["WB_QTY"]),

                                    DISC_PER = reader["DISC_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["DISC_PER"]),
                                    DISC_AMT = reader["DISC_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["DISC_AMT"])
                                });
                            }

                            if (reader.NextResult())
                            {
                                if (reader.Read())
                                {
                                    totalcount = reader["TotalCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalCount"]);
                                }
                            }
                        }
                    }
                }


                return new RepositoryResponseList<SalesReturnListModel> { status = true, data = data, totalCount = totalcount };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<SalesReturnListModel> { status = false, message = ex.Message };
            }
        }
        public RepositoryResponseData<SalesEditStatus> GetSalesEditStatus(string vType, int vNo)
        {
            try
            {
                var result = new SalesEditStatus();
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_SalesReturn", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "EditStatusBeforeEdit");
                    cmd.Parameters.AddWithValue("@v_type", vType);
                    cmd.Parameters.AddWithValue("@v_no", vNo);
                    cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@branch_code", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@year_code", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@USER_CODE", gv.PubUserId);

                    if (con.State != ConnectionState.Open)
                        con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        // 1. Approval in process
                        if (dr.Read())
                        {
                            result.ApprovalUser = dr["OpenApprovalUser"] == DBNull.Value ? "" : dr["OpenApprovalUser"].ToString();
                            result.IsApprovalInProcess = dr["status"] != DBNull.Value && dr["status"].ToString().Equals("OPEN", StringComparison.OrdinalIgnoreCase);
                        }

                        // 2. Approval stage / EA Flag
                        if (dr.NextResult() && dr.Read())
                        {
                            result.IsFinalApprovalBody = dr["APPROV_USER"] != DBNull.Value;
                            result.EAFlag = dr["EAFlag"] == DBNull.Value ? "" : dr["EAFlag"].ToString()?.Trim();
                        }

                        // 3. Sale1
                        if (dr.NextResult() && dr.Read())
                        {
                            result.FAProvStatus = dr["FAPROV_STATUS"] == DBNull.Value ? "" : dr["FAPROV_STATUS"].ToString()?.Trim();
                            result.IsApproved = result.FAProvStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase);
                            result.Status = dr["Status"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Status"]);
                            string signedQr = dr["signed_qr"] == DBNull.Value ? "" : dr["signed_qr"].ToString()?.Trim();
                            string irn = dr["IRN"] == DBNull.Value ? "" : dr["IRN"].ToString()?.Trim();

                            // =========================================
                            // E-INVOICE - EXACT VB LOGIC
                            // =========================================
                            if (!string.IsNullOrWhiteSpace(signedQr))
                            {
                                // Cancelled
                                if (result.Status == 2)
                                {
                                    result.CanEdit = false;
                                    result.Message = "E-Invoice of this Voucher has been Cancelled, Modification not allowed.";
                                    result.MessageType = "stop";
                                }
                                // E-Invoice generated
                                else if (!string.IsNullOrWhiteSpace(irn))
                                {
                                    if (gv.PubUserLevel != "1")
                                    {
                                        result.CanEdit = false;
                                        result.Message = "E-Invoice generated, Modification not allowed.";
                                        result.MessageType = "stop";
                                    }
                                    else
                                    {
                                        // UserLevel 1 -> warning only
                                        result.EInvoiceWarning = true;
                                        result.EInvoiceMessage = "Alert ! E-Invoice generated.";
                                    }
                                }
                            }
                        }
                    }

                    // =========================================
                    // APPROVAL VALIDATION - EXACT VB LOGIC
                    // =========================================
                    if (gv.PubUserLevel != "1")
                    {
                        // Approved document
                        if (!result.IsFinalApprovalBody && result.IsApproved)
                        {
                            result.CanEdit = false;
                            result.Message = "This Document has been Approved, Edit not allowed.";
                            result.MessageType = "stop";
                        }

                        // Approval in process
                        if (result.IsApprovalInProcess)
                        {
                            result.CanEdit = false;
                            result.Message = "This Document Approval is in process at User:" + result.ApprovalUser + "., Edit not allowed.";
                            result.MessageType = "stop";
                        }
                    }

                    return new RepositoryResponseData<SalesEditStatus> { status = true, data = result };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<SalesEditStatus> { status = false, message = ex.Message };
            }
        }
        public RepositoryResponseData<SalesDeleteStatus> GetSalesDeleteStatus(string vType, int vNo)
        {
            try
            {
                var result = new SalesDeleteStatus();
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_SalesReturn", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "DeleteStatus");
                    cmd.Parameters.AddWithValue("@v_type", vType);
                    cmd.Parameters.AddWithValue("@v_no", vNo);
                    cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@branch_code", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@year_code", gv.PubFYearCode);

                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        // 1. E-Invoice
                        if (dr.Read())
                        {
                            bool eInvoiceExists = dr["EInvoiceExists"] != DBNull.Value && Convert.ToInt32(dr["EInvoiceExists"]) == 1;

                            if (eInvoiceExists)
                            {
                                result.CanDelete = false;
                                result.Message = "E-Invoice generated, Deletion not allowed.";

                                return new RepositoryResponseData<SalesDeleteStatus> { status = true, data = result };
                            }
                        }

                        // 2. Gate
                        if (dr.NextResult() && dr.Read())
                        {
                            result.GateExists = true;
                            result.GateMessage = "This document exists in Gate Serial No :" + dr["v_no"] + " dated :" + dr["v_date"];

                            return new RepositoryResponseData<SalesDeleteStatus> { status = true, data = result };
                        }

                        // 3. Ledger
                        if (dr.NextResult() && dr.Read())
                        {
                            result.LedgerExists = true;
                            result.LedgerMessage = "This document exists in Ledger Serial No :" + dr["v_no"] + " dated :" + dr["v_date"];

                            return new RepositoryResponseData<SalesDeleteStatus> { status = true, data = result };
                        }
                    }
                }
                return new RepositoryResponseData<SalesDeleteStatus> { status = true, data = result };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<SalesDeleteStatus> { status = false, message = ex.Message };
            }
        }
        public RepositoryResponse DeleteSales(string vType, int vNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("sp_SalesReturn", con, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@Action", "DeleteSales");
                                cmd.Parameters.AddWithValue("@v_type", vType);
                                cmd.Parameters.AddWithValue("@v_no", vNo);
                                cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@branch_code", gv.PubBranchCode);
                                cmd.Parameters.AddWithValue("@year_code", gv.PubFYearCode);

                                cmd.ExecuteNonQuery();

                                tran.Commit();
                                //_logService.InsertLog("SALE1", "SALES RETURN", "Transaction", "Delete", vType, vNo.ToString(), null);
                                //_logService.InsertLog("SALE2", "SALES RETURN", "Transaction", "Delete", vType, vNo.ToString(), null);
                                //_logService.InsertLog("LEDGER2", "SALES RETURN", "Transaction", "Delete", vType, vNo.ToString(), null);
                            }
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            return new RepositoryResponse { status = false, message = "Error in Record deleting." };
                        }
                    }
                }
                return new RepositoryResponse { status = true, message = "Record deleted successfully." };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "Error in Record deleting." };
            }
        }
    }
}
