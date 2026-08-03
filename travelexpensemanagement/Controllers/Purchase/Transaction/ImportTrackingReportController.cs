using iText.StyledXmlParser.Jsoup.Select;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Ocsp;
using System.Data;
using System.Reflection.Emit;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportTrackingReportController : Controller
    {

        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;

        public ImportTrackingReportController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, DropdownService dropdownService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportTrackingReport/Index.cshtml");
        }

        [HttpGet]
        public async Task<object> GetReportData(DateTime FromDate, DateTime ToDate)
        {
            var gv = _globalValue.GetGlobalVariables();
            var dataList = new List<object>();
            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_ImportTrackingReport", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "ReportSql");
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.Add("@FromDate", SqlDbType.SmallDateTime).Value = FromDate;
                        cmd.Parameters.Add("@ToDate", SqlDbType.SmallDateTime).Value = ToDate;

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                dataList.Add(new
                                {
                                    // Dates
                                    SaudaDate = SafeDate(rdr, "SaudaDate"),
                                    MRNDate = SafeDate(rdr, "MRNDate"),
                                    InvDate = SafeDate(rdr, "InvDate"),
                                    ETAPortDate = SafeDate(rdr, "ETAPortDate"),
                                    RailDate = SafeDate(rdr, "RailDate"),
                                    ICDReachDate = SafeDate(rdr, "ICDReachDate"),
                                    SCANDOC_VERIFYDATE = SafeDate(rdr, "SCANDOC_VERIFYDATE"),
                                    CHAHANDOVER_DATE = SafeDate(rdr, "CHAHANDOVER_DATE"),
                                    BL_DATE = SafeDate(rdr, "BL_DATE"),
                                    BL_RECDDATE = SafeDate(rdr, "BL_RECDDATE"),
                                    BL_HANDOVERDATE = SafeDate(rdr, "BL_HANDOVERDATE"),
                                    CHKLIST_APPROVDATE = SafeDate(rdr, "CHKLIST_APPROVDATE"),
                                    BE_DATE = SafeDate(rdr, "BE_DATE"),
                                    ETA_BEDATE = SafeDate(rdr, "ETA_BEDATE"),
                                    IGM_DATE = SafeDate(rdr, "IGM_DATE"),
                                    SAMPLE_COLDATE = SafeDate(rdr, "SAMPLE_COLDATE"),
                                    SAMPLE_TESTREPORTDATE = SafeDate(rdr, "SAMPLE_TESTREPORTDATE"),
                                    ASSESMENT_DATE = SafeDate(rdr, "ASSESMENT_DATE"),
                                    DUTY_PAYDATE = SafeDate(rdr, "DUTY_PAYDATE"),
                                    SHIPLINE_PAYDATE = SafeDate(rdr, "SHIPLINE_PAYDATE"),
                                    DO_DATE = SafeDate(rdr, "DO_DATE"),
                                    RECEIPT_DATE = SafeDate(rdr, "RECEIPT_DATE"),
                                    TENTATIVE_HODATE = SafeDate(rdr, "TENTATIVE_HODATE"),
                                    CONTAINER_HODATE = SafeDate(rdr, "CONTAINER_HODATE"),
                                    AdvPaymentDate = SafeDate(rdr, "AdvPaymentDate"),
                                    BalPaymentDate = SafeDate(rdr, "BalPaymentDate"),
                                    SBLC_APPLDATE = SafeDate(rdr, "SBLC_APPLDATE"),
                                    SBLC_PAYDATE = SafeDate(rdr, "SBLC_PAYDATE"),
                                    SBLC_DUEDATE = SafeDate(rdr, "SBLC_DUEDATE"),
                                    SBLC_DISBDATE = SafeDate(rdr, "SBLC_DISBDATE"),
                                    DO_VALIDDATE = SafeDate(rdr, "DO_VALIDDATE"),
                                    SEC_DATE = SafeDate(rdr, "SEC_DATE"),
                                    REFUND_RECDATE = SafeDate(rdr, "REFUND_RECDATE"),
                                    DETENTION_DATE = SafeDate(rdr, "DETENTION_DATE"),
                                    CUSTOM_OOCDATE = SafeDate(rdr, "CUSTOM_OOCDATE"),
                                    FACTORY_DATE = SafeDate(rdr, "FACTORY_DATE"),
                                    LC_DATE = SafeDate(rdr, "LC_DATE"),
                                    LC_EXPIRYDATE = SafeDate(rdr, "LC_EXPIRYDATE"),

                                    // Strings
                                    SaudaNo = SafeString(rdr, "SaudaNo"),
                                    SupplierName = SafeString(rdr, "SupplierName"),
                                    ItemName = SafeString(rdr, "ItemName"),
                                    DeliveryLocation = SafeString(rdr, "DeliveryLocation"),
                                    Country = SafeString(rdr, "Country"),
                                    ExpectedLoading = SafeString(rdr, "ExpectedLoading"),
                                    PaymentTerm = SafeString(rdr, "PaymentTerm"),
                                    FrtTerm = SafeString(rdr, "FrtTerm"),
                                    DelTerm = SafeString(rdr, "DelTerm"),
                                    Remark = SafeString(rdr, "Remark"),
                                    ContainerNo = SafeString(rdr, "ContainerNo"),
                                    ContainerSize = SafeString(rdr, "ContainerSize"),
                                    InvNo = SafeString(rdr, "InvNo"),
                                    POD = SafeString(rdr, "POD"),
                                    POL = SafeString(rdr, "POL"),
                                    VSLName = SafeString(rdr, "VSLName"),
                                    ICDName = SafeString(rdr, "ICDName"),
                                    ETD = SafeString(rdr, "ETD"),
                                    PINO = SafeString(rdr, "PINO"),
                                    OFFERNO = SafeString(rdr, "OFFERNO"),
                                    CHAName = SafeString(rdr, "CHAName"),
                                    ETA_PLACE = SafeString(rdr, "ETA_PLACE"),
                                    BL_NO = SafeString(rdr, "BL_NO"),
                                    CHA_CHARGEDETAIL = SafeString(rdr, "CHA_CHARGEDETAIL"),
                                    BE_NO = SafeString(rdr, "BE_NO"),
                                    SAMPLE_COLLECTED = SafeString(rdr, "SAMPLE_COLLECTED"),
                                    ASSESMENT_TYPE = SafeString(rdr, "ASSESMENT_TYPE"),
                                    DUTY_PAYMODE = SafeString(rdr, "DUTY_PAYMODE"),
                                    ADVANCE_LICNO = SafeString(rdr, "ADVANCE_LICNO"),
                                    DOC_HANDOVERDTBANK = SafeString(rdr, "DOC_HANDOVERDTBANK"),
                                    ShippingLine = SafeString(rdr, "ShippingLine"),
                                    AdvPaymentAmount = SafeString(rdr, "AdvPaymentAmount"),
                                    AdvPaymentReference = SafeString(rdr, "AdvPaymentReference"),
                                    BaPaymentAmount = SafeString(rdr, "BaPaymentAmount"),
                                    BalPaymentReference = SafeString(rdr, "BalPaymentReference"),
                                    Supplier = SafeString(rdr, "Supplier"),
                                    Agent = SafeString(rdr, "Agent"),
                                    Bank = SafeString(rdr, "Bank"),
                                    SHIPLINE_NAME = SafeString(rdr, "SHIPLINE_NAME"),
                                    SBLC_NO = SafeString(rdr, "SBLC_NO"),
                                    UNDER_PROTEST = SafeString(rdr, "UNDER_PROTEST"),
                                    CHA_CHARGETYPE = SafeString(rdr, "CHA_CHARGETYPE"),

                                    // Decimals
                                    SaudaQty = SafeDecimal(rdr, "SaudaQty"),
                                    TrackingQty = SafeDecimal(rdr, "TrackingQty"),
                                    MRNQty = SafeDecimal(rdr, "MRNQty"),
                                    InvAmt = SafeDecimal(rdr, "InvAmt"),
                                    HOLLAGE_CHARGES = SafeDecimal(rdr, "HOLLAGE_CHARGES"),
                                    SECURITY_AMT = SafeDecimal(rdr, "SECURITY_AMT"),
                                    REFUND_AMT = SafeDecimal(rdr, "REFUND_AMT"),
                                    INT_RATE = SafeDecimal(rdr, "INT_RATE"),
                                    OTH_CHARGES = SafeDecimal(rdr, "OTH_CHARGES"),
                                    SBLC_AVLAMT = SafeDecimal(rdr, "SBLC_AVLAMT"),
                                    LC_AMT = SafeDecimal(rdr, "LC_AMT"),
                                    QC_AMT = SafeDecimal(rdr, "QC_AMT"),
                                    SBLC_DISBAMT = SafeDecimal(rdr, "SBLC_DISBAMT"),
                                    SBLC_INTRATESOFR = SafeDecimal(rdr, "SBLC_INTRATESOFR"),
                                    DETENTION_AMT = SafeDecimal(rdr, "DETENTION_AMT"),
                                    QLTY_CLAIMAMT = SafeDecimal(rdr, "QLTY_CLAIMAMT"),
                                    ENHANCE_RATE = SafeDecimal(rdr, "ENHANCE_RATE"),
                                    PASSING_RATE = SafeDecimal(rdr, "PASSING_RATE"),
                                    DUTY_AMT = SafeDecimal(rdr, "DUTY_AMT"),
                                    CONCOR_EXPS = SafeDecimal(rdr, "CONCOR_EXPS"),
                                    CHA_AMT = SafeDecimal(rdr, "CHA_AMT"),
                                    CFS_CHARGES = SafeDecimal(rdr, "CFS_CHARGES"),
                                    CFS_AMT = SafeDecimal(rdr, "CFS_AMT"),
                                    QC_AMTRECD = SafeDecimal(rdr, "QC_AMTRECD"),

                                    // Ints
                                    NoofContainer = SafeInt(rdr, "NoofContainer"),
                                    MRNNo = SafeInt(rdr, "MRNNo"),
                                    FreeDays = SafeInt(rdr, "FreeDays"),
                                    DIFF_DAYS = SafeInt(rdr, "DIFF_DAYS"),
                                });
                            }
                        }
                    }
                }

                return new { success = true, data = dataList };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
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

        private static int SafeInt(SqlDataReader rdr, string col)
        {
            if (rdr[col] == DBNull.Value) return 0;
            var raw = rdr[col].ToString();
            if (string.IsNullOrWhiteSpace(raw)) return 0;
            return int.TryParse(raw, out var val) ? val : 0;
        }

        private static string SafeString(SqlDataReader rdr, string col, string fallback = "")
        {
            if (rdr[col] == DBNull.Value) return fallback;
            var raw = rdr[col].ToString();
            // treat literal "NULL" text (sometimes returned by SPs) as empty too
            return string.Equals(raw, "NULL", StringComparison.OrdinalIgnoreCase) ? fallback : raw;
        }


        public JsonResult cmbDoctype()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = @"Select distinct doctype , doctype as vv from Doctype_mast Where doctype in('EXIMTracking') Order by doctype ";
                var cmbDoctype = _dropdownService.GetDropdownList(query);
                return Json(cmbDoctype);
            }
        }
        public JsonResult cboVType()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = @"Select code,name from Doctype_mast Where code in('EXIM') Order by doctype,name ";
                var cboVType = _dropdownService.GetDropdownList(query);
                return Json(cboVType);
            }

        }
        public JsonResult SendTo(string Textbox , string v_type)
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string l1 = "";

                if (Textbox != null)
                {
                     l1 = " and d.full_name like '" + Textbox + "%'";
                }

                string query = @"Select distinct d.CODE , d.full_name from DOCFLOW_USERS a 
                    Left join DOCTYPE_MAST b on a.Doc_code=b.code 
                    Left join SUBUSER_MAST c on a.USER_CODE=c.USER_CODE 
                    Left join USER_MAST d on c.USER_CODE=d.code and 
                    a.COMP_CODE = d.COMP_CODE 
                    where a.Comp_code=  " + getdata.PubCompCode +" and   a.DOC_CODE='"+ v_type + "' and SACTION='Receive Document'  " + l1 + "  ORDER by d.full_name  ";

                var SendTo = _dropdownService.GetDropdownList(query);
                return Json(SendTo);
            }

        }

        [HttpGet]
        public async Task<object> GetDocumentListData( DateTime FromDate, DateTime ToDate,string DocStatus, string Searchby,  string SearchText)
        {
            var gv = _globalValue.GetGlobalVariables();
            var dataList = new List<object>();

            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_ImportTrackingReport", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SendDocumentGridData");
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@DocStatus", DocStatus);
                        cmd.Parameters.AddWithValue("@Searchby", Searchby ?? string.Empty);
                        cmd.Parameters.AddWithValue("@SearchText", SearchText ?? string.Empty);
                        cmd.Parameters.Add("@FromDate", SqlDbType.SmallDateTime).Value = FromDate;
                        cmd.Parameters.Add("@ToDate", SqlDbType.SmallDateTime).Value = ToDate;

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                string sendFlag = "Pending";
                                string recdFlag = "Pending";

                                if (rdr["SEND_FLAG"] != DBNull.Value && Convert.ToInt32(rdr["SEND_FLAG"]) == 1)
                                {
                                    sendFlag = "Send";
                                }                           

                                if (rdr["RECD_FLAG"] != DBNull.Value && Convert.ToInt32(rdr["RECD_FLAG"]) == 1)
                                {
                                    recdFlag = "Received";
                                }                  

                                    dataList.Add(new
                                    {
                                        V_TYPE = rdr["V_TYPE"]?.ToString(),
                                        V_NO = rdr["V_NO"]?.ToString(),
                                        SAUDA_TYPE = rdr["SAUDA_TYPE"]?.ToString(),
                                        SAUDA_NO = rdr["SAUDA_NO"]?.ToString(),
                                        V_DATE = rdr["V_DATE"]?.ToString(),
                                        PartyName = rdr["PartyName"]?.ToString(),
                                        PARTY_CODE = rdr["PARTY_CODE"]?.ToString(),
                                        PARTY_CITY = rdr["PARTY_CITY"]?.ToString(),
                                        CityName = rdr["CityName"]?.ToString(),
                                        BILL_NO = rdr["BILL_NO"]?.ToString(),
                                        BILL_DATE = rdr["BILL_DATE"]?.ToString(),
                                        INV_AMT = rdr["INV_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["INV_AMT"]),
                                        TRUCK_NO = rdr["TRUCK_NO"]?.ToString(),
                                        SenderCode = rdr["SenderCode"]?.ToString(),
                                        SenderName = rdr["SenderName"]?.ToString(),
                                        SEND_DATE = rdr["SEND_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(rdr["SEND_DATE"]).ToString("dd/MM/yyyy"),
                                        ReceiverCode = rdr["ReceiverCode"]?.ToString(),
                                        ReceiverName = rdr["ReceiverName"]?.ToString(),
                                        RECD_DATE = rdr["RECD_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(rdr["RECD_DATE"]).ToString("dd/MM/yyyy"),
                                        SEND_FLAG = sendFlag,
                                        RECD_FLAG = recdFlag,
                                        REMARKS = rdr["REMARKS"]?.ToString()
                                    });
                            }
                        }
                    }
                }

                return new { success = true, data = dataList };
            }
            catch (Exception ex)
            {
                return new {  success = false, message = ex.Message };
            }
        }

        [HttpPost]
        public IActionResult SendDocument([FromBody] List<ImportTrackingReport> model)
        {
            var GlobalVariable = _globalValue.GetGlobalVariables();

            if (model == null || model.Count == 0)
            {
                return BadRequest(new  { success = false, message = "No data received" });
            }

            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    con.Open();

                    foreach (var item in model)
                    {

                        if(item.SEND_TO == null)
                        {
                            return Json(new { validation = false , success = false, count = model.Count, message = "Please select 'Send To' User." });
                        }

                        string prQuery = @" SELECT 1 FROM DOCFLOW WHERE COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode
                            AND V_TYPE = @VType AND V_NO = @VNo  AND BILL_NO = @BillNo AND ISNULL(SEND_FLAG, 0) = 1";

                        using (SqlCommand cmd = new SqlCommand(prQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@CompCode", GlobalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BranchCode", GlobalVariable.PubBranchCode);
                            cmd.Parameters.AddWithValue("@VType", item.V_TYPE ?? "");
                            cmd.Parameters.AddWithValue("@VNo", item.V_NO ?? 0);
                            cmd.Parameters.AddWithValue("@BillNo", item.BILL_NO ?? "");

                            var exists = cmd.ExecuteScalar();

                            if (exists != null)
                            {
                                return Json(new { validation = false, success = false, count = model.Count, message = "Document already sent" });
                            }
                        }                               

                        using (SqlCommand cmd = new SqlCommand("sp_ImportTrackingReport", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Action", "SendDocument");

                            cmd.Parameters.AddWithValue("@YEAR_CODE", GlobalVariable.PubFYearCode );
                            cmd.Parameters.AddWithValue("@CompCode", GlobalVariable.PubCompCode );
                            cmd.Parameters.AddWithValue("@BranchCode", GlobalVariable.PubBranchCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", item.V_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@V_NO", item.V_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SAUDA_TYPE", item.SAUDA_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SAUDA_NO",  item.SAUDA_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@V_DATE", item.V_DATE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PARTY_CODE",  item.PARTY_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PARTY_NAME",  item.PARTY_NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CITY_CODE",   item.CITY_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CITY_NAME", item.CITY_NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_NO", item.BILL_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_DATE", item.BILL_DATE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_AMT",  item.BILL_AMT ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TRUCK_NO",  item.TRUCK_NO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SEND_BY",  item.SEND_BY ?? GlobalVariable.PubUserId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SEND_TO",  item.SEND_TO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SEND_DATE", item.SEND_DATE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@RECD_DATE",  item.RECD_DATE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SEND_FLAG",  item.SEND_FLAG ?? 1);
                            cmd.Parameters.AddWithValue("@RECD_FLAG", item.RECD_FLAG ?? 0);
                            cmd.Parameters.AddWithValue("@REMARKS", item.REMARKS ?? (object)DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                return Json(new { success = true, count = model.Count, message = "Document sent successfully" });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool isExist(string query)
        {
            using (var con = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value;
                }
            }
        }

    }
}
