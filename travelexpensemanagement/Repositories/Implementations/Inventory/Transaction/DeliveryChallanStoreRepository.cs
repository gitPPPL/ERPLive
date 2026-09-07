using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Repositories.Implementations.Inventory.Transaction
{
    public class DeliveryChallanStoreRepository : IDeliveryChallanStoreRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly LogService.LogService _logService;
        public DeliveryChallanStoreRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DbHelper dbHelper,
            GlobalValidationdate globalValidationdate, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
        }
        public RepositoryResponseData<AddressDetails> GetAddressByBillToParty(int code, int addressId)
        {
            try
            {
                var addressDetails = new AddressDetails();

                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection connection = _dbConnection.GetErpConnection())
                {
                    //using (SqlCommand cmd = new SqlCommand("Select top(1) ADD1,ADD2,ADD3,PINCODE,GSTIN,CITY_CODE from SUBGROUP_ADDRESS where COMP_CODE = @COMP_CODE AND Code = @PCODE", connection))
                    using (SqlCommand cmd = new SqlCommand(
                        $@"Select a.Add1,a.Add2,a.Add3,a.GSTIN,a.City_Code,b.Name State,c.name City,a.Pincode,a.Distance , d.einv_party
                            from Subgroup_Address a left join STATE_MAST b on a.STATE_CODE=b.code left join CITY_MAST c on a.CITY_CODE=c.code
                            left join SUBGROUP_MAST d on d.CODE = a.code 
                            where a.comp_code=@COMP_CODE and a.Code=@CODE and a.Address_Id=@Address_Id",
                        connection))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@Address_Id", addressId);
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                addressDetails.add1 = reader["ADD1"].ToString();
                                addressDetails.add2 = reader["ADD2"].ToString();
                                addressDetails.add3 = reader["ADD3"].ToString();
                                addressDetails.pincode = reader["PINCODE"].ToString();
                                addressDetails.gstin = reader["GSTIN"].ToString();
                                addressDetails.cityCode = reader["CITY_CODE"].ToString();
                                addressDetails.einv_party = reader["einv_party"] != DBNull.Value ? Convert.ToInt32(reader["einv_party"]) : 0;

                            }
                        }
                    }
                }
                return new RepositoryResponseData<AddressDetails> { status = true, data = addressDetails };
            }
            catch (Exception)
            {
                return new RepositoryResponseData<AddressDetails> { status = false, message = "Error retrieving the address by specfic address id" };
            }
        }

        public RepositoryResponseData<DeliveryChallanStoreOrderDetails> GetDetailsOnRefNoLoad(string vType, int vNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var itemList = new List<DeliveryChallanStoreOrderItem>();
            var data = new DeliveryChallanStoreOrderDetails
            {
                Items = itemList
            };
            try
            {
                //string headerQry = $@"Select ORDER1.V_DATE, ORDER1.V_TYPE, ORDER1.V_NO, ORDER1.PARTY_CODE, SUBGROUP_MAST.NAME [PARTY_NAME], 
                //                    ORDER1.BILL_ADD1, ORDER1.BILL_ADD2, ORDER1.BILL_ADD3, ORDER1.BILL_CITY, CITY_MAST.NAME [CITY_NAME], 
                //                    ORDER1.BILL_GST, ORDER1.BILL_PINCODE, ORDER1.SHIP_FROM, SHIPSG.NAME [SHIP_NAME], ORDER1.SHIP_ADD1, 
                //                    ORDER1.SHIP_ADD2, ORDER1.SHIP_ADD3, ORDER1.SHIP_CITY, SHIPCITY.NAME [SHIPCITY_NAME], 
                //                    ORDER1.SHIP_GST, ORDER1.SHIP_PINCODE from ORDER1
                //                    LEFT JOIN SUBGROUP_MAST ON SUBGROUP_MAST.CODE = ORDER1.PARTY_CODE  AND SUBGROUP_MAST.COMP_CODE = 
                //                    ORDER1.COMP_CODE 
                //                    LEFT JOIN SUBGROUP_MAST SHIPSG ON SHIPSG.CODE = ORDER1.SHIP_FROM  AND SHIPSG.COMP_CODE = 
                //                    ORDER1.COMP_CODE
                //                    LEFT JOIN CITY_MAST ON  CITY_MAST.CODE = ORDER1.BILL_CITY LEFT JOIN CITY_MAST SHIPCITY ON  
                //                    SHIPCITY.CODE = ORDER1.SHIP_CITY 
                //                    where ORDER1.V_TYPE = @V_TYPE  AND ORDER1.V_NO = @V_NO AND ORDER1.COMP_CODE = @COMP_CODE and 
                //                    ORDER1.BRANCH_CODE = @BRANCH_CODE and ORDER1.YEAR_CODE = @YEAR_CODE;
                                    
                //                    Select ORDER2.ITEM_CODE, ITEM_MAST.NAME[item_name], ITEM_MAST.UNIT_CODE, ITEM_MAST.UNIT_NAME, 
                //                    item_mast.HSN_CODE, ORDER2.NOS, ORDER2.QTY, ORDER2.RATE, ORDER2.AMOUNT, ORDER2.PACK_PER, ORDER2.PACK_AMT,
                //                    ORDER2.DISC_PER, ORDER2.DISC_AMT, ORDER2.CGST_PER, ORDER2.CGST_AMT, ORDER2.SGST_PER, ORDER2.SGST_AMT, 
                //                    ORDER2.IGST_PER, ORDER2.IGST_AMT from ORDER2 
                //                    LEFT JOIN ITEM_MAST ON ITEM_MAST.CODE=ORDER2.ITEM_CODE AND ITEM_MAST.COMP_CODE = ORDER2.COMP_CODE 
                //                    where V_TYPE = @V_TYPE  AND V_NO = @V_NO AND ORDER2.COMP_CODE = @COMP_CODE 
                //                    and ORDER2.BRANCH_CODE = @BRANCH_CODE  AND ORDER2.YEAR_CODE = @YEAR_CODE";

                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_DeliveryChallanStore", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "LOADBYREFNO");
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                data.V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null;
                                data.V_TYPE = reader["V_TYPE"]?.ToString();
                                data.V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : null;
                                data.PARTY_CODE = reader["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PARTY_CODE"]) : null;
                                data.BILL_ADD1 = reader["BILL_ADD1"]?.ToString();
                                data.BILL_ADD2 = reader["BILL_ADD2"]?.ToString();
                                data.BILL_ADD3 = reader["BILL_ADD3"]?.ToString();
                                data.BILL_CITY = reader["BILL_CITY"] != DBNull.Value ? Convert.ToInt32(reader["BILL_CITY"]) : null;
                                data.BILL_GST = reader["BILL_GST"]?.ToString();
                                data.BILL_PINCODE = reader["BILL_PINCODE"]?.ToString();

                                data.SHIP_FROM = reader["SHIP_FROM"] != DBNull.Value ? Convert.ToInt32(reader["SHIP_FROM"]) : null;
                                data.SHIP_ADD1 = reader["SHIP_ADD1"]?.ToString();
                                data.SHIP_ADD2 = reader["SHIP_ADD2"]?.ToString();
                                data.SHIP_ADD3 = reader["SHIP_ADD3"]?.ToString();
                                data.SHIP_CITY = reader["SHIP_CITY"] != DBNull.Value ? Convert.ToInt32(reader["SHIP_CITY"]) : null;
                                data.SHIP_GST = reader["SHIP_GST"]?.ToString();
                                data.SHIP_PINCODE = reader["SHIP_PINCODE"]?.ToString();
                            }

                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    itemList.Add(new DeliveryChallanStoreOrderItem
                                    {
                                        ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : null,
                                        ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
                                        UOM_CODE = reader["UNIT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["UNIT_CODE"]) : null,
                                        UNIT = reader["UNIT_NAME"]?.ToString(),
                                        HSN_CODE = reader["HSN_CODE"]?.ToString(),
                                        NOS = reader["NOS"] != DBNull.Value ? Convert.ToInt32(reader["NOS"]) : null,
                                        RECD_QTY = reader["QTY"] != DBNull.Value ? Convert.ToDecimal(reader["QTY"]) : null,
                                        INV_QTY = reader["QTY"] != DBNull.Value ? Convert.ToDecimal(reader["QTY"]) : null,
                                        RATE = reader["RATE"] != DBNull.Value ? Convert.ToDecimal(reader["RATE"]) : null,
                                        AMOUNT = reader["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["AMOUNT"]) : null,
                                        PACK_PER = reader["PACK_PER"] != DBNull.Value ? Convert.ToDecimal(reader["PACK_PER"]) : null,
                                        PACK_AMT = reader["PACK_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["PACK_AMT"]) : null,
                                        DISC_PER = reader["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(reader["DISC_PER"]) : null,
                                        DISC_AMT = reader["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["DISC_AMT"]) : null,
                                        CGST_PER = reader["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["CGST_PER"]) : null,
                                        CGST_AMT = reader["CGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["CGST_AMT"]) : null,
                                        SGST_PER = reader["SGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["SGST_PER"]) : null,
                                        SGST_AMT = reader["SGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["SGST_AMT"]) : null,
                                        IGST_PER = reader["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["IGST_PER"]) : null,
                                        IGST_AMT = reader["IGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["IGST_AMT"]) : null,
                                    });
                                }
                            }
                        }
                    }
                }
                return new RepositoryResponseData<DeliveryChallanStoreOrderDetails> { status = true, data = data };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<DeliveryChallanStoreOrderDetails> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseList<DeliveryChallanStoreOrderItem> GetDetailsOnWBLoad(int vNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var itemList = new List<DeliveryChallanStoreOrderItem>();
            try
            {
                string headerQry = $@"SELECT ITEM_CODE, ITEM_NAME, ITEM_MAST.HSN_CODE , itemunit_mast.name [unit_name], 
                                        itemunit_mast.code [unit_code] FROM WB2 
                                        LEFT JOIN ITEM_MAST ON ITEM_MAST.CODE=WB2.ITEM_CODE AND ITEM_MAST.COMP_CODE=WB2.COMP_CODE  
                                        LEFT JOIN ITEMUNIT_MAST ON ITEMUNIT_MAST.CODE=ITEM_MAST.UNIT_CODE AND ITEMUNIT_MAST.COMP_CODE = 
                                        ITEM_MAST.COMP_CODE  
                                        WHERE V_TYPE='KSOT' AND V_NO=@V_NO AND WB2.COMP_CODE = @COMP_CODE AND BRANCH_CODE=@BRANCH_CODE";

                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(headerQry, con))
                    {
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                itemList.Add(new DeliveryChallanStoreOrderItem
                                {
                                    ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : null,
                                    ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
                                    UOM_CODE = reader["UNIT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["UNIT_CODE"]) : null,
                                    UNIT = reader["UNIT_NAME"]?.ToString(),
                                    HSN_CODE = reader["HSN_CODE"]?.ToString(),
                                    REF_NO = vNo,
                                    REF_TYPE = "KSOT"
                                });
                            }
                        }
                    }
                }
                return new RepositoryResponseList<DeliveryChallanStoreOrderItem> { status = true, data = itemList };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<DeliveryChallanStoreOrderItem> { status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponse> SaveDeliveryChallanStore(DeliveryChallanStoreModel model)
        {
            if (model == null || model.items == null)
            {
                return new RepositoryResponse { status = false, message = "Invalid request!" };
            }
            var gv = _globalVariableService.GetGlobalVariables();
            using (var con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (SqlTransaction tran = con.BeginTransaction())
                {
                    try
                    {
                        string appQry = $@"select APPROV_USER from DOC_APPROSTAGE where USER_CODE={gv.PubUserId} and DOC_CODE='{model.V_TYPE}' 
                                            and comp_code={gv.PubCompCode}";

                        string result = await _dbHelper.GetExecuteScalarAsync<string>(appQry);
                        string fAppRemarks = "";
                        string fAppStatus = "";
                        if (string.Equals(result, "FINAL", StringComparison.OrdinalIgnoreCase))
                        {
                            fAppRemarks = "Document Approved.";
                            fAppStatus = "Approved";
                        }
                        string mode = "";
                        string action = model.ACTION == "INSERT" ? "HeaderInsert" : "UpdateHeader";
                        string? docId = string.IsNullOrEmpty(model.V_TYPE?.ToString()) && string.IsNullOrEmpty(model.V_NO?.ToString())
                                                                ? null : model.V_TYPE?.ToString() + model.V_NO?.ToString();
                        //==========================HEADER======================
                        using (SqlCommand cmd = new SqlCommand("sp_DeliveryChallanStore", con, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Action", action);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DOC_ID", docId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_NO", model.BILL_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_DATE", model.BILL_DATE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ITEM_TYPE", model.ITEM_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_CODE", model.BILL_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_NAME", model.BILL_NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_ADD1", model.BILL_ADD1 ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_ADD2", model.BILL_ADD2 ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_ADD3", model.BILL_ADD3 ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_CITY", model.BILL_CITY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_GST", model.BILL_GST ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_PINCODE", model.BILL_PINCODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DRCR_CODE", model.DRCR_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BROKER_CODE", 0);
                            cmd.Parameters.AddWithValue("@PARTY_NAME", "");
                            cmd.Parameters.AddWithValue("@SHIP_CODE", model.SHIP_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_NAME", model.SHIP_NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_ADD1", model.SHIP_ADD1 ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_ADD2", model.SHIP_ADD2 ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_ADD3", model.SHIP_ADD3 ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_CITY", model.SHIP_CITY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_GST", model.SHIP_GST ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIP_PINCODE", model.SHIP_PINCODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CITY_CODE", model.CITY_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DOC_TYPE", model.DOC_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DOC_NO", model.DOC_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DOC_DATE", model.DOC_DATE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DOC_NAME", model.DOC_NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TOT_NOS", model.TOT_NOS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TOT_GROSS", model.TOT_GROSS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TOT_QTY", model.TOT_QTY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@AMOUNT", model.AMOUNT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DISC_PER", model.DISC_PER ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DISC_AMT", model.DISC_AMT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CESS_PER", 0);
                            cmd.Parameters.AddWithValue("@CESS_AMT", 0);
                            cmd.Parameters.AddWithValue("@PACK_PER", model.PACK_PER ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PACK_AMT", model.PACK_AMT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SGST_PER", model.SGST_PER ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IGST_PER", model.IGST_PER ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CGST_PER", model.CGST_PER ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CGST_AMT", model.CGST_AMT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SGST_AMT", model.SGST_AMT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IGST_AMT", model.IGST_AMT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@FRT_AMT", model.FRT_AMT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@FRT_TAXP", model.FRT_TAXP ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@FRT_TAX", model.FRT_TAX ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TCS_PER", model.TCS_PER ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TCS_AMT", model.TCS_AMT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ROUNDOFF", model.ROUNDOFF ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@NAMOUNT", model.NAMOUNT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@INPUT_TYPE", "");
                            cmd.Parameters.AddWithValue("@GR_NO", model.GR_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@GR_DATE", model.GR_DATE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TRANSPORT_NAME", model.TRANSPORT_NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TRANSPORT_CODE", model.TRANSPORT_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TRUCK_NO", model.TRUCK_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TDS_PER", model.TDS_PER ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TDS_AMT", model.TDS_AMT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@STATION_NAME", model.STATION_NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@STATION_CODE", model.STATION_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CONSG_ADD_ID", model.CONSG_ADD_ID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PARTY_ADD_ID", model.PARTY_ADD_ID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@REMARK", model.REMARK ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@FAPROV_STATUS", fAppStatus);
                            cmd.Parameters.AddWithValue("@FAPROV_REMARKS", fAppRemarks);
                            cmd.Parameters.AddWithValue("@NATURE_OFWORK", model.NATURE_OFWORK ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@STATUS", model.STATUS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IRN", model.IRN ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SIGNED_JSON", model.SIGNED_JSON ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SIGNED_QR", model.SIGNED_QR ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@EINVOICE_FLG", model.EINVOICE_FLG ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@EWAYBILL_FLG", model.EWAYBILL_FLG ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@EWAYBILL_NO", model.EWAYBILL_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@EWAYBILL_JSON", model.EWAYBILL_JSON ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@EWAYBILL_DATE", model.EWAYBILL_DATE ?? (object)DBNull.Value);
                            if (model.ACTION == "INSERT")
                            {
                                cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                mode = "INSERT";
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@EUSER", gv.PubUserId);
                                mode = "UPDATE";
                            }
                            cmd.Parameters.AddWithValue("@SRNO", model.SRNO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmd.Parameters.AddWithValue("@SUPPLY_TYPE", model.SUPPLY_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@GSTRECO_REFTYPE", model.GSTRECO_REFTYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@GSTRECO_REFNO", model.GSTRECO_REFNO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TRAN_TYPE", model.TRAN_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MAILSEND", model.MAILSEND ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MONTH_3B", model.MONTH_3B ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MONTH_3BN", model.MONTH_3BN ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MTH_REVYN3B", model.MTH_REVYN3B ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MOVE_TYPE", model.MOVE_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DESP_ADDRESS", model.DESP_ADDRESS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@JW_NATURE", model.JW_NATURE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@EWB_NO", model.EWB_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@EMP_CODE", model.EMP_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@RET_DATE", model.RET_DATE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DESP_FROMPARTY", model.DESP_FROMPARTY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DESP_TOPARTY", model.DESP_TOPARTY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DESP_FROMGST", model.DESP_FROMGST ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DESP_TOGST", model.DESP_TOGST ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DESP_FROMCITY", model.DESP_FROMCITY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DESP_TOPCITY", model.DESP_TOPCITY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TPT_DISTANCE", model.TPT_DISTANCE ?? (object)DBNull.Value);

                            cmd.ExecuteNonQuery();
                        }

                        //==========================FOOTER======================
                        string DelQry = @"DELETE FROM DC_NOTE2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE 
                                                AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                        using (SqlCommand fDelcmd = new SqlCommand(DelQry, con, tran))
                        {

                            fDelcmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                            fDelcmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                            fDelcmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            fDelcmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                            fDelcmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                            fDelcmd.ExecuteNonQuery();
                        }

                        int i = 1;
                        foreach (var item in model.items)
                        {
                            using (SqlCommand fInscmd = new SqlCommand("sp_DeliveryChallanStore", con, tran))
                            {
                                fInscmd.CommandType = CommandType.StoredProcedure;
                                fInscmd.Parameters.AddWithValue("@Action", "FooterInsert");
                                fInscmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                fInscmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                fInscmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                fInscmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@DOC_ID", docId ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@ITEM_CODE", item.ITEM_CODE ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@ITEM_NAME", item.ITEM_NAME ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@ITEM_UNIT", item.ITEM_UNIT ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@HSN_CODE", item.HSN_CODE ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@SNO", i++);
                                fInscmd.Parameters.AddWithValue("@NOS", item.NOS ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@GROSS", item.GROSS ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@QTY", item.QTY ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@RATE", item.RATE ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@AMOUNT", item.AMOUNT ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@DISC_PER", item.DISC_PER ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@CGST_PER", item.CGST_PER ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@SGST_PER", item.SGST_PER ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@IGST_PER", item.IGST_PER ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@DISC_AMT", item.DISC_AMT ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@CGST_AMT", item.CGST_AMT ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@SGST_AMT", item.SGST_AMT ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@IGST_AMT", item.IGST_AMT ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@PACK_AMT", item.PACK_AMT ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@PACK_PER", item.PACK_PER ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@CESS_PER", item.CESS_PER ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@CESS_AMT", item.CESS_AMT ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@REMARK", item.REMARK ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@REF_TYPE", item.REF_TYPE ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@REF_NO", item.REF_NO ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@WB_TYPE", item.WB_TYPE ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@WB_NO", item.WB_NO ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@TYPE", item.TYPE ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@STATUS", item.STATUS ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                fInscmd.Parameters.AddWithValue("@SRNO", item.SRNO ?? (object)DBNull.Value);
                                fInscmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                fInscmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                fInscmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                fInscmd.Parameters.AddWithValue("@TAX_CODE", item.TAX_CODE ?? (object)DBNull.Value);

                                fInscmd.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                        //Log Service
                        //_logService.InsertLog("DC_NOTE1", "Delivery Challan Store", "Transaction", mode, model.V_TYPE, model.V_NO.ToString(), model.V_DATE);
                        //_logService.InsertLog("DC_NOTE2", "Delivery Challan Store", "Transaction", mode, model.V_TYPE, model.V_NO.ToString(), model.V_DATE);
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

        public RepositoryResponseData<DeliveryChallanStoreModel> GetDataById(string docId, string docType)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new DeliveryChallanStoreModel();
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_DeliveryChallanStore", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetById");
                        cmd.Parameters.AddWithValue("@V_NO", docId);
                        cmd.Parameters.AddWithValue("@V_TYPE", docType);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                data.V_NO = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]);
                                data.V_TYPE = reader["V_TYPE"] == DBNull.Value ? null : reader["V_TYPE"].ToString();
                                data.V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]);
                                data.TRAN_TYPE = reader["TRAN_TYPE"] == DBNull.Value ? null : reader["TRAN_TYPE"].ToString();
                                data.DOC_TYPE = reader["DOC_TYPE"] == DBNull.Value ? null : reader["DOC_TYPE"].ToString();
                                data.DOC_NO = reader["DOC_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["DOC_NO"]);
                                data.DOC_DATE = reader["DOC_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["DOC_DATE"]);
                                data.BILL_CODE = reader["BILL_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["BILL_CODE"]);
                                data.BILL_ADD1 = reader["BILL_ADD1"] == DBNull.Value ? null : reader["BILL_ADD1"].ToString();
                                data.BILL_ADD2 = reader["BILL_ADD2"] == DBNull.Value ? null : reader["BILL_ADD2"].ToString();
                                data.BILL_ADD3 = reader["BILL_ADD3"] == DBNull.Value ? null : reader["BILL_ADD3"].ToString();
                                data.BILL_CITY = reader["BILL_CITY"] == DBNull.Value ? null : Convert.ToInt32(reader["BILL_CITY"]);
                                data.BILL_GST = reader["BILL_GST"] == DBNull.Value ? null : reader["BILL_GST"].ToString();
                                data.BILL_PINCODE = reader["BILL_PINCODE"] == DBNull.Value ? null : reader["BILL_PINCODE"].ToString();
                                data.CITY_CODE = reader["CITY_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["CITY_CODE"]);
                                data.DESP_FROMCITY = reader["DESP_FROMCITY"] == DBNull.Value ? null : Convert.ToInt32(reader["DESP_FROMCITY"]);
                                data.DESP_FROMPARTY = reader["DESP_FROMPARTY"] == DBNull.Value ? null : Convert.ToInt32(reader["DESP_FROMPARTY"]);
                                data.DESP_FROMGST = reader["DESP_FROMGST"] == DBNull.Value ? null : reader["DESP_FROMGST"].ToString();
                                data.SHIP_CODE = reader["SHIP_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["SHIP_CODE"]);
                                data.SHIP_ADD1 = reader["SHIP_ADD1"] == DBNull.Value ? null : reader["SHIP_ADD1"].ToString();
                                data.SHIP_ADD2 = reader["SHIP_ADD2"] == DBNull.Value ? null : reader["SHIP_ADD2"].ToString();
                                data.SHIP_ADD3 = reader["SHIP_ADD3"] == DBNull.Value ? null : reader["SHIP_ADD3"].ToString();
                                data.SHIP_CITY = reader["SHIP_CITY"] == DBNull.Value ? null : Convert.ToInt32(reader["SHIP_CITY"]);
                                data.SHIP_GST = reader["SHIP_GST"] == DBNull.Value ? null : reader["SHIP_GST"].ToString();
                                data.SHIP_PINCODE = reader["SHIP_PINCODE"] == DBNull.Value ? null : reader["SHIP_PINCODE"].ToString();
                                data.MOVE_TYPE = reader["MOVE_TYPE"] == DBNull.Value ? null : reader["MOVE_TYPE"].ToString();
                                data.DESP_TOPARTY = reader["DESP_TOPARTY"] == DBNull.Value ? null : Convert.ToInt32(reader["DESP_TOPARTY"]);
                                data.DESP_TOPCITY = reader["DESP_TOPCITY"] == DBNull.Value ? null : Convert.ToInt32(reader["DESP_TOPCITY"]);
                                data.DESP_TOGST = reader["DESP_TOGST"] == DBNull.Value ? null : reader["DESP_TOGST"].ToString();
                                data.BILL_NO = reader["BILL_NO"] == DBNull.Value ? null : reader["BILL_NO"].ToString();
                                data.BILL_DATE = reader["BILL_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["BILL_DATE"]);
                                data.NATURE_OFWORK = reader["NATURE_OFWORK"] == DBNull.Value ? null : reader["NATURE_OFWORK"].ToString();
                                data.JW_NATURE = reader["JW_NATURE"] == DBNull.Value ? null : reader["JW_NATURE"].ToString();
                                data.EWB_NO = reader["EWB_NO"] == DBNull.Value ? null : reader["EWB_NO"].ToString();
                                data.EMP_CODE = reader["EMP_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["EMP_CODE"]);
                                data.RET_DATE = reader["RET_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["RET_DATE"]);
                                data.TPT_DISTANCE = reader["TPT_DISTANCE"] == DBNull.Value ? null : Convert.ToInt32(reader["TPT_DISTANCE"]);
                                data.REMARK = reader["REMARK"] == DBNull.Value ? null : reader["REMARK"].ToString();
                                data.DESP_ADDRESS = reader["DESP_ADDRESS"] == DBNull.Value ? null : reader["DESP_ADDRESS"].ToString();
                                data.CONSG_ADD_ID = reader["CONSG_ADD_ID"] == DBNull.Value ? null : Convert.ToInt32(reader["CONSG_ADD_ID"]);
                                data.PARTY_ADD_ID = reader["PARTY_ADD_ID"] == DBNull.Value ? null : Convert.ToInt32(reader["PARTY_ADD_ID"]);

                                data.TOT_NOS = reader["TOT_NOS"] == DBNull.Value ? null : Convert.ToDecimal(reader["TOT_NOS"]);
                                data.TOT_GROSS = reader["TOT_GROSS"] == DBNull.Value ? null : Convert.ToDecimal(reader["TOT_GROSS"]);
                                data.TOT_QTY = reader["TOT_QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["TOT_QTY"]);
                                data.AMOUNT = reader["AMOUNT"] == DBNull.Value ? null : Convert.ToDecimal(reader["AMOUNT"]);
                                data.DISC_PER = reader["DISC_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["DISC_PER"]);
                                data.DISC_AMT = reader["DISC_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["DISC_AMT"]);
                                data.PACK_PER = reader["PACK_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["PACK_PER"]);
                                data.PACK_AMT = reader["PACK_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["PACK_AMT"]);
                                data.CGST_PER = reader["CGST_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["CGST_PER"]);
                                data.CGST_AMT = reader["CGST_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["CGST_AMT"]);
                                data.SGST_PER = reader["SGST_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["SGST_PER"]);
                                data.SGST_AMT = reader["SGST_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["SGST_AMT"]);
                                data.IGST_PER = reader["IGST_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["IGST_PER"]);
                                data.IGST_AMT = reader["IGST_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["IGST_AMT"]);
                                data.ROUNDOFF = reader["ROUNDOFF"] == DBNull.Value ? null : Convert.ToDecimal(reader["ROUNDOFF"]);
                                data.NAMOUNT = reader["NAMOUNT"] == DBNull.Value ? null : Convert.ToDecimal(reader["NAMOUNT"]);

                                data.TRANSPORT_CODE = reader["TRANSPORT_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["TRANSPORT_CODE"]);
                                data.TRUCK_NO = reader["TRUCK_NO"]?.ToString();
                                data.GR_NO = reader["GR_NO"]?.ToString();
                                data.GR_DATE = reader["GR_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["GR_DATE"]);
                            }

                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    var item = new DeliveryChallanStoreFooterModel
                                    {
                                        ITEM_CODE = reader["ITEM_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["ITEM_CODE"]),
                                        ITEM_NAME = reader["ITEM_NAME"] == DBNull.Value ? null : reader["ITEM_NAME"].ToString(),
                                        HSN_CODE = reader["HSN_CODE"] == DBNull.Value ? null : reader["HSN_CODE"].ToString(),
                                        ITEM_UNIT = reader["ITEM_UNIT"] == DBNull.Value ? null : reader["ITEM_UNIT"].ToString(),
                                        NOS = reader["NOS"] == DBNull.Value ? null : Convert.ToDecimal(reader["NOS"]),
                                        GROSS = reader["GROSS"] == DBNull.Value ? null : Convert.ToDecimal(reader["GROSS"]),
                                        QTY = reader["QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["QTY"]),
                                        RATE = reader["RATE"] == DBNull.Value ? null : Convert.ToDecimal(reader["RATE"]),
                                        AMOUNT = reader["AMOUNT"] == DBNull.Value ? null : Convert.ToDecimal(reader["AMOUNT"]),
                                        TAX_CODE = reader["TAX_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["TAX_CODE"]),
                                        PACK_PER = reader["PACK_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["PACK_PER"]),
                                        PACK_AMT = reader["PACK_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["PACK_AMT"]),
                                        DISC_PER = reader["DISC_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["DISC_PER"]),
                                        DISC_AMT = reader["DISC_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["DISC_AMT"]),
                                        SGST_PER = reader["SGST_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["SGST_PER"]),
                                        SGST_AMT = reader["SGST_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["SGST_AMT"]),
                                        CGST_PER = reader["CGST_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["CGST_PER"]),
                                        CGST_AMT = reader["CGST_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["CGST_AMT"]),
                                        IGST_PER = reader["IGST_PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["IGST_PER"]),
                                        IGST_AMT = reader["IGST_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["IGST_AMT"]),
                                        REF_TYPE = reader["WB_TYPE"] == DBNull.Value ? null : reader["WB_TYPE"].ToString(),
                                        REF_NO = reader["WB_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["WB_NO"])
                                    };

                                    data.items.Add(item);
                                }
                            }
                        }
                    }
                }
                return new RepositoryResponseData<DeliveryChallanStoreModel> { status = true, data = data };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<DeliveryChallanStoreModel> { status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponse> CheckWeight(WBCheckRequest request)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                if (request.VType != "DCHL" && request.VType != "DCIC")
                {
                    return new RepositoryResponse { status = true, message = "" };
                }

                foreach (var item in request.Items)
                {
                    string wbType = item.WbType ?? "";
                    string wbNo = item.WbNo ?? "";

                    // Weighing not done
                    if (wbType == "" && (string.IsNullOrEmpty(wbNo) || double.TryParse(wbNo, out double wbNoValue) && wbNoValue == 0))
                    {
                        return new RepositoryResponse { status = false, message = $"Weighing of item : {item.ItemName} not done (Need Approval)." };
                    }

                    string WB_DOC_NO = wbType + wbNo;
                    double WBQTY = 0;

                    if (WB_DOC_NO.Length == 13)
                    {
                        string sql = $@"SELECT NET_WGT FROM wb2 WHERE item_code = {item.ItemCode} AND doc_id = '{WB_DOC_NO}' AND comp_code = {gv.PubCompCode}
                      AND branch_code = {gv.PubBranchCode}";

                        WBQTY = await _dbHelper.GetExecuteScalarAsync<double>(sql);
                    }

                    // Compare weight
                    if (WBQTY != item.Quantity)
                    {
                        return new RepositoryResponse { status = false, message = $"Net Weight of item = {item.ItemName} did not match with WB NO. = {WB_DOC_NO}" };
                    }
                }

                return new RepositoryResponse { status = true, message = "" };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }
    }
}
