using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.GlobalFunction;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transiction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class ImportExportExpensesEntryRepository : IImportExportExpensesEntryRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly DbHelper _dbHelper;
        private readonly GlobalFunction _globalFunction;
        public ImportExportExpensesEntryRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DbHelper dbHelper, GlobalFunction globalFunction)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
            _dbHelper = dbHelper;
            _globalFunction = globalFunction;
        }

        public decimal? oldBankAmt = 0.0m;
        public int? oldplno = 0;
        public async Task<decimal> CheckExistingTDS(string billNo, int drCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            using var con = _dbConnection.GetErpConnection();
            using var cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Action", "CHeckExistingTDS");
            cmd.Parameters.Add("@BILL_NO", SqlDbType.VarChar).Value = billNo;
            cmd.Parameters.Add("@DEBIT_AC", SqlDbType.Int).Value = drCode;
            cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = gv.PubCompCode;
            cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = gv.PubBranchCode;

            await con.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();

            return result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
        }

        //==========================Validations=======================

        public async Task<ValidationResult> ValidatePartyGst(string gstType, string partyCode, string gstNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            if (string.IsNullOrWhiteSpace(gstNo))
            {
                return new ValidationResult { IsValid = true };
            }

            string query;
            var parameters = new Dictionary<string, object>
            {
                { "@COMP_CODE", gv.PubCompCode },
                { "@CODE", partyCode }
            };

            if (gstType.Equals("BillTo", StringComparison.OrdinalIgnoreCase))
            {
                query = @"SELECT LTRIM(RTRIM(GSTIN)) FROM SUBGROUP_ADDRESS WHERE GSTIN = @GSTIN AND COMP_CODE = @COMP_CODE AND CODE = @CODE";
                parameters.Add("@GSTIN", gstNo);
                string dbGstNo = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);
                if (!string.Equals(dbGstNo?.Trim(), gstNo.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "Mismatch Bill To GST No from Master Record."
                    };
                }
            }
            else if (gstType.Equals("ShipTo", StringComparison.OrdinalIgnoreCase))
            {
                query = @"SELECT GSTIN FROM SUBGROUP_ADDRESS WHERE IS_DEFAULT = 1 AND COMP_CODE = @COMP_CODE AND CODE = @CODE";
                string dbGstNo = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);
                if (!string.Equals(dbGstNo?.Trim(), gstNo.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult { IsValid = false, Message = "Mismatch GST No from Master Record." };
                }
            }

            return new ValidationResult { IsValid = true };
        }

        //==============================Save & Update======================
        public async Task<RepositoryResponse> SavePurchaseBillPassEntry(PurchaseWrapper data)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            if (data == null)
            {
                return new RepositoryResponse { status = false, message = "Invalid data!" };
            }

            var model = data.header;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            var (fAppStatus, fAppRemark) = await GetApprovalStatusAsync(tran, model);

                            var (priceType, gstHold) = await GetPurchaseCalculationBeforeSaveAsync(tran, data, model);

                            using var cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con, tran);
                            //Purchase1
                            var docID = model.V_TYPE + model.V_NO;
                            AddPurchaseParameters(cmd, model, docID, priceType, gstHold, fAppStatus, fAppRemark);

                            //Delete From Purchase2
                            await DeleteRecordsAsync(con, tran, "PURCHASE2", model.V_TYPE, model.V_NO);

                            // PURCHASE2
                            DataTable dtPurchase2 = ConvertToPurchase2TVP(data.lineRows, docID);
                            SqlParameter tvpParam = cmd.Parameters.AddWithValue("@PURCHASE2_TYPE", dtPurchase2);
                            tvpParam.SqlDbType = SqlDbType.Structured;
                            tvpParam.TypeName = "dbo.PURCHASE2_TYPE";

                            //Delete Attachments
                            await DeleteRecordsAsync(con, tran, "IMG_TABLE", model.V_TYPE, model.V_NO);
                            //Attachments
                            await SaveAttachmentsAsync(con, tran, data.Attachement, "InsertAttachments", docID, model);

                            await cmd.ExecuteNonQueryAsync();
                            tran.Commit();

                            await PostPurchaseSaveAsync(con, model, fAppStatus);

                            return new RepositoryResponse { status = true, message = "Purchase saved successfully." };
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            return new RepositoryResponse { status = false, message = ex.Message };
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return new RepositoryResponse { status = false, message = "SQL Error: " + ex.Message };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "Error: " + ex.Message };
            }
        }

        private async Task<(string FAppStatus, string FAppRemark)> GetApprovalStatusAsync(SqlTransaction tran, PURCHASE1 model)
        {
            var g = _globalVariableService.GetGlobalVariables();

            decimal crNoteAmt = (model.QTY_CR_AMT ?? 0) + (model.QTY_CR_TAX ?? 0) + (model.QC_CR_AMT ?? 0) + (model.QC_CR_TAX ?? 0) +
                                (model.RDF_CR_AMT ?? 0) + (model.RDF_CR_TAX ?? 0) + (model.QLT_CR_AMT ?? 0) + (model.QLT_CR_TAX ?? 0);

            bool isFinalApprovalBody = false;
            bool isFinalApprovalBodyCN = false;

            isFinalApprovalBody = "FINAL".Equals(
                await _dbHelper.ExecuteScalarAsynctran<string>(@"SELECT APPROV_USER FROM DOC_APPROSTAGE WHERE USER_CODE=@USER_CODE AND DOC_CODE=@DOC_CODE AND COMP_CODE=@COMP_CODE",
                    new()
                    {
                        new("@USER_CODE", g.PubUserId),
                        new("@DOC_CODE", model.V_TYPE),
                        new("@COMP_CODE", g.PubCompCode)
                    }, tran), StringComparison.OrdinalIgnoreCase);

            isFinalApprovalBodyCN = "FINAL".Equals(await _dbHelper.ExecuteScalarAsynctran<string>(
                    @"SELECT APPROV_USER FROM DOC_APPROSTAGE WHERE FLAG_A='C' AND USER_CODE=@USER_CODE AND DOC_CODE=@DOC_CODE AND COMP_CODE=@COMP_CODE",
                    new()
                    {
                        new("@USER_CODE", g.PubUserId),
                        new("@DOC_CODE", model.V_TYPE),
                        new("@COMP_CODE", g.PubCompCode)
                    }, tran), StringComparison.OrdinalIgnoreCase);

            string fAppStatus = "";
            string fAppRemark = "";

            if (crNoteAmt > 0 && isFinalApprovalBodyCN)
            {
                fAppStatus = "Approved";
                fAppRemark = "Document Approved.";
            }
            else if (crNoteAmt == 0 && isFinalApprovalBody)
            {
                fAppStatus = "Approved";
                fAppRemark = "Document Approved.";
            }

            return (fAppStatus, fAppRemark);
        }
        private async Task<(string PriceType, string GstHold)> GetPurchaseCalculationBeforeSaveAsync(SqlTransaction tran, PurchaseWrapper data,
            PURCHASE1 model)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            //=======================
            //      Price Type
            //=======================
            string priceType = string.Empty;

            if (data.lineRows?.Any() == true)
            {
                var firstRow = data.lineRows.First();

                priceType = await _dbHelper.ExecuteScalarAsynctran<string>(
                    @"SELECT ISNULL(PRICE_TYPE,'') FROM ORDER1 WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE
                    AND BRANCH_CODE=@BRANCH_CODE",
                    new()
                    {
                        new("@V_TYPE", firstRow.PO_TYPE),
                        new("@V_NO", firstRow.PO_NO),
                        new("@COMP_CODE", globalVar.PubCompCode),
                        new("@BRANCH_CODE", globalVar.PubBranchCode)
                    },
                    tran);
            }

            //=======================
            //      GST Hold
            //=======================
            string gstHold = "No";

            if (model.INPUT_TYPE == "Input GST" || model.INPUT_TYPE == "GST Input" || model.INPUT_TYPE == "Local" || model.INPUT_TYPE == "Central" || model.INPUT_TYPE == "Import")
            {
                object result = await _dbHelper.ExecuteScalarAsynctran<object>(
                    @"SELECT TOP 1 1 FROM GSTHOLD_MAST WHERE PARTY_CODE=@PARTY_CODE AND COMP_CODE=@COMP_CODE",
                    new()
                    {
                        new("@PARTY_CODE", model.PARTY_CODE),
                        new("@COMP_CODE", globalVar.PubCompCode)
                    },
                    tran);

                bool gstHoldExists = result != null && Convert.ToInt32(result) == 1;

                if (!gstHoldExists)
                {
                    object releaseExistResult = await _dbHelper.ExecuteScalarAsynctran<object>(
                        @"SELECT TOP 1 1 FROM GSTHOLD_RELEASE WHERE REF_TYPE=@REF_TYPE AND REF_NO=@REF_NO AND COMP_CODE=@COMP_CODE
                        AND BRANCH_CODE=@BRANCH_CODE",
                        new()
                        {
                            new("@REF_TYPE", model.V_TYPE),
                            new("@REF_NO", model.V_NO),
                            new("@COMP_CODE", globalVar.PubCompCode),
                            new("@BRANCH_CODE", globalVar.PubBranchCode)
                        },
                        tran);
                    bool releaseExist = releaseExistResult != null && Convert.ToInt32(releaseExistResult) == 1;
                    gstHold = releaseExist ? "No" : "Yes";
                }
            }

            if ((model.CGST_AMT ?? 0) +
                (model.SGST_AMT ?? 0) +
                (model.IGST_AMT ?? 0) <= 0)
            {
                gstHold = "No";
            }

            return (priceType, gstHold);
        }

        private void AddPurchaseParameters(SqlCommand cmd, PURCHASE1 model, string docID, string priceType, string gstHold, string fAppStatus, string fAppRemark)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            cmd.CommandType = CommandType.StoredProcedure;

            // PURCHASE1
            cmd.Parameters.AddWithValue("@Action", "INSERTANDUPDATE");
            cmd.Parameters.AddWithValue("@SubAction", model.ACTION);
            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

            cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? "");
            cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DOC_ID", docID ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@REF_TYPE", model.REF_TYPE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@REF_NO", model.REF_NO ?? (object)DBNull.Value);

            //------------ Bill Details -----------
            cmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_ADDRESSID", model.BILL_ADDRESSID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_ADD1", model.BILL_ADD1 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_ADD2", model.BILL_ADD2 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_ADD3", model.BILL_ADD3 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_CITY", model.BILL_CITY ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_GST", model.BILL_GST ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_PINCODE", model.BILL_PINCODE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@DISP_ADDRESS", model.DISP_ADDRESS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DISP_CITY", model.DISP_CITY ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@CURRENCY", model.CURRENCY ?? (object)DBNull.Value);

            //------------ Ship Details -----------
            cmd.Parameters.AddWithValue("@SHIP_CODE", model.SHIP_CODE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SHIP_ADDRESSID", model.SHIP_ADDRESSID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SHIP_ADD1", model.SHIP_ADD1 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SHIP_ADD2", model.SHIP_ADD2 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SHIP_ADD3", model.SHIP_ADD3 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SHIP_CITY", model.SHIP_CITY ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SHIP_GST", model.SHIP_GST ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SHIP_PINCODE", model.SHIP_PINCODE ?? (object)DBNull.Value);

            //------------ Document Details -----------
            cmd.Parameters.AddWithValue("@BILL_NO", model.BILL_NO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_DATE", model.BILL_DATE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@CHALL_NO", model.CHALL_NO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CHALL_DATE", model.CHALL_DATE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@BL_NO", model.BL_NO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BL_DT", model.BL_DT ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@WAYBILL_NO", model.WAYBILL_NO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EWB_DATE", model.EWB_DATE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EWB_INVNO", model.EWB_INVNO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EWB_EXPDATE", model.EWB_EXPDATE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@DEBIT_AC", model.DEBIT_AC ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CREDIT_AC", model.CREDIT_AC ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@EMP_CODE", model.EMP_CODE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EXPS_TYPE", model.EXPS_TYPE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@INPUT_TYPE", model.INPUT_TYPE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@STATUS", model.STATUS);
            cmd.Parameters.AddWithValue("@EXCH_RATE", model.EXCH_RATE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@NAMOUNT", model.NAMOUNT ?? (object)DBNull.Value);

            //------------ Item Total -----------

            cmd.Parameters.AddWithValue("@RECD_QTY", model.RECD_QTY ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_QTY", model.BILL_QTY ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@AMOUNT", model.AMOUNT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DISC_AMT", model.DISC_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@PACK_AMT", model.PACK_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CGST_AMT", model.CGST_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SGST_AMT", model.SGST_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IGST_AMT", model.IGST_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CESS_AMT", model.CESS_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@VAT_AMT", model.VAT_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@OTH_AMT", model.OTH_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TCS_PER", model.TCS_PER ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TCS_AMT", model.TCS_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ROUND_OFF", model.ROUND_OFF ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@TDS_ACT", model.TDS_ACT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TDS_PER", model.TDS_PER ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TDS_AMT", model.TDS_AMT ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@TDS_PER194Q", model.TDS_PER194Q ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TDS_AMT194Q", model.TDS_AMT194Q ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@BANK_RATE", model.BANK_RATE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BANK_AMT", model.BANK_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DIFF_AMT", model.DIFF_AMT ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@PL_NO", model.PL_NO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@PL_DATE", model.PL_DATE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@BILLAMT_USD", model.BILLAMT_USD ?? (object)DBNull.Value);

            //------------ Logistic Details -----------

            cmd.Parameters.AddWithValue("@TRANSPORT_CODE", model.TRANSPORT_CODE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TRANSPORT_NAME", model.TRANSPORT_NAME ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@TRUCK_NO", model.TRUCK_NO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CONTAINER_NO", model.CONTAINER_NO ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@GR_NO", model.GR_NO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@GR_DATE", model.GR_DATE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@SEALED_VEHICLE", model.SEALED_VEHICLE ?? (object)DBNull.Value);

            // Freight

            cmd.Parameters.AddWithValue("@FRTPAY_AMT", model.FRTPAY_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FRTPAY_TAXPER", model.FRTPAY_TAXPER ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FRTPAY_TAX", model.FRTPAY_TAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FRTPAY_DRAC", model.FRTPAY_DRAC ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FRTPAY_CRAC", model.FRTPAY_CRAC ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FRTPAY_NAR", model.FRTPAY_NAR ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@FRT_TDSPER", model.FRT_TDSPER ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FRT_TDS", model.FRT_TDS ?? (object)DBNull.Value);

            // Transport GST

            cmd.Parameters.AddWithValue("@TRP_GSTNO", model.TRP_GSTNO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TRP_TAXTYPE", model.TRP_TAXTYPE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TRP_BILLNO", model.TRP_BILLNO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TRP_BILLDATE", model.TRP_BILLDATE ?? (object)DBNull.Value);

            // Weigh Bridge

            cmd.Parameters.AddWithValue("@WB_AMT", model.WB_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@WB_TDSPER", model.WB_TDSPER ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@WB_TDS", model.WB_TDS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@WB_DRACT", model.WB_DRACT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@WB_CRACT", model.WB_CRACT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@WB_NARR", model.WB_NARR ?? (object)DBNull.Value);

            // Unloading

            cmd.Parameters.AddWithValue("@UL_AMT", model.UL_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UL_TDSPER", model.UL_TDSPER ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UL_TDS", model.UL_TDS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UL_DRACT", model.UL_DRACT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UL_CRACT", model.UL_CRACT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UL_NARR", model.UL_NARR ?? (object)DBNull.Value);

            //------------ CR/DR Note Details -----------

            cmd.Parameters.AddWithValue("@DR_FROM_TPT", model.DR_FROM_TPT ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@QLT_DR_AMT", model.QLT_DR_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@QLT_DR_TAX", model.QLT_DR_TAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@QLT_DR_NAR", model.QLT_DR_NAR ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@QLT_CR_AMT", model.QLT_CR_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@QLT_CR_TAX", model.QLT_CR_TAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@QLT_CR_NAR", model.QLT_CR_NAR ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@RDF_DR_AMT", model.RDF_DR_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RDF_DR_TAX", model.RDF_DR_TAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RDF_DR_NAR", model.RDF_DR_NAR ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@RDF_CR_AMT", model.RDF_CR_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RDF_CR_TAX", model.RDF_CR_TAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RDF_CR_NAR", model.RDF_CR_NAR ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@QTY_DR_AMT", model.QTY_DR_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@QTY_DR_TAX", model.QTY_DR_TAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@QTY_DR_NAR", model.QTY_DR_NAR ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@QTY_CR_AMT", model.QTY_CR_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@QTY_CR_TAX", model.QTY_CR_TAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@QTY_CR_NAR", model.QTY_CR_NAR ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@QC_DR_AMT", model.QC_DR_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@QC_DR_TAX", model.QC_DR_TAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@QC_DR_NAR", model.QC_DR_NAR ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@QC_CR_AMT", model.QC_CR_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@QC_CR_TAX", model.QC_CR_TAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@QC_CR_NAR", model.QC_CR_NAR ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@OTH_DR_AMT", model.OTH_DR_AMT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@OTH_DR_TAX", model.OTH_DR_TAX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@OTH_DR_NAR", model.OTH_DR_NAR ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@HOLD_PAY", model.HOLD_PAY ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@HOLD_REASON", model.HOLD_REASON ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@HOLD_DATE", model.HOLD_DATE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@FAPROV_STATUS", fAppStatus);
            cmd.Parameters.AddWithValue("@FAPROV_REMARKS", fAppRemark);

            cmd.Parameters.AddWithValue("@PRICE_TYPE", priceType);
            cmd.Parameters.AddWithValue("@TAX_HOLD", gstHold);

            // Audit Fields
            if (model.ACTION == "INSERT")
            {
                cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                cmd.Parameters.AddWithValue("@AED", "A");
            }
            else
            {
                cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                cmd.Parameters.AddWithValue("@AED", "E");
            }
            cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
        }

        private async Task DeleteRecordsAsync(SqlConnection con, SqlTransaction tran, string tableName, string vType, int? vNo)
        {
            var g = _globalVariableService.GetGlobalVariables();

            string sql = $@"DELETE FROM {tableName} WHERE YEAR_CODE=@YEAR_CODE AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE
                        AND V_TYPE=@V_TYPE AND V_NO=@V_NO";

            await ExecuteQueryAsync(con, sql, tran, new()
            {
                new("@YEAR_CODE", g.PubFYearCode),
                new("@COMP_CODE", g.PubCompCode),
                new("@BRANCH_CODE", g.PubBranchCode),
                new("@V_TYPE", vType),
                new("@V_NO", vNo)
            });
        }

        private async Task SaveAttachmentsAsync(SqlConnection con, SqlTransaction tran, IEnumerable<PurchaseBillAttachments> attachments,
        string action, string docId, PURCHASE1 model)
        {
            var g = _globalVariableService.GetGlobalVariables();
            int rowId = 1;

            foreach (var attachment in attachments)
            {
                if (string.IsNullOrWhiteSpace(attachment.FILE_NAME))
                    continue;

                byte[] fileBytes = Convert.FromBase64String(attachment.FILE_DATA);

                using var cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con, tran)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                cmd.Parameters.AddWithValue("@DOC_ID", docId);
                cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                cmd.Parameters.AddWithValue("@ROWID", rowId);

                cmd.Parameters.AddWithValue("@FILE_NAME", attachment.FILE_NAME);
                cmd.Parameters.AddWithValue("@FILE_Path", attachment.FILE_NAME);
                cmd.Parameters.Add("@IMG_FILE", SqlDbType.VarBinary).Value = fileBytes;

                cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                cmd.Parameters.AddWithValue("@AED", "A");
                cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                await cmd.ExecuteNonQueryAsync();

                rowId++;
            }
        }

        public DataTable ConvertToPurchase2TVP(List<PURCHASE2> list, string docID)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            DataTable dt = new DataTable("PURCHASE2_TYPE");

            // Add columns exactly as in PURCHASE2_TYPE (order and types must match)
            dt.Columns.Add("SNO", typeof(int));
            dt.Columns.Add("ITEM_CODE", typeof(int));
            dt.Columns.Add("ITEM_NAME", typeof(string));
            dt.Columns.Add("MAKE_CODE", typeof(int));
            dt.Columns.Add("HSN_CODE", typeof(string));
            dt.Columns.Add("RCM_YN", typeof(string));
            dt.Columns.Add("INPUT_YN", typeof(string));
            dt.Columns.Add("UOM_CODE", typeof(int));
            dt.Columns.Add("UOM_NAME", typeof(string));
            dt.Columns.Add("DEPT_CODE", typeof(int));
            dt.Columns.Add("NOS", typeof(int));
            dt.Columns.Add("PLUS_MINUSQTY", typeof(decimal));
            dt.Columns.Add("WB_QTY", typeof(decimal));
            dt.Columns.Add("RECD_QTY", typeof(decimal));
            dt.Columns.Add("BILL_QTY", typeof(decimal));
            dt.Columns.Add("USD_RATE", typeof(decimal));
            dt.Columns.Add("EXCH_RATE", typeof(decimal));
            dt.Columns.Add("RATE", typeof(decimal));
            dt.Columns.Add("AMOUNT", typeof(decimal));
            dt.Columns.Add("DISC_PER", typeof(decimal));
            dt.Columns.Add("DISC_AMT", typeof(decimal));
            dt.Columns.Add("PACK_PER", typeof(decimal));
            dt.Columns.Add("PACK_AMT", typeof(decimal));
            dt.Columns.Add("TAX_CODE", typeof(int));
            dt.Columns.Add("CGST_PER", typeof(decimal));
            dt.Columns.Add("CGST_AMT", typeof(decimal));
            dt.Columns.Add("SGST_PER", typeof(decimal));
            dt.Columns.Add("SGST_AMT", typeof(decimal));
            dt.Columns.Add("IGST_PER", typeof(decimal));
            dt.Columns.Add("IGST_AMT", typeof(decimal));
            dt.Columns.Add("CESS_PER", typeof(decimal));
            dt.Columns.Add("CESS_AMT", typeof(decimal));
            dt.Columns.Add("VAT_PER", typeof(decimal));
            dt.Columns.Add("VAT_AMT", typeof(decimal));
            dt.Columns.Add("OTH_AMT", typeof(decimal));
            dt.Columns.Add("NET_AMT", typeof(decimal));
            dt.Columns.Add("LAND_RATE", typeof(decimal));
            dt.Columns.Add("LAND_AMT", typeof(decimal));
            dt.Columns.Add("POLAND_RATE", typeof(decimal));
            dt.Columns.Add("PO_RATE", typeof(decimal));
            dt.Columns.Add("BIN_LOCATION", typeof(string));
            dt.Columns.Add("BIN_CODE", typeof(int));
            dt.Columns.Add("PO_TYPE", typeof(string));
            dt.Columns.Add("PO_NO", typeof(int));
            dt.Columns.Add("SAUDA_TYPE", typeof(string));
            dt.Columns.Add("SAUDA_NO", typeof(int));
            dt.Columns.Add("KANTA_TYPE", typeof(string));
            dt.Columns.Add("KANTA_NO", typeof(int));
            dt.Columns.Add("REQ_TYPE", typeof(string));
            dt.Columns.Add("REQ_NO", typeof(int));
            dt.Columns.Add("GATE_TYPE", typeof(string));
            dt.Columns.Add("GATE_NO", typeof(int));
            dt.Columns.Add("REF_TYPE", typeof(string));
            dt.Columns.Add("REF_NO", typeof(int));
            dt.Columns.Add("QC_TYPE", typeof(string));
            dt.Columns.Add("QC_NO", typeof(int));
            dt.Columns.Add("PASS_TYPE", typeof(string));
            dt.Columns.Add("PASS_NO", typeof(int));
            dt.Columns.Add("EMPTY_YN", typeof(string));
            dt.Columns.Add("MACH_CODE", typeof(int));
            dt.Columns.Add("REMARKS", typeof(string));
            dt.Columns.Add("RATE_MONTHLY", typeof(decimal));
            dt.Columns.Add("RATE_QUARTERLY", typeof(decimal));
            dt.Columns.Add("RATE_ANNUALY", typeof(decimal));
            dt.Columns.Add("RATE_SPECIAL", typeof(decimal));
            dt.Columns.Add("FINAL_LOCK", typeof(string));
            dt.Columns.Add("DRNOTE_AMT", typeof(decimal));
            dt.Columns.Add("CRNOTE_AMT", typeof(decimal));
            dt.Columns.Add("QLTDIFF_DRAMT", typeof(decimal));
            dt.Columns.Add("RDIFF_DRAMT", typeof(decimal));
            dt.Columns.Add("QCDIFF_DRAMT", typeof(decimal));
            dt.Columns.Add("QTYDIFF_DRAMT", typeof(decimal));
            dt.Columns.Add("OTH_DRAMT", typeof(decimal));

            int sno = 1;

            foreach (var item in list)
            {
                dt.Rows.Add(
                    sno++,
                    item.ITEM_CODE ?? (object)DBNull.Value,
                    item.ITEM_NAME ?? (object)DBNull.Value,
                    item.MAKE_CODE ?? (object)DBNull.Value,
                    item.HSN_CODE ?? (object)DBNull.Value,
                    item.RCM_YN ?? (object)DBNull.Value,
                    item.INPUT_YN ?? (object)DBNull.Value,
                    item.UOM_CODE ?? (object)DBNull.Value,
                    item.UOM_NAME ?? (object)DBNull.Value,
                    item.DEPT_CODE ?? (object)DBNull.Value,
                    item.NOS ?? (object)DBNull.Value,
                    item.PLUS_MINUSQTY ?? (object)DBNull.Value,
                    item.WB_QTY ?? (object)DBNull.Value,
                    item.RECD_QTY ?? (object)DBNull.Value,
                    item.BILL_QTY ?? (object)DBNull.Value,
                    item.USD_RATE ?? (object)DBNull.Value,
                    item.EXCH_RATE ?? (object)DBNull.Value,
                    item.RATE ?? (object)DBNull.Value,
                    item.AMOUNT ?? (object)DBNull.Value,
                    item.DISC_PER ?? (object)DBNull.Value,
                    item.DISC_AMT ?? (object)DBNull.Value,
                    item.PACK_PER ?? (object)DBNull.Value,
                    item.PACK_AMT ?? (object)DBNull.Value,
                    item.TAX_CODE ?? (object)DBNull.Value,
                    item.CGST_PER ?? (object)DBNull.Value,
                    item.CGST_AMT ?? (object)DBNull.Value,
                    item.SGST_PER ?? (object)DBNull.Value,
                    item.SGST_AMT ?? (object)DBNull.Value,
                    item.IGST_PER ?? (object)DBNull.Value,
                    item.IGST_AMT ?? (object)DBNull.Value,
                    item.CESS_PER ?? (object)DBNull.Value,
                    item.CESS_AMT ?? (object)DBNull.Value,
                    item.VAT_PER ?? (object)DBNull.Value,
                    item.VAT_AMT ?? (object)DBNull.Value,
                    item.OTH_AMT ?? (object)DBNull.Value,
                    item.NET_AMT ?? (object)DBNull.Value,
                    item.LAND_RATE ?? (object)DBNull.Value,
                    item.LAND_AMT ?? (object)DBNull.Value,
                    item.POLAND_RATE ?? (object)DBNull.Value,
                    item.PO_RATE ?? (object)DBNull.Value,
                    item.BIN_LOCATION ?? (object)DBNull.Value,
                    item.BIN_CODE ?? (object)DBNull.Value,
                    item.PO_TYPE ?? (object)DBNull.Value,
                    item.PO_NO ?? (object)DBNull.Value,
                    item.SAUDA_TYPE ?? (object)DBNull.Value,
                    item.SAUDA_NO ?? (object)DBNull.Value,
                    item.KANTA_TYPE ?? (object)DBNull.Value,
                    item.KANTA_NO ?? (object)DBNull.Value,
                    item.REQ_TYPE ?? (object)DBNull.Value,
                    item.REQ_NO ?? (object)DBNull.Value,
                    item.GATE_TYPE ?? (object)DBNull.Value,
                    item.GATE_NO ?? (object)DBNull.Value,
                    item.REF_TYPE ?? (object)DBNull.Value,
                    item.REF_NO ?? (object)DBNull.Value,
                    item.QC_TYPE ?? (object)DBNull.Value,
                    item.QC_NO ?? (object)DBNull.Value,
                    item.PASS_TYPE ?? (object)DBNull.Value,
                    item.PASS_NO ?? (object)DBNull.Value,
                    item.EMPTY_YN ?? (object)DBNull.Value,
                    item.MACH_CODE ?? (object)DBNull.Value,
                    item.REMARKS ?? (object)DBNull.Value,
                    item.RATE_MONTHLY ?? (object)DBNull.Value,
                    item.RATE_QUARTERLY ?? (object)DBNull.Value,
                    item.RATE_ANNUALY ?? (object)DBNull.Value,
                    item.RATE_SPECIAL ?? (object)DBNull.Value,
                    item.FINAL_LOCK ?? (object)DBNull.Value,
                    item.DRNOTE_AMT ?? (object)DBNull.Value,
                    item.CRNOTE_AMT ?? (object)DBNull.Value,
                    item.QLTDIFF_DRAMT ?? (object)DBNull.Value,
                    item.RDIFF_DRAMT ?? (object)DBNull.Value,
                    item.QCDIFF_DRAMT ?? (object)DBNull.Value,
                    item.QTYDIFF_DRAMT ?? (object)DBNull.Value,
                    item.OTH_DRAMT ?? (object)DBNull.Value
                );
            }

            return dt;
        }

        private async Task PostPurchaseSaveAsync(SqlConnection con, PURCHASE1 model, string fAppStatus)
        {
            int? vno = model.V_NO;
            string? vType = model.V_TYPE;

            await ProcessApprovalAsync(con, fAppStatus, vType, vno, model.V_DATE);

            await _globalFunction.StockValuationAsync(con, vType, vno);

        }

        private async Task ProcessApprovalAsync(SqlConnection con, string approvalStatus, string vType, int? vNo, DateTime? vDate)
        {
            if (!string.Equals(approvalStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                return;

            var g = _globalVariableService.GetGlobalVariables();

            // Existing Ledger Posting Method
            //await _accountPostingService.ACTPostingPurchase("LEDGER2", vDate, vDate, vType, vNo); //Implement Later

            await ExecuteQueryAsync(con, query: "sp_PurchaseBillPassEntryDirect", parameters: new()
            {
                new("@Action", "UpdateApprovalOnSave"),
                new("@USER_CODE", g.PubUserId),
                new("@V_TYPE", vType),
                new("@V_NO", vNo),
                new("@COMP_CODE", g.PubCompCode),
                new("@BRANCH_CODE", g.PubBranchCode),
                new("@YEAR_CODE", g.PubFYearCode)
            }, isProcOrQry: true);

            //await loadPendingApprovals(); //Implement Later
        }

        private async Task ExecuteQueryAsync(SqlConnection con, string query, SqlTransaction? tran = null, List<SqlParameter>? parameters = null, bool isProcOrQry = false)
        {
            using var cmd = new SqlCommand(query, con, tran);
            cmd.CommandType = isProcOrQry ? CommandType.StoredProcedure : CommandType.Text;
            if (parameters?.Any() == true)
                cmd.Parameters.AddRange(parameters.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }

        //============================Get By Id====================
        public async Task<RepositoryResponseData<FullPurchaseBillResponse>> GetFullQuotationByVno(int vNo, string vType)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            PURCHASE1 header = null;
            List<PURCHASE2> items = new();
            List<PurchaseBillAttachments> attachments = new();

            try
            {
                using SqlConnection conn = _dbConnection.GetErpConnection();
                using SqlCommand cmd = new("sp_PurchaseBillPassEntryDirect", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "SELECT");
                cmd.Parameters.AddWithValue("@SubAction", "GETALLBYVNO");
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                conn.Open();
                using SqlDataReader rdr = cmd.ExecuteReader();

                // Header (PURCHASE1)
                if (rdr.Read())
                {
                    header = new PURCHASE1
                    {
                        V_TYPE = rdr["V_TYPE"]?.ToString(),
                        V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : null,
                        V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : null,
                        PLACE_CODE = rdr["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PLACE_CODE"]) : null,
                        EMP_CODE = rdr["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["EMP_CODE"]) : null,
                        PARTY_CODE = rdr["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_CODE"]) : null,
                        //PARTY_NAME = rdr["PARTY_NAME"]?.ToString(),
                        EXCH_RATE = rdr["EXCH_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["EXCH_RATE"]) : null,
                        CREDIT_AC = rdr["CREDIT_AC"] != DBNull.Value ? Convert.ToInt32(rdr["CREDIT_AC"]) : null,
                        DEBIT_AC = rdr["DEBIT_AC"] != DBNull.Value ? Convert.ToInt32(rdr["DEBIT_AC"]) : null,
                        BILL_ADD1 = rdr["BILL_ADD1"]?.ToString(),
                        BILL_ADD2 = rdr["BILL_ADD2"]?.ToString(),
                        BILL_ADD3 = rdr["BILL_ADD3"]?.ToString(),
                        BILL_CITY = rdr["BILL_CITY"] != DBNull.Value ? Convert.ToInt32(rdr["BILL_CITY"]) : null,
                        BILL_STATE = rdr["BILL_STATE"] != DBNull.Value ? Convert.ToInt32(rdr["BILL_STATE"]) : null,
                        BILL_PINCODE = rdr["BILL_PINCODE"]?.ToString(),
                        BILL_ADDRESSID = rdr["BILL_ADDRESSID"] != DBNull.Value ? Convert.ToInt32(rdr["BILL_ADDRESSID"]) : null,
                        BILL_GST = rdr["BILL_GST"]?.ToString(),
                        SHIP_CODE = rdr["SHIP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_CODE"]) : null,
                        SHIP_ADD1 = rdr["SHIP_ADD1"]?.ToString(),
                        SHIP_ADD2 = rdr["SHIP_ADD2"]?.ToString(),
                        SHIP_ADD3 = rdr["SHIP_ADD3"]?.ToString(),
                        SHIP_CITY = rdr["SHIP_CITY"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_CITY"]) : null,
                        SHIP_STATE = rdr["SHIP_STATE"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_STATE"]) : null,
                        SHIP_PINCODE = rdr["SHIP_PINCODE"]?.ToString(),
                        SHIP_ADDRESSID = rdr["SHIP_ADDRESSID"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_ADDRESSID"]) : null,
                        SHIP_GST = rdr["SHIP_GST"]?.ToString(),
                        BILL_NO = rdr["BILL_NO"]?.ToString(),
                        BILL_DATE = rdr["BILL_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["BILL_DATE"]) : null,
                        CHALL_NO = rdr["CHALL_NO"]?.ToString(),
                        CHALL_DATE = rdr["CHALL_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["CHALL_DATE"]) : null,
                        UOM_CODE = rdr["UOM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["UOM_CODE"]) : null,
                        GATE_TYPE = rdr["GATE_TYPE"]?.ToString(),
                        GATE_NO = rdr["GATE_NO"] != DBNull.Value ? Convert.ToInt32(rdr["GATE_NO"]) : null,
                        REF_TYPE = rdr["REF_TYPE"]?.ToString(),
                        REF_NO = rdr["REF_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REF_NO"]) : null,
                        PASS_TYPE = rdr["PASS_TYPE"]?.ToString(),
                        PASS_NO = rdr["PASS_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PASS_NO"]) : null,
                        TRANSIT_NO = rdr["TRANSIT_NO"] != DBNull.Value ? Convert.ToInt32(rdr["TRANSIT_NO"]) : null,
                        WAYBILL_NO = rdr["WAYBILL_NO"]?.ToString(),
                        TRANSPORT_CODE = rdr["TRANSPORT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["TRANSPORT_CODE"]) : null,
                        TRANSPORT_NAME = rdr["TRANSPORT_NAME"]?.ToString(),
                        TRANSPORT_AC = rdr["TRANSPORT_AC"] != DBNull.Value ? Convert.ToInt32(rdr["TRANSPORT_AC"]) : null,
                        GR_NO = rdr["GR_NO"]?.ToString(),
                        GR_DATE = rdr["GR_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["GR_DATE"]) : null,
                        TRUCK_NO = rdr["TRUCK_NO"]?.ToString(),
                        CONTAINER_NO = rdr["CONTAINER_NO"]?.ToString(),
                        SEALED_VEHICLE = rdr["SEALED_VEHICLE"] != DBNull.Value ? Convert.ToInt32(rdr["SEALED_VEHICLE"]) : null,
                        INPUT_TYPE = rdr["INPUT_TYPE"]?.ToString(),
                        EXPS_TYPE = rdr["EXPS_TYPE"]?.ToString(),
                        REMARKS = rdr["REMARKS"]?.ToString(),
                        STATUS = rdr["STATUS"] != DBNull.Value ? Convert.ToInt32(rdr["STATUS"]) : null,
                        RECD_QTY = rdr["RECD_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["RECD_QTY"]) : null,
                        BILL_QTY = rdr["BILL_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["BILL_QTY"]) : null,
                        AMOUNT = rdr["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["AMOUNT"]) : null,
                        DISC_PER = rdr["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_PER"]) : null,
                        DISC_AMT = rdr["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_AMT"]) : null,
                        PACK_PER = rdr["PACK_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_PER"]) : null,
                        PACK_AMT = rdr["PACK_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_AMT"]) : null,
                        CGST_PER = rdr["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_PER"]) : null,
                        CGST_AMT = rdr["CGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_AMT"]) : null,
                        SGST_PER = rdr["SGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_PER"]) : null,
                        SGST_AMT = rdr["SGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_AMT"]) : null,
                        IGST_PER = rdr["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_PER"]) : null,
                        IGST_AMT = rdr["IGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_AMT"]) : null,
                        CESS_PER = rdr["CESS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_PER"]) : null,
                        CESS_AMT = rdr["CESS_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_AMT"]) : null,
                        VAT_PER = rdr["VAT_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["VAT_PER"]) : null,
                        VAT_AMT = rdr["VAT_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["VAT_AMT"]) : null,
                        OTH_AMT = rdr["OTH_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["OTH_AMT"]) : null,
                        TCS_PER = rdr["TCS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["TCS_PER"]) : null,
                        TCS_AMT = rdr["TCS_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["TCS_AMT"]) : null,
                        ROUND_OFF = rdr["ROUND_OFF"] != DBNull.Value ? Convert.ToDecimal(rdr["ROUND_OFF"]) : null,
                        NAMOUNT = rdr["NAMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["NAMOUNT"]) : null,
                        DIFF_AMT = rdr["DIFF_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["DIFF_AMT"]) : null,
                        BANK_AMT = rdr["BANK_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["BANK_AMT"]) : null,
                        BANK_RATE = rdr["BANK_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["BANK_RATE"]) : null,
                        PL_NO = rdr["PL_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PL_NO"]) : null,
                        PL_DATE = rdr["PL_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["PL_DATE"]) : null,
                        BILLAMT_USD = rdr["BILLAMT_USD"] != DBNull.Value ? Convert.ToDecimal(rdr["BILLAMT_USD"]) : null,
                        FRTPAY_AMT = rdr["FRTPAY_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["FRTPAY_AMT"]) : null,
                        FRTPAY_TAXPER = rdr["FRTPAY_TAXPER"] != DBNull.Value ? Convert.ToDecimal(rdr["FRTPAY_TAXPER"]) : null,
                        FRTPAY_TAX = rdr["FRTPAY_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["FRTPAY_TAX"]) : null,
                        FRTPAY_NAR = rdr["FRTPAY_NAR"]?.ToString(),
                        FRTPAY_DRAC = rdr["FRTPAY_DRAC"] != DBNull.Value ? Convert.ToInt32(rdr["FRTPAY_DRAC"]) : null,
                        FRTPAY_CRAC = rdr["FRTPAY_CRAC"] != DBNull.Value ? Convert.ToInt32(rdr["FRTPAY_CRAC"]) : null,
                        FRT_TDSPER = rdr["FRT_TDSPER"] != DBNull.Value ? Convert.ToDecimal(rdr["FRT_TDSPER"]) : null,
                        FRT_TDS = rdr["FRT_TDS"] != DBNull.Value ? Convert.ToDecimal(rdr["FRT_TDS"]) : null,
                        DR_FROM_TPT = rdr["DR_FROM_TPT"]?.ToString(),
                        TDS_ACT = rdr["TDS_ACT"] != DBNull.Value ? Convert.ToInt32(rdr["TDS_ACT"]) : null,
                        TDS_PER = rdr["TDS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["TDS_PER"]) : null,
                        TDS_AMT = rdr["TDS_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["TDS_AMT"]) : null,
                        WB_AMT = rdr["WB_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["WB_AMT"]) : null,
                        WB_TDSPER = rdr["WB_TDSPER"] != DBNull.Value ? Convert.ToDecimal(rdr["WB_TDSPER"]) : null,
                        WB_TDS = rdr["WB_TDS"] != DBNull.Value ? Convert.ToDecimal(rdr["WB_TDS"]) : null,
                        WB_DRACT = rdr["WB_DRACT"] != DBNull.Value ? Convert.ToInt32(rdr["WB_DRACT"]) : null,
                        WB_CRACT = rdr["WB_CRACT"] != DBNull.Value ? Convert.ToInt32(rdr["WB_CRACT"]) : null,
                        WB_NARR = rdr["WB_NARR"]?.ToString(),
                        UL_AMT = rdr["UL_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["UL_AMT"]) : null,
                        UL_TDSPER = rdr["UL_TDSPER"] != DBNull.Value ? Convert.ToDecimal(rdr["UL_TDSPER"]) : null,
                        UL_TDS = rdr["UL_TDS"] != DBNull.Value ? Convert.ToDecimal(rdr["UL_TDS"]) : null,
                        UL_DRACT = rdr["UL_DRACT"] != DBNull.Value ? Convert.ToInt32(rdr["UL_DRACT"]) : null,
                        UL_CRACT = rdr["UL_CRACT"] != DBNull.Value ? Convert.ToInt32(rdr["UL_CRACT"]) : null,
                        UL_NARR = rdr["UL_NARR"]?.ToString(),
                        QLT_DR_AMT = rdr["QLT_DR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["QLT_DR_AMT"]) : null,
                        QLT_DR_TAX = rdr["QLT_DR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["QLT_DR_TAX"]) : null,
                        QLT_DR_NAR = rdr["QLT_DR_NAR"]?.ToString(),
                        QLT_CR_AMT = rdr["QLT_CR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["QLT_CR_AMT"]) : null,
                        QLT_CR_TAX = rdr["QLT_CR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["QLT_CR_TAX"]) : null,
                        QLT_CR_NAR = rdr["QLT_CR_NAR"]?.ToString(),
                        RDF_DR_AMT = rdr["RDF_DR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["RDF_DR_AMT"]) : null,
                        RDF_DR_TAX = rdr["RDF_DR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["RDF_DR_TAX"]) : null,
                        RDF_DR_NAR = rdr["RDF_DR_NAR"]?.ToString(),
                        RDF_CR_AMT = rdr["RDF_CR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["RDF_CR_AMT"]) : null,
                        RDF_CR_TAX = rdr["RDF_CR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["RDF_CR_TAX"]) : null,
                        RDF_CR_NAR = rdr["RDF_CR_NAR"]?.ToString(),
                        QTY_DR_AMT = rdr["QTY_DR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY_DR_AMT"]) : null,
                        QTY_DR_TAX = rdr["QTY_DR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY_DR_TAX"]) : null,
                        QTY_DR_NAR = rdr["QTY_DR_NAR"]?.ToString(),
                        QTY_CR_AMT = rdr["QTY_CR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY_CR_AMT"]) : null,
                        QTY_CR_TAX = rdr["QTY_CR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY_CR_TAX"]) : null,
                        QTY_CR_NAR = rdr["QTY_CR_NAR"]?.ToString(),
                        QC_DR_AMT = rdr["QC_DR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["QC_DR_AMT"]) : null,
                        QC_DR_TAX = rdr["QC_DR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["QC_DR_TAX"]) : null,
                        QC_DR_NAR = rdr["QC_DR_NAR"]?.ToString(),
                        QC_CR_AMT = rdr["QC_CR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["QC_CR_AMT"]) : null,
                        QC_CR_TAX = rdr["QC_CR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["QC_CR_TAX"]) : null,
                        QC_CR_NAR = rdr["QC_CR_NAR"]?.ToString(),
                        OTH_DR_AMT = rdr["OTH_DR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["OTH_DR_AMT"]) : null,
                        OTH_DR_TAX = rdr["OTH_DR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["OTH_DR_TAX"]) : null,
                        OTH_DR_NAR = rdr["OTH_DR_NAR"]?.ToString(),
                        QC_TYPE = rdr["QC_TYPE"]?.ToString(),
                        QC_NO = rdr["QC_NO"] != DBNull.Value ? Convert.ToInt32(rdr["QC_NO"]) : null,
                        DEPT_CODE = rdr["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_CODE"]) : null,
                        TAX_HOLD = rdr["TAX_HOLD"]?.ToString(),
                        PRICE_TYPE = rdr["PRICE_TYPE"]?.ToString(),
                        FAPROV_STATUS = rdr["FAPROV_STATUS"]?.ToString(),
                        FAPROV_REMARKS = rdr["FAPROV_REMARKS"]?.ToString(),
                        HOLD_PAY = rdr["HOLD_PAY"]?.ToString(),
                        HOLD_REASON = rdr["HOLD_REASON"]?.ToString(),
                        HOLD_DATE = rdr["HOLD_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["HOLD_DATE"]) : null,
                        IMPORT_AMT = rdr["IMPORT_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["IMPORT_AMT"]) : null,
                        IMPORT_TAX = rdr["IMPORT_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["IMPORT_TAX"]) : null,
                        INVLAND_AMT = rdr["INVLAND_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["INVLAND_AMT"]) : null,
                        RCM_NO = rdr["RCM_NO"]?.ToString(),
                        DRNOTE_MAILSEND = rdr["DRNOTE_MAILSEND"] != DBNull.Value ? Convert.ToInt32(rdr["DRNOTE_MAILSEND"]) : null,
                        FRT_BILLNO = rdr["FRT_BILLNO"] != DBNull.Value ? Convert.ToInt32(rdr["FRT_BILLNO"]) : null,
                        FRT_BILLDT = rdr["FRT_BILLDT"] != DBNull.Value ? Convert.ToDateTime(rdr["FRT_BILLDT"]) : null,
                        FRT_PASSDT = rdr["FRT_PASSDT"] != DBNull.Value ? Convert.ToDateTime(rdr["FRT_PASSDT"]) : null,
                        FRT_CHQ = rdr["FRT_CHQ"]?.ToString(),
                        FRT_REMARK = rdr["FRT_REMARK"]?.ToString(),
                        GSTRMAIL_PARTYCNTR = rdr["GSTRMAIL_PARTYCNTR"] != DBNull.Value ? Convert.ToInt32(rdr["GSTRMAIL_PARTYCNTR"]) : null,
                        GSTRMAIL_BILLCNTR = rdr["GSTRMAIL_BILLCNTR"] != DBNull.Value ? Convert.ToInt32(rdr["GSTRMAIL_BILLCNTR"]) : null,
                        TDS_PER194Q = rdr["TDS_PER194Q"] != DBNull.Value ? Convert.ToDecimal(rdr["TDS_PER194Q"]) : null,
                        TDS_AMT194Q = rdr["TDS_AMT194Q"] != DBNull.Value ? Convert.ToDecimal(rdr["TDS_AMT194Q"]) : null,
                        DISP_ADDRESS = rdr["DISP_ADDRESS"]?.ToString(),
                        DISP_CITY = rdr["DISP_CITY"] != DBNull.Value ? Convert.ToInt32(rdr["DISP_CITY"]) : null,
                        GSTRECO_REFTYPE = rdr["GSTRECO_REFTYPE"]?.ToString(),
                        GSTRECO_REFNO = rdr["GSTRECO_REFNO"] != DBNull.Value ? Convert.ToInt32(rdr["GSTRECO_REFNO"]) : null,
                        STOREIMG_FLG = rdr["STOREIMG_FLG"] != DBNull.Value ? Convert.ToInt32(rdr["STOREIMG_FLG"]) : null,
                        RET_TYPE = rdr["RET_TYPE"]?.ToString(),
                        FEXCH_USD = rdr["FEXCH_USD"] != DBNull.Value ? Convert.ToDecimal(rdr["FEXCH_USD"]) : null,
                        TRP_GSTNO = rdr["TRP_GSTNO"]?.ToString(),
                        TRP_BILLNO = rdr["TRP_BILLNO"]?.ToString(),
                        TRP_BILLDATE = rdr["TRP_BILLDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["TRP_BILLDATE"]) : null,
                        TRP_TAXTYPE = rdr["TRP_TAXTYPE"]?.ToString(),
                        MONTH_3B = rdr["MONTH_3B"] != DBNull.Value ? Convert.ToDateTime(rdr["MONTH_3B"]) : null,
                        MONTH_3BN = rdr["MONTH_3BN"] != DBNull.Value ? Convert.ToDateTime(rdr["MONTH_3BN"]) : null,
                        TRP_MONTH3B = rdr["TRP_MONTH3B"] != DBNull.Value ? Convert.ToDateTime(rdr["TRP_MONTH3B"]) : null,
                        MTH_REVYN3B = rdr["MTH_REVYN3B"]?.ToString(),
                        TRP_MTHREVYN3B = rdr["TRP_MTHREVYN3B"]?.ToString(),
                        MONTH_2B = rdr["MONTH_2B"] != DBNull.Value ? Convert.ToDateTime(rdr["MONTH_2B"]) : null,
                        EWB_DATE = rdr["EWB_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EWB_DATE"]) : null,
                        EWB_EXPDATE = rdr["EWB_EXPDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EWB_EXPDATE"]) : null,
                        EWB_INVNO = rdr["EWB_INVNO"]?.ToString(),
                        PL_AMT = rdr["PL_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["PL_AMT"]) : null,
                        CURRENCY = rdr["CURRENCY"] != DBNull.Value ? Convert.ToInt32(rdr["CURRENCY"]) : null,
                        UDATE = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : null,
                        EINV_PARTY = rdr["EINV_PARTY"] != DBNull.Value ? Convert.ToInt32(rdr["EINV_PARTY"]) : null,


                    };
                }

                //  Items (PURCHASE2)
                if (rdr.NextResult())
                {
                    while (rdr.Read())
                    {
                        items.Add(new PURCHASE2
                        {
                            DOC_ID = rdr["DOC_ID"]?.ToString(),
                            YEAR_CODE = rdr["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["YEAR_CODE"]) : 0,
                            COMP_CODE = rdr["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COMP_CODE"]) : 0,
                            BRANCH_CODE = rdr["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BRANCH_CODE"]) : 0,
                            V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                            V_TYPE = rdr["V_TYPE"]?.ToString(),
                            V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                            SNO = rdr["SNO"] != DBNull.Value ? Convert.ToInt32(rdr["SNO"]) : 0,
                            ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                            ITEM_NAME = rdr["ITEM_NAME"]?.ToString(),
                            MAKE_CODE = rdr["MAKE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MAKE_CODE"]) : 0,
                            HSN_CODE = rdr["HSN_CODE"]?.ToString(),
                            RCM_YN = rdr["RCM_YN"]?.ToString(),
                            INPUT_YN = rdr["INPUT_YN"]?.ToString(),
                            UOM_CODE = rdr["UOM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["UOM_CODE"]) : 0,
                            UNIT = rdr["UOM_NAME"]?.ToString(),
                            DEPT_CODE = rdr["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_CODE"]) : 0,
                            NOS = rdr["NOS"] != DBNull.Value ? Convert.ToInt32(rdr["NOS"]) : 0,
                            PLUS_MINUSQTY = rdr["PLUS_MINUSQTY"] != DBNull.Value ? Convert.ToDecimal(rdr["PLUS_MINUSQTY"]) : 0,
                            WB_QTY = rdr["WB_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["WB_QTY"]) : 0,
                            RECD_QTY = rdr["RECD_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["RECD_QTY"]) : 0,
                            BILL_QTY = rdr["BILL_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["BILL_QTY"]) : 0,
                            USD_RATE = rdr["USD_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["USD_RATE"]) : 0,
                            EXCH_RATE = rdr["EXCH_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["EXCH_RATE"]) : 0,
                            RATE = rdr["RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE"]) : 0,
                            AMOUNT = rdr["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["AMOUNT"]) : 0,
                            DISC_PER = rdr["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_PER"]) : 0,
                            DISC_AMT = rdr["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_AMT"]) : 0,
                            PACK_PER = rdr["PACK_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_PER"]) : 0,
                            PACK_AMT = rdr["PACK_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_AMT"]) : 0,
                            TAX_CODE = rdr["TAX_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["TAX_CODE"]) : 0,
                            CGST_PER = rdr["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_PER"]) : 0,
                            CGST_AMT = rdr["CGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_AMT"]) : 0,
                            SGST_PER = rdr["SGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_PER"]) : 0,
                            SGST_AMT = rdr["SGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_AMT"]) : 0,
                            IGST_PER = rdr["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_PER"]) : 0,
                            IGST_AMT = rdr["IGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_AMT"]) : 0,
                            CESS_PER = rdr["CESS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_PER"]) : 0,
                            CESS_AMT = rdr["CESS_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_AMT"]) : 0,
                            VAT_PER = rdr["VAT_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["VAT_PER"]) : 0,
                            VAT_AMT = rdr["VAT_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["VAT_AMT"]) : 0,
                            OTH_AMT = rdr["OTH_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["OTH_AMT"]) : 0,
                            NET_AMT = rdr["NET_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["NET_AMT"]) : 0,
                            LAND_RATE = rdr["LAND_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["LAND_RATE"]) : 0,
                            LAND_AMT = rdr["LAND_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["LAND_AMT"]) : 0,
                            POLAND_RATE = rdr["POLAND_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["POLAND_RATE"]) : 0,
                            PO_RATE = rdr["PO_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["PO_RATE"]) : 0,
                            BIN_LOCATION = rdr["BIN_LOCATION"]?.ToString(),
                            BIN_CODE = rdr["BIN_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BIN_CODE"]) : 0,
                            PO_TYPE = rdr["PO_TYPE"]?.ToString(),
                            PO_NO = rdr["PO_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PO_NO"]) : 0,
                            SAUDA_TYPE = rdr["SAUDA_TYPE"]?.ToString(),
                            SAUDA_NO = rdr["SAUDA_NO"] != DBNull.Value ? Convert.ToInt32(rdr["SAUDA_NO"]) : 0,
                            KANTA_TYPE = rdr["KANTA_TYPE"]?.ToString(),
                            KANTA_NO = rdr["KANTA_NO"] != DBNull.Value ? Convert.ToInt32(rdr["KANTA_NO"]) : 0,
                            REQ_TYPE = rdr["REQ_TYPE"]?.ToString(),
                            REQ_NO = rdr["REQ_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REQ_NO"]) : 0,
                            GATE_TYPE = rdr["GATE_TYPE"]?.ToString(),
                            GATE_NO = rdr["GATE_NO"] != DBNull.Value ? Convert.ToInt32(rdr["GATE_NO"]) : 0,
                            REF_TYPE = rdr["REF_TYPE"]?.ToString(),
                            REF_NO = rdr["REF_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REF_NO"]) : 0,
                            QC_TYPE = rdr["QC_TYPE"]?.ToString(),
                            QC_NO = rdr["QC_NO"] != DBNull.Value ? Convert.ToInt32(rdr["QC_NO"]) : 0,
                            PASS_TYPE = rdr["PASS_TYPE"]?.ToString(),
                            PASS_NO = rdr["PASS_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PASS_NO"]) : 0,
                            EMPTY_YN = rdr["EMPTY_YN"]?.ToString(),
                            MACH_CODE = rdr["MACH_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MACH_CODE"]) : 0,
                            REMARKS = rdr["REMARKS"]?.ToString(),
                            RATE_MONTHLY = rdr["RATE_MONTHLY"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_MONTHLY"]) : 0,
                            RATE_QUARTERLY = rdr["RATE_QUARTERLY"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_QUARTERLY"]) : 0,
                            RATE_ANNUALY = rdr["RATE_ANNUALY"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_ANNUALY"]) : 0,
                            RATE_SPECIAL = rdr["RATE_SPECIAL"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_SPECIAL"]) : 0,
                            FINAL_LOCK = rdr["FINAL_LOCK"]?.ToString(),
                            UUSER = rdr["UUSER"] != DBNull.Value ? Convert.ToInt32(rdr["UUSER"]) : 0,
                            UDATE = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : DateTime.MinValue,
                            EUSER = rdr["EUSER"] != DBNull.Value ? Convert.ToInt32(rdr["EUSER"]) : 0,
                            EDATE = rdr["EDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EDATE"]) : DateTime.MinValue,
                            AED = rdr["AED"]?.ToString(),
                            WSID = rdr["WSID"]?.ToString(),
                            LIP = rdr["LIP"]?.ToString(),
                            LID = rdr["LID"]?.ToString()
                        });
                    }
                }

                // Attachments
                if (rdr.NextResult())
                {
                    while (rdr.Read())
                    {
                        attachments.Add(new PurchaseBillAttachments
                        {
                            FILE_NAME = rdr["FILE_NAME"]?.ToString(),
                            FILE_DATA = rdr["IMG_FILE"] != DBNull.Value
                                            ? Convert.ToBase64String((byte[])rdr["IMG_FILE"])
                                            : null,
                            FILE_Path = rdr["FILE_Path"]?.ToString()
                        });
                    }
                }


                if (header != null)
                {
                    oldBankAmt = header.BANK_AMT;
                    oldplno = header.PL_NO;
                }

                decimal existingTDS = await getTotalExistingAdjustment(vType, vNo);
;
                var result = new FullPurchaseBillResponse
                {
                    Header = header,
                    Items = items,
                    Attachments = attachments,
                    existingTDS = existingTDS
                };
                return new RepositoryResponseData<FullPurchaseBillResponse> { status = true, data = result };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<FullPurchaseBillResponse> { status = false, message = "Error fetching quotation" + ex.Message };
            }
        }

        //===============Calculate TDS Button Click================
        public async Task<PBTdsCalculation> CalculateTDS(PURCHASE1 model)
        {
            var g = _globalVariableService.GetGlobalVariables();

            var result = new PBTdsCalculation();

            // Advance TDS
            string advTdsQuery = @"SELECT ISNULL(SUM(AMT),0) FROM LEDGER2 WHERE BILL_NO=@BILL_NO AND DR_CODE=@DR_CODE AND COMP_CODE=@COMP_CODE
                                    AND BRANCH_CODE=@BRANCH_CODE";

            result.AdvTds = await _dbHelper.GetExecuteScalarAsync<decimal>(advTdsQuery, new Dictionary<string, object>
            {
                { "@BILL_NO", $"{model.REF_TYPE}{model.REF_NO}" },
                { "@DR_CODE", model.PARTY_CODE },
                { "@COMP_CODE", g.PubCompCode },
                { "@BRANCH_CODE", g.PubBranchCode }
            });

            // Purchase Details
            string purchaseQuery = @"SELECT AMOUNT, ISNULL(QLT_DR_AMT,0)+ISNULL(RDF_DR_AMT,0)+ISNULL(QTY_DR_AMT,0)+ISNULL(QC_DR_AMT,0)+ISNULL(OTH_DR_AMT,0) AS DrNote,
                                    ISNULL(QLT_CR_AMT,0)+ISNULL(RDF_CR_AMT,0)+ISNULL(QTY_CR_AMT,0)+ISNULL(QC_CR_AMT,0) AS CrNote FROM PURCHASE1
                                    WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE";

            using var con = _dbConnection.GetErpConnection();
            await con.OpenAsync();
            using var cmd = new SqlCommand(purchaseQuery, con);
            cmd.Parameters.AddRange(new[]
            {
                new SqlParameter("@V_TYPE", model.V_TYPE),
                new SqlParameter("@V_NO", model.V_NO),
                new SqlParameter("@COMP_CODE", g.PubCompCode),
                new SqlParameter("@BRANCH_CODE", g.PubBranchCode),
                new SqlParameter("@YEAR_CODE", g.PubFYearCode)
            });

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                result.NetAmt = reader["AMOUNT"] == DBNull.Value ? 0M : Convert.ToDecimal(reader["AMOUNT"]);
                result.DrNote = reader["DrNote"] == DBNull.Value ? 0M : Convert.ToDecimal(reader["DrNote"]);
                result.CrNote = reader["CrNote"] == DBNull.Value ? 0M : Convert.ToDecimal(reader["CrNote"]);
            }
            else
            {
                result.NetAmt = model.AMOUNT ?? 0;
                result.DrNote = (model.QTY_DR_AMT ?? 0) + (model.RDF_DR_AMT ?? 0) + (model.QC_DR_AMT ?? 0) + (model.QLT_DR_AMT ?? 0) + (model.OTH_DR_AMT ?? 0);
                result.CrNote = (model.QTY_CR_AMT ?? 0) + (model.RDF_CR_AMT ?? 0) + (model.QC_CR_AMT ?? 0) + (model.QLT_CR_AMT ?? 0);
            }
            result.Tds194Q = result.NetAmt - result.DrNote - result.AdvTds + result.CrNote;
            return result;
        }

        //===============Copy From====================
        public RepositoryResponseList<CopyFromMenuItem> GetCopyFromMenu()
        {
            var list = new List<CopyFromMenuItem>();

            try
            {
                string qry = @"Select Code, Name
                   from DOCTYPE_MAST
                   where code in ('DORD') order by Name";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand(qry, con);
                    con.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        string code = dr["Code"].ToString();
                        list.Add(new CopyFromMenuItem
                        {
                            Code = code,
                            Name = dr["Name"].ToString(),
                            //Modal = GetModalId(code)
                        });
                    }
                }

                return new RepositoryResponseList<CopyFromMenuItem> { status = true, data = list };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<CopyFromMenuItem> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<CopyFromGridResponse> GetCopyFromData(CopyFromRequest request)
        {
            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();
                using SqlCommand? cmd = BuildCopyFromCommand(request, con);

                if (cmd == null)
                {
                    return new RepositoryResponseData<CopyFromGridResponse>
                    {
                        status = true,
                        data = new CopyFromGridResponse()
                    };
                }

                con.Open();

                DataTable dt = new();

                using (SqlDataAdapter da = new(cmd))
                {
                    da.Fill(dt);
                }

                // Columns
                var columns = dt.Columns.Cast<DataColumn>()
                    .Select(col =>
                    {
                        string field = col.ColumnName;
                        string title = col.ColumnName;

                        int start = col.ColumnName.IndexOf(" (");

                        if (start >= 0 && col.ColumnName.EndsWith(")"))
                        {
                            field = col.ColumnName[..start];
                            title = col.ColumnName[(start + 2)..^1];
                        }

                        return new
                        {
                            OriginalColumn = col.ColumnName,
                            Field = field.ToUpperInvariant(),
                            Title = title
                        };
                    })
                    .ToList();


                // Rows
                var rows = dt.AsEnumerable()
                    .Select(row =>
                    {
                        var dict = new Dictionary<string, object?>();

                        foreach (var col in columns)
                        {
                            dict[col.Field] =
                                row[col.OriginalColumn] == DBNull.Value
                                    ? null
                                    : row[col.OriginalColumn];
                        }

                        return dict;
                    })
                    .ToList();


                // Response columns
                var responseColumns = columns
                    .Select(col => new CopyFromColumn
                    {
                        Field = col.Field,
                        Title = col.Title
                    })
                    .ToList();

                return new RepositoryResponseData<CopyFromGridResponse>
                {
                    status = true,
                    data = new CopyFromGridResponse
                    {
                        Columns = responseColumns,
                        Rows = rows
                    }
                };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<CopyFromGridResponse>
                {
                    status = false,
                    message = ex.Message
                };
            }
        }

        private SqlCommand? BuildCopyFromCommand(CopyFromRequest request, SqlConnection con)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string action = $"CopyFrom{request.CurrentVType}";

            var supportedActions = new[]
            {
                "CopyFromDORD"
            };

            if (!supportedActions.Contains(action))
                return null;

            var cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@PARTY_CODE", request.BillTo);
            //cmd.Parameters.AddWithValue("@BILL_NO", request.BillNo ?? "");
            cmd.Parameters.AddWithValue("@V_NO", request.VNo);
            //cmd.Parameters.AddWithValue("@V_TYPE", request.vType);
            //cmd.Parameters.AddWithValue("@CopyFromVtype", request.CurrentVType);
            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

            return cmd;
        }

        //=================Pending Approval List=============
        public RepositoryResponseData<List<PendingApprovalModel>> GetPendingApprovalList()
        {
            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();

                var gv = _globalVariableService.GetGlobalVariables();

                string qry = $@"with tmpApprovalList as (
                                select 'Send' as Type, doc_id as DocID, format(v_date,'') as [Doc Date], send_name as [Send By], send_date as [Send Date], 
                                user_name as [Send To], Status, Approval_remark as [Approval Status], Remarks, origin_name as [Created By], origin_date as [Created Date]
                                from APPROVAL_STATUS 
                                where COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND year_code=@year_code and close_date is null and
                                v_type ='RMDP'
                                union all 
                                select 'Not Send',a.DOC_ID,format(a.v_date,'dd/MM/yyyy'),'',NULL,'',iif(a.STATUS=1,'OPEN',iif(a.STATUS=2,'CANCEL','CLOSE')),'','',
                                b.user_name,a.udate from PURCHASE1 a 
                                left join USER_MAST b on a.UUSER=b.code and a.comp_Code=b.comp_code where isnull(a.faprov_status,'')<>'Approved'
                                and a.COMP_CODE=@COMP_CODE and a.BRANCH_CODE =@BRANCH_CODE and a.YEAR_CODE=@YEAR_CODE
                                and a.V_TYPE in (select code from DOCTYPE_MAST where doctype='PurchaseExpenses')) 
                                select * from tmpApprovalList order by type, DocID";

                using SqlCommand cmd = new(qry, con)
                {
                    CommandType = CommandType.Text
                };

                //cmd.Parameters.AddWithValue("@Action", "GetPendingApprovalList");
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                con.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                List<PendingApprovalModel> list = new();

                while (reader.Read())
                {
                    list.Add(new PendingApprovalModel
                    {
                        Type = reader["Type"]?.ToString(),
                        DocID = reader["DocID"].ToString(),
                        DocDate = reader["Doc Date"]?.ToString(),
                        SendBy = reader["Send By"]?.ToString(),
                        SendDate = reader["Send Date"]?.ToString(),
                        SendTo = reader["Send To"]?.ToString(),
                        Status = reader["Status"]?.ToString(),
                        ApprovalStatus = reader["Approval Status"]?.ToString(),
                        Remarks = reader["Remarks"]?.ToString(),
                        CreatedBy = reader["Created By"]?.ToString(),
                        CreatedDate = reader["Created Date"]?.ToString(),
                        //PartyName = reader["Party Name"]?.ToString(),
                        //BillAmount = reader["Bill Amount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Bill Amount"])
                    });
                }

                return new RepositoryResponseData<List<PendingApprovalModel>>
                {
                    status = true,
                    data = list
                };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<PendingApprovalModel>>
                {
                    status = false,
                    message = ex.Message
                };
            }
        }

        //Address
        public AddressDetails GetAddByParty(int code, int addressId)
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
            return addressDetails;
        }

        //Pack on basic
        public async Task<RepositoryResponseData<int>> GetPackOnBasic(int code)
        {
            int packOnBasic = 0;
            string query = @"SELECT PACK_ONBASIC FROM TAX_MAST WHERE code = @code AND Active = 1";
            var parameters = new Dictionary<string, object> { { "@CODE", code } };
            packOnBasic = await _dbHelper.GetExecuteScalarAsync<int>(query, parameters);
            return new RepositoryResponseData<int> { status = true, data = packOnBasic };
        }

        // Get Freight Credit Account by Transport Code
        public async Task<(int PartyCode, string PartyName)> GetFrtCrAcByTransCodeAsync(int transportCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                // Get Party Code & Party Name
                string transportQuery = @"
                SELECT ISNULL(T.PARTY_CODE,0) AS PARTY_CODE, ISNULL(S.NAME,'') AS PARTY_NAME FROM TRANSPORT_MAST T LEFT JOIN SUBGROUP_MAST S ON T.PARTY_CODE = S.CODE AND T.COMP_CODE = S.COMP_CODE
                WHERE T.COMP_CODE = @COMP_CODE AND T.CODE = @CODE";

                int partyCode = 0;
                string partyName = "";

                using (SqlCommand cmd = new SqlCommand(transportQuery, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", transportCode);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            partyCode = reader["PARTY_CODE"] != DBNull.Value
                                ? Convert.ToInt32(reader["PARTY_CODE"])
                                : 0;

                            partyName = reader["PARTY_NAME"]?.ToString() ?? "";
                        }
                    }
                }
                return (partyCode, partyName);
            }

        }


        // Get Purchase Amt
        public async Task<RepositoryResponseData<decimal>> GetPartyPurchaseAmount(int partyCode, string vType, int? vNo = null,
            decimal? currentAmount = null)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            string query;
            var parameters = new Dictionary<string, object>
                                {
                                    { "@PARTY_CODE", partyCode },
                                    { "@V_TYPE", vType },
                                    { "@COMP_CODE", globalVar.PubCompCode },
                                    { "@YEAR_CODE", globalVar.PubFYearCode }
                                };

            if (vNo.HasValue && currentAmount.HasValue)
            {
                query = @"SELECT ISNULL(SUM(NAMOUNT), 0) + @CURRENT_AMOUNT FROM PURCHASE1 WHERE PARTY_CODE = @PARTY_CODE AND V_TYPE = @V_TYPE AND V_NO <> @V_NO
                      AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE";

                parameters.Add("@V_NO", vNo.Value);
                parameters.Add("@CURRENT_AMOUNT", currentAmount.Value);
            }
            else
            {
                query = @"SELECT ISNULL(SUM(NAMOUNT), 0) FROM PURCHASE1 WHERE PARTY_CODE = @PARTY_CODE AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE
                      AND YEAR_CODE = @YEAR_CODE";
            }

            decimal totalAmount = await _dbHelper.GetExecuteScalarAsync<decimal>(query, parameters);

            return new RepositoryResponseData<decimal> { status = true, data = totalAmount };
        }

        public async Task<RepositoryResponseData<string>> GetTDS206Apply(int partyCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            string query = @"SELECT ISNULL(TDS_206APPLY, '') FROM SUBGROUP_MAST WHERE COMP_CODE = @COMP_CODE AND CODE = @PARTY_CODE";

            var parameters = new Dictionary<string, object>
                                {
                                    { "@COMP_CODE", globalVar.PubCompCode },
                                    { "@PARTY_CODE", partyCode }
                                };

            string tds206Apply = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);

            return new RepositoryResponseData<string> { status = true, data = tds206Apply };
        }


        //Purchase or Sale Voucher No
        public async Task<RepositoryResponseData<string>> GetPurchaseOrSaleVoucherNo(string transportName, string grNo, string currentVoucher, string purchaseOrSale)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = "";

            if (purchaseOrSale.Equals("PURCHASE", StringComparison.OrdinalIgnoreCase))
            {
                query += @"SELECT TOP 1 CONCAT(V_TYPE, V_NO) FROM Purchase1 WHERE CONCAT(V_TYPE, V_NO) <> @DOCID AND Transport_Name = @Transport_Name AND GR_NO = @GR_NO 
                            AND V_Type NOT IN (SELECT Code FROM Doctype_Mast WHERE Doctype = 'MaterialReceipt' ) AND Comp_Code = @Comp_Code AND Branch_Code = @Branch_Code 
                            AND Year_Code = @Year_Code";
            }
            else if (purchaseOrSale.Equals("SALE", StringComparison.OrdinalIgnoreCase))
            {
                query = @"SELECT TOP 1 CONCAT(V_TYPE, V_NO) FROM Sale1 WHERE CONCAT(V_TYPE, V_NO) <> @DOCID AND Transport_Name = @Transport_Name
                      AND GR_NO = @GR_NO AND Comp_Code = @Comp_Code AND Branch_Code = @Branch_Code AND Year_Code = @Year_Code";
            }
            else
            {
                return new RepositoryResponseData<string> { status = false, message = "Invalid data." };
            }

            var parameters = new Dictionary<string, object>
                {
                    { "@DOCID", currentVoucher },
                    { "@Transport_Name", transportName },
                    { "@GR_NO", grNo },
                    { "@Comp_Code", gv.PubCompCode },
                    { "@Branch_Code", gv.PubBranchCode },
                    { "@Year_Code", gv.PubFYearCode }
                };

            string voucherNo = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);

            return new RepositoryResponseData<string> { status = true, data = voucherNo };
        }

        //payment exists
        public async Task<RepositoryResponseData<bool>> CheckPaymentExists(string docType, int docNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = @"SELECT TOP 1 1 FROM LEDGER_OS WHERE V_TYPE = 'BPMT' AND DOC_TYPE = @V_TYPE AND DOC_NO = @V_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";
            var parameters = new Dictionary<string, object>
                {
                    { "@V_TYPE", docType },
                    { "@V_NO", docNo },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode }
                };

            int result = await _dbHelper.GetExecuteScalarAsync<int>(query, parameters);
            bool exists = result == 1;
            return new RepositoryResponseData<bool> { status = true, data = exists };
        }

        //Duplicate Bill Check
        public async Task<(bool Exists, string DocId, DateTime? VDate)> CheckDuplicateBill(int partyCode, string billNo, int currentVNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = @"SELECT TOP 1 DOC_ID AS DocId, format(V_date, 'dd/MM/yyyy') AS VDate FROM PURCHASE1 WHERE PARTY_CODE = @PARTY_CODE
                                AND BILL_NO = @BILL_NO AND V_TYPE IN ('STPB','STDP','STJW','RMPB','BFPB','RIMP','RMDP','SIDP','SADP')
                                AND V_NO <> @V_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";

            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@PARTY_CODE", partyCode),
                    new SqlParameter("@BILL_NO", billNo),
                    new SqlParameter("@V_NO", currentVNo),
                    new SqlParameter("@COMP_CODE", gv.PubCompCode),
                    new SqlParameter("@BRANCH_CODE", gv.PubBranchCode),
                    new SqlParameter("@YEAR_CODE", gv.PubFYearCode)
                };

            DataTable dt = await _dbHelper.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count > 0)
            {
                string docId = dt.Rows[0]["DOC_ID"].ToString();
                DateTime? vDate = dt.Rows[0]["V_DATE"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(dt.Rows[0]["V_DATE"]);

                return (true, docId, vDate);
            }

            return (false, "", null);
        }

        public async Task<RepositoryResponseData<bool>> ValidateTaxType(int billToCode, decimal totalIGST, decimal totalCGST, decimal totalSGST)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string isExistQry = $@"select 1 from SUBGROUP_MAST where nature not in ('Cash','Bank','Others') and code={billToCode} and COMP_CODE={gv.PubCompCode} 
                                        and ACTIVE=1";
            bool isExist = await _dbHelper.GetExecuteScalarAsync<int>(isExistQry) == 1;

            if (!isExist)
                return new RepositoryResponseData<bool> { status = true, data = true };

            string query = $@"SELECT State_Code FROM SUBGROUP_MAST WHERE Code = @CITY_CODE and COMP_CODE={gv.PubCompCode}";
            var parameters = new Dictionary<string, object>
                {
                    { "@CITY_CODE", billToCode }
                };

            int stateCode = await _dbHelper.GetExecuteScalarAsync<int>(query, parameters);
            string stateType = gv.STATE_CODE == stateCode.ToString() ? "Local" : "Central/Other";

            if (gv.STATE_CODE == stateCode.ToString() && totalIGST > 0)
            {
                return new RepositoryResponseData<bool> { status = true, data = false, message = $"IGST not applicable as Party State type is {stateType}." };
            }
            if (gv.STATE_CODE != stateCode.ToString() && (totalCGST + totalSGST) > 0)
            {
                return new RepositoryResponseData<bool> { status = true, data = false, message = $"CGST/SGST not applicable as Party State type is {stateType}." };
            }
            if (totalIGST > 0 && (totalCGST + totalSGST) > 0)
            {
                return new RepositoryResponseData<bool> { status = true, data = false, message = "CGST + SGST + IGST all three types of tax are not applicable." };
            }

            return new RepositoryResponseData<bool> { status = true, data = true };
        }

        public async Task<RepositoryResponseData<bool>> ValidateFreightExpense(string refType, string refVNo, string expsType)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                if (string.IsNullOrWhiteSpace(refType) || string.IsNullOrWhiteSpace(refVNo) || !int.TryParse(refVNo, out int refNo) || refNo <= 0)
                {
                    return new RepositoryResponseData<bool> { status = true, data = true };
                }

                // ---------------- SALE ----------------
                if (refType.Equals("SAGT", StringComparison.OrdinalIgnoreCase))
                {
                    if (expsType.Equals("Freight", StringComparison.OrdinalIgnoreCase))
                    {
                        string query = @"SELECT TOP 1 sauda_no FROM sale2 WHERE v_type = '{refType}' AND v_no = {refNo} AND comp_code = {gv.comp_code} AND branch_code = {gv.branch_code}";

                        string saudaNo = await _dbHelper.GetExecuteScalarAsync<string>(query);

                        if (int.TryParse(saudaNo, out int saudaNumber) && saudaNumber > 0)
                        {
                            query = @"SELECT TOP 1 ISNULL(FRT_TERM, '') FROM SAUDA WHERE v_type = 'SAUD' AND v_no = {saudaNumber} AND comp_code = {gv.comp_code} AND branch_code = {gv.branch_code} AND FRT_TERM IN ('ExWork')";

                            string frtTerm = await _dbHelper.GetExecuteScalarAsync<string>(query);

                            if (!string.IsNullOrEmpty(frtTerm))
                            {
                                return new RepositoryResponseData<bool>
                                {
                                    status = false,
                                    data = false,
                                    message = $"Freight payable by customer in Sauda No {saudaNumber}, so freight/expense can not be charged."
                                };
                            }
                        }
                    }
                }

                // ---------------- PURCHASE ----------------
                else if (
                    refType.Equals("RMPB", StringComparison.OrdinalIgnoreCase) || refType.Equals("STPB", StringComparison.OrdinalIgnoreCase) || refType.Equals("RIMP", StringComparison.OrdinalIgnoreCase) || refType.Equals("STJW", StringComparison.OrdinalIgnoreCase))
                {
                    string query = $@"SELECT TOP 1 po_no FROM purchase2 WHERE v_type = '{refType}' AND v_no = {refNo} AND comp_code = {gv.PubCompCode} AND branch_code = {gv.PubBranchCode}";

                    string orderNo = await _dbHelper.GetExecuteScalarAsync<string>(query);

                    if (int.TryParse(orderNo, out int orderNumber) && orderNumber > 0)
                    {
                        query = $@"SELECT TOP 1 po_type FROM purchase2 WHERE v_type = '{refType}' AND v_no = {refNo} AND comp_code = {gv.PubCompCode} AND branch_code = {gv.PubBranchCode}";

                        string orderType = await _dbHelper.GetExecuteScalarAsync<string>(query);

                        if (!string.IsNullOrEmpty(orderType))
                        {
                            query = $@"SELECT TOP 1 ISNULL(PRICE_TYPE, '') FROM ORDER1 WHERE v_type = '{orderType}' AND v_no = {orderNo} AND comp_code = {gv.PubCompCode} AND branch_code = {gv.PubBranchCode} AND PRICE_TYPE IN ('F.O.R.  - at our Plant')";

                            string priceType = await _dbHelper.GetExecuteScalarAsync<string>(query);

                            if (!string.IsNullOrEmpty(priceType))
                            {
                                return new RepositoryResponseData<bool>
                                {
                                    status = false,
                                    data = false,
                                    message = $"Freight payable by Supplier in Order No {orderNumber}, so freight/expense can not be charged."
                                };
                            }
                        }
                    }
                }

                return new RepositoryResponseData<bool> { status = true, data = true };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, data = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponseData<bool>> ValidateImportTracking(string refType, string refVNo, string billFromPartyCode, string billNo, string billToName)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                if (!int.TryParse(refVNo, out int refNo) || refNo <= 0 || !refType.Equals("RIMP", StringComparison.OrdinalIgnoreCase))
                {
                    return new RepositoryResponseData<bool> { status = true, data = true };
                }

                string query = $@"SELECT TOP 1 1 FROM EXIM3 WHERE CONCAT(Sauda_type, Sauda_No) IN (SELECT DISTINCT CONCAT(b.Sauda_type, b.Sauda_No) FROM PURCHASE2 a 
                                    LEFT JOIN ORDER1 b ON a.PO_TYPE = b.V_TYPE AND a.PO_NO = b.V_NO AND a.COMP_CODE = b.COMP_CODE
                                    WHERE a.V_TYPE = 'RIMP' AND a.V_NO = {refNo}) AND COMP_CODE = {gv.PubCompCode} AND BRANCH_CODE = {gv.PubBranchCode} 
                                    AND PARTY_CODE = {billFromPartyCode}";

                string result = await _dbHelper.GetExecuteScalarAsync<string>(query);

                if (string.IsNullOrEmpty(result))
                {
                    return new RepositoryResponseData<bool> { status = false, data = false, message = "Please check, Bill From Not matched as per Import Tracking record." };
                }

                query = $@"SELECT TOP 1 1 FROM EXIM3 WHERE CONCAT(Sauda_type, Sauda_No) IN (SELECT DISTINCT CONCAT(b.Sauda_type, b.Sauda_No)
                        FROM PURCHASE2 a LEFT JOIN ORDER1 b ON a.PO_TYPE = b.V_TYPE AND a.PO_NO = b.V_NO AND a.COMP_CODE = b.COMP_CODE
                        WHERE a.V_TYPE = 'RIMP' AND a.V_NO = {refNo}) AND COMP_CODE = {gv.PubCompCode} AND BRANCH_CODE = {gv.PubBranchCode} 
                        AND INV_NO = '{billNo.Trim()}'";

                result = await _dbHelper.GetExecuteScalarAsync<string>(query);

                if (string.IsNullOrEmpty(result))
                {
                    return new RepositoryResponseData<bool> { status = false, data = false, message = $"Please check, Bill no. Not matched as per Import Tracking record of Party=>{billFromPartyCode}" };
                }

                return new RepositoryResponseData<bool> { status = true, data = true };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, data = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponseData<bool>> ValidateCostAllocation(ValidateCostAllocationRequest model)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                if (!int.TryParse(model.DrActCode, out int drCode) || drCode <= 0 || !int.TryParse(model.VNo, out int voucherNo) || voucherNo <= 0)
                {
                    return new RepositoryResponseData<bool> { status = true, data = true };
                }

                //Get GR Nature
                string query = $@"SELECT type FROM GR_MAST WHERE comp_Code = {gv.PubCompCode} AND code = (SELECT GR_CODE FROM MGROUP_MAST WHERE comp_Code = {gv.PubCompCode}
                                    AND code = (SELECT GROUP_CODE FROM subgroup_mast WHERE code = {drCode} AND comp_code = {gv.PubCompCode}))";

                var grNature = Convert.ToString(await _dbHelper.ExecuteScalarAsync(query)) ?? "";

                if (!grNature.Equals("EXPENSE", StringComparison.OrdinalIgnoreCase) && !grNature.Equals("REVENUE", StringComparison.OrdinalIgnoreCase))
                {
                    return new RepositoryResponseData<bool> { status = true, data = true };
                }


                // Get Cost Allocation
                query = $@"SELECT REF_NO, SUM(ALLOCATION_AMT) AS totAmt FROM COST_ALLOCATION WHERE Ref_Type = '{model.VType}' AND Ref_No = {model.VNo} 
                        AND Comp_code = {gv.PubCompCode} AND Branch_code = {gv.PubBranchCode} AND Year_code = {gv.PubFYearCode} GROUP BY REF_NO";

                var dtc = await _dbHelper.ExecuteQueryAsync(query);

                decimal drc = ToDecimal(model.QDiffDrAmt) + ToDecimal(model.RateDiffDrAmt) + ToDecimal(model.QcDrNoteAmt) + ToDecimal(model.WgtDrNoteAmt) +
                                ToDecimal(model.OthDrNoteAmt);
                decimal crc = ToDecimal(model.QCrNoteAmt) + ToDecimal(model.RDiffCrNoteAmt) + ToDecimal(model.QcCrNoteAmt) + ToDecimal(model.WgtCrNoteAmt);

                decimal drcWT = ToDecimal(model.QDiffDrAmt) + ToDecimal(model.QDiffDrTax) + ToDecimal(model.RateDiffDrAmt) + ToDecimal(model.RateDiffDrTax) +
                                ToDecimal(model.QcDrNoteAmt) + ToDecimal(model.QcDrNoteTax) + ToDecimal(model.WgtDrNoteAmt) + ToDecimal(model.WgtDrNoteTax) +
                                ToDecimal(model.OthDrNoteAmt) + ToDecimal(model.OthDrNoteTax);

                decimal crcWT = ToDecimal(model.QCrNoteAmt) + ToDecimal(model.QCrNoteTax) + ToDecimal(model.RDiffCrNoteAmt) + ToDecimal(model.RDiffCrNoteTax) +
                                ToDecimal(model.QcCrNoteAmt) + ToDecimal(model.QcCrNoteTax) + ToDecimal(model.WgtCrNoteAmt) + ToDecimal(model.WgtCrNoteTax);

                decimal gamt;

                if (model.InputType.Equals("GST Input", StringComparison.OrdinalIgnoreCase) || model.InputType.Equals("Input GST", StringComparison.OrdinalIgnoreCase))
                {
                    gamt = ToDecimal(model.TotAmt) + ToDecimal(model.TotPacking) - ToDecimal(model.TotDisc) + crc - drc;
                }
                else
                {
                    gamt = ToDecimal(model.TotNetAmount) + crcWT - drcWT;
                }

                if (dtc == null || dtc.Rows.Count == 0)
                {
                    return new RepositoryResponseData<bool> { status = false, data = false, message = "Please Allocate this expenses in Cost Allocation." };
                }

                decimal allocatedAmount = ToDecimal(Convert.ToString(dtc.Rows[0]["totAmt"]));

                if (gamt != allocatedAmount)
                {
                    return new RepositoryResponseData<bool> { status = false, data = false, message = "Total Amount not Matched with Total Allocated Amount.\n [for GST Input : Gross Amt, Otherwise Net Amount]" };
                }

                return new RepositoryResponseData<bool> { status = true, data = true };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, data = false, message = ex.Message };
            }
        }

        private decimal ToDecimal(string value)
        {
            return decimal.TryParse(value, out decimal result) ? result : 0;
        }

        //=================Pending Approval List=============
        public RepositoryResponseData<List<ImportInvoiceListModel>> GetImportInvoiceList(int partyCode)
        {
            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();

                var gv = _globalVariableService.GetGlobalVariables();

                string qry = $@"SELECT SAUDA_NO As SaudaNo , EXPS_TYPE as Expensetype, INV_NO As InvNo , CONVERT(varchar(10), INV_DATE, 103) As InvDate , INV_AMT As InvAmt , B.NAME AS Partyname 
                                FROM EXIM3 A
                                LEFT JOIN SUBGROUP_MAST B ON B.CODE= A.PARTY_CODE  AND B.COMP_CODE = A.COMP_CODE 
                                WHERE A.PARTY_CODE = @PARTY_CODE and A.COMP_CODE = @COMP_CODE and A.BRANCH_CODE = @BRANCH_CODE and A.YEAR_CODE = @YEAR_CODE 
                                ORDER BY A.V_DATE ";

                using SqlCommand cmd = new(qry, con)
                {
                    CommandType = CommandType.Text
                };

                cmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                con.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                List<ImportInvoiceListModel> list = new();

                while (reader.Read())
                {
                    list.Add(new ImportInvoiceListModel
                    {
                        SaudaNo = reader["SaudaNo"]?.ToString(),
                        ExpenseType = reader["Expensetype"].ToString(),
                        InvNo = reader["InvNo"]?.ToString(),
                        InvDate = reader["InvDate"]?.ToString(),
                        InvAmt = reader["InvAmt"]?.ToString(),
                        PartyName = reader["Partyname"]?.ToString()
                    });
                }

                return new RepositoryResponseData<List<ImportInvoiceListModel>>
                {
                    status = true,
                    data = list
                };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<ImportInvoiceListModel>>
                {
                    status = false,
                    message = ex.Message
                };
            }
        }

        private async Task<decimal> getTotalExistingAdjustment(string vType, int VNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string qry = $@"Select Sum(isnull(ADJ_AMT,0)) From TDSLedger_OS Where DOC_TYPE='{vType}' and DOC_NO={VNo} and 
                                Comp_code={gv.PubCompCode} and Branch_code={gv.PubBranchCode}";

            decimal totalExistingAdjustment = await _dbHelper.GetExecuteScalarAsync<decimal>(qry);

            return totalExistingAdjustment;
        }
    }
}
