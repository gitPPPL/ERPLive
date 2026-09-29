using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class DeliveryOrderRepository : IDeliveryOrderRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public DeliveryOrderRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
        }

        public async Task<object> SaveAndUpdateDataAsync([FromBody] DeliveryOrderModel model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            string? vtype = model.V_TYPE;
            string docId = vtype + model.V_NO;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                SqlTransaction transaction = con.BeginTransaction();

                try
                {

                    var generalSetting = await _globalVariableService.LoadGeneralSetting();
                    var validation = await ValidateBeforeSaveAsync(model, con, transaction, globalVariable, generalSetting.pubDefSSINSO);

                    if (!validation.CanSave)
                    {
                        await transaction.RollbackAsync();

                        return new { success = false, message = validation.Message };
                    }

                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT
                        c.STATE_CODE AS StateCode,
                        s.NAME AS StateName,
                        c.COUNTRY_CODE AS CountryCode,
                        co.NAME AS CountryName
                    FROM CITY_MAST c
                    LEFT JOIN STATE_MAST s ON s.CODE = c.STATE_CODE
                    LEFT JOIN COUNTRY_MAST co ON co.CODE = c.COUNTRY_CODE
                    WHERE c.CODE = @CITY_CODE", con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CITY_CODE",(object?)model.BILL_CITY ?? DBNull.Value);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                model.BILL_STATE = reader["StateCode"] == DBNull.Value ? null : Convert.ToInt32(reader["StateCode"]);
                                model.BILL_STATENAME = reader["StateName"] == DBNull.Value ? null : reader["StateName"].ToString();
                                model.BILL_COUNTRY = reader["CountryCode"] == DBNull.Value ? null : Convert.ToInt32(reader["CountryCode"]);
                                model.BILL_COUNTRYNAME = reader["CountryName"] == DBNull.Value ? null : reader["CountryName"].ToString();
                            }
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT
                        c.STATE_CODE AS StateCode,
                        s.NAME AS StateName,
                        c.COUNTRY_CODE AS CountryCode,
                        co.NAME AS CountryName
                    FROM CITY_MAST c
                    LEFT JOIN STATE_MAST s ON s.CODE = c.STATE_CODE
                    LEFT JOIN COUNTRY_MAST co ON co.CODE = c.COUNTRY_CODE
                    WHERE c.CODE = @CITY_CODE", con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CITY_CODE",(object?)model.SHIP_CITY ?? DBNull.Value);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                model.SHIP_STATE = reader["StateCode"] == DBNull.Value ? null : Convert.ToInt32(reader["StateCode"]);
                                model.SHIP_STATENAME = reader["StateName"] == DBNull.Value ? null : reader["StateName"].ToString();
                                model.SHIP_COUNTRY = reader["CountryCode"] == DBNull.Value ? null : Convert.ToInt32(reader["CountryCode"]);
                                model.SHIP_COUNTRYNAME = reader["CountryName"] == DBNull.Value ? null : reader["CountryName"].ToString();
                            }
                        }
                    }

                    // Get Insurance Credit Days from CRLIMIT_MAST
                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT ISNULL(CR_DAYS, 0)
                    FROM CRLIMIT_MAST
                    WHERE PARTY_CODE = @PARTY_CODE
                      AND COMP_CODE = @COMP_CODE", con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@PARTY_CODE", model.BILL_CODE);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                        object? result = await cmd.ExecuteScalarAsync();

                        model.INSUCR_DAYS = result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
                    }

                    bool isUpdate;

                    using (SqlCommand checkCmd = new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM DO1
                    WHERE YEAR_CODE = @YEAR_CODE
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND V_TYPE = @V_TYPE
                      AND V_NO = @V_NO", con, transaction))
                    {
                        checkCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        checkCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        checkCmd.Parameters.AddWithValue("@V_TYPE", vtype);
                        checkCmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);

                        isUpdate = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    }

                    using (SqlCommand cmd = new SqlCommand("sp_DeliveryOrder", con, transaction))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", isUpdate ? "UpdateData" : "InsertHeader");

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                        cmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DEL_DATE", (object?)model.DEL_DATE ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@GODOWN_CODE", (object?)model.GODOWN_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_CODE", (object?)model.BILL_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_NAME", (object?)model.BILL_NAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_ADD1", (object?)model.BILL_ADD1 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_ADD2", (object?)model.BILL_ADD2 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_ADD3", (object?)model.BILL_ADD3 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_CITY", (object?)model.BILL_CITY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_CITYNAME", (object?)model.BILL_CITYNAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_STATE", (object?)model.BILL_STATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_STATENAME", (object?)model.BILL_STATENAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_COUNTRY", (object?)model.BILL_COUNTRY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_COUNTRYNAME", (object?)model.BILL_COUNTRYNAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_GST", (object?)model.BILL_GST ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_PINCODE", (object?)model.BILL_PINCODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@INSUCR_DAYS", (object?)model.INSUCR_DAYS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_CODE", (object?)model.SHIP_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_NAME", (object?)model.SHIP_NAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_ADD1", (object?)model.SHIP_ADD1 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_ADD2", (object?)model.SHIP_ADD2 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_ADD3", (object?)model.SHIP_ADD3 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_CITY", (object?)model.SHIP_CITY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_CITYNAME", (object?)model.SHIP_CITYNAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_STATE", (object?)model.SHIP_STATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_STATENAME", (object?)model.SHIP_STATENAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_COUNTRY", (object?)model.SHIP_COUNTRY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_COUNTRYNAME", (object?)model.SHIP_COUNTRYNAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_GST", (object?)model.SHIP_GST ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_PINCODE", (object?)model.SHIP_PINCODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TAX_CODE", (object?)model.TAX_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PACK_TYPE", (object?)model.PACK_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PACK_NO", (object?)model.PACK_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ITEM_TYPE", (object?)model.ITEM_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WB_NO", (object?)model.WB_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WB_TYPE", (object?)model.WB_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ISSUE_NO", (object?)model.ISSUE_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ISSUE_TYPE", (object?)model.ISSUE_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AMOUNT", (object?)model.AMOUNT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PACK_PER", (object?)model.PACK_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PACK_AMT", (object?)model.PACK_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TCS_PER", (object?)model.TCS_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TCS_AMT", (object?)model.TCS_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CGST_PER", (object?)model.CGST_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CGST_AMT", (object?)model.CGST_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SGST_PER", (object?)model.SGST_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SGST_AMT", (object?)model.SGST_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IGST_PER", (object?)model.IGST_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IGST_AMT", (object?)model.IGST_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CESS_PER", (object?)model.CESS_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CESS_AMT", (object?)model.CESS_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LOAD_PER", (object?)model.LOAD_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LOAD_AMT", (object?)model.LOAD_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LOAD_AC", (object?)model.LOAD_AC ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LOAD_REM", (object?)model.LOAD_REM ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WB_AMT", (object?)model.WB_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WB_AC", (object?)model.WB_AC ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TOT_NOS", (object?)model.TOT_NOS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FRT_AMT", (object?)model.FRT_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ROUND_OFF", (object?)model.ROUND_OFF ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NAMOUNT", (object?)model.NAMOUNT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@INSU_PER", (object?)model.INSU_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@INSU_AMT", (object?)model.INSU_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TDS_PER", (object?)model.TDS_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TDS_AMT", (object?)model.TDS_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FRT_TAXPER", (object?)model.FRT_TAXPER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FRT_TAXAMT", (object?)model.FRT_TAXAMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WB_REM", (object?)model.WB_REM ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TOT_GROSS", (object?)model.TOT_GROSS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TOT_NET", (object?)model.TOT_NET ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@GR_NO", (object?)model.GR_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@GR_DATE", (object?)model.GR_DATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@VEHICLE_NO", (object?)model.VEHICLE_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TRANSPORT_CODE", (object?)model.TRANSPORT_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TRANSPORT_NAME", (object?)model.TRANSPORT_NAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DRIVER_NAME", (object?)model.DRIVER_NAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DRIVER_NO", (object?)model.DRIVER_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TPT_MODE", (object?)model.TPT_MODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TPT_DISTANCE", (object?)model.TPT_DISTANCE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WAYBILL_NO", (object?)model.WAYBILL_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FRT_TOPAY", (object?)model.FRT_TOPAY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARK", (object?)model.REMARK ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WB_QTY", (object?)model.WB_QTY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DISC_PER", (object?)model.DISC_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DISC_AMT", (object?)model.DISC_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CDISC_PER", (object?)model.CDISC_PER ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CDISC_AMT", (object?)model.CDISC_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AGENT_CODE", (object?)model.AGENT_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FORM_CODE", (object?)model.FORM_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SAUDA_TYPE", (object?)model.SAUDA_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SAUDA_NO", (object?)model.SAUDA_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SAUDA_RATE", (object?)model.SAUDA_RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DEFECTIVE_GOODS", (object?)model.DEFECTIVE_GOODS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CAL_ONPCS", (object?)model.CAL_ONPCS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PRINT_DETAIL", (object?)model.PRINT_DETAIL ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EXRATE", (object?)model.EXRATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BUYER_ORDNO", (object?)model.BUYER_ORDNO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PLACE_RECEIPT", (object?)model.PLACE_RECEIPT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PORT_LOADING", (object?)model.PORT_LOADING ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PORT_DISCHARGE", (object?)model.PORT_DISCHARGE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FINAL_DEST", (object?)model.FINAL_DEST ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FINAL_DEST_COUNTRY", (object?)model.FINAL_DEST_COUNTRY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DELIVERY_TERMS", (object?)model.DELIVERY_TERMS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SB_NO", (object?)model.SB_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SB_DATE", (object?)model.SB_DATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PORT_CODE", (object?)model.PORT_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LUT_DETAIL", (object?)model.LUT_DETAIL ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@INSU_DETAIL", (object?)model.INSU_DETAIL ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SUPPLY_TYPE", (object?)model.SUPPLY_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LC_NO", (object?)model.LC_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIPMENT_TYPE", "OUR");
                        cmd.Parameters.AddWithValue("@TRAN_TYPE", (object?)model.TRAN_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@STATUS", (object?)model.STATUS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PAYMENT_TERM", (object?)model.PAYMENT_TERM ?? DBNull.Value);
                        
                        cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        await cmd.ExecuteNonQueryAsync();

                        
                    }

                    foreach (var item in model.Footer)
                    {
                        using (SqlCommand itemCmd = new SqlCommand(@"
                            SELECT PRINT_NAME, NAME, UNIT_NAME, UNIT_CODE, HSN_CODE
                            FROM ITEM_MAST
                            WHERE CODE = @ITEM_CODE
                              AND COMP_CODE = @COMP_CODE", con, transaction))
                        {
                            itemCmd.Parameters.AddWithValue("@ITEM_CODE", (object?)item.ITEM_CODE ?? DBNull.Value);
                            itemCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                            using (SqlDataReader reader = await itemCmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    string printName = Convert.ToString(reader["PRINT_NAME"])?.Trim() ?? "";
                                    string itemName = Convert.ToString(reader["NAME"]) ?? "";

                                    item.ITEM_NAME = !string.IsNullOrWhiteSpace(printName) ? Convert.ToString(reader["PRINT_NAME"]) : itemName;
                                    item.UNIT_NAME = reader["UNIT_NAME"] == DBNull.Value ? null : Convert.ToString(reader["UNIT_NAME"]);
                                    item.UNIT_CODE = reader["UNIT_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["UNIT_CODE"]);
                                    item.HSN_CODE = reader["HSN_CODE"] == DBNull.Value ? null : Convert.ToString(reader["HSN_CODE"]);
                                }
                            }
                        }

                        using (SqlCommand detailCmd = new SqlCommand("sp_DeliveryOrder", con, transaction))
                        {
                            detailCmd.CommandType = CommandType.StoredProcedure;

                            detailCmd.Parameters.AddWithValue("@Action", "InsertFooter");

                            detailCmd.Parameters.AddWithValue("@DOC_ID", docId);
                            detailCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            detailCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            detailCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            detailCmd.Parameters.AddWithValue("@V_TYPE", (object?)model.V_TYPE ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);

                            detailCmd.Parameters.AddWithValue("@ITEM_CODE", (object?)item.ITEM_CODE ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@ITEM_NAME", (object?)item.ITEM_NAME ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@UNIT_NAME", (object?)item.UNIT_NAME ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@UNIT_CODE", (object?)item.UNIT_CODE ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@HSN_CODE", (object?)item.HSN_CODE ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@NOS", (object?)item.NOS ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@GROSS_QTY", (object?)item.GROSS_QTY ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@QTY", (object?)item.QTY ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@FOR_RATE", (object?)item.FOR_RATE ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@RATE", (object?)item.RATE ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@AMOUNT", (object?)item.AMOUNT ?? DBNull.Value);

                            detailCmd.Parameters.AddWithValue("@PACK_PER", (object?)item.PACK_PER ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@PACK_AMT", (object?)item.PACK_AMT ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@DISC_PER", (object?)item.DISC_PER ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@DISC_AMT", (object?)item.DISC_AMT ?? DBNull.Value);

                            // Item-wise taxes: from footer
                            detailCmd.Parameters.AddWithValue("@CGST_PER", (object?)model.CGST_PER ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@CGST_AMT", (object?)model.CGST_AMT ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@SGST_PER", (object?)model.SGST_PER ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@SGST_AMT", (object?)model.SGST_AMT ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@IGST_PER", (object?)model.IGST_PER ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@IGST_AMT", (object?)model.IGST_AMT ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@CESS_PER", (object?)model.CESS_PER ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@CESS_AMT", (object?)model.CESS_AMT ?? DBNull.Value);

                            detailCmd.Parameters.AddWithValue("@REMARK", (object?)item.REMARK ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@STATUS", (object?)model.STATUS ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@PACK_TYPE", (object?)item.PACK_TYPE ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@PACK_NO", (object?)item.PACK_NO ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@LOT_No", (object?)item.LOT_No ?? DBNull.Value);

                            detailCmd.Parameters.AddWithValue("@SAUDA_TYPE", (object?)item.SAUDA_TYPE ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@SAUDA_NO", (object?)item.SAUDA_NO ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@SAUDA_RATE", (object?)item.SAUDA_RATE ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@ORD_TYPE", (object?)item.ORD_TYPE ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@ORD_NO", (object?)item.ORD_NO ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@ORD_RATE", (object?)item.ORD_RATE ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@DCN_TYPE", (object?)item.DCN_TYPE ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@DCN_NO", (object?)item.DCN_NO ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@ROW_ID", (object?)item.ROW_ID ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@TAX_CODE", (object?)item.TAX_CODE ?? DBNull.Value);

                            detailCmd.Parameters.AddWithValue("@FRT_AMT", (object?)item.FRT_AMT ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@INSU_AMT", (object?)item.INSU_AMT ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@CDISC_AMT", (object?)item.CDISC_AMT ?? DBNull.Value);
                            detailCmd.Parameters.AddWithValue("@WB_QTY", (object?)item.WB_QTY ?? DBNull.Value);

                            detailCmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            detailCmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            detailCmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                            detailCmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                            await detailCmd.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();

                    return new { success = true, message = isUpdate ? "Data updated successfully." : "Data saved successfully." };
                }
                catch (Exception ex)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                    }
                    catch (Exception rollbackEx)
                    {
                        Console.WriteLine($"Rollback Error: {rollbackEx.Message}");
                    }

                    return new
                    {
                        success = false,
                        message = ex.Message
                    };
                }
            }
        }

        private async Task<(bool CanSave, string Message, string FAppStatus, string FAppRemark, int FAppUserCode)> ValidateBeforeSaveAsync(DeliveryOrderModel model, SqlConnection con, SqlTransaction transaction, dynamic globalVariable, string defSSINSO)
        {
            string vtype = model.V_TYPE ?? "";
            int vno = model.V_NO ?? 0;
            int partyCode = model.BILL_CODE ?? 0;
            int compCode = globalVariable.PubCompCode;
            int branchCode = globalVariable.PubBranchCode;
            int userCode = globalVariable.PubUserId;

            bool isApprovalBody = false;
            bool isFinalApprovalBody = false;
            bool isFinalApprovalBodyCS = false;
            bool isFinalApprovalBodyLCS = false;

            string fappstatus = "";
            string fappRemark = "";
            int fappUserCode = 0;

            var messages = new List<string>();

            // 1. Approval-stage checks
            using (SqlCommand cmd = new SqlCommand(@"
			SELECT
				CASE WHEN EXISTS (
					SELECT 1 FROM DOC_APPROSTAGE
					WHERE USER_CODE = @USER_CODE
					  AND DOC_CODE = @DOC_CODE
					  AND COMP_CODE = @COMP_CODE
				) THEN 1 ELSE 0 END AS ApprovalBody,

				CASE WHEN EXISTS (
					SELECT 1 FROM DOC_APPROSTAGE
					WHERE USER_CODE = @USER_CODE
					  AND DOC_CODE = @DOC_CODE
					  AND COMP_CODE = @COMP_CODE
					  AND APPROV_USER = 'FINAL'
				) THEN 1 ELSE 0 END AS FinalApproval,

				CASE WHEN EXISTS (
					SELECT 1 FROM DOC_APPROSTAGE
					WHERE USER_CODE = @USER_CODE
					  AND DOC_CODE = @DOC_CODE
					  AND COMP_CODE = @COMP_CODE
					  AND FLAG_B = 'CS'
					  AND APPROV_USER = 'FINAL'
				) THEN 1 ELSE 0 END AS FinalCS,

				CASE WHEN EXISTS (
					SELECT 1 FROM DOC_APPROSTAGE
					WHERE USER_CODE = @USER_CODE
					  AND DOC_CODE = @DOC_CODE
					  AND COMP_CODE = @COMP_CODE
					  AND FLAG_B = 'LCS'
					  AND APPROV_USER = 'FINAL'
				) THEN 1 ELSE 0 END AS FinalLCS",
                    con, transaction))
            {
                cmd.Parameters.AddWithValue("@USER_CODE", userCode);
                cmd.Parameters.AddWithValue("@DOC_CODE", vtype);
                cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        isApprovalBody = Convert.ToInt32(reader["ApprovalBody"]) == 1;
                        isFinalApprovalBody = Convert.ToInt32(reader["FinalApproval"]) == 1;
                        isFinalApprovalBodyCS = Convert.ToInt32(reader["FinalCS"]) == 1;
                        isFinalApprovalBodyLCS = Convert.ToInt32(reader["FinalLCS"]) == 1;
                    }
                }
            }

            // Original VB also loads the CS approval user names.
            string usrs = "";

            using (SqlCommand cmd = new SqlCommand(@"
			SELECT b.USER_NAME
			FROM DOC_APPROSTAGE a
			LEFT JOIN USER_MAST b ON a.USER_CODE = b.CODE
			WHERE a.FLAG_B = 'CS'
			  AND a.DOC_CODE = @DOC_CODE
			  AND a.COMP_CODE = @COMP_CODE",
                    con, transaction))
            {
                cmd.Parameters.AddWithValue("@DOC_CODE", vtype);
                cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    var userNames = new List<string>();

                    while (await reader.ReadAsync())
                    {
                        if (reader["USER_NAME"] != DBNull.Value)
                            userNames.Add(Convert.ToString(reader["USER_NAME"]) ?? "");
                    }

                    usrs = string.Join("/", userNames);
                }
            }

            // 2. Sold item must exist in Delivery Order
            using (SqlCommand cmd = new SqlCommand(@"
			SELECT DISTINCT ITEM_CODE
			FROM SALE2
			WHERE STATUS <> 2
			  AND ORD_TYPE = 'DOGT'
			  AND ORD_NO = @V_NO
			  AND COMP_CODE = @COMP_CODE
			  AND BRANCH_CODE = @BRANCH_CODE",
                    con, transaction))
            {
                cmd.Parameters.AddWithValue("@V_NO", vno);
                cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", branchCode);

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int itemCode = Convert.ToInt32(reader["ITEM_CODE"]);
                        bool itemExists = model.Footer != null && model.Footer.Any(x => (x.ITEM_CODE ?? 0) == itemCode);

                        if (!itemExists)
                        {
                            return (false, "Sold item must exist in Order.", "", "", 0);
                        }
                    }
                }
            }

            // 3. Sauda validation
            int fg = 0;
            int unmatchedSaudaNo = 0;

            if (defSSINSO == "Yes")
            {
                foreach (var item in model.Footer ?? Enumerable.Empty<dynamic>())
                {
                    // Validate rows containing an item.
                    if ((item.ITEM_CODE ?? 0) == 0)
                        continue;

                    using (SqlCommand cmd = new SqlCommand(@"
					SELECT 1
					FROM SAUDA
					WHERE V_TYPE = 'SAUD'
					  AND V_NO = @SAUDA_NO
					  AND PARTY_CODE = @PARTY_CODE
					  AND ITEM_CODE = @ITEM_CODE
					  AND RATE = @RATE
					  AND COMP_CODE = @COMP_CODE
					  AND BRANCH_CODE = @BRANCH_CODE",
                            con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@SAUDA_NO", (object?)item.SAUDA_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", item.ITEM_CODE);
                        cmd.Parameters.AddWithValue("@RATE", (object?)item.SAUDA_RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", branchCode);

                        object? result = await cmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            fg = 1;

                            if (unmatchedSaudaNo == 0)
                                unmatchedSaudaNo = Convert.ToInt32(item.SAUDA_NO ?? 0);
                        }
                    }
                }
            }

            // Same approval-status conditions as the original VB.
            if (isFinalApprovalBody || fg == 0)
            {
                fappstatus = "Approved";
                fappRemark = "Document Approved.";
                fappUserCode = userCode;
            }

            if (fg == 1 && !isFinalApprovalBody)
            {
                messages.Add($"Item Name/Party Name/Rate not matched with Sauda No: {unmatchedSaudaNo}, Approval Required.");
            }

            // 4. Credit-limit checks
            if (vtype != "SAJI")
            {
                decimal closingBalance;

                using (SqlCommand cmd = new SqlCommand(@"
				SELECT
					ISNULL(SUM(CASE WHEN CR_CODE = @PARTY_CODE
									THEN AMT ELSE 0 END), 0) * -1
				  + ISNULL(SUM(CASE WHEN DR_CODE = @PARTY_CODE
									THEN AMT ELSE 0 END), 0)
				FROM LEDGER2
				WHERE COMP_CODE = @COMP_CODE
				  AND CONCAT(V_TYPE, V_NO) <> @DOC_ID",
                        con, transaction))
                {
                    cmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@DOC_ID", vtype + vno);

                    closingBalance = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0m);
                }

                using (SqlCommand cmd = new SqlCommand(@"
				SELECT
					ISNULL(a.CREDIT_LIMIT, 0) AS CREDIT_LIMIT,
					ISNULL(a.LASTCREDIT_LIMIT, 0) AS LASTCREDIT_LIMIT,
					ISNULL(b.CREDIT_TYPE, '') AS CREDIT_TYPE
				FROM SUBGROUP_MAST a
				LEFT JOIN PAYTERM_MAST b
					ON a.PAYTERM_CODE = b.CODE
				   AND a.COMP_CODE = b.COMP_CODE
				WHERE a.CODE = @PARTY_CODE
				  AND a.COMP_CODE = @COMP_CODE",
                        con, transaction))
                {
                    cmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            decimal creditLimit = Convert.ToDecimal(reader["CREDIT_LIMIT"]);

                            decimal lastCreditLimit = Convert.ToDecimal(reader["LASTCREDIT_LIMIT"]);

                            string creditType = Convert.ToString(reader["CREDIT_TYPE"]) ?? "";

                            decimal totalAmount = model.NAMOUNT ?? 0m;
                            decimal balanceAfterBill = closingBalance + totalAmount;

                            if (creditType == "Against LC" &&
                                string.IsNullOrWhiteSpace(model.LC_NO))
                            {
                                messages.Add("LC Number Required. Otherwise invoice will not Approve.");

                                fappstatus = "";
                                fappRemark = "";
                                fappUserCode = 0;
                            }
                            else if (isFinalApprovalBodyLCS)
                            {
                                if (balanceAfterBill > lastCreditLimit)
                                {
                                    // Original VB displays a warning but does not clear approval status.
                                    messages.Add($"Party Closing Bal. {closingBalance} + Bill Net Amount {totalAmount} Exceeds the allowed Final Credit Limit.");
                                }
                            }
                            else if (isFinalApprovalBodyCS)
                            {
                                if (balanceAfterBill > lastCreditLimit)
                                {
                                    messages.Add($"Party Closing Bal. {closingBalance} + Bill Net Amount {totalAmount} Exceeds the allowed Final Credit Limit. Please send Approval to Final Approval User.");

                                    fappstatus = "";
                                    fappRemark = "";
                                    fappUserCode = 0;
                                }
                            }
                            else if (creditLimit >= 0 && balanceAfterBill > creditLimit)
                            {
                                messages.Add($"Party Closing Bal. {closingBalance} + Bill Net Amount {totalAmount} Exceeds the allowed Credit Limit. Please send Approval to Approval User.");

                                fappstatus = "";
                                fappRemark = "";
                                fappUserCode = 0;
                            }
                        }
                    }
                }
            }

            // 5. WB quantity check
            if (compCode == 1 && !isFinalApprovalBodyLCS && vtype == "DOGT" && (model.PACK_NO ?? 0) <= 0)
            {
                foreach (var item in model.Footer ?? Enumerable.Empty<dynamic>())
                {
                    if ((item.ITEM_CODE ?? 0) != 0)
                    {
                        decimal netQty = item.NET_QTY ?? 0m;
                        decimal wbQty = item.WB_QTY ?? 0m;

                        if (netQty != wbQty)
                        {
                            // Same as old VB: no save-blocking action.
                        }
                    }
                }
            }

            return (true, string.Join(Environment.NewLine, messages), fappstatus, fappRemark, fappUserCode);
        }

        public async Task<object> LoadEditDataAsync(int vNo, string vType)
        {
            try
            {
                var globalVariable = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_DeliveryOrder", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "LoadDataOnEdit");
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return new { success = false, message = "Delivery Order data not found." };
                            }

                            var data = new Dictionary<string, object?>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                data[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }

                            var footer = new List<Dictionary<string, object?>>();

                            if (await reader.NextResultAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var item = new Dictionary<string, object?>();

                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        string columnName = reader.GetName(i);
                                        item[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    }

                                    footer.Add(item);
                                }
                            }

                            return new { success = true, data = data, footer = footer};
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message};
            }
        }



    }
}
