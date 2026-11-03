using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics.Metrics;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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

                    bool calOnPcs = model.CAL_ONPCS == 1;

                    var basicValidation = await ValidateBasicDataAsync(model, con, transaction, globalVariable, generalSetting, calOnPcs, isUpdate);
                   
                    if (!basicValidation.IsValid)
                    {
                        await transaction.RollbackAsync();
                        return new { success = false, message = basicValidation.Message };
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
            int compCode = Convert.ToInt32(globalVariable.PubCompCode);
            int branchCode = Convert.ToInt32(globalVariable.PubBranchCode);
            int userCode = Convert.ToInt32(globalVariable.PubUserId);

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
            //if (compCode == 1 && !isFinalApprovalBodyLCS && vtype == "DOGT" && (model.PACK_NO ?? 0) <= 0)
            //{
            //    foreach (var item in model.Footer ?? Enumerable.Empty<dynamic>())
            //    {
            //        if ((item.ITEM_CODE ?? 0) != 0)
            //        {
            //            decimal netQty = item.NET_QTY ?? 0m;
            //            decimal wbQty = item.WB_QTY ?? 0m;

            //            if (netQty != wbQty)
            //            {
                            
            //            }
            //        }
            //    }
            //}

            return (true, string.Join(Environment.NewLine, messages), fappstatus, fappRemark, fappUserCode);
        }

        //====================Validate Data ========================================
        private async Task<(bool IsValid, string Message)> ValidateBasicDataAsync(DeliveryOrderModel model, SqlConnection con, SqlTransaction transaction, dynamic globalVariable , GlobalGeneralSettingModel generalSetting, bool calOnPcs, bool isUpdate)
        {
            bool chkval = false;
            string issuevtype = "";

            if (string.IsNullOrWhiteSpace(model.V_TYPE))
                return (false, "Invoice Type is required.");

            if (model.V_NO <= 0)
                return (false, "Invalid Voucher No.");

            if (model.FORM_CODE <= 0)
                return (false, "Form Type is required.");

            if (globalVariable.PubCompCode == "1" && model.ITEM_TYPE == "PSF")
            {
                chkval = true;
            }
            else if ((globalVariable.PubCompCode == "2" || globalVariable.PubCompCode == "5") && (model.ITEM_TYPE == "Fabric" || model.ITEM_TYPE == "Sacks"))
            {
                chkval = true;
            }
            else if (globalVariable.PubCompCode == "4" && model.ITEM_TYPE == "Finish")
            {
                chkval = true;
            }

            if (model.BILL_CODE <= 0)
                return (false, "Party Name is required.");

            if (model.SUPPLY_TYPE != "EXPWOP" && model.SUPPLY_TYPE != "EXPWP")
            {
                const string creditLimitQuery = @"
                SELECT CR_LIMIT
                FROM CRLIMIT_MAST
                WHERE PARTY_CODE = @PARTY_CODE
                  AND COMP_CODE = @COMP_CODE";

                using (SqlCommand cmd = new SqlCommand(creditLimitQuery, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@PARTY_CODE", model.BILL_CODE);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                    object result = await cmd.ExecuteScalarAsync();

                    if (result == null || result == DBNull.Value)
                    {
                        return (false, "CR Limit Refused by Insurance company. Invoice can not generate.");
                    }

                    decimal crLimitAmt = Convert.ToDecimal(result);

                    if (crLimitAmt <= 0)
                        return (false, "Credit Limit is 0. Invoice can not generate.");

                    const string balanceQuery = @"
                    SELECT
                        ISNULL(SUM(CASE
                            WHEN DR_CODE = @PARTY_CODE AND DR_CODE > 0
                            THEN AMT ELSE 0 END), 0)
                        -
                        ISNULL(SUM(CASE
                            WHEN CR_CODE = @PARTY_CODE AND CR_CODE > 0
                            THEN AMT ELSE 0 END), 0)
                    FROM LEDGER2
                    WHERE COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE";

                    decimal osamt;

                    using (SqlCommand balanceCmd = new SqlCommand(balanceQuery, con, transaction))
                    {
                        balanceCmd.Parameters.AddWithValue("@PARTY_CODE", model.BILL_CODE);
                        balanceCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        balanceCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                        osamt = Convert.ToDecimal(await balanceCmd.ExecuteScalarAsync());
                    }

                    decimal balAmt = osamt + (model.NAMOUNT ?? 0m);

                    if (balAmt - crLimitAmt > 1)
                    {
                        return (false, "Total DO Amount exceeds Credit Limit (Incl. this DO). Invoice can not generate.");
                    }
                }
            }

            if (Convert.ToInt32(model.BILL_CITY ?? 0) <= 0)
                return (false, "'Bill To' Station is blank.");

            if (Convert.ToInt32(model.SHIP_CITY ?? 0) <= 0)
                return (false, "'Ship To' Station is blank.");

            if (model.V_TYPE == "DOGT" && (model.ITEM_TYPE == "Waste" || model.ITEM_TYPE == "Scrap"))
            {
                if ((model.TCS_PER ?? 0m) < 1m)
                {
                    // VB code displays the message but does not stop validation.
                    // Display this as a warning in the caller.
                }
            }

            bool checkIssueNo = false;
            issuevtype = "";

            if (generalSetting.pubDefISSUEINSI == "Yes")
            {
                if (model.ITEM_TYPE == "Store" || model.ITEM_TYPE == "Other" || model.ITEM_TYPE == "Scrap" || model.V_TYPE == "SACH")
                {
                    checkIssueNo = false;
                }
                else
                {
                    checkIssueNo = true;

                    int compCode = Convert.ToInt32(globalVariable.PubCompCode);

                    if (compCode == 1 || compCode == 3 || compCode == 7 || compCode == 8)
                    {
                        issuevtype = "RAID";
                    }
                    else
                    {
                        issuevtype = "RAIS";
                    }
                }
            }

            if (model.Footer == null || !model.Footer.Any())
                return (false, "Item Details Should not be Blank");

            string transportName = (model.TRANSPORT_NAME ?? "").Trim();
            string grNo = (model.GR_NO ?? "").Trim();

            if (transportName != "" && grNo != "")
            {
                string docId = (model.V_TYPE ?? "") + model.V_NO;

                string purchaseQuery = @"
                SELECT TOP 1 CONCAT(V_TYPE, V_NO)
                FROM PURCHASE1
                WHERE CONCAT(V_TYPE, V_NO) <> @DOC_ID
                  AND TRANSPORT_NAME = @TRANSPORT_NAME
                  AND GR_NO = @GR_NO
                  AND V_TYPE NOT IN
                      (SELECT CODE FROM DOCTYPE_MAST
                       WHERE DOCTYPE = 'MaterialReceipt')
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND YEAR_CODE = @YEAR_CODE";

                string purdocid = "";

                using (SqlCommand cmd = new SqlCommand(purchaseQuery, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@TRANSPORT_NAME", transportName);
                    cmd.Parameters.AddWithValue("@GR_NO", grNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                    purdocid = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
                }

                if (purdocid != "")
                {
                    string warning = $"Transport Name '{transportName}' with GRNo='{grNo}' already exist in Purchase Bill/Direct Exps/JW/Imported Exps/Return No:{purdocid}";
                }

                string salesQuery = @"
                SELECT TOP 1 CONCAT(V_TYPE, V_NO)
                FROM DO1
                WHERE ISNULL(Status, 0) <> 2
                  AND CONCAT(V_TYPE, V_NO) <> @DOC_ID
                  AND TRANSPORT_NAME = @TRANSPORT_NAME
                  AND GR_NO = @GR_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND YEAR_CODE = @YEAR_CODE";

                string saledocid = "";

                using (SqlCommand cmd = new SqlCommand(salesQuery, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@TRANSPORT_NAME", transportName);
                    cmd.Parameters.AddWithValue("@GR_NO", grNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                    saledocid = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
                }

                if (saledocid != "")
                {
                    string warning = $"Transport Name '{transportName}' with GRNo='{grNo}' already exist in Sale/JW Issue/Sale Return invoice No:{saledocid}";
                }
            }

            if (!string.IsNullOrWhiteSpace(grNo) && model.GR_DATE == null)
                return (false, "GR Date is required with GR No.");

            if ((model.FRT_AMT ?? 0m) > 0m && string.IsNullOrWhiteSpace(transportName))
                return (false, "Please select Transport.");

            if ((model.FRT_AMT ?? 0m) > 0m && !string.IsNullOrWhiteSpace(transportName))
            {
                string transportPartyQuery = @"
                SELECT PARTY_CODE
                FROM TRANSPORT_MAST
                WHERE CODE = @TRANSPORT_CODE
                  AND COMP_CODE = @COMP_CODE
                  AND ACTIVE = 1";

                object? tptResult;

                using (SqlCommand cmd = new SqlCommand(transportPartyQuery, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@TRANSPORT_CODE", model.TRANSPORT_CODE ?? 0);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                    tptResult = await cmd.ExecuteScalarAsync();
                }

                int tptCode = tptResult == null || tptResult == DBNull.Value ? 0 : Convert.ToInt32(tptResult);

                if (tptCode == 0)
                {
                    return (false, $"Party Name not Linked with Transport => {transportName}, Please update first.");
                }

                string partyExistsQuery = @"
                SELECT 1
                FROM SUBGROUP_MAST
                WHERE CODE = @PARTY_CODE
                  AND COMP_CODE = @COMP_CODE
                  AND ACTIVE = 1";

                using (SqlCommand cmd = new SqlCommand(partyExistsQuery, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@PARTY_CODE", tptCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                    object? partyResult = await cmd.ExecuteScalarAsync();

                    if (partyResult == null || partyResult == DBNull.Value)
                    {
                        return (false,$"Party not linked in Transport Master OR not Active/Exist in BP Master which is Linked with Transport=>{transportName}");
                    }
                }
            }

            // Item Details Should Not Be Blank
            if (model.Footer == null || !model.Footer.Any())
            {
                return (false, "Item Details Should not be Blank");
            }

            //=================Footer Validation=========================

            for (int i = 0; i < model.Footer.Count; i++)
            {
                var item = model.Footer[i];

                int itemCode = Convert.ToInt32(item.ITEM_CODE ?? 0);

                // Same as VB: validate only rows having Item Code <> 0
                if (itemCode == 0)
                    continue;

                string itemName = item.ITEM_NAME ?? "";

                // Item Name Validation
                if (string.IsNullOrWhiteSpace(itemName))
                {
                    return (false, $"Item Name Should not be Blank of Item Code={itemCode}");
                }

                // Get HSN Code from ITEM_MAST
                string hsnQuery = @"
                SELECT ISNULL(HSN_CODE, 0)
                FROM ITEM_MAST
                WHERE CODE = @ITEM_CODE
                  AND COMP_CODE = @COMP_CODE";

                string hsnCode;

                using (SqlCommand cmd = new SqlCommand(hsnQuery, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                    hsnCode = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
                }

                // HSN must be at least 6 characters
                if (hsnCode.Trim().Length < 6)
                {
                    return (false, $"HSN Code must be 6 digits long of Item = {itemCode}");
                }

                // Kept from VB logic; this condition is unreachable after the check above.
                if (hsnCode.Length < 4)
                {
                    return (false, $"Invalid HSN Code in item mast of Item = {itemCode}");
                }

                // Product Type / Item Group Validation
                if (model.V_TYPE != "SASI")
                {
                    string groupQuery = @"
                    SELECT SALE_GROUP
                    FROM ITEM_MAST a
                    LEFT JOIN ITEM_GROUP b
                        ON a.GROUP_CODE = b.CODE
                       AND a.COMP_CODE = b.COMP_CODE
                    WHERE a.CODE = @ITEM_CODE
                      AND a.COMP_CODE = @COMP_CODE";

                    string mgroup;

                    using (SqlCommand cmd = new SqlCommand(groupQuery, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                        mgroup = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
                    }

                    if (mgroup != (model.ITEM_TYPE ?? ""))
                    {
                        return (false, $"Item Name={itemName} (Group:{mgroup}), but you selected Product Type : {model.ITEM_TYPE} in header.");
                    }
                }

                // Quantity Validation
                // cbCalOnPcs.Checked == false means calculation is not on pieces.
                // Pass the checkbox value into this method if it is not already available here.
                if (!calOnPcs && Convert.ToDecimal(item.QTY ?? 0) == 0)
                {
                    return (false, $"Quantity Should not be Blank of Item Name={itemName}");
                }

                // Get CGST and IGST from ITEM_MAST
                string taxQuery = @"
                SELECT
                    ISNULL(CGST_PER, 0) AS CGST_PER,
                    ISNULL(IGST_PER, 0) AS IGST_PER
                FROM ITEM_MAST
                WHERE CODE = @ITEM_CODE
                  AND COMP_CODE = @COMP_CODE";

                decimal masterCgst = 0m;
                decimal masterIgst = 0m;

                using (SqlCommand cmd = new SqlCommand(taxQuery, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            masterCgst = Convert.ToDecimal(reader["CGST_PER"]);
                            masterIgst = Convert.ToDecimal(reader["IGST_PER"]);
                        }
                    }
                }

                decimal rowCgst = Convert.ToDecimal(item.CGST_PER ?? 0);
                decimal rowIgst = Convert.ToDecimal(item.IGST_PER ?? 0);

                if (rowCgst > 0)
                {
                    if (rowCgst != masterCgst)
                    {
                        return (false, $"Tax Percentage of Item =>{itemName} not matched as per Item Master.");
                    }
                }
                else if (rowIgst > 0)
                {
                    if (rowIgst != masterIgst)
                    {
                        return (false, $"Tax Percentage of Item =>{itemName} not matched as per Item Master.");
                    }
                }

                // =========================
                // DOGT VALIDATIONS
                // =========================
                if (model.V_TYPE == "DOGT")
                {
                    // -----------------------------------------
                    // 1. Check WB Qty with Net Qty
                    // -----------------------------------------
                    string getWBYN = "";
                    int godownCode = Convert.ToInt32(model.GODOWN_CODE ?? 0);

                    if (godownCode != 1)
                    {
                        const string wbQuery = @"
                        SELECT ISNULL(WB_YN, '')
                        FROM GODOWN_MAST
                        WHERE COMP_CODE = @COMP_CODE
                          AND CODE = @GODOWN_CODE";

                        using (SqlCommand cmd = new SqlCommand(wbQuery, con, transaction))
                        {
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@GODOWN_CODE", godownCode);

                            getWBYN = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
                        }
                    }

                    if (getWBYN == "Yes")
                    {
                        if (Convert.ToInt32(globalVariable.PubCompCode) == 1)
                        {
                            if (Convert.ToInt32(model.PACK_NO ?? 0) <= 0 && itemCode > 0)
                            {
                                decimal netQty = Convert.ToDecimal(item.QTY ?? 0m);
                                decimal wbQty = Convert.ToDecimal(item.WB_QTY ?? 0m);

                                if (netQty != wbQty)
                                {
                                    string warning = $"Net Qty not Matched with WB Qty, Please Check it of =>{item.ITEM_NAME}, Approval Required";
                                }
                            }
                        }
                    }

                    // -----------------------------------------
                    // 2. Sauda Approval and Freight Validation
                    // -----------------------------------------
                    if (generalSetting.pubDefSSINSI == "Yes")
                    {
                        int saudaNo = Convert.ToInt32(item.SAUDA_NO ?? 0);

                        if (itemCode > 0 && saudaNo > 0)
                        {
                            // Check Sauda approval
                            const string saudaApprovalQuery = @"
                            SELECT 1
                            FROM SAUDA
                            WHERE V_TYPE = 'SAUD'
                              AND V_NO = @V_NO
                              AND FAPROV_STATUS = 'Approved'
                              AND COMP_CODE = @COMP_CODE
                              AND BRANCH_CODE = @BRANCH_CODE";

                            using (SqlCommand cmd = new SqlCommand(saudaApprovalQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@V_NO", saudaNo);
                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                                object? result = await cmd.ExecuteScalarAsync();

                                if (result == null || result == DBNull.Value)
                                {
                                    return (false, $"Sauda No {saudaNo} not approved, Please Check it of ={item.ITEM_NAME}");
                                }
                            }

                            // Check freight term only when consignment freight > 0
                            if ((model.FRT_AMT ?? 0m) > 0m)
                            {
                                const string freightTermQuery = @"
                                SELECT ISNULL(FRT_TERM, '')
                                FROM SAUDA
                                WHERE V_TYPE = 'SAUD'
                                  AND V_NO = @V_NO
                                  AND COMP_CODE = @COMP_CODE
                                  AND BRANCH_CODE = @BRANCH_CODE";

                                string freightTerm;

                                using (SqlCommand cmd = new SqlCommand(
                                    freightTermQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@V_NO", saudaNo);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                                    freightTerm = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
                                }

                                if (freightTerm != "FOR")
                                {
                                    return (false, $"Freight payable by customer in Sauda No {saudaNo}, so freight can not be charged.");
                                }
                            }
                        }
                    }
                }

                // -----------------------------------------
                // 2. Packing Slip / Packing Qty Validation
                // -----------------------------------------
                if (itemCode > 0)
                {
                    if (Convert.ToInt32(globalVariable.PubCompCode) != 3)
                    {
                        if (chkval && model.V_TYPE == "DOGT")
                        {
                            if (generalSetting.pubDefPACKINSI == "Yes")
                            {
                                if (Convert.ToInt32(model.PACK_NO ?? 0) > 0)
                                {
                                    int packNo = Convert.ToInt32(item.PACK_NO ?? 0);
                                    int compCode = Convert.ToInt32(globalVariable.PubCompCode);

                                    // -----------------------------------------
                                    // 1. Check duplicate Packing Slip
                                    // -----------------------------------------
                                    const string duplicateQuery = @"
                                    SELECT ISNULL(MAX(V_NO), 0)
                                    FROM DO2
                                    WHERE PACK_TYPE = @PACK_TYPE
                                      AND PACK_NO = @PACK_NO
                                      AND ISNULL(STATUS, 0) <> 2
                                      AND ITEM_CODE = @ITEM_CODE
                                      AND COMP_CODE = @COMP_CODE
                                      AND BRANCH_CODE = @BRANCH_CODE
                                      AND YEAR_CODE = @YEAR_CODE
                                      AND V_TYPE = @V_TYPE
                                      AND V_NO <> @V_NO";

                                    using (SqlCommand cmd = new SqlCommand(duplicateQuery, con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@PACK_TYPE", model.PACK_TYPE ?? "");
                                        cmd.Parameters.AddWithValue("@PACK_NO", packNo);
                                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                                        cmd.Parameters.AddWithValue("@V_NO", Convert.ToInt32(model.V_NO ?? 0));
                                        int existingVNo = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

                                        if (existingVNo > 0)
                                        {
                                            return (false, $"Packing Slip No. already Exist in Sale, Serial No. {existingVNo}");
                                        }
                                    }

                                    // -----------------------------------------
                                    // 2. Packing No cannot be blank
                                    // -----------------------------------------
                                    if (packNo <= 0)
                                    {
                                        return (false, $"Packing No. can not be Blank, Please Check it of ={item.ITEM_NAME}");
                                    }

                                    // -----------------------------------------
                                    // 3. Determine Packing Type
                                    // -----------------------------------------
                                    string packType = "";

                                    if (model.V_TYPE == "DOGT" || model.V_TYPE == "SABS")
                                    {
                                        packType = model.ITEM_TYPE == "Flakes" ? "SFIS" : "FPIS";
                                    }
                                    else if (model.V_TYPE == "SACH")
                                    {
                                        packType = "FGIS";
                                    }

                                    // -----------------------------------------
                                    // 4. Get Packing Qty from PRODUCTION2
                                    // -----------------------------------------
                                    string packingQtyQuery;

                                    if (compCode == 2 || compCode == 4 || compCode == 5)
                                    {
                                        packingQtyQuery = @"
                                        SELECT ISNULL(SUM(QTY), 0)
                                        FROM PRODUCTION2
                                        WHERE ITEM_CODE = @ITEM_CODE
                                          AND V_NO = @PACK_NO
                                          AND V_TYPE = @PACK_TYPE
                                          AND COMP_CODE = @COMP_CODE
                                          AND BRANCH_CODE = @BRANCH_CODE";
                                    }
                                    else
                                    {
                                        packingQtyQuery = @"
                                        SELECT ISNULL(SUM(QTY), 0)
                                        FROM PRODUCTION2
                                        WHERE ITEM_CODE = @ITEM_CODE
                                          AND V_NO = @PACK_NO
                                          AND V_TYPE = @PACK_TYPE
                                          AND DOROW_ID = @DOROW_ID
                                          AND COMP_CODE = @COMP_CODE
                                          AND BRANCH_CODE = @BRANCH_CODE";
                                    }

                                    decimal packingQty;

                                    using (SqlCommand cmd = new SqlCommand(packingQtyQuery, con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                                        cmd.Parameters.AddWithValue("@PACK_NO", packNo);
                                        cmd.Parameters.AddWithValue("@PACK_TYPE", packType);
                                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                                        if (compCode != 2 && compCode != 4 && compCode != 5)
                                        {
                                            cmd.Parameters.AddWithValue("@DOROW_ID", item.ROW_ID ?? "");
                                        }

                                        packingQty = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0m);
                                    }

                                    // -----------------------------------------
                                    // 5. Compare Packing Qty with DO Qty
                                    // -----------------------------------------
                                    decimal doQty = Convert.ToDecimal(item.QTY ?? 0m);

                                    if (packingQty != doQty)
                                    {
                                        if (compCode == 2 || compCode == 4 || compCode == 5)
                                        {
                                            return (false, $"Packing Qty and DO Qty not matched of Item {item.ITEM_NAME}, Please Check it.");
                                        }
                                        else
                                        {
                                            string message = $"Packing Qty ({packingQty}) and DO Qty ({doQty}) not matched of Item {item.ITEM_NAME}, Please Check it.";

                                            // Original VB: company 7 does not block saving.
                                            if (compCode != 7)
                                            {
                                                return (false, message);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // -----------------------------------------
                // 3. Check Issue Item and Qty with Sale
                // -----------------------------------------
                int issueNo = Convert.ToInt32(model.ISSUE_NO ?? 0);
                string issueType = model.ISSUE_TYPE ?? "";

                if (issueNo > 0 && checkIssueNo == true)
                {
                    // Check whether Issue No exists
                    const string issueExistsQuery = @"
                    SELECT 1
                    FROM Issue2
                    WHERE V_TYPE = @V_TYPE
                      AND V_NO = @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE";

                    object? issueExists;

                    using (SqlCommand cmd = new SqlCommand(issueExistsQuery, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@V_TYPE", issueType);
                        cmd.Parameters.AddWithValue("@V_NO", issueNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                        issueExists = await cmd.ExecuteScalarAsync();
                    }

                    if (issueExists != null && issueExists != DBNull.Value)
                    {
                        // Check whether the Sale Item exists in Issue
                        const string issueItemQuery = @"
                        SELECT 1
                        FROM Issue2
                        WHERE ITEM_CODE = @ITEM_CODE
                          AND V_TYPE = @V_TYPE
                          AND V_NO = @V_NO
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE";

                        object? issueItemExists;

                        using (SqlCommand cmd = new SqlCommand(issueItemQuery, con, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", issueType);
                            cmd.Parameters.AddWithValue("@V_NO", issueNo);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            issueItemExists = await cmd.ExecuteScalarAsync();
                        }

                        if (issueItemExists != null && issueItemExists != DBNull.Value)
                        {
                            // Compare Issue Qty with Sale Qty
                            const string issueQtyQuery = @"
                            SELECT ISNULL(SUM(QTY), 0)
                            FROM Issue2
                            WHERE ITEM_CODE = @ITEM_CODE
                              AND V_TYPE = @V_TYPE
                              AND V_NO = @V_NO
                              AND COMP_CODE = @COMP_CODE
                              AND BRANCH_CODE = @BRANCH_CODE";

                            decimal issueQty;

                            using (SqlCommand cmd = new SqlCommand(issueQtyQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                                cmd.Parameters.AddWithValue("@V_TYPE", issueType);
                                cmd.Parameters.AddWithValue("@V_NO", issueNo);
                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                                issueQty = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0m);
                            }

                            decimal saleQty = Convert.ToDecimal(item.QTY ?? 0m);

                            if (issueQty != saleQty)
                            {
                                return (false, $"Issue Qty not matched with Sale Qty of Item:({itemCode}) {item.ITEM_NAME} Issue No: {issueNo}.");
                            }
                        }
                        else
                        {
                            return (false, $"Sale Item and Issue Item not matched, please check for Item:({itemCode}) {item.ITEM_NAME}, Issue No: {issueNo}.");
                        }
                    }
                    else
                    {
                        // Original VB only displays a message; Return False is commented out.
                        // Add this message to your existing warning mechanism if required:
                        string warning = $"Issue No: {issueNo} not found.";
                    }
                }

                // -----------------------------------------
                // 4. Check Stock Availability
                // -----------------------------------------
                if (model.V_TYPE != "SASI")
                {
                    const string stockQuery = @"
                    SELECT ISNULL(QTY, 0)
                    FROM tmpStockBalance
                    WHERE ITEM_CODE = @ITEM_CODE
                      AND COMP_CODE = @COMP_CODE";

                    decimal stockQty;

                    using (SqlCommand cmd = new SqlCommand(stockQuery, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                        stockQty = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0m);
                    }

                    // Original VB adds current DO quantity back during update.
                    // Apply this only when your existing update-mode flag is true.
                    if (isUpdate)
                    {
                        const string currentDoQtyQuery = @"
                        SELECT ISNULL(SUM(QTY), 0)
                        FROM DO2
                        WHERE ITEM_CODE = @ITEM_CODE
                          AND ISNULL(STATUS, 0) <> 2
                          AND V_TYPE = @V_TYPE
                          AND V_NO = @V_NO
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE
                          AND YEAR_CODE = @YEAR_CODE";

                        using (SqlCommand cmd = new SqlCommand(currentDoQtyQuery, con, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                            cmd.Parameters.AddWithValue("@V_NO", Convert.ToInt32(model.V_NO ?? 0));
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                            stockQty += Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0m);
                        }
                    }

                    if (stockQty <= 0)
                    {
                        // Original VB displays a warning but does not block saving.
                        string warning = $"Stock not available, Please Check it of ={item.ITEM_NAME}";
                    }
                }

                //--------------------------------------------
                // 5. DOGT Sauda Validation
                //--------------------------------------------
                if (model.V_TYPE == "DOGT" && model.ITEM_TYPE != "Other" && generalSetting.pubDefSSINSI == "Yes")
                {
                    // Sauda Type Required
                    if (string.IsNullOrWhiteSpace(item.SAUDA_TYPE))
                    {
                        return (false, $"Sauda Type can not be Blank, Please Check it of = {item.ITEM_NAME}");
                    }

                    // Sauda No Required
                    if (string.IsNullOrWhiteSpace(Convert.ToString(item.SAUDA_NO)))
                    {
                        return (false, $"Sauda No. can not be Blank, Please Check it of = {item.ITEM_NAME}");
                    }

                    // Sauda Rate Required
                    if (item.SAUDA_RATE == null ||
                        Convert.ToDecimal(item.SAUDA_RATE) == 0)
                    {
                        return (false, $"Sauda Rate can not be Blank, Please Check it of = {item.ITEM_NAME}");
                    }

                    decimal saudaQty = 0;
                    decimal saleQty = 0;

                    // Total Sauda Quantity
                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT ISNULL(SUM(QTY), 0)
                    FROM SAUDA
                    WHERE V_TYPE = @SAUDA_TYPE
                      AND V_NO = @SAUDA_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE", con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@SAUDA_TYPE", item.SAUDA_TYPE);
                        cmd.Parameters.AddWithValue("@SAUDA_NO", item.SAUDA_NO);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                        saudaQty = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                    }

                    // Already used quantity in Sale2
                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT ISNULL(SUM(B.QTY), 0)
                    FROM SALE1 A
                    LEFT JOIN SALE2 B
                      ON A.V_TYPE = B.V_TYPE
                     AND A.V_NO = B.V_NO
                     AND A.COMP_CODE = B.COMP_CODE
                     AND A.BRANCH_CODE = B.BRANCH_CODE
                     AND A.YEAR_CODE = B.YEAR_CODE
                    WHERE B.SAUDA_TYPE = 'SAUD'
                      AND B.SAUDA_NO = @SAUDA_NO
                      AND ISNULL(B.STATUS, 0) <> 2
                      AND A.COMP_CODE = @COMP_CODE
                      AND A.BRANCH_CODE = @BRANCH_CODE
                      AND A.V_TYPE = 'SAGT'
                      AND A.V_NO <> @V_NO", con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@SAUDA_NO", item.SAUDA_NO);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);

                        saleQty = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                    }

                    // Match old VB txtsauda condition
                    if (!string.IsNullOrWhiteSpace(model.SAUDA_NO?.ToString()))
                    {
                        saleQty += Convert.ToDecimal(model.TOT_NET ?? 0);
                    }
                    else
                    {
                        saleQty += Convert.ToDecimal(item.QTY ?? 0);
                    }

                    // Pending Sauda Quantity validation
                    if (saleQty > saudaQty + 3000)
                    {
                        return (false, $"Sauda Pending Quantity is = {saudaQty - saleQty:N2}, " + $"Your DO is = {model.TOT_NET:N2}, Please Check it");
                    }
                }

                //----------------------------------------------------------
                // 6.  Trade Discount and Cash Discount validation against Sauda
                //---------------------------------------------------------
                if (model.Footer != null && model.Footer.Count > 0 && model.Footer[0].ITEM_CODE != null && model.Footer[0].SAUDA_NO != null)
                {
                    var firstItem = model.Footer[0];

                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT DISC_PER, CDISC_PER
                    FROM SAUDA
                    WHERE COMP_CODE = @COMP_CODE
                      AND V_TYPE = @SAUDA_TYPE
                      AND V_NO = @SAUDA_NO", con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@SAUDA_TYPE", firstItem.SAUDA_TYPE);
                        cmd.Parameters.AddWithValue("@SAUDA_NO", firstItem.SAUDA_NO);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                decimal saudaDiscount = reader["DISC_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DISC_PER"]);

                                decimal saudaCashDiscount = reader["CDISC_PER"] == DBNull.Value  ? 0 : Convert.ToDecimal(reader["CDISC_PER"]);

                                if (Convert.ToDecimal(model.DISC_PER ?? 0) > 0 && saudaDiscount != Convert.ToDecimal(model.DISC_PER ?? 0))
                                {
                                    return (false, $"Discount not matched From Sauda no.=>{firstItem.SAUDA_NO}");
                                }

                                if (Convert.ToDecimal(model.CDISC_PER ?? 0) > 0 && saudaCashDiscount != Convert.ToDecimal(model.CDISC_PER ?? 0))
                                {
                                    return (false, $"Cash Discount not matched From Sauda no.=>{firstItem.SAUDA_NO}");
                                }
                            }
                        }
                    }
                }


                //====================== Check when do page done=-========================

                // Check Freight From Transport Rate Quotation
                //if (model.Tag == "DInv")
                //{
                //    decimal transportRate = 0;

                //    using (SqlCommand cmd = new SqlCommand(@"
                //    SELECT ISNULL(B.OUR_RATE, 0)
                //    FROM TRANSPORT_QT1 A
                //    LEFT JOIN TRANSPORT_QT2 B
                //        ON A.V_TYPE = B.V_TYPE
                //       AND A.V_NO = B.V_NO
                //       AND A.COMP_CODE = B.COMP_CODE
                //       AND A.BRANCH_CODE = B.BRANCH_CODE
                //       AND A.YEAR_CODE = B.YEAR_CODE
                //    WHERE A.DO_TYPE = 'DOGT'
                //      AND A.DO_NO = @V_NO
                //      AND ISNULL(A.TRUCK_NO, '') <> ''
                //      AND A.FAPROV_STATUS = 'Approved'
                //      AND A.COMP_CODE = @COMP_CODE", con, transaction))
                //    {
                //        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                //        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                //        var result = await cmd.ExecuteScalarAsync();

                //        if (result != null && result != DBNull.Value)
                //        {
                //            transportRate = Convert.ToDecimal(result);
                //        }
                //    }

                //    if (transportRate > 0)
                //    {
                //        decimal freight = Convert.ToDecimal(model.FRT_AMT ?? 0);

                //        if (freight != transportRate)
                //        {
                //            string warning = $"Please Check Freight in Transport rate Quotation ({transportRate}) " + $"and here is {freight}.";

                //        }
                //    }
                //}


                //------------------------------------------------------
                // Check Total Net Qty matched with Packing Slip Qty
                //------------------------------------------------------
                int totalPackNo = Convert.ToInt32(model.PACK_NO ?? 0);

                if (totalPackNo != 0)
                {
                    decimal packingSlipQty = 0;

                    if (model.ITEM_TYPE == "PSF" ||  model.ITEM_TYPE == "Fabric" || model.ITEM_TYPE == "Sacks" || model.ITEM_TYPE == "Finish")
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(B.QTY), 0)
                        FROM PRODUCTION1 A
                        LEFT JOIN PRODUCTION2 B
                            ON A.V_TYPE = B.V_TYPE
                           AND A.V_NO = B.V_NO
                           AND A.BRANCH_CODE = B.BRANCH_CODE
                           AND A.YEAR_CODE = B.YEAR_CODE
                        WHERE ISNULL(A.PORD_NO, 0) = @V_NO
                          AND A.PORD_TYPE = @V_TYPE
                          AND A.COMP_CODE = @COMP_CODE
                          AND A.BRANCH_CODE = @BRANCH_CODE", con, transaction))
                        {
                            cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                            cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            packingSlipQty = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                        }
                    }
                    else if (model.ITEM_TYPE == "Flakes")
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(B.QTY), 0)
                        FROM PROD_SFG1 A
                        LEFT JOIN PROD_SFG2 B
                            ON A.V_TYPE = B.V_TYPE
                           AND A.V_NO = B.V_NO
                           AND A.BRANCH_CODE = B.BRANCH_CODE
                           AND A.YEAR_CODE = B.YEAR_CODE
                        WHERE ISNULL(A.DO_NO, 0) = @V_NO
                          AND A.DO_TYPE = @V_TYPE
                          AND A.COMP_CODE = @COMP_CODE
                          AND A.BRANCH_CODE = @BRANCH_CODE", con, transaction))
                        {
                            cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                            cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            packingSlipQty = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                        }
                    }

                    decimal totalNetQty = Convert.ToDecimal(model.TOT_NET ?? 0);

                    if (packingSlipQty > 0 && totalNetQty != packingSlipQty)
                    {
                        return (false, $"Please check Total Net Qty ({totalNetQty}) not matched with " + $"Total Qty ({packingSlipQty}) of Packing Slip for same DO.");
                    }
                }

                //---------------------------------------------------
                // Check Total Net Qty matched with Issue Slip Qty
                //---------------------------------------------------
                int totalIssueNo = Convert.ToInt32(model.ISSUE_NO ?? 0);

                if (totalIssueNo != 0)
                {
                    decimal issueSlipQty;

                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT ISNULL(SUM(B.QTY), 0)
                    FROM ISSUE1 A
                    LEFT JOIN ISSUE2 B
                        ON A.V_TYPE = B.V_TYPE
                       AND A.V_NO = B.V_NO
                       AND A.BRANCH_CODE = B.BRANCH_CODE
                       AND A.YEAR_CODE = B.YEAR_CODE
                    WHERE ISNULL(A.PLAN_NO, 0) = @V_NO
                      AND A.PLAN_TYPE = @V_TYPE
                      AND A.COMP_CODE = @COMP_CODE
                      AND A.BRANCH_CODE = @BRANCH_CODE", con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                        issueSlipQty = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                    }

                    decimal totalNetQty = Convert.ToDecimal(model.TOT_NET ?? 0);

                    if (totalNetQty != issueSlipQty)
                    {
                        return (false, $"Please check Total Net Qty ({totalNetQty}) not matched with " + $"Total Qty ({issueSlipQty}) of Issue Slip for same DO.");
                    }
                }

                if (model.V_TYPE == "DOGT" && (Convert.ToInt32(globalVariable.PubCompCode) == 1 || Convert.ToInt32(globalVariable.PubCompCode) == 2 || Convert.ToInt32(globalVariable.PubCompCode) == 5))
                {
                    if (model.TPT_DISTANCE == 0)
                    {
                        return (false, "Please enter approx. Distance.");
                    }

                    if (model.TPT_MODE == null)
                    {
                        return (false, "Please select Mode of Transport.");
                    }
                }


            }


            //========== Validate Tax Type======================
            if (model.TAX_CODE == null || model.TAX_CODE == 0)
            {
                return (false, "Please select Tax Type.");
            }

            // TCS Validation
            if (model.V_TYPE == "DOGT" && model.V_DATE >= new DateTime(2020, 10, 1))
            {
                if (model.BILL_CODE > 0)
                {
                    string panQuery = @"
                    SELECT LTRIM(RTRIM(ISNULL(PAN, '')))
                    FROM SUBGROUP_MAST
                    WHERE COMP_CODE = @COMP_CODE
                      AND CODE = @BILL_CODE";

                    using (SqlCommand cmd = new SqlCommand(panQuery, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", Convert.ToInt32(globalVariable.PubCompCode));
                        cmd.Parameters.AddWithValue("@BILL_CODE", model.BILL_CODE);

                        string pan = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";

                        if (!string.IsNullOrWhiteSpace(pan))
                        {
                            string tcsQuery = @"
                            SELECT ISNULL(TCS_APPLY, '')
                            FROM SUBGROUP_MAST
                            WHERE COMP_CODE = @COMP_CODE
                            AND CODE = @BILL_CODE";

                            using (SqlCommand tcsCmd = new SqlCommand(tcsQuery, con, transaction))
                            {
                                tcsCmd.Parameters.AddWithValue("@COMP_CODE", Convert.ToInt32(globalVariable.PubCompCode));
                                tcsCmd.Parameters.AddWithValue("@BILL_CODE", model.BILL_CODE);

                                string tcsApply = Convert.ToString(await tcsCmd.ExecuteScalarAsync()) ?? "";

                                if (tcsApply == "Yes")
                                {
                                    if (model.TCS_PER == 0)
                                    {
                                        return (true, $"WARNING: Please Check, TCS applicable @ {generalSetting.pubBPTCSPer}% for {model.BILL_NAME}.");
                                    }
                                }
                                else
                                {
                                    if (model.TCS_PER > 0)
                                    {
                                        return (true, $"WARNING: Please Check, TCS not applicable for Party => {model.BILL_NAME}.");
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (model.TCS_PER < 1)
                            {
                                return (true, $"WARNING: Please Check, PAN No. not found in Party Master of {model.BILL_NAME}.{Environment.NewLine}So, TCS applicable @ 1%.");
                            }
                        }
                    }
                }
            }

            // GST State Validation
            if (model.BILL_CODE > 0)
            {
                string stateQuery = @"
                SELECT STATE_CODE
                FROM CITY_MAST
                WHERE CODE = @CITY_CODE";

                int stateCode = 0;

                using (SqlCommand cmd = new SqlCommand(stateQuery, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@CITY_CODE", model.BILL_CITY ?? 0);

                    object? result = await cmd.ExecuteScalarAsync();

                    if (result != null && result != DBNull.Value)
                    {
                        stateCode = Convert.ToInt32(result);
                    }
                }

                int compStateCode = Convert.ToInt32(globalVariable.PubCompStateCode);

                string stateType = compStateCode == stateCode ? "Local" : "Central/Other";

                if (compStateCode == stateCode && model.IGST_AMT > 0)
                {
                    return (false, $"IGST Not applicable as per Party State type is {stateType}");
                }
                else if (compStateCode != stateCode && (model.CGST_AMT + model.SGST_AMT) > 0)
                {
                    return (false, $"CGST/SGST not applicable as per Party State type is {stateType}");
                }

                if ((model.CGST_AMT + model.SGST_AMT) > 0 && model.IGST_AMT > 0)
                {
                    return (false, "Both GST Tax Rate is not Applicable in One Sales Invoice (CGST+SGST & IGST)");
                }

                if (model.CGST_AMT != model.SGST_AMT)
                {
                    return (false, "CGST & SGST Amount Should Be Same.");
                }
            }

            return (true, "");
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

        private async Task UpdateDefRemarkAsync(int vNo, bool defectiveGoods, SqlConnection con, SqlTransaction transaction)
        {
            try
            {
                // Same condition as legacy VB: Pack No must be greater than zero.
                if (vNo <= 0)
                    return;

                using (SqlCommand cmd = new SqlCommand(@"
                SELECT PACK_TYPE, PACK_NO
                FROM DO1
                WHERE COMP_CODE = 2
                  AND YEAR_CODE = 3
                  AND V_TYPE = 'DOGT'
                  AND V_NO = @V_NO
                  AND (@DEFECTIVE_GOODS = 0 OR DEFECTIVE_GOODS = 1)
                ORDER BY V_NO DESC", con, transaction))
                {
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@DEFECTIVE_GOODS", defectiveGoods ? 1 : 0);

                    var packingRecords = new List<(string PackType, int PackNo)>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string packType = Convert.ToString(reader["PACK_TYPE"]) ?? "";
                            int packNo = reader["PACK_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PACK_NO"]);
                            packingRecords.Add((packType, packNo));
                        }
                    }

                    foreach (var packing in packingRecords)
                    {
                        // Get the production details for this packing voucher.
                        var productionItems = new List<(int ItemCode, string BaleNo, decimal Qty)>();

                        using (SqlCommand cmd2 = new SqlCommand(@"
                        SELECT ITEM_CODE, BALE_NO, QTY
                        FROM PRODUCTION2
                        WHERE COMP_CODE = 2
                          AND V_TYPE = @PACK_TYPE
                          AND V_NO = @PACK_NO
                        ORDER BY SNO", con, transaction))
                        {
                            cmd2.Parameters.AddWithValue("@PACK_TYPE", packing.PackType);
                            cmd2.Parameters.AddWithValue("@PACK_NO", packing.PackNo);

                            using (SqlDataReader reader = await cmd2.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    productionItems.Add((
                                        Convert.ToInt32(reader["ITEM_CODE"]),
                                        Convert.ToString(reader["BALE_NO"])?.Trim() ?? "",
                                        Convert.ToDecimal(reader["QTY"])
                                    ));
                                }
                            }
                        }

                        if (productionItems.Count == 0)
                            continue;

                        string remark = defectiveGoods ? "BGrade" : "";

                        // Update the complete packing voucher in PRODUCTION2.
                        using (SqlCommand cmdUpdate = new SqlCommand(@"
                        UPDATE PRODUCTION2
                        SET REMARKS = @REMARK
                        WHERE COMP_CODE = 2
                          AND YEAR_CODE = 3
                          AND V_TYPE = @PACK_TYPE
                          AND V_NO = @PACK_NO
                          AND (
                              (@DEFECTIVE_GOODS = 1 AND ISNULL(REMARKS, '') = '')
                              OR
                              (@DEFECTIVE_GOODS = 0 AND REMARKS = 'BGrade')
                      )", con, transaction))
                        {
                            cmdUpdate.Parameters.AddWithValue("@REMARK", remark);
                            cmdUpdate.Parameters.AddWithValue("@PACK_TYPE", packing.PackType);
                            cmdUpdate.Parameters.AddWithValue("@PACK_NO", packing.PackNo);
                            cmdUpdate.Parameters.AddWithValue("@DEFECTIVE_GOODS", defectiveGoods ? 1 : 0);

                            await cmdUpdate.ExecuteNonQueryAsync();
                        }

                        foreach (var item in productionItems)
                        {
                            // Update matching PRODUCTION2 rows.
                            using (SqlCommand cmdUpdate = new SqlCommand(@"
                            UPDATE PRODUCTION2
                            SET REMARKS = @REMARK
                            WHERE COMP_CODE = 2
                              AND YEAR_CODE = 3
                              AND ITEM_CODE = @ITEM_CODE
                              AND LTRIM(RTRIM(BALE_NO)) = @BALE_NO
                              AND QTY = @QTY
                              AND (
                                  (@DEFECTIVE_GOODS = 1 AND ISNULL(REMARKS, '') = '')
                                  OR
                                  (@DEFECTIVE_GOODS = 0 AND REMARKS = 'BGrade')
                          )", con, transaction))
                            {
                                cmdUpdate.Parameters.AddWithValue("@REMARK", remark);
                                cmdUpdate.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                                cmdUpdate.Parameters.AddWithValue("@BALE_NO", item.BaleNo);
                                cmdUpdate.Parameters.AddWithValue("@QTY", item.Qty);
                                cmdUpdate.Parameters.AddWithValue("@DEFECTIVE_GOODS", defectiveGoods ? 1 : 0);

                                await cmdUpdate.ExecuteNonQueryAsync();
                            }

                            // Update matching PROD_ROLL rows.
                            using (SqlCommand cmdUpdate = new SqlCommand(@"
                            UPDATE PROD_ROLL
                            SET REMARKS = @REMARK
                            WHERE COMP_CODE = 2
                              AND YEAR_CODE = 3
                              AND ITEM_CODE = @ITEM_CODE
                              AND LTRIM(RTRIM(BALE_NO)) = @BALE_NO
                              AND QTY = @QTY
                              AND (
                                  (@DEFECTIVE_GOODS = 1 AND ISNULL(REMARKS, '') = '')
                                  OR
                                  (@DEFECTIVE_GOODS = 0 AND REMARKS = 'BGrade')
                          )", con, transaction))
                            {
                                cmdUpdate.Parameters.AddWithValue("@REMARK", remark);
                                cmdUpdate.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                                cmdUpdate.Parameters.AddWithValue("@BALE_NO", item.BaleNo);
                                cmdUpdate.Parameters.AddWithValue("@QTY", item.Qty);
                                cmdUpdate.Parameters.AddWithValue("@DEFECTIVE_GOODS", defectiveGoods ? 1 : 0);

                                await cmdUpdate.ExecuteNonQueryAsync();
                            }

                            // Update matching LAMINATION rows.
                            using (SqlCommand cmdUpdate = new SqlCommand(@"
                            UPDATE LAMINATION
                            SET REMARKS_A = @REMARK
                            WHERE COMP_CODE = 2
                                AND YEAR_CODE = 3
                                AND ISNULL(REMARKS_A, '') =
                                    CASE WHEN @DEFECTIVE_GOODS = 1 THEN '' ELSE 'BGrade' END
                                AND ITEM_CODE_A = @ITEM_CODE
                                AND LTRIM(RTRIM(ROLL_NO_A)) = @BALE_NO
                                AND NT_WT_A = @QTY", con, transaction))
                            {
                                cmdUpdate.Parameters.AddWithValue("@REMARK", remark);
                                cmdUpdate.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                                cmdUpdate.Parameters.AddWithValue("@BALE_NO", item.BaleNo);
                                cmdUpdate.Parameters.AddWithValue("@QTY", item.Qty);
                                cmdUpdate.Parameters.AddWithValue("@DEFECTIVE_GOODS", defectiveGoods ? 1 : 0);

                                await cmdUpdate.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        //==================Proforma Invoice Generation========================
        public async Task<object> SaveProformaDataAsync(DeliveryOrderModel model, string vNo)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            string sVtype = "DMEU";
            string sVno = Convert.ToString(vNo) ?? "";

            string sDocId = "";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                
                // =========================
                // 1. GENERATE DOC_ID
                // Old: PREFIX + scode
                // =========================
                using (SqlCommand cmd = new SqlCommand(@"
                SELECT PREFIX
                FROM DOC_NUMBER
                WHERE COMP_CODE = @COMP_CODE
                    AND V_TYPE = @V_TYPE
                    AND YEAR_CODE = @YEAR_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", sVtype);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                    string prefix = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";

                    sDocId = prefix + sVno;
                }

                // =========================
                // 2. CHECK EXISTING PROFORMA
                // Old: DO1 INSU_TYPE + INSU_NO
                // =========================
                using (SqlCommand cmd = new SqlCommand(@"
                SELECT CONCAT(ISNULL(INSU_TYPE, ''), ISNULL(CONVERT(VARCHAR(50), INSU_NO), ''))
                FROM DO1
                WHERE V_TYPE = @SOURCE_V_TYPE
                  AND V_NO = @SOURCE_V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@SOURCE_V_TYPE", (object?)model?.V_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SOURCE_V_NO", model.V_NO);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                    string refid = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";

                    if (!string.IsNullOrWhiteSpace(refid))
                    {
                        return new { success = false, message = $"Proforma invoice No. {refid} already generated." };
                    }
                }

                // =========================
                // 3. RECONCILIATION VALIDATION
                // Packing No. OR Issue No. required
                // =========================
                if (Convert.ToDecimal(model.PACK_NO ?? 0) <= 0 && Convert.ToDecimal(model.ISSUE_NO ?? 0) <= 0)
                {
                    return new { success = false, message = "Reconciliation Pending.Either Packing No. or Issue No. required." };
                }

                // =========================
                // 4. WB NO. REQUIRED
                // =========================
                if (Convert.ToDecimal(model.WB_NO ?? 0) <= 0)
                {
                    return new { success = false, message = "Reconciliation Pending.WB No. should not be blank." };
                }

                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // =========================
                        // SALE1 : PROFORMA HEADER
                        // =========================
                        using (SqlCommand cmd = new SqlCommand("sp_DeliveryOrder", con, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Action", "InsertProformaHeader");

                            cmd.Parameters.AddWithValue("@PROFORMA_DOC_ID", sDocId);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", sVtype);
                            cmd.Parameters.AddWithValue("@PROFORMA_V_NO", sVno);

                            cmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@GODOWN_CODE", (object?)model.GODOWN_CODE ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@BILL_CODE", (object?)model.BILL_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_NAME", (object?)model.BILL_NAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_ADD1", (object?)model.BILL_ADD1 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_ADD2", (object?)model.BILL_ADD2 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_ADD3", (object?)model.BILL_ADD3 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_CITY", (object?)model.BILL_CITY ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@SHIP_CODE", (object?)model.SHIP_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_NAME", (object?)model.SHIP_NAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_ADD1", (object?)model.SHIP_ADD1 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_ADD2", (object?)model.SHIP_ADD2 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_ADD3", (object?)model.SHIP_ADD3 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_CITY", (object?)model.SHIP_CITY ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@IMPORT_CURRENCY", DBNull.Value);
                            cmd.Parameters.AddWithValue("@EXRATE", (object?)model.EXRATE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TAX_CODE", (object?)model.TAX_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PACK_NO", (object?)model.PACK_NO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PACK_TYPE", (object?)model.PACK_TYPE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ITEM_TYPE", (object?)model.ITEM_TYPE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@WB_NO", (object?)model.WB_NO ?? DBNull.Value);

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
                            cmd.Parameters.AddWithValue("@TOT_NET", (object?)model.TOT_NET ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@FRT_AMT", (object?)model.FRT_AMT ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ROUND_OFF", (object?)model.ROUND_OFF ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@NAMOUNT", (object?)model.NAMOUNT ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@INSU_PER", (object?)model.INSU_PER ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@INSU_AMT", (object?)model.INSU_AMT ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TDS_PER", (object?)model.TDS_PER ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TDS_AMT", (object?)model.TDS_AMT ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@WB_REM", (object?)model.WB_REM ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TOT_GROSS", (object?)model.TOT_GROSS ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@GR_NO", (object?)model.GR_NO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@GR_DATE", (object?)model.GR_DATE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@VEHICLE_NO", (object?)model.VEHICLE_NO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TRANSPORT_CODE", (object?)model.TRANSPORT_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TRANSPORT_NAME", (object?)model.TRANSPORT_NAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DRIVER_NAME", (object?)model.DRIVER_NAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DRIVER_NO", (object?)model.DRIVER_NO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@WAYBILL_NO", (object?)model.WAYBILL_NO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@FRT_TOPAY", (object?)model.FRT_TOPAY ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@REMARK", (object?)model.REMARK ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@WB_QTY", (object?)model.WB_QTY ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@DISC_PER", (object?)model.DISC_PER ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DISC_AMT", (object?)model.DISC_AMT ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@AGENT_CODE", (object?)model.AGENT_CODE ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@FINAL_DEST", (object?)model.FINAL_DEST ?? "");
                            cmd.Parameters.AddWithValue("@FINAL_DEST_COUNTRY", (object?)model.FINAL_DEST_COUNTRY ?? "");
                            cmd.Parameters.AddWithValue("@DELIVERY_TERMS", (object?)model.DELIVERY_TERMS ?? "");
                            cmd.Parameters.AddWithValue("@LUT_DETAIL", (object?)model.LUT_DETAIL ?? "");
                            cmd.Parameters.AddWithValue("@INSU_DETAIL", (object?)model.INSU_DETAIL ?? "");
                            cmd.Parameters.AddWithValue("@SAUDA_TYPE", (object?)model.SAUDA_TYPE ?? "");
                            cmd.Parameters.AddWithValue("@SAUDA_NO", (object?)model.SAUDA_NO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SAUDA_RATE", (object?)model.SAUDA_RATE ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@TRADE_TERM", "");
                            cmd.Parameters.AddWithValue("@INCOTERM", "");
                            cmd.Parameters.AddWithValue("@SHIPMENT_TYPE", "");
                            cmd.Parameters.AddWithValue("@MODEOF_PAYMENT", "");

                            cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        using (SqlCommand cmd = new SqlCommand(@"
                        DELETE FROM SALE2
                        WHERE V_TYPE = @V_TYPE
                          AND V_NO = @V_NO
                          AND COMP_CODE = @COMP_CODE
                          AND YEAR_CODE = @YEAR_CODE
                          AND BRANCH_CODE = @BRANCH_CODE", con, transaction))
                        {
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", sVtype);
                            cmd.Parameters.AddWithValue("@V_NO", sVno);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // =========================
                        // SALE2 : PROFORMA FOOTER
                        // =========================
                        int sno = 0;

                        foreach (var item in model.Footer)
                        {
                            sno++;

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
                                        item.ITEM_NAME = !string.IsNullOrWhiteSpace(printName) ? printName : Convert.ToString(reader["NAME"]);
                                        item.UNIT_NAME = reader["UNIT_NAME"] == DBNull.Value ? null : Convert.ToString(reader["UNIT_NAME"]);
                                        item.UNIT_CODE = reader["UNIT_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["UNIT_CODE"]);
                                        item.HSN_CODE = reader["HSN_CODE"] == DBNull.Value ? null : Convert.ToString(reader["HSN_CODE"]);
                                    }
                                }
                            }

                            using (SqlCommand detailCmd = new SqlCommand("sp_DeliveryOrder", con, transaction))
                            {
                                detailCmd.CommandType = CommandType.StoredProcedure;

                                detailCmd.Parameters.AddWithValue("@Action", "InsertProformaFooter");

                                detailCmd.Parameters.AddWithValue("@PROFORMA_DOC_ID", sDocId);
                                detailCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                detailCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                detailCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                detailCmd.Parameters.AddWithValue("@V_TYPE", sVtype);
                                detailCmd.Parameters.AddWithValue("@PROFORMA_V_NO", sVno);

                                detailCmd.Parameters.AddWithValue("@PROFORMA_ITEM_CODE", (object?)item.ITEM_CODE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@ITEM_NAME", (object?)item.ITEM_NAME ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@UNIT_NAME", (object?)item.UNIT_NAME ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@UNIT_CODE", (object?)item.UNIT_CODE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@HSN_CODE", (object?)item.HSN_CODE ?? DBNull.Value);

                                detailCmd.Parameters.AddWithValue("@PROFORMA_NOS", (object?)item.NOS ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@PROFORMA_GROSS_QTY", (object?)item.GROSS_QTY ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@PROFORMA_QTY", (object?)item.QTY ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@FOR_RATE", (object?)item.FOR_RATE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@PROFORMA_RATE", (object?)item.RATE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@PROFORMA_AMOUNT", (object?)item.AMOUNT ?? DBNull.Value);

                                detailCmd.Parameters.AddWithValue("@PACK_PER", (object?)item.PACK_PER ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@PACK_AMT", (object?)item.PACK_AMT ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@DISC_PER", (object?)item.DISC_PER ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@DISC_AMT", (object?)item.DISC_AMT ?? DBNull.Value);

                                detailCmd.Parameters.AddWithValue("@CGST_PER", (object?)item.CGST_PER ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@CGST_AMT", (object?)item.CGST_AMT ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@SGST_PER", (object?)item.SGST_PER ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@SGST_AMT", (object?)item.SGST_AMT ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@IGST_PER", (object?)item.IGST_PER ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@IGST_AMT", (object?)item.IGST_AMT ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@CESS_PER", (object?)item.CESS_PER ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@CESS_AMT", (object?)item.CESS_AMT ?? DBNull.Value);

                                detailCmd.Parameters.AddWithValue("@PROFORMA_REMARK", (object?)item.REMARK ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@STATUS", (object?)model.STATUS ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@PACK_TYPE", (object?)item.PACK_TYPE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@PACK_NO", (object?)item.PACK_NO ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@PROFORMA_LOT_NO", (object?)item.LOT_No ?? DBNull.Value);

                                detailCmd.Parameters.AddWithValue("@SAUDA_TYPE", (object?)item.SAUDA_TYPE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@SAUDA_NO", (object?)item.SAUDA_NO ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@SAUDA_RATE", (object?)item.SAUDA_RATE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@ORD_TYPE", (object?)item.ORD_TYPE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@ORD_NO", (object?)item.ORD_NO ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@ORD_RATE", (object?)item.ORD_RATE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@DCN_TYPE", (object?)item.DCN_TYPE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@DCN_NO", (object?)item.DCN_NO ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@TAX_CODE", (object?)item.TAX_CODE ?? DBNull.Value);

                                detailCmd.Parameters.AddWithValue("@FRT_AMT", (object?)item.FRT_AMT ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@INSU_AMT", (object?)item.INSU_AMT ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@CDISC_AMT", (object?)item.CDISC_AMT ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@WB_QTY", (object?)item.WB_QTY ?? DBNull.Value);

                                detailCmd.Parameters.AddWithValue("@PROFORMA_SNO", sno);
                                detailCmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                                detailCmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                                detailCmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                                detailCmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                await detailCmd.ExecuteNonQueryAsync();
                            }
                        }

                        using (SqlCommand cmd = new SqlCommand(@"
                        UPDATE DO1
                        SET INSU_TYPE = @INSU_TYPE,
                            INSU_NO = @INSU_NO
                        WHERE V_TYPE = @SOURCE_V_TYPE
                          AND V_NO = @SOURCE_V_NO
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE", con, transaction))
                        {
                            cmd.Parameters.AddWithValue("@INSU_TYPE", sVtype);
                            cmd.Parameters.AddWithValue("@INSU_NO", sVno);
                            cmd.Parameters.AddWithValue("@SOURCE_V_TYPE", model.V_TYPE);
                            cmd.Parameters.AddWithValue("@SOURCE_V_NO", model.V_NO);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();

                        return new { success = true, message = "Proforma invoice saved successfully.", docId = sDocId };
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        return new { success = false, message = ex.Message };
                    }
                }
            }
        }

        //=============================Sales Invoice Generation=========================

        public async Task<object> SaveSaleInvoiceDataAsync(DeliveryOrderModel model, string vNo)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            string sVtype = "SAGT";
            string sVno = "";
            string sDocId = "";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                // =========================
                // 1. ADVANCE PAYMENT VALIDATION
                // =========================
                var firstItem = model.Footer.FirstOrDefault();

                if (firstItem != null)
                {
                    object advanceResult;

                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT ISNULL(b.DAY_INT, 0)
                    FROM PAYTERM_MAST b
                    WHERE b.COMP_CODE = @COMP_CODE
                      AND b.CODE = (
                          SELECT ISNULL(PAYTERM_CODE, 0)
                          FROM SAUDA
                          WHERE COMP_CODE = @COMP_CODE
                            AND V_TYPE = @SAUDA_TYPE
                            AND V_NO = @SAUDA_NO
                      )", con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@SAUDA_TYPE", (object?)firstItem.SAUDA_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SAUDA_NO", (object?)firstItem.SAUDA_NO ?? DBNull.Value);

                        advanceResult = await cmd.ExecuteScalarAsync();
                    }

                    decimal advanceFlg = Convert.ToDecimal(advanceResult ?? 0);

                    if (advanceFlg <= 1)
                    {
                        decimal dramt;
                        decimal cramt;

                        using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(AMT), 0)
                        FROM LEDGER2
                        WHERE DR_CODE = @PARTY_CODE
                          AND COMP_CODE = @COMP_CODE
                          AND DR_CODE > 0
                          AND BRANCH_CODE = @BRANCH_CODE", con))
                        {
                            cmd.Parameters.AddWithValue("@PARTY_CODE", (object?)model.BILL_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            dramt = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
                        }

                        using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(AMT), 0)
                        FROM LEDGER2
                        WHERE CR_CODE = @PARTY_CODE
                          AND COMP_CODE = @COMP_CODE
                          AND CR_CODE > 0
                          AND BRANCH_CODE = @BRANCH_CODE", con))
                        {
                            cmd.Parameters.AddWithValue("@PARTY_CODE", (object?)model.BILL_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            cramt = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
                        }

                        decimal bal = dramt - cramt;

                        if (bal > 0)
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
                            SELECT V_NO
                            FROM SALE1
                            WHERE V_TYPE = 'DMEU'
                              AND REF_TYPE = @REF_TYPE
                              AND REF_NO = @REF_NO
                              AND COMP_CODE = @COMP_CODE
                              AND BRANCH_CODE = @BRANCH_CODE", con))
                            {
                                cmd.Parameters.AddWithValue("@REF_TYPE", model.V_TYPE);
                                cmd.Parameters.AddWithValue("@REF_NO", model.V_NO);
                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                                var piNo = await cmd.ExecuteScalarAsync();

                                if (piNo == null || piNo == DBNull.Value)
                                {
                                    return new
                                    {
                                        success = false,
                                        message = "First generate Proforma Invoice, As Party Payment term='Advance' and have Debit Ledger balance."
                                    };
                                }
                            }
                        }
                    }
                }

                // =========================
                // 3. GENERATE DOC_ID
                // =========================
                using (SqlCommand cmd = new SqlCommand(@"
                SELECT PREFIX
                FROM DOC_NUMBER
                WHERE COMP_CODE = @COMP_CODE
                  AND V_TYPE = @V_TYPE
                  AND YEAR_CODE = @YEAR_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", sVtype);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                    string prefix = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
                    sDocId = prefix + sVno;
                }

                // =========================
                // 4. CHECK EXISTING INVOICE
                // Old: DO1 REF_TYPE + REF_NO
                // =========================
                using (SqlCommand cmd = new SqlCommand(@"
                SELECT CONCAT(ISNULL(REF_TYPE, ''), ISNULL(CONVERT(VARCHAR(50), REF_NO), ''))
                FROM DO1
                WHERE V_TYPE = @SOURCE_V_TYPE
                  AND V_NO = @SOURCE_V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@SOURCE_V_TYPE", model.V_TYPE);
                    cmd.Parameters.AddWithValue("@SOURCE_V_NO", model.V_NO);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                    string refid = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";

                    if (!string.IsNullOrWhiteSpace(refid))
                    {
                        return new
                        {
                            success = false,
                            message = $"Invoice No. {refid} already generated."
                        };
                    }
                }

                // =========================
                // 5. RECONCILIATION VALIDATION
                // =========================
                if (Convert.ToDecimal(model.PACK_NO ?? 0) <= 0 &&
                    Convert.ToDecimal(model.ISSUE_NO ?? 0) <= 0)
                {
                    return new
                    {
                        success = false,
                        message = "Reconciliation Pending. Either Packing No. or Issue No. required."
                    };
                }

                if (Convert.ToDecimal(model.WB_NO ?? 0) <= 0)
                {
                    return new
                    {
                        success = false,
                        message = "Reconciliation Pending. WB No. should not be blank."
                    };
                }

                // =========================
                // 6. APPROVAL VALIDATION
                // =========================
                string fappstatus = "";
                string fappRemark = "";
                int fappUserCode = 0;

                bool isFinalApprovalBody = false;

                using (SqlCommand cmd = new SqlCommand(@"
                SELECT APPROV_USER
                FROM DOC_APPROSTAGE
                WHERE USER_CODE = @USER_CODE
                  AND DOC_CODE = @DOC_CODE
                  AND COMP_CODE = @COMP_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@USER_CODE", globalVariable.PubUserId);
                    cmd.Parameters.AddWithValue("@DOC_CODE", sVtype);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                    string approvalUser = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";

                    if (approvalUser == "FINAL")
                    {
                        isFinalApprovalBody = true;
                        fappstatus = "Approved";
                        fappRemark = "Document Approved.";
                        fappUserCode = Convert.ToInt32(globalVariable.PubUserId);
                    }
                }

                string approvalFlg = "";

                using (SqlCommand cmd = new SqlCommand(@"
                SELECT ISNULL(FAPROV_STATUS, '')
                FROM DO1
                WHERE V_TYPE = @V_TYPE
                  AND V_NO = @V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                    cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                    approvalFlg = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
                }

                if (approvalFlg != "Approved")
                {
                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT ISNULL(b.CREDIT_TYPE, '')
                    FROM SUBGROUP_MAST a
                    LEFT JOIN PAYTERM_MAST b
                        ON a.PAYTERM_CODE = b.CODE
                       AND a.COMP_CODE = b.COMP_CODE
                    WHERE a.CODE = @PARTY_CODE
                      AND a.COMP_CODE = @COMP_CODE", con))
                    {
                        cmd.Parameters.AddWithValue("@PARTY_CODE", (object?)model.BILL_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                        string creditType = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";

                        if (creditType == "Against LC" &&string.IsNullOrWhiteSpace(model.LC_NO))
                        {
                            fappstatus = "";
                            fappRemark = "";
                        }
                    }
                }

                // =========================
                // 7. WB QTY APPROVAL CHECK
                // =========================
                if (globalVariable.PubCompCode == "1" &&Convert.ToDecimal(model.PACK_NO ?? 0) <= 0)
                {
                    foreach (var item in model.Footer)
                    {
                        if (Convert.ToDecimal(item.QTY ?? 0) != Convert.ToDecimal(item.WB_QTY ?? 0))
                        {
                            fappstatus = "";
                            fappRemark = "";
                        }
                    }
                }

                // =========================
                // 8. CITY / STATE / COUNTRY
                // =========================
                object billState = DBNull.Value;
                object billCountry = DBNull.Value;
                string billStateName = "";
                string billCountryName = "";

                object shipState = DBNull.Value;
                object shipCountry = DBNull.Value;
                string shipStateName = "";
                string shipCountryName = "";

                using (SqlCommand cmd = new SqlCommand(@"
                SELECT c.STATE_CODE, s.NAME AS STATE_NAME,
                       c.COUNTRY_CODE, co.NAME AS COUNTRY_NAME
                FROM CITY_MAST c
                LEFT JOIN STATE_MAST s ON s.CODE = c.STATE_CODE
                LEFT JOIN COUNTRY_MAST co ON co.CODE = c.COUNTRY_CODE
                WHERE c.CODE = @CITY_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@CITY_CODE", (object?)model.BILL_CITY ?? DBNull.Value);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            billState = reader["STATE_CODE"];
                            billCountry = reader["COUNTRY_CODE"];
                            billStateName = Convert.ToString(reader["STATE_NAME"]) ?? "";
                            billCountryName = Convert.ToString(reader["COUNTRY_NAME"]) ?? "";
                        }
                    }
                }

                using (SqlCommand cmd = new SqlCommand(@"
                SELECT c.STATE_CODE, s.NAME AS STATE_NAME,
                       c.COUNTRY_CODE, co.NAME AS COUNTRY_NAME
                FROM CITY_MAST c
                LEFT JOIN STATE_MAST s ON s.CODE = c.STATE_CODE
                LEFT JOIN COUNTRY_MAST co ON co.CODE = c.COUNTRY_CODE
                WHERE c.CODE = @CITY_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@CITY_CODE", (object?)model.SHIP_CITY ?? DBNull.Value);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            shipState = reader["STATE_CODE"];
                            shipCountry = reader["COUNTRY_CODE"];
                            shipStateName = Convert.ToString(reader["STATE_NAME"]) ?? "";
                            shipCountryName = Convert.ToString(reader["COUNTRY_NAME"]) ?? "";
                        }
                    }
                }

                // =========================
                // 9. TRANSACTION START
                // =========================
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // SALE2 DELETE
                        using (SqlCommand cmd = new SqlCommand(@"
                        DELETE FROM SALE2
                        WHERE V_TYPE = @V_TYPE
                          AND V_NO = @V_NO
                          AND COMP_CODE = @COMP_CODE
                          AND YEAR_CODE = @YEAR_CODE
                          AND BRANCH_CODE = @BRANCH_CODE", con, transaction))
                        {
                            cmd.Parameters.AddWithValue("@V_TYPE", sVtype);
                            cmd.Parameters.AddWithValue("@V_NO", sVno);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // =========================
                        // SALE1 : SALE INVOICE HEADER
                        // =========================
                        using (SqlCommand cmd = new SqlCommand("sp_DeliveryOrder", con, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Action", "InsertSaleInvoiceHeader");

                            cmd.Parameters.AddWithValue("@DOC_ID", sDocId);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", sVtype);
                            cmd.Parameters.AddWithValue("@V_NO", sVno);
                            cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);

                            cmd.Parameters.AddWithValue("@GODOWN_CODE", (object?)model.GODOWN_CODE ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@BILL_CODE", (object?)model.BILL_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_NAME", (object?)model.BILL_NAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_ADD1", (object?)model.BILL_ADD1 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_ADD2", (object?)model.BILL_ADD2 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_ADD3", (object?)model.BILL_ADD3 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_CITY", (object?)model.BILL_CITY ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_CITYNAME", (object?)model.BILL_CITYNAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_STATE", billState);
                            cmd.Parameters.AddWithValue("@BILL_STATENAME", billStateName);
                            cmd.Parameters.AddWithValue("@BILL_COUNTRY", billCountry);
                            cmd.Parameters.AddWithValue("@BILL_COUNTRYNAME", billCountryName);
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
                            cmd.Parameters.AddWithValue("@SHIP_STATE", shipState);
                            cmd.Parameters.AddWithValue("@SHIP_STATENAME", shipStateName);
                            cmd.Parameters.AddWithValue("@SHIP_COUNTRY", shipCountry);
                            cmd.Parameters.AddWithValue("@SHIP_COUNTRYNAME", shipCountryName);
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
                            cmd.Parameters.AddWithValue("@PAYMENT_TERM", (object?)model.PAYMENT_TERM ?? DBNull.Value);

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

                            cmd.Parameters.AddWithValue("@FAPROV_STATUS", fappstatus);
                            cmd.Parameters.AddWithValue("@FAPROV_REMARKS", fappRemark);
                            cmd.Parameters.AddWithValue("@APPROVAL_USER", fappUserCode);

                            cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // =========================
                        // SALE2 : SALE INVOICE FOOTER
                        // =========================
                        int sno = 0;

                        foreach (var item in model.Footer)
                        {
                            sno++;

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

                                        item.ITEM_NAME = !string.IsNullOrWhiteSpace(printName) ? printName : Convert.ToString(reader["NAME"]);
                                        item.UNIT_NAME = reader["UNIT_NAME"] == DBNull.Value ? null : Convert.ToString(reader["UNIT_NAME"]);
                                        item.UNIT_CODE = reader["UNIT_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["UNIT_CODE"]);
                                        item.HSN_CODE = reader["HSN_CODE"] == DBNull.Value ? null : Convert.ToString(reader["HSN_CODE"]);
                                    }
                                }
                            }

                            using (SqlCommand detailCmd = new SqlCommand("sp_DeliveryOrder", con, transaction))
                            {
                                detailCmd.CommandType = CommandType.StoredProcedure;

                                detailCmd.Parameters.AddWithValue("@Action", "InsertSaleInvoiceFooter");

                                detailCmd.Parameters.AddWithValue("@DOC_ID", sDocId);
                                detailCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                detailCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                detailCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                detailCmd.Parameters.AddWithValue("@V_TYPE", sVtype);
                                detailCmd.Parameters.AddWithValue("@V_NO", sVno);
                                detailCmd.Parameters.AddWithValue("@V_DATE", DateTime.Today);

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

                                detailCmd.Parameters.AddWithValue("@CGST_PER", (object?)item.CGST_PER ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@CGST_AMT", (object?)item.CGST_AMT ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@SGST_PER", (object?)item.SGST_PER ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@SGST_AMT", (object?)item.SGST_AMT ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@IGST_PER", (object?)item.IGST_PER ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@IGST_AMT", (object?)item.IGST_AMT ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@CESS_PER", (object?)item.CESS_PER ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@CESS_AMT", (object?)item.CESS_AMT ?? DBNull.Value);

                                detailCmd.Parameters.AddWithValue("@REMARK", (object?)item.REMARK ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@STATUS", (object?)model.STATUS ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@PACK_TYPE", (object?)model.PACK_TYPE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@PACK_NO", (object?)item.PACK_NO ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@LOT_NO", (object?)item.LOT_No ?? DBNull.Value);

                                detailCmd.Parameters.AddWithValue("@SAUDA_TYPE", (object?)item.SAUDA_TYPE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@SAUDA_NO", (object?)item.SAUDA_NO ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@SAUDA_RATE", (object?)item.SAUDA_RATE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@ORD_TYPE", (object?)item.ORD_TYPE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@ORD_NO", (object?)item.ORD_NO ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@ORD_RATE", (object?)item.RATE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@DCN_TYPE", (object?)item.DCN_TYPE ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@DCN_NO", (object?)item.DCN_NO ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@TAX_CODE", (object?)item.TAX_CODE ?? DBNull.Value);

                                detailCmd.Parameters.AddWithValue("@FRT_AMT", (object?)item.FRT_AMT ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@INSU_AMT", (object?)item.INSU_AMT ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@CDISC_AMT", (object?)item.CDISC_AMT ?? DBNull.Value);
                                detailCmd.Parameters.AddWithValue("@WBQTY", (object?)item.WB_QTY ?? DBNull.Value);

                                detailCmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                                detailCmd.Parameters.AddWithValue("@AED", "A");
                                detailCmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                                detailCmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                                detailCmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                detailCmd.Parameters.AddWithValue("@SNO", sno);

                                await detailCmd.ExecuteNonQueryAsync();
                            }
                        }

                        // =========================
                        // 10. UPDATE DO1 REFERENCE
                        // =========================
                        await transaction.CommitAsync();

                        using (SqlCommand cmd = new SqlCommand(@"
                        UPDATE DO1
                        SET REF_TYPE = @REF_TYPE,
                            REF_NO = @REF_NO
                        WHERE V_TYPE = @SOURCE_V_TYPE
                          AND V_NO = @SOURCE_V_NO
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE", con))
                        {
                            cmd.Parameters.AddWithValue("@REF_TYPE", sVtype);
                            cmd.Parameters.AddWithValue("@REF_NO", sVno);
                            cmd.Parameters.AddWithValue("@SOURCE_V_TYPE", model.V_TYPE);
                            cmd.Parameters.AddWithValue("@SOURCE_V_NO", model.V_NO);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // =========================
                        // UPDATE DEFECTIVE GOODS REMARKS
                        // =========================
                        using (SqlTransaction remarkTransaction = con.BeginTransaction())
                        {
                            try
                            {
                                await UpdateDefRemarkAsync( con,  remarkTransaction, model, globalVariable );

                                await remarkTransaction.CommitAsync();
                            }
                            catch
                            {
                                await remarkTransaction.RollbackAsync();
                                throw;
                            }
                        }

                        return new { success = true, message = $"Sales Invoice No. {sVtype}{sVno} generated.", docId = sDocId, vNo = sVno };
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        return new { success = false, message = ex.Message };
                    }
                }
            }
        }

        //============================UPDATE DO AFTER RECONCILE =================================
        public async Task<object> UpdateDOAsync(DeliveryOrderModel model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            string docId = model.V_TYPE + model.V_NO;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // =========================================
                        // Check Existing DO Approval Status
                        // =========================================

                        string currentApprovalStatus = "";

                        using (SqlCommand checkCmd = new SqlCommand(@"
                        SELECT FAPROV_STATUS
                        FROM DO1
                        WHERE V_TYPE = @V_TYPE
                          AND V_NO = @V_NO
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE
                          AND YEAR_CODE = @YEAR_CODE", con, transaction))
                        {
                            checkCmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                            checkCmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                            checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            checkCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            checkCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                            object? result = await checkCmd.ExecuteScalarAsync();

                            if (result == null)
                            {
                                await transaction.RollbackAsync();
                                return new { success = false, message = "Delivery Order not found." };
                            }

                            currentApprovalStatus = Convert.ToString(result) ?? "";
                        }

                        string approvalStatus = "";
                        string approvalRemark = "";
                        string approvalUser = "";

                        if (currentApprovalStatus == "Approved")
                        {
                            approvalStatus = "Approved";
                            approvalRemark = "Document Approved.";
                            approvalUser = globalVariable.PubUserId;
                        }

                        // =========================================
                        // Update DO1 Header + Delete DO2 Footer
                        // =========================================

                        using (SqlCommand cmd = new SqlCommand("sp_DeliveryOrder", con, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Action", "UpdateDO");

                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                            cmd.Parameters.AddWithValue("@V_NO", model.V_NO);

                            cmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DEL_DATE", (object?)model.DEL_DATE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@GODOWN_CODE", (object?)model.GODOWN_CODE ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@BILL_CODE", (object?)model.BILL_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_NAME", (object?)model.BILL_NAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_ADD1", (object?)model.BILL_ADD1 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_ADD2", (object?)model.BILL_ADD2 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_ADD3", (object?)model.BILL_ADD3 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_CITY", (object?)model.BILL_CITY ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_GST", (object?)model.BILL_GST ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_PINCODE", (object?)model.BILL_PINCODE ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@SHIP_CODE", (object?)model.SHIP_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_NAME", (object?)model.SHIP_NAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_ADD1", (object?)model.SHIP_ADD1 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_ADD2", (object?)model.SHIP_ADD2 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_ADD3", (object?)model.SHIP_ADD3 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_CITY", (object?)model.SHIP_CITY ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_GST", (object?)model.SHIP_GST ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_PINCODE", (object?)model.SHIP_PINCODE ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@TAX_CODE", (object?)model.TAX_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PACK_NO", (object?)model.PACK_NO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PACK_TYPE", (object?)model.PACK_TYPE ?? DBNull.Value);
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
                            cmd.Parameters.AddWithValue("@PAYMENT_TERM", (object?)model.PAYMENT_TERM ?? DBNull.Value);
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

                            cmd.Parameters.AddWithValue("@FAPROV_STATUS", approvalStatus);
                            cmd.Parameters.AddWithValue("@FAPROV_REMARKS", approvalRemark);
                            cmd.Parameters.AddWithValue("@APPROVAL_USER", approvalUser);

                            cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // =========================================
                        // Insert DO2 Footer Rows
                        // =========================================

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

                                        item.ITEM_NAME = !string.IsNullOrWhiteSpace(printName) ? printName : Convert.ToString(reader["NAME"]);
                                        item.UNIT_NAME = reader["UNIT_NAME"] == DBNull.Value ? null : Convert.ToString(reader["UNIT_NAME"]);
                                        item.UNIT_CODE = reader["UNIT_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["UNIT_CODE"]);
                                        item.HSN_CODE = reader["HSN_CODE"] == DBNull.Value ? null : Convert.ToString(reader["HSN_CODE"]);
                                    }
                                }
                            }

                            using (SqlCommand detailCmd = new SqlCommand("sp_DeliveryOrder", con, transaction))
                            {
                                detailCmd.CommandType = CommandType.StoredProcedure;

                                detailCmd.Parameters.AddWithValue("@Action", "InsertFooterForDo");

                                detailCmd.Parameters.AddWithValue("@DOC_ID", docId);
                                detailCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                detailCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                detailCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                detailCmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                                detailCmd.Parameters.AddWithValue("@V_NO", model.V_NO);
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
                                detailCmd.Parameters.AddWithValue("@LOT_NO", (object?)item.LOT_No ?? DBNull.Value);

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
                                detailCmd.Parameters.AddWithValue("@SNO", model.Footer.IndexOf(item) + 1);

                                await detailCmd.ExecuteNonQueryAsync();
                            }
                        }

                        await transaction.CommitAsync();

                        return new { success = true, message = "DO Reconcile and updated Successfully. Please check DO carefully, before generate Sale invoice." };
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        return new { success = false, message = ex.Message };
                    }
                }
            }
        }

        // =========================================
        // Update Defective Goods Remarks
        // =========================================

        private async Task UpdateDefRemarkAsync(SqlConnection con, SqlTransaction transaction, DeliveryOrderModel model, dynamic globalVariable)
        {
            int packNo = Convert.ToInt32(model.PACK_NO);
            int defectiveGoods = Convert.ToInt32(model.DEFECTIVE_GOODS);

            if (packNo <= 0)
                return;

            // Get Packing Voucher Details from DO1
            using (SqlCommand cmd = new SqlCommand(@"
            SELECT PACK_TYPE, PACK_NO
            FROM DO1
            WHERE COMP_CODE = @COMP_CODE
              AND YEAR_CODE = @YEAR_CODE
              AND V_TYPE = @V_TYPE
              AND V_NO = @V_NO
            ORDER BY V_NO DESC", con, transaction))
            {
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                cmd.Parameters.AddWithValue("@V_NO", model.V_NO);

                var packingVouchers = new List<(string PackType, int PackNo)>();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        packingVouchers.Add((
                            Convert.ToString(reader["PACK_TYPE"]) ?? "",
                            Convert.ToInt32(reader["PACK_NO"])
                        ));
                    }
                }

                foreach (var voucher in packingVouchers)
                {
                    // Get Production Items
                    var productionItems = new List<(object ItemCode, string BaleNo, object Qty)>();

                    using (SqlCommand productionCmd = new SqlCommand(@"
                    SELECT ITEM_CODE, BALE_NO, QTY
                    FROM PRODUCTION2
                    WHERE COMP_CODE = @COMP_CODE
                      AND V_TYPE = @PACK_TYPE
                      AND V_NO = @PACK_NO
                    ORDER BY SNO", con, transaction))
                    {
                        productionCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        productionCmd.Parameters.AddWithValue("@PACK_TYPE", voucher.PackType);
                        productionCmd.Parameters.AddWithValue("@PACK_NO", voucher.PackNo);

                        using (SqlDataReader reader = await productionCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                productionItems.Add((reader["ITEM_CODE"], Convert.ToString(reader["BALE_NO"])?.Trim() ?? "", reader["QTY"]));
                            }
                        }
                    }

                    if (productionItems.Count == 0)
                        continue;

                    // Update Packing Voucher Remarks
                    string remarkCondition = defectiveGoods == 1 ? "ISNULL(REMARKS, '') = ''" : "REMARKS = 'BGrade'";

                    string remarkValue = defectiveGoods == 1 ? "BGrade" : "";

                    using (SqlCommand updateCmd = new SqlCommand($@"
                    UPDATE PRODUCTION2
                    SET REMARKS = @REMARK
                    WHERE COMP_CODE = @COMP_CODE
                      AND YEAR_CODE = @YEAR_CODE
                      AND V_TYPE = @PACK_TYPE
                      AND V_NO = @PACK_NO
                      AND {remarkCondition}", con, transaction))
                    {
                        updateCmd.Parameters.AddWithValue("@REMARK", remarkValue);
                        updateCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        updateCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        updateCmd.Parameters.AddWithValue("@PACK_TYPE", voucher.PackType);
                        updateCmd.Parameters.AddWithValue("@PACK_NO", voucher.PackNo);

                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    // Update Production2, Prod_Roll and Lamination Item Remarks
                    foreach (var item in productionItems)
                    {
                        using (SqlCommand updateCmd = new SqlCommand($@"
                        UPDATE PRODUCTION2
                        SET REMARKS = @REMARK
                        WHERE COMP_CODE = @COMP_CODE
                          AND YEAR_CODE = @YEAR_CODE
                          AND ISNULL(REMARKS, '') {(defectiveGoods == 1 ? "= ''" : "= 'BGrade'")}
                          AND ITEM_CODE = @ITEM_CODE
                          AND LTRIM(RTRIM(BALE_NO)) = @BALE_NO
                          AND QTY = @QTY;

                        UPDATE PROD_ROLL
                        SET REMARKS = @REMARK
                        WHERE COMP_CODE = @COMP_CODE
                          AND YEAR_CODE = @YEAR_CODE
                          AND ISNULL(REMARKS, '') {(defectiveGoods == 1 ? "= ''" : "= 'BGrade'")}
                          AND ITEM_CODE = @ITEM_CODE
                          AND LTRIM(RTRIM(BALE_NO)) = @BALE_NO
                          AND QTY = @QTY;

                        UPDATE LAMINATION
                        SET REMARKS_A = @REMARK
                        WHERE COMP_CODE = @COMP_CODE
                          AND YEAR_CODE = @YEAR_CODE
                          AND ISNULL(REMARKS_A, '') {(defectiveGoods == 1 ? "= ''" : "= 'BGrade'")}
                          AND ITEM_CODE_A = @ITEM_CODE
                          AND LTRIM(RTRIM(ROLL_NO_A)) = @BALE_NO
                          AND NT_WT_A = @QTY;
                    ", con, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@REMARK", remarkValue);
                            updateCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            updateCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            updateCmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                            updateCmd.Parameters.AddWithValue("@BALE_NO", item.BaleNo);
                            updateCmd.Parameters.AddWithValue("@QTY", item.Qty);

                            await updateCmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }



    }
}
