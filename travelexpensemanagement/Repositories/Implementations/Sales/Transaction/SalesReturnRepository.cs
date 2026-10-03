using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class SalesReturnRepository : ISalesReturnRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly LogService.LogService _logService;
        public SalesReturnRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _logService = logService;
        }

        public async Task<RepositoryResponseData<object>> GetReferenceDetails(string refValue)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var headerList = new List<Dictionary<string, object>>();
            var itemList = new List<Dictionary<string, object>>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_GetSaleDetailsByReference", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                //cmd.Parameters.Add("@VType", SqlDbType.VarChar).Value = refValue;
                //cmd.Parameters.Add("@VNo", SqlDbType.VarChar).Value = refText;
                cmd.Parameters.Add("@DOC_ID", SqlDbType.NVarChar).Value = refValue;
                cmd.Parameters.Add("@CompCode", SqlDbType.Int).Value = gv.PubCompCode;
                cmd.Parameters.Add("@BranchCode", SqlDbType.Int).Value = gv.PubBranchCode;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    // ===== HEADER =====
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        headerList.Add(row);
                    }

                    // ===== ITEMS =====
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            itemList.Add(row);
                        }
                    }
                }
            }
            return new RepositoryResponseData<object> { status = headerList.Count > 0, data = new { header = headerList, items = itemList } };
        }
        public RepositoryResponse Save(SalesReturn salesReturn)
        {

            if (salesReturn.FormData == null || salesReturn.RowData == null)
                return new RepositoryResponse { status = false, message = "Invalid request." };

            var gv = _globalVariableService.GetGlobalVariables();
            string resMessage = "";
            string logAction = "";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var tran = con.BeginTransaction();

            try
            {
                var f = salesReturn.FormData;
                var docId = $"{f.V_TYPE}{f.V_NO}";

                string delQry = @"DELETE FROM SALE2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND YEAR_CODE = @YEAR_CODE
                                                AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";
                using var deleteCmd = new SqlCommand(delQry, con, tran);

                deleteCmd.Parameters.AddWithValue("@V_NO", f.V_NO);
                deleteCmd.Parameters.AddWithValue("@V_TYPE", f.V_TYPE);
                deleteCmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                deleteCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                deleteCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                deleteCmd.ExecuteNonQuery();

                using (var cmd = new SqlCommand("sp_SalesReturn", con, tran))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (f.ACTION == "INSERT")
                    {
                        cmd.Parameters.AddWithValue("@ACTION", "HeaderInsert");
                        cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                        resMessage = "Data Saved Successfully!";
                        logAction = "Insert";
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@ACTION", "Update");
                        cmd.Parameters.AddWithValue("@EUSER", gv.PubUserId);
                        resMessage = "Data Updated Successfully!";
                        logAction = "Update";
                    }

                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", f.V_TYPE);
                    cmd.Parameters.AddWithValue("@V_NO", f.V_NO);
                    cmd.Parameters.AddWithValue("@V_DATE", f.V_DATE);
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);

                    cmd.Parameters.AddWithValue("@BILL_CODE", f.BILL_CODE);
                    cmd.Parameters.AddWithValue("@BILL_NAME", (object?)f.BILL_NAME ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BILL_ADD1", (object?)f.BILL_ADD1 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BILL_ADD2", (object?)f.BILL_ADD2 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BILL_ADD3", (object?)f.BILL_ADD3 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BILL_CITY", (object?)f.BILL_CITY ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BILL_STATE", (object?)f.BILL_STATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BILL_COUNTRY", (object?)f.BILL_COUNTRY ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BILL_CITYNAME", (object?)f.BILL_CITYNAME ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BILL_STATENAME", (object?)f.BILL_STATENAME ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BILL_COUNTRYNAME", (object?)f.BILL_COUNTRYNAME ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BILL_GST", (object?)f.BILL_GST ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BILL_PINCODE", (object?)f.BILL_PINCODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BILL_ADDRESSID", (object?)f.BILL_ADDRESSID ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@GODOWN_CODE", (object?)f.GODOWN_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_CODE", (object?)f.SHIP_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_NAME", (object?)f.SHIP_NAME ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_ADD1", (object?)f.SHIP_ADD1 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_ADD2", (object?)f.SHIP_ADD2 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_ADD3", (object?)f.SHIP_ADD3 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_CITY", (object?)f.SHIP_CITY ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_STATE", (object?)f.SHIP_STATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_COUNTRY", (object?)f.SHIP_COUNTRY ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_CITYNAME", (object?)f.SHIP_CITYNAME ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_STATENAME", (object?)f.SHIP_STATENAME ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_COUNTRYNAME", (object?)f.SHIP_COUNTRYNAME ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_GST", (object?)f.SHIP_GST ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_PINCODE", (object?)f.SHIP_PINCODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_ADDRESSID", (object?)f.SHIP_ADDRESSID ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@IMPORT_CURRENCY", (object?)f.IMPORT_CURRENCY ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EXRATE", (object?)f.EXRATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TAX_CODE", (object?)f.TAX_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PACK_TYPE", (object?)f.PACK_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PACK_NO", (object?)f.PACK_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ITEM_TYPE", (object?)f.ITEM_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AGENT_CODE", (object?)f.AGENT_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DEFECTIVE_GOODS", (object?)f.DEFECTIVE_GOODS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CAL_ONPCS", (object?)f.CAL_ONPCS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PRINT_DETAIL", (object?)f.PRINT_DETAIL ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@TOT_NOS", (object?)f.TOT_NOS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TOT_GROSS", (object?)f.TOT_GROSS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TOT_NET", (object?)f.TOT_NET ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WB_QTY", (object?)f.WB_QTY ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AMOUNT", (object?)f.AMOUNT ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@TCS_PER", (object?)f.TCS_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TCS_AMT", (object?)f.TCS_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PACK_PER", (object?)f.PACK_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PACK_AMT", (object?)f.PACK_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DISC_PER", (object?)f.DISC_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DISC_AMT", (object?)f.DISC_AMT ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@CGST_PER", (object?)f.CGST_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CGST_AMT", (object?)f.CGST_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SGST_PER", (object?)f.SGST_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SGST_AMT", (object?)f.SGST_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IGST_PER", (object?)f.IGST_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IGST_AMT", (object?)f.IGST_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CESS_PER", (object?)f.CESS_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CESS_AMT", (object?)f.CESS_AMT ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@ROUND_OFF", (object?)f.ROUND_OFF ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NAMOUNT", (object?)f.NAMOUNT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TDS_PER", (object?)f.TDS_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TDS_AMT", (object?)f.TDS_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FRT_AMT", (object?)f.FRT_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FRT_TOPAY", (object?)f.FRT_TOPAY ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@FRT_BILLNO", (object?)f.FRT_BILLNO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FRT_BILLDT", (object?)f.FRT_BILLDT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FRT_PASSDT", (object?)f.FRT_PASSDT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FRT_CHQ", (object?)f.FRT_CHQ ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FRT_REMARK", (object?)f.FRT_REMARK ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@TRANSPORT_CODE", (object?)f.TRANSPORT_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TRANSPORT_NAME", (object?)f.TRANSPORT_NAME ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GR_NO", (object?)f.GR_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GR_DATE", (object?)f.GR_DATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@VEHICLE_NO", (object?)f.VEHICLE_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DRIVER_NAME", (object?)f.DRIVER_NAME ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DRIVER_NO", (object?)f.DRIVER_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TPT_MODE", (object?)f.TPT_MODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TPT_DISTANCE", (object?)f.TPT_DISTANCE ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@REMARK", (object?)f.REMARK ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WB_TYPE", (object?)f.WB_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WB_NO", (object?)f.WB_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@INSU_TYPE", (object?)f.INSU_TYPE ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@GATE_TYPE", (object?)f.GATE_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GATE_NO", (object?)f.GATE_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@REF_TYPE", (object?)f.REF_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@REF_NO", (object?)f.REF_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@REF_DATE", (object?)f.REF_DATE ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@ISSUE_TYPE", (object?)f.ISSUE_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ISSUE_NO", (object?)f.ISSUE_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BUYER_ORDNO", (object?)f.BUYER_ORDNO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PLACE_RECEIPT", (object?)f.PLACE_RECEIPT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PORT_LOADING", (object?)f.PORT_LOADING ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PORT_DISCHARGE", (object?)f.PORT_DISCHARGE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FINAL_DEST", (object?)f.FINAL_DEST ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FINAL_DEST_COUNTRY", (object?)f.FINAL_DEST_COUNTRY ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@SAUDA_TYPE", (object?)f.SAUDA_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SAUDA_NO", (object?)f.SAUDA_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SAUDA_RATE", (object?)f.SAUDA_RATE ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@STATUS", 1);

                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd.Parameters.AddWithValue("@CDISC_AMT", (object?)f.CDISC_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CDISC_PER", (object?)f.CDISC_PER ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@SUPPLY_TYPE", (object?)f.SUPPLY_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LC_NO", (object?)f.LC_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TRADE_TERM", (object?)f.TRADE_TERM ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DISP_PLACE", (object?)f.DISP_PLACE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIPMENT_TYPE", (object?)f.SHIPMENT_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MODEOF_PAYMENT", (object?)f.MODEOF_PAYMENT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@INCOTERM", (object?)f.INCOTERM ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@FRT_TAXPER", (object?)f.FRT_TAXPER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FRT_TAXAMT", (object?)f.FRT_TAXAMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SB_NO", (object?)f.SB_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SB_DATE", (object?)f.SB_DATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PORT_CODE", (object?)f.PORT_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TRAN_TYPE", (object?)f.TRAN_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DEL_DATE", (object?)f.DEL_DATE ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@FOB_VALUE", (object?)f.FOB_VALUE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FOB_FRT", (object?)f.FOB_FRT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FOB_INSU", (object?)f.FOB_INSU ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FOB_OTHER", (object?)f.FOB_OTHER ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@BILLOF_LADING", (object?)f.BILLOF_LADING ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EXPV_TYPE", (object?)f.EXPV_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EXPV_NO", (object?)f.EXPV_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHIP_TYPE", (object?)f.SHIP_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CURRENCY", (object?)f.CURRENCY ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BANK_CODE", (object?)f.BANK_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DEL_SCH", (object?)f.DEL_SCH ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LUT_NO", (object?)f.LUT_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LUT_DATE", (object?)f.LUT_DATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PAY_TERM", (object?)f.PAY_TERM ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SOLD_BY", (object?)f.SOLD_BY ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@INV_STATUS", (object?)f.INV_STATUS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@INSUCR_DAYS", (object?)f.INSUCR_DAYS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CONTAINER_SIZE", (object?)f.CONTAINER_SIZE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PAYMENT_TERM", (object?)f.PAYMENT_TERM ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LICENCE_NO", (object?)f.LICENCE_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LICENCE_TYPE", (object?)f.LICENCE_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LICENCE_DATE", (object?)f.LICENCE_DATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FORM_CODE", (object?)f.FORM_CODE ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@RCM_NO", (object?)f.RCM_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PAYREF_DOCID", (object?)f.PAYREF_DOCID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PAY_AMT", (object?)f.PAY_AMT ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@DELIVERY_TERMS", (object?)f.DELIVERY_TERMS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LUT_DETAIL", (object?)f.LUT_DETAIL ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@INSU_DETAIL", (object?)f.INSU_DETAIL ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FAPROV_STATUS", (object?)f.FAPROV_STATUS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FAPROV_REMARKS", (object?)f.FAPROV_REMARKS ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@COND_DATE", (object?)f.COND_DATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@COND_MNTH", (object?)f.COND_MNTH ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@APPROVAL_USER", (object?)f.APPROVAL_USER ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@IRN", (object?)f.IRN ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SIGNED_JSON", (object?)f.SIGNED_JSON ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SIGNED_QR", (object?)f.SIGNED_QR ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EINVOICE_FLG", (object?)f.EINVOICE_FLG ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EWAYBILL_FLG", (object?)f.EWAYBILL_FLG ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EWAYBILL_NO", (object?)f.EWAYBILL_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EWAYBILL_JSON", (object?)f.EWAYBILL_JSON ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EWAYBILL_DATE", (object?)f.EWAYBILL_DATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EWAYBILL_VALIDDATE", (object?)f.EWAYBILL_VALIDDATE ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@INSU_PER", (object?)f.INSU_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@INSU_AMT", (object?)f.INSU_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@INSU_NO", (object?)f.INSU_NO ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@ORD_AMT", (object?)f.ORD_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@COMM_RATE1", (object?)f.COMM_RATE1 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@COMM_RATE2", (object?)f.COMM_RATE2 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GST_RATE", (object?)f.GST_RATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TDS_RATE", (object?)f.TDS_RATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LUL_BILLNO", (object?)f.LUL_BILLNO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LUL_BILLDT", (object?)f.LUL_BILLDT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LUL_PASSDT", (object?)f.LUL_PASSDT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LUL_CHQ", (object?)f.LUL_CHQ ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LUL_REMARK", (object?)f.LUL_REMARK ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@LOAD_PER", (object?)f.LOAD_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LOAD_AMT", (object?)f.LOAD_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LOAD_REM", (object?)f.LOAD_REM ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LOAD_AC", (object?)f.LOAD_AC ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WB_AMT", (object?)f.WB_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WB_AC", (object?)f.WB_AC ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WB_REM", (object?)f.WB_REM ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WAYBILL_NO", (object?)f.WAYBILL_NO ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }

                int i = 1;
                foreach (var row in salesReturn.RowData)
                {
                    string itmName = "";
                    string unitName = "";
                    string unitCode = "";
                    string hsnCode = "";

                    const string itemQry = @"SELECT PRINT_NAME, NAME, UNIT_NAME, UNIT_CODE, HSN_CODE FROM ITEM_MAST WHERE CODE = 
                                            @ITEM_CODE AND COMP_CODE = @COMP_CODE";

                    using (var itemCmd = new SqlCommand(itemQry, con, tran))
                    {
                        itemCmd.Parameters.AddWithValue("@ITEM_CODE", row.ITEM_CODE);
                        itemCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        using var reader = itemCmd.ExecuteReader();

                        if (reader.Read())
                        {
                            var printName = Convert.ToString(reader["PRINT_NAME"]);
                            var name = Convert.ToString(reader["NAME"]);

                            if (!string.IsNullOrWhiteSpace(printName))
                            {
                                itmName = printName;
                            }
                            else if (f.ITEM_TYPE == "PSF" || f.ITEM_TYPE == "Finish")
                            {
                                itmName = "100% Recycle " + name;
                            }
                            else
                            {
                                itmName = name;
                            }

                            unitName = Convert.ToString(reader["UNIT_NAME"]);
                            unitCode = Convert.ToString(reader["UNIT_CODE"]);
                            hsnCode = Convert.ToString(reader["HSN_CODE"]);
                        }
                    }


                    using var cmd = new SqlCommand("sp_SalesReturn", con, tran);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ACTION", "FooterInsert");

                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@V_TYPE", f.V_TYPE);
                    cmd.Parameters.AddWithValue("@V_NO", f.V_NO);
                    cmd.Parameters.AddWithValue("@V_DATE", f.V_DATE);

                    cmd.Parameters.AddWithValue("@ITEM_CODE", row.ITEM_CODE);
                    //cmd.Parameters.AddWithValue("@ITEM_NAME", (object?)row.ITEM_NAME ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ITEM_NAME", itmName);
                    cmd.Parameters.AddWithValue("@SNO", i++);
                    cmd.Parameters.AddWithValue("@UNIT_NAME", unitName);
                    cmd.Parameters.AddWithValue("@UNIT_CODE", unitCode);
                    cmd.Parameters.AddWithValue("@HSN_CODE", hsnCode);
                    cmd.Parameters.AddWithValue("@NOS", (object?)row.NOS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@QTY", (object?)row.QTY ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GROSS_QTY", (object?)row.GROSS_QTY ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GATE_QTY", (object?)row.GATE_QTY ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RATE", (object?)row.RATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FOR_RATE", (object?)row.FOR_RATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AMOUNT", (object?)row.AMOUNT ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@PACK_PER", (object?)row.PACK_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PACK_AMT", (object?)row.PACK_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DISC_PER", (object?)row.DISC_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DISC_AMT", (object?)row.DISC_AMT ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@TAX_CODE", (object?)row.TAX_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CGST_PER", (object?)row.CGST_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CGST_AMT", (object?)row.CGST_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SGST_PER", (object?)row.SGST_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SGST_AMT", (object?)row.SGST_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IGST_PER", (object?)row.IGST_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IGST_AMT", (object?)row.IGST_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CESS_PER", (object?)row.CESS_PER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CESS_AMT", (object?)row.CESS_AMT ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@LAND_RATE", (object?)row.LAND_RATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LAND_AMT", (object?)row.LAND_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@REMARK", (object?)row.REMARK ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PACK_TYPE", (object?)row.PACK_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PACK_NO", (object?)row.PACK_NO ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@ORD_TYPE", (object?)row.ORD_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ORD_NO", (object?)row.ORD_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ORD_RATE", (object?)row.ORD_RATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SAUDA_TYPE", (object?)row.SAUDA_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SAUDA_NO", (object?)row.SAUDA_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SAUDA_RATE", (object?)row.SAUDA_RATE ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@LOT_No", (object?)row.LOT_No ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DEPT_CODE", (object?)row.DEPT_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DCN_TYPE", (object?)row.DCN_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DCN_NO", (object?)row.DCN_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FINAL_LOCK", (object?)row.FINAL_LOCK ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@STATUS", (object?)row.STATUS ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd.Parameters.AddWithValue("@CDISC_AMT", (object?)row.CDISC_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@INSU_AMT", (object?)row.INSU_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FRT_AMT", (object?)row.FRT_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WBQTY", (object?)row.WBQTY ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FEXCH_USD", (object?)row.FEXCH_USD ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ROW_ID", (object?)row.ROW_ID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GATE_INQTY", (object?)row.GATE_INQTY ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MIS_GROUP", (object?)row.MIS_GROUP ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FREIGHT_AMT", (object?)row.FREIGHT_AMT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PROD_DESC", (object?)row.PROD_DESC ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MIS_GRP", (object?)row.MIS_GRP ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }

                tran.Commit();
                //_logService.InsertLog("SALE1", "SALES RETURN", "Transaction", logAction, f.V_TYPE, f.V_NO.ToString(), f.V_DATE);
                //_logService.InsertLog("SALE2", "SALES RETURN", "Transaction", logAction, f.V_TYPE, f.V_NO.ToString(), f.V_DATE);

                return new RepositoryResponse { status = true, message = resMessage };
            }
            catch (Exception ex)
            {
                tran.Rollback();
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }
        public RepositoryResponseData<object> GetID(int id, string vType)
        {
            try
            {
                if (id <= 0 || string.IsNullOrWhiteSpace(vType))
                    return new RepositoryResponseData<object> { status = false, message = "Invalid request data" };

                var g = _globalVariableService.GetGlobalVariables();
                var headerList = new List<FormDataModel>();
                var itemList = new List<RowDataModel>();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_SalesReturn", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ACTION", "GetById");
                    cmd.Parameters.AddWithValue("@V_NO", id);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // =========================
                        // SALE1 - HEADER
                        // =========================
                        if (reader.Read())
                        {
                            var header = new FormDataModel
                            {
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_TYPE = reader["V_TYPE"]?.ToString() ?? "",
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,

                                TRAN_TYPE = reader["TRAN_TYPE"]?.ToString(),
                                SUPPLY_TYPE = reader["SUPPLY_TYPE"]?.ToString(),
                                TAX_CODE = reader["TAX_CODE"] != DBNull.Value ? Convert.ToInt32(reader["TAX_CODE"]) : null,
                                ITEM_TYPE = reader["ITEM_TYPE"]?.ToString(),

                                REF_NO = reader["REF_NO"] != DBNull.Value ? Convert.ToInt32(reader["REF_NO"]) : null,
                                REF_TYPE = reader["REF_TYPE"]?.ToString(),
                                REF_DATE = reader["REF_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["REF_DATE"]) : null,

                                GATE_NO = reader["GATE_NO"] != DBNull.Value ? Convert.ToInt32(reader["GATE_NO"]) : null,
                                GATE_TYPE = reader["GATE_TYPE"]?.ToString(),

                                WB_NO = reader["WB_NO"] != DBNull.Value ? Convert.ToInt32(reader["WB_NO"]) : null,
                                WB_TYPE = reader["WB_TYPE"]?.ToString(),

                                PACK_NO = reader["PACK_NO"] != DBNull.Value ? Convert.ToInt32(reader["PACK_NO"]) : null,
                                PACK_TYPE = reader["PACK_TYPE"]?.ToString(),

                                SAUDA_NO = reader["SAUDA_NO"] != DBNull.Value ? Convert.ToInt32(reader["SAUDA_NO"]) : null,
                                SAUDA_TYPE = reader["SAUDA_TYPE"]?.ToString(),
                                SAUDA_RATE = reader["SAUDA_RATE"] != DBNull.Value ? Convert.ToDecimal(reader["SAUDA_RATE"]) : null,

                                CAL_ONPCS = reader["CAL_ONPCS"] != DBNull.Value ? Convert.ToInt32(reader["CAL_ONPCS"]) : null,

                                BILL_CODE = reader["BILL_CODE"] != DBNull.Value ? Convert.ToInt32(reader["BILL_CODE"]) : 0,
                                BILL_ADD1 = reader["BILL_ADD1"]?.ToString(),
                                BILL_ADD2 = reader["BILL_ADD2"]?.ToString(),
                                BILL_ADD3 = reader["BILL_ADD3"]?.ToString(),
                                BILL_CITY = reader["BILL_CITY"] != DBNull.Value ? Convert.ToInt32(reader["BILL_CITY"]) : null,
                                BILL_PINCODE = reader["BILL_PINCODE"]?.ToString(),
                                BILL_GST = reader["BILL_GST"]?.ToString(),

                                AGENT_CODE = reader["AGENT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["AGENT_CODE"]) : null,

                                SHIP_CODE = reader["SHIP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["SHIP_CODE"]) : null,
                                SHIP_ADD1 = reader["SHIP_ADD1"]?.ToString(),
                                SHIP_ADD2 = reader["SHIP_ADD2"]?.ToString(),
                                SHIP_ADD3 = reader["SHIP_ADD3"]?.ToString(),
                                SHIP_CITY = reader["SHIP_CITY"] != DBNull.Value ? Convert.ToInt32(reader["SHIP_CITY"]) : null,
                                SHIP_PINCODE = reader["SHIP_PINCODE"]?.ToString(),
                                SHIP_GST = reader["SHIP_GST"]?.ToString(),

                                FORM_CODE = reader["FORM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["FORM_CODE"]) : null,

                                AMOUNT = reader["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["AMOUNT"]) : null,
                                TOT_NOS = reader["TOT_NOS"] != DBNull.Value ? Convert.ToInt32(reader["TOT_NOS"]) : null,
                                PACK_PER = reader["PACK_PER"] != DBNull.Value ? Convert.ToDecimal(reader["PACK_PER"]) : null,
                                PACK_AMT = reader["PACK_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["PACK_AMT"]) : null,

                                CESS_PER = reader["CESS_PER"] != DBNull.Value ? Convert.ToDecimal(reader["CESS_PER"]) : null,
                                CESS_AMT = reader["CESS_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["CESS_AMT"]) : null,

                                DISC_PER = reader["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(reader["DISC_PER"]) : null,
                                DISC_AMT = reader["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["DISC_AMT"]) : null,

                                TCS_PER = reader["TCS_PER"] != DBNull.Value ? Convert.ToDecimal(reader["TCS_PER"]) : null,
                                TCS_AMT = reader["TCS_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["TCS_AMT"]) : null,

                                ROUND_OFF = reader["ROUND_OFF"] != DBNull.Value ? Convert.ToDecimal(reader["ROUND_OFF"]) : null,

                                CGST_PER = reader["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["CGST_PER"]) : null,
                                CGST_AMT = reader["CGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["CGST_AMT"]) : null,

                                NAMOUNT = reader["NAMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["NAMOUNT"]) : null,

                                SGST_PER = reader["SGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["SGST_PER"]) : null,
                                SGST_AMT = reader["SGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["SGST_AMT"]) : null,

                                INSU_PER = reader["INSU_PER"] != DBNull.Value ? Convert.ToDecimal(reader["INSU_PER"]) : null,
                                INSU_AMT = reader["INSU_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["INSU_AMT"]) : null,

                                IGST_PER = reader["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["IGST_PER"]) : null,
                                IGST_AMT = reader["IGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["IGST_AMT"]) : null,

                                FRT_AMT = reader["FRT_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["FRT_AMT"]) : null,

                                TDS_PER = reader["TDS_PER"] != DBNull.Value ? Convert.ToDecimal(reader["TDS_PER"]) : null,
                                TDS_AMT = reader["TDS_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["TDS_AMT"]) : null,

                                TOT_GROSS = reader["TOT_GROSS"] != DBNull.Value ? Convert.ToDecimal(reader["TOT_GROSS"]) : null,
                                TOT_NET = reader["TOT_NET"] != DBNull.Value ? Convert.ToDecimal(reader["TOT_NET"]) : null,

                                FRT_TOPAY = reader["FRT_TOPAY"] != DBNull.Value ? Convert.ToDecimal(reader["FRT_TOPAY"]) : null,
                                WB_QTY = reader["WB_QTY"] != DBNull.Value ? Convert.ToDecimal(reader["WB_QTY"]) : null,

                                LOAD_PER = reader["LOAD_PER"] != DBNull.Value ? Convert.ToDecimal(reader["LOAD_PER"]) : null,
                                LOAD_AMT = reader["LOAD_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["LOAD_AMT"]) : null,
                                LOAD_AC = reader["LOAD_AC"]?.ToString(),
                                LOAD_REM = reader["LOAD_REM"]?.ToString(),

                                WB_AMT = reader["WB_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["WB_AMT"]) : null,
                                WB_AC = reader["WB_AC"]?.ToString(),
                                WB_REM = reader["WB_REM"]?.ToString(),

                                WAYBILL_NO = reader["WAYBILL_NO"]?.ToString(),

                                GR_NO = reader["GR_NO"]?.ToString(),
                                GR_DATE = reader["GR_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["GR_DATE"]) : null,

                                TRANSPORT_CODE = reader["TRANSPORT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["TRANSPORT_CODE"]) : null,
                                VEHICLE_NO = reader["VEHICLE_NO"]?.ToString(),
                                DRIVER_NAME = reader["DRIVER_NAME"]?.ToString(),
                                DRIVER_NO = reader["DRIVER_NO"]?.ToString(),

                                REMARK = reader["REMARK"]?.ToString(),

                                IRN = reader["IRN"]?.ToString(),
                                STATUS = reader["STATUS"] != DBNull.Value ? Convert.ToInt32(reader["STATUS"]) : null,

                                EINVOICE_FLG = reader["EINVOICE_FLG"] != DBNull.Value ? Convert.ToInt32(reader["EINVOICE_FLG"]) : null,
                            };

                            headerList.Add(header);
                        }

                        // =========================
                        // SALE2 - ITEMS
                        // =========================
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                var item = new RowDataModel
                                {
                                    ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : 0,
                                    ITEM_NAME = reader["ITEM_NAME"]?.ToString(),

                                    NOS = reader["NOS"] != DBNull.Value ? Convert.ToInt32(reader["NOS"]) : null,
                                    GROSS_QTY = reader["GROSS_QTY"] != DBNull.Value ? Convert.ToDecimal(reader["GROSS_QTY"]) : null,
                                    QTY = reader["QTY"] != DBNull.Value ? Convert.ToDecimal(reader["QTY"]) : null,
                                    RATE = reader["RATE"] != DBNull.Value ? Convert.ToDecimal(reader["RATE"]) : null,
                                    AMOUNT = reader["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["AMOUNT"]) : null,

                                    PACK_PER = reader["PACK_PER"] != DBNull.Value ? Convert.ToDecimal(reader["PACK_PER"]) : null,
                                    PACK_AMT = reader["PACK_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["PACK_AMT"]) : null,

                                    DISC_PER = reader["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(reader["DISC_PER"]) : null,
                                    DISC_AMT = reader["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["DISC_AMT"]) : null,

                                    TAX_CODE = reader["TAX_CODE"] != DBNull.Value ? Convert.ToInt32(reader["TAX_CODE"]) : null,

                                    CGST_PER = reader["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["CGST_PER"]) : null,
                                    CGST_AMT = reader["CGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["CGST_AMT"]) : null,

                                    SGST_PER = reader["SGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["SGST_PER"]) : null,
                                    SGST_AMT = reader["SGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["SGST_AMT"]) : null,

                                    IGST_PER = reader["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["IGST_PER"]) : null,
                                    IGST_AMT = reader["IGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["IGST_AMT"]) : null,

                                    CESS_PER = reader["CESS_PER"] != DBNull.Value ? Convert.ToDecimal(reader["CESS_PER"]) : null,
                                    CESS_AMT = reader["CESS_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["CESS_AMT"]) : null,

                                    REMARK = reader["REMARK"]?.ToString(),
                                    PACK_NO = reader["PACK_NO"] != DBNull.Value ? Convert.ToInt32(reader["PACK_NO"]) : null,

                                    LOT_No = reader["LOT_No"]?.ToString(),

                                    SAUDA_TYPE = reader["SAUDA_TYPE"]?.ToString(),
                                    SAUDA_NO = reader["SAUDA_NO"] != DBNull.Value ? Convert.ToInt32(reader["SAUDA_NO"]) : null,
                                    SAUDA_RATE = reader["SAUDA_RATE"] != DBNull.Value ? Convert.ToDecimal(reader["SAUDA_RATE"]) : null,

                                    ORD_TYPE = reader["ORD_TYPE"]?.ToString(),
                                    ORD_NO = reader["ORD_NO"] != DBNull.Value ? Convert.ToInt32(reader["ORD_NO"]) : null,
                                    ORD_RATE = reader["ORD_RATE"] != DBNull.Value ? Convert.ToDecimal(reader["ORD_RATE"]) : null,

                                    DCN_TYPE = reader["DCN_TYPE"]?.ToString(),
                                    DCN_NO = reader["DCN_NO"] != DBNull.Value ? Convert.ToInt32(reader["DCN_NO"]) : null,

                                    HSN_CODE = reader["HSN_CODE"]?.ToString()
                                };

                                itemList.Add(item);
                            }
                        }
                    }
                }

                return new RepositoryResponseData<object>
                {
                    status = true,
                    data = new
                    {
                        Header = headerList,
                        Items = itemList
                    }
                };
            }
            catch (SqlException ex)
            {
                return new RepositoryResponseData<object> { status = false, message = "Database error occurred." + ex.Message };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<object> { status = false, message = "An error occurred while retrieving sales return details." + ex.Message };
            }
        }
        public async Task<RepositoryResponseList<SaudaItemDetail>> GetSaudaItemDetails(string saudaType, string saudaNo, int partyCode, string itemCodes)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                if (string.IsNullOrWhiteSpace(saudaType) || string.IsNullOrWhiteSpace(saudaNo))
                    return new RepositoryResponseList<SaudaItemDetail> { status = false, message = "Invalid Sauda." };

                if (partyCode <= 0)
                    return new RepositoryResponseList<SaudaItemDetail> { status = false, message = "Please select Party." };

                if (string.IsNullOrWhiteSpace(itemCodes))
                    return new RepositoryResponseList<SaudaItemDetail> { status = true, data = new List<SaudaItemDetail>() };

                using var con = _dbConnection.GetErpConnection();
                await con.OpenAsync();

                // 1. Party DISCGRP_CODE
                string qry = $@"SELECT ISNULL(DISCGRP_CODE,0) FROM SUBGROUP_MAST WHERE CODE={partyCode} AND COMP_CODE={gv.PubCompCode}";
                int discGrpCode;

                using (var cmd = new SqlCommand(qry, con))
                {
                    discGrpCode = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // 2. If DISCGRP_CODE <= 0 then get AGENT_CODE
                if (discGrpCode <= 0)
                {
                    qry = $@"SELECT ISNULL(AGENT_CODE,0) FROM SUBGROUP_MAST WHERE CODE={partyCode} AND COMP_CODE={gv.PubCompCode}";
                    int agentCode;
                    using (var cmd = new SqlCommand(qry, con))
                    {
                        agentCode = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    // 3. Agent DISCGRP_CODE
                    if (agentCode > 0)
                    {
                        qry = $@"SELECT ISNULL(DISCGRP_CODE,0) FROM SUBGROUP_MAST WHERE CODE={agentCode} AND COMP_CODE={gv.PubCompCode}";
                        using var cmd = new SqlCommand(qry, con);
                        discGrpCode = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }

                var result = new List<SaudaItemDetail>();

                foreach (string code in itemCodes.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!int.TryParse(code, out int itemCode) || itemCode <= 0)
                        continue;

                    // 4. Size Diff
                    qry = $@"SELECT ISNULL(size_diff,0) FROM disc_mast LEFT JOIN ITEMSIZE_MAST ON ITEMSIZE_MAST.code=disc_mast.size_code AND ITEMSIZE_MAST.comp_code=
                            disc_mast.comp_code LEFT JOIN item_mast ON item_mast.size_code=disc_mast.size_code AND item_mast.comp_code=disc_mast.comp_code
                            WHERE item_mast.code={itemCode} AND item_mast.comp_code={gv.PubCompCode} AND item_mast.ACTIVE=1 AND disc_mast.code={discGrpCode}";
                    decimal sizeDiff;
                    using (var cmd = new SqlCommand(qry, con))
                    {
                        sizeDiff = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
                    }

                    // 5. Color Diff
                    qry = $@"SELECT ISNULL(color_diff,0) FROM disc_mast LEFT JOIN color_mast ON color_mast.code=disc_mast.color_code AND color_mast.comp_code=disc_mast.comp_code
                            LEFT JOIN item_mast ON item_mast.color_code=disc_mast.color_code AND item_mast.comp_code=disc_mast.comp_code WHERE item_mast.code={itemCode}
                            AND item_mast.comp_code={gv.PubCompCode} AND item_mast.ACTIVE=1 AND disc_mast.code={discGrpCode}";
                    decimal colorDiff;
                    using (var cmd = new SqlCommand(qry, con))
                    {
                        colorDiff = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
                    }

                    // 6. Gram Diff
                    qry = $@"SELECT ISNULL(gram_diff,0) FROM disc_mast LEFT JOIN ITEMCAT_MAST ON ITEMCAT_MAST.code=disc_mast.gram_code AND ITEMCAT_MAST.comp_code=disc_mast.comp_code
                            LEFT JOIN item_mast ON item_mast.cat_code=disc_mast.gram_code AND item_mast.comp_code=disc_mast.comp_code WHERE item_mast.code={itemCode}
                            AND item_mast.comp_code={gv.PubCompCode} AND item_mast.ACTIVE=1 AND disc_mast.code={discGrpCode}";
                    decimal gramDiff;
                    using (var cmd = new SqlCommand(qry, con))
                    {
                        gramDiff = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
                    }

                    // 7. Item Diff
                    qry = $@"SELECT ISNULL(item_diff,0) FROM disc_mast WHERE item_code={itemCode} AND comp_code={gv.PubCompCode} AND code={discGrpCode}";
                    decimal itemDiff;
                    using (var cmd = new SqlCommand(qry, con))
                    {
                        itemDiff = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
                    }

                    // 8. ORDER2 V_TYPE
                    qry = $@"SELECT TOP 1 V_TYPE FROM ORDER2 WHERE SAUDA_TYPE='{saudaType}' AND SAUDA_NO={saudaNo} AND ITEM_CODE={itemCode} AND COMP_CODE={gv.PubCompCode}
                            AND BRANCH_CODE={gv.PubBranchCode}";
                    string orderType;
                    using (var cmd = new SqlCommand(qry, con))
                    {
                        orderType = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
                    }

                    // 9. ORDER2 V_NO
                    qry = $@"SELECT TOP 1 V_NO FROM ORDER2 WHERE SAUDA_TYPE='{saudaType}' AND SAUDA_NO={saudaNo} AND ITEM_CODE={itemCode} AND COMP_CODE={gv.PubCompCode}
                            AND BRANCH_CODE={gv.PubBranchCode}";
                    string orderNo;
                    using (var cmd = new SqlCommand(qry, con))
                    {
                        orderNo = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
                    }

                    // 10. ORDER2 Rate
                    qry = $@"SELECT TOP 1 ISNULL(Rate,0) FROM ORDER2 WHERE SAUDA_TYPE='{saudaType}' AND SAUDA_NO={saudaNo} AND ITEM_CODE={itemCode} AND COMP_CODE={gv.PubCompCode}
                     AND BRANCH_CODE={gv.PubBranchCode}";
                    decimal orderRate;
                    using (var cmd = new SqlCommand(qry, con))
                    {
                        orderRate = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
                    }

                    result.Add(new SaudaItemDetail
                    {
                        ITEM_CODE = itemCode,
                        SIZE_DIFF = sizeDiff,
                        COLOR_DIFF = colorDiff,
                        GRAM_DIFF = gramDiff,
                        ITEM_DIFF = itemDiff,
                        ORD_TYPE = orderType,
                        ORD_NO = orderNo,
                        ORD_RATE = orderRate
                    });
                }

                return new RepositoryResponseList<SaudaItemDetail> { status = true, data = result };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<SaudaItemDetail> { status = false, message = ex.Message };
            }
        }
        public RepositoryResponseData<object> GetWBWeight(string wbDocId)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                string sql = @"SELECT ISNULL(SUM(NET_WGT), 0) FROM WB2 WHERE DOC_ID = @DOC_ID AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@DOC_ID", wbDocId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                    con.Open();
                    var result = cmd.ExecuteScalar();

                    return new RepositoryResponseData<object> { status = true, data = result };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<object> { status = false, message = ex.Message };
            }
        }
        public RepositoryResponseData<object> GetPackingData(string packType, int packNo, string vType, int vNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    // Check existing packing slip
                    string checkSql = @"SELECT TOP 1 V_NO FROM sale2 WHERE pack_type = @PACK_TYPE AND pack_no = @PACK_NO AND comp_code = @COMP_CODE
                                        AND branch_code = @BRANCH_CODE AND year_code = @YEAR_CODE AND V_type = @V_TYPE AND V_NO <> @V_NO";

                    using (SqlCommand cmd = new SqlCommand(checkSql, con))
                    {
                        cmd.Parameters.AddWithValue("@PACK_TYPE", packType);
                        cmd.Parameters.AddWithValue("@PACK_NO", packNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);

                        var existingVNo = cmd.ExecuteScalar();

                        if (existingVNo != null)
                        {
                            return new RepositoryResponseData<object>
                            {
                                status = true,
                                data = new
                                {
                                    exists = true,
                                    existingVNo = existingVNo
                                }
                            };
                        }
                    }

                    // Get production items
                    string sql = @"SELECT COUNT(a.NOS) AS NOS, ISNULL(SUM(a.GROSS_QTY), 0) AS G_QTY, ISNULL(SUM(a.QTY), 0) AS N_QTY, a.V_TYPE, a.V_NO, a.ITEM_CODE,
                                    b.NAME, a.LOT_NO FROM PRODUCTION2 a
                                    LEFT JOIN ITEM_MAST b ON b.CODE = a.ITEM_CODE AND b.ACTIVE = 1 AND b.COMP_CODE = a.COMP_CODE
                                    WHERE a.V_TYPE = @PACK_TYPE AND a.V_NO = @PACK_NO AND a.COMP_CODE = @COMP_CODE AND a.BRANCH_CODE = @BRANCH_CODE AND a.YEAR_CODE = 
                                    @YEAR_CODE GROUP BY a.V_TYPE, a.V_NO, a.ITEM_CODE, b.NAME, a.LOT_NO ORDER BY b.NAME";

                    var data = new List<object>();

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@PACK_TYPE", packType);
                        cmd.Parameters.AddWithValue("@PACK_NO", packNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                data.Add(new
                                {
                                    NOS = Convert.ToDecimal(reader["NOS"]),
                                    GROSS_QTY = Convert.ToDecimal(reader["G_QTY"]),
                                    QTY = Convert.ToDecimal(reader["N_QTY"]),
                                    PACK_NO = Convert.ToInt32(reader["V_NO"]),
                                    ITEM_CODE = Convert.ToInt32(reader["ITEM_CODE"]),
                                    ITEM_NAME = Convert.ToString(reader["NAME"]),
                                    LOT_No = Convert.ToString(reader["LOT_NO"])
                                });
                            }
                        }
                    }

                    return new RepositoryResponseData<object>
                    {
                        status = true,
                        data = new
                        {
                            exists = false,
                            data = data
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<object> { status = false, message = ex.Message };
            }
        }
        public RepositoryResponseData<object> GetGateData(string gateDocId)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using var con = _dbConnection.GetErpConnection();
                con.Open();

                // ==================== GATE1 ====================
                string partySql = @"SELECT Party_code, Add1, Add2, Add3, Party_city, Party_Pincode FROM GATE1 WHERE DOC_ID = @DOC_ID AND comp_code = @COMP_CODE 
                                    AND Branch_code = @BRANCH_CODE";

                object party = null;

                using (var cmd = new SqlCommand(partySql, con))
                {
                    cmd.Parameters.AddWithValue("@DOC_ID", gateDocId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                    using var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        party = new
                        {
                            Party_code = reader["Party_code"]?.ToString(),
                            Add1 = reader["Add1"]?.ToString(),
                            Add2 = reader["Add2"]?.ToString(),
                            Add3 = reader["Add3"]?.ToString(),
                            Party_city = reader["Party_city"]?.ToString(),
                            Party_Pincode = reader["Party_Pincode"]?.ToString(),
                        };
                    }
                }

                // ==================== GATE2 ====================
                string itemSql = @"SELECT a.Item_code, a.Item_name, SUM(a.Nos) AS NOS, SUM(a.QTY) AS QTY FROM GATE2 a WHERE a.DOC_ID = @DOC_ID
                                    AND a.COMP_CODE = @COMP_CODE AND a.BRANCH_CODE = @BRANCH_CODE GROUP BY a.Item_code, a.Item_name";

                var items = new List<object>();

                using (var cmd = new SqlCommand(itemSql, con))
                {
                    cmd.Parameters.AddWithValue("@DOC_ID", gateDocId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                    using var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        items.Add(new
                        {
                            ITEM_CODE = reader["Item_code"],
                            ITEM_NAME = reader["Item_name"],
                            NOS = reader["NOS"],
                            QTY = reader["QTY"],
                            GROSS_QTY = reader["QTY"]
                        });
                    }
                }

                return new RepositoryResponseData<object>
                {
                    status = true,
                    data = new
                    {
                        party,
                        items
                    }
                };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<object> { status = false, message = ex.Message };
            }
        }
        public RepositoryResponse ValidateData(ValidateDataRequest model)
        {
            if (model == null)
                return new RepositoryResponse { status = false, message = "Invalid request data" };

            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using var con = _dbConnection.GetErpConnection();
                con.Open();

                // 1. GATE PARTY VALIDATION 
                if (!string.IsNullOrWhiteSpace(model.GateDocId))
                {
                    const string sql = @"SELECT PARTY_CODE FROM GATE1 WHERE DOC_ID = @DOC_ID AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";
                    using var cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@DOC_ID", model.GateDocId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                    var result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        int gatePartyCode = Convert.ToInt32(result);
                        if (model.PartyCode != gatePartyCode)
                        {
                            //Testing
                            //return Json(new { success = false, message = $"Party not matched with Gate Entry no. = {model.GateDocId}" });
                        }
                    }
                }

                // 2. WEIGHBRIDGE VALIDATION
                if (model.ProductType != "Other" && model.GrossWeight > 500)
                {
                    if (string.IsNullOrWhiteSpace(model.TruckNo))
                    {
                        return new RepositoryResponse { status = false, message = "Truck No. can not be Blank, Please Check it." };
                    }

                    if (!string.IsNullOrWhiteSpace(model.WbNo))
                    {
                        const string wbSql = @"SELECT PARTY_CODE, VEHICLE_NO FROM WB1 WHERE DOC_ID = @DOC_ID AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";

                        using var wbCmd = new SqlCommand(wbSql, con);
                        wbCmd.Parameters.AddWithValue("@DOC_ID", model.WbNo ?? "");
                        wbCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        wbCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                        using var reader = wbCmd.ExecuteReader();

                        int wbPartyCode = 0;
                        string wbVehicleNo = "";

                        if (reader.Read())
                        {
                            wbPartyCode = reader["PARTY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PARTY_CODE"]);
                            wbVehicleNo = reader["VEHICLE_NO"]?.ToString() ?? "";
                        }

                        reader.Close();

                        const string weightSql = @"SELECT ISNULL(SUM(NET_WGT), 0) FROM WB2 WHERE DOC_ID = @DOC_ID AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";

                        using var weightCmd = new SqlCommand(weightSql, con);
                        weightCmd.Parameters.AddWithValue("@DOC_ID", model.WbNo ?? "");
                        weightCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        weightCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                        decimal wbNetWeight = Convert.ToDecimal(weightCmd.ExecuteScalar() ?? 0);

                        //===================================================================
                        //Commented As Per Amar Sir, Because It was also commented in VB code
                        //===================================================================

                        //if (model.PartyCode != wbPartyCode)
                        //{
                        //    return Json(new
                        //    {
                        //        success = false,
                        //        message = "Party Name not match with Weighbridge Party Name"
                        //    });
                        //}

                        //if (model.TruckNo != wbVehicleNo)
                        //{
                        //    return Json(new
                        //    {
                        //        success = false,
                        //        message = $"Invoice Truck No. {model.TruckNo} not match with Weighbridge Truck No., Please Check Truck No. {wbVehicleNo}"
                        //    });
                        //}

                        //if (model.ProductType == "Waste" && model.TotalNetWeight != wbNetWeight)
                        //{
                        //    return Json(new
                        //    {
                        //        success = false,
                        //        message = $"Invoice Quantity {model.TotalNetWeight} not match with Weighbridge Quantity, Please Check Quantity {wbNetWeight}"
                        //    });
                        //}
                    }
                }

                // 3. PRODUCT TYPE / PACKING VALIDATION
                bool chkVal = false;

                if (gv.PubCompCode == "1" && model.ProductType == "PSF")
                    chkVal = true;
                else if ((gv.PubCompCode == "2" || gv.PubCompCode == "5") && (model.ProductType == "Fabric" || model.ProductType == "Sacks"))
                    chkVal = true;
                else if (gv.PubCompCode == "4" && model.ProductType == "Finish")
                    chkVal = true;

                if (chkVal)
                {
                    //if (model.DocumentType == "SAJR" && string.IsNullOrWhiteSpace(model.PackingDocId))
                    //    return Json(new {success = false, message = "Packing No. can not be Blank, Please Check it."});

                    //string packType;

                    //if (model.DocumentType == "SAJR")
                    //    packType = (gv.PubCompCode == "2" || gv.PubCompCode == "5") ? "FPJR" : "FFRC";
                    //else
                    //    packType = "FPIS";

                    // PACKING DATE VALIDATION
                    if (!string.IsNullOrWhiteSpace(model.PackingDocId))
                    {
                        const string sql = @"SELECT V_DATE FROM PRODUCTION1 WHERE DOC_ID = @DOC_ID AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";

                        using var cmd = new SqlCommand(sql, con);
                        cmd.Parameters.AddWithValue("@DOC_ID", model.PackingDocId);
                        //cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar).Value = packType;
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                        var result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            DateTime packingDate = Convert.ToDateTime(result);
                            if (packingDate.Date != model.InvoiceDate.Date)
                            {
                                return new RepositoryResponse { status = false, message = $"Packing Date {packingDate:dd/MM/yyyy} not matched with Invoice date, Please Check it." };
                            }
                        }
                    }
                }

                // 4. TRANSPORT + GR NO DUPLICATE VALIDATION
                if (!string.IsNullOrWhiteSpace(model.Transport) && !string.IsNullOrWhiteSpace(model.GrNo))
                {
                    string currentDoc = (model.DocumentType ?? "") + model.DocumentNo;

                    // PURCHASE1
                    const string purchaseSql = @"SELECT TOP 1 CONCAT(V_TYPE, V_NO) FROM PURCHASE1 WHERE CONCAT(V_TYPE, V_NO) <> @CURRENT_DOC
                                                AND Transport_Name = @TRANSPORT AND GR_NO = @GR_NO AND V_TYPE NOT IN (SELECT CODE FROM Doctype_mast 
                                                WHERE DOCTYPE = 'MaterialReceipt') AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND Year_Code = 
                                                @YEAR_CODE";

                    using (var cmd = new SqlCommand(purchaseSql, con))
                    {
                        cmd.Parameters.AddWithValue("@CURRENT_DOC", currentDoc);
                        cmd.Parameters.AddWithValue("@TRANSPORT", model.Transport.Trim());
                        cmd.Parameters.AddWithValue("@GR_NO", model.GrNo.Trim());
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        var result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            string purDocId = result.ToString();
                            return new RepositoryResponse { status = false, message = $@"Transport Name '{model.Transport}' with GRNo='{model.GrNo}' already exist in Purchase 
                                              Bill/Direct Exps/JW/Imported Exps/Return No:{purDocId}" };
                        }
                    }

                    // SALE1
                    const string saleSql = @"SELECT TOP 1 CONCAT(V_TYPE, V_NO) FROM SALE1 WHERE CONCAT(V_TYPE, V_NO) <> @CURRENT_DOC AND Transport_Name = @TRANSPORT
                                            AND GR_NO = @GR_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";

                    using (var cmd = new SqlCommand(saleSql, con))
                    {
                        cmd.Parameters.AddWithValue("@CURRENT_DOC", currentDoc);
                        cmd.Parameters.AddWithValue("@TRANSPORT", model.Transport.Trim());
                        cmd.Parameters.AddWithValue("@GR_NO", model.GrNo.Trim());
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                        var result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            string saleDocId = result.ToString();
                            return new RepositoryResponse { status = false, message = $@"Transport Name '{model.Transport}' with GRNo='{model.GrNo}' already exist in Sale/JW 
                                                Issue/Sale Return invoice No:{saleDocId}" };
                        }
                    }
                }

                // 5. ITEM VALIDATIONS
                foreach (var item in model.Items ?? new List<ValidateItem>())
                {
                    if (item.ItemCode == 0)
                        continue;

                    // Item should exist in SALE2
                    if (model.DocumentType != "SAJR")
                    {
                        const string sql = @"SELECT 1 FROM SALE2 WHERE ITEM_CODE = @ITEM_CODE AND DOC_ID = @DOC_ID AND COMP_CODE = @COMP_CODE
                                                AND BRANCH_CODE = @BRANCH_CODE";
                        using var cmd = new SqlCommand(sql, con);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                        cmd.Parameters.AddWithValue("@DOC_ID", model.ReferenceDocId ?? "");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                        var result = cmd.ExecuteScalar();

                        if (result == null)
                        {
                            return new RepositoryResponse { status = false, message = $"Item {item.ItemName} not exist in Sale Invoice No. {model.ReferenceDocId}" };
                        }
                    }

                    // ITEM GROUP = PRODUCT TYPE
                    const string groupSql = @"SELECT SALE_GROUP FROM ITEM_MAST a LEFT JOIN ITEM_GROUP b ON a.GROUP_CODE = b.CODE AND a.COMP_CODE = b.COMP_CODE
                                                WHERE a.CODE = @ITEM_CODE AND a.COMP_CODE = @COMP_CODE";

                    using (var cmd = new SqlCommand(groupSql, con))
                    {
                        cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        var result = cmd.ExecuteScalar();
                        string mgroup = result?.ToString() ?? "";
                        if (mgroup != model.ProductType)
                        {
                            return new RepositoryResponse { status = false, message = $@"Item Name={item.ItemName} (Group:{mgroup}), but you selected Product Type : 
                                                {model.ProductType} in header." };
                        }
                    }
                }

                // 6. PARTY STATE + GST VALIDATION
                if (model.PartyCode > 0 && model.ProductType == "PSF")
                {
                    const string sql = @"SELECT STATE_CODE FROM SUBGROUP_MAST WHERE CODE = @PARTY_CODE AND COMP_CODE = @COMP_CODE";

                    using var cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", model.PartyCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                    var result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        int stateCode = Convert.ToInt32(result);
                        bool isLocal = Convert.ToInt32(gv.STATE_CODE) == stateCode;
                        string stateType = isLocal ? "Local" : "Central/Other";

                        if (isLocal && model.IgstAmount > 0)
                        {
                            return new RepositoryResponse { status = false, message = $"IGST Not applicable as per Party State type is {stateType}" };
                        }

                        if (!isLocal && model.CgstAmount + model.SgstAmount > 0)
                        {
                            return new RepositoryResponse { status = false, message = $"CGST/SGST not applicable as per Party State type is {stateType}" };
                        }

                        if (model.IgstAmount > 0 && model.CgstAmount + model.SgstAmount > 0)
                        {
                            return new RepositoryResponse { status = false, message = "CGST+SGST+IGST all three type tax not applicable." };
                        }
                    }
                }
                else if (model.PartyCode > 0)
                {
                    if (model.CgstAmount + model.SgstAmount > 0 && model.IgstAmount > 0)
                    {
                        return new RepositoryResponse { status = false, message = "Both GST Tax Rate is not Applicable in One Sales Invoice (CGST+SGST & IGST)" };
                    }

                    if (model.CgstAmount != model.SgstAmount)
                    {
                        return new RepositoryResponse { status = false, message = "CGST & SGST Amount Should Be Same" };
                    }
                }

                return new RepositoryResponse { status = true };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }
        public async Task<RepositoryResponseList<SaudaItemDetail>> CalculateSaudaRate(int partyCode, string itemCodes)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                if (partyCode <= 0)
                    return new RepositoryResponseList<SaudaItemDetail> { status = false, message = "Please select Party." };

                if (string.IsNullOrWhiteSpace(itemCodes))
                    return new RepositoryResponseList<SaudaItemDetail> { status = true, data = new List<SaudaItemDetail>() };

                using var con = _dbConnection.GetErpConnection();
                await con.OpenAsync();

                // Get Party DISCGRP_CODE
                string qry = $@"SELECT ISNULL(DISCGRP_CODE,0) FROM SUBGROUP_MAST WHERE CODE={partyCode} AND COMP_CODE={gv.PubCompCode}";

                int discGrpCode;

                using (var cmd = new SqlCommand(qry, con))
                    discGrpCode = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

                // If Party DISCGRP_CODE not found, get Agent DISCGRP_CODE
                if (discGrpCode <= 0)
                {
                    qry = $@"SELECT ISNULL(AGENT_CODE,0) FROM SUBGROUP_MAST WHERE CODE={partyCode} AND COMP_CODE={gv.PubCompCode}";

                    int agentCode;

                    using (var cmd = new SqlCommand(qry, con))
                        agentCode = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

                    if (agentCode > 0)
                    {
                        qry = $@"SELECT ISNULL(DISCGRP_CODE,0) FROM SUBGROUP_MAST WHERE CODE={agentCode} AND COMP_CODE={gv.PubCompCode}";

                        using var cmd = new SqlCommand(qry, con);
                        discGrpCode = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
                    }
                }

                var result = new List<SaudaItemDetail>();

                foreach (var code in itemCodes.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!int.TryParse(code, out int itemCode) || itemCode <= 0)
                        continue;

                    // Size Diff
                    qry = $@"SELECT ISNULL(size_diff,0) FROM disc_mast LEFT JOIN ITEMSIZE_MAST ON ITEMSIZE_MAST.code=disc_mast.size_code
                      AND ITEMSIZE_MAST.comp_code=disc_mast.comp_code LEFT JOIN item_mast ON item_mast.size_code=disc_mast.size_code
                      AND item_mast.comp_code=disc_mast.comp_code WHERE item_mast.code={itemCode} AND item_mast.comp_code={gv.PubCompCode}
                       AND item_mast.ACTIVE=1 AND disc_mast.code={discGrpCode}";

                    decimal sizeDiff;

                    using (var cmd = new SqlCommand(qry, con))
                        sizeDiff = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);

                    // Color Diff
                    qry = $@"SELECT ISNULL(color_diff,0) FROM disc_mast LEFT JOIN color_mast ON color_mast.code=disc_mast.color_code                      
                            AND color_mast.comp_code=disc_mast.comp_code
                             LEFT JOIN item_mast ON item_mast.color_code=disc_mast.color_code AND item_mast.comp_code=disc_mast.comp_code
                             WHERE item_mast.code={itemCode} AND item_mast.comp_code={gv.PubCompCode} AND item_mast.ACTIVE=1
                            AND disc_mast.code={discGrpCode}";

                    decimal colorDiff;

                    using (var cmd = new SqlCommand(qry, con))
                        colorDiff = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);

                    // Gram Diff
                    qry = $@"SELECT ISNULL(gram_diff,0) FROM disc_mast LEFT JOIN ITEMCAT_MAST ON ITEMCAT_MAST.code=disc_mast.gram_code AND 
                            ITEMCAT_MAST.comp_code=disc_mast.comp_code LEFT JOIN item_mast ON item_mast.cat_code=disc_mast.gram_code
                            AND item_mast.comp_code=disc_mast.comp_code WHERE item_mast.code={itemCode} AND item_mast.comp_code={gv.PubCompCode}
                            AND item_mast.ACTIVE=1 AND disc_mast.code={discGrpCode}";

                    decimal gramDiff;

                    using (var cmd = new SqlCommand(qry, con))
                        gramDiff = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);

                    // Item Diff
                    qry = $@"SELECT ISNULL(item_diff,0) FROM disc_mast WHERE item_code={itemCode} AND comp_code={gv.PubCompCode} AND code={discGrpCode}";
                    decimal itemDiff;

                    using (var cmd = new SqlCommand(qry, con))
                        itemDiff = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);

                    result.Add(new SaudaItemDetail
                    {
                        ITEM_CODE = itemCode,
                        SIZE_DIFF = sizeDiff,
                        COLOR_DIFF = colorDiff,
                        GRAM_DIFF = gramDiff,
                        ITEM_DIFF = itemDiff
                    });
                }

                return new RepositoryResponseList<SaudaItemDetail> { status = true, data = result };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<SaudaItemDetail> { status = false, message = ex.Message };
            }
        }
        public RepositoryResponse PostSalesReturn(string vType, int vNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                // Posting
                //if (vType == "SAJR")
                //{
                //    _accountPostingService.ACTPostingSaleChallan(
                //        gv.PubFYearCode,
                //        vType,
                //        vNo);
                //}
                //else
                //{
                //    _accountPostingService.ACTPostingSalesReturn(
                //        gv.PubFYearCode,
                //        vType,
                //        vNo);
                //}

                // Check Ledger2
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(@"SELECT 1 FROM LEDGER2 WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE
                                                        AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                    con.Open();

                    var exists = cmd.ExecuteScalar() != null;

                    if (!exists)
                    {
                        return new RepositoryResponse { status = false, message = $"Voucher not posted of VType:{vType} and VNo:{vNo}" };
                    }
                }

                return new RepositoryResponse { status = true };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "Error while posting voucher." };
            }
        }
        public RepositoryResponseData<object> SaveTransport(SaveTransportRequest req)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.VType) || req.VNo <= 0)
                    return new RepositoryResponseData<object> { status = false, message = "Invalid request." };

                string purDocId = "", saleDocId = "";

                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    if (req.TransportName != "" && req.GrNo != "")
                    {
                        using (var cmd = new SqlCommand(@"SELECT TOP 1 CONCAT(V_TYPE,V_NO) FROM PURCHASE1 WHERE CONCAT(V_TYPE,V_NO)<>@DOC_KEY AND TRANSPORT_NAME=@TNAME AND GR_NO=@GR_NO
                                                            AND V_TYPE NOT IN (SELECT CODE FROM DOCTYPE_MAST WHERE DOCTYPE='MaterialReceipt') AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=
                                                            @BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con))
                        {
                            cmd.Parameters.AddWithValue("@DOC_KEY", req.VType + req.VNo);
                            cmd.Parameters.AddWithValue("@TNAME", req.TransportName);
                            cmd.Parameters.AddWithValue("@GR_NO", req.GrNo);
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                            purDocId = Convert.ToString(cmd.ExecuteScalar()) ?? "";
                        }

                        using (var cmd = new SqlCommand(@"SELECT TOP 1 CONCAT(V_TYPE,V_NO) FROM SALE1 WHERE CONCAT(V_TYPE,V_NO)<>@DOC_KEY AND TRANSPORT_NAME=@TNAME AND GR_NO=@GR_NO
                                                            AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con))
                        {
                            cmd.Parameters.AddWithValue("@DOC_KEY", req.VType + req.VNo);
                            cmd.Parameters.AddWithValue("@TNAME", req.TransportName);
                            cmd.Parameters.AddWithValue("@GR_NO", req.GrNo);
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                            saleDocId = Convert.ToString(cmd.ExecuteScalar()) ?? "";
                        }
                    }

                    // ---- 4. Update (stored procedure) ----
                    using (var cmd = new SqlCommand("sp_SalesReturn", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@ACTION", "UpdateTptDetails");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", req.VType);
                        cmd.Parameters.AddWithValue("@V_NO", req.VNo);

                        cmd.Parameters.AddWithValue("@FRT_AMT", req.FrtAmt);
                        cmd.Parameters.AddWithValue("@TDS_PER", req.TdsPer);
                        cmd.Parameters.AddWithValue("@TDS_AMT", req.TdsAmt);
                        cmd.Parameters.AddWithValue("@FRT_TOPAY", req.FrtToPay);
                        cmd.Parameters.AddWithValue("@LOAD_PER", req.LoadPer);
                        cmd.Parameters.AddWithValue("@LOAD_AMT", req.LoadAmt);
                        cmd.Parameters.AddWithValue("@LOAD_AC", req.LoadAc);
                        cmd.Parameters.AddWithValue("@LOAD_REM", (object)req.LoadRem ?? "");
                        cmd.Parameters.AddWithValue("@WB_AMT", req.WbAmt);
                        cmd.Parameters.AddWithValue("@WB_AC", req.WbAc);
                        cmd.Parameters.AddWithValue("@GR_NO", req.GrNo);
                        cmd.Parameters.Add("@GR_DATE", SqlDbType.Date).Value = req.GrDate.HasValue ? (object)req.GrDate.Value.Date : DBNull.Value;
                        cmd.Parameters.AddWithValue("@TRANSPORT_CODE", req.TransportCode);
                        cmd.Parameters.AddWithValue("@TRANSPORT_NAME", req.TransportName);
                        cmd.Parameters.AddWithValue("@VEHICLE_NO", req.VehicleNo);
                        cmd.Parameters.AddWithValue("@DRIVER_NAME", (req.DriverName ?? "").Trim());
                        cmd.Parameters.AddWithValue("@DRIVER_NO", (req.DriverNo ?? "").Trim());
                        cmd.Parameters.AddWithValue("@REMARK", (object)req.Remark ?? "");
                        cmd.Parameters.AddWithValue("@INSU_PER", req.InsuPer);
                        cmd.Parameters.AddWithValue("@INSU_AMT", req.InsuAmt);

                        cmd.ExecuteNonQuery();
                    }
                }
                var warnings = new List<string>();
                if (purDocId != "")
                    warnings.Add($"Transport Name '{req.TransportName}' with GRNo='{req.GrNo}' already exist in Purchase Bill/Direct Exps/JW/Imported Exps/Return No: {purDocId}");
                if (saleDocId != "")
                    warnings.Add($"Transport Name '{req.TransportName}' with GRNo='{req.GrNo}' already exist in Sale/JW Issue/Sale Return invoice No: {saleDocId}");
                return new RepositoryResponseData<object> { status = true, message = "Transport data updated successfully.", data = warnings };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<object> { status = false, message = "Error occurred while saving transport data." };
            }
        }
    }
}
