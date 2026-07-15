using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseReceiptEntry;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class PurchaseReceiptEntryRepository : IPurchaseReceiptEntryRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        public PurchaseReceiptEntryRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
        }

        public async Task<(bool Success, string Message)> SaveAllData(string Header,List<ItemDetailModel> ItemDetails,List<AttachmentModel> Attachments)
        {
            var headerObj = JsonConvert.DeserializeObject<PurchaseReceiptHeaderModel>(Header);
            var globalVar = _globalVariableService.GetGlobalVariables();
            int transportCode = 0;
            bool isUpdate = !string.IsNullOrWhiteSpace(headerObj.code) && headerObj.code != "0";
            bool isInsert = !isUpdate;
            string vNo = isInsert ? headerObj.DocNo : headerObj.code;
            string DOC_ID = headerObj.DocType + vNo;

            // ================= Validation =================
            var validationResult = await ValidatePurchaseReceiptAsync(headerObj, ItemDetails, isInsert, vNo);

            if (!validationResult.IsValid)
            {
                return (false, validationResult.Message);
            }

            if (isInsert)
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    string checkQuery = @"SELECT COUNT(*) FROM PURCHASE1 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO";

                    using (var cmd = new SqlCommand(checkQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);

                        int count = (int)await cmd.ExecuteScalarAsync();

                        if (count > 0)
                        {
                            return (false, "Record already exists in PURCHASE1.");
                        }
                    }
                }
            }

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (var transaction = con.BeginTransaction())
                {
                    try
                    {
                        using (var cmdHeader = new SqlCommand("InsertPurchaseReceiptHeader", con, transaction))
                        {
                            cmdHeader.CommandType = CommandType.StoredProcedure;
                            AddParameterSafe(cmdHeader, "@COMP_CODE", globalVar.PubCompCode);
                            AddParameterSafe(cmdHeader, "@BRANCH_CODE", globalVar.PubBranchCode);
                            AddParameterSafe(cmdHeader, "@YEAR_CODE", globalVar.PubFYearCode);
                            AddParameterSafe(cmdHeader, "@DOC_ID", DOC_ID);
                            AddParameterSafe(cmdHeader, "@V_NO", vNo);
                            AddParameterSafe(cmdHeader, "@V_TYPE", headerObj.DocType);
                            AddParameterSafe(cmdHeader, "@V_DATE", DateTime.Parse(headerObj.DocDate));
                            AddParameterSafe(cmdHeader, "@EXCH_RATE", headerObj.ExchangeRate);
                            AddParameterSafe(cmdHeader, "@PARTY_CODE", headerObj.BillFrom);
                            AddParameterSafe(cmdHeader, "@BILL_ADD1", headerObj.AddLine1);
                            AddParameterSafe(cmdHeader, "@BILL_ADD2", headerObj.AddLine2);
                            AddParameterSafe(cmdHeader, "@BILL_ADD3", headerObj.AddLine3);
                            AddParameterSafe(cmdHeader, "@BILL_CITY", headerObj.City);
                            AddParameterSafe(cmdHeader, "@BILL_PINCODE", headerObj.Pincode);
                            AddParameterSafe(cmdHeader, "@BILL_ADDRESSID", headerObj.BILL_ADDRESSID);
                            AddParameterSafe(cmdHeader, "@BILL_GST", headerObj.GST);
                            AddParameterSafe(cmdHeader, "@SHIP_GST", headerObj.ShipGST);

                            AddParameterSafe(cmdHeader, "@SHIP_CODE", headerObj.ShipFrom);
                            AddParameterSafe(cmdHeader, "@SHIP_ADD1", headerObj.ShipAddLine1);
                            AddParameterSafe(cmdHeader, "@SHIP_ADD2", headerObj.ShipAddLine2);
                            AddParameterSafe(cmdHeader, "@SHIP_ADD3", headerObj.ShipAddLine3);
                            AddParameterSafe(cmdHeader, "@SHIP_CITY", headerObj.ShipCity);
                            AddParameterSafe(cmdHeader, "@SHIP_PINCODE", headerObj.ShipPincode);
                            AddParameterSafe(cmdHeader, "@SHIP_ADDRESSID", headerObj.SHIP_ADDRESSID);

                            AddParameterSafe(cmdHeader, "@BILL_NO", headerObj.BillNo);
                            AddParameterSafe(cmdHeader, "@BILL_DATE", DateTime.Parse(headerObj.BillDate));
                            AddParameterSafe(cmdHeader, "@CHALL_NO", headerObj.ChallanNo);
                            AddParameterSafe(cmdHeader, "@CHALL_DATE", string.IsNullOrWhiteSpace(headerObj.ChallanDate) ? (object)DBNull.Value : DateTime.Parse(headerObj.ChallanDate));
                            AddParameterSafe(cmdHeader, "@GATE_NO", headerObj.GateNo);
                            AddParameterSafe(cmdHeader, "@GATE_TYPE", headerObj.GATE_TYPE);
                            AddParameterSafe(cmdHeader, "@TRANSIT_NO", headerObj.TRANSIT_NO);

                            AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WaybillNo);

                            if (!string.IsNullOrWhiteSpace(headerObj.TransportName) && int.TryParse(headerObj.TRANSPORT_CODE, out int code) && code > 0)
                            {
                                transportCode = code;
                            }

                            AddParameterSafe(cmdHeader, "@TRANSPORT_CODE", transportCode);
                            AddParameterSafe(cmdHeader, "@TRANSPORT_NAME", headerObj.TransportName?.Trim());
                            AddParameterSafe(cmdHeader, "@GR_NO", headerObj.GRNo);
                            AddParameterSafe(cmdHeader, "@GR_DATE", headerObj.GRDate);

                            AddParameterSafe(cmdHeader, "@TRUCK_NO", headerObj.VehicleNo);
                            AddParameterSafe(cmdHeader, "@CONTAINER_NO", headerObj.ContainerNo);
                            AddParameterSafe(cmdHeader, "@FRTPAY_AMT", headerObj.FreightPay);
                            AddParameterSafe(cmdHeader, "@FRTPAY_TAXPER", headerObj.FrtTax1);
                            AddParameterSafe(cmdHeader, "@FRTPAY_TAX", headerObj.FrtTax2);
                            AddParameterSafe(cmdHeader, "@FRTPAY_NAR", headerObj.FrtPayNarr);
                            AddParameterSafe(cmdHeader, "@REMARKS", headerObj.Remarks);
                            AddParameterSafe(cmdHeader, "@NAMOUNT", headerObj.NumFinalNetAmt);

                            AddParameterSafe(cmdHeader, "@EWB_DATE", headerObj.EWB_DATE);
                            AddParameterSafe(cmdHeader, "@EWB_EXPDATE", headerObj.EWB_EXPDATE);
                            AddParameterSafe(cmdHeader, "@EWB_INVNO", headerObj.EWB_INVNO);
                            AddParameterSafe(cmdHeader, "@HOLD_PAY", headerObj.HOLD_PAY);
                            AddParameterSafe(cmdHeader, "@HOLD_REASON", headerObj.HOLD_REASON);
                            AddParameterSafe(cmdHeader, "@HOLD_DATE", headerObj.HOLD_DATE);

                            AddParameterSafe(cmdHeader, "@STATUS", headerObj.DocStatus);
                            AddParameterSafe(cmdHeader, "@RECD_QTY", headerObj.NumReceivedQty);
                            AddParameterSafe(cmdHeader, "@BILL_QTY", headerObj.NumBillQty);
                            AddParameterSafe(cmdHeader, "@AMOUNT", headerObj.NumAmount);
                            AddParameterSafe(cmdHeader, "@DISC_AMT", headerObj.NumDiscount);
                            AddParameterSafe(cmdHeader, "@PACK_AMT", headerObj.NumPacking);
                            AddParameterSafe(cmdHeader, "@CGST_AMT", headerObj.NumCGST);
                            AddParameterSafe(cmdHeader, "@SGST_AMT", headerObj.NumSGST);
                            AddParameterSafe(cmdHeader, "@IGST_AMT", headerObj.NumIGST);
                            AddParameterSafe(cmdHeader, "@CESS_AMT", headerObj.NumCESS);
                            AddParameterSafe(cmdHeader, "@VAT_AMT", headerObj.NumVAT);
                            AddParameterSafe(cmdHeader, "@OTH_AMT", headerObj.NumOtherAmt);
                            AddParameterSafe(cmdHeader, "@TCS_PER", headerObj.NumTCSPer1);
                            AddParameterSafe(cmdHeader, "@TCS_AMT", headerObj.NumTCSPer2);
                            AddParameterSafe(cmdHeader, "@ROUND_OFF", headerObj.NumRoundOff);
                            AddParameterSafe(cmdHeader, "@UUSER", globalVar.PubUserId);
                            AddParameterSafe(cmdHeader, "@EUSER", globalVar.PubUserId);
                            AddParameterSafe(cmdHeader, "@WSID", globalVar.PubWorkStationID);
                            AddParameterSafe(cmdHeader, "@LIP", globalVar.PubLocalId);
                            AddParameterSafe(cmdHeader, "@LID", Environment.MachineName);

                            AddParameterSafe(cmdHeader, "@Action", isInsert ? "Insert" : "Update");
                            await cmdHeader.ExecuteNonQueryAsync();
                        }

                        if (!isInsert)
                        {
                            string deleteQuery = @"DELETE FROM PURCHASE2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE
                                                   DELETE FROM IMG_TABLE WHERE V_NO=@V_NO AND V_TYPE=@V_TYPE AND COMP_CODE=@COMP_CODE AND YEAR_CODE=@YEAR_CODE AND BRANCH_CODE=@BRANCH_CODE
                                                   DELETE FROM PROD_BATCH WHERE V_NO=@V_NO AND V_TYPE=@V_TYPE AND COMP_CODE=@COMP_CODE AND YEAR_CODE=@YEAR_CODE AND BRANCH_CODE=@BRANCH_CODE";

                            using (var cmdDelete = new SqlCommand(deleteQuery, con, transaction))
                            {
                                cmdDelete.Parameters.AddWithValue("@V_NO", vNo);
                                cmdDelete.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                                cmdDelete.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                cmdDelete.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                cmdDelete.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                await cmdDelete.ExecuteNonQueryAsync();
                            }
                        }

                        int serialNo = 1;
                        foreach (var item in ItemDetails)
                        {
                            using (var cmdItem = new SqlCommand("InsertPurchaseItemDetail", con, transaction))
                            {
                                cmdItem.CommandType = CommandType.StoredProcedure;

                                AddParameterSafe(cmdItem, "@V_NO", vNo);
                                AddParameterSafe(cmdItem, "@DOC_ID", DOC_ID);
                                AddParameterSafe(cmdItem, "@V_TYPE", headerObj.DocType);
                                AddParameterSafe(cmdItem, "@V_DATE", DateTime.Parse(headerObj.DocDate));
                                AddParameterSafe(cmdItem, "@COMP_CODE", globalVar.PubCompCode);
                                AddParameterSafe(cmdItem, "@BRANCH_CODE", globalVar.PubBranchCode);
                                AddParameterSafe(cmdItem, "@YEAR_CODE", globalVar.PubFYearCode);
                                AddParameterSafe(cmdItem, "@SNO", serialNo++);
                                AddParameterSafe(cmdItem, "@ITEM_CODE", item.ItemCode);
                                AddParameterSafe(cmdItem, "@ITEM_NAME", item.ItemName);
                                AddParameterSafe(cmdItem, "@HSN_CODE", item.HSNCode);
                                AddParameterSafe(cmdItem, "@UOM_NAME", item.UOMName);
                                AddParameterSafe(cmdItem, "@UOM_CODE", item.UOMCode);
                                AddParameterSafe(cmdItem, "@NOS", item.Nos);
                                AddParameterSafe(cmdItem, "@PLUS_MINUSQTY", item.PlusMinusQty);
                                AddParameterSafe(cmdItem, "@RECD_QTY", item.RecQty);
                                AddParameterSafe(cmdItem, "@BILL_QTY", item.BillQty);
                                AddParameterSafe(cmdItem, "@USD_RATE", item.USDRate);
                                AddParameterSafe(cmdItem, "@EXCH_RATE", item.ExRate);
                                AddParameterSafe(cmdItem, "@RATE", item.Rate);
                                AddParameterSafe(cmdItem, "@AMOUNT", item.Amount);
                                AddParameterSafe(cmdItem, "@EMPTY_YN", item.EmptyYN);
                                AddParameterSafe(cmdItem, "@WB_QTY", item.WBQty);
                                AddParameterSafe(cmdItem, "@PACK_PER", item.PackPer);
                                AddParameterSafe(cmdItem, "@PACK_AMT", item.PackAmt);
                                AddParameterSafe(cmdItem, "@DISC_PER", item.DiscPer);
                                AddParameterSafe(cmdItem, "@DISC_AMT", item.DiscAmt);
                                AddParameterSafe(cmdItem, "@CGST_PER", item.CGSTPer);
                                AddParameterSafe(cmdItem, "@CGST_AMT", item.CGSTAmt);
                                AddParameterSafe(cmdItem, "@SGST_PER", item.SGSTPer);
                                AddParameterSafe(cmdItem, "@SGST_AMT", item.SGSTAmt);
                                AddParameterSafe(cmdItem, "@IGST_PER", item.IGSTPer);
                                AddParameterSafe(cmdItem, "@IGST_AMT", item.IGSTAmt);
                                AddParameterSafe(cmdItem, "@CESS_PER", item.CESSPer);
                                AddParameterSafe(cmdItem, "@CESS_AMT", item.CESSAmt);
                                AddParameterSafe(cmdItem, "@VAT_PER", item.VATPer);
                                AddParameterSafe(cmdItem, "@VAT_AMT", item.VATAmt);
                                AddParameterSafe(cmdItem, "@OTH_AMT", item.OthAmt);
                                AddParameterSafe(cmdItem, "@NET_AMT", item.NetAmt);
                                AddParameterSafe(cmdItem, "@LAND_RATE", item.LDRate);
                                AddParameterSafe(cmdItem, "@LAND_AMT", item.LDAmt);
                                AddParameterSafe(cmdItem, "@BIN_LOCATION", item.BinLocation);
                                AddParameterSafe(cmdItem, "@PO_TYPE", item.POType);
                                AddParameterSafe(cmdItem, "@PO_NO", item.PONo);
                                AddParameterSafe(cmdItem, "@KANTA_TYPE", item.KantaType);
                                AddParameterSafe(cmdItem, "@KANTA_NO", item.KantaNo);
                                AddParameterSafe(cmdItem, "@REQ_TYPE", item.ReqType);
                                AddParameterSafe(cmdItem, "@REQ_NO", item.ReqNo);
                                AddParameterSafe(cmdItem, "@GATE_TYPE", headerObj.DocType);
                                AddParameterSafe(cmdItem, "@GATE_NO", headerObj.GateNo);
                                AddParameterSafe(cmdItem, "@BIN_CODE", item.BinCode);
                                AddParameterSafe(cmdItem, "@MAKE_CODE", item.MakeCode);
                                AddParameterSafe(cmdItem, "@TAX_CODE", item.TaxCode);
                                AddParameterSafe(cmdItem, "@DEPT_CODE", item.DeptCode);
                                AddParameterSafe(cmdItem, "@REMARKS", item.Remarks);
                                AddParameterSafe(cmdItem, "@UUSER", globalVar.PubUserId);
                                AddParameterSafe(cmdItem, "@UDATE", DateTime.Now);
                                AddParameterSafe(cmdItem, "@AED", "A");
                                AddParameterSafe(cmdItem, "@WSID", globalVar.PubWorkStationID);
                                AddParameterSafe(cmdItem, "@LIP", globalVar.PubLocalId);
                                AddParameterSafe(cmdItem, "@LID", Environment.MachineName);
                                AddParameterSafe(cmdItem, "@Action", "Insert");

                                await cmdItem.ExecuteNonQueryAsync();
                            }

                            //====================== Insert Into PROD_BATCH ======================
                            if (headerObj.DocType == "RCPT" || headerObj.DocType == "RCPI")
                            {
                                using (var cmdBatch = new SqlCommand(@"
                                    INSERT INTO PROD_BATCH
                                    ( COMP_CODE, BRANCH_CODE, YEAR_CODE, V_TYPE, V_NO, V_DATE, BATCH_NO, BAG_NO, ITEM_CODE, GROSS_QTY, QTY, REMARKS, SNO,
                                      UUSER, UDATE, AED, WSID, LIP, LID )

                                    VALUES
                                    ( @COMP_CODE, @BRANCH_CODE, @YEAR_CODE, @V_TYPE, @V_NO, @V_DATE, @BATCH_NO, @BAG_NO, @ITEM_CODE, @GROSS_QTY, @QTY,
                                      @REMARKS, @SNO, @UUSER, GETDATE(), @AED, @WSID, @LIP, @LID )", con, transaction))
                                {
                                    AddParameterSafe(cmdBatch, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdBatch, "@BRANCH_CODE", globalVar.PubBranchCode);
                                    AddParameterSafe(cmdBatch, "@YEAR_CODE", globalVar.PubFYearCode);

                                    AddParameterSafe(cmdBatch, "@V_TYPE", headerObj.DocType);
                                    AddParameterSafe(cmdBatch, "@V_NO", vNo);
                                    AddParameterSafe(cmdBatch, "@V_DATE", DateTime.Parse(headerObj.DocDate));

                                    AddParameterSafe(cmdBatch, "@BATCH_NO", vNo);
                                    AddParameterSafe(cmdBatch, "@BAG_NO", $"{vNo}{serialNo - 1}");

                                    AddParameterSafe(cmdBatch, "@ITEM_CODE", item.ItemCode);
                                    AddParameterSafe(cmdBatch, "@GROSS_QTY", item.RecQty);
                                    AddParameterSafe(cmdBatch, "@QTY", item.RecQty);

                                    AddParameterSafe(cmdBatch, "@REMARKS", item.Remarks);
                                    AddParameterSafe(cmdBatch, "@SNO", serialNo - 1);

                                    AddParameterSafe(cmdBatch, "@UUSER", globalVar.PubUserId);
                                    AddParameterSafe(cmdBatch, "@AED", isInsert ? "A" : "E");
                                    AddParameterSafe(cmdBatch, "@WSID", globalVar.PubWorkStationID);
                                    AddParameterSafe(cmdBatch, "@LIP", globalVar.PubLocalId);
                                    AddParameterSafe(cmdBatch, "@LID", Environment.MachineName);

                                    await cmdBatch.ExecuteNonQueryAsync();
                                }
                            }
                            //====================== End PROD_BATCH ======================

                        }

                        //=================For Image===========================
                        int rowId = 1;

                        if (Attachments != null && Attachments.Any())
                        {
                            foreach (var attachment in Attachments)
                            {
                                byte[] fileBytes;
                                string fileName;
                                string fileType;

                                if (attachment.File != null && attachment.File.Length > 0)
                                {
                                    // New uploaded image
                                    using (var ms = new MemoryStream())
                                    {
                                        await attachment.File.CopyToAsync(ms);
                                        fileBytes = ms.ToArray();
                                    }

                                    fileName = attachment.File.FileName;
                                    fileType = Path.GetExtension(attachment.File.FileName);
                                }
                                else if (attachment.IMG_FILE != null && attachment.IMG_FILE.Length > 0)
                                {
                                    // Existing image
                                    fileBytes = Convert.FromBase64String(attachment.IMG_FILE);
                                    fileName = attachment.FILE_NAME;
                                    fileType = attachment.FILE_TYPE;
                                }
                                else
                                {
                                    continue;
                                }

                                using (var cmdImage = new SqlCommand("InsertPurchaseReceiptHeader", con, transaction))
                                {
                                    cmdImage.CommandType = CommandType.StoredProcedure;

                                    AddParameterSafe(cmdImage, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdImage, "@BRANCH_CODE", globalVar.PubBranchCode);
                                    AddParameterSafe(cmdImage, "@YEAR_CODE", globalVar.PubFYearCode);

                                    AddParameterSafe(cmdImage, "@DOC_ID", DOC_ID);
                                    AddParameterSafe(cmdImage, "@V_NO", vNo);
                                    AddParameterSafe(cmdImage, "@V_TYPE", headerObj.DocType);
                                    AddParameterSafe(cmdImage, "@V_DATE", DateTime.Parse(headerObj.DocDate));

                                    AddParameterSafe(cmdImage, "@ROWID", rowId++);
                                    AddParameterSafe(cmdImage, "@IMG_FILE", fileBytes);
                                    AddParameterSafe(cmdImage, "@FILE_NAME", fileName);
                                    AddParameterSafe(cmdImage, "@FILE_TYPE", fileType);

                                    AddParameterSafe(cmdImage, "@UUSER", globalVar.PubUserId);
                                    AddParameterSafe(cmdImage, "@WSID", globalVar.PubWorkStationID);
                                    AddParameterSafe(cmdImage, "@LIP", globalVar.PubLocalId);
                                    AddParameterSafe(cmdImage, "@LID", Environment.MachineName);

                                    AddParameterSafe(cmdImage, "@Action", "ImageInsert");

                                    await cmdImage.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        //==========Both are commented in  old code ==================

                        //========Update Gate1 ==============
                        //using (var cmdGate = new SqlCommand(@"UPDATE GATE1 SET BILL_NO = @BILL_NO, BILL_DATE = @BILL_DATE, MRN_TYPE = @MRN_TYPE, MRN_NO = @MRN_NO
                        //                                   WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE", con, transaction))
                        //{
                        //    AddParameterSafe(cmdGate, "@BILL_NO", headerObj.BillNo);
                        //    AddParameterSafe(cmdGate, "@BILL_DATE", string.IsNullOrWhiteSpace(headerObj.BillDate) ? (object)DBNull.Value : DateTime.Parse(headerObj.BillDate));

                        //    AddParameterSafe(cmdGate, "@V_TYPE", headerObj.GATE_TYPE);
                        //    AddParameterSafe(cmdGate, "@V_NO", headerObj.GateNo);

                        //    AddParameterSafe(cmdGate, "@MRN_TYPE", headerObj.DocType);
                        //    AddParameterSafe(cmdGate, "@MRN_NO", vNo);

                        //    AddParameterSafe(cmdGate, "@COMP_CODE", globalVar.PubCompCode);
                        //    AddParameterSafe(cmdGate, "@BRANCH_CODE", globalVar.PubBranchCode);
                        //    AddParameterSafe(cmdGate, "@YEAR_CODE", globalVar.PubFYearCode);

                        //    await cmdGate.ExecuteNonQueryAsync();
                        //}

                        //========== Update Qc1 ============
                        //using (var cmdQC = new SqlCommand(@"UPDATE QC1 SET BILL_NO = @BILL_NO, BILL_DATE = @BILL_DATE, CONTAINER_NO = @CONTAINER_NO WHERE MRN_TYPE = @MRN_TYPE
                        //                                 AND MRN_NO = @MRN_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE", con, transaction))
                        //{
                        //    AddParameterSafe(cmdQC, "@BILL_NO", headerObj.BillNo);
                        //    AddParameterSafe(cmdQC, "@BILL_DATE", string.IsNullOrWhiteSpace(headerObj.BillDate) ? (object)DBNull.Value : DateTime.Parse(headerObj.BillDate));

                        //    AddParameterSafe(cmdQC, "@CONTAINER_NO", headerObj.ContainerNo);

                        //    AddParameterSafe(cmdQC, "@MRN_TYPE", headerObj.DocType);
                        //    AddParameterSafe(cmdQC, "@MRN_NO", vNo);

                        //    AddParameterSafe(cmdQC, "@COMP_CODE", globalVar.PubCompCode);
                        //    AddParameterSafe(cmdQC, "@BRANCH_CODE", globalVar.PubBranchCode);
                        //    AddParameterSafe(cmdQC, "@YEAR_CODE", globalVar.PubFYearCode);

                        //    await cmdQC.ExecuteNonQueryAsync();
                        //}

                        transaction.Commit();

                        foreach (var item in ItemDetails)
                        {
                            if (item.ReqNo > 0)
                            {
                                //==================== Update PREQUEST2 ====================
                                using (var cmdReq = new SqlCommand(@"
                                  UPDATE PREQUEST2
                                   SET Adj_Qty = @Adj_Qty,
                                   Status = 3,
                                   PO_TYPE = IIF(ISNULL(PO_TYPE,'')='', @PO_TYPE, PO_TYPE),
                                   PO_NO = IIF(ISNULL(PO_NO,0)=0, @PO_NO, PO_NO),
                                   MRN_TYPE = @MRN_TYPE,
                                   MRN_NO = @MRN_NO
                                  WHERE V_TYPE = 'STPI'
                                   AND V_NO = @REQ_NO
                                   AND ITEM_CODE = @ITEM_CODE
                                   AND COMP_CODE = @COMP_CODE
                                   AND BRANCH_CODE = @BRANCH_CODE", con))
                                {
                                    AddParameterSafe(cmdReq, "@Adj_Qty", item.RecQty);
                                    AddParameterSafe(cmdReq, "@PO_TYPE", item.POType);
                                    AddParameterSafe(cmdReq, "@PO_NO", item.PONo);
                                    AddParameterSafe(cmdReq, "@MRN_TYPE", headerObj.DocType);
                                    AddParameterSafe(cmdReq, "@MRN_NO", vNo);

                                    AddParameterSafe(cmdReq, "@REQ_NO", item.ReqNo);
                                    AddParameterSafe(cmdReq, "@ITEM_CODE", item.ItemCode);

                                    AddParameterSafe(cmdReq, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdReq, "@BRANCH_CODE", globalVar.PubBranchCode);

                                    await cmdReq.ExecuteNonQueryAsync();
                                }

                                //==================== Check Pending ====================
                                using (var cmdCheck = new SqlCommand(@"
                                    SELECT COUNT(*)
                                    FROM PREQUEST2
                                    WHERE STATUS = 1
                                      AND V_TYPE = 'STPI'
                                      AND V_NO = @REQ_NO
                                      AND COMP_CODE = @COMP_CODE
                                      AND BRANCH_CODE = @BRANCH_CODE
                                      AND YEAR_CODE = @YEAR_CODE", con))
                                {
                                    AddParameterSafe(cmdCheck, "@REQ_NO", item.ReqNo);
                                    AddParameterSafe(cmdCheck, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdCheck, "@BRANCH_CODE", globalVar.PubBranchCode);
                                    AddParameterSafe(cmdCheck, "@YEAR_CODE", globalVar.PubFYearCode);

                                    int pending = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());

                                    if (pending == 0)
                                    {
                                        using (var cmdReq1 = new SqlCommand(@"
                                        UPDATE PREQUEST1
                                        SET STATUS = 3
                                        WHERE V_TYPE = 'STPI'
                                          AND V_NO = @REQ_NO
                                          AND COMP_CODE = @COMP_CODE
                                          AND BRANCH_CODE = @BRANCH_CODE", con))
                                        {
                                            AddParameterSafe(cmdReq1, "@REQ_NO", item.ReqNo);
                                            AddParameterSafe(cmdReq1, "@COMP_CODE", globalVar.PubCompCode);
                                            AddParameterSafe(cmdReq1, "@BRANCH_CODE", globalVar.PubBranchCode);
                                            await cmdReq1.ExecuteNonQueryAsync();
                                        }
                                    }
                                }
                            }
                        }
                        return (true, "Purchase Receipt saved successfully.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return (false, ex.Message);
                    }
                }

            }

        }

        public async Task<( bool Success,string Message, string WBType, int WBNo, Dictionary<string, object>? Header, List<Dictionary<string, object>>? Items)> GetGatDetailsList(string StrVNo, string StrV_type)
        {

            var gv = _globalVariableService.GetGlobalVariables();

            string wbType = "";
            int wbNo = 0;

            Dictionary<string, object>? header = null;
            List<Dictionary<string, object>> items = new List<Dictionary<string, object>>();

            try
            {
                if (!int.TryParse(StrVNo, out int gateNo))
                {
                    return (false, "Invalid Gate No", "", 0, null, null);
                }

                string gateType = StrV_type.Substring(0, 4);

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    // ===========================
                    // 1. WB Query
                    // ===========================

                    string qry = @"
                    SELECT V_TYPE,V_NO
                    FROM WB1
                    WHERE GATE_TYPE=@GateType
                    AND GATE_NO=@GateNo
                    AND COMP_CODE=@CompCode
                    AND BRANCH_CODE=@BranchCode
                    AND YEAR_CODE=@YearCode";

                    using (SqlCommand cmd = new SqlCommand(qry, con))
                    {
                        cmd.Parameters.AddWithValue("@GateType", gateType);
                        cmd.Parameters.AddWithValue("@GateNo", gateNo);
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

                        using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                        {
                            if (await dr.ReadAsync())
                            {
                                wbType = dr["V_TYPE"].ToString();
                                wbNo = Convert.ToInt32(dr["V_NO"]);
                            }
                        }
                    }

                    // ===========================
                    // 2. GATE1 Header Query
                    // ===========================

                    string qry1 = @"
                        SELECT
                        a.BILL_NO,
                        a.BILL_DATE,
                        a.CHALL_NO,
                        a.CHALL_DATE,
                        a.WAYBILL_NO,
                        a.TRANSIT_NO,
                        a.PARTY_CODE,
                        a.SHIP_PARTY,
                        b.NAME AS PartyName,
                        a.ADD1,
                        a.ADD2,
                        a.ADD3,
                        sa.ADDRESS_ID AS PARTY_ADDRESSID,
                        a.PARTY_CITY AS CITY_CODE,
                        e.NAME AS CITY,
                        f.CODE AS StateCode,
                        f.NAME AS State,
                        a.PARTY_GST AS GSTIN,
                        a.PARTY_PINCODE,
                        ISNULL(a.TRANSPORT_CODE,0) TRANSPORT_CODE,
                        d.NAME AS Transport,
                        a.TRUCK_NO,
                        a.REMARKS,
                        a.EWB_DATE,
                        a.EWB_EXPDATE,
                        a.EWB_INVNO,
                        a.GR_NO,
                        a.GR_DATE
                        FROM GATE1 a
                        LEFT JOIN SUBGROUP_MAST b
                        ON a.PARTY_CODE=b.CODE
                        AND b.COMP_CODE=@CompCode
                         
                        LEFT JOIN SUBGROUP_ADDRESS sa
                        ON sa.CODE = a.PARTY_CODE
                        AND sa.COMP_CODE = @CompCode
                        AND ISNULL(sa.ADD1,'') = ISNULL(a.ADD1,'')
                        AND ISNULL(sa.ADD2,'') = ISNULL(a.ADD2,'')
                        AND ISNULL(sa.ADD3,'') = ISNULL(a.ADD3,'')
                        
                        LEFT JOIN TRANSPORT_MAST d
                        ON a.TRANSPORT_CODE=d.CODE
                        AND d.COMP_CODE=@CompCode
                        
                        LEFT JOIN CITY_MAST e
                        ON a.PARTY_CITY=e.CODE
                                                                                                   
                        LEFT JOIN STATE_MAST f
                        ON e.STATE_CODE=f.CODE
                        
                        WHERE
                        a.V_TYPE=@GateType
                        AND a.V_NO=@GateNo
                        AND a.COMP_CODE=@CompCode
                        AND a.BRANCH_CODE=@BranchCode
                        AND a.YEAR_CODE=@YearCode";

                    using (SqlCommand cmd = new SqlCommand(qry1, con))
                    {
                        cmd.Parameters.AddWithValue("@GateType", gateType);
                        cmd.Parameters.AddWithValue("@GateNo", gateNo);
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

                        using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                        {
                            if (await dr.ReadAsync())
                            {
                                header = new Dictionary<string, object>();

                                for (int i = 0; i < dr.FieldCount; i++)
                                {
                                    header.Add(
                                        dr.GetName(i),
                                        dr.IsDBNull(i) ? null : dr.GetValue(i)
                                    );
                                }
                            }
                        }
                    }

                    //===========================
                    // Container List
                    //===========================

                    if (header != null)
                    {
                        int partyCode = Convert.ToInt32(header["PARTY_CODE"]);
                        string billNo = header["BILL_NO"]?.ToString() ?? "";

                        if (partyCode > 0 && !string.IsNullOrWhiteSpace(billNo))
                        {
                            header["ContainerList"] = await GetContainerList(con, partyCode, billNo);
                        }
                    }

                    // ===========================
                    // 3. Item Query
                    // ===========================

                    string qry2 = "";

                    if (gateType == "INJB" && wbNo > 0)
                    {
                        qry2 = @"
                            Select
                                @GateType v_type,
                                @GateNo v_no,
                                a.ITEM_CODE,
                                b.NAME ITEM_NAME,
                                ISNULL(b.UNIT_NAME,'') Unit,
                                b.HSN_CODE,
                                0 NOS,
                                a.NET_WGT QTY,
                                0 RATE,
                                '' EMPTY,
                                0 PACK_PER,
                                0 DISC_PER,
                                '' TaxType,
                                0 CGST_PER,
                                0 SGST_PER,
                                0 IGST_PER,
                                0 OTH_AMT,
                                a.REF_TYPE,
                                a.REF_NO,
                                '' REQUEST_TYPE,
                                0 REQUEST_NO,
                                '' Make,
                                '' Department,
                                0 DEPT_CODE,
                                0 TAX_CODE,
                                0 MAKE_CODE,
                                b.UNIT_CODE UOM_CODE
                            from WB2 a
                            left join ITEM_MAST b
                                on a.ITEM_CODE=b.CODE
                                and b.COMP_CODE=a.COMP_CODE
                            where
                                ISNULL(a.ITEM_CODE,0)>0
                                and a.V_TYPE=@WBType
                                and a.V_NO=@WBNo
                                and a.COMP_CODE=@CompCode
                                and a.BRANCH_CODE=@BranchCode
                                and a.YEAR_CODE=@YearCode
                            order by a.SNO";
                    }
                    else
                    {
                        qry2 = @"
                            select
                                a.v_type,
                                a.v_no,
                                a.ITEM_CODE,
                                c.NAME ITEM_NAME,
                                ISNULL(a.UOM_NAME,b.NAME) Unit,
                                c.HSN_CODE,
                                a.NOS,
                                a.QTY,
                                d.RATE,
                                a.EMPTY,
                                d.PACK_PER,
                                d.PACK_AMT,
                                d.DISC_PER,
                                d.DISC_AMT,
                                e.NAME TaxType,
                                d.CGST_PER,
                                d.SGST_PER,
                                d.IGST_PER,
                                d.CESS_PER,
                                d.CESS_AMT,
                                d.OTH_AMT,
                                a.REF_TYPE,
                                a.REF_NO,
                                d.REQUEST_TYPE,
                                d.REQUEST_NO,
                                g.NAME Make,
                                f.NAME Department,
                                d.DEPT_CODE,
                                d.TAX_CODE,
                                d.MAKE_CODE,
                                a.UOM_CODE
                            from GATE2 a
                            left join ITEMUNIT_MAST b
                                on a.UOM_CODE=b.CODE
                                and b.COMP_CODE=@CompCode
                            left join ITEM_MAST c
                                on a.ITEM_CODE=c.CODE
                                and c.COMP_CODE=@CompCode
                            left join ORDER2 d
                                on a.REF_TYPE=d.V_TYPE
                                and a.REF_NO=d.V_NO
                                and a.ITEM_CODE=d.ITEM_CODE
                                and d.COMP_CODE=@CompCode
                                and d.BRANCH_CODE=@BranchCode
                            left join TAX_MAST e
                                on d.TAX_CODE=e.CODE
                            left join ITEMDEPT_MAST f
                                on d.DEPT_CODE=f.CODE
                                and f.COMP_CODE=@CompCode
                            left join ITEMMAKE_MAST g
                                on d.MAKE_CODE=g.CODE
                                and g.COMP_CODE=@CompCode
                            where
                                a.V_TYPE=@GateType
                                and a.V_NO=@GateNo
                                and a.COMP_CODE=@CompCode
                                and a.BRANCH_CODE=@BranchCode
                         order by a.SNO";
                    }
                    using (SqlCommand cmd = new SqlCommand(qry2, con))
                    {
                        cmd.Parameters.AddWithValue("@GateType", gateType);
                        cmd.Parameters.AddWithValue("@GateNo", gateNo);

                        cmd.Parameters.AddWithValue("@WBType", wbType);
                        cmd.Parameters.AddWithValue("@WBNo", wbNo);

                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

                        List<Dictionary<string, object>> tempItems = new();

                        using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                        {
                            while (await dr.ReadAsync())
                            {
                                Dictionary<string, object> row = new();

                                for (int i = 0; i < dr.FieldCount; i++)
                                {
                                    row.Add(
                                        dr.GetName(i),
                                        dr.IsDBNull(i) ? null : dr.GetValue(i)
                                    );
                                }

                                tempItems.Add(row);
                            }
                        }

                        foreach (var row in tempItems)
                        {
                            int itemCode = Convert.ToInt32(row["ITEM_CODE"]);

                            decimal recQty = await GetRecQty(
                                con,
                                itemCode,
                                gateType,
                                gateNo,
                                wbType
                            );

                            decimal wbQty = 0;

                            if (wbNo > 0)
                            {
                                wbQty = await GetWBQty(
                                    con,
                                    itemCode,
                                    gateType,
                                    gateNo,
                                    wbType
                                );
                            }

                            row["RecQty"] = recQty;
                            row["WBQty"] = wbQty;
                            row["KantaType"] = wbType;
                            row["KantaNo"] = wbNo;

                            await FillDepartmentFromWB(con, row, itemCode, wbType, wbNo);
                            row["WB_YN"] = await GetWBYN(con, itemCode);
                            await FillTCSAndPaymentDetails(con, row, gateType, gateNo);

                            items.Add(row);
                        }
                    }

                }

                return (true,"Gate details fetched successfully.",wbType, wbNo,header,items );
            }
            catch (Exception ex)
            {
                return ( false,ex.Message,"", 0,null,null );
            }
        }
        
        //=========GET REC & WN QTY Method==========
        private async Task<decimal> GetRecQty(SqlConnection con, int itemCode, string gateType, int gateNo, string wbType)
        {
            string query;
            var gv = _globalVariableService.GetGlobalVariables();
            if (wbType == "KSIN" || wbType == "KSOT")
            {
                query = @"
                SELECT ISNULL(SUM(a.NET_WGT),0)
                FROM WB2 a
                INNER JOIN WB1 b
                   ON a.V_NO = b.V_NO
                   AND a.V_TYPE = b.V_TYPE
                   AND a.COMP_CODE = b.COMP_CODE
                   AND a.BRANCH_CODE = b.BRANCH_CODE
                   AND a.YEAR_CODE = b.YEAR_CODE
                   WHERE
                    a.ITEM_CODE=@ItemCode
                    AND b.GATE_TYPE=@GateType
                    AND b.GATE_NO=@GateNo
                    AND a.COMP_CODE=@CompCode
                    AND a.BRANCH_CODE=@BranchCode
                    AND a.YEAR_CODE=@YearCode";
            }
            else
            {
                query = @"
                SELECT ISNULL(SUM(a.NET_WGT),0)
                FROM WB2 a
                INNER JOIN WB1 b
                    ON a.V_NO = b.V_NO
                   AND a.V_TYPE = b.V_TYPE
                   AND a.COMP_CODE = b.COMP_CODE
                   AND a.BRANCH_CODE = b.BRANCH_CODE
                   AND a.YEAR_CODE = b.YEAR_CODE
                WHERE
                    b.STATUS=3
                    AND a.ITEM_CODE=@ItemCode
                    AND b.GATE_TYPE=@GateType
                    AND b.GATE_NO=@GateNo
                    AND a.COMP_CODE=@CompCode
                    AND a.BRANCH_CODE=@BranchCode
                    AND a.YEAR_CODE=@YearCode";
            }

            using SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@ItemCode", itemCode);
            cmd.Parameters.AddWithValue("@GateType", gateType);
            cmd.Parameters.AddWithValue("@GateNo", gateNo);
            cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
            cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

            object result = await cmd.ExecuteScalarAsync();

            return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
        }

        private async Task<decimal> GetWBQty(SqlConnection con, int itemCode, string gateType, int gateNo, string kantaType)
        {
            string query;
            var gv = _globalVariableService.GetGlobalVariables();
            if (kantaType == "KSIN" || kantaType == "KSOT")
            {
                query = @"
                SELECT ISNULL(SUM(b.NET_WGT),0)
                FROM WB1 a
                INNER JOIN WB2 b
                   ON a.V_TYPE=b.V_TYPE
                   AND a.V_NO=b.V_NO
                   AND a.COMP_CODE=b.COMP_CODE
                   AND a.BRANCH_CODE=b.BRANCH_CODE
                   AND a.YEAR_CODE=b.YEAR_CODE
                 WHERE
                    b.ITEM_CODE=@ItemCode
                    AND a.GATE_TYPE=@GateType
                    AND a.GATE_NO=@GateNo
                    AND a.COMP_CODE=@CompCode
                    AND a.BRANCH_CODE=@BranchCode
                    AND a.YEAR_CODE=@YearCode";
            }
            else
            {
                query = @"
                SELECT ISNULL(SUM(b.NET_WGT),0)
                FROM WB1 a
                INNER JOIN WB2 b
                    ON a.V_TYPE=b.V_TYPE
                    AND a.V_NO=b.V_NO
                    AND a.COMP_CODE=b.COMP_CODE
                    AND a.BRANCH_CODE=b.BRANCH_CODE
                    AND a.YEAR_CODE=b.YEAR_CODE
                WHERE
                    a.STATUS=3
                    AND b.ITEM_CODE=@ItemCode
                    AND a.GATE_TYPE=@GateType
                    AND a.GATE_NO=@GateNo
                    AND a.COMP_CODE=@CompCode
                    AND a.BRANCH_CODE=@BranchCode
                    AND a.YEAR_CODE=@YearCode";
            }

            using SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@ItemCode", itemCode);
            cmd.Parameters.AddWithValue("@GateType", gateType);
            cmd.Parameters.AddWithValue("@GateNo", gateNo);
            cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
            cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

            object result = await cmd.ExecuteScalarAsync();

            return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
        }

        //=========TCS & Payment Block ================
        private async Task FillTCSAndPaymentDetails(SqlConnection con, Dictionary<string, object> row, string gateType, int gateNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string poType = row["REF_TYPE"]?.ToString() ?? "";
            int poNo = row["REF_NO"] == null ? 0 : Convert.ToInt32(row["REF_NO"]);

            if (poNo > 0)
            {
                string tcsQry = @"
                SELECT TOP 1 TCS_PER
                FROM ORDER1
                WHERE COMP_CODE=@CompCode
                    AND CONCAT(V_TYPE,V_NO) IN
                    (
                        SELECT CONCAT(REF_TYPE,REF_NO)
                        FROM GATE2 a
                        LEFT JOIN DOCTYPE_MAST b
                            ON a.REF_TYPE=b.CODE
                        WHERE b.DOCTYPE='Purchaseorder'
                        AND a.COMP_CODE=@CompCode
                        AND a.V_TYPE=@GateType
                        AND a.V_NO=@GateNo
                    )";

                using (SqlCommand cmd = new SqlCommand(tcsQry, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@GateType", gateType);
                    cmd.Parameters.AddWithValue("@GateNo", gateNo);

                    object result = await cmd.ExecuteScalarAsync();

                    row["TCS_PER"] = result == DBNull.Value || result == null
                        ? 0
                        : Convert.ToDecimal(result);
                }

                string paymentQry;

                if (poType != "PAUD")
                {
                    paymentQry = @"
                    SELECT HOLD_PAY
                    FROM SAUDA
                    WHERE CONCAT(V_TYPE,V_NO)=
                    (
                        SELECT TOP 1 CONCAT(SAUDA_TYPE,SAUDA_NO)
                        FROM ORDER2
                        WHERE V_TYPE=@POType
                          AND V_NO=@PONo
                          AND COMP_CODE=@CompCode
                          AND BRANCH_CODE=@BranchCode
                    )
                    AND COMP_CODE=@CompCode
                    AND BRANCH_CODE=@BranchCode";
                }
                else
                {
                    paymentQry = @"
                    SELECT HOLD_PAY
                    FROM SAUDA
                    WHERE V_TYPE=@POType
                    AND V_NO=@PONo
                    AND COMP_CODE=@CompCode
                    AND BRANCH_CODE=@BranchCode";
                }

                using (SqlCommand cmd = new SqlCommand(paymentQry, con))
                {
                    cmd.Parameters.AddWithValue("@POType", poType);
                    cmd.Parameters.AddWithValue("@PONo", poNo);
                    cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);

                    object result = await cmd.ExecuteScalarAsync();

                    row["Payment"] = result?.ToString() ?? "";
                }

                row["IsHold"] = row["Payment"]?.ToString() == "HOLD";
            }

        }

        //==================Container Method============
        private async Task<List<string>> GetContainerList(SqlConnection con, int partyCode, string billNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            List<string> containers = new();

            string contQry = @"
            SELECT DISTINCT CONTAINER_NO
            FROM
            (
                SELECT CONTAINER_NO
                FROM ORDER4
                WHERE PARTY_CODE=@PartyCode
                  AND INV_NO=@BillNo
                  AND COMP_CODE=@CompCode
                  AND BRANCH_CODE=@BranchCode

                UNION ALL

                SELECT b.CONTAINER_NO
                FROM EXIM1 a
                LEFT JOIN EXIM2 b
                     ON a.V_TYPE=b.V_TYPE
                    AND a.V_NO=b.V_NO
                    AND a.COMP_CODE=b.COMP_CODE
                    AND a.BRANCH_CODE=b.BRANCH_CODE
                    AND a.YEAR_CODE=b.YEAR_CODE

                WHERE a.SUPPLIER=@PartyCode
                  AND a.SUPPLIER_INVNO=@BillNo
                  AND a.COMP_CODE=@CompCode
                  AND a.BRANCH_CODE=@BranchCode
            ) x
            WHERE ISNULL(CONTAINER_NO,'') <> ''";

            using SqlCommand cmd = new(contQry, con);

            cmd.Parameters.AddWithValue("@PartyCode", partyCode);
            cmd.Parameters.AddWithValue("@BillNo", billNo);
            cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);

            using SqlDataReader dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                containers.Add(dr["CONTAINER_NO"].ToString());
            }

            return containers;
        }

        //===========Department Override method==================
        private async Task FillDepartmentFromWB(SqlConnection con, Dictionary<string, object> row, int itemCode, string kantaType, int kantaNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            if (string.IsNullOrWhiteSpace(kantaType) || kantaNo <= 0)
                return;

            string qry = @"
            SELECT TOP 1
                TO_PLACE,
                TO_NAME
            FROM WB2
            WHERE ITEM_CODE=@ItemCode
              AND V_TYPE=@VType
              AND V_NO=@VNo
              AND COMP_CODE=@CompCode
              AND BRANCH_CODE=@BranchCode
              AND YEAR_CODE=@YearCode";

            using SqlCommand cmd = new(qry, con);

            cmd.Parameters.AddWithValue("@ItemCode", itemCode);
            cmd.Parameters.AddWithValue("@VType", kantaType);
            cmd.Parameters.AddWithValue("@VNo", kantaNo);
            cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
            cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

            using SqlDataReader dr = await cmd.ExecuteReaderAsync();

            if (await dr.ReadAsync())
            {
                row["DEPT_CODE"] = dr["TO_PLACE"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TO_PLACE"]);
                row["Department"] = dr["TO_NAME"]?.ToString() ?? "";
            }
        }

        //============Item Check(WB_YN)==================
        private async Task<string> GetWBYN(SqlConnection con, int itemCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = @"
            SELECT ISNULL(WB_YN,'')
            FROM ITEM_MAST
            WHERE CODE=@ItemCode
            AND COMP_CODE=@CompCode";

            using SqlCommand cmd = new(qry, con);

            cmd.Parameters.AddWithValue("@ItemCode", itemCode);
            cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);

            object result = await cmd.ExecuteScalarAsync();

            return result?.ToString() ?? "";
        }

        public async Task<(bool Success, string Message, PurchaseAllDetailsResponse Data)> GetAllDatadetails(GetDetailsRequest request)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            PurchaseAllDetailsResponse response = new();

            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();
                using SqlCommand cmd = new("sp_GetPurchaseAllDetails", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@VNO", request.VNO);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@V_TYPE", request.vType);

                await con.OpenAsync();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                //--------------- PURCHASE1 -----------------

                while (await reader.ReadAsync())
                {
                    Purchase1List obj = new();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var prop = typeof(Purchase1List).GetProperty(reader.GetName(i));

                        if (prop != null && !reader.IsDBNull(i))
                        {
                            var value = reader.GetValue(i);
                            var converted = ChangeType(value, prop.PropertyType);
                            prop.SetValue(obj, converted);
                        }
                    }

                    response.Purchase1.Add(obj);
                }

                //---------------- PURCHASE2 ----------------

                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Purchase2List obj = new();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var prop = typeof(Purchase2List).GetProperty(reader.GetName(i));

                            if (prop != null && !reader.IsDBNull(i))
                            {
                                var value = reader.GetValue(i);
                                var converted = ChangeType(value, prop.PropertyType);
                                prop.SetValue(obj, converted);
                            }
                        }

                        response.Purchase2.Add(obj);
                    }
                }

                //---------------- PURCHASE3 ----------------

                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Purchase3List obj = new();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);

                            var prop = typeof(Purchase3List).GetProperty(columnName);

                            if (prop == null || reader.IsDBNull(i))
                                continue;

                            if (columnName == "IMG_FILE")
                            {
                                prop.SetValue(obj, (byte[])reader["IMG_FILE"]);
                            }
                            else
                            {
                                var value = reader.GetValue(i);
                                var converted = ChangeType(value, prop.PropertyType);
                                prop.SetValue(obj, converted);
                            }
                        }

                        response.Purchase3.Add(obj);
                    }
                }

                return (true, "Data fetched successfully.", response);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        private static void AddParameterSafe(SqlCommand cmd, string paramName, object value)
        {
            try
            {
                cmd.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message} | Parameter: {paramName}", ex);
            }
        }

        private object ChangeType(object value, Type targetType)
        {
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null || value == DBNull.Value) return null;
                targetType = Nullable.GetUnderlyingType(targetType);
            }
            if (targetType.IsEnum)
            {
                return Enum.ToObject(targetType, value);
            }

            return Convert.ChangeType(value, targetType);
        }

        //=================Validation Method ====================
        private async Task<(bool IsValid, string Message)> ValidatePurchaseReceiptAsync(PurchaseReceiptHeaderModel headerObj, List<ItemDetailModel> ItemDetails, bool isInsert, string vNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var generalSetting = await _globalVariableService.LoadGeneralSetting();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                // ============================
                // Bill From GST Validation
                // ============================
                if (!string.IsNullOrWhiteSpace(headerObj.GST))
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT 1
                    FROM SUBGROUP_ADDRESS
                    WHERE COMP_CODE=@CompCode
                      AND CODE=@Code
                      AND GSTIN=@GSTIN", con);

                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@Code", headerObj.BillFrom);
                    cmd.Parameters.AddWithValue("@GSTIN", headerObj.GST);

                    object obj = await cmd.ExecuteScalarAsync();

                    if (obj == null)
                        return (false, "Missmatch 'Bill from' GST No from Master Record.");
                }

                // ============================
                // Ship From GST Validation
                // ============================
                if (!string.IsNullOrWhiteSpace(headerObj.ShipGST))
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT 1
                    FROM SUBGROUP_ADDRESS
                    WHERE COMP_CODE=@CompCode
                      AND CODE=@Code
                      AND GSTIN=@GSTIN", con);

                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@Code", headerObj.ShipFrom);
                    cmd.Parameters.AddWithValue("@GSTIN", headerObj.ShipGST);

                    object obj = await cmd.ExecuteScalarAsync();

                    if (obj == null)
                        return (false, "Missmatch 'Ship from' GST No from Master Record.");
                }

                // ============================
                // Ship From vs Purchase Order Validation
                // ============================
                if (!string.IsNullOrWhiteSpace(headerObj.ShipFrom) && ItemDetails != null && ItemDetails.Any())
                {
                    var firstItem = ItemDetails.First();

                    SqlCommand cmd = new SqlCommand(@"
                    SELECT 1
                    FROM ORDER1
                    WHERE SHIP_FROM <> @SHIP_FROM
                      AND V_TYPE = @V_TYPE
                      AND V_NO = @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE", con);

                    cmd.Parameters.AddWithValue("@SHIP_FROM", headerObj.ShipFrom);
                    cmd.Parameters.AddWithValue("@V_TYPE", firstItem.POType);
                    cmd.Parameters.AddWithValue("@V_NO", firstItem.PONo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                    object obj = await cmd.ExecuteScalarAsync();

                    if (obj != null)
                    {
                        return (false, "Ship From not matched as per Purchase Order., Please Check");
                    }
                }

                // ============================
                // GST State Validation
                // ============================

                // Party State (Supplier/Bill From)
                SqlCommand stateCmd = new SqlCommand(@"
                SELECT STATE_CODE
                FROM SUBGROUP_MAST
                WHERE CODE = @CODE
                    AND COMP_CODE = @COMP_CODE", con);

                stateCmd.Parameters.AddWithValue("@CODE", headerObj.BillFrom);
                stateCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                object stateObj = await stateCmd.ExecuteScalarAsync();

                if (stateObj != null && stateObj != DBNull.Value)
                {
                    int partyStateCode = Convert.ToInt32(stateObj);

                    // Company State
                    SqlCommand compStateCmd = new SqlCommand(@"
                    SELECT CM.STATE_CODE
                    FROM COMP_MAST C
                    INNER JOIN CITY_MAST CM
                        ON C.CITY_CODE = CM.CODE
                    WHERE C.CODE = @COMP_CODE", con);

                    compStateCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                    object compStateObj = await compStateCmd.ExecuteScalarAsync();

                    if (compStateObj != null && compStateObj != DBNull.Value)
                    {
                        int companyStateCode = Convert.ToInt32(compStateObj);

                        string stateType = partyStateCode == companyStateCode
                            ? "Local"
                            : "Central/Other";

                        // Local Party → IGST not allowed
                        if (partyStateCode == companyStateCode && headerObj.NumIGST > 0)
                        {
                            return (false, $"IGST not applicable as per Party State type is {stateType}");
                        }

                        // Interstate Party → CGST + SGST not allowed
                        if (partyStateCode != companyStateCode &&
                            (headerObj.NumCGST + headerObj.NumSGST) > 0)
                        {
                            return (false, $"CGST/SGST not applicable as per Party State type is {stateType}");
                        }

                        // Both tax types together not allowed
                        if (headerObj.NumIGST > 0 &&
                            (headerObj.NumCGST + headerObj.NumSGST) > 0)
                        {
                            return (false, "CGST+SGST+IGST all three type tax not applicable.");
                        }
                    }
                }

                // Only while updating
                if (!isInsert)
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT CONCAT(V_TYPE, V_NO)
                    FROM QC1
                    WHERE V_TYPE = @V_TYPE
                      AND V_NO <> @V_NO
                      AND MRN_TYPE = @MRN_TYPE
                      AND MRN_NO = @MRN_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE", con);

                    cmd.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@MRN_TYPE", headerObj.DocType);
                    cmd.Parameters.AddWithValue("@MRN_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                    object obj = await cmd.ExecuteScalarAsync();

                    if (obj != null)
                    {
                        return (false, $"MRN No engaged in QC Entry Document no : {obj}, modification not allowed.");
                    }
                }

                // ============================
                // Purchase Bill Reference Validation
                // ============================
                if (!isInsert)
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT CONCAT(V_TYPE, V_NO)
                    FROM PURCHASE2
                    WHERE V_TYPE = @V_TYPE
                      AND V_NO <> @V_NO
                      AND REF_TYPE = @REF_TYPE
                      AND REF_NO = @REF_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE", con);

                    cmd.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@REF_TYPE", headerObj.DocType);
                    cmd.Parameters.AddWithValue("@REF_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                    object obj = await cmd.ExecuteScalarAsync();

                    if (obj != null)
                    {
                        return (false, $"MRN No engaged in Purchase bill entry Document no : {obj}, modification not allowed.");
                    }
                }

                // ============================
                // Duplicate Bill No Validation
                // ============================
                if (!string.IsNullOrWhiteSpace(headerObj.BillNo))
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT TOP 1
                           DOC_ID,
                           V_DATE
                    FROM PURCHASE1
                    WHERE PARTY_CODE = @PARTY_CODE
                      AND BILL_NO = @BILL_NO
                      AND V_TYPE IN ('SRPU','RCPT','BFRC')
                      AND V_NO <> @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE", con);

                    cmd.Parameters.AddWithValue("@PARTY_CODE", headerObj.BillFrom);
                    cmd.Parameters.AddWithValue("@BILL_NO", headerObj.BillNo);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string docId = reader["DOC_ID"].ToString();
                            string vDate = reader["V_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(reader["V_DATE"]).ToString("dd/MM/yyyy");

                            return (false,
                                $"Bill No {headerObj.BillNo} already exists in MRN, Serial No : {docId} dated : {vDate}");
                        }
                    }
                }

                // ============================
                // Duplicate Container No Validation
                // ============================
                if (!string.IsNullOrWhiteSpace(headerObj.ContainerNo))
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT TOP 1
                           DOC_ID,
                           V_DATE
                    FROM PURCHASE1
                    WHERE PARTY_CODE = @PARTY_CODE
                      AND CONTAINER_NO = @CONTAINER_NO
                      AND BILL_NO = @BILL_NO
                      AND V_TYPE IN ('SRPU','RCPT','BFRC')
                      AND V_NO <> @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE", con);

                    cmd.Parameters.AddWithValue("@PARTY_CODE", headerObj.BillFrom);
                    cmd.Parameters.AddWithValue("@CONTAINER_NO", headerObj.ContainerNo);
                    cmd.Parameters.AddWithValue("@BILL_NO", headerObj.BillNo);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string docId = reader["DOC_ID"].ToString();
                            string vDate = Convert.ToDateTime(reader["V_DATE"]).ToString("dd/MM/yyyy");

                            return (false,
                                $"Container No. {headerObj.ContainerNo} already exists in MRN, Serial No : {docId} dated : {vDate}");
                        }
                    }
                }

                // ============================
                // Gate Date Validation
                // ============================
                if (!string.IsNullOrWhiteSpace(headerObj.GATE_TYPE) &&
                    !string.IsNullOrWhiteSpace(headerObj.GateNo))
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT V_DATE
                    FROM GATE1
                    WHERE V_TYPE = @V_TYPE
                      AND V_NO = @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE", con);

                    cmd.Parameters.AddWithValue("@V_TYPE", headerObj.GATE_TYPE);
                    cmd.Parameters.AddWithValue("@V_NO", headerObj.GateNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                    object gateDateObj = await cmd.ExecuteScalarAsync();

                    if (gateDateObj != null && gateDateObj != DBNull.Value)
                    {
                        DateTime gateDate = Convert.ToDateTime(gateDateObj);
                        DateTime mrnDate = Convert.ToDateTime(headerObj.DocDate);

                        if (gateDate.Date > mrnDate.Date)
                        {
                            return (false,
                                $"MRN Date ({mrnDate:dd/MM/yyyy}) can not be less than Gate Date ({gateDate:dd/MM/yyyy}).");
                        }
                    }
                }

                // ============================
                // WB Qty Approval Validation
                // ============================
                foreach (var item in ItemDetails)
                {
                    if (globalVar.PubCompCode == "1" &&
                        item.WBQty == 0 &&
                        !string.Equals(headerObj.ReturnType, "Return", StringComparison.OrdinalIgnoreCase))
                    {
                        SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(WB_YN,'')
                        FROM ITEM_MAST
                        WHERE CODE = @ITEM_CODE
                        AND COMP_CODE = @COMP_CODE", con);

                        cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                        string wbYN = Convert.ToString(await cmd.ExecuteScalarAsync());

                        if (wbYN == "Yes")
                        {
                            return (false, "WB Qty is 0, Approval required.");
                        }
                    }
                }

                foreach (var item in ItemDetails)
                {
                    if (globalVar.PubCompCode != "1")
                    {
                        SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(WB_YN,'')
                        FROM ITEM_MAST
                        WHERE CODE = @ITEM_CODE
                          AND COMP_CODE = @COMP_CODE", con);

                        cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                        string wbYN = Convert.ToString(await cmd.ExecuteScalarAsync());

                        if (wbYN == "Yes")
                        {
                            if (item.RecQty == 0)
                            {
                                return (false, "WB_YN='Yes' and Received Qty is 0.");
                            }

                            if (string.IsNullOrWhiteSpace(item.KantaType) || item.KantaNo == 0)
                            {
                                return (false, $"WB Type and WB No is blank of Weighbridge item : {item.ItemName}");
                            }
                        }
                    }
                }

                // ============================
                // Duplicate Gate MRN Validation
                // ============================
                if (generalSetting.pubDefGateInMRN == "Yes")
                {
                    if (!string.IsNullOrWhiteSpace(headerObj.GATE_TYPE) &&
                        !string.IsNullOrWhiteSpace(headerObj.GateNo))
                    {
                        SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 1 CONCAT(V_TYPE, CAST(V_NO AS VARCHAR))
                        FROM PURCHASE2
                        WHERE V_TYPE = @MRN_TYPE
                          AND V_NO <> @MRN_NO
                          AND CONCAT(GATE_TYPE, GATE_NO) = CONCAT(@GATE_TYPE, @GATE_NO)
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE", con);

                        cmd.Parameters.AddWithValue("@MRN_TYPE", headerObj.DocType);
                        cmd.Parameters.AddWithValue("@MRN_NO", vNo);
                        cmd.Parameters.AddWithValue("@GATE_TYPE", headerObj.GATE_TYPE);
                        cmd.Parameters.AddWithValue("@GATE_NO", headerObj.GateNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                        object obj = await cmd.ExecuteScalarAsync();

                        if (obj != null)
                        {
                            return (false, $"GATE No already exist in MRN No : {obj}");
                        }
                    }
                }

                // ============================
                // Item Wise Validations
                // ============================
                foreach (var item in ItemDetails)
                {
                    // ============================
                    // Gate Item Validation
                    // ============================
                    if (generalSetting.pubDefGateInMRN == "Yes")
                    {
                        if (!string.IsNullOrWhiteSpace(headerObj.GATE_TYPE) &&
                            (headerObj.DocType == "SRPU" || headerObj.DocType == "STJW"))
                        {
                            // Item Exists in Gate2
                            SqlCommand cmd = new SqlCommand(@"
                            SELECT LTRIM(RTRIM(ITEM_CODE))
                            FROM GATE2
                            WHERE ITEM_CODE=@ITEM_CODE
                              AND V_TYPE=@GATE_TYPE
                              AND V_NO=@GATE_NO
                              AND COMP_CODE=@COMP_CODE
                              AND BRANCH_CODE=@BRANCH_CODE", con);

                            cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                            cmd.Parameters.AddWithValue("@GATE_TYPE", item.GateType);
                            cmd.Parameters.AddWithValue("@GATE_NO", item.GateNo);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                            object gateItemObj = await cmd.ExecuteScalarAsync();

                            if (gateItemObj == null || gateItemObj == DBNull.Value)
                            {
                                return (false, $"Item {item.ItemName} not exist in Gate document No : {headerObj.GateNo}");
                            }

                            int gateItemCode = Convert.ToInt32(gateItemObj);

                            if (gateItemCode != item.ItemCode)
                            {
                                return (false, $"Item name not matched as per Gate record of {item.ItemName}");
                            }

                            // Gate Bill Qty Validation
                            SqlCommand qtyCmd = new SqlCommand(@"
                            SELECT QTY
                            FROM GATE2
                            WHERE ITEM_CODE=@ITEM_CODE
                              AND V_TYPE=@GATE_TYPE
                              AND V_NO=@GATE_NO
                              AND REF_TYPE=@PO_TYPE
                              AND REF_NO=@PO_NO
                              AND COMP_CODE=@COMP_CODE
                              AND BRANCH_CODE=@BRANCH_CODE", con);

                            qtyCmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                            qtyCmd.Parameters.AddWithValue("@GATE_TYPE", item.GateType);
                            qtyCmd.Parameters.AddWithValue("@GATE_NO", item.GateNo);
                            qtyCmd.Parameters.AddWithValue("@PO_TYPE", item.POType);
                            qtyCmd.Parameters.AddWithValue("@PO_NO", item.PONo);
                            qtyCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            qtyCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                            object qtyObj = await qtyCmd.ExecuteScalarAsync();

                            decimal gateQty = qtyObj == null || qtyObj == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(qtyObj);

                            if (item.BillQty != gateQty)
                            {
                                return (false, $"MRN Bill Qty and Gate Bill Qty not matched of Item {item.ItemName}");
                            }
                        }
                    }

                    // ============================
                    // PO Mandatory Validation
                    // ============================
                    if (generalSetting.pubDefPOInMRN == "Yes")
                    {
                        if (item.PONo == 0)
                        {
                            return (false, $"PO Number is Required/Compulsory of Item {item.ItemName}");
                        }

                    }

                    // ============================
                    // Gate Number Mandatory Validation
                    // ============================
                    if (generalSetting.pubDefGateInMRN == "Yes")
                    {
                        if (item.GateNo == 0)
                        {
                            return (false, $"Gate Number is Required/Compulsory of Item {item.ItemName}");
                        }
                    }

                    // ============================
                    // PO / Sauda Validation
                    // ============================
                    if (generalSetting.pubDefPOInMRN == "Yes")
                    {
                        //================ RCPT / RCPI =================
                        if (headerObj.DocType == "RCPT" || headerObj.DocType == "RCPI")
                        {
                            // Current Item ka Sauda No
                            SqlCommand saudaCmd = new SqlCommand(@"
                            SELECT ISNULL(SAUDA_NO,0)
                            FROM ORDER2
                            WHERE V_TYPE=@V_TYPE
                                AND V_NO=@V_NO
                                AND COMP_CODE=@COMP_CODE
                                AND BRANCH_CODE=@BRANCH_CODE", con);

                            saudaCmd.Parameters.AddWithValue("@V_TYPE", item.POType);
                            saudaCmd.Parameters.AddWithValue("@V_NO", item.PONo);
                            saudaCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            saudaCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                            //int saudaNo = Convert.ToInt32(await saudaCmd.ExecuteScalarAsync() ?? 0);
                            object saudaObj = await saudaCmd.ExecuteScalarAsync();

                            int saudaNo = saudaObj == null || saudaObj == DBNull.Value
                                ? 0
                                : Convert.ToInt32(saudaObj);

                            if (saudaNo > 0)
                            {
                                decimal billQty = 0;
                                decimal totalSaudaQty = 0;
                                decimal totalReceivedQty = 0;

                                // Same Sauda wale sab items ka Bill Qty
                                foreach (var itm in ItemDetails)
                                {
                                    SqlCommand cmd = new SqlCommand(@"
                                    SELECT ISNULL(SAUDA_NO,0)
                                    FROM ORDER2
                                    WHERE V_TYPE=@V_TYPE
                                      AND V_NO=@V_NO
                                      AND COMP_CODE=@COMP_CODE
                                      AND BRANCH_CODE=@BRANCH_CODE", con);

                                    cmd.Parameters.AddWithValue("@V_TYPE", itm.POType);
                                    cmd.Parameters.AddWithValue("@V_NO", itm.PONo);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                    //int saudaNo1 = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
                                    object saudaObj1 = await cmd.ExecuteScalarAsync();

                                    int saudaNo1 = saudaObj1 == null || saudaObj1 == DBNull.Value
                                        ? 0
                                        : Convert.ToInt32(saudaObj1);

                                    if (saudaNo1 == saudaNo)
                                    {
                                        billQty += itm.BillQty ?? 0m;
                                    }
                                }

                                // Total Sauda Qty
                                SqlCommand qtyCmd = new SqlCommand(@"
                                SELECT ISNULL(SUM(QTY),0)
                                FROM SAUDA
                                WHERE V_TYPE='PAUD'
                                  AND V_NO=@V_NO
                                  AND COMP_CODE=@COMP_CODE
                                  AND BRANCH_CODE=@BRANCH_CODE", con);

                                qtyCmd.Parameters.AddWithValue("@V_NO", saudaNo);
                                qtyCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                qtyCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                totalSaudaQty = Convert.ToDecimal(await qtyCmd.ExecuteScalarAsync());

                                // Already Received Qty
                                SqlCommand recCmd = new SqlCommand(@"
                                SELECT ISNULL(SUM(RECD_QTY),0)
                                FROM PURCHASE2
                                WHERE SAUDA_TYPE='PAUD'
                                  AND SAUDA_NO=@SAUDA_NO
                                  AND COMP_CODE=@COMP_CODE
                                  AND BRANCH_CODE=@BRANCH_CODE
                                  AND V_TYPE=@MRN_TYPE
                                  AND V_NO<>@MRN_NO", con);

                                recCmd.Parameters.AddWithValue("@SAUDA_NO", saudaNo);
                                recCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                recCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                                recCmd.Parameters.AddWithValue("@MRN_TYPE", headerObj.DocType);
                                recCmd.Parameters.AddWithValue("@MRN_NO", vNo);

                                totalReceivedQty = Convert.ToDecimal(await recCmd.ExecuteScalarAsync());

                                totalReceivedQty += billQty;

                                // Sauda Date Validation
                                SqlCommand dateCmd = new SqlCommand(@"
                                SELECT V_DATE
                                FROM SAUDA
                                WHERE V_TYPE='PAUD'
                                  AND V_NO=@V_NO
                                  AND COMP_CODE=@COMP_CODE
                                  AND BRANCH_CODE=@BRANCH_CODE", con);

                                dateCmd.Parameters.AddWithValue("@V_NO", saudaNo);
                                dateCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                dateCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                object dtObj = await dateCmd.ExecuteScalarAsync();

                                if (dtObj != null && dtObj != DBNull.Value)
                                {
                                    DateTime saudaDate = Convert.ToDateTime(dtObj);

                                    if (saudaDate.AddDays(-2).Date >
                                        Convert.ToDateTime(headerObj.BillDate).Date)
                                    {
                                        return (false,
                                            $"Sauda No : '{saudaNo}' Date is Greater than Vendor Invoice Date");
                                    }
                                }

                                if (totalReceivedQty >
                                    (totalSaudaQty + Convert.ToDecimal(generalSetting.pubBPPurchTolQty)))
                                {
                                    decimal pendingQty = totalSaudaQty - totalReceivedQty + (headerObj.NumBillQty ?? 0m);

                                    return (false,
                                        $"Sauda Pending Quantity is = {pendingQty}, Your Invoice Qty is = {headerObj.NumBillQty}, Please Check it.");
                                }
                            }
                        }
                        
                        //================ Other MRN =================
                        else
                        {
                            if (item.PONo > 0)
                            {
                                SqlCommand poDateCmd = new SqlCommand(@"
                                SELECT V_DATE
                                FROM ORDER1
                                WHERE V_TYPE=@V_TYPE
                                  AND V_NO=@V_NO
                                  AND COMP_CODE=@COMP_CODE
                                  AND BRANCH_CODE=@BRANCH_CODE", con);

                                poDateCmd.Parameters.AddWithValue("@V_TYPE", item.POType);
                                poDateCmd.Parameters.AddWithValue("@V_NO", item.PONo);
                                poDateCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                poDateCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                object poDateObj = await poDateCmd.ExecuteScalarAsync();

                                if (poDateObj != null && poDateObj != DBNull.Value)
                                {
                                    DateTime poDate = Convert.ToDateTime(poDateObj);

                                    if (poDate.Date >
                                        Convert.ToDateTime(headerObj.DocDate).Date)
                                    {
                                        return (false,
                                            $"PO No : '{item.POType}{item.PONo}' Date is Greater than Vendor Invoice Date");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return (true, "");
        }

    }
}
