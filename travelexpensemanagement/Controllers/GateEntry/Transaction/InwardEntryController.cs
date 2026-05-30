
using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Org.BouncyCastle.Bcpg.OpenPgp;
using StackExchange.Redis;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Interfaces;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    [SessionAuthorize] 
    public class InwardEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IInwardEntryRepository _inwardEntryRepository;
        private readonly HttpClient _httpClient;
        public int pubBPPurchTolQty = 2000;

        public InwardEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
            ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate , IInwardEntryRepository inwardEntryRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _inwardEntryRepository = inwardEntryRepository;
        }
        public IActionResult Index()
        {
            TempData["LoginDate"] = _globalVariableService.GetGlobalVariables().PubLoginDate;
            TempData["PubUserLevel"] = _globalVariableService.GetGlobalVariables().PubUserLevel;
            return View("~/Views/GateEntry/Transaction/InwardEntry/Index.cshtml");
        }
        public JsonResult GetVNo(string Vtype, string Tablename = "Gate1")
        {
            string newV_NO = "00000";
            try
            {
                newV_NO = _globalValidationdate.GetVNo(Vtype,Tablename);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO });
        }
        public async Task<JsonResult> GetDataByPartyCode(int PartyId, int addressid)
        {
            var data = await _inwardEntryRepository.GetDataByPartyCodeAsync(PartyId, addressid);
            return Json(data);
        }
        public async Task<JsonResult> GetPartyAddressbyCode(int PartyId)
        {
            var data = await _inwardEntryRepository.GetPartyAddressByCodeAsync(PartyId);
            return Json(data);
        }
        public async Task<JsonResult> fetchShipFromAdd(int ShipFromID)
        {
            var data = await _inwardEntryRepository.FetchShipFromAddressAsync(ShipFromID);
            return Json(data);
        }
        public class ApiResponse
        {
            public string Status { get; set; }
            public string Message { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SavedData([FromBody] InwardEntryModel request)
        {
            if (request?.Header == null)
            {
                return Json(new
                {
                    success = false,
                    status = "Error",
                    message = "Input model is null"
                });
            }

            var action = request.Header.action == "INSERT" ? "INSERT"  : "UPDATE";

            //    var action = request.Header.action == "INSERT" ? "INSERT" : "UPDATE";
            //    //var validation = Validation(  request.Header, request.Deatils);
            //    //if (validation.Status == "VALIDATION" || validation.Status == "Error")
            //    //{
            //    //    return Json(new { success = validation.Status == "VALIDATION", status = validation.Status, message = validation.Message });
            //    //}     

            var result = await SubmitRequest( request.Header, request.Deatils, action);

            return Json(new  { success = result.Status == "Success", status = result.Status,  message = result.Message });
        }

        private async Task<ApiResponse> SubmitRequest(InwardEntry_Header header, List<Details> details, string action)
        { try
            {
                string sql = "";
                var g = _globalVariableService.GetGlobalVariables();
                var LoadGeneralSetting = await  _globalVariableService.LoadGeneralSetting();
                using var conn = _dbConnection.GetErpConnection();
                var SUPPLIER_INVNOs = 0;
                int CountryCode = 0;

                Boolean isApprovalBody = false;
                Boolean isFinalApprovalBody = false;
                string DOC_APPROSTAGE = "";
                string APPROV_USER = "";
                string fappstatus = "";
                string fappRemark = "";
                string gstExmptflg = "0";

                DOC_APPROSTAGE = GetText("select 1 from DOC_APPROSTAGE where USER_CODE= " + g.PubUserId + " and DOC_CODE= '" + header.V_TYPE + "' and comp_code= " + g.PubCompCode + " ");

                if (DOC_APPROSTAGE == "1")
                {
                 isApprovalBody = true;
                }

                APPROV_USER = GetText("select APPROV_USER from DOC_APPROSTAGE where USER_CODE = " + g.PubCompCode + " and DOC_CODE = '" + header.V_TYPE + "' and comp_code = " + g.PubCompCode + "");

                conn.Open(); 

                foreach (var Details in details)
                {
                    if (string.IsNullOrWhiteSpace(Details.ITEM_NAME))
                        continue;
                    sql = @" SELECT Code  FROM ITEM_MAST  WHERE ISNULL(GST_EXAMPTED, '') = 'Yes'  AND Code = @Itemcode
                    AND Comp_code = @comp_code";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Itemcode", Details.ITEM_CODE);
                        cmd.Parameters.AddWithValue("@comp_code", g.PubCompCode);

                        object result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            string code = result.ToString();
                            if (code != "")
                            {
                                gstExmptflg = "1";
                            }
                            else
                            {
                                gstExmptflg = "0";
                            }
                        }
                    }
                }

                if (APPROV_USER == "FINAL")
                {
                  isFinalApprovalBody = true;
                }

                if (isFinalApprovalBody == true)
                {
                    fappstatus = "Approved";
                    fappRemark = "Document Approved.";
                }

                else if ((header.BILL_AMT) <= LoadGeneralSetting.PubDefEWaybillAmt)
                {
                    fappstatus = "Approved";
                    fappRemark = "Document Approved.";
                }

                else if (header.WAYBILL_NO  !=  "")
                {
                    fappstatus = "Approved";
                    fappRemark = "Document Approved.";
                }

                else if (gstExmptflg == "1")
                {
                    fappstatus = "Approved";
                    fappRemark = "Document Approved.";
                }       
                              
                if (action == "INSERT")
                {
                    var jsonResult = GetVNo(header.V_TYPE) as JsonResult;
                    dynamic data = jsonResult.Value;
                    header.V_NO = Convert.ToInt32(data.V_NO);
                }     
                
                 
                using (var cmd = new SqlCommand("sp_InwardEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@SaveAction", "Header");
                    cmd.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE) + header.V_NO);
                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", header.V_TYPE);
                    cmd.Parameters.AddWithValue("@v_NO", header.V_NO);
                    cmd.Parameters.Add("@RETURN_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE == null ? DBNull.Value : Convert.ToDateTime(header.V_DATE);
                    cmd.Parameters.AddWithValue("@V_TIME", header.V_TIME);
                    cmd.Parameters.Add("@R_DATE", SqlDbType.SmallDateTime).Value = header.R_DATE == null  ? DBNull.Value : Convert.ToDateTime(header.R_DATE);
                    cmd.Parameters.AddWithValue("@R_TIME", header.R_TIME);
                    cmd.Parameters.AddWithValue("@DISP_PLAN_NO", header.DISP_PLAN_NO);
                    cmd.Parameters.AddWithValue("@DISP_PLAN_TYPE", header.DISP_PLAN_TYPE);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", header.PARTY_CODE);
                    cmd.Parameters.AddWithValue("@PARTY_ADDRESSID", header.PARTY_ADDRESSID);
                    cmd.Parameters.AddWithValue("@BILL_NO", header.BILL_NO);
                    cmd.Parameters.Add("@BILL_DATE", SqlDbType.SmallDateTime).Value = header.BILL_DATE == null ? DBNull.Value : Convert.ToDateTime(header.BILL_DATE);
                    cmd.Parameters.AddWithValue("@BILL_AMT", header.BILL_AMT);
                    cmd.Parameters.AddWithValue("@CHALL_NO", header.CHALL_NO);
                    cmd.Parameters.Add("@CHALL_DATE", SqlDbType.SmallDateTime).Value = header.CHALL_DATE == null ? DBNull.Value : Convert.ToDateTime(header.CHALL_DATE);
                    cmd.Parameters.AddWithValue("@TRUCK_NO", header.TRUCK_NO);
                    cmd.Parameters.AddWithValue("@TRANSPORT_CODE", header.TRANSPORT_CODE);
                    cmd.Parameters.AddWithValue("@DRIVER_NAME", header.DRIVER_NAME);
                    cmd.Parameters.AddWithValue("@DRIVER_NO", header.DRIVER_NO);
                    cmd.Parameters.Add("@EWB_DATE", SqlDbType.SmallDateTime).Value =
                    header.EWB_EXPDATE == null ? DBNull.Value : Convert.ToDateTime(header.EWB_EXPDATE);                    
        
                    cmd.Parameters.AddWithValue("@EWB_INVNO", header.EWB_INVNO);
                    cmd.Parameters.AddWithValue("@EWB_INVAMT", header.EWB_INVAMT);
                    cmd.Parameters.AddWithValue("@PARTY_WBSLIPNO", header.PARTY_WBSLIPNO);
                    cmd.Parameters.AddWithValue("@PARTY_WBGRWT", header.PARTY_WBGRWT);
                    cmd.Parameters.AddWithValue("@PARTY_WBTRWT", header.PARTY_WBTRWT);
                    cmd.Parameters.AddWithValue("@PARTY_WBTIME", header.PARTY_WBTIME);
                    cmd.Parameters.AddWithValue("@PARTY_EWBCITY", header.PARTY_EWBCITY);
                    cmd.Parameters.AddWithValue("@TRANSIT_NO", header.TRANSIT_NO);
                    cmd.Parameters.AddWithValue("@WAYBILL_NO", header.WAYBILL_NO);
                    cmd.Parameters.AddWithValue("@REMARKS", header.REMARKS);
                    cmd.Parameters.AddWithValue("@Remarks2", header.Remarks2);
                    cmd.Parameters.AddWithValue("@ADD1", header.Add1);
                    cmd.Parameters.AddWithValue("@ADD2", header.Add2);
                    cmd.Parameters.AddWithValue("@ADD3", header.Add3);
                    cmd.Parameters.AddWithValue("@PARTY_CITY", header.PARTY_CITY);
                    cmd.Parameters.AddWithValue("@PARTY_GST", header.PARTY_GST);
                    cmd.Parameters.AddWithValue("@PARTY_PINCODE", header.PARTY_PINCODE);
                    cmd.Parameters.AddWithValue("@SHIP_PARTY", header.SHIP_PARTY);
                    cmd.Parameters.AddWithValue("@SHIP_BILLNO", header.SHIP_BILLNO);
                    cmd.Parameters.Add("@SHIP_BILLDATE", SqlDbType.SmallDateTime).Value = header.SHIP_BILLDATE == null ? DBNull.Value : Convert.ToDateTime(header.SHIP_BILLDATE);
                    cmd.Parameters.AddWithValue("@RETURN_TYPE", header.RETURN_TYPE);
                    cmd.Parameters.AddWithValue("@GR_NO", header.GR_NO);
                    cmd.Parameters.AddWithValue("@OUT_TIME", header.OUT_TIME);
                    cmd.Parameters.Add("@GR_DATE", SqlDbType.SmallDateTime).Value = header.GR_DATE == null ? DBNull.Value : Convert.ToDateTime(header.GR_DATE);
                    cmd.Parameters.AddWithValue("@RC_NO", header.RC_NO);
                    cmd.Parameters.AddWithValue("@DL_NO", header.DL_NO);
                    cmd.Parameters.AddWithValue("@INSU_NO", header.INSU_NO);
                    cmd.Parameters.AddWithValue("@PAN_NO", header.PAN_NO);
                    cmd.Parameters.AddWithValue("@STATUS", header.STATUS);
                    cmd.Parameters.AddWithValue("@INSU_EXPDT", header.INSU_EXPDT);
                    cmd.Parameters.AddWithValue("@DL_EXPDT", header.DL_EXPDT);
                    cmd.Parameters.AddWithValue("@FAPROV_STATUS", fappstatus);
                    cmd.Parameters.AddWithValue("@CONTAINER_NO", header.CONTAINER_NO);
                    cmd.Parameters.AddWithValue("@FAPROV_REMARKS",fappRemark);
                    cmd.Parameters.AddWithValue("@ACTIVE", header.ACTIVE);
                    cmd.Parameters.AddWithValue("@Out_Date", header.OUT_DATE);
                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.ExecuteNonQuery();
                }

                string deleteSql = @"DELETE FROM GATE2  WHERE COMP_CODE = @CompCode  AND V_NO = @VNo  AND BRANCH_CODE = @BranchCode   
                AND YEAR_CODE = @YearCode;";

                using (var deleteCmd = new SqlCommand(deleteSql, conn))
                {
                    deleteCmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                    deleteCmd.Parameters.AddWithValue("@VNo", header.V_NO);
                    deleteCmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                    deleteCmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);
                    deleteCmd.ExecuteNonQuery();
                }

                foreach (var Details in details)
                {
                    if (string.IsNullOrWhiteSpace(Details.ITEM_NAME))
                        continue;
                    using var cmd3 = new SqlCommand("sp_InwardEntry", conn) { CommandType = CommandType.StoredProcedure };
                    cmd3.Parameters.AddWithValue("@Action", action);
                    cmd3.Parameters.AddWithValue("@SaveAction", "Details");
                    cmd3.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd3.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd3.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd3.Parameters.AddWithValue("@V_TYPE", header.V_TYPE);
                    cmd3.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd3.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE == null ? DBNull.Value : Convert.ToDateTime(header.V_DATE);
                    cmd3.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE) + header.V_NO);
                    cmd3.Parameters.AddWithValue("@TRF_TYPE", "");
                    cmd3.Parameters.AddWithValue("@TRF_NO", "");
                    cmd3.Parameters.AddWithValue("@ITEM_CODE", Details.ITEM_CODE);
                    cmd3.Parameters.AddWithValue("@ITEM_NAME", Details.ITEM_NAME);
                    cmd3.Parameters.AddWithValue("@DEPT_CODE", Details.DEPT_CODE);
                    cmd3.Parameters.AddWithValue("@NOS", Details.NOS);
                    cmd3.Parameters.AddWithValue("@QTY", Details.QTY);
                    cmd3.Parameters.AddWithValue("@UOM_CODE", Details.UOM_CODE);
                    cmd3.Parameters.AddWithValue("@UOM_NAME", Details.UOM_NAME);
                    cmd3.Parameters.AddWithValue("@EMPTY", Details.EMPTY);
                    cmd3.Parameters.AddWithValue("@REMARKS", Details.REMARKS);
                    cmd3.Parameters.AddWithValue("@REF_TYPE", Details.REF_TYPE);
                    cmd3.Parameters.AddWithValue("@REF_NO", Details.REF_NO);
                    cmd3.Parameters.AddWithValue("@MRN_TYPE", Details.MRN_TYPE);
                    cmd3.Parameters.AddWithValue("@MRN_NO", Details.MRN_NO);
                    cmd3.Parameters.AddWithValue("@STATUS", Details.STATUS);
                    cmd3.Parameters.AddWithValue("@ADJ_QTY", Details.ADJ_QTY);
                    cmd3.Parameters.AddWithValue("@BALANCEQTY", Details.BALANCEQTY);
                    cmd3.Parameters.AddWithValue("@SHIP_RATE", Details.SHIP_RATE);
                    cmd3.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd3.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd3.Parameters.AddWithValue("@EUSER", g.PubUserId);
                    cmd3.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd3.Parameters.AddWithValue("@AED", "A");
                    cmd3.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd3.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd3.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd3.ExecuteNonQuery();
                }

                //if (action == "UPDATE")
                //{
                //    _globalValidationdate.LogInsertUpdateDelete(destinationTable: "gate1", sourceTable: "gate1", transactionType: "Transaction",
                //    codeVNo: header.V_NO.ToString(), vtype: header.V_TYPE);
                //}
                return new ApiResponse { Status = "Success", Message = "Data Save Successfully" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = "Error", Message = ex.Message };
            }
        }

        public string GetText(string query)
        {


            try
            {
                using var con = _dbConnection.GetErpConnection();
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return reader[0].ToString();
                            }
                            else
                            {
                                return string.Empty;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetText() Error: " + ex.Message);
                return string.Empty;
            }
        }

        private ApiResponse Validation(InwardEntry_Header header, List<Details> details)
        {
            try
            {
                string sql = "";
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                var SUPPLIER_INVNOs = 0;
                int CountryCode = 0;
                conn.Open();
                string Message = "";

                using (var cmd1 = new SqlCommand("sp_InwardEntry", conn))
                {
                    cmd1.CommandType = CommandType.StoredProcedure;
                    cmd1.Parameters.AddWithValue("@Party_Code", header.PARTY_CODE);
                    cmd1.Parameters.AddWithValue("@V_No", header.V_NO);
                    cmd1.Parameters.AddWithValue("@comp_Code", g.PubCompCode);
                    cmd1.Parameters.AddWithValue("@Branch_Code", g.PubBranchCode);
                    cmd1.Parameters.AddWithValue("@Action", "GatenoModifica");
                    using var reader1 = cmd1.ExecuteReader();
                    var response = new ApiResponse();
                    if (reader1.Read())
                    {
                        var V_NO = reader1["V_No"].ToString();
                        if (g.PubUserId != "1" && g.PubUserId != "53")
                        {
                            Message = $"Gate no. {V_NO} exist in MRN No. {header.V_NO} Modification not allowed.";
                            return new ApiResponse { Status = "VALIDATION", Message = Message };
                        }
                    }
                }

                if (header.TRANSIT_NO != null)
                {
                    using (var cmd1 = new SqlCommand("sp_InwardEntry", conn))
                    {
                        cmd1.CommandType = CommandType.StoredProcedure;
                        cmd1.Parameters.AddWithValue("@V_NO", header.TRANSIT_NO);
                        cmd1.Parameters.AddWithValue("@Party_Code", header.PARTY_CODE);
                        cmd1.Parameters.AddWithValue("@comp_Code", g.PubCompCode);
                        cmd1.Parameters.AddWithValue("@Branch_Code", g.PubBranchCode);
                        cmd1.Parameters.AddWithValue("@Action", "TRANSIT_NO");
                        using var reader1 = cmd1.ExecuteReader();
                        var response = new ApiResponse();
                        if (reader1.Read())
                        {
                            var V_NO = reader1["V_NO"];
                            Message = $"Transit no. not valid for Party=> {header.PARTY_NAME}";
                            return new ApiResponse { Status = "VALIDATION", Message = Message };
                        }
                    }
                }

                if (header.WAYBILL_NO != null)
                {
                    using (var cmd1 = new SqlCommand("sp_InwardEntry", conn))
                    {
                        cmd1.CommandType = CommandType.StoredProcedure;

                        cmd1.Parameters.AddWithValue("@WAYBILL_NO", header.TRANSIT_NO);
                        cmd1.Parameters.AddWithValue("@PARTY_CODE", header.PARTY_CODE);
                        cmd1.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd1.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd1.Parameters.AddWithValue("@Action", "WAYBILL_NO");
                        using var reader1 = cmd1.ExecuteReader();
                        var response = new ApiResponse();
                        if (reader1.Read())
                        {
                            Message = $"Waybill no. not valid for Party=>{header.PARTY_NAME}, Please check in Transit Entry.";
                            return new ApiResponse { Status = "VALIDATION", Message = Message };
                        }
                    }
                }

                if (header.TRANSIT_NO != null)
                {

                    using (var cmd1 = new SqlCommand("sp_InwardEntry", conn))
                    {
                        cmd1.CommandType = CommandType.StoredProcedure;

                        cmd1.Parameters.AddWithValue("@TRANSIT_NO", header.TRANSIT_NO);
                        cmd1.Parameters.AddWithValue("COMP_CODE", g.PubCompCode);
                        cmd1.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd1.Parameters.AddWithValue("@Action", "Trnsitnowaybillno");
                        using var reader1 = cmd1.ExecuteReader();
                        var response = new ApiResponse();
                        if (reader1.Read())
                        {
                            var V_NO = reader1["V_NO"];
                            Message = $"Transit no. {header.TRANSIT_NO} exist in MRN No.= {V_NO}";
                            return new ApiResponse { Status = "VALIDATION", Message = Message };
                        }
                    }
                }

                if (header.WAYBILL_NO != null)
                {
                    string BillNoDate = "";
                    string BILL_NO = "";
                    DateTime? BILL_DATE = null;

                    using (SqlCommand cmd = new SqlCommand("sp_InwardEntry", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@WAYBILL_NO", header.WAYBILL_NO);
                        cmd.Parameters.AddWithValue("@comp_Code", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@Branch_Code", g.PubBranchCode);
                        cmd.Parameters.AddWithValue("@Action", "GETWAYBILL_NOData");
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                BillNoDate = reader["BillNoDate"]?.ToString();
                                BILL_NO = reader["BILL_NO"]?.ToString();
                                if (reader["BILL_DATE"] != DBNull.Value)
                                BILL_DATE = Convert.ToDateTime(reader["BILL_DATE"]);
                            }
                        }
                    }

                    // Bill Validation
                    if (!string.IsNullOrEmpty(BillNoDate))
                    {
                        if (!string.IsNullOrEmpty(header.BILL_NO))
                        {
                            if (!string.IsNullOrEmpty(BILL_NO) &&
                                header.BILL_NO.Trim() != BILL_NO.Trim())
                            {
                                Message = $"Bill No. {header.BILL_NO} not matched with Bill No. {BILL_NO} in Transit Entry No => {header.TRANSIT_NO}";

                                return new ApiResponse
                                {
                                    Status = "VALIDATION",
                                    Message = Message
                                };
                            }

                            if (BILL_DATE.HasValue &&
                                header.BILL_DATE?.Date != BILL_DATE.Value.Date)
                            {
                                Message = $"Bill Date {header.BILL_DATE:dd/MM/yyyy} not matched with Bill Date {BILL_DATE:dd/MM/yyyy} in Transit Entry No => {header.TRANSIT_NO}";

                                return new ApiResponse
                                {
                                    Status = "VALIDATION",
                                    Message = Message
                                };
                            }
                        }

                        // Challan Validation
                        if (!string.IsNullOrEmpty(header.CHALL_NO))
                        {
                            if (!string.IsNullOrEmpty(BILL_NO) &&
                                header.CHALL_NO.Trim() != BILL_NO.Trim())
                            {
                                Message = $"Challan No. {header.CHALL_NO} not matched with Challan No. {BILL_NO} in Transit Entry No => {header.TRANSIT_NO}";

                                return new ApiResponse
                                {
                                    Status = "VALIDATION",
                                    Message = Message
                                };
                            }

                            if (BILL_DATE.HasValue &&
                                header.CHALL_DATE?.Date != BILL_DATE.Value.Date)
                            {
                                Message = $"Challan Date {header.CHALL_DATE:dd/MM/yyyy} not matched with Challan Date {BILL_DATE:dd/MM/yyyy} in Transit Entry No => {header.TRANSIT_NO}";

                                return new ApiResponse
                                {
                                    Status = "VALIDATION",
                                    Message = Message
                                };
                            }
                        }
                    }
                }

                if (header.V_TYPE == "INRM")
                {
                    sql = @"SELECT COUNTRY_CODE  FROM SUBGROUP_MAST  WHERE CODE = @FORM_NO   AND Comp_Code = @CompCode  AND ACTIVE = 1;";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@FORM_NO", SqlDbType.BigInt)
                        .Value = Convert.ToInt64(header.WAYBILL_NO);
                        cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);

                        using var READERS = cmd.ExecuteReader();

                        var response = new ApiResponse();

                        if (READERS.Read())
                        {
                            CountryCode = Convert.ToInt32(READERS["COUNTRY_CODE"]);

                        }
                    }

                    var INV_NO = 0;
                    if (CountryCode != 1)
                    {
                        sql = @"SELECT  INV_NO FROM ORDER4  WHERE INV_NO =@INV_NO AND PARTY_CODE = @PARTY_CODE  AND COMP_CODE = @COMP_CODE;";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@INV_NO", header.BILL_NO);
                            cmd.Parameters.AddWithValue("@PARTY_CODE", header.PARTY_CODE);
                            cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                            using var READERS = cmd.ExecuteReader();
                            var response = new ApiResponse();
                            if (READERS.Read())
                            {
                                INV_NO = Convert.ToInt32(READERS["INV_NO"]);
                            }
                        }
                    }

                    if (INV_NO != null)
                    {
                        sql = @"SELECT SUPPLIER_INVNO  FROM EXIM1 WHERE SUPPLIER_INVNO = @SUPPLIER_INVNO   AND  SUPPLIER = @SUPPLIER AND COMP_CODE = @COMP_CODE;";
                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@SUPPLIER_INVNO", header.BILL_NO);
                            cmd.Parameters.AddWithValue("@SUPPLIER", header.PARTY_CODE);
                            cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                            using var READERS = cmd.ExecuteReader();
                            var response = new ApiResponse();
                            if (READERS.Read())
                            {
                                Message = $"Bill No.  {header.BILL_NO} not matched with Bill No in Container Tracking Record.";
                                return new ApiResponse { Status = "VALIDATION", Message = Message };
                            }
                        }
                    }
                }

                foreach (var Details in details)
                {
                    if (string.IsNullOrWhiteSpace(Details.ITEM_NAME))
                        continue;

                    if (header.V_TYPE == "INFU" && header.V_TYPE == "INST" && header.V_TYPE == "INRM")
                    {
                        sql = @"SELECT Party_Code FROM Order1  WHERE Party_Code = @PartyCode  AND V_TYPE = @V_TYPE
                        AND V_NO = @V_NO   AND COMP_CODE = @CompCode AND Branch_Code = @BranchCode;";
                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@PartyCode", header.PARTY_CODE);
                            cmd.Parameters.AddWithValue("@V_TYPE", Details.REF_TYPE);
                            cmd.Parameters.AddWithValue("@V_NO", Details.REF_NO);
                            cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                            cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                            using var READERS = cmd.ExecuteReader();
                            var response = new ApiResponse();
                            if (READERS.Read())
                            {
                                var Party_Code = READERS["Party_Code"].ToString();
                                Message = $"Party Name not matched with Order Party Name";
                                return new ApiResponse { Status = "VALIDATION", Message = Message };
                            }
                        }
                    }
                    if (header.V_TYPE == "INRT")
                    {
                        sql = @"SELECT Party_Code FROM GATE1 WHERE Party_Code = @PartyCode  AND V_TYPE = @V_TYPE AND V_NO = @V_NO 
                        AND COMP_CODE = @CompCode AND Branch_Code = @BranchCode;";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@PartyCode", header.PARTY_CODE);
                            cmd.Parameters.AddWithValue("@V_TYPE", Details.REF_TYPE);
                            cmd.Parameters.AddWithValue("@V_NO", Details.REF_NO);
                            cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                            cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);

                            using var READERS = cmd.ExecuteReader();

                            var response = new ApiResponse();

                            if (READERS.Read())
                            {
                                var Party_Code = READERS["Party_Code"].ToString();
                                Message = $"Party Name not matched with Gate Out Entry Party Name.";
                                return new ApiResponse { Status = "VALIDATION", Message = Message };

                            }
                        }
                    }
                    if (header.V_TYPE == "INSR")
                    {
                        sql = @"SELECT Party_Code  FROM SALE1  WHERE Party_Code = @PartyCode   AND V_TYPE =@V_TYPE  AND V_NO = @V_NO 
                        AND COMP_CODE = @CompCode AND Branch_Code = @BranchCode;";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@PartyCode", header.PARTY_CODE);
                            cmd.Parameters.AddWithValue("@V_TYPE", Details.REF_TYPE);
                            cmd.Parameters.AddWithValue("@V_NO", Details.REF_NO);
                            cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                            cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);

                            using var READERS = cmd.ExecuteReader();

                            var response = new ApiResponse();

                            if (READERS.Read())
                            {
                                var Party_Code = READERS["Party_Code"].ToString();
                                Message = $"Party Name not matched with Sale Return Entry Party Name..";
                                return new ApiResponse { Status = "VALIDATION", Message = Message };

                            }
                        }

                    }
                    if (header.V_TYPE == "INFU" || header.V_TYPE == "INST" || header.V_TYPE == "INRM")
                    {
                        if (Details.REF_TYPE == "PAUD")
                        {
                            if (!string.IsNullOrEmpty(Details.ITEM_NAME) && Details.REF_NO > 0)
                            {
                                string gateNosQuery = @"  DECLARE @cols AS VARCHAR(200) SELECT @cols = STUFF((
                                SELECT ',' + CAST(V_No AS VARCHAR(20))   FROM gate2   WHERE ref_TYPE = @RefType 
                                AND ref_NO = @RefNo   AND COMP_CODE = @CompCode   AND BRANCH_CODE = @BranchCode    AND v_type = @VType
                                FOR XML PATH(''), TYPE).value('.', 'VARCHAR(MAX)'), 1, 1, '') SELECT @cols";

                                string gateNos = "";
                                using (var cmd = new SqlCommand(gateNosQuery, conn))
                                {
                                    cmd.Parameters.AddWithValue("@RefType", Details.REF_TYPE);
                                    cmd.Parameters.AddWithValue("@RefNo", Details.REF_NO);
                                    cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@VType", header.V_TYPE);

                                    var result = cmd.ExecuteScalar();
                                    gateNos = result?.ToString();
                                }

                                double pubRes1Dbl = 0;
                                string sql1 = @"SELECT ISNULL(SUM(qty),0)  FROM SAUDA   WHERE V_TYPE = @RefType  AND V_NO = @RefNo 
                                AND COMP_CODE = @CompCode   AND BRANCH_CODE = @BranchCode";

                                using (var cmd = new SqlCommand(sql1, conn))
                                {
                                    cmd.Parameters.AddWithValue("@RefType", Details.REF_TYPE);
                                    cmd.Parameters.AddWithValue("@RefNo", Details.REF_NO);
                                    cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);

                                    pubRes1Dbl = Convert.ToDouble(cmd.ExecuteScalar());
                                }

                                double pubRes2Dbl = 0;
                                string sql2 = @"SELECT ISNULL(SUM(qty),0) FROM gate2 WHERE ref_TYPE = @RefType  AND ref_NO = @RefNo  
                                 AND COMP_CODE = @CompCode 
                                AND BRANCH_CODE = @BranchCode  AND v_type = @VType   AND v_no <> @VNo";

                                using (var cmd = new SqlCommand(sql2, conn))
                                {
                                    cmd.Parameters.AddWithValue("@RefType", Details.REF_TYPE);
                                    cmd.Parameters.AddWithValue("@RefNo", Details.REF_NO);
                                    cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@VType", header.V_TYPE);
                                    cmd.Parameters.AddWithValue("@VNo", header.V_NO);

                                    pubRes2Dbl = Convert.ToDouble(cmd.ExecuteScalar());
                                }

                                pubRes2Dbl += Convert.ToDouble(Details.QTY);

                                if (pubRes2Dbl > pubRes1Dbl + pubBPPurchTolQty)
                                {
                                    double pendingQty = pubRes1Dbl - pubRes2Dbl + Convert.ToDouble(Details.QTY) + pubBPPurchTolQty;

                                    Message = $"Pending Sauda Quantity is {pendingQty} (including tolerance) " +
                                              $"and Gate Quantity is {Details.QTY}\n" +
                                              $"Sauda already exists in Gate No => {gateNos}";

                                    return new ApiResponse { Status = "VALIDATION", Message = Message };
                                }
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(Details.ITEM_NAME) && Details.REF_NO > 0)
                            {
                                double pubRes1Dbl = 0;
                                string sql1 = @"SELECT ISNULL(SUM(qty),0) 
                                FROM order2  
                                WHERE V_TYPE = @RefType 
                                AND V_NO = @RefNo 
                                AND COMP_CODE = @CompCode 
                                AND BRANCH_CODE = @BranchCode 
                                AND item_code = @ItemCode";

                                using (var cmd = new SqlCommand(sql1, conn))
                                {
                                    cmd.Parameters.AddWithValue("@RefType", Details.REF_TYPE);
                                    cmd.Parameters.AddWithValue("@RefNo", Details.REF_NO);
                                    cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@ItemCode", Details.ITEM_CODE);

                                    pubRes1Dbl = Convert.ToDouble(cmd.ExecuteScalar());
                                }

                                double pubRes2Dbl = 0;
                                string sql2 = @"SELECT ISNULL(SUM(qty),0)   FROM gate2   WHERE ref_TYPE = @RefType  AND ref_NO = @RefNo 
                                AND COMP_CODE = @CompCode  AND BRANCH_CODE = @BranchCode   AND item_code = @ItemCode   AND v_type <> @VType 
                                AND v_no <> @VNo";

                                using (var cmd = new SqlCommand(sql2, conn))
                                {
                                    cmd.Parameters.AddWithValue("@RefType", Details.REF_TYPE);
                                    cmd.Parameters.AddWithValue("@RefNo", Details.REF_NO);
                                    cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@ItemCode", Details.ITEM_CODE);
                                    cmd.Parameters.AddWithValue("@VType", header.V_TYPE);
                                    cmd.Parameters.AddWithValue("@VNo", header.V_NO);

                                    pubRes2Dbl = Convert.ToDouble(cmd.ExecuteScalar());
                                }

                                pubRes2Dbl += Convert.ToDouble(Details.QTY);

                                if (pubRes2Dbl > pubRes1Dbl)
                                {

                                    double pendingQty = pubRes1Dbl - pubRes2Dbl + Convert.ToDouble(Details.QTY);

                                    Message = $"Pending Order Quantity is {pendingQty} " +
                                              $"and Gate Quantity is {Details.QTY}. (Item Name: {Details.ITEM_NAME})";

                                    return new ApiResponse { Status = "VALIDATION", Message = Message };
                                }
                            }
                        }
                    }
                                 
                }

                return new ApiResponse { Status = "Success", Message = Message };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = "Error", Message = ex.Message };
            }
        }
        [HttpGet]
        public async Task<IActionResult> BillNoValidation(int PARTY_CODE, string BILL_NO, int V_NO)
        {
            var result = await _inwardEntryRepository.ValidateBillNoAsync(PARTY_CODE, BILL_NO, V_NO);

            return Json(new
            {  success = result.status == true,  message = result.message });
        }
        [HttpGet]
        public async Task<IActionResult> GatenoValidation(string V_TYPE, int V_NO)
        {
            var result = await _inwardEntryRepository.ValidateGateNoAsync(V_TYPE, V_NO);

            return Json(new
            {
                success = result.status == true,
                message = result.message
            });
        }
        [HttpGet]
        public async Task<JsonResult> GetVehcleinfo(string rc_number, string VType, int VNo)
        {
            var res = await _globalValidationdate.GetVehicleInfo(rc_number, VType, VNo);
            return new JsonResult(res);
        }
        public JsonResult GetVehicledetail(int v_no, string v_type)
        {
            try
            {
                var global = _globalVariableService.GetGlobalVariables();

                using (var conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    using (var cmd = new SqlCommand("sp_InwardEntry", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_NO", v_no);
                        cmd.Parameters.AddWithValue("@V_TYPE", v_type);
                        cmd.Parameters.AddWithValue("@Action", "GetVehicledetail");


                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var result = new
                                {
                                    client_id = reader["client_id"],
                                    rc_number = reader["rc_number"],
                                    registration_date = reader["registration_date"],
                                    maker_description = reader["maker_description"],
                                    owner_name = reader["owner_name"],
                                    father_name = reader["father_name"],
                                    permanent_address = reader["permanent_address"],
                                    mobile_number = reader["mobile_number"],
                                    maker_model = reader["maker_model"],
                                    present_address = reader["present_address"],
                                    vehicle_category = reader["vehicle_category"],
                                    vehicle_chasi_number = reader["vehicle_chasi_number"],
                                    vehicle_engine_number = reader["vehicle_engine_number"],
                                    body_type = reader["body_type"],
                                    fuel_type = reader["fuel_type"],
                                    color = reader["color"],
                                    norms_type = reader["norms_type"],
                                    fit_up_to = reader["fit_up_to"],
                                    financer = reader["financer"],
                                    financed = reader["financed"],
                                    insurance_company = reader["insurance_company"],
                                    insurance_policy_number = reader["insurance_policy_number"],
                                    insurance_upto = reader["insurance_upto"],
                                    manufacturing_date = reader["manufacturing_date"],
                                    manufacturing_date_formatted = reader["manufacturing_date_formatted"],
                                    registered_at = reader["registered_at"],
                                    less_info = reader["less_info"],
                                    tax_upto = reader["tax_upto"],
                                    tax_paid_upto = reader["tax_paid_upto"],
                                    cubic_capacity = reader["cubic_capacity"],
                                    vehicle_gross_weight = reader["vehicle_gross_weight"],
                                    no_cylinders = reader["no_cylinders"],
                                    seat_capacity = reader["seat_capacity"],
                                    sleeper_capacity = reader["sleeper_capacity"],
                                    standing_capacity = reader["standing_capacity"],
                                    wheelbase = reader["wheelbase"],
                                    unladen_weight = reader["unladen_weight"],
                                    vehicle_category_description = reader["vehicle_category_description"],
                                    pucc_number = reader["pucc_number"],
                                    pucc_upto = reader["pucc_upto"],
                                    permit_number = reader["permit_number"],
                                    permit_issue_date = reader["permit_issue_date"],
                                    permit_valid_from = reader["permit_valid_from"],
                                    permit_valid_upto = reader["permit_valid_upto"],
                                    permit_type = reader["permit_type"],
                                    national_permit_number = reader["national_permit_number"],
                                    national_permit_upto = reader["national_permit_upto"],
                                    national_permit_issued_by = reader["national_permit_issued_by"],
                                    non_use_status = reader["non_use_status"],
                                    non_use_from = reader["non_use_from"],
                                    non_use_to = reader["non_use_to"],
                                    blacklist_status = reader["blacklist_status"],
                                    noc_details = reader["noc_details"],
                                    owner_number = reader["owner_number"],
                                    rc_status = reader["rc_status"],
                                    masked_name = reader["masked_name"],
                                    challan_details = reader["challan_details"]
                                };

                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult(new { message = "No data found" });
                            }
                        }
                    }
                }
            }
            catch (Exception er)
            {
                return new JsonResult(new { error = er.Message });
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetVehcleFastaginfocall([FromQuery] string rc_number, string VType, int VNo)
        {
            try
            {  
                var res = await _globalValidationdate.GetVehcleFastaginfo(rc_number, VType, VNo);
                return new JsonResult(new { success = true,  data = res });
            }
            catch (HttpRequestException ex)
            {
                return new JsonResult(new { error = "Request failed", details = ex.Message });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = "Unexpected error", details = ex.Message });
            }
        }
        public JsonResult GetFasttagdetail(int v_no, string v_type)
        {
            try
            {
                var global = _globalVariableService.GetGlobalVariables();

                using (var conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    using (var cmd = new SqlCommand("sp_InwardEntry", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_NO", v_no);
                        cmd.Parameters.AddWithValue("@V_TYPE", v_type);
                        cmd.Parameters.AddWithValue("@Action", "GETFASTTAGDETAIL");

                        using (var reader = cmd.ExecuteReader())
                        {
                            var list = new List<object>();

                            while (reader.Read())
                            {
                                var result = new
                                {
                                    client_id = reader["ClientId"],
                                    rc_number = reader["RcNumber"],
                                    BankName = reader["BankName"],
                                    TagId = reader["TagId"],
                                    Status = reader["Status"],
                                    LaneDirection = reader["LaneDirection"],
                                    TransactionDateTime = reader["TransactionDateTime"],
                                    SeqNo = reader["SeqNo"],
                                    TollPlazaGeoCode = reader["TollPlazaGeoCode"],
                                    TollPlazaName = reader["TollPlazaName"],
                                    VehicleType = reader["VehicleType"]
                                };

                                list.Add(result);
                            }

                            if (list.Count > 0)
                                return new JsonResult(list);
                            else
                                return new JsonResult(new { message = "No data found" });
                        }
                    }
                }
            }
            catch (Exception er)
            {
                return new JsonResult(new { error = er.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("Gate1", vdate, vtype, vno);
            return Ok(result);
        }
        public async Task<JsonResult> GetSEARCHCONTAINER(string Container_No)
        {
            var res = await _inwardEntryRepository.GetSEARCHCONTAINERAsync(Container_No);
            return Json(new {  StatusCode = res.status, message = res.message,  supplier = res.data });
        }
        [HttpGet]
        public async Task<JsonResult> DDlTransitNo(string v_type, int v_no, int partycode, DateTime ExpiryDate)
        {
            var res = await _inwardEntryRepository.DDlTransitNoAsync(v_type, v_no, partycode, ExpiryDate);
            return Json(res);
        }
        [HttpGet]
        public async Task<JsonResult> GetEWayBillDatacall(DateTime edate, string inoutdata)
        {
            try
            {    
                var result = await _globalValidationdate.GetEWayBillData(edate, inoutdata);
                return result;
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
        public JsonResult fetchSelectedAddress(int PartyId)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"
                SELECT DISTINCT  address_id AS code, add1 AS name  FROM  SUBGROUP_ADDRESS 
                WHERE  code = " + PartyId + " AND COMP_CODE = " + getdata.PubCompCode + "    and ADD1 <> ''  ORDER BY  ADDRESS_ID;";
                var selectAddList = _dropdownService.GetDropdownList(query);
                return Json(selectAddList);
            }

        }
        public JsonResult DDlVType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('GateInward') order by Name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);        
            }
        }
        public JsonResult DDlParty()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select CODE, name from SUBGROUP_MAST where Nature in ('Customer','Supplier','Broker','Staff') and COMP_CODE = " + getdata.PubCompCode + "    AND ACTIVE=1  and name <> '' order by name ";

                var Partylist = _dropdownService.GetDropdownList(query);

                return Json(Partylist);
            }

        }
        public JsonResult DDlShipFrom()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select CODE, name from SUBGROUP_MAST where Nature in ('Customer','Supplier','Broker','Staff') and COMP_CODE =" + getdata.PubCompCode + " AND ACTIVE=1 and name <> ''    order by name ";

                var ShipFromList = _dropdownService.GetDropdownList(query);

                return Json(ShipFromList);
            }

        }
        public JsonResult DDDocStatus()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document'   and Name <> ''  Order by CODE";

                var DocStatusList = _dropdownService.GetDropdownList(query);

                return Json(DocStatusList);
            }

        }
        public JsonResult DDlPartycity()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from City_mast  Where Name <> ''  Order by name";

                var PartyCitylist = _dropdownService.GetDropdownList(query);

                return Json(PartyCitylist);
            }

        }
        public JsonResult DDlstate()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select CODE , name from STATE_MAST  where active = 1  AND name <> '' order by NAME ";

                var DDlstate = _dropdownService.GetDropdownList(query);

                return Json(DDlstate);
            }

        }
        public JsonResult DDlTransportName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE , NAME  FROM TRANSPORT_MAST  WHERE COMP_CODE =" + getdata.PubCompCode + "  AND ACTIVE = 1  and NAME <> ''   order by NAME asc";

                var TransportNamelist = _dropdownService.GetDropdownList(query);

                return Json(TransportNamelist);
            }

        }
        public JsonResult DDlItemMast()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select b.CODE , b.name from item_mast b where B.ACTIVE=1 AND b.comp_code=" + getdata.PubCompCode + " group by b.name ,b.CODE  order by b.name ";
                var ItemList = _dropdownService.GetDropdownList(query);
                return Json(ItemList);
            }
        }
        public JsonResult DDlDeptMast()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select b.CODE , b.name  from ITEMDEPT_MAST b where B.ACTIVE=1 and b.Tran_type='Store' AND b.comp_code=" + getdata.PubCompCode + "  group by b.name ,b.CODE  order by b.name ";
                var DeptList = _dropdownService.GetDropdownList(query);
                return Json(DeptList);
            }
        }
        public JsonResult DDlUnitMast()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select  b.CODE , b.name  from ITEMUNIT_MAST b where B.ACTIVE=1 AND b.comp_code=" + getdata.PubCompCode + " group by b.name ,b.CODE  order by b.name ";
                var UnitList = _dropdownService.GetDropdownList(query);
                return Json(UnitList);
            }
        }
        public async Task<JsonResult> Approvalbtn(string v_type, int v_no)
        {
            try
            {
                var globalvariable = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                // First Check 
                using SqlCommand cmd = new SqlCommand("sp_InwardEntry", conn);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "ApprovalbtnShow");
                cmd.Parameters.AddWithValue("@v_type", v_type);
                cmd.Parameters.AddWithValue("@v_no", v_no);
                cmd.Parameters.AddWithValue("@comp_code", globalvariable.PubCompCode);
                cmd.Parameters.AddWithValue("@branch_code", globalvariable.PubBranchCode);
                cmd.Parameters.AddWithValue("@year_code", globalvariable.PubFYearCode);
                cmd.Parameters.AddWithValue("@UUSER", globalvariable.PubUserId);

                object result = await cmd.ExecuteScalarAsync();

                bool isApproved = result != null;

                if (isApproved)
                {
                    return new JsonResult(new { success = false, approved = true ,message = "SendForApproval" });
                }

                return new JsonResult(new { success = true, approved = false , message = "ApprovalWindow" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
        public async Task<JsonResult> Approval(string v_type, int v_no)
        {
            try
            {
                var globalvariable = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                // First Check 
                using SqlCommand cmd = new SqlCommand("sp_InwardEntry", conn);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "Approval");
                cmd.Parameters.AddWithValue("@v_type", v_type);
                cmd.Parameters.AddWithValue("@v_no", v_no);
                cmd.Parameters.AddWithValue("@comp_code", globalvariable.PubCompCode);
                cmd.Parameters.AddWithValue("@branch_code", globalvariable.PubBranchCode);
                cmd.Parameters.AddWithValue("@year_code", globalvariable.PubFYearCode);

                object result = await cmd.ExecuteScalarAsync();

                bool isApproved = result != null;

                if (isApproved)
                {
                    return new JsonResult(new { success = false, approved = true, message = "Document Approved. Approval not Required." });
                }
                // Second Check
                using SqlCommand cmd2 = new SqlCommand("sp_InwardEntry", conn);
                cmd2.CommandType = CommandType.StoredProcedure;
                cmd2.Parameters.AddWithValue("@Action", "ApprovalStatus");
                cmd2.Parameters.AddWithValue("@v_type", v_type);
                cmd2.Parameters.AddWithValue("@v_no", v_no);
                cmd2.Parameters.AddWithValue("@comp_code", globalvariable.PubCompCode);
                cmd2.Parameters.AddWithValue("@branch_code", globalvariable.PubBranchCode);
                cmd2.Parameters.AddWithValue("@year_code", globalvariable.PubFYearCode);
                cmd2.Parameters.AddWithValue("@UUSER", globalvariable.PubUserId);
                object result1 = await cmd2.ExecuteScalarAsync();
                string approvalStatus = result1?.ToString();
                if (!string.IsNullOrEmpty(approvalStatus))
                {

                    using SqlCommand cmd3 = new SqlCommand("sp_InwardEntry", conn);
                    cmd2.CommandType = CommandType.StoredProcedure;
                    cmd2.Parameters.AddWithValue("@Action", "DocumentProcess");
                    cmd2.Parameters.AddWithValue("@v_type", v_type);
                    cmd2.Parameters.AddWithValue("@v_no", v_no);
                    cmd2.Parameters.AddWithValue("@comp_code", globalvariable.PubCompCode);
                    cmd2.Parameters.AddWithValue("@branch_code", globalvariable.PubBranchCode);
                    cmd2.Parameters.AddWithValue("@year_code", globalvariable.PubFYearCode);
                    cmd2.Parameters.AddWithValue("@UUSER", globalvariable.PubUserId);
                    object result2 = await cmd3.ExecuteScalarAsync();
                    string user_name = result2?.ToString();
                    return new JsonResult(new {  success = false, approved = false, message = "This Document Approval is in process at User:"+ user_name + " " });
                }
               
                return new JsonResult(new {   success = true, approved = false,  message = approvalStatus });
            }
            catch (Exception ex)
            {
                return new JsonResult(new {  success = false,  message = ex.Message });
            }
        }
        public JsonResult DDlApprovalRemark()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select CODE,NAME from APPROVAL_RMKS Order by code";
                var DDlApprovalRemark = _dropdownService.GetDropdownList(query);
                return Json(DDlApprovalRemark);
            }
        }
        public JsonResult DDlSendTo(string v_type)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT  a.USER_CODE, b.FULL_NAME FROM DOC_APPROSTAGE a LEFT JOIN CONDATABASE.dbo.USER_MAST b " +
                "   ON a.USER_CODE = b.CODE LEFT JOIN CONDATABASE.dbo.SUBUSER_MAST c     ON b.CODE = c.USER_CODE  " +
                " AND c.COMP_CODE = "+  getdata.PubCompCode +"  WHERE b.Active = 1  AND a.User_Code <> "+  getdata.PubUserId +" " +
                " AND a.DOC_CODE = '" +  v_type + "'    AND a.comp_code = "+  getdata.PubCompCode +"  " +
                "GROUP BY      a.USER_CODE, b.FULL_NAME,  a.SRNO  ORDER BY a.SRNO;";
                var DDlSendTo = _dropdownService.GetDropdownList(query);
                return Json(DDlSendTo);
            }
        }




        [HttpPost]
        public async Task<JsonResult> SendApproval( string vtype, int vno, DateTime vDate, string appStatus, string appRemark,
           int SendTo,  string menuCode, string formName, string deptName , string STATUS , String TableName)
        {
            try
            {
                var globalvariable = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();
                using var cmd = new SqlCommand("USP_SEND_APPROVAL", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                int approvalCode = 0;
                switch ((appStatus ?? "").ToUpper())
                {
                    case "APPROVED":
                        approvalCode = 8;
                        break;

                    case "REJECT":
                        approvalCode = 9;
                        break;

                    case "HOLD":
                        approvalCode = 7;
                        break;

                    default:
                        approvalCode = 0;
                        break;
                }

                //string origicode = GetText("SELECT UUSER  FROM GATE1 WHERE COMP_CODE="+ globalvariable.PubCompCode + "  AND BRANCH_CODE="+ globalvariable.PubBranchCode + "    AND YEAR_CODE="+ globalvariable.PubFYearCode +"   and DOC_ID = '"+ vtype + vno +"'");
                //string origidate = GetText("SELECT UDATE  FROM GATE1 WHERE COMP_CODE=@COMP_CODEAND BRANCH_CODE=@BRANCH_CODE  AND YEAR_CODE=@YEAR_CODE  and DOC_ID = @DOC_ID");
                
                cmd.Parameters.AddWithValue("@SEND_CODE", SendTo);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalvariable.PubFYearCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalvariable.PubBranchCode);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalvariable.PubCompCode);
                cmd.Parameters.AddWithValue("@MENU_CODE", menuCode ?? "");
                cmd.Parameters.AddWithValue("@DOC_ID", $"{vtype}{vno}");
                cmd.Parameters.AddWithValue("@DOC_NAME", vtype ?? "");
                cmd.Parameters.AddWithValue("@FORM_NAME", formName ?? "");
                cmd.Parameters.AddWithValue("@V_TYPE", vtype ?? "");
                cmd.Parameters.AddWithValue("@V_NO", vno);               
                        
                cmd.Parameters.AddWithValue("@V_DATE", SqlDbType.SmallDateTime).Value = vDate == null ? DBNull.Value : Convert.ToDateTime(vDate);
                //cmd.Parameters.AddWithValue("@ORIGIN_CODE", origicode);
                //cmd.Parameters.AddWithValue("@ORIGIN_DATE", SqlDbType.SmallDateTime).Value = origidate == null ? DBNull.Value : Convert.ToDateTime(origidate);
                cmd.Parameters.AddWithValue("@DEPARTMENT", deptName ?? "");
                cmd.Parameters.AddWithValue("@SEND_NAME", globalvariable.PubUserName ?? "");
                cmd.Parameters.AddWithValue("@USER_CODE", globalvariable.PubUserId);
                cmd.Parameters.AddWithValue("@USER_NAME", globalvariable.PubUserName ?? "");
                cmd.Parameters.AddWithValue("@APPROVAL_CODE", approvalCode);
                cmd.Parameters.AddWithValue("@APPROVAL_REMARK", appRemark ?? "");
                cmd.Parameters.AddWithValue("@REMARKS", appRemark ?? "");
                cmd.Parameters.AddWithValue("@UPDATE_SRNO", DBNull.Value);
                cmd.Parameters.AddWithValue("@TABLE_NAME", TableName);
                cmd.Parameters.AddWithValue("@TABLE_TYPE", "ENTRY");
                cmd.Parameters.AddWithValue("@WSID", globalvariable.PubWorkStationID ?? "");
                cmd.Parameters.AddWithValue("@LIP", globalvariable.PubLocalId ?? "");
                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                cmd.Parameters.AddWithValue("@APP_REMARKS", appRemark ?? "");
                cmd.Parameters.AddWithValue("@STATUS", STATUS ?? "");
                await cmd.ExecuteNonQueryAsync();
                return Json(new { Status = "Success", Message = "Approval Send Successfully" });
            }
            catch (Exception ex)
            {
                return Json(new  { Status = "Error", Message = ex.Message });
            }
        }


    }
}
