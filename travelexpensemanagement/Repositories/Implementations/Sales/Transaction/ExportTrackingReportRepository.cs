using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class ExportTrackingReportRepository : IExportTrackingReportRepository
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        public ExportTrackingReportRepository(DataBaseConnection dbcontext, GlobalVariableService globalValue)
        {
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        //================Export Tracking List==================
        public async Task<RepositoryResponseList<ExportTrackingReportRow>> GetReportData(DateTime fromDate, DateTime toDate, string status, int withContainerDetails)
        {
            var gv = _globalValue.GetGlobalVariables();
            var dataList = new List<ExportTrackingReportRow>();
            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_ExportTrackingReport", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "GetReportList");
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.Add("@FromDate", SqlDbType.SmallDateTime).Value = fromDate;
                        cmd.Parameters.Add("@ToDate", SqlDbType.SmallDateTime).Value = toDate;
                        cmd.Parameters.AddWithValue("@DocType", status);
                        cmd.Parameters.AddWithValue("@WithContainer", withContainerDetails);

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {

                                dataList.Add(new ExportTrackingReportRow
                                {
                                    // Dates
                                    SAUDA_DATE = SafeDate(rdr, "SAUDA_DATE"),
                                    INV_DATE = SafeDate(rdr, "INV_DATE"),
                                    SBLC_DUE_DATE = SafeDate(rdr, "SBLC_DUE_DATE"),
                                    LC_DUE_DATE = SafeDate(rdr, "LC_DUE_DATE"),
                                    LC_DATE = SafeDate(rdr, "LC_DATE"),
                                    SHIP_BILL_DATE = SafeDate(rdr, "SHIP_BILL_DATE"),
                                    BL_DATE = SafeDate(rdr, "BL_DATE"),
                                    BL_CONFIRM_DATE = SafeDate(rdr, "BL_CONFIRM_DATE"),
                                    BL_RECEIVED_DATE = SafeDate(rdr, "BL_RECEIVED_DATE"),
                                    SOB_DATE = SafeDate(rdr, "SOB_DATE"),
                                    SI_SUBMIT_DATE = SafeDate(rdr, "SI_SUBMIT_DATE"),
                                    ETA_POL_DATE = SafeDate(rdr, "ETA_POL_DATE"),
                                    ETA_POD_DATE = SafeDate(rdr, "ETA_POD_DATE"),
                                    EGM_DATE = SafeDate(rdr, "EGM_DATE"),
                                    COMMISSION_BILL_DATE = SafeDate(rdr, "COMMISSION_BILL_DATE"),
                                    BRC_DATE = SafeDate(rdr, "BRC_DATE"),
                                    DOCUMENT_LODGEMENT_DATE = SafeDate(rdr, "DOCUMENT_LODGEMENT_DATE"),

                                    // Strings
                                    SAUDA_NO = SafeString(rdr, "SAUDA_NO"),
                                    SI_NO = SafeString(rdr, "SI_NO"),
                                    PINO = SafeString(rdr, "PINO"),
                                    BUYER_ORD_NO = SafeString(rdr, "BUYER_ORD_NO"),
                                    CURRENCY = SafeString(rdr, "CURRENCY"),
                                    PARTY_NAME = SafeString(rdr, "PARTY_NAME"),
                                    LOCATION = SafeString(rdr, "LOCATION"),
                                    ITEM_TYPE = SafeString(rdr, "ITEM_TYPE"),
                                    PAYMENT_TERM = SafeString(rdr, "PAYMENT_TERM"),
                                    INCOTERM = SafeString(rdr, "INCOTERM"),
                                    SOLD_BY = SafeString(rdr, "SOLD_BY"),
                                    TRANSPORT = SafeString(rdr, "TRANSPORT"),
                                    MODE_OF_TRANSPORT = SafeString(rdr, "MODE_OF_TRANSPORT"),
                                    PLACE_OF_RECEIPT = SafeString(rdr, "PLACE_OF_RECEIPT"),
                                    POL = SafeString(rdr, "POL"),
                                    POD = SafeString(rdr, "POD"),
                                    FINAL_PLACE_OF_DELIVERY = SafeString(rdr, "FINAL_PLACE_OF_DELIVERY"),
                                    FINAL_COUNTRY = SafeString(rdr, "FINAL_COUNTRY"),
                                    FINAL_DESTINATION = SafeString(rdr, "FINAL_DESTINATION"),
                                    BL_TYPE = SafeString(rdr, "BL_TYPE"),
                                    CUSTOM_CLEARANCE_PORT = SafeString(rdr, "CUSTOM_CLEARANCE_PORT"),
                                    FIRST_NOTIFY_PARTY = SafeString(rdr, "FIRST_NOTIFY_PARTY"),
                                    SECOND_NOTIFY_PARTY = SafeString(rdr, "SECOND_NOTIFY_PARTY"),
                                    COMMISSION_AGENT = SafeString(rdr, "COMMISSION_AGENT"),
                                    LC_NUMBER = SafeString(rdr, "LC_NUMBER"),
                                    BANK_NAME = SafeString(rdr, "BANK_NAME"),
                                    ACCOUNT_NUMBER = SafeString(rdr, "ACCOUNT_NUMBER"),
                                    IFSC_CODE = SafeString(rdr, "IFSC_CODE"),
                                    AD_CODE = SafeString(rdr, "AD_CODE"),
                                    SWIFT_CODE = SafeString(rdr, "SWIFT_CODE"),
                                    BANK_BRANCH = SafeString(rdr, "BANK_BRANCH"),
                                    NATURE_KIND_OF_PACKING = SafeString(rdr, "NATURE_KIND_OF_PACKING"),
                                    TECHNICAL_SPECIFICATION = SafeString(rdr, "TECHNICAL_SPECIFICATION"),
                                    SPECIAL_INSTRUCTION = SafeString(rdr, "SPECIAL_INSTRUCTION"),
                                    FORWARDER = SafeString(rdr, "FORWARDER"),
                                    CHA = SafeString(rdr, "CHA"),
                                    SHIPPING_LINE = SafeString(rdr, "SHIPPING_LINE"),
                                    SHIP_BILL_NO = SafeString(rdr, "SHIP_BILL_NO"),
                                    BILL_BOOKING_NO = SafeString(rdr, "BILL_BOOKING_NO"),
                                    BL_NO = SafeString(rdr, "BL_NO"),
                                    BL_REMARKS = SafeString(rdr, "BL_REMARKS"),
                                    BL_LAST_ACTIVITY = SafeString(rdr, "BL_LAST_ACTIVITY"),
                                    VESSEL_NO = SafeString(rdr, "VESSEL_NO"),
                                    VYOG_NO = SafeString(rdr, "VYOG_NO"),
                                    EPCG_LIC_NO = SafeString(rdr, "EPCG_LIC_NO"),
                                    EXPORT_PER_NO = SafeString(rdr, "EXPORT_PER_NO"),
                                    EGM_NO = SafeString(rdr, "EGM_NO"),
                                    ADV_LIC_NO = SafeString(rdr, "ADV_LIC_NO"),
                                    TAX_TYPE = SafeString(rdr, "TAX_TYPE"),
                                    ADV_PAYMENT_AMT_USD = SafeString(rdr, "ADV_PAYMENT_AMT_USD"),
                                    ADV_RECEIVED_DATE = SafeString(rdr, "ADV_RECEIVED_DATE"),
                                    ADV_PAYMENT_REF_NO = SafeString(rdr, "ADV_PAYMENT_REF_NO"),
                                    BALANCE_PAYMENT_AMT = SafeString(rdr, "BALANCE_PAYMENT_AMT"),
                                    BALANCE_PAYMENT_RECEIVED_DATE = SafeString(rdr, "BALANCE_PAYMENT_RECEIVED_DATE"),
                                    BALANCE_PAYMENT_REF_NO = SafeString(rdr, "BALANCE_PAYMENT_REF_NO"),
                                    COMMISSION_BILL_NO = SafeString(rdr, "COMMISSION_BILL_NO"),
                                    COMMISSION_BILL_STATUS = SafeString(rdr, "COMMISSION_BILL_STATUS"),
                                    BRC_NUMBER = SafeString(rdr, "BRC_NUMBER"),
                                    DOCUMENT_LODGEMENT_NO = SafeString(rdr, "DOCUMENT_LODGEMENT_NO"),

                                    PACKING_SLIP_NO = withContainerDetails == 1 ? SafeString(rdr, "PACKING_SLIP_NO") : "",
                                    CONTAINER_NO = withContainerDetails == 1 ? SafeString(rdr, "CONTAINER_NO") : "",
                                    LINE_SEAL_NO = withContainerDetails == 1 ? SafeString(rdr, "LINE_SEAL_NO") : "",
                                    CUSTOM_SEAL_NO = withContainerDetails == 1 ? SafeString(rdr, "CUSTOM_SEAL_NO") : "",

                                    // Decimals
                                    SAUDA_QTY = SafeDecimal(rdr, "SAUDA_QTY"),
                                    INV_QTY = SafeDecimal(rdr, "INV_QTY"),
                                    EXCHANGE_RATE = SafeDecimal(rdr, "EXCHANGE_RATE"),
                                    RATE_KG_IN_USD = SafeDecimal(rdr, "RATE_KG_IN_USD"),
                                    BASIC_AMOUNT = SafeDecimal(rdr, "BASIC_AMOUNT"),
                                    USD_AMOUNT = SafeDecimal(rdr, "USD_AMOUNT"),
                                    DISCOUNT = SafeDecimal(rdr, "DISCOUNT"),
                                    ACTUAL_FREIGHT = SafeDecimal(rdr, "ACTUAL_FREIGHT"),
                                    INSURANCE = SafeDecimal(rdr, "INSURANCE"),
                                    COMMISSION = SafeDecimal(rdr, "COMMISSION"),
                                    FOB_VALUE = SafeDecimal(rdr, "FOB_VALUE"),
                                    CGST = SafeDecimal(rdr, "CGST"),
                                    SGST = SafeDecimal(rdr, "SGST"),
                                    IGST = SafeDecimal(rdr, "IGST"),
                                    TOTAL_SB_DDBK = SafeDecimal(rdr, "TOTAL_SB_DDBK"),
                                    TOTAL_SB_RODTAP = SafeDecimal(rdr, "TOTAL_SB_RODTAP")
                                });

                            }
                        }
                    }
                }

                return new RepositoryResponseList<ExportTrackingReportRow> { status = true, data = dataList };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<ExportTrackingReportRow> { status = false, message = ex.Message };
            }
        }

        // ---- Safe conversion helpers ----

        private static DateTime? SafeDate(SqlDataReader rdr, string col)
        {
            if (rdr[col] == DBNull.Value) return null;
            var raw = rdr[col].ToString();
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return DateTime.TryParse(raw, out var dt) ? dt : (DateTime?)null;
        }

        private static decimal SafeDecimal(SqlDataReader rdr, string col)
        {
            if (rdr[col] == DBNull.Value) return 0;
            var raw = rdr[col].ToString();
            if (string.IsNullOrWhiteSpace(raw)) return 0;
            return decimal.TryParse(raw, out var val) ? val : 0;
        }

        private static string SafeString(SqlDataReader rdr, string col, string fallback = "")
        {
            if (rdr[col] == DBNull.Value) return fallback;
            var raw = rdr[col].ToString();
            // treat literal "NULL" text (sometimes returned by SPs) as empty too
            return string.Equals(raw, "NULL", StringComparison.OrdinalIgnoreCase) ? fallback : raw;
        }
    }
}
