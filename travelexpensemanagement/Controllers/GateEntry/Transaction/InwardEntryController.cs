
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
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
        public IActionResult SavedData([FromBody] InwardEntryModel request)
        {
            if (request?.Header == null)
            {
                return Json(new { success = false, status = "Error", message = "Input model is null" });
            }

            var action = request.Header.action == "INSERT" ? "INSERT" : "UPDATE";

            var result = SubmitRequest(request.Header, request.Deatils, action);

            return Json(new { success = result.Status == "Success", status = result.Status, message = result.Message });
        }
        
        private ApiResponse SubmitRequest(InwardEntry_Header header, List<Details> details, string action)
        {
            try
            {
                string sql = "";
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();
                string Message = "";

                if (action == "INSERT")
                {
                    var jsonResult = GetVNo(header.V_TYPE) as JsonResult;
                    dynamic data = jsonResult.Value;
                    header.V_NO = Convert.ToInt32(data.V_NO);
                }

                sql = @"SELECT V_No FROM waybill1 WHERE V_No =@V_No  AND V_Type = 'TRIN'   AND Party_Code = @Party_Code  
                AND comp_Code = @comp_Code  AND Branch_Code = @Branch_Code;";
                using (var cmd1 = new SqlCommand(sql, conn))
                {
                    cmd1.Parameters.AddWithValue("@Party_Code", header.PARTY_CODE);
                    cmd1.Parameters.AddWithValue("@V_No", header.V_NO);
                    cmd1.Parameters.AddWithValue("@comp_Code", g.PubCompCode);
                    cmd1.Parameters.AddWithValue("@Branch_Code", g.PubBranchCode);

                    using var reader1 = cmd1.ExecuteReader();

                    var response = new ApiResponse();

                    if (reader1.Read())
                    {
                        var V_NO = reader1["V_NO"];

                        if (g.PubUserId != "1" && g.PubUserId != "53")
                        {
                            Message = $"Gate no. {V_NO} exist in MRN No.{header.V_NO}  Modification not allowed.";
                            return new ApiResponse { Status = "Error", Message = Message };
                        }
                    }
                }

                if (header.TRANSIT_NO != null)

                {
                    sql = @"SELECT V_No FROM waybill1 WHERE V_No = @V_No AND V_Type = 'TRIN' AND Party_Code = @PartyCode 
                          AND comp_Code = @CompCode AND Branch_Code = @BranchCode;";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@V_No", header.TRANSIT_NO);
                        cmd.Parameters.AddWithValue("@PartyCode", header.PARTY_CODE);
                        cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);

                        using var READERS = cmd.ExecuteReader();

                        var response = new ApiResponse();

                        if (READERS.Read())
                        {
                            var V_NO = READERS["V_NO"];
                            Message = $"Transit no. not valid for Party=> {header.PARTY_NAME}";
                            return new ApiResponse { Status = "Error", Message = Message };
                        }
                     }
                 }

                if (header.WAYBILL_NO != null)
                {
                    sql = @"SELECT Form_No  FROM waybill1  WHERE Form_No = @FormNo AND V_Type = 'TRIN'  AND
                    Party_Code = @PartyCode AND comp_Code = @CompCode
                    AND Branch_Code = @BranchCode;";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FormNo", header.WAYBILL_NO);
                        cmd.Parameters.AddWithValue("@PartyCode", header.PARTY_CODE);
                        cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);

                        using var READERS = cmd.ExecuteReader();
                        var response = new ApiResponse();
                        if (READERS.Read())
                        {
                            Message = $"Waybill no. not valid for Party=>{header.PARTY_NAME}, Please check in Transit Entry.";
                            return new ApiResponse { Status = "Error", Message = Message };
                        }
                    }
                }

                if (header.TRANSIT_NO != null)
                {
                    sql = @"SELECT TOP 1 CONCAT(V_type, V_no) AS V_NO  FROM Purchase1 WHERE Transit_No = @TransitNo  AND Comp_Code = @CompCode
                            AND Branch_Code = @BranchCode;";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TransitNo", header.TRANSIT_NO);
                        cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);

                        using var READERS = cmd.ExecuteReader();

                        var response = new ApiResponse();

                        if (READERS.Read())
                        {
                            var V_NO = READERS["V_NO"];
                            Message = $"Transit no. {header.TRANSIT_NO} exist in MRN No.= {V_NO}";
                            return new ApiResponse { Status = "Error", Message = Message };
                        }
                    }
                }

                if (header.WAYBILL_NO != null)
                {
                    sql = @"SELECT 
                    LTRIM(RTRIM(BILL_NO)) + '|' + FORMAT(BILL_DATE, 'your_date_format')  AS BillNoDate , BILL_NO  , BILL_DATE
                    FROM waybill1 WHERE FORM_NO = @FORM_NO  AND V_Type = 'TRIN'  AND comp_Code = @comp_Code
                    AND Branch_Code = @Branch_Code;";
                    var BillNoDate = "";
                    var BILL_DATE = "";
                    var BILL_NO = "";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FORM_NO", header.WAYBILL_NO);
                        cmd.Parameters.AddWithValue("@comp_Code", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@Branch_Code", g.PubBranchCode);

                        using var READERS = cmd.ExecuteReader();

                        var response = new ApiResponse();

                        if (READERS.Read())
                        {
                            BillNoDate = READERS["BillNoDate"].ToString();
                            BILL_DATE = READERS["BILL_DATE"].ToString();
                            BILL_NO = READERS["BILL_NO"].ToString();
                        }
                    }
                    if (BillNoDate == null)
                    {
                        if (header.BILL_NO != null)
                        {

                            if (BILL_NO != null && header.BILL_NO != BILL_NO)
                            {
                                Message = $"Bill No. {header.BILL_NO} not matched with Bill No. {BillNoDate} in Transit Entry No=>{header.TRANSIT_NO}";
                                return new ApiResponse { Status = "Error", Message = Message };
                            }


                            if (!string.IsNullOrEmpty(BILL_DATE) && DateTime.TryParse(BILL_DATE, out DateTime parsedBillDate) && header.BILL_DATE?.Date != parsedBillDate.Date)
                            {
                                Message = $"Bill Date. {header.BILL_DATE} not matched with Bill date {BillNoDate} in Transit Entry No=>{header.TRANSIT_NO}";
                                return new ApiResponse { Status = "Error", Message = Message };
                            }
                        }


                        if (header.CHALL_NO != null)
                        {
                            if (BILL_NO != null && header.CHALL_NO != BILL_NO)
                            {
                                Message = $"Challan No.  {header.CHALL_NO} not matched with Challan No. {BILL_NO} in Transit Entry No=>{header.TRANSIT_NO}";
                                return new ApiResponse { Status = "Error", Message = Message };
                            }

                            if (!string.IsNullOrEmpty(BILL_DATE) && DateTime.TryParse(BILL_DATE, out DateTime parsedBillDate) && header.CHALL_DATE?.Date != parsedBillDate.Date)
                            {
                                Message = $"Challan Date.  {header.CHALL_DATE}  not matched with Challan date {BILL_DATE} in Transit Entry No=>{header.TRANSIT_NO}";
                                return new ApiResponse { Status = "Error", Message = Message };
                            }

                        }

                    }
                }

                int CountryCode = 0;

                if (header.V_TYPE == "INRM")
                {
                    sql = @"SELECT COUNTRY_CODE  FROM SUBGROUP_MAST  WHERE CODE = @FORM_NO  
                    AND Comp_Code = @CompCode  AND ACTIVE = 1;";

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

                    var SUPPLIER_INVNOs = 0;

                    if (INV_NO != null)
                    {

                        sql = @"SELECT SUPPLIER_INVNO  FROM EXIM1 WHERE SUPPLIER_INVNO = @SUPPLIER_INVNO   AND 
                         SUPPLIER = @SUPPLIER AND COMP_CODE = @COMP_CODE;";

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
                                return new ApiResponse { Status = "Error", Message = Message };
                            }

                        }

                    }
                }

                string deleteSql = @"DELETE FROM GATE2  WHERE COMP_CODE = @CompCode  AND V_NO = @VNo      AND BRANCH_CODE = @BranchCode   
                             AND YEAR_CODE = @YearCode;";

                using (var deleteCmd = new SqlCommand(deleteSql, conn))
                {
                    deleteCmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                    deleteCmd.Parameters.AddWithValue("@VNo", header.V_NO);
                    deleteCmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                    deleteCmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);
                    deleteCmd.ExecuteNonQuery();
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
                    cmd.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                    cmd.Parameters.AddWithValue("@V_TIME", header.V_TIME);
                    cmd.Parameters.AddWithValue("@R_DATE", header.R_DATE);
                    cmd.Parameters.AddWithValue("@R_TIME", header.R_TIME);
                    cmd.Parameters.AddWithValue("@DISP_PLAN_NO", header.DISP_PLAN_NO);
                    cmd.Parameters.AddWithValue("@DISP_PLAN_TYPE", header.DISP_PLAN_TYPE);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", header.PARTY_CODE);
                    cmd.Parameters.AddWithValue("@PARTY_ADDRESSID", header.PARTY_ADDRESSID);
                    cmd.Parameters.AddWithValue("@BILL_NO", header.BILL_NO);
                    cmd.Parameters.AddWithValue("@BILL_DATE", header.BILL_DATE);
                    cmd.Parameters.AddWithValue("@BILL_AMT", header.BILL_AMT);
                    cmd.Parameters.AddWithValue("@CHALL_NO", header.CHALL_NO);
                    cmd.Parameters.AddWithValue("@CHALL_DATE", header.CHALL_DATE);
                    cmd.Parameters.AddWithValue("@TRUCK_NO", header.TRUCK_NO);
                    cmd.Parameters.AddWithValue("@TRANSPORT_CODE", header.TRANSPORT_CODE);
                    cmd.Parameters.AddWithValue("@DRIVER_NAME", header.DRIVER_NAME);
                    cmd.Parameters.AddWithValue("@DRIVER_NO", header.DRIVER_NO);
                    cmd.Parameters.AddWithValue("@EWB_DATE", header.EWB_DATE);
                    cmd.Parameters.AddWithValue("@EWB_EXPDATE", header.EWB_EXPDATE);
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
                    cmd.Parameters.AddWithValue("@SHIP_BILLDATE", header.SHIP_BILLDATE);
                    cmd.Parameters.AddWithValue("@RETURN_TYPE", header.RETURN_TYPE);
                    cmd.Parameters.AddWithValue("@GR_NO", header.GR_NO);
                    cmd.Parameters.AddWithValue("@OUT_TIME", header.OUT_TIME);
                    cmd.Parameters.AddWithValue("@GR_DATE", header.GR_DATE);
                    cmd.Parameters.AddWithValue("@RC_NO", header.RC_NO);
                    cmd.Parameters.AddWithValue("@DL_NO", header.DL_NO);
                    cmd.Parameters.AddWithValue("@INSU_NO", header.INSU_NO);
                    cmd.Parameters.AddWithValue("@PAN_NO", header.PAN_NO);
                    cmd.Parameters.AddWithValue("@STATUS", header.STATUS);
                    cmd.Parameters.AddWithValue("@INSU_EXPDT", header.INSU_EXPDT);
                    cmd.Parameters.AddWithValue("@DL_EXPDT", header.DL_EXPDT);
                    cmd.Parameters.AddWithValue("@FAPROV_STATUS", header.FAPROV_STATUS);
                    cmd.Parameters.AddWithValue("@CONTAINER_NO", header.CONTAINER_NO);
                    cmd.Parameters.AddWithValue("@FAPROV_REMARKS", "");
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
                                return new ApiResponse { Status = "Error", Message = Message };

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
                                return new ApiResponse { Status = "Error", Message = Message };

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
                                return new ApiResponse { Status = "Error", Message = Message };

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

                                    return new ApiResponse { Status = "Error", Message = Message };
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

                                    return new ApiResponse { Status = "Error", Message = Message };
                                }
                            }
                        }
                    }

                    using var cmd3 = new SqlCommand("sp_InwardEntry", conn) { CommandType = CommandType.StoredProcedure };
                    cmd3.Parameters.AddWithValue("@Action", action);
                    cmd3.Parameters.AddWithValue("@SaveAction", "Details");
                    cmd3.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd3.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd3.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd3.Parameters.AddWithValue("@V_TYPE", header.V_TYPE);
                    cmd3.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd3.Parameters.AddWithValue("@V_DATE", header.V_DATE);
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

                if (action == "UPDATE")
                {
                    _globalValidationdate.LogInsertUpdateDelete(destinationTable: "gate1", sourceTable: "gate1", transactionType: "Transaction",
                    codeVNo: header.V_NO.ToString(), vtype: header.V_TYPE);
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

                    string sql = @"SELECT client_id , rc_number , registration_date , owner_name , father_name ,present_address,permanent_address,mobile_number,maker_description,maker_model,
                    vehicle_category ,vehicle_chasi_number , vehicle_engine_number ,body_type,fuel_type,color ,norms_type ,fit_up_to ,financer ,
                    financed ,insurance_company ,insurance_policy_number , insurance_upto ,manufacturing_date ,manufacturing_date_formatted ,registered_at 
                    ,less_info ,tax_upto ,tax_paid_upto , cubic_capacity ,vehicle_gross_weight,no_cylinders,seat_capacity,sleeper_capacity,
                    standing_capacity,wheelbase,unladen_weight ,vehicle_category,
                    vehicle_category_description,pucc_number,pucc_upto,permit_number,permit_issue_date,permit_valid_from,permit_valid_upto,
                    permit_type,national_permit_number,national_permit_upto ,national_permit_issued_by ,non_use_status,non_use_from, non_use_to,
                    blacklist_status,noc_details,owner_number,rc_status,masked_name,challan_details  FROM GATE_VAHAN
                    WHERE COMP_CODE = @CompCode     AND YEAR_CODE = @YearCode    AND V_NO = @VNo     AND V_TYPE = @VType";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@YearCode", global.PubFYearCode);
                        cmd.Parameters.AddWithValue("@VNo", v_no);
                        cmd.Parameters.AddWithValue("@VType", v_type);

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
                return new JsonResult(new { success = true, message = "Data saved successfully",  data = res });
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

                    string sql = @"SELECT ClientId, RcNumber, BankName, TagId, Status,
                           LaneDirection, TransactionDateTime, SeqNo,
                           TollPlazaGeoCode, TollPlazaName, VehicleType
                           FROM GATE_FASTAG
                           WHERE COMP_CODE = @CompCode
                           AND YEAR_CODE = @YearCode
                           AND V_NO = @VNo
                           AND V_TYPE = @VType";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@YearCode", global.PubFYearCode);
                        cmd.Parameters.AddWithValue("@VNo", v_no);
                        cmd.Parameters.AddWithValue("@VType", v_type);

                        using (var reader = cmd.ExecuteReader())
                        {
                            var list = new List<object>();

                            while (reader.Read()) // 👈 loop for multiple rows
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

            return Json(new
            {
                StatusCode = res.status,
                message = res.message,
                supplier = res.data
            });
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
                var parameters = new Dictionary<string, object> { { "@Type", "v_type" } };
                var data = _dropdownService.GetMultipleDropdownList("sp_GetDropdownData", CommandType.StoredProcedure, parameters);
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
    }
}
