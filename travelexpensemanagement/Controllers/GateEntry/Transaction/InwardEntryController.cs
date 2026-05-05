
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Interfaces;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
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
                newV_NO = _inwardEntryRepository.GetVNoAsync(Vtype,Tablename).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO }); // ✅ FIXED
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
        public async Task<JsonResult> GetVehcleinfo([FromQuery] string rc_number, string VType, int VNo)
        {
            try
            {
                using var client = new HttpClient();

                string url = "https://kyc-api.surepass.io/api/v1/rc/rc-full";
                string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJmcmVzaCI6ZmFsc2UsImlhdCI6MTc1MTg3ODU4MiwianRpIjoiYzczZmFkMTAtZjk0MC00NzdkLThlNDgtMjU3ZTViMzVkYjY4IiwidHlwZSI6ImFjY2VzcyIsImlkZW50aXR5IjoiZGV2LnBhc2h1cGF0aWdycF9jb25zb2xlQHN1cmVwYXNzLmlvIiwibmJmIjoxNzUxODc4NTgyLCJleHAiOjIzODI1OTg1ODIsImVtYWlsIjoicGFzaHVwYXRpZ3JwX2NvbnNvbGVAc3VyZXBhc3MuaW8iLCJ0ZW5hbnRfaWQiOiJtYWluIiwidXNlcl9jbGFpbXMiOnsic2NvcGVzIjpbInVzZXIiXX19.vVom9nrkmom4XGJUEXAkntNzof1lHNwlHsRBdErWXQQ"; // Replace with your actual token

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var payload = new JObject
                {
                    ["id_number"] = rc_number
                };

                var content = new StringContent(payload.ToString(), System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(url, content);
                string responseData = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new JsonResult(new { error = "API request failed", status = (int)response.StatusCode, details = responseData });
                }

                var jsonResponse = JObject.Parse(responseData);

                var vehicleData = jsonResponse["data"];
                if (vehicleData == null)
                {
                    return new JsonResult(new { error = "No vehicle data found" });
                }

                var vehicleInfo = new RcRequest
                {
                    RcNumber = vehicleData["rc_number"]?.ToString(),
                    ClientId = vehicleData["client_id"]?.ToString(),
                    RegistrationDate = vehicleData["registration_date"]?.ToObject<DateTime?>(),
                    OwnerName = vehicleData["owner_name"]?.ToString(),
                    FatherName = vehicleData["father_name"]?.ToString(),
                    PresentAddress = vehicleData["present_address"]?.ToString(),
                    PermanentAddress = vehicleData["permanent_address"]?.ToString(),
                    MobileNumber = vehicleData["mobile_number"]?.ToString(),
                    VehicleCategory = vehicleData["vehicle_category"]?.ToString(),
                    VehicleChasiNumber = vehicleData["vehicle_chasi_number"]?.ToString(),
                    VehicleEngineNumber = vehicleData["vehicle_engine_number"]?.ToString(),
                    MakerDescription = vehicleData["maker_description"]?.ToString(),
                    MakerModel = vehicleData["maker_model"]?.ToString(),
                    BodyType = vehicleData["body_type"]?.ToString(),
                    FuelType = vehicleData["fuel_type"]?.ToString(),
                    Color = vehicleData["color"]?.ToString(),
                    NormsType = vehicleData["norms_type"]?.ToString(),
                    FitUpTo = vehicleData["fit_up_to"]?.ToObject<DateTime?>(),
                    Financer = vehicleData["financer"]?.ToString(),
                    Financed = vehicleData["financed"]?.ToObject<bool?>(),
                    InsuranceCompany = vehicleData["insurance_company"]?.ToString(),
                    InsurancePolicyNumber = vehicleData["insurance_policy_number"]?.ToString(),
                    InsuranceUpto = vehicleData["insurance_upto"]?.ToObject<DateTime?>(),
                    ManufacturingDate = vehicleData["manufacturing_date"]?.ToObject<DateTime?>(),
                    ManufacturingDateFormatted = vehicleData["manufacturing_date_formatted"]?.ToString(),
                    RegisteredAt = vehicleData["registered_at"]?.ToString(),
                    LatestBy = vehicleData["latest_by"]?.ToString(),
                    LessInfo = vehicleData["less_info"]?.ToObject<bool?>(),
                    TaxUpto = vehicleData["tax_upto"]?.ToObject<DateTime?>(),
                    TaxPaidUpto = vehicleData["tax_paid_upto"]?.ToObject<DateTime?>(),
                    CubicCapacity = vehicleData["cubic_capacity"]?.ToString(),
                    VehicleGrossWeight = vehicleData["vehicle_gross_weight"]?.ToString(),
                    NoCylinders = vehicleData["no_cylinders"]?.ToString(),
                    SeatCapacity = vehicleData["seat_capacity"]?.ToString(),
                    SleeperCapacity = vehicleData["sleeper_capacity"]?.ToString(),
                    StandingCapacity = vehicleData["standing_capacity"]?.ToString(),
                    Wheelbase = vehicleData["wheelbase"]?.ToString(),
                    UnladenWeight = vehicleData["unladen_weight"]?.ToString(),
                    VehicleCategoryDescription = vehicleData["vehicle_category_description"]?.ToString(),
                    PuccNumber = vehicleData["pucc_number"]?.ToString(),
                    PuccUpto = vehicleData["pucc_upto"]?.ToObject<DateTime?>(),
                    PermitNumber = vehicleData["permit_number"]?.ToString(),
                    PermitIssueDate = vehicleData["permit_issue_date"]?.ToObject<DateTime?>(),
                    PermitValidFrom = vehicleData["permit_valid_from"]?.ToObject<DateTime?>(),
                    PermitValidUpto = vehicleData["permit_valid_upto"]?.ToObject<DateTime?>(),
                    PermitType = vehicleData["permit_type"]?.ToString(),
                    NationalPermitNumber = vehicleData["national_permit_number"]?.ToString(),
                    NationalPermitUpto = vehicleData["national_permit_upto"]?.ToObject<DateTime?>(),
                    NationalPermitIssuedBy = vehicleData["national_permit_issued_by"]?.ToString(),
                    NonUseStatus = vehicleData["non_use_status"]?.ToString(),
                    NonUseFrom = vehicleData["non_use_from"]?.ToObject<DateTime?>(),
                    NonUseTo = vehicleData["non_use_to"]?.ToObject<DateTime?>(),
                    BlacklistStatus = vehicleData["blacklist_status"]?.ToString(),
                    NocDetails = vehicleData["noc_details"]?.ToString(),
                    OwnerNumber = vehicleData["owner_number"]?.ToString(),
                    RcStatus = vehicleData["rc_status"]?.ToString(),
                    MaskedName = vehicleData["masked_name"]?.ToObject<bool?>(),
                    ChallanDetails = vehicleData["challan_details"]?.ToString()
                };

                var global = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                string sql = "";


                string deletequery = " DELETE from GATE_VAHAN  WHERE  V_TYPE = @Vtype  AND V_NO=@V_no and COMP_CODE =@Compcode  AND BRANCH_CODE = @BranchCode  AND YEAR_CODE = @YEAR_CODE";

                using (var cmd1 = new SqlCommand(deletequery, conn))
                {
                    cmd1.Parameters.AddWithValue("@Vtype", VType);
                    cmd1.Parameters.AddWithValue("@V_no", VNo);
                    cmd1.Parameters.AddWithValue("@Compcode", global.PubCompCode);
                    cmd1.Parameters.AddWithValue("@BranchCode", global.PubBranchCode);
                    cmd1.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);

                    cmd1.ExecuteNonQuery();
                }

                sql = "INSERT INTO GATE_VAHAN (COMP_CODE, BRANCH_CODE, YEAR_CODE, V_TYPE, V_NO, client_id, rc_number, registration_date, owner_name, father_name, present_address, permanent_address, " +
                "mobile_number, vehicle_category, vehicle_chasi_number, vehicle_engine_number, maker_description, maker_model, body_type, fuel_type, Color, norms_type, fit_up_to, financer, financed, " +
                "insurance_company, insurance_policy_number, insurance_upto, manufacturing_date, manufacturing_date_formatted, registered_at, latest_by, less_info, tax_upto, tax_paid_upto, cubic_capacity, " +
                "vehicle_gross_weight, no_cylinders, seat_capacity, sleeper_capacity, standing_capacity, wheelbase, unladen_weight, vehicle_category_description, pucc_number, pucc_upto, permit_number, permit_issue_date, " +
                "permit_valid_from, permit_valid_upto, permit_type, national_permit_number, national_permit_upto, national_permit_issued_by, non_use_status, non_use_from, non_use_to, " +
                "blacklist_status, noc_details, owner_number, rc_status, masked_name, challan_details, UUSER, UDATE, AED, WSID, LIP, LID) " +
                "VALUES (@COMP_CODE, @BRANCH_CODE, @YEAR_CODE, @V_TYPE, @V_NO, @client_id, @rc_number, @registration_date, @owner_name, @father_name, @present_address, @permanent_address, " +
                "@mobile_number, @vehicle_category, @vehicle_chasi_number, @vehicle_engine_number, @maker_description, @maker_model, @body_type, @fuel_type, @color, @norms_type, @fit_up_to, @financer, @financed, " +
                "@insurance_company, @insurance_policy_number, @insurance_upto, @manufacturing_date, @manufacturing_date_formatted, @registered_at, @latest_by, @less_info, @tax_upto, @tax_paid_upto, @cubic_capacity, " +
                "@vehicle_gross_weight, @no_cylinders, @seat_capacity, @sleeper_capacity, @standing_capacity, @wheelbase, @unladen_weight, @vehicle_category_description, @pucc_number, @pucc_upto, @permit_number, @permit_issue_date, " +
                "@permit_valid_from, @permit_valid_upto, @permit_type, @national_permit_number, @national_permit_upto, @national_permit_issued_by, @non_use_status, @non_use_from, @non_use_to, " +
                "@blacklist_status, @noc_details, @owner_number, @rc_status, @masked_name, @challan_details, @UUSER, GETDATE(), @AED, @WSID, @LIP, @LID)";


                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", VType);
                    cmd.Parameters.AddWithValue("@V_NO", VNo);
                    cmd.Parameters.AddWithValue("@client_id", vehicleInfo.ClientId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@rc_number", vehicleInfo.RcNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@registration_date", vehicleInfo.RegistrationDate ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@owner_name", vehicleInfo.OwnerName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@father_name", vehicleInfo.FatherName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@present_address", vehicleInfo.PresentAddress ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@permanent_address", vehicleInfo.PermanentAddress ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@mobile_number", vehicleInfo.MobileNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@vehicle_category", vehicleInfo.VehicleCategory ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@vehicle_chasi_number", vehicleInfo.VehicleChasiNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@vehicle_engine_number", vehicleInfo.VehicleEngineNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@maker_description", vehicleInfo.MakerDescription ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@maker_model", vehicleInfo.MakerModel ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@body_type", vehicleInfo.BodyType ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@fuel_type", vehicleInfo.FuelType ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@color", vehicleInfo.Color ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@norms_type", vehicleInfo.NormsType ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@fit_up_to", vehicleInfo.FitUpTo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@financer", vehicleInfo.Financer ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@financed", vehicleInfo.Financed ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@insurance_company", vehicleInfo.InsuranceCompany ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@insurance_policy_number", vehicleInfo.InsurancePolicyNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@insurance_upto", vehicleInfo.InsuranceUpto ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@manufacturing_date", vehicleInfo.ManufacturingDate ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@manufacturing_date_formatted", vehicleInfo.ManufacturingDateFormatted ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@registered_at", vehicleInfo.RegisteredAt ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@latest_by", vehicleInfo.LatestBy ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@less_info", vehicleInfo.LessInfo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@tax_upto", vehicleInfo.TaxUpto ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@tax_paid_upto", vehicleInfo.TaxPaidUpto ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@cubic_capacity", vehicleInfo.CubicCapacity ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@vehicle_gross_weight", vehicleInfo.VehicleGrossWeight ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@no_cylinders", vehicleInfo.NoCylinders ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@seat_capacity", vehicleInfo.SeatCapacity ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@sleeper_capacity", vehicleInfo.SleeperCapacity ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@standing_capacity", vehicleInfo.StandingCapacity ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@wheelbase", vehicleInfo.Wheelbase ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@unladen_weight", vehicleInfo.UnladenWeight ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@vehicle_category_description", vehicleInfo.VehicleCategoryDescription ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@pucc_number", vehicleInfo.PuccNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@pucc_upto", vehicleInfo.PuccUpto ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@permit_number", vehicleInfo.PermitNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@permit_issue_date", vehicleInfo.PermitIssueDate ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@permit_valid_from", vehicleInfo.PermitValidFrom ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@permit_valid_upto", vehicleInfo.PermitValidUpto ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@permit_type", vehicleInfo.PermitType ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@national_permit_number", vehicleInfo.NationalPermitNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@national_permit_upto", vehicleInfo.NationalPermitUpto ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@national_permit_issued_by", vehicleInfo.NationalPermitIssuedBy ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@non_use_status", vehicleInfo.NonUseStatus ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@non_use_from", vehicleInfo.NonUseFrom ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@non_use_to", vehicleInfo.NonUseTo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@blacklist_status", vehicleInfo.BlacklistStatus ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@noc_details", vehicleInfo.NocDetails ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@owner_number", vehicleInfo.OwnerName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@rc_status", vehicleInfo.RcStatus ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@masked_name", vehicleInfo.MaskedName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@challan_details", vehicleInfo.ChallanDetails ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", global.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", global.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@UUSER", global.PubUserId);

                    // Execute the query
                    cmd.ExecuteNonQuery();
                }

                return new JsonResult(new { success = true, message = "Data inserted successfully." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
        }

        public class RcRequest
        {
            public string? RcNumber { get; set; }
            public int? CompCode { get; set; }
            public int? BranchCode { get; set; }
            public int? YearCode { get; set; }
            public string? VType { get; set; }
            public int? VNo { get; set; }
            public string? ClientId { get; set; }
            public DateTime? RegistrationDate { get; set; }
            public string? OwnerName { get; set; }
            public string? FatherName { get; set; }
            public string? PresentAddress { get; set; }
            public string? PermanentAddress { get; set; }
            public string? MobileNumber { get; set; }
            public string? VehicleCategory { get; set; }
            public string? VehicleChasiNumber { get; set; }
            public string? VehicleEngineNumber { get; set; }
            public string? MakerDescription { get; set; }
            public string? MakerModel { get; set; }
            public string? BodyType { get; set; }
            public string? FuelType { get; set; }
            public string? Color { get; set; }
            public string? NormsType { get; set; }
            public DateTime? FitUpTo { get; set; }
            public string? Financer { get; set; }
            public bool? Financed { get; set; }
            public string? InsuranceCompany { get; set; }
            public string? InsurancePolicyNumber { get; set; }
            public DateTime? InsuranceUpto { get; set; }
            public DateTime? ManufacturingDate { get; set; }
            public string? ManufacturingDateFormatted { get; set; }
            public string? RegisteredAt { get; set; }
            public string? LatestBy { get; set; }
            public bool? LessInfo { get; set; }
            public DateTime? TaxUpto { get; set; }
            public DateTime? TaxPaidUpto { get; set; }
            public string? CubicCapacity { get; set; }
            public string? VehicleGrossWeight { get; set; }
            public string? NoCylinders { get; set; }
            public string? SeatCapacity { get; set; }
            public string? SleeperCapacity { get; set; }
            public string? StandingCapacity { get; set; }
            public string? Wheelbase { get; set; }
            public string? UnladenWeight { get; set; }
            public string? VehicleCategoryDescription { get; set; }
            public string? PuccNumber { get; set; }
            public DateTime? PuccUpto { get; set; }
            public string? PermitNumber { get; set; }
            public DateTime? PermitIssueDate { get; set; }
            public DateTime? PermitValidFrom { get; set; }
            public DateTime? PermitValidUpto { get; set; }
            public string? PermitType { get; set; }
            public string? NationalPermitNumber { get; set; }
            public DateTime? NationalPermitUpto { get; set; }
            public string? NationalPermitIssuedBy { get; set; }
            public string? NonUseStatus { get; set; }
            public DateTime? NonUseFrom { get; set; }
            public DateTime? NonUseTo { get; set; }
            public string? BlacklistStatus { get; set; }
            public string? NocDetails { get; set; }
            public string? OwnerNumber { get; set; }
            public string? RcStatus { get; set; }
            public bool? MaskedName { get; set; }
            public string? ChallanDetails { get; set; }
            public int? UUser { get; set; }
            public DateTime? UDate { get; set; }
            public int? EUser { get; set; }
            public DateTime? EDate { get; set; }
            public string? Aed { get; set; }
            public string? Wsid { get; set; }
            public string? Lip { get; set; }
            public string? Lid { get; set; }
            public int? SrNo { get; set; }
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
        public async Task<JsonResult> GetVehcleFastaginfo([FromQuery] string rc_number, string VType, int VNo)
        {
            try
            {
                using var client = new HttpClient();
                string url = "https://kyc-api.surepass.app/api/v1/fastag/fastag-verification-v2";

                string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJmcmVzaCI6ZmFsc2UsImlhdCI6MTc1MTg3ODU4MiwianRpIjoiYzczZmFkMTAtZjk0MC00NzdkLThlNDgtMjU3ZTViMzVkYjY4IiwidHlwZSI6ImFjY2VzcyIsImlkZW50aXR5IjoiZGV2LnBhc2h1cGF0aWdycF9jb25zb2xlQHN1cmVwYXNzLmlvIiwibmJmIjoxNzUxODc4NTgyLCJleHAiOjIzODI1OTg1ODIsImVtYWlsIjoicGFzaHVwYXRpZ3JwX2NvbnNvbGVAc3VyZXBhc3MuaW8iLCJ0ZW5hbnRfaWQiOiJtYWluIiwidXNlcl9jbGFpbXMiOnsic2NvcGVzIjpbInVzZXIiXX19.vVom9nrkmom4XGJUEXAkntNzof1lHNwlHsRBdErWXQQ";

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var payload = new JObject
                {
                    ["rc_number"] = rc_number
                };

                var content = new StringContent(payload.ToString(), System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(url, content);
                string responseData = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new JsonResult(new
                    {
                        error = "API request failed",
                        status = (int)response.StatusCode,
                        details = responseData
                    });
                }

                var global = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                // ✅ DELETE OLD DATA
                string deletequery = @"DELETE FROM GATE_FASTAG WHERE V_TYPE = @Vtype AND V_NO = @V_no  AND COMP_CODE = @Compcode  AND BRANCH_CODE = @BranchCode  AND YEAR_CODE = @YEAR_CODE";

                using (var cmd1 = new SqlCommand(deletequery, conn))
                {
                    cmd1.Parameters.AddWithValue("@Vtype", VType);
                    cmd1.Parameters.AddWithValue("@V_no", VNo);
                    cmd1.Parameters.AddWithValue("@Compcode", global.PubCompCode);
                    cmd1.Parameters.AddWithValue("@BranchCode", global.PubBranchCode);
                    cmd1.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd1.ExecuteNonQuery();
                }

                // ✅ INSERT QUERY
                string sql = @"INSERT INTO GATE_FASTAG
                (YEAR_CODE,COMP_CODE,BRANCH_CODE,V_TYPE,V_NO,ClientId,RcNumber,BankName,TagId,Status,FastagId,
                LaneDirection,TransactionDateTime,SeqNo,TollPlazaGeoCode,TollPlazaName,VehicleType,UUSER,UDATE,AED,WSID,LIP,LID,TransactionId)
                VALUES
                (@YEAR_CODE,@COMP_CODE,@BRANCH_CODE,@V_TYPE,@V_NO,@ClientId,@RcNumber,@BankName,@TagId,@Status,@FastagId,
                @LaneDirection,@TransactionDateTime,@SeqNo,@TollPlazaGeoCode,@TollPlazaName,@VehicleType,@UUSER,GETDATE(),@AED,@WSID,@LIP,@LID,@TRANSACTIONID)";

                // ✅ PARSE JSON
                var json = JObject.Parse(responseData);
                var data = json["data"];
                var transactions = data["transactions"];

                int rowNo = 0;



                if (!transactions.HasValues)
                {
                    return new JsonResult(new { error = "Data Not Found", status = false });
                }

                foreach (var item in transactions)
                {
                    var model = new FasttagList
                    {
                        V_TYPE = VType,
                        V_NO = VNo,
                        ClientId = data["client_id"]?.ToString(),
                        RcNumber = data["rc_number"]?.ToString(),
                        BankName = data["bank_name"]?.ToString(),
                        TagId = data["tag_id"]?.ToString(),
                        Status = data["status"]?.ToString(),
                        LaneDirection = item["lane_direction"]?.ToString()?.FirstOrDefault(),
                        TransactionDateTime = Convert.ToDateTime(item["transaction_date_time"]),
                        SeqNo = item["seq_no"]?.ToString(),
                        TollPlazaGeoCode = item["toll_plaza_geocode"]?.ToString(),
                        TollPlazaName = item["toll_plaza_name"]?.ToString(),
                        VehicleType = item["vehicle_type"]?.ToString()
                    };

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = global.PubFYearCode;
                        cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = global.PubCompCode;
                        cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = global.PubBranchCode;
                        cmd.Parameters.Add("@V_TYPE", SqlDbType.NVarChar, 4).Value = model.V_TYPE ?? "";
                        cmd.Parameters.Add("@V_NO", SqlDbType.Int).Value = model.V_NO;
                        cmd.Parameters.Add("@ClientId", SqlDbType.NVarChar, 100).Value = model.ClientId ?? "";
                        cmd.Parameters.Add("@RcNumber", SqlDbType.NVarChar, 20).Value = model.RcNumber ?? "";
                        cmd.Parameters.Add("@BankName", SqlDbType.NVarChar, 100).Value = model.BankName ?? "";
                        cmd.Parameters.Add("@TagId", SqlDbType.NVarChar, 50).Value = model.TagId ?? "";
                        cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = model.Status ?? "";
                        cmd.Parameters.Add("@FastagId", SqlDbType.Int).Value = (object?)model.FastagId ?? DBNull.Value;
                        cmd.Parameters.Add("@LaneDirection", SqlDbType.Char, 1).Value = (object?)model.LaneDirection ?? DBNull.Value;
                        cmd.Parameters.Add("@TransactionDateTime", SqlDbType.DateTime2).Value = (object?)model.TransactionDateTime ?? DBNull.Value;
                        cmd.Parameters.Add("@TRANSACTIONID", SqlDbType.BigInt).Value = rowNo;
                        cmd.Parameters.Add("@SeqNo", SqlDbType.NVarChar, 50).Value = model.SeqNo ?? "";
                        cmd.Parameters.Add("@TollPlazaGeoCode", SqlDbType.NVarChar, 50).Value = model.TollPlazaGeoCode ?? "";
                        cmd.Parameters.Add("@TollPlazaName", SqlDbType.NVarChar, 150).Value = model.TollPlazaName ?? "";
                        cmd.Parameters.Add("@VehicleType", SqlDbType.NVarChar, 10).Value = model.VehicleType ?? "";
                        cmd.Parameters.Add("@UUSER", SqlDbType.Int).Value = global.PubUserId;
                        cmd.Parameters.Add("@AED", SqlDbType.NVarChar, 1).Value = "A";
                        cmd.Parameters.Add("@WSID", SqlDbType.NVarChar, 30).Value = global.PubWorkStationID ?? "";
                        cmd.Parameters.Add("@LIP", SqlDbType.NVarChar, 30).Value = global.PubLocalId ?? "";
                        cmd.Parameters.Add("@LID", SqlDbType.NVarChar, 30).Value = Environment.MachineName;
                        cmd.ExecuteNonQuery();
                        rowNo++;
                    }
                }

                return new JsonResult(new { success = true, message = "Data saved successfully", count = transactions.Count() });
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
        public class FasttagList
        {
            public string? V_TYPE { get; set; }
            public int V_NO { get; set; }
            public string ClientId { get; set; }
            public string RcNumber { get; set; }
            public string BankName { get; set; }
            public string TagId { get; set; }
            public string Status { get; set; }
            public long TransactionId { get; set; }
            public int? FastagId { get; set; }
            public char? LaneDirection { get; set; }
            public DateTime? TransactionDateTime { get; set; }
            public string SeqNo { get; set; }
            public string TollPlazaGeoCode { get; set; }
            public string TollPlazaName { get; set; }
            public string VehicleType { get; set; }

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

        private async Task<string> AuthenticateEWayBillAsync()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            try
            {
                string unm = "API_pashupati";
                string pas = Uri.EscapeDataString("Ksp@5588");

                using var client = new HttpClient();

                string url =
                    "https://api.mastergst.com/ewaybillapi/v1.03/authenticate" +
                    "?email=it%40pashupatigrp.com" +
                    $"&username={unm}&password={pas}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                request.Headers.Add("ip_address", getdata.ip_address);
                request.Headers.Add("client_id", getdata.client_id);
                request.Headers.Add("client_secret", getdata.client_secret);
                request.Headers.Add("gstin", getdata.gstin);
                request.Headers.Add("auth_access_type", getdata.auth_access_type);

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return null;

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("status_cd", out var status) &&
                    status.GetString() == "1")
                {

                    return status.GetString();

                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetEWayBillData(DateTime edate, string inoutdata)
        {
            try
            {
                string token = await AuthenticateEWayBillAsync();
                var getdata = _globalVariableService.GetGlobalVariables();

                if (string.IsNullOrEmpty(token))
                {
                    return new JsonResult(new { success = false, message = "Auth failed" });
                }

                string dt = Uri.EscapeDataString(edate.ToString("dd/MM/yyyy"));

                string apiUrl = inoutdata == "IN"
                    ? $"https://api.mastergst.com/ewaybillapi/v1.03/ewayapi/getewaybillsofotherparty?email=it%40pashupatigrp.com&date={dt}"
                    : $"https://api.mastergst.com/ewaybillapi/v1.03/ewayapi/getewaybillsbydate?email=it%40pashupatigrp.com&date={dt}";

                using var client = new HttpClient();

                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

                request.Headers.Add("ip_address", getdata.ip_address);
                request.Headers.Add("client_id", getdata.client_id);
                request.Headers.Add("client_secret", getdata.client_secret);
                request.Headers.Add("gstin", getdata.gstin);
                request.Headers.Add("auth_access_type", getdata.auth_access_type);
                request.Headers.Add("authtoken", token);

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();


                if (!response.IsSuccessStatusCode)
                {
                    return new JsonResult(new { success = false, message = content });
                }
       
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
       

                if(inoutdata == "IN")
                {
                    var dataArray = root.GetProperty("data");
                    List<EwayBillData> list = new List<EwayBillData>();

                    foreach (var item in dataArray.EnumerateArray())
                    {
                        int partyCode = 0;
                        string query = @"SELECT v_date, ISNULL(party_code, 0) AS Party_Code 
                        FROM PURCHASE1  WHERE BILL_GST = @gstin  AND comp_code = @compCode  ORDER BY v_date DESC";

                        using (SqlConnection con = _dbConnection.GetErpConnection())
                        {
                            con.Open();

                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@gstin", item.GetProperty("fromGstin").GetString());
                                cmd.Parameters.AddWithValue("@compCode", getdata.PubCompCode);

                                using (SqlDataReader reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        DateTime vDate = reader.GetDateTime(0);
                                        partyCode = reader.GetInt32(1);
                                    }
                                }
                            }
                        }

                        list.Add(new EwayBillData
                        {
                            PARTY_CODE = partyCode,
                            ewbNo = item.GetProperty("ewbNo").ValueKind == JsonValueKind.Number ? item.GetProperty("ewbNo").GetInt64() : long.TryParse(item.GetProperty("ewbNo").GetString(), out var ewb) ? ewb : 0,
                            ewayBillDate = item.GetProperty("ewayBillDate").GetString(),
                            genMode = item.GetProperty("genMode").GetString(),
                            docNo = item.GetProperty("docNo").GetString(),
                            docDate = item.GetProperty("docDate").GetString(),
                            fromGstin = item.GetProperty("fromGstin").GetString(),
                            fromTradeName = item.GetProperty("fromTradeName").GetString(),
                            toGstin = item.GetProperty("toGstin").GetString(),
                            toTradeName = item.GetProperty("toTradeName").GetString(),
                            totInvValue = item.GetProperty("totInvValue").ValueKind == JsonValueKind.Number ? item.GetProperty("totInvValue").GetDecimal() : decimal.TryParse(item.GetProperty("totInvValue").GetString(), out var val) ? val : 0,
                            hsnCode = item.GetProperty("hsnCode").ValueKind == JsonValueKind.Number
                            ? item.GetProperty("hsnCode").GetInt32()
                            : int.TryParse(item.GetProperty("hsnCode").GetString(), out var hsn) ? hsn : 0,
                            hsnDesc = item.GetProperty("hsnDesc").GetString(),
                            status = item.GetProperty("status").GetString(),
                            rejectStatus = item.GetProperty("rejectStatus").GetString(),
                            FORM_NO = (item.GetProperty("ewbNo").ValueKind == JsonValueKind.Number
                            ? item.GetProperty("ewbNo").GetInt64()
                            : long.TryParse(item.GetProperty("ewbNo").GetString(), out var ewb2) ? ewb2 : 0).ToString(),
                            FORM_DATE = DateTime.TryParse(item.GetProperty("ewayBillDate").GetString(), out var fd) ? fd : (DateTime?)null,
                            BILL_NO = item.GetProperty("docNo").GetString(), // ✅ fixed
                            BILL_DATE = DateTime.TryParse(item.GetProperty("docDate").GetString(), out var bd) ? bd : (DateTime?)null,
                            PARTY_GSTIN = item.GetProperty("fromGstin").GetString(),
                            OTHER_GSTIN = item.GetProperty("toGstin").GetString(),
                            HSN_CODE = item.GetProperty("hsnCode").ValueKind == JsonValueKind.Number
                            ? item.GetProperty("hsnCode").GetInt32()
                            : int.TryParse(item.GetProperty("hsnCode").GetString(), out var hsn2) ? hsn2 : 0,
                            ITEM_DESC = item.GetProperty("hsnDesc").GetString(),
                            BILL_AMT = item.TryGetProperty("totInvValue", out var amt) && amt.ValueKind != JsonValueKind.Null
                            ? (amt.ValueKind == JsonValueKind.Number
                            ? amt.GetDecimal()
                            : decimal.TryParse(amt.GetString(), out var a) ? a : (decimal?)null)
                            : null,

                            STATUS = item.GetProperty("status").GetString() == "ACT" ? 1 : 0
                        });
                    }
                    await EwayBillInsertData(list, edate);
                    return new JsonResult(new { success = true, count = list.Count, message = "EWaybill data import and Saved successfully." });
                }
                else
                {
                    return new JsonResult(new { success = true,  message = "EWaybill data import and Saved successfully." });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<string> EwayBillInsertData(List<EwayBillData> list , DateTime edate)
        {
            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                var jsonResult = GetVNo("TRIN", "WAYBILL1") as JsonResult;
                dynamic data = jsonResult.Value;
                int srvno = Convert.ToInt32(data.V_NO);

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    foreach (var obj in list)
                    {
                        string sql = @"
                            INSERT INTO waybill1
                            (COMP_CODE,BRANCH_CODE,YEAR_CODE,V_TYPE,V_NO,DOC_ID,FORM_NO,FORM_DATE,PARTY_CODE,PARTY_GSTIN,
                            OTHER_GSTIN,BILL_NO,BILL_DATE, HSN_CODE,ITEM_DESC,STATUS,UUSER,UDATE,AED,WSID,LIP,LID,GATE_TYPE)
                            VALUES
                            (@COMP_CODE,@BRANCH_CODE,@YEAR_CODE,@V_TYPE,@V_NO,@DOC_ID,@FORM_NO,@FORM_DATE,@PARTY_CODE,@PARTY_GSTIN,@OTHER_GSTIN,
                            @BILL_NO,@BILL_DATE,@HSN_CODE,@ITEM_DESC,@STATUS,@UUSER,@UDATE,@AED,@WSID,@LIP,@LID,@GATE_TYPE)";

                        using SqlCommand cmd = new SqlCommand(sql, con);

                        cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", "TRIN");
                        cmd.Parameters.AddWithValue("@V_NO", srvno);
                        cmd.Parameters.AddWithValue("@DOC_ID", "TRIN" + srvno);
                        cmd.Parameters.AddWithValue("@FORM_NO", obj.FORM_NO ?? "");
                        cmd.Parameters.AddWithValue("@FORM_DATE", (object)obj.FORM_DATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PARTY_CODE", obj.PARTY_CODE ?? 0);
                        cmd.Parameters.AddWithValue("@PARTY_GSTIN", obj.PARTY_GSTIN ?? "");
                        cmd.Parameters.AddWithValue("@OTHER_GSTIN", obj.OTHER_GSTIN ?? "");
                        cmd.Parameters.AddWithValue("@BILL_NO", obj.BILL_NO ?? "");
                        cmd.Parameters.AddWithValue("@BILL_DATE", (object)obj.BILL_DATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@HSN_CODE", obj.HSN_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ITEM_DESC", obj.ITEM_DESC ?? "");
                        cmd.Parameters.AddWithValue("@STATUS", obj.STATUS ?? 0);
                        cmd.Parameters.AddWithValue("@UUSER", getdata.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", getdata.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", getdata.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@GATE_TYPE", "TEST");
                        cmd.ExecuteNonQuery();
                    }
                }

                string query = @"SELECT ISNULL(FORM_NO, '') AS FORM_NO,  V_No FROM WAYBILL1 WHERE Gate_type = 'TEST'
                AND Comp_code = @Comp_code AND Branch_code = @Branch_code ;";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    List<string> formList = new List<string>();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@edate", edate);
                        cmd.Parameters.AddWithValue("@Comp_code", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@Branch_code", getdata.PubBranchCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                             await GetEWayBillDataOTHER(reader["FORM_NO"]?.ToString());               
                            }
                        }
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        [HttpGet]
        public async Task<string> GetEWayBillDataOTHER(string ewaybno)
        {
            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                string apiUrl = $"https://api.mastergst.com/ewaybillapi/v1.03/ewayapi/getewaybill?email=it%40pashupatigrp.com&ewbNo={ewaybno}";

                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

                request.Headers.Add("ip_address", "103.74.69.13");
                request.Headers.Add("client_id", "8a2017bb-6f67-4bf9-bc62-46bd802ed390");
                request.Headers.Add("client_secret", "5e3dd92c-64ba-440f-a964-1a396397da66");
                request.Headers.Add("gstin", "05AAFCP0864M1Z7");
                request.Headers.Add("auth_access_type", "read");

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return "False";

                using var doc = JsonDocument.Parse(content);

                var data = doc.RootElement.GetProperty("data");

                // ✅ Extract values (replacement of VB substring logic)
                string expiryDate = data.GetProperty("validUpto").GetString();
                decimal billAmt = data.GetProperty("totalValue").GetDecimal();
                decimal totalAmt = data.GetProperty("totInvValue").GetDecimal();
                decimal cgst = data.GetProperty("cgstValue").GetDecimal();
                decimal sgst = data.GetProperty("sgstValue").GetDecimal();
                decimal igst = data.GetProperty("igstValue").GetDecimal();

                // Vehicle details array
                var vehicle = data.GetProperty("VehiclListDetails")[0];

                string truckNo = vehicle.GetProperty("vehicleNo").GetString();
                string transport = vehicle.GetProperty("userGSTINTransin").GetString();
                string grNo = vehicle.GetProperty("transDocNo").GetString();
                string grDate = vehicle.GetProperty("transDocDate").GetString();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    string sql = @"
                        UPDATE WAYBILL1 SET 
                        EXPIRY_DATE = @EXPIRY_DATE,
                        BILL_AMT    = @BILL_AMT,
                        TOTAL_AMT   = @TOTAL_AMT,
                        CGST_AMT    = @CGST_AMT,
                        SGST_AMT    = @SGST_AMT,
                        IGST_AMT    = @IGST_AMT,
                        TRUCK_NO    = @TRUCK_NO,
                        TRANSPORT   = @TRANSPORT,
                        GR_NO       = @GR_NO,
                        GR_DATE     = @GR_DATE
                        WHERE 
                        Gate_Type   = 'TEST'
                        AND FORM_NO = @ewaybno
                        AND Comp_code = @Comp_code
                        AND BRANCH_CODE = @BRANCH_CODE;";

                    using SqlCommand cmd = new SqlCommand(sql, con);

 
                    // ✅ EXPIRY DATE
                    cmd.Parameters.Add("@EXPIRY_DATE", SqlDbType.SmallDateTime).Value = DateTime.TryParseExact(  data.GetProperty("validUpto").GetString(),
                        "dd/MM/yyyy hh:mm:ss tt",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime expDate)
                        ? expDate
                        : (object)DBNull.Value;

                    // ✅ AMOUNTS
                    cmd.Parameters.AddWithValue("@BILL_AMT", billAmt);
                    cmd.Parameters.AddWithValue("@TOTAL_AMT", totalAmt);
                    cmd.Parameters.AddWithValue("@CGST_AMT", cgst);
                    cmd.Parameters.AddWithValue("@SGST_AMT", sgst);
                    cmd.Parameters.AddWithValue("@IGST_AMT", igst);

                    // ✅ STRINGS
                    cmd.Parameters.AddWithValue("@TRUCK_NO", truckNo ?? "");
                    cmd.Parameters.AddWithValue("@TRANSPORT", transport ?? "");
                    cmd.Parameters.AddWithValue("@GR_NO", grNo ?? "");

                    // ✅ GR DATE (FIXED — was wrong earlier)
                    cmd.Parameters.Add("@GR_DATE", SqlDbType.SmallDateTime).Value =  DateTime.TryParseExact(  grDate,  "dd/MM/yyyy",  CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime grd)
                    ? grd
                    : (object)DBNull.Value;

                    // ✅ OTHER PARAMS
                    cmd.Parameters.AddWithValue("@ewaybno", ewaybno);
                    cmd.Parameters.AddWithValue("@Comp_code", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);

                    await cmd.ExecuteNonQueryAsync();
                }

                return "True";
            }
            catch (Exception ex)
            {
                return ex.Message; // better for debugging
            }
        }
        public class EwayBillData
        {
            public string FORM_NO { get; set; }
            public DateTime? FORM_DATE { get; set; }
            public DateTime? EXPIRY_DATE { get; set; }
            public int? PARTY_CODE { get; set; }
            public string PARTY_GSTIN { get; set; }
            public string BILL_NO { get; set; }
            public DateTime? BILL_DATE { get; set; }
            public string GR_NO { get; set; }
            public DateTime? GR_DATE { get; set; }
            public string TRUCK_NO { get; set; }
            public string TRANSPORT { get; set; }
            public string PO_STATUS { get; set; }
            public string ORD_TYPE { get; set; }
            public int? ORD_NO { get; set; }
            public int? HSN_CODE { get; set; }
            public string ITEM_DESC { get; set; }
            public decimal? NOS { get; set; }
            public decimal? BILL_AMT { get; set; }
            public decimal? SGST_AMT { get; set; }
            public decimal? CGST_AMT { get; set; }
            public decimal? IGST_AMT { get; set; }
            public decimal? CESS_AMT { get; set; }
            public decimal? CESS_NONADVOLAMT { get; set; }
            public decimal? OTHER_AMT { get; set; }
            public decimal? TOTAL_AMT { get; set; }
            public string GATE_TYPE { get; set; }
            public int? GATE_NO { get; set; }
            public DateTime? GATE_DATE { get; set; }
            public int? STATUS { get; set; }
            public string OTHER_GSTIN { get; set; }
            public DateTime? ARRIVAL_DATE { get; set; }

            public long ewbNo { get; set; }
            public string ewayBillDate { get; set; }
            public string genMode { get; set; }
            public string genGstin { get; set; }
            public string docNo { get; set; }
            public string docDate { get; set; }
            public string fromGstin { get; set; }
            public string fromTradeName { get; set; }
            public string toGstin { get; set; }
            public string toTradeName { get; set; }
            public decimal totInvValue { get; set; }
            public int hsnCode { get; set; }
            public string hsnDesc { get; set; }
            public string status { get; set; }
            public string rejectStatus { get; set; }





        }









        // Drp down

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
