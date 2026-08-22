using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class ImportPaymentEntryRepository : IImportPaymentEntryRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public ImportPaymentEntryRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _logService = logService;
        }

        public async Task<(bool Success, string Message)> SaveImportPaymentEntryAsync(ImportPaymentEntryModel.SaveImportPaymentEntry model)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            if (model == null)
            {
                return (false, "Request model is null.");
            }

            string docId = model.Header.V_TYPE + model.Header.V_NO;
            bool isUpdate = !string.IsNullOrWhiteSpace(model.Header.DOC_ID);

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    SqlCommand cmd = new SqlCommand("dbo.sp_ImportPaymentEntry", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", model.Header.V_TYPE);
                    cmd.Parameters.AddWithValue("@V_NO", model.Header.V_NO);
                    cmd.Parameters.AddWithValue("@V_DATE", model.Header.V_DATE);
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", model.Header.PARTY_CODE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PAY_TYPE", model.Header.PAY_TYPE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BANK_CODE", model.Header.BANK_CODE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IMPORT_CAT", model.Header.IMPORT_CAT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IMPORT_REMIT", model.Header.IMPORT_REMIT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ITEM_CAT", model.Header.ITEM_CAT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CURRENCY", model.Header.CURRENCY ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@TOT_AMT", model.Header.TOT_AMT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FOREIGN_BANKCHARGE", model.Header.FOREIGN_BANKCHARGE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_BANK", model.Header.BENI_BANK ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_ACTNO", model.Header.BENI_ACTNO ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_SWIFT", model.Header.BENI_SWIFT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_ABA", model.Header.BENI_ABA ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_ROUT", model.Header.BENI_ROUT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_SC", model.Header.BENI_SC ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_BANKADD", model.Header.BENI_BANKADD ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_BANK", model.Header.CORR_BANK ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_ACTNO", model.Header.CORR_ACTNO ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_SWIFT", model.Header.CORR_SWIFT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_ABA", model.Header.CORR_ABA ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_ROUT", model.Header.CORR_ROUT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_SC", model.Header.CORR_SC ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_BANKADD", model.Header.CORR_BANKADD ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@DOC_EVEDENCE", model.Header.DOC_EVEDENCE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@INTRATE_APPL", model.Header.INTRATE_APPL ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ROI", model.Header.ROI ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ROI_PERIOD", model.Header.ROI_PERIOD ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@SPFC_BANK", model.Header.SPFC_BANK ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@SPFC_BANKNAME", model.Header.SPFC_BANKNAME ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_BILLREFNO", model.Header.CD_BILLREFNO ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_CCY", model.Header.CD_CCY ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_AMTREMITT", model.Header.CD_AMTREMITT ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@CDFEMA_NC", model.Header.CDFEMA_NC ?? 0);
                    cmd.Parameters.AddWithValue("@CDFEMA_RES", model.Header.CDFEMA_RES ?? 0);

                    cmd.Parameters.AddWithValue("@CD_ATTCH1", model.Header.CD_ATTCH1 ?? 0);
                    cmd.Parameters.AddWithValue("@CD_ATTCH2", model.Header.CD_ATTCH2 ?? 0);
                    cmd.Parameters.AddWithValue("@CD_ATTCH3", model.Header.CD_ATTCH3 ?? 0);
                    cmd.Parameters.AddWithValue("@CD_ATTCH4", model.Header.CD_ATTCH4 ?? 0);
                    cmd.Parameters.AddWithValue("@CD_ATTCH5", model.Header.CD_ATTCH5 ?? 0);
                    cmd.Parameters.AddWithValue("@CD_ATTCH6", model.Header.CD_ATTCH6 ?? 0);
                    cmd.Parameters.AddWithValue("@CD_ATTCH7", model.Header.CD_ATTCH7 ?? 0);
                    cmd.Parameters.AddWithValue("@CD_ATTCH8", model.Header.CD_ATTCH8 ?? 0);
                    cmd.Parameters.AddWithValue("@CD_ATTCH9", model.Header.CD_ATTCH9 ?? 0);

                    cmd.Parameters.AddWithValue("@A2_ISSUEDRAFT", model.Header.A2_ISSUEDRAFT ?? 0);
                    cmd.Parameters.AddWithValue("@A2_FEREFFECT", model.Header.A2_FEREFFECT ?? 0);
                    cmd.Parameters.AddWithValue("@A2_BENIFICIARY", model.Header.A2_BENIFICIARY ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_ACTNO", model.Header.A2_ACTNO ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_NAMEADD", model.Header.A2_NAMEADD ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_ISSUETRAVELLER", model.Header.A2_ISSUETRAVELLER ?? 0);
                    cmd.Parameters.AddWithValue("@A2_ITFOR", model.Header.A2_ITFOR ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_FCN", model.Header.A2_FCN ?? 0);
                    cmd.Parameters.AddWithValue("@A2_FCNFOR", model.Header.A2_FCNFOR ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_AMOUNT", model.Header.A2_AMOUNT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_LRS", model.Header.A2_LRS ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_PC", model.Header.A2_PC ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_DESC", model.Header.A2_DESC ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_PURPOSE", model.Header.ECB_PURPOSE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_LENDER", model.Header.ECB_LENDER ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NAMEADD", model.Header.ECB_NAMEADD ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@ECB_NATURE1", model.Header.ECB_NATURE1 ?? 0);
                    cmd.Parameters.AddWithValue("@ECB_NATURE2", model.Header.ECB_NATURE2 ?? 0);
                    cmd.Parameters.AddWithValue("@ECB_NATURE3", model.Header.ECB_NATURE3 ?? 0);
                    cmd.Parameters.AddWithValue("@ECB_NATURE4", model.Header.ECB_NATURE4 ?? 0);
                    cmd.Parameters.AddWithValue("@ECB_NATURE5", model.Header.ECB_NATURE5 ?? 0);
                    cmd.Parameters.AddWithValue("@ECB_NATURE6", model.Header.ECB_NATURE6 ?? 0);
                    cmd.Parameters.AddWithValue("@ECB_NATURE7", model.Header.ECB_NATURE7 ?? 0);
                    cmd.Parameters.AddWithValue("@ECB_NATURE8", model.Header.ECB_NATURE8 ?? 0);
                    cmd.Parameters.AddWithValue("@ECB_NATURE9", model.Header.ECB_NATURE9 ?? 0);
                    cmd.Parameters.AddWithValue("@ECB_NATURE10", model.Header.ECB_NATURE10 ?? 0);

                    cmd.Parameters.AddWithValue("@ECB_ROI", model.Header.ECB_ROI ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_UPFRONTFEE", model.Header.ECB_UPFRONTFEE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_MGMTFEE", model.Header.ECB_MGMTFEE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_OTHCH", model.Header.ECB_OTHCH ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_ALLINCOST", model.Header.ECB_ALLINCOST ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_COMMITMENTFEE", model.Header.ECB_COMMITMENTFEE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_ROPI", model.Header.ECB_ROPI ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_PERIOD", model.Header.ECB_PERIOD ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_CALLPUT", model.Header.ECB_CALLPUT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_GRACE", model.Header.ECB_GRACE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_REPAYTERM", model.Header.ECB_REPAYTERM ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_AVGMATURITY", model.Header.ECB_AVGMATURITY ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NATUREOFSEC", model.Header.ECB_NATUREOFSEC ?? (object)DBNull.Value);

                    if (model.Header.PCD_DDMONTH.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@PCD_DDMONTH", model.Header.PCD_DDMONTH.Value);
                        cmd.Parameters.AddWithValue("@PCD_DDAMT", model.Header.PCD_DDAMT ?? 0);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@PCD_DDMONTH", DBNull.Value);
                        cmd.Parameters.AddWithValue("@PCD_DDAMT", 0);
                    }

                    if (model.Header.PCD_RPMONTH.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@PCD_RPMONTH", model.Header.PCD_RPMONTH.Value);
                        cmd.Parameters.AddWithValue("@PCD_RPAMT", model.Header.PCD_RPAMT ?? 0);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@PCD_RPMONTH", DBNull.Value);
                        cmd.Parameters.AddWithValue("@PCD_RPAMT", 0);
                    }
                    if (model.Header.PCD_IPMONTH.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@PCD_IPMONTH", model.Header.PCD_IPMONTH.Value);
                        cmd.Parameters.AddWithValue("@PCD_IPAMT", model.Header.PCD_IPAMT ?? 0);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@PCD_IPMONTH", DBNull.Value);
                        cmd.Parameters.AddWithValue("@PCD_IPAMT", 0);
                    }

                    cmd.Parameters.AddWithValue("@PCD_NAMELOC", model.Header.PCD_NAMELOC ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PCD_TOTALCOST", model.Header.PCD_TOTALCOST ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PCD_PERCOST", model.Header.PCD_PERCOST ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PCD_PIBANKAPPL", model.Header.PCD_PIBANKAPPL ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@PCD_IS1", model.Header.PCD_IS1 ?? 0);
                    cmd.Parameters.AddWithValue("@PCD_IS2", model.Header.PCD_IS2 ?? 0);
                    cmd.Parameters.AddWithValue("@PCD_IS3", model.Header.PCD_IS3 ?? 0);
                    cmd.Parameters.AddWithValue("@PCD_IS4", model.Header.PCD_IS4 ?? 0);
                    cmd.Parameters.AddWithValue("@PCD_IS5", model.Header.PCD_IS5 ?? 0);
                    cmd.Parameters.AddWithValue("@PCD_IS6", model.Header.PCD_IS6 ?? 0);
                    cmd.Parameters.AddWithValue("@PCD_IS7", model.Header.PCD_IS7 ?? 0);

                    cmd.Parameters.AddWithValue("@PCD_REQSA", model.Header.PCD_REQSA ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PCD_AUTHORITY", model.Header.PCD_AUTHORITY ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PCD_CLNO", model.Header.PCD_CLNO ?? (object)DBNull.Value);

                    if (model.Header.PCD_CLDATE.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@PCD_CLDATE", model.Header.PCD_CLDATE.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@PCD_CLDATE", DBNull.Value);
                    }
                    cmd.Parameters.AddWithValue("@REMARKS", model.Header.REMARKS ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CLEARANCE_NO", model.Header.CLEARANCE_NO ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@OTHDOC_DETAILS", model.Header.OTHDOC_DETAILS ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@Action", isUpdate ? "UpdateHeader" : "InsertHeader");
                    cmd.ExecuteNonQuery();

                    if (isUpdate)
                    {
                        using (SqlCommand deleteFooterCmd = new SqlCommand("dbo.sp_ImportPaymentEntry", con))
                        {
                            deleteFooterCmd.CommandType = CommandType.StoredProcedure;

                            deleteFooterCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            deleteFooterCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                            deleteFooterCmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                            deleteFooterCmd.Parameters.AddWithValue("@DOC_ID", docId);

                            deleteFooterCmd.Parameters.AddWithValue("@Action", "DeleteFooter");

                            deleteFooterCmd.ExecuteNonQuery();
                        }
                    }

                    foreach (var item in model.Footer)
                    {
                        SqlCommand footerCmd = new SqlCommand("dbo.sp_ImportPaymentEntry", con);
                        footerCmd.CommandType = CommandType.StoredProcedure;

                        footerCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        footerCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        footerCmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        footerCmd.Parameters.AddWithValue("@V_TYPE", model.Header.V_TYPE);
                        footerCmd.Parameters.AddWithValue("@V_NO", model.Header.V_NO);
                        footerCmd.Parameters.AddWithValue("@V_DATE", model.Header.V_DATE);
                        footerCmd.Parameters.AddWithValue("@DOC_ID", docId);
                        footerCmd.Parameters.AddWithValue("@PO_TYPE", item.PO_TYPE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@PO_NO", item.PO_NO ?? (object)DBNull.Value);
                        if (item.PO_DATE.HasValue)
                        {
                            footerCmd.Parameters.AddWithValue("@PO_DATE", item.PO_DATE.Value);
                        }
                        else
                        {
                            footerCmd.Parameters.AddWithValue("@PO_DATE", DBNull.Value);
                        }

                        footerCmd.Parameters.AddWithValue("@INV_NO", item.INV_NO ?? (object)DBNull.Value);

                        if (item.INV_DATE.HasValue)
                        {
                            footerCmd.Parameters.AddWithValue("@INV_DATE", item.INV_DATE.Value);
                        }
                        else
                        {
                            footerCmd.Parameters.AddWithValue("@INV_DATE", DBNull.Value);
                        }
                        footerCmd.Parameters.AddWithValue("@AMOUNT", item.AMOUNT ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@QTY", item.QTY ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@ITEM_CODE", item.ITEM_CODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@ITEM_NAME", item.ITEM_NAME ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@HSN_CODE", item.HSN_CODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@COUNTRY_ORIGIN", item.COUNTRY_ORIGIN ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@SHIPMENT_MODE", item.SHIPMENT_MODE ?? (object)DBNull.Value);
                        if (item.SHIPMENT_DATE.HasValue)
                        {
                            footerCmd.Parameters.AddWithValue("@SHIPMENT_DATE", item.SHIPMENT_DATE.Value);
                        }
                        else
                        {
                            footerCmd.Parameters.AddWithValue("@SHIPMENT_DATE", DBNull.Value);
                        }

                        if (item.EXPECTED_DOD.HasValue)
                        {
                            footerCmd.Parameters.AddWithValue("@EXPECTED_DOD", item.EXPECTED_DOD.Value);
                        }
                        else
                        {
                            footerCmd.Parameters.AddWithValue("@EXPECTED_DOD", DBNull.Value);
                        }
                        footerCmd.Parameters.AddWithValue("@SHIPCOMP_CODE", item.SHIPCOMP_CODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@SHIPPING_COMP", item.SHIPPING_COMP ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@POD_CODE", item.POD_CODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@POD", item.POD ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@DEST_PORTCODE", item.DEST_PORTCODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@DEST_PORT", item.DEST_PORT ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@BL_NO", item.BL_NO ?? (object)DBNull.Value);
                        if (item.BL_DATE.HasValue)
                        {
                            footerCmd.Parameters.AddWithValue("@BL_DATE", item.BL_DATE.Value);
                        }
                        else
                        {
                            footerCmd.Parameters.AddWithValue("@BL_DATE", DBNull.Value);
                        }

                        footerCmd.Parameters.AddWithValue("@BE_NO", item.BE_NO ?? (object)DBNull.Value);
                        if (item.BE_DATE.HasValue)
                        {
                            footerCmd.Parameters.AddWithValue("@BE_DATE", item.BE_DATE.Value);
                        }
                        else
                        {
                            footerCmd.Parameters.AddWithValue("@BE_DATE", DBNull.Value);
                        }
                        footerCmd.Parameters.AddWithValue("@BE_CCYNO", item.BE_CCYNO ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@BE_AMT", item.BE_AMT ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@BE_UTIAMT", item.BE_UTIAMT ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@FOB_VALUE", item.FOB_VALUE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@AD_CODE", item.AD_CODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@PORT_CODE", item.PORT_CODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@ITEM_DESC", item.ITEM_DESC ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                        footerCmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                        footerCmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                        footerCmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        footerCmd.Parameters.AddWithValue("@Action", "InsertFooter");
                                  
                        footerCmd.ExecuteNonQuery();
                    }

                }
                return (true, isUpdate ? "Data updated successfully." : "Data saved successfully.");

            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(Dictionary<string, object> Header, List<Dictionary<string, object>> Footer)> LoadEditDataAsync(string docId)
        {
            if (string.IsNullOrWhiteSpace(docId))
            {
                throw new ArgumentException("DOC_ID is required.", nameof(docId));
            }

            var gv = _globalVariableService.GetGlobalVariables();

            using SqlConnection con = _dbConnection.GetErpConnection();

            using SqlCommand cmd = new SqlCommand("sp_ImportPaymentEntry", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Action", "LoadEditData");
            cmd.Parameters.AddWithValue("@DOC_ID", docId);

            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

            await con.OpenAsync();

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            var header = new Dictionary<string, object>();
            var footer = new List<Dictionary<string, object>>();

            // =========================
            // HEADER
            // =========================

            if (await reader.ReadAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    header[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
            }

            // =========================
            // FOOTER
            // =========================

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }

                    footer.Add(row);
                }
            }

            return (header, footer);
            
        }

    }
}
