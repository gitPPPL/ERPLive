
using Dapper;
using DocumentFormat.OpenXml.Office.Word;
using iText.StyledXmlParser.Jsoup.Select;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Org.BouncyCastle.Bcpg.OpenPgp;
using StackExchange.Redis;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
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
     

            var validation = Validation(request.Header, request.Deatils);

            if (validation.Status == "VALIDATION" || validation.Status == "Error")
            {         
                return Json(new { success = validation.Status == "VALIDATION", status = validation.Status, message = validation.Message });
            }

            var result = await SubmitRequest( request.Header, request.Deatils, action);

            return Json(new  { success = result.Status == "Success", status = result.Status,  message = result.Message });
        }

        private async Task<ApiResponse> SubmitRequest(InwardEntry_Header header, List<Details> details, string action)
        { 
            try
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

                APPROV_USER = GetText("select APPROV_USER from DOC_APPROSTAGE where USER_CODE = " + g.PubUserId + " and DOC_CODE = '" + header.V_TYPE + "' and comp_code = " + g.PubCompCode + "");

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

                else if (header.WAYBILL_NO  !=  null)
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
                    cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE == null ? DBNull.Value : Convert.ToDateTime(header.V_DATE);
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


                if(details.Count > 0)
                {
                    string approval = GetText("SELECT 1  FROM approval_status WHERE user_Code = " + g.PubUserId + " AND " +
                    "V_Type = '" + header.V_TYPE + "' AND V_No = " + header.V_NO + "   AND  " +
                    "  COMP_CODE = " + g.PubCompCode + "  AND Branch_Code = " + g.PubBranchCode + "  AND Year_Code = " + g.PubFYearCode + ";");

                    if (isFinalApprovalBody == true && approval != "")
                    {
                        string UpdateSql = @"UPDATE approval_status
                        SET
                        STATUS = 'CLOSE',
                        CLOSE_DATE = GETDATE(),
                        Approval_code = 8,
                        Approval_remark = 'Approved',
                        remarks = 'Document Approved'
                        WHERE
                        V_Type = @V_Type
                        AND V_No = @V_No
                        AND COMP_CODE = @COMP_CODE
                        AND Branch_Code = @Branch_Code
                        AND Year_Code = @Year_Code;";

                        using (var updateCmd = new SqlCommand(UpdateSql, conn))
                        {                           
                            updateCmd.Parameters.AddWithValue("@V_No", header.V_NO);
                            updateCmd.Parameters.AddWithValue("@V_Type", header.V_TYPE);
                            updateCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                            updateCmd.Parameters.AddWithValue("@Branch_Code", g.PubBranchCode);
                            updateCmd.Parameters.AddWithValue("@Year_Code", g.PubFYearCode);
                            updateCmd.ExecuteNonQuery();

                        }
                    }
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
                var g = _globalVariableService.GetGlobalVariables();               
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                string Message = "";

                // ================= 1. GATE NO MODIFICATION CHECK =================
                using (var cmd = new SqlCommand("sp_InwardEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Party_Code", (object?)header.PARTY_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@V_No", (object?)header.V_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@comp_Code", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@Branch_Code", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@Action", "GatenoModifica");

                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        var vno = reader["V_No"].ToString();

                        if (g.PubUserId != "1" && g.PubUserId != "53")
                        {
                            return new ApiResponse  { Status = "VALIDATION",  Message = $"Gate no. {vno} exist in MRN No. {header.V_NO} Modification not allowed."
                            };
                        }
                    }
                }

                // ================= 2. TRANSIT NO VALIDATION =================
                if (header.TRANSIT_NO.HasValue)
                {
                    using var cmd = new SqlCommand("sp_InwardEntry", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@V_NO", (object?)header.TRANSIT_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Party_Code", (object?)header.PARTY_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@comp_Code", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@Branch_Code", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@Action", "TRANSIT_NO");
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        if(g.PubUserId != "1")
                        {                     
                            return new ApiResponse { Status = "VALIDATION", Message = $"Transit no. not valid for Party=> {header.PARTY_NAME}" };
                        }
                    }
                }

                // ================= 3. WAYBILL VALIDATION =================
                if (!string.IsNullOrEmpty(header.WAYBILL_NO))
                {
                    using var cmd = new SqlCommand("sp_InwardEntry", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@WAYBILL_NO", SqlDbType.BigInt) .Value = string.IsNullOrEmpty(header.WAYBILL_NO)
                    ? DBNull.Value : Convert.ToInt64(header.WAYBILL_NO);
                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@Action", "WAYBILL_NO");
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        return new ApiResponse
                        {
                            Status = "VALIDATION",
                            Message = $"Waybill no. not valid for Party=>{header.PARTY_NAME}, Please check in Transit Entry."
                        };
                    }
                }


                // ================= 4. TRANSIT DUPLICATE CHECK =================
                if (header.TRANSIT_NO.HasValue)
                {
                    using var cmd = new SqlCommand("sp_InwardEntry", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@TRANSIT_NO", (object?)header.TRANSIT_NO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@Action", "Trnsitnowaybillno");

                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    { 
                        if(g.PubUserId != "1")
                        {
                            var vno = reader["V_NO"];
                            return new ApiResponse { Status = "VALIDATION", Message = $"Transit no. {header.TRANSIT_NO} exist in MRN No.= {vno}" };

                        }
                    }
                }

                // ================= 5. BILL + CHALLAN VALIDATION =================
                if (!string.IsNullOrEmpty(header.WAYBILL_NO))
                {
                    string billNoDate = GetText(
                        "SELECT LTRIM(RTRIM(BILL_NO)) + '|' + FORMAT(BILL_DATE, 'dd/MM/yyyy') " +
                        "FROM waybill1 WHERE FORM_NO='" + header.WAYBILL_NO +
                        "' AND V_Type='TRIN' AND comp_Code=" + g.PubCompCode +
                        " AND Branch_Code=" + g.PubBranchCode
                    );

                    if (!string.IsNullOrEmpty(billNoDate) && billNoDate.Contains("|"))
                    {
                        var parts = billNoDate.Split('|');

                        string dbBillNo = parts[0].Trim();
                        DateTime.TryParse(parts[1], out DateTime dbBillDate);

                        // BILL CHECK
                        if (!string.IsNullOrEmpty(header.BILL_NO))
                        {
                            if (header.BILL_NO.Trim() != dbBillNo)
                            {
                                return new ApiResponse
                                {
                                    Status = "VALIDATION",
                                    Message = $"Bill No. {header.BILL_NO} not matched with Bill No. {dbBillNo} in Transit Entry No => {header.TRANSIT_NO}"
                                };
                            }

                            if (dbBillDate != DateTime.MinValue && header.BILL_DATE?.Date != dbBillDate.Date)
                            {
                                return new ApiResponse
                                {
                                    Status = "VALIDATION",
                                    Message = $"Bill Date {header.BILL_DATE:dd/MM/yyyy} not matched with Bill date {dbBillDate:dd/MM/yyyy} in Transit Entry No => {header.TRANSIT_NO}"
                                };
                            }
                        }

                        // CHALLAN CHECK
                        if (!string.IsNullOrEmpty(header.CHALL_NO))
                        {
                            if (header.CHALL_NO.Trim() != dbBillNo)
                            {
                                return new ApiResponse
                                {
                                    Status = "VALIDATION",
                                    Message = $"Challan No. {header.CHALL_NO} not matched with Challan No. {dbBillNo} in Transit Entry No => {header.TRANSIT_NO}"
                                };
                            }

                            if (dbBillDate != DateTime.MinValue &&
                                header.CHALL_DATE?.Date != dbBillDate.Date)
                            {
                                return new ApiResponse
                                {
                                    Status = "VALIDATION",
                                    Message = $"Challan Date {header.CHALL_DATE:dd/MM/yyyy} not matched with {dbBillDate:dd/MM/yyyy} in Transit Entry No => {header.TRANSIT_NO}"
                                };
                            }
                        }
                    }
                }


                //foreach (var d in details)
                //{
                //    if (string.IsNullOrWhiteSpace(d.ITEM_NAME))
                //        continue;

                //    #region Party Validation

                //    if (header.V_TYPE == "INFU" || header.V_TYPE == "INST" ||  header.V_TYPE == "INRM")
                //    {
                //        string sql = @"
                //            SELECT 1
                //            FROM ORDER1
                //            WHERE PARTY_CODE = @PartyCode
                //            AND V_TYPE = @RefType
                //            AND V_NO = @RefNo
                //            AND COMP_CODE = @CompCode
                //            AND BRANCH_CODE = @BranchCode";

                //        using var cmd = new SqlCommand(sql, conn);
                //        cmd.Parameters.AddWithValue("@PartyCode", header.PARTY_CODE);
                //        cmd.Parameters.AddWithValue("@RefType", d.REF_TYPE);
                //        cmd.Parameters.AddWithValue("@RefNo", d.REF_NO);
                //        cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                //        cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);

                //        var exists = cmd.ExecuteScalar();

                //        if (exists == null)
                //        {
                //            return new ApiResponse
                //            {
                //                Status = "VALIDATION",
                //                Message = "Party Name not matched with Order Party Name."
                //            };
                //        }
                //    }
                //    else if (header.V_TYPE == "INRT")
                //    {
                //        string sql = @"
                //            SELECT 1
                //            FROM GATE1
                //            WHERE PARTY_CODE = @PartyCode
                //            AND V_TYPE = @RefType
                //            AND V_NO = @RefNo
                //            AND COMP_CODE = @CompCode
                //            AND BRANCH_CODE = @BranchCode";

                //        using var cmd = new SqlCommand(sql, conn);
                //        cmd.Parameters.AddWithValue("@PartyCode", header.PARTY_CODE);
                //        cmd.Parameters.AddWithValue("@RefType", d.REF_TYPE);
                //        cmd.Parameters.AddWithValue("@RefNo", d.REF_NO);
                //        cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                //        cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);

                //        var exists = cmd.ExecuteScalar();

                //        if (exists == null)
                //        {
                //            return new ApiResponse
                //            {
                //                Status = "",
                //                Message = "Party Name not matched with Gate Out Entry Party Name."
                //            };
                //        }
                //    }
                //    else if (header.V_TYPE == "INSR")
                //    {
                //        string sql = @"
                //            SELECT 1
                //            FROM SALE1
                //            WHERE PARTY_CODE = @PartyCode
                //            AND V_TYPE = @RefType
                //            AND V_NO = @RefNo
                //            AND COMP_CODE = @CompCode
                //            AND BRANCH_CODE = @BranchCode";

                //        using var cmd = new SqlCommand(sql, conn);
                //        cmd.Parameters.AddWithValue("@PartyCode", header.PARTY_CODE);
                //        cmd.Parameters.AddWithValue("@RefType", d.REF_TYPE);
                //        cmd.Parameters.AddWithValue("@RefNo", d.REF_NO);
                //        cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                //        cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);

                //        var exists = cmd.ExecuteScalar();

                //        if (exists == null)
                //        {
                //            return new ApiResponse
                //            {
                //                Status = "",
                //                Message = "Party Name not matched with Sale Return Entry Party Name."
                //            };
                //        }
                //    }

                //    #endregion

                //    #region Mandatory Reference Validation

                //    if (header.V_TYPE != "INJB" && header.V_TYPE != "INMS")
                //    {
                //        if (!string.IsNullOrWhiteSpace(d.ITEM_NAME) &&
                //            (d.REF_NO == null || d.REF_NO == 0))
                //        {
                //            return new ApiResponse
                //            {
                //                Status = "VALIDATION",
                //                Message = "Reference Type and No is compulsory."
                //            };
                //        }
                //    }

                //    #endregion

                //    #region Quantity Validation

                //    if (header.V_TYPE == "INFU" ||  header.V_TYPE == "INST" || header.V_TYPE == "INRM")
                //    {
                //        if (!string.IsNullOrWhiteSpace(d.REF_TYPE) &&
                //            d.REF_NO > 0)
                //        {
                //            if (d.REF_TYPE == "PAUD")
                //            {
                //                decimal saudaQty;
                //                decimal gateQty;

                //                string sql1 = @"
                //                    SELECT ISNULL(SUM(QTY),0)
                //                    FROM SAUDA
                //                    WHERE V_TYPE = @RefType
                //                    AND V_NO = @RefNo
                //                    AND COMP_CODE = @CompCode
                //                    AND BRANCH_CODE = @BranchCode";

                //                using (var cmd = new SqlCommand(sql1, conn))
                //                {
                //                    cmd.Parameters.AddWithValue("@RefType", d.REF_TYPE);
                //                    cmd.Parameters.AddWithValue("@RefNo", d.REF_NO);
                //                    cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                //                    cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);

                //                    saudaQty = Convert.ToDecimal(cmd.ExecuteScalar());
                //                }

                //                string sql2 = @"
                //                    SELECT ISNULL(SUM(QTY),0)
                //                    FROM GATE2
                //                    WHERE REF_TYPE = @RefType
                //                    AND REF_NO = @RefNo
                //                    AND COMP_CODE = @CompCode
                //                    AND BRANCH_CODE = @BranchCode
                //                    AND V_TYPE = @VType
                //                    AND V_NO <> @CurrentVNo";

                //                using (var cmd = new SqlCommand(sql2, conn))
                //                {
                //                    cmd.Parameters.AddWithValue("@RefType", d.REF_TYPE);
                //                    cmd.Parameters.AddWithValue("@RefNo", d.REF_NO);
                //                    cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                //                    cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                //                    cmd.Parameters.AddWithValue("@VType", header.V_TYPE);
                //                    cmd.Parameters.AddWithValue("@CurrentVNo", header.V_NO);

                //                    gateQty = Convert.ToDecimal(cmd.ExecuteScalar());
                //                }

                //                gateQty += (decimal)d.QTY;

                //                if (gateQty > saudaQty + pubBPPurchTolQty)
                //                {
                //                    return new ApiResponse
                //                    {
                //                        Status = "VALIDATION",
                //                        Message =
                //                            $"Pending Sauda Quantity is {saudaQty - gateQty + d.QTY + pubBPPurchTolQty} " +
                //                            $"and Gate Quantity is {d.QTY}"
                //                    };
                //                }
                //            }
                //            else
                //            {
                //                decimal orderQty;
                //                decimal gateQty;

                //                string sql1 = @"
                //                    SELECT ISNULL(SUM(QTY),0)
                //                    FROM ORDER2
                //                    WHERE V_TYPE = @RefType
                //                    AND V_NO = @RefNo
                //                    AND ITEM_CODE = @ItemCode
                //                    AND COMP_CODE = @CompCode
                //                    AND BRANCH_CODE = @BranchCode";

                //                using (var cmd = new SqlCommand(sql1, conn))
                //                {
                //                    cmd.Parameters.AddWithValue("@RefType", d.REF_TYPE);
                //                    cmd.Parameters.AddWithValue("@RefNo", d.REF_NO);
                //                    cmd.Parameters.AddWithValue("@ItemCode", d.ITEM_CODE);
                //                    cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                //                    cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);

                //                    orderQty = Convert.ToDecimal(cmd.ExecuteScalar());
                //                }

                //                string sql2 = @"
                //                    SELECT ISNULL(SUM(QTY),0)
                //                    FROM GATE2
                //                    WHERE REF_TYPE = @RefType
                //                    AND REF_NO = @RefNo
                //                    AND ITEM_CODE = @ItemCode
                //                    AND COMP_CODE = @CompCode
                //                    AND BRANCH_CODE = @BranchCode
                //                    AND V_TYPE <> @VType
                //                    AND V_NO <> @CurrentVNo";

                //                using (var cmd = new SqlCommand(sql2, conn))
                //                {
                //                    cmd.Parameters.AddWithValue("@RefType", d.REF_TYPE);
                //                    cmd.Parameters.AddWithValue("@RefNo", d.REF_NO);
                //                    cmd.Parameters.AddWithValue("@ItemCode", d.ITEM_CODE);
                //                    cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                //                    cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                //                    cmd.Parameters.AddWithValue("@VType", header.V_TYPE);
                //                    cmd.Parameters.AddWithValue("@CurrentVNo", header.V_NO);

                //                    gateQty = Convert.ToDecimal(cmd.ExecuteScalar());
                //                }

                //                gateQty += (decimal)d.QTY;

                //                if (gateQty > orderQty)
                //                {
                //                    return new ApiResponse
                //                    {
                //                        Status = "VALIDATION",
                //                        Message =
                //                            $"Pending Order Quantity is {orderQty - gateQty + d.QTY} " +
                //                            $"and Gate Quantity is {d.QTY}. (Item Name : '{d.ITEM_NAME}')"
                //                    };
                //                }
                //            }
                //        }
                //    }

                //    #endregion
                //}


                // ================= 6.DETAIL VALIDATIONS =================
                foreach (var d in details)
                {
                    if (string.IsNullOrWhiteSpace(d.ITEM_NAME))
                        continue;

                    // INRM COUNTRY CHECK
                    if (header.V_TYPE == "INRM")
                    {
                        string sql = @"SELECT COUNTRY_CODE FROM SUBGROUP_MAST 
                               WHERE CODE = @CODE AND Comp_Code = @Comp AND ACTIVE = 1";

                        using var cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.Add("@CODE", SqlDbType.BigInt)
                        .Value = string.IsNullOrEmpty(header.WAYBILL_NO)
                        ? DBNull.Value
                        : Convert.ToInt64(header.WAYBILL_NO);
                        cmd.Parameters.AddWithValue("@Comp", g.PubCompCode);

                        var country = cmd.ExecuteScalar();
                        int countryCode = country != null ? Convert.ToInt32(country) : 0;

                        if (countryCode != 1)
                        {
                            string sql2 = @"SELECT INV_NO FROM ORDER4 
                                    WHERE INV_NO=@INV AND PARTY_CODE=@PARTY AND COMP_CODE=@COMP";

                            using var cmd2 = new SqlCommand(sql2, conn);
                            cmd2.Parameters.AddWithValue("@INV", (object?)header.BILL_NO ?? DBNull.Value);
                            cmd2.Parameters.AddWithValue("@PARTY", (object?)header.PARTY_CODE ?? DBNull.Value);
                            cmd2.Parameters.AddWithValue("@COMP", g.PubCompCode);

                            var inv = cmd2.ExecuteScalar();

                            if (inv != null)
                            {
                                return new ApiResponse
                                {
                                    Status = "VALIDATION",
                                    Message = $"Bill No. {header.BILL_NO} already exists in order system."
                                };
                            }
                        }
                    }

                    // ITEM BASED CHECK (example simplified)
                    if (header.V_TYPE == "INSR" || header.V_TYPE == "INRT" || header.V_TYPE == "INFU" || header.V_TYPE == "INST")
                    {
                        string table = header.V_TYPE switch
                        {
                            "INSR" => "SALE1",
                            "INRT" => "GATE1",
                            _ => "ORDER1"
                        };

                        string sql = $"SELECT Party_Code FROM {table} WHERE Party_Code=@Party AND V_NO=@VNO";

                        using var cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@Party", (object?)header.PARTY_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@VNO", d.REF_NO);

                        using var reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            return new ApiResponse
                            {
                                Status = "",
                                Message = $"Party mismatch in {table}."
                            };
                        }
                    }
                }

                // ================= FINAL SUCCESS RETURN =================
                return new ApiResponse
                {
                    Status = "Success",
                    Message = "Validation Passed"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = "Error",
                    Message = ex.Message
                };
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
        public async Task<JsonResult> DDlTransitNo(string v_type, int v_no, int partycode, DateTime ExpiryDate , string mode = "")
        {
            var res = await _inwardEntryRepository.DDlTransitNoAsync(v_type, v_no, partycode, ExpiryDate , mode);
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

                string message = "NoAction";

                using SqlCommand cmd = new SqlCommand("sp_InwardEntry", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "ApprovalbtnShow");
                cmd.Parameters.AddWithValue("@UUSER", globalvariable.PubUserId);
                cmd.Parameters.AddWithValue("@V_TYPE", v_type);
                cmd.Parameters.AddWithValue("@V_NO", v_no);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalvariable.PubCompCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalvariable.PubFYearCode);
             

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    message = reader["StatusMessage"]?.ToString() ?? "NoAction";
                }

                return new JsonResult(new
                {
                    success = true,
                    message
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message
                });
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
        public JsonResult DDlSendTo(string v_type )
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT  a.USER_CODE, b.FULL_NAME FROM DOC_APPROSTAGE a LEFT JOIN CONDATABASE.dbo.USER_MAST b " +
                "   ON a.USER_CODE = b.CODE LEFT JOIN CONDATABASE.dbo.SUBUSER_MAST c     ON b.CODE = c.USER_CODE  " +
                " AND c.COMP_CODE = "+  getdata.PubCompCode +"  WHERE b.Active = 1  AND a.User_Code <> "+  getdata.PubUserId +" " +
                " AND a.DOC_CODE = '" +  v_type + "'    AND a.comp_code = "+  getdata.PubCompCode +"  " +
                " GROUP BY      a.USER_CODE, b.FULL_NAME,  a.SRNO  ORDER BY a.SRNO;";
                var DDlSendTo = _dropdownService.GetDropdownList(query);
                return Json(DDlSendTo);
            }
        }
        public JsonResult DDlForwordTo(string v_type, int v_no)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                    string query = @"
                    SELECT a.USER_CODE AS Value, b.FULL_NAME AS Text
                    FROM DOC_APPROSTAGE a
                    LEFT JOIN CONDATABASE.dbo.USER_MAST b
                    ON a.USER_CODE = b.CODE
                    LEFT JOIN CONDATABASE.dbo.SUBUSER_MAST c
                    ON b.CODE = c.USER_CODE
                    AND c.COMP_CODE = @CompCode
                    WHERE a.USER_CODE <> @UserCode
                    AND a.DOC_CODE = @VType
                    AND a.COMP_CODE = @CompCode and  b.FULL_NAME <> ''

                    UNION ALL

                    SELECT SEND_CODE AS Value, SEND_NAME AS Text
                    FROM APPROVAL_STATUS
                    WHERE COMP_CODE = @CompCode
                    AND BRANCH_CODE = @BranchCode
                    AND YEAR_CODE = @FYearCode
                    AND V_NO = @VNo
                    AND V_TYPE = @VType  and SEND_NAME <> ''  ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@UserCode", getdata.PubUserId); 
                    cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                    cmd.Parameters.AddWithValue("@FYearCode", getdata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@VNo", v_no);
                    cmd.Parameters.AddWithValue("@VType", v_type);

                    con.Open();

                    var list = new List<object>();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new
                            {
                                value = dr["Value"].ToString(),
                                text = dr["Text"].ToString()
                            });
                        }
                    }
                    return Json(list);
                }
            }
        }
         public JsonResult DDlAPPStatus()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code,name from DOCSTATUS_MAST where V_TYPE='Approval' order by code";
                var DDlAPPStatus = _dropdownService.GetDropdownList(query);
                return Json(DDlAPPStatus);
            }
        }
        public JsonResult DDlAPPRemark()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code,name from DOCSTATUS_MAST where V_TYPE='Approval' and code<>7 and code<>8 order by code";
                var DDlAPPRemark = _dropdownService.GetDropdownList(query);
                return Json(DDlAPPRemark);
            }
        }
        [HttpPost]
        public async Task<JsonResult> SendApproval( string vtype, int vno,  DateTime vDate, string appStatus,  string appRemark, int SendTo,
            string menuCode, string formName, string deptName,  string STATUS,  string TableName, string sendName, string tabletype = "ENTRY")
        {
            try
            {
                var globalvariable = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();
                using var cmd = new SqlCommand("USP_SEND_APPROVAL", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                int approvalCode = 0;

                string origicode = GetText(
                    $"SELECT UUSER FROM GATE1 " +
                    $"WHERE COMP_CODE={globalvariable.PubCompCode} " +
                    $"AND BRANCH_CODE={globalvariable.PubBranchCode} " +
                    $"AND YEAR_CODE={globalvariable.PubFYearCode} " +
                    $"AND DOC_ID='{vtype}{vno}'");

                string origidate = GetText(
                    $"SELECT UDATE FROM GATE1 " +
                    $"WHERE COMP_CODE={globalvariable.PubCompCode} " +
                    $"AND BRANCH_CODE={globalvariable.PubBranchCode} " +
                    $"AND YEAR_CODE={globalvariable.PubFYearCode} " +
                    $"AND DOC_ID='{vtype}{vno}'");
                
                cmd.Parameters.AddWithValue("@SEND_CODE", globalvariable.PubUserId ?? "");
                cmd.Parameters.AddWithValue("@SEND_NAME", globalvariable.PubUserName ?? "");
                cmd.Parameters.AddWithValue("@USER_CODE", SendTo);
                cmd.Parameters.AddWithValue("@USER_NAME", sendName);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalvariable.PubFYearCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalvariable.PubBranchCode);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalvariable.PubCompCode);
                cmd.Parameters.AddWithValue("@MENU_CODE", menuCode ?? "");
                cmd.Parameters.AddWithValue("@DOC_ID", $"{vtype}{vno}");
                cmd.Parameters.AddWithValue("@DOC_NAME", vtype ?? "");
                cmd.Parameters.AddWithValue("@FORM_NAME", formName ?? "");
                cmd.Parameters.AddWithValue("@V_TYPE", vtype ?? "");
                cmd.Parameters.AddWithValue("@V_NO", vno);
                cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value =  vDate == DateTime.MinValue ? DBNull.Value : Convert.ToDateTime(vDate);
                cmd.Parameters.Add("@ORIGIN_DATE", SqlDbType.SmallDateTime).Value =  string.IsNullOrWhiteSpace(origidate) ? DBNull.Value : Convert.ToDateTime(origidate);
                cmd.Parameters.AddWithValue("@ORIGIN_CODE", string.IsNullOrWhiteSpace(origicode)  ? DBNull.Value : Convert.ToInt32(origicode));
                cmd.Parameters.AddWithValue("@DEPARTMENT", deptName ?? "");
                cmd.Parameters.AddWithValue("@APPROVAL_CODE", approvalCode);
                cmd.Parameters.AddWithValue("@APPROVAL_REMARK", appRemark ?? "");
                cmd.Parameters.AddWithValue("@REMARKS", appRemark ?? "");
                cmd.Parameters.AddWithValue("@TABLE_NAME", TableName ?? "");
                cmd.Parameters.AddWithValue("@TABLE_TYPE", tabletype ?? "ENTRY");
                cmd.Parameters.AddWithValue("@WSID", globalvariable.PubWorkStationID ?? "");
                cmd.Parameters.AddWithValue("@LIP", globalvariable.PubLocalId ?? "");
                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                cmd.Parameters.AddWithValue("@APP_REMARKS", appRemark ?? "");
                cmd.Parameters.AddWithValue("@STATUS", STATUS ?? "");
                // OUTPUT PARAMETER
                SqlParameter returnMessage = new SqlParameter( "@ReturnMessage",  SqlDbType.NVarChar, 500)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(returnMessage);
                await cmd.ExecuteNonQueryAsync();
                string message = Convert.ToString(returnMessage.Value);
                return Json(new { Status = "Success", Message = string.IsNullOrWhiteSpace(message) ? "Approval Sent Successfully." : message });
            }
            catch (SqlException ex)
            {
                return Json(new {  Status = "Error", Message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new  { Status = "Error", Message = ex.Message });
            }
        }

        public async Task<JsonResult> GetTransitData(int VoucherNo)
        {
            var data = await _inwardEntryRepository.GetGetTransitDataCode(VoucherNo);
            return Json(data);
        }

        public JsonResult GetTransitNoLeaveEwayBill(int partyCode, long waybillNo)
        {
            var Globaldata = _globalVariableService.GetGlobalVariables();

            string waybill = GetText(
                $"SELECT TOP 1 Form_No " +
                $"FROM waybill1 " +
                $"WHERE Form_No = '{waybillNo}' " +
                $"AND PARTY_CODE = {partyCode} " +
                $"AND V_Type = 'TRIN' " +
                $"AND Status = 1 " +
                $"AND comp_Code = {Globaldata.PubCompCode} " +
                $"AND Branch_Code = {Globaldata.PubBranchCode} " +
                $"AND Year_Code = {Globaldata.PubFYearCode}");

            string vNo = string.Empty;

            if (!string.IsNullOrEmpty(waybill))
            {
                vNo = GetText(
                    $"SELECT V_NO " +
                    $"FROM waybill1 " +
                    $"WHERE Form_No = '{waybillNo}' " +
                    $"AND V_Type = 'TRIN' " +
                    $"AND comp_Code = {Globaldata.PubCompCode} " +
                    $"AND Branch_Code = {Globaldata.PubBranchCode} " +
                    $"AND Year_Code = {Globaldata.PubFYearCode}");
            }          

            if(vNo == "")
            {
                return Json(new { Success = false});

            }
            return Json(new {  Success = true , V_NO = vNo });
        }

        public JsonResult GetPasrtyBillNo(int partyCode, string PartyBillNo)
        {
            var globalData = _globalVariableService.GetGlobalVariables();

            string refSaudaNo = GetText(
                "SELECT CONCAT(SAUDA_TYPE, SAUDA_NO) AS RefSaudaNo " +
                "FROM ORDER4 " +
                "WHERE INV_NO = '" + PartyBillNo + " '" +
                " AND PARTY_CODE = " + partyCode +
                " AND COMP_CODE = " + globalData.PubCompCode);

            if (!string.IsNullOrEmpty(refSaudaNo))
            {
                return Json(new {  success = false,   refSaudaNo = refSaudaNo });
            }

            return Json(new
            {
                success = true,
                refSaudaNo = string.Empty
            });
        }
        public JsonResult Getvehicleno( string TruckNo)
        {
            using var conn = _dbConnection.GetErpConnection();
            var globalData = _globalVariableService.GetGlobalVariables();

            string sql = @"
                SELECT TOP 1
                RC_NO,
                INSU_NO
                FROM GATE1
                WHERE LTRIM(RTRIM(TRUCK_NO)) = @VehicleNo
                AND (
                ISNULL(RC_NO, '') <> ''
                OR ISNULL(INSU_NO, '') <> ''
                )
                AND COMP_CODE = @CompCode
                ORDER BY V_DATE DESC;";

            var result = conn.QueryFirstOrDefault(sql, new
            {
                @VehicleNo = TruckNo,
                @CompCode = globalData.PubCompCode
            });

            string rcNo = result?.RC_NO ?? string.Empty;
            string insuNo = result?.INSU_NO ?? string.Empty;

            return Json(new { success = true, rcNo = rcNo, insuNo = insuNo });
        }

        public JsonResult GetMobilenodata(string mobileno)
        {
            using var conn = _dbConnection.GetErpConnection();
            var globalData = _globalVariableService.GetGlobalVariables();

            string sql = @"
                    SELECT TOP 1
                    DL_NO,
                    PAN_NO
                    FROM GATE1
                    WHERE LTRIM(RTRIM(DRIVER_NO)) = 'MobileNumberHere'
                    AND (
                    ISNULL(DL_NO, '') <> ''
                    OR ISNULL(PAN_NO, '') <> ''
                    )
                    AND COMP_CODE = CompCodeValue
                    ORDER BY V;";

            var result = conn.QueryFirstOrDefault(sql, new
            {
                @VehicleNo = mobileno,
                @CompCode = globalData.PubCompCode
            });

            string DL_NO = result?.DL_NO ?? string.Empty;
            string PAN_NO = result?.PAN_NO ?? string.Empty;

            return Json(new { success = true, DL_NO = DL_NO, PAN_NO = PAN_NO });
        }

        public JsonResult DDlpono()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "   SELECT DISTINCT  a.V_NO , a.V_TYPE " +
                "  FROM Order1 a LEFT JOIN Order2 b    " +
                " ON a.V_TYPE = b.V_TYPE AND a.V_NO = b.V_NO  AND a.COMP_CODE = b.COMP_CODE AND " +
                "a.BRANCH_CODE = b.BRANCH_CODE  AND a.YEAR_CODE = b.YEAR_CODE   " +
                " LEFT JOIN Subgroup_Mast c ON a.PARTY_CODE = c.CODE AND a.COMP_CODE = c.COMP_CODE  " +
                " WHERE a.V_TYPE IN ('RORD', 'PORD', 'JORD')    AND a.STATUS = 1  " +
                "  AND a.COMP_CODE = "+ getdata.PubCompCode + "  AND a.YEAR_CODE = "+ getdata.PubFYearCode + " " +
                "  AND a.BRANCH_CODE = "+ getdata.PubBranchCode + ";";

                var DDlpono = _dropdownService.GetDropdownList(query);
                return Json(DDlpono);
            }
        }

    }
}
