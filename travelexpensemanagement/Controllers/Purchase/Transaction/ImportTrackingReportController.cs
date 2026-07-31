using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

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
                        cmd.Parameters.Add("@FromDate", SqlDbType.SmallDateTime).Value = FromDate ;
                        cmd.Parameters.Add("@ToDate", SqlDbType.SmallDateTime).Value = ToDate ;

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                dataList.Add(new
                                {
                                    SaudaNo = rdr["SaudaNo"]?.ToString(),
                                    SaudaDate = rdr["SaudaDate"]?.ToString(),
                                    SupplierName = rdr["SupplierName"]?.ToString(),
                                    ItemName = rdr["ItemName"]?.ToString(),
                                    DeliveryLocation = rdr["DeliveryLocation"]?.ToString(),
                                    Country = rdr["Country"]?.ToString(),
                                    ExpectedLoading = rdr["ExpectedLoading"]?.ToString(),
                                    PaymentTerm = rdr["PaymentTerm"]?.ToString(),
                                    FrtTerm = rdr["FrtTerm"]?.ToString(),
                                    DelTerm = rdr["DelTerm"]?.ToString(),
                                    Remark = rdr["Remark"]?.ToString(),
                                    NoofContainer = rdr["NoofContainer"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["NoofContainer"]),
                                    SaudaQty = rdr["SaudaQty"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["SaudaQty"]),
                                    TrackingQty = rdr["TrackingQty"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["TrackingQty"]),
                                    MRNQty = rdr["MRNQty"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["MRNQty"]),
                                    MRNNo = rdr["MRNNo"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["MRNNo"]),
                                    MRNDate = rdr["MRNDate"]?.ToString(),
                                    ContainerNo = rdr["ContainerNo"]?.ToString(),
                                    ContainerSize = rdr["ContainerSize"]?.ToString(),
                                    InvNo = rdr["InvNo"]?.ToString(),
                                    InvDate = rdr["InvDate"]?.ToString(),
                                    InvAmt = rdr["InvAmt"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["InvAmt"]),
                                    POD = rdr["POD"]?.ToString(),
                                    POL = rdr["POL"]?.ToString(),
                                    VSLName = rdr["VSLName"]?.ToString(),
                                    ICDName = rdr["ICDName"]?.ToString(),
                                    FreeDays = rdr["FreeDays"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["FreeDays"]),
                                    ETD = rdr["ETD"]?.ToString(),
                                    ETAPortDate = rdr["ETAPortDate"]?.ToString(),
                                    RailDate = rdr["RailDate"]?.ToString(),
                                    ICDReachDate = rdr["ICDReachDate"]?.ToString(),
                                    PINO = rdr["PINO"]?.ToString(),
                                    OFFERNO = rdr["OFFERNO"]?.ToString(),
                                    CHAName = rdr["CHAName"]?.ToString(),
                                    SCANDOC_VERIFYDATE = rdr["SCANDOC_VERIFYDATE"]?.ToString(),
                                    ETA_PLACE = rdr["ETA_PLACE"]?.ToString(),
                                    CHAHANDOVER_DATE = rdr["CHAHANDOVER_DATE"]?.ToString(),                    
                                    BL_NO = rdr["BL_NO"]?.ToString(),
                                    BL_DATE = rdr["BL_DATE"]?.ToString(),
                                    BL_RECDDATE = rdr["BL_RECDDATE"]?.ToString(),
                                    BL_HANDOVERDATE = rdr["BL_HANDOVERDATE"]?.ToString(),
                                    CHA_CHARGEDETAIL = rdr["CHA_CHARGEDETAIL"]?.ToString(),
                                    CHKLIST_APPROVDATE = rdr["CHKLIST_APPROVDATE"]?.ToString(),
                                    BE_NO = rdr["BE_NO"]?.ToString(),
                                    BE_DATE = rdr["BE_DATE"]?.ToString(),
                                    ETA_BEDATE = rdr["ETA_BEDATE"]?.ToString(),
                                    IGM_DATE = rdr["IGM_DATE"]?.ToString(),
                                    SAMPLE_COLLECTED = rdr["SAMPLE_COLLECTED"]?.ToString(),
                                    SAMPLE_COLDATE = rdr["SAMPLE_COLDATE"]?.ToString(),
                                    SAMPLE_TESTREPORTDATE = rdr["SAMPLE_TESTREPORTDATE"]?.ToString(),
                                    ASSESMENT_TYPE = rdr["ASSESMENT_TYPE"]?.ToString(),
                                    ASSESMENT_DATE = rdr["ASSESMENT_DATE"]?.ToString(),
                                    DUTY_PAYDATE = rdr["DUTY_AMT"]?.ToString(),
                                    DUTY_AMT = rdr["DUTY_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["DUTY_AMT"]),
                                    DUTY_PAYMODE = rdr["DUTY_PAYMODE"]?.ToString(),
                                    ADVANCE_LICNO = rdr["ADVANCE_LICNO"]?.ToString(),
                                    DOC_HANDOVERDTBANK = rdr["DOC_HANDOVERDTBANK"]?.ToString(),
                                    SHIPLINE_PAYDATE = rdr["SHIPLINE_PAYDATE"]?.ToString(),
                                    ShippingLine = rdr["ShippingLine"]?.ToString(),
                                    HOLLAGE_CHARGES = rdr["HOLLAGE_CHARGES"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["HOLLAGE_CHARGES"]),
                                    DO_DATE = rdr["DO_DATE"]?.ToString(),
                                    SECURITY_AMT = rdr["SECURITY_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["SECURITY_AMT"]),
                                    REFUND_AMT = rdr["REFUND_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["REFUND_AMT"]),
                                    RECEIPT_DATE = rdr["RECEIPT_DATE"]?.ToString(),
                                    TENTATIVE_HODATE = rdr["TENTATIVE_HODATE"]?.ToString(),
                                    CONTAINER_HODATE = rdr["CONTAINER_HODATE"]?.ToString(),
                                    AdvPaymentDate = rdr["AdvPaymentDate"]?.ToString(),
                                    BalPaymentDate = rdr["BalPaymentDate"]?.ToString(),                            
                                    AdvPaymentAmount = rdr["AdvPaymentAmount"] == DBNull.Value ? "" : rdr["AdvPaymentAmount"].ToString(),
                                    AdvPaymentReference = rdr["AdvPaymentReference"]?.ToString(),
                                    BaPaymentAmount = rdr["BaPaymentAmount"] == DBNull.Value ? "" : rdr["BaPaymentAmount"].ToString(),
                                    BalPaymentReference = rdr["BalPaymentReference"]?.ToString(),                   
                                    DIFF_DAYS = rdr["DIFF_DAYS"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["DIFF_DAYS"]),
                                    INT_RATE = rdr["INT_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["INT_RATE"]),
                                    OTH_CHARGES = rdr["OTH_CHARGES"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["OTH_CHARGES"]),
                                    SBLC_AVLAMT = rdr["SBLC_AVLAMT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["SBLC_AVLAMT"]),
                                    Supplier = rdr["Supplier"]?.ToString(),
                                    Agent = rdr["Agent"]?.ToString(),
                                    Bank = rdr["Bank"]?.ToString(),
                                    SHIPLINE_NAME = rdr["SHIPLINE_NAME"]?.ToString(),
                                    SBLC_NO = rdr["SBLC_NO"]?.ToString(),
                                    SBLC_APPLDATE = rdr["SBLC_APPLDATE"]?.ToString(),
                                    SBLC_PAYDATE = rdr["SBLC_PAYDATE"]?.ToString(),
                                    SBLC_DUEDATE = rdr["SBLC_DUEDATE"]?.ToString(),
                                    SBLC_DISBDATE = rdr["SBLC_DISBDATE"]?.ToString(),
                                    DO_VALIDDATE = rdr["DO_VALIDDATE"]?.ToString(),
                                    SEC_DATE = rdr["SEC_DATE"]?.ToString(),
                                    REFUND_RECDATE = rdr["REFUND_RECDATE"]?.ToString(),
                                    DETENTION_DATE = rdr["DETENTION_DATE"]?.ToString(),
                                    CUSTOM_OOCDATE = rdr["CUSTOM_OOCDATE"]?.ToString(),
                                    FACTORY_DATE = rdr["FACTORY_DATE"]?.ToString(),
                                    LC_DATE = rdr["LC_DATE"]?.ToString(),
                                    LC_EXPIRYDATE = rdr["LC_EXPIRYDATE"]?.ToString(),
                                    TENTATIVE_HORETDATE = rdr["TENTATIVE_HORETDATE"]?.ToString(),
                                    MOVE_DATE = rdr["MOVE_DATE"]?.ToString(),
                                    TENCONT_HORETDATE = rdr["TENCONT_HORETDATE"]?.ToString(),
                                    LATEST_SHIPDATE = rdr["LATEST_SHIPDATE"]?.ToString(),
                                    BOE_SUBMITDATE = rdr["BOE_SUBMITDATE"]?.ToString(),
                                    CFS_DSTUFFDATE = rdr["CFS_DSTUFFDATE"]?.ToString(),
                                    CFS_PAYDATE = rdr["CFS_PAYDATE"]?.ToString(),
                                    ACTUAL_CONTHODATE = rdr["ACTUAL_CONTHODATE"]?.ToString(),
                                    SBLC_DISBAMT = rdr["SBLC_DISBAMT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["SBLC_DISBAMT"]),                    
                                    SBLC_INTRATESOFR = rdr["SBLC_INTRATESOFR"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["SBLC_INTRATESOFR"]),
                                    DETENTION_AMT = rdr["DETENTION_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["DETENTION_AMT"]),                 
                                    DETENTION_REASON = rdr["DETENTION_REASON"]?.ToString(),
                                    DETREASON_APPROVBY = rdr["DETREASON_APPROVBY"]?.ToString(),
                                    LC_NO = rdr["LC_NO"]?.ToString(),                       
                                    LC_BENIFICIARY = rdr["LC_BENIFICIARY"]?.ToString(),
                                    LC_ISSUEBANK = rdr["LC_ISSUEBANK"]?.ToString(),                     
                                    LC_AMT = rdr["LC_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["LC_AMT"]),
                                    QLTY_CLAIMAMT = rdr["QLTY_CLAIMAMT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["QLTY_CLAIMAMT"]),
                                    QC_AMT = rdr["QC_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["QC_AMT"]),
                                    ENHANCE_RATE = rdr["ENHANCE_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["ENHANCE_RATE"]),
                                    PASSING_RATE = rdr["PASSING_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["PASSING_RATE"]),
                                    UNDER_PROTEST = rdr["UNDER_PROTEST"]?.ToString(),
                                    CHA_CHARGETYPE = rdr["CHA_CHARGETYPE"]?.ToString(),
                                    CONCOR_EXPS = rdr["CONCOR_EXPS"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["CONCOR_EXPS"]),
                                    CHA_AMT = rdr["CHA_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["CHA_AMT"]),
                                    CFS_CHARGES = rdr["CFS_CHARGES"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["CFS_CHARGES"]),           
                                    CFS_AMT = rdr["CFS_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["CFS_AMT"]),                 
                                    QC_AMTRECD = rdr["QC_AMTRECD"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["QC_AMTRECD"]),
                        
                                });
                            }
                        }
                    }
                }

                return new  { success = true, data = dataList };
            }
            catch (Exception ex)
            {
                return new { success = false,  message = ex.Message };
            }
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
        public JsonResult SendTo(string Textbox)
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string l1 = "";

                if (Textbox != null)
                {
                     l1 = " and d.full_name like '" + Textbox + "%'";
                }

                string query = @"Select distinct d.full_name,d.CODE from DOCFLOW_USERS a 
                    Left join DOCTYPE_MAST b on a.Doc_code=b.code 
                    Left join SUBUSER_MAST c on a.USER_CODE=c.USER_CODE 
                    Left join USER_MAST d on c.USER_CODE=d.code and 
                    a.COMP_CODE = d.COMP_CODE 
                    where a.Comp_code=1 and   a.DOC_CODE='' and SACTION='Receive Document'  " + l1 + "  ORDER by d.full_name  ";

                var SendTo = _dropdownService.GetDropdownList(query);
                return Json(SendTo);
            }

        }









        [HttpGet]
        public async Task<object> GetDocumentListData(DateTime FromDate, DateTime ToDate)
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
                        cmd.Parameters.Add("@FromDate", SqlDbType.SmallDateTime).Value = FromDate;
                        cmd.Parameters.Add("@ToDate", SqlDbType.SmallDateTime).Value = ToDate;

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
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
                                    SEND_DATE = rdr["SEND_DATE"]?.ToString(),
                                    ReceiverCode = rdr["ReceiverCode"]?.ToString(),
                                    ReceiverName = rdr["ReceiverName"]?.ToString(),
                                    RECD_DATE = rdr["RECD_DATE"]?.ToString(),
                                    SEND_FLAG = rdr["SEND_FLAG"]?.ToString(),
                                    RECD_FLAG = rdr["RECD_FLAG"]?.ToString(),
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
                return new { success = false, message = ex.Message };
            }
        }

    }
}
