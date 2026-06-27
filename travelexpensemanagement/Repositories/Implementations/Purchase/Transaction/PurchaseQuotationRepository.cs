using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.AddAttachmentService;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transiction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class PurchaseQuotationRepository : IPurchaseQuotationRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        public PurchaseQuotationRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, GlobalValidationdate globalValidationdate,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
        }

        public string GenerateVNo(string vType)
        {
            string newV_NO = "00001";

            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string prefixYRQuery =@"SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";

                SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);

                prefixCmd.Parameters.AddWithValue("@YearCode",getdata.PubFYearCode);

                string prefixYR =prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                string lastV_NO_Query = @"SELECT ISNULL(MAX(CAST(RIGHT(V_NO,5) AS INT)),0)+1 FROM QUOTATION1 WHERE V_TYPE=@V_TYPE AND COMP_CODE=@COMP_CODE  AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE";

                SqlCommand cmd = new SqlCommand(lastV_NO_Query, con);

                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);

                int nextNo = Convert.ToInt32(cmd.ExecuteScalar());

                newV_NO = prefixYR + nextNo.ToString("D5");
            }

            return newV_NO;
        }

        public async Task<object> GetFullQuotationByVno(int vNo, string vType)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            QUOTATION1 header = null;
            List<QUOTATION2> items = new();
            List<QUOTATION3> attachments = new();

            try
            {
                using SqlConnection conn = _dbConnection.GetErpConnection();
                using SqlCommand cmd = new("sp_QUOTATION1_MGMT", conn);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "SELECT");
                cmd.Parameters.AddWithValue("@SubAction", "GETALLBYVNO");
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);

                await conn.OpenAsync();

                using SqlDataReader rdr = await cmd.ExecuteReaderAsync();

                // Header Read
                if (rdr.Read())
                {
                    header = new QUOTATION1
                    {
                        YEAR_CODE = rdr["YEAR_CODE"] as int? ?? 0,
                        COMP_CODE = rdr["COMP_CODE"] as int? ?? 0,
                        BRANCH_CODE = rdr["BRANCH_CODE"] as int? ?? 0,
                        V_TYPE = rdr["V_TYPE"]?.ToString(),
                        V_NO = rdr["V_NO"] as int? ?? 0,
                        V_DATE = rdr["V_DATE"] as DateTime? ?? DateTime.MinValue,
                        PARTY_CODE = rdr["PARTY_CODE"] as int? ?? 0,
                        OLD_NO = rdr["OLD_NO"] as int? ?? 0,
                        QUOTE_NO = rdr["QUOTE_NO"]?.ToString(),
                        QUOTE_DATE = rdr["QUOTE_DATE"] as DateTime? ?? DateTime.MinValue,
                        CONT_PERSON = rdr["CONT_PERSON"]?.ToString(),
                        VALID_DATE = rdr["VALID_DATE"] as DateTime? ?? DateTime.MinValue,
                        REMARKS = rdr["REMARKS"]?.ToString(),
                        PRICE_TYPE = rdr["PRICE_TYPE"]?.ToString(),
                        STATUS = rdr["STATUS"] as int? ?? 0,
                        QTY = rdr["QTY"] as decimal? ?? 0,
                        AMOUNT = rdr["AMOUNT"] as decimal? ?? 0,
                        FREIGHT_AMT = rdr["FREIGHT_AMT"] as decimal? ?? 0,
                        GROUP_NO = rdr["GROUP_NO"] as int? ?? 0,
                        DELIVERY_TERM = rdr["DELIVERY_TERM"]?.ToString(),
                        FREIGHT_TERM = rdr["FREIGHT_TERM"]?.ToString(),
                        PAYTERM_CODE = rdr["PAYTERM_CODE"] as int? ?? 0,
                        PAYMENT_TERM = rdr["PAYMENT_TERM"]?.ToString(),
                        PACK_AMT = rdr["PACK_AMT"] as decimal? ?? 0,
                        DISC_AMT = rdr["DISC_AMT"] as decimal? ?? 0,
                        CGST_AMT = rdr["CGST_AMT"] as decimal? ?? 0,
                        SGST_AMT = rdr["SGST_AMT"] as decimal? ?? 0,
                        IGST_AMT = rdr["IGST_AMT"] as decimal? ?? 0,
                        VAT_AMT = rdr["VAT_AMT"] as decimal? ?? 0,
                        OTH_AMT = rdr["OTH_AMT"] as decimal? ?? 0,
                        CESS_AMT = rdr["CESS_AMT"] as decimal? ?? 0,
                        NET_AMT = rdr["NET_AMT"] as decimal? ?? 0,
                        BULK_QTY = rdr["BULK_QTY"] as decimal? ?? 0,
                        BULK_DISCAMT = rdr["BULK_DISCAMT"] as decimal? ?? 0,
                        DOC_ID = rdr["DOC_ID"]?.ToString(),
                        FAPROV_STATUS = rdr["FAPROV_STATUS"]?.ToString(),
                        FAPROV_REMARKS = rdr["FAPROV_REMARKS"]?.ToString(),
                        MAILSEND = rdr["MAILSEND"] as int? ?? 0,
                        SRNO = rdr["SRNO"] as int? ?? 0,
                        IMPORT_CURRENCY = rdr["IMPORT_CURRENCY"]?.ToString(),
                        EXRATE = rdr["EXRATE"] as decimal? ?? 0
                    };
                }

                // Items Read
                if (rdr.NextResult())
                {
                    while (rdr.Read())
                    {
                        items.Add(new QUOTATION2
                        {
                            DOC_ID = rdr["DOC_ID"]?.ToString(),
                            YEAR_CODE = rdr["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["YEAR_CODE"]) : 0,
                            COMP_CODE = rdr["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COMP_CODE"]) : 0,
                            //COMP_NAME = compName ,
                            BRANCH_CODE = rdr["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BRANCH_CODE"]) : 0,
                            V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                            V_TYPE = rdr["V_TYPE"]?.ToString(),
                            V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                            PARTY_CODE = rdr["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_CODE"]) : 0,
                            ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                            MAKE_CODE = rdr["MAKE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MAKE_CODE"]) : 0,
                            TECH_DESC = rdr["TECH_DESC"]?.ToString(),
                            UOM_CODE = rdr["UOM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["UOM_CODE"]) : 0,
                            REF_NO = rdr["REF_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REF_NO"]) : 0,
                            REF_DATE = rdr["REF_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["REF_DATE"]) : DateTime.MinValue,
                            REF_TYPE = rdr["REF_TYPE"]?.ToString(),
                            REF_DOCID = rdr["REF_DOCID"]?.ToString(),
                            QTY = rdr["QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY"]) : 0,
                            RATE = rdr["RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE"]) : 0,
                            IMPORT_RATE = rdr["IMPORT_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["IMPORT_RATE"]) : 0,
                            AMOUNT = rdr["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["AMOUNT"]) : 0,
                            PACK_PER = rdr["PACK_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_PER"]) : 0,
                            PACK_AMT = rdr["PACK_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_AMT"]) : 0,
                            DISC_PER = rdr["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_PER"]) : 0,
                            DISC_AMT = rdr["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_AMT"]) : 0,
                            FREIGHT = rdr["FREIGHT"] != DBNull.Value ? Convert.ToDecimal(rdr["FREIGHT"]) : 0,
                            TAX_CODE = rdr["TAX_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["TAX_CODE"]) : 0,
                            CGST_PER = rdr["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_PER"]) : 0,
                            CGST_AMT = rdr["CGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_AMT"]) : 0,
                            SGST_PER = rdr["SGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_PER"]) : 0,
                            SGST_AMT = rdr["SGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_AMT"]) : 0,
                            IGST_PER = rdr["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_PER"]) : 0,
                            IGST_AMT = rdr["IGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_AMT"]) : 0,
                            VAT_PER = rdr["VAT_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["VAT_PER"]) : 0,
                            VAT_AMT = rdr["VAT_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["VAT_AMT"]) : 0,
                            CESS_PER = rdr["CESS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_PER"]) : 0,
                            CESS_AMT = rdr["CESS_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_AMT"]) : 0,
                            OTH_EXPS = rdr["OTH_EXPS"] != DBNull.Value ? Convert.ToDecimal(rdr["OTH_EXPS"]) : 0,
                            LD_RATE = rdr["LD_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["LD_RATE"]) : 0,
                            NET_AMT = rdr["NET_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["NET_AMT"]) : 0,
                            BULK_QTY = rdr["BULK_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["BULK_QTY"]) : 0,
                            BULK_RATE = rdr["BULK_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["BULK_RATE"]) : 0,
                            BULK_DISC_PER = rdr["BULK_DISC_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["BULK_DISC_PER"]) : 0,
                            BULK_DISC_AMT = rdr["BULK_DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["BULK_DISC_AMT"]) : 0,
                            WARRANTY = rdr["WARRANTY"]?.ToString(),
                            LEADTIME_DAYS = rdr["LEADTIME_DAYS"] != DBNull.Value ? Convert.ToInt32(rdr["LEADTIME_DAYS"]) : 0,
                            PURCHASER_REMARKS = rdr["PURCHASER_REMARKS"]?.ToString(),
                            PREORITY_LEVEL = rdr["PREORITY_LEVEL"] != DBNull.Value ? Convert.ToInt32(rdr["PREORITY_LEVEL"]) : 0,
                            RATE_MONTHLY = rdr["RATE_MONTHLY"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_MONTHLY"]) : 0,
                            RATE_QUARTERLY = rdr["RATE_QUARTERLY"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_QUARTERLY"]) : 0,
                            RATE_ANNUALY = rdr["RATE_ANNUALY"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_ANNUALY"]) : 0,
                            RATE_SPECIAL = rdr["RATE_SPECIAL"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_SPECIAL"]) : 0,
                            REQ_TYPE = rdr["REQ_TYPE"]?.ToString(),
                            REQ_NO = rdr["REQ_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REQ_NO"]) : 0,
                            STATUS = rdr["STATUS"] != DBNull.Value ? Convert.ToInt32(rdr["STATUS"]) : 1,
                            APROV_CODE = rdr["APROV_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["APROV_CODE"]) : 0,
                            APROV_STATUS = rdr["APROV_STATUS"]?.ToString(),
                            APROV_REMARKS = rdr["APROV_REMARKS"]?.ToString(),
                            FAPROV_STATUS = rdr["FAPROV_STATUS"]?.ToString(),
                            FAPROV_REMARKS = rdr["FAPROV_REMARKS"]?.ToString(),
                            PACK_UR = rdr["PACK_UR"]?.ToString(),
                            DISC_UR = rdr["DISC_UR"]?.ToString(),
                            FREIGHT_UR = rdr["FREIGHT_UR"]?.ToString(),
                            CGST_UR = rdr["CGST_UR"]?.ToString(),
                            SGST_UR = rdr["SGST_UR"]?.ToString(),
                            IGST_UR = rdr["IGST_UR"]?.ToString(),
                            OTHEXP_UR = rdr["OTHEXP_UR"]?.ToString(),
                            BULKDISC_UR = rdr["BULKDISC_UR"]?.ToString(),
                            AUTOPO_FLG = rdr["AUTOPO_FLG"] != DBNull.Value ? Convert.ToInt32(rdr["AUTOPO_FLG"]) : 0,
                            UUSER = rdr["UUSER"] != DBNull.Value ? Convert.ToInt32(rdr["UUSER"]) : 0,
                            UDATE = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : DateTime.MinValue,
                            EUSER = rdr["EUSER"] != DBNull.Value ? Convert.ToInt32(rdr["EUSER"]) : 0,
                            EDATE = rdr["EDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EDATE"]) : DateTime.MinValue,
                            AED = rdr["AED"]?.ToString(),
                            WSID = rdr["WSID"]?.ToString(),
                            LIP = rdr["LIP"]?.ToString(),
                            LID = rdr["LID"]?.ToString(),
                            SRNO = rdr["SRNO"] != DBNull.Value ? Convert.ToInt32(rdr["SRNO"]) : 0
                        });
                    }
                }

                // Attachment Read
                if (rdr.NextResult())
                {
                    while (rdr.Read())
                    {
                        attachments.Add(new QUOTATION3
                        {
                            DOC_ID = rdr["DOC_ID"]?.ToString(),
                            YEAR_CODE = rdr["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["YEAR_CODE"]) : 0,
                            COMP_CODE = rdr["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COMP_CODE"]) : 0,
                            BRANCH_CODE = rdr["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BRANCH_CODE"]) : 0,
                            V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                            V_TYPE = rdr["V_TYPE"]?.ToString(),
                            ATTACHMENT = rdr["ATTACHMENT"]?.ToString() ?? string.Empty,
                            UUSER = rdr["UUSER"] != DBNull.Value ? Convert.ToInt32(rdr["UUSER"]) : 0,
                            UDATE = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : DateTime.MinValue,
                            EUSER = rdr["EUSER"] != DBNull.Value ? Convert.ToInt32(rdr["EUSER"]) : 0,
                            EDATE = rdr["EDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EDATE"]) : DateTime.MinValue,
                            AED = rdr["AED"]?.ToString(),
                            WSID = rdr["WSID"]?.ToString(),
                            LIP = rdr["LIP"]?.ToString(),
                            LID = rdr["LID"]?.ToString(),
                            SRNO = rdr["SRNO"] != DBNull.Value ? Convert.ToInt32(rdr["SRNO"]) : 0,
                            ATTACHMENT_FILE = rdr["ATTACHMENT_FILE"] != DBNull.Value ? Convert.ToBase64String((byte[])rdr["ATTACHMENT_FILE"]) : null,
                        });
                    }
                }

                return new { success = true, header, items,attachments };
            }
            catch (Exception ex)
            {
                return new { success = false, message = "Error fetching quotation", error = ex.Message };
            }
        }

        public async Task<(bool Success, string Message)> SaveQuotation(QuotationWrapper data)
        {
            if (data.header == null)
                return (false, "HEADER IS NULL");

            if (data.lineRows == null)
                return (false, "Line items missing");

            var validationResult = await ValidateQuotationAsync(
                data.header,
                data.lineRows,
                data.Attachement);

            if (!validationResult.IsValid)
                return (false, validationResult.Message);

            var model = data.header;

            int vNo;
            string subAction;

            if (model.AED == "D")
            {
                subAction = "INSERT";

                string vNoStr = GenerateVNo(model.V_TYPE);

                vNo = Convert.ToInt32(vNoStr);
                model.V_NO = vNo;
            }
            else if (model.V_NO > 0 && model.AED == "E")
            {
                subAction = "UPDATE";
                vNo = model.V_NO.Value;
            }
            else
            {
                string vNoStr = GenerateVNo(model.V_TYPE);
                vNo = Convert.ToInt32(vNoStr.Substring(vNoStr.Length - 5));
                subAction = "INSERT";
            }

            var globalVar = _globalVariableService.GetGlobalVariables();

            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();

                await con.OpenAsync();

                using SqlCommand cmd = new SqlCommand("sp_QUOTATION1_MGMT", con);

                string docID = model.V_TYPE + model.V_NO;

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "INSERTANDUPDATE");
                cmd.Parameters.AddWithValue("@SubAction", subAction);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? "");
                cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                cmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE);
                cmd.Parameters.AddWithValue("@QUOTE_NO", model.QUOTE_NO ?? "");
                cmd.Parameters.AddWithValue("@QUOTE_DATE", model.QUOTE_DATE);
                cmd.Parameters.AddWithValue("@CONT_PERSON", model.CONT_PERSON ?? "");
                cmd.Parameters.AddWithValue("@VALID_DATE", model.VALID_DATE);
                cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? "");
                cmd.Parameters.AddWithValue("@PRICE_TYPE", model.PRICE_TYPE ?? "");
                cmd.Parameters.AddWithValue("@STATUS", model.STATUS);
                cmd.Parameters.AddWithValue("@PAYTERM_CODE", model.PAYTERM_CODE);
                cmd.Parameters.AddWithValue("@PAYMENT_TERM", model.PAYMENT_TERM);
                cmd.Parameters.AddWithValue("@FREIGHT_TERM", model.FREIGHT_TERM ?? "");
                cmd.Parameters.AddWithValue("@DELIVERY_TERM", model.DELIVERY_TERM ?? "");
                cmd.Parameters.AddWithValue("@GROUP_NO", model.GROUP_NO);
                cmd.Parameters.AddWithValue("@PACK_AMT", model.PACK_AMT);
                cmd.Parameters.AddWithValue("@DISC_AMT", model.DISC_AMT);
                cmd.Parameters.AddWithValue("@FREIGHT_AMT", model.FREIGHT_AMT);
                cmd.Parameters.AddWithValue("@CGST_AMT", model.CGST_AMT);
                cmd.Parameters.AddWithValue("@SGST_AMT", model.SGST_AMT);
                cmd.Parameters.AddWithValue("@IGST_AMT", model.IGST_AMT);
                cmd.Parameters.AddWithValue("@VAT_AMT", model.VAT_AMT);
                cmd.Parameters.AddWithValue("@CESS_AMT", model.CESS_AMT);
                cmd.Parameters.AddWithValue("@OTH_AMT", model.OTH_AMT);
                cmd.Parameters.AddWithValue("@NET_AMT", model.NET_AMT);
                cmd.Parameters.AddWithValue("@QTY", model.QTY);
                cmd.Parameters.AddWithValue("@AMOUNT", model.AMOUNT);
                cmd.Parameters.AddWithValue("@BULK_QTY", model.BULK_QTY);
                cmd.Parameters.AddWithValue("@BULK_DISCAMT", model.BULK_DISCAMT);
                cmd.Parameters.AddWithValue("@DOC_ID", model.V_TYPE + model.V_NO ?? "");
                cmd.Parameters.AddWithValue("@FAPROV_STATUS", model.FAPROV_STATUS ?? "");
                cmd.Parameters.AddWithValue("@FAPROV_REMARKS", model.FAPROV_REMARKS ?? "");
                cmd.Parameters.AddWithValue("@MAILSEND", model.MAILSEND);
                cmd.Parameters.AddWithValue("@IMPORT_CURRENCY", model.IMPORT_CURRENCY);
                cmd.Parameters.AddWithValue("@EXRATE", model.EXRATE);

                cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                cmd.Parameters.AddWithValue("@AED", model.AED ?? "A");
                cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                DataTable dtQuotation2 =ConvertToQuotation2TVP(data.lineRows, data.header, docID);

                SqlParameter tvpParam = cmd.Parameters.AddWithValue("@TVP_Quotation2", dtQuotation2);

                tvpParam.SqlDbType = SqlDbType.Structured;
                tvpParam.TypeName = "dbo.TVP_Quotation2";

                DataTable dtQuotation3 =await ConvertToQuotation3TVP(data.header, data.Attachement, docID);

                SqlParameter tvpParam3 = cmd.Parameters.AddWithValue("@TVP_Quotation3", dtQuotation3);

                tvpParam3.SqlDbType = SqlDbType.Structured;
                tvpParam3.TypeName = "dbo.TVP_Quotation3";

                await cmd.ExecuteNonQueryAsync();

                return (true, subAction);
            }
            catch (SqlException ex)
            {
                return (false, $"SQL Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private DataTable ConvertToQuotation2TVP(List<QUOTATION2> list, QUOTATION1 header, string DocId)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            #region
            DataTable dt = new DataTable("TVP_Quotation2");

            dt.Columns.Add("SRNO", typeof(int));
            dt.Columns.Add("YEAR_CODE", typeof(int));
            dt.Columns.Add("COMP_CODE", typeof(int));
            //dt.Columns.Add("COMP_NAME", typeof(string));
            dt.Columns.Add("BRANCH_CODE", typeof(int));
            dt.Columns.Add("V_NO", typeof(int));
            dt.Columns.Add("V_TYPE", typeof(string));
            dt.Columns.Add("ITEM_CODE", typeof(int));
            dt.Columns.Add("MAKE_CODE", typeof(int));
            dt.Columns.Add("V_DATE", typeof(DateTime));
            dt.Columns.Add("PARTY_CODE", typeof(int));
            dt.Columns.Add("TECH_DESC", typeof(string));
            dt.Columns.Add("UOM_CODE", typeof(int));
            dt.Columns.Add("REF_NO", typeof(int));
            dt.Columns.Add("REF_DATE", typeof(DateTime));
            dt.Columns.Add("REF_TYPE", typeof(string));
            dt.Columns.Add("REF_DOCID", typeof(string));
            dt.Columns.Add("QTY", typeof(decimal));
            dt.Columns.Add("IMPORT_RATE", typeof(decimal));
            dt.Columns.Add("RATE", typeof(decimal));
            dt.Columns.Add("AMOUNT", typeof(decimal));
            dt.Columns.Add("PACK_PER", typeof(decimal));
            dt.Columns.Add("PACK_AMT", typeof(decimal));
            dt.Columns.Add("DISC_PER", typeof(decimal));
            dt.Columns.Add("DISC_AMT", typeof(decimal));
            dt.Columns.Add("FREIGHT", typeof(decimal));
            dt.Columns.Add("TAX_CODE", typeof(int));
            dt.Columns.Add("CGST_PER", typeof(decimal));
            dt.Columns.Add("CGST_AMT", typeof(decimal));
            dt.Columns.Add("SGST_PER", typeof(decimal));
            dt.Columns.Add("SGST_AMT", typeof(decimal));
            dt.Columns.Add("IGST_PER", typeof(decimal));
            dt.Columns.Add("IGST_AMT", typeof(decimal));
            dt.Columns.Add("VAT_PER", typeof(decimal));
            dt.Columns.Add("VAT_AMT", typeof(decimal));
            dt.Columns.Add("CESS_PER", typeof(decimal));
            dt.Columns.Add("CESS_AMT", typeof(decimal));
            dt.Columns.Add("OTH_EXPS", typeof(decimal));
            dt.Columns.Add("LD_RATE", typeof(decimal));
            dt.Columns.Add("NET_AMT", typeof(decimal));
            dt.Columns.Add("BULK_QTY", typeof(decimal));
            dt.Columns.Add("BULK_RATE", typeof(decimal));
            dt.Columns.Add("BULK_DISC_PER", typeof(decimal));
            dt.Columns.Add("BULK_DISC_AMT", typeof(decimal));
            dt.Columns.Add("WARRANTY", typeof(string));
            dt.Columns.Add("LEADTIME_DAYS", typeof(int));
            dt.Columns.Add("PURCHASER_REMARKS", typeof(string));
            dt.Columns.Add("PREORITY_LEVEL", typeof(int));
            dt.Columns.Add("RATE_MONTHLY", typeof(decimal));
            dt.Columns.Add("RATE_QUARTERLY", typeof(decimal));
            dt.Columns.Add("RATE_ANNUALY", typeof(decimal));
            dt.Columns.Add("RATE_SPECIAL", typeof(decimal));
            dt.Columns.Add("REQ_TYPE", typeof(string));
            dt.Columns.Add("REQ_NO", typeof(int));
            dt.Columns.Add("STATUS", typeof(int));
            dt.Columns.Add("APROV_CODE", typeof(int));
            dt.Columns.Add("APROV_STATUS", typeof(string));
            dt.Columns.Add("APROV_REMARKS", typeof(string));
            dt.Columns.Add("FAPROV_STATUS", typeof(string));
            dt.Columns.Add("FAPROV_REMARKS", typeof(string));
            dt.Columns.Add("PACK_UR", typeof(string));
            dt.Columns.Add("DISC_UR", typeof(string));
            dt.Columns.Add("FREIGHT_UR", typeof(string));
            dt.Columns.Add("CGST_UR", typeof(string));
            dt.Columns.Add("SGST_UR", typeof(string));
            dt.Columns.Add("IGST_UR", typeof(string));
            dt.Columns.Add("OTHEXP_UR", typeof(string));
            dt.Columns.Add("BULKDISC_UR", typeof(string));
            dt.Columns.Add("AUTOPO_FLG", typeof(int));
            dt.Columns.Add("DOC_ID", typeof(string));
            dt.Columns.Add("UUSER", typeof(int));
            dt.Columns.Add("UDATE", typeof(DateTime));
            dt.Columns.Add("EUSER", typeof(int));
            dt.Columns.Add("EDATE", typeof(DateTime));
            dt.Columns.Add("AED", typeof(string));
            dt.Columns.Add("WSID", typeof(string));
            dt.Columns.Add("LIP", typeof(string));
            dt.Columns.Add("LID", typeof(string));
            #endregion

            foreach (var item in list)
            {
                dt.Rows.Add(
                    item.SRNO ?? (object)DBNull.Value,
                    globalVar.PubFYearCode,
                    globalVar.PubCompCode,
                    globalVar.PubBranchCode,
                    header.V_NO,
                    header.V_TYPE ?? "",
                    item.ITEM_CODE ?? (object)DBNull.Value,
                    item.MAKE_CODE ?? (object)DBNull.Value,
                    item.V_DATE ?? (object)DBNull.Value,
                    item.PARTY_CODE ?? (object)DBNull.Value,
                    item.TECH_DESC ?? "",
                    item.UOM_CODE ?? (object)DBNull.Value,
                    item.REF_NO ?? (object)DBNull.Value,
                    item.REF_DATE ?? (object)DBNull.Value,
                    item.REF_TYPE ?? "",
                    item.REF_DOCID ?? "",
                    item.QTY ?? (object)DBNull.Value,
                    item.IMPORT_RATE ?? (object)DBNull.Value,
                    item.RATE ?? (object)DBNull.Value,
                    item.AMOUNT ?? (object)DBNull.Value,
                    item.PACK_PER ?? (object)DBNull.Value,
                    item.PACK_AMT ?? (object)DBNull.Value,
                    item.DISC_PER ?? (object)DBNull.Value,
                    item.DISC_AMT ?? (object)DBNull.Value,
                    item.FREIGHT ?? (object)DBNull.Value,
                    item.TAX_CODE ?? (object)DBNull.Value,
                    item.CGST_PER ?? (object)DBNull.Value,
                    item.CGST_AMT ?? (object)DBNull.Value,
                    item.SGST_PER ?? (object)DBNull.Value,
                    item.SGST_AMT ?? (object)DBNull.Value,
                    item.IGST_PER ?? (object)DBNull.Value,
                    item.IGST_AMT ?? (object)DBNull.Value,
                    item.VAT_PER ?? (object)DBNull.Value,
                    item.VAT_AMT ?? (object)DBNull.Value,
                    item.CESS_PER ?? (object)DBNull.Value,
                    item.CESS_AMT ?? (object)DBNull.Value,
                    item.OTH_EXPS ?? (object)DBNull.Value,
                    item.LD_RATE ?? (object)DBNull.Value,
                    item.NET_AMT ?? (object)DBNull.Value,
                    item.BULK_QTY ?? (object)DBNull.Value,
                    item.BULK_RATE ?? (object)DBNull.Value,
                    item.BULK_DISC_PER ?? (object)DBNull.Value,
                    item.BULK_DISC_AMT ?? (object)DBNull.Value,
                    item.WARRANTY ?? "",
                    item.LEADTIME_DAYS ?? (object)DBNull.Value,
                    item.PURCHASER_REMARKS ?? "",
                    item.PREORITY_LEVEL ?? (object)DBNull.Value,
                    item.RATE_MONTHLY ?? (object)DBNull.Value,
                    item.RATE_QUARTERLY ?? (object)DBNull.Value,
                    item.RATE_ANNUALY ?? (object)DBNull.Value,
                    item.RATE_SPECIAL ?? (object)DBNull.Value,
                    item.REQ_TYPE ?? "",
                    item.REQ_NO ?? (object)DBNull.Value,
                    item.STATUS ?? (object)DBNull.Value,
                    item.APROV_CODE ?? (object)DBNull.Value,
                    item.APROV_STATUS ?? "",
                    item.APROV_REMARKS ?? "",
                    item.FAPROV_STATUS ?? "",
                    item.FAPROV_REMARKS ?? "",
                    item.PACK_UR ?? "",
                    item.DISC_UR ?? "",
                    item.FREIGHT_UR ?? "",
                    item.CGST_UR ?? "",
                    item.SGST_UR ?? "",
                    item.IGST_UR ?? "",
                    item.OTHEXP_UR ?? "",
                    item.BULKDISC_UR ?? "",
                    item.AUTOPO_FLG ?? (object)DBNull.Value,
                    DocId ?? "",
                    globalVar.PubUserId,
                    item.UDATE ?? (object)DBNull.Value,
                    globalVar.PubUserId,
                    item.EDATE ?? (object)DBNull.Value,
                    item.AED ?? "",
                    globalVar.PubWorkStationID,
                    globalVar.PubLocalId,
                    Environment.MachineName
                );
            }

            return dt;
        }

        private async Task<DataTable> ConvertToQuotation3TVP(QUOTATION1 header, List<QUOTATION3> list, string DocId)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            DataTable dt = new DataTable("TVP_Quotation3");

            // Define columns
            dt.Columns.Add("YEAR_CODE", typeof(int));
            dt.Columns.Add("COMP_CODE", typeof(int));
            dt.Columns.Add("BRANCH_CODE", typeof(int));
            dt.Columns.Add("V_NO", typeof(int));
            dt.Columns.Add("V_TYPE", typeof(string));
            dt.Columns.Add("V_DATE", typeof(DateTime));
            dt.Columns.Add("ATTACHMENT", typeof(string));
            dt.Columns.Add("DOC_ID", typeof(string));
            dt.Columns.Add("UUSER", typeof(int));
            dt.Columns.Add("UDATE", typeof(DateTime));
            dt.Columns.Add("EUSER", typeof(int));
            dt.Columns.Add("EDATE", typeof(DateTime));
            dt.Columns.Add("AED", typeof(string));
            dt.Columns.Add("WSID", typeof(string));
            dt.Columns.Add("LIP", typeof(string));
            dt.Columns.Add("LID", typeof(string));
            dt.Columns.Add("SRNO", typeof(int));
            dt.Columns.Add("ATTACHMENT_FILE", typeof(byte[]));

            // Prepare files for saving
            var filesToSave = list
                .Where(x => x.ATTACHMENT_FILE != null && x.ATTACHMENT_FILE.Length > 0)
                .Select(x => (
                    FileName: x.ATTACHMENT,
                    Base64Content: x.ATTACHMENT_FILE
                ))
                .ToList();

            string folderName = "PurchaseQuotation";
            var savedFiles = await FileHelper.SaveBase64FilesAsync(filesToSave, folderName);

            // Use ToDictionary on the actual List<SaveFileModel>
            var fileMap = savedFiles.ToDictionary(f => f.FileName, f => f);

            foreach (var item in list)
            {
                byte[] attachmentBytes = new byte[0];
                if (!string.IsNullOrEmpty(item.ATTACHMENT_FILE))
                {
                    attachmentBytes = Convert.FromBase64String(item.ATTACHMENT_FILE);
                }

                dt.Rows.Add(
                    globalVar.PubFYearCode,
                    globalVar.PubCompCode,
                    globalVar.PubBranchCode,
                    header.V_NO,
                    header.V_TYPE,
                    header.V_DATE,
                    item.ATTACHMENT ?? "",// ATTACHMENT
                    DocId ?? "",
                    globalVar.PubUserId,
                    item.UDATE.HasValue ? (object)item.UDATE.Value : DBNull.Value,
                    globalVar.PubUserId,
                    item.EDATE.HasValue ? (object)item.EDATE.Value : DBNull.Value,
                    item.AED ?? "",
                    globalVar.PubWorkStationID,
                    globalVar.PubLocalId,
                    Environment.MachineName,
                    item.SRNO,
                    attachmentBytes // ATTACHMENT_FILE
                );
            }
            return dt;
        }

        private async Task<(bool IsValid, string Message)> ValidateQuotationAsync(QUOTATION1 model, List<QUOTATION2> lineRows, List<QUOTATION3> attachments)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            // ======================
            // Party State Validation
            // ======================

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                // Party State
                SqlCommand partyCmd = new SqlCommand(
                    @"SELECT STATE_CODE FROM SUBGROUP_MAST WHERE CODE=@PartyCode AND COMP_CODE=@CompCode", con);

                partyCmd.Parameters.AddWithValue("@PartyCode", model.PARTY_CODE);
                partyCmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);

                object stateObj = await partyCmd.ExecuteScalarAsync();

                int partyStateCode = stateObj != null
                    ? Convert.ToInt32(stateObj)
                    : 0;

                // Company State
                SqlCommand companyCmd = new SqlCommand(
                    @"SELECT STATE_CODE FROM COMP_MAST WHERE CODE=@CompCode", con);

                companyCmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);

                object companyStateObj = await companyCmd.ExecuteScalarAsync();

                int companyStateCode = companyStateObj != null ? Convert.ToInt32(companyStateObj) : 0;

                string stateType = partyStateCode == companyStateCode ? "Local" : "Central/Other";

                if (partyStateCode == companyStateCode &&
                    Convert.ToDecimal(model.IGST_AMT ?? 0) > 0)
                {
                    return (false,
                        $"IGST Not applicable as per Party State type is {stateType}");
                }

                if (partyStateCode != companyStateCode &&
                    (Convert.ToDecimal(model.CGST_AMT ?? 0)
                    + Convert.ToDecimal(model.SGST_AMT ?? 0)) > 0)
                {
                    return (false,
                        $"CGST/SGST not applicable as per Party State type is {stateType}");
                }

                if (Convert.ToDecimal(model.IGST_AMT ?? 0) > 0 &&
                    (Convert.ToDecimal(model.CGST_AMT ?? 0)
                    + Convert.ToDecimal(model.SGST_AMT ?? 0)) > 0)
                {
                    return (false,
                        "CGST+SGST+IGST all three type tax not applicable.");
                }
            }

            return (true, "");
        }

        public async Task<object> CopyData(string actionType, DateTime? vDate)
        {
            var data = new List<object>();
            var globalVariable= _globalVariableService.GetGlobalVariables();
            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();

                using SqlCommand cmd =new SqlCommand("sp_QUOTATION1_MGMT", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", actionType);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                cmd.Parameters.AddWithValue ("@V_DATE", vDate);

                await con.OpenAsync();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    data.Add(new
                    {
                        VNo = reader["VNo"],
                        VType = reader["VType"],
                        VDate = reader["VDate"],
                        ItemCode = reader["ItemCode"],
                        ItemName = reader["ItemName"],
                        Make = reader["Make"],
                        TechDesc = reader["TechDesc"],
                        Unit = reader["Unit"] == DBNull.Value ? "" : reader["Unit"].ToString(),
                        Qty = reader["Qty"] == DBNull.Value ? "" : reader["Qty"].ToString(),
                        MakeCode = reader["MakeCode"] == DBNull.Value ? "" : reader["MakeCode"].ToString(),
                        UCode = reader["UCode"] == DBNull.Value ? "" : reader["UCode"].ToString(),
                        TaxCode = reader["TaxCode"] == DBNull.Value ? "" : reader["TaxCode"].ToString()
                    });
                }

                return new
                {
                    success = true,
                    message = "Data copied successfully.",
                    data
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }

        public async Task<object> GetPurchaseHistory(int itemcode)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            var result = new List<object>();

            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();

                using SqlCommand cmd = new SqlCommand("sp_QUOTATION1_MGMT", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "PurchaseHistory");
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                cmd.Parameters.AddWithValue("@ITEM_CODE", itemcode);

                await con.OpenAsync();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        VNo = reader["VNo"]?.ToString() ?? "",
                        Date = reader["Date"]?.ToString() ?? "",
                        Supplier = reader["Supplier"]?.ToString() ?? "",
                        ItemName = reader["ItemName"]?.ToString() ?? "",
                        Make = reader["Make"]?.ToString() ?? "",
                        Unit = reader["Unit"]?.ToString() ?? "",

                        Qty = reader["Qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Qty"]),
                        Rate = reader["Rate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Rate"]),
                        OthAmt = reader["OthAmt"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["OthAmt"]),

                        CGSTPer = reader["CGSTPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CGSTPer"]),
                        SGSTPer = reader["SGSTPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SGSTPer"]),
                        IGSTPer = reader["IGSTPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["IGSTPer"]),

                        PackPer = reader["PackPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PackPer"]),
                        DiscPer = reader["DiscPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DiscPer"]),
                        LDRate = reader["LDRate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["LDRate"]),

                        Remarks = reader["Remarks"]?.ToString() ?? "",
                        Status = reader["Status"]?.ToString() ?? ""
                    });
                }

                return new
                {
                    success = true,
                    data = result
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }

        public async Task<object> GetPurchaseQuotation(int itemcode)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            var result = new List<object>();

            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();

                using SqlCommand cmd = new SqlCommand("sp_QUOTATION1_MGMT", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "PurchaseQuotationHistory");
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                cmd.Parameters.AddWithValue("@ITEM_CODE", itemcode);

                await con.OpenAsync();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        VNo = reader["VNo"]?.ToString() ?? "",
                        Date = reader["Date"]?.ToString() ?? "",
                        Supplier = reader["Supplier"]?.ToString() ?? "",
                        ItemName = reader["ItemName"]?.ToString() ?? "",
                        Make = reader["Make"]?.ToString() ?? "",
                        Unit = reader["Unit"]?.ToString() ?? "",
                        GroupNo = reader["GroupNo"] == DBNull.Value ? 0 : Convert.ToInt32(reader["GroupNo"]),
                        Qty = reader["Qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Qty"]),
                        Rate = reader["Rate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Rate"]),
                        Freight = reader["Freight"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Freight"]),
                        CGSTPer = reader["CGSTPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CGSTPer"]),
                        SGSTPer = reader["SGSTPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SGSTPer"]),
                        IGSTPer = reader["IGSTPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["IGSTPer"]),
                        PackPer = reader["PackPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PackPer"]),
                        DiscPer = reader["DiscPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DiscPer"]),
                        OthExps = reader["OthExps"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["OthExps"]),
                        LDRate = reader["LDRate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["LDRate"]),
                        Remarks = reader["Remarks"]?.ToString() ?? "",
                        Status = reader["Status"]?.ToString() ?? ""
                    });
                }

                return new
                {
                    success = true,
                    data = result
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }

        public async Task<object> OrderHistory(int itemcode, DateTime? vDate)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            var result = new List<object>();

            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();

                using SqlCommand cmd = new SqlCommand("sp_QUOTATION1_MGMT", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "OrderHistory");
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                cmd.Parameters.AddWithValue("@ITEM_CODE", itemcode);
                cmd.Parameters.AddWithValue("@V_DATE", (object?)vDate ?? DBNull.Value);

                await con.OpenAsync();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        VNo = reader["VNo"]?.ToString() ?? "",
                        Date = reader["Date"]?.ToString() ?? "",
                        Supplier = reader["Supplier"]?.ToString() ?? "",
                        ItemName = reader["ItemName"]?.ToString() ?? "",
                        Make = reader["Make"]?.ToString() ?? "",
                        Unit = reader["Unit"]?.ToString() ?? "",
                        Qty = reader["Qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Qty"]),
                        Rate = reader["Rate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Rate"]),
                        CGSTPer = reader["CGSTPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CGSTPer"]),
                        SGSTPer = reader["SGSTPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SGSTPer"]),
                        IGSTPer = reader["IGSTPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["IGSTPer"]),
                        PackPer = reader["PackPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PackPer"]),
                        DiscPer = reader["DiscPer"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DiscPer"]),
                        OthExps = reader["OthExps"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["OthExps"]),
                        LDRate = reader["LDRate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["LDRate"]),
                        Remarks = reader["Remarks"]?.ToString() ?? "",
                        Status = reader["Status"]?.ToString() ?? ""
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<byte[]> ExportToExcel(int vNo, string vType)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            using SqlConnection con = _dbConnection.GetErpConnection();

            using SqlCommand cmd = new SqlCommand("sp_QUOTATION1_MGMT", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Action", "ExportToExcel");
            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
            cmd.Parameters.AddWithValue("@V_TYPE", vType);
            cmd.Parameters.AddWithValue("@V_NO", vNo);

            await con.OpenAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Quotation");

            int row = 1;

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            // Header
            for (int i = 0; i < reader.FieldCount; i++)
            {
                ws.Cell(row, i + 1).Value = reader.GetName(i);
            }

            row++;

            // Data
            while (await reader.ReadAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    ws.Cell(row, i + 1).Value = reader[i]?.ToString();
                }

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        public decimal GetLastOrderRate(int itemCode, DateTime vDate)
        {
            decimal rate = 0;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"SELECT TOP 1 LAND_RATE FROM Order2 WHERE COMP_CODE = @COMP_CODE AND ITEM_CODE = @ITEM_CODE AND V_TYPE IN ('PORD','SORD')
                              AND V_DATE < @V_DATE  ORDER BY V_DATE DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", _globalVariableService.GetGlobalVariables().PubCompCode);

                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                    cmd.Parameters.AddWithValue("@V_DATE", vDate);

                    con.Open();

                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        rate = Convert.ToDecimal(result);
                    }
                }
            }

            return rate;
        }

    }
}
