using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Api_Model;
using static travelexpensemanagement.Controllers.GateEntry.Transaction.InwardEntryController;

namespace travelexpensemanagement.Common.Globalvariable
{
    public class GlobalValidationdate
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly ModuleService.ModuleService _moduleService;
        private readonly IConfiguration _configuration;
        public GlobalValidationdate(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            ModuleService.ModuleService moduleService, IConfiguration configuration)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _configuration = configuration;
        }
        public async Task<ValidationResult> CheckValidDate(string tablename, DateTime vdate, string vtype, string vno)
        {
            try
            {
                var global = _globalVariableService.GetGlobalVariables();
                using (var conn = _dbConnection.GetErpConnection())
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand("sp_GlobalCheckValidDate", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TableName", tablename);
                        cmd.Parameters.AddWithValue("@VDate", vdate);
                        cmd.Parameters.AddWithValue("@VType", vtype);
                        cmd.Parameters.AddWithValue("@VNo", vno);
                        cmd.Parameters.AddWithValue("@CompCode", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", global.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", global.PubFYearCode);
                        cmd.Parameters.AddWithValue("@LoginDate", global.PubLoginDate);
                        cmd.Parameters.AddWithValue("@ServerDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@DateFormat", "dd/MM/yyyy");

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new ValidationResult
                                {
                                    Status = Convert.ToBoolean(reader["IsValid"]),
                                    Message = reader["Message"].ToString()
                                };
                            }
                        }
                    }
                }
                return new ValidationResult
                {
                    Status = false,
                    Message = "No response from server"
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }

        public void LogInsertUpdateDelete(string destinationTable, string sourceTable, string transactionType,
            string codeVNo, string vtype = "", string condition1 = "", string condition2 = "", string condition3 = "")
        {
            var global = _globalVariableService.GetGlobalVariables();
            string imgDatabaseName = "";

            // STEP 1: Get IMGDATABASE_NAME from MAIN DB
            using (var connMain = _dbConnection.GetConDbConnection())
            {
                connMain.Open();
                string imgQuery = "SELECT IMGDATABASE_NAME FROM COMP_MAST WHERE CODE = " + global.PubCompCode + "";

                using (var cmd = new SqlCommand(imgQuery, connMain))
                {
                    var result = cmd.ExecuteScalar();
                    imgDatabaseName = result?.ToString();
                }
            }
            // STEP 2: ERP DB Operations
            using (var conn = _dbConnection.GetErpConnection())
            {
                conn.Open();
                // Get Column List
                string columnQuery = $@" DECLARE @cols AS NVARCHAR(MAX);
                SELECT @cols = STUFF(( SELECT ',' + name FROM SYS.COLUMNS WHERE OBJECT_ID = OBJECT_ID('{sourceTable}') 
                AND is_computed = 0 FOR XML PATH(''), TYPE).value('.', 'VARCHAR(MAX)'), 1, 1, ''); SELECT @cols; ";
                string columnsList = ExecuteScalar(conn, columnQuery);
                // Check COMP_CODE exists
                bool hasCompCode = CheckExists(conn, $@"SELECT 1  FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{sourceTable}' 
                AND COLUMN_NAME = 'COMP_CODE' ");

                // Build Query
                string mqry = "";

                if (transactionType == "Master")
                {
                    if (hasCompCode)
                    {
                        mqry = $@"INSERT INTO {imgDatabaseName}.dbo.{destinationTable} ({columnsList})
                        SELECT {columnsList} FROM {sourceTable} WHERE Code = {Convert.ToInt32(codeVNo)} AND comp_code = {global.PubCompCode}";
                    }
                    else
                    {
                        mqry = $@" INSERT INTO {imgDatabaseName}.dbo.{destinationTable} ({columnsList})
                        SELECT {columnsList}  FROM {sourceTable} WHERE Code = {Convert.ToInt32(codeVNo)}";
                    }
                }
                else
                {
                    mqry = $@" INSERT INTO {imgDatabaseName}.dbo.{destinationTable} ({columnsList})
                SELECT {columnsList} FROM {sourceTable} WHERE V_No = {Convert.ToInt32(codeVNo)} AND V_Type = '{vtype}'
                AND comp_code = {global.PubCompCode} AND Branch_code = {global.PubBranchCode} AND Year_code = {global.PubFYearCode}";
                }
                // Execute Final Query
                ExecuteNonQuery(conn, mqry);
            }
        }
        private string ExecuteScalar(SqlConnection conn, string query)
        {
            using (var cmd = new SqlCommand(query, conn))
            {
                var result = cmd.ExecuteScalar();
                return result?.ToString();
            }
        }
        private bool CheckExists(SqlConnection conn, string query)
        {
            using (var cmd = new SqlCommand(query, conn))
            {
                var result = cmd.ExecuteScalar();
                return result != null;
            }
        }
        private void ExecuteNonQuery(SqlConnection conn, string query)
        {
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
        public class ValidationResult
        {
            public bool Status { get; set; }
            public string Message { get; set; }
        }

        public string GetVNo(string Vtype, string Tablename = "")
        {
            string newV_NO = "00000";
            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";

                    using (SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con))
                    {
                        prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                        string prefixYR = prefixCmd.ExecuteScalar()?.ToString();

                        string lastV_NO_Query = @"SELECT MAX(CAST(V_NO AS INT)) FROM " + Tablename + @" 
                                         WHERE COMP_CODE = @CompCode 
                                         AND YEAR_CODE = @YearCode 
                                         AND BRANCH_CODE = @BranchCode 
                                         AND V_TYPE = @Vtype";

                        using (SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con))
                        {
                            lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                            lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                            lastVnoCmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                            lastVnoCmd.Parameters.AddWithValue("@Vtype", Vtype);

                            object result = lastVnoCmd.ExecuteScalar();

                            if (result != DBNull.Value && result != null)
                            {
                                int lastV_NO = Convert.ToInt32(result);
                                newV_NO = (lastV_NO + 1).ToString("D5");
                            }
                            else
                            {
                                newV_NO = prefixYR + "00001";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return null;
            }

            return newV_NO;
        }
        private async Task<string> AuthenticateEWayBillAsync()
        {
            var GlobalVaraible = _globalVariableService.GetGlobalVariables();
            var LoadGeneralSetting = await _globalVariableService.LoadGeneralSetting();
            try
            {
                string password = LoadGeneralSetting.PubEinvPass;
                string unm = LoadGeneralSetting.PubEinvUName;
                string pas = Uri.EscapeDataString(password);
                using var client = new HttpClient();
                string url = "https://api.mastergst.com/ewaybillapi/v1.03/authenticate" + "?email=it%40pashupatigrp.com" + $"&username={unm}&password={pas}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("ip_address", LoadGeneralSetting.PubEinvIP);
                request.Headers.Add("client_id", LoadGeneralSetting.PubEWayBillCID);
                request.Headers.Add("client_secret", LoadGeneralSetting.PubEWayBillCSID);
                request.Headers.Add("gstin", LoadGeneralSetting.PubEinvGSTIN);
                request.Headers.Add("auth_access_type", "read");
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
                var GlobalVaraible = _globalVariableService.GetGlobalVariables();
                var LoadGeneralSetting = await _globalVariableService.LoadGeneralSetting();

                string token = await AuthenticateEWayBillAsync();
                if (string.IsNullOrEmpty(token))
                    return new JsonResult(new { success = false, message = "Auth failed" });

                string dt = Uri.EscapeDataString(edate.ToString("dd/MM/yyyy"));

                string apiUrl = inoutdata == "IN"
                    ? $"https://api.mastergst.com/ewaybillapi/v1.03/ewayapi/getewaybillsofotherparty?email=it%40pashupatigrp.com&date={dt}"
                    : $"https://api.mastergst.com/ewaybillapi/v1.03/ewayapi/getewaybillsbydate?email=it%40pashupatigrp.com&date={dt}";

                using var client = new HttpClient();

                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

                request.Headers.Add("ip_address", LoadGeneralSetting.PubEinvIP);
                request.Headers.Add("client_id", LoadGeneralSetting.PubEWayBillCID);
                request.Headers.Add("client_secret", LoadGeneralSetting.PubEWayBillCSID);
                request.Headers.Add("gstin", LoadGeneralSetting.PubEinvGSTIN);
                request.Headers.Add("auth_access_type", GlobalVaraible.auth_access_type);
                request.Headers.Add("authtoken", token);
                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new JsonResult(new { success = false, message = content });

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (!root.TryGetProperty("data", out JsonElement dataArray))
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Eway Bill Data Not found For this Date "
                    });
                }

                if (inoutdata != "IN")
                    return new JsonResult(new { success = true, message = "No IN data" });

                dataArray = root.GetProperty("data");

                List<EwayBillData> list = new List<EwayBillData>();

                // ? LOAD PARTY DATA ONCE (NO LOOP DB CALL)
                Dictionary<string, int> partyLookup = new();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string query = @"SELECT BILL_GST, ISNULL(party_code,0) FROM PURCHASE1 WHERE comp_code = @compCode";

                    using SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@compCode", GlobalVaraible.PubCompCode);

                    using SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        partyLookup[reader.GetString(0)] = reader.GetInt32(1);
                    }
                }

                foreach (var item in dataArray.EnumerateArray())
                {
                    string gst = item.GetProperty("fromGstin").GetString();
                    partyLookup.TryGetValue(gst, out int partyCode);

                    list.Add(new EwayBillData
                    {
                        PARTY_CODE = partyCode,
                        FORM_NO = item.GetProperty("ewbNo").ToString(),
                        //FORM_DATE = DateTime.TryParse(item.GetProperty("ewayBillDate").GetString(), out var fd) ? fd : null,
                        FORM_DATE = item.GetProperty("ewayBillDate").GetString(),
                        BILL_NO = item.GetProperty("docNo").GetString(),
                        //BILL_DATE = DateTime.TryParse(item.GetProperty("docDate").GetString(), out var bd) ? bd : null,
                        BILL_DATE = item.GetProperty("docDate").GetString(),
                        PARTY_GSTIN = gst,
                        OTHER_GSTIN = item.GetProperty("toGstin").GetString(),
                        ITEM_DESC = item.GetProperty("hsnDesc").GetString(),
                        STATUS = item.GetProperty("status").GetString() == "ACT" ? 1 : 0
                    });
                }

                await EwayBillInsertData(list, edate);

                return new JsonResult(new
                {
                    success = true,
                    count = list.Count,
                    message = "Imported EWayBill Data successfully"
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<string> EwayBillInsertData(List<EwayBillData> list, DateTime edate)
        {
            string[] formats =
            {
                "dd/MM/yyyy",
                "dd/MM/yyyy hh:mm tt",
                "dd/MM/yyyy hh:mm:ss tt"
            };
            try
            {
                var GlobalVaraible = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    foreach (var obj in list)
                    {
                        string eWaybillNo = obj.FORM_NO ?? "";

                        string existsQuery = @"SELECT 1 FROM waybill1   WHERE comp_code=@comp AND branch_code=@branch  AND year_code=@year AND FORM_NO=@formNo";

                        using (SqlCommand checkCmd = new SqlCommand(existsQuery, con))
                        {
                            checkCmd.Parameters.AddWithValue("@comp", GlobalVaraible.PubCompCode);
                            checkCmd.Parameters.AddWithValue("@branch", GlobalVaraible.PubBranchCode);
                            checkCmd.Parameters.AddWithValue("@year", GlobalVaraible.PubFYearCode);
                            checkCmd.Parameters.AddWithValue("@formNo", eWaybillNo);

                            var exists = await checkCmd.ExecuteScalarAsync();
                            if (exists != null)
                                continue;
                        }

                        string gst = obj.PARTY_GSTIN ?? "";
                        int party_code = 0;

                        // ?? Get party_code from PURCHASE1
                        string purchaseQuery = @"SELECT TOP 1 ISNULL(party_code,0) 
                        FROM PURCHASE1 
                        WHERE BILL_GST=@gst AND comp_code=@comp 
                        ORDER BY v_date DESC";

                        using (SqlCommand cmd2 = new SqlCommand(purchaseQuery, con))
                        {
                            cmd2.Parameters.AddWithValue("@gst", gst);
                            cmd2.Parameters.AddWithValue("@comp", GlobalVaraible.PubCompCode);

                            var result = await cmd2.ExecuteScalarAsync();
                            if (result != null)
                                party_code = Convert.ToInt32(result);
                        }

                        // ?? If not found, get from SUBGROUP_MAST
                        if (party_code == 0)
                        {
                            string subQuery = @"SELECT ISNULL(a.code,0)
                            FROM SUBGROUP_MAST a
                            LEFT JOIN subgroup_Address b 
                            ON a.CODE=b.CODE AND a.COMP_CODE=b.COMP_CODE
                            WHERE a.Active=1 
                            AND a.nature='Supplier'
                            AND b.GSTIN=@gst 
                            AND a.comp_code=@comp";

                            using (SqlCommand cmd3 = new SqlCommand(subQuery, con))
                            {
                                cmd3.Parameters.AddWithValue("@gst", gst);
                                cmd3.Parameters.AddWithValue("@comp", GlobalVaraible.PubCompCode);

                                var result = await cmd3.ExecuteScalarAsync();
                                if (result != null)
                                    party_code = Convert.ToInt32(result);
                            }
                        }

                        // ?? Skip if still not found
                        if (party_code == 0)
                            continue;

                        // ?? Generate voucher number
                        string vno = GetVNo("TRIN", "WAYBILL1");
                        int srvno = Convert.ToInt32(vno);



                        DateTime? formDate = null;
                        if (!string.IsNullOrWhiteSpace(obj.FORM_DATE))
                        {
                            if (DateTime.TryParseExact(
                                obj.FORM_DATE,
                                formats,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out var fd))
                            {
                                formDate = fd;
                            }
                        }

                        DateTime? billDate = null;
                        if (!string.IsNullOrWhiteSpace(obj.BILL_DATE))
                        {
                            if (DateTime.TryParseExact(
                                obj.BILL_DATE,
                                formats,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out var bd))
                            {
                                billDate = bd;
                            }
                        }

                        // ?? Insert into waybill1
                        string sql = @"INSERT INTO waybill1
                        (COMP_CODE,BRANCH_CODE,YEAR_CODE,V_TYPE,V_NO,DOC_ID,FORM_NO,FORM_DATE,PARTY_CODE,PARTY_GSTIN,
                        OTHER_GSTIN,BILL_NO,BILL_DATE,ITEM_DESC,STATUS,UUSER,UDATE,GATE_TYPE)
                        VALUES
                        (@COMP_CODE,@BRANCH_CODE,@YEAR_CODE,@V_TYPE,@V_NO,@DOC_ID,@FORM_NO,@FORM_DATE,@PARTY_CODE,@PARTY_GSTIN,
                        @OTHER_GSTIN,@BILL_NO,@BILL_DATE,@ITEM_DESC,@STATUS,@UUSER,@UDATE,'TEST')";

                        using (SqlCommand cmd = new SqlCommand(sql, con))
                        {
                            cmd.Parameters.AddWithValue("@COMP_CODE", GlobalVaraible.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", GlobalVaraible.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", GlobalVaraible.PubFYearCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", "TRIN");
                            cmd.Parameters.AddWithValue("@V_NO", srvno);
                            cmd.Parameters.AddWithValue("@DOC_ID", "TRIN" + srvno);
                            cmd.Parameters.AddWithValue("@FORM_NO", eWaybillNo);
                            //cmd.Parameters.AddWithValue("@FORM_DATE", (object)obj.FORM_DATE ?? DBNull.Value);
                            cmd.Parameters.Add("@FORM_DATE", SqlDbType.SmallDateTime).Value = formDate ?? (object)DBNull.Value;
                            cmd.Parameters.AddWithValue("@PARTY_CODE", party_code);
                            cmd.Parameters.AddWithValue("@PARTY_GSTIN", gst);
                            cmd.Parameters.AddWithValue("@OTHER_GSTIN", obj.OTHER_GSTIN ?? "");
                            cmd.Parameters.AddWithValue("@BILL_NO", obj.BILL_NO ?? "");
                            //cmd.Parameters.AddWithValue("@BILL_DATE", (object)obj.BILL_DATE ?? DBNull.Value);
                            cmd.Parameters.Add("@BILL_DATE", SqlDbType.SmallDateTime).Value = billDate ?? (object)DBNull.Value;
                            cmd.Parameters.AddWithValue("@ITEM_DESC", obj.ITEM_DESC ?? "");
                            cmd.Parameters.AddWithValue("@STATUS", obj.STATUS ?? 0);
                            cmd.Parameters.AddWithValue("@UUSER", GlobalVaraible.PubUserId);
                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // ?? Controlled parallel API calls (avoid overload)
                var semaphore = new SemaphoreSlim(5);

                var tasks = list.Select(async x =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await GetEWayBillDataOTHER(x.FORM_NO);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

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
                var GlobalVaraible = _globalVariableService.GetGlobalVariables();
                var LoadGeneralSetting = await _globalVariableService.LoadGeneralSetting();

                string apiUrl = $"https://api.mastergst.com/ewaybillapi/v1.03/ewayapi/getewaybill?email=it%40pashupatigrp.com&ewbNo={ewaybno}";

                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

                request.Headers.Add("ip_address", LoadGeneralSetting.PubEinvIP);
                request.Headers.Add("client_id", LoadGeneralSetting.PubEWayBillCID);
                request.Headers.Add("client_secret", LoadGeneralSetting.PubEWayBillCSID);
                request.Headers.Add("gstin", LoadGeneralSetting.PubEinvGSTIN);
                request.Headers.Add("auth_access_type", GlobalVaraible.auth_access_type);

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return "False";

                using var doc = JsonDocument.Parse(content);

                var data = doc.RootElement.GetProperty("data");

                // ? Extract values (replacement of VB substring logic)
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


                    // ? EXPIRY DATE
                    cmd.Parameters.Add("@EXPIRY_DATE", SqlDbType.SmallDateTime).Value = DateTime.TryParseExact(data.GetProperty("validUpto").GetString(),
                        "dd/MM/yyyy hh:mm:ss tt",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime expDate)
                        ? expDate
                        : (object)DBNull.Value;

                    // ? AMOUNTS
                    cmd.Parameters.AddWithValue("@BILL_AMT", billAmt);
                    cmd.Parameters.AddWithValue("@TOTAL_AMT", totalAmt);
                    cmd.Parameters.AddWithValue("@CGST_AMT", cgst);
                    cmd.Parameters.AddWithValue("@SGST_AMT", sgst);
                    cmd.Parameters.AddWithValue("@IGST_AMT", igst);

                    // ? STRINGS
                    cmd.Parameters.AddWithValue("@TRUCK_NO", truckNo ?? "");
                    cmd.Parameters.AddWithValue("@TRANSPORT", transport ?? "");
                    cmd.Parameters.AddWithValue("@GR_NO", grNo ?? "");

                    // ? GR DATE (FIXED — was wrong earlier)
                    cmd.Parameters.Add("@GR_DATE", SqlDbType.SmallDateTime).Value = DateTime.TryParseExact(grDate, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime grd)
                    ? grd
                    : (object)DBNull.Value;

                    // ? OTHER PARAMS
                    cmd.Parameters.AddWithValue("@ewaybno", ewaybno);
                    cmd.Parameters.AddWithValue("@Comp_code", GlobalVaraible.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", GlobalVaraible.PubBranchCode);

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
            //public DateTime? FORM_DATE { get; set; }
            public string FORM_DATE { get; set; }
            public DateTime? EXPIRY_DATE { get; set; }
            public int? PARTY_CODE { get; set; }
            public string PARTY_GSTIN { get; set; }
            public string BILL_NO { get; set; }
            //public DateTime? BILL_DATE { get; set; }
            public string BILL_DATE { get; set; }
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

        [HttpGet]
        public async Task<IActionResult> GetVehicleInfo(string rc_number, string VType, int VNo)
        {
            try
            {
                using var client = new HttpClient();

                string url = _configuration["VehicleApiKey:Url"];
                string token = _configuration["VehicleApiKey:Token"];


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
                    return new JsonResult(new
                    {
                        success = false,
                        message = responseData
                    });
                }

                var jsonResponse = JObject.Parse(responseData);

                var vehicleData = jsonResponse["data"];
                if (vehicleData == null)
                {
                    return new JsonResult(new { error = "No vehicle data found" });
                }

                var vehicleInfo = new RcRequest_Model
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


                return new JsonResult(new
                {
                    success = true,
                    message = "Data inserted successfully"
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

        [HttpGet]
        public async Task<JsonResult> GetVehcleFastaginfo([FromQuery] string rc_number, string VType, int VNo)
        {
            try
            {
                using var client = new HttpClient();
                string url = _configuration["FasttagApiKey:Url"];
                string token = _configuration["FasttagApiKey:Token"];


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

                // ? DELETE OLD DATA
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

                // ? INSERT QUERY
                string sql = @"INSERT INTO GATE_FASTAG
                (YEAR_CODE,COMP_CODE,BRANCH_CODE,V_TYPE,V_NO,ClientId,RcNumber,BankName,TagId,Status,FastagId,
                LaneDirection,TransactionDateTime,SeqNo,TollPlazaGeoCode,TollPlazaName,VehicleType,UUSER,UDATE,AED,WSID,LIP,LID,TransactionId)
                VALUES
                (@YEAR_CODE,@COMP_CODE,@BRANCH_CODE,@V_TYPE,@V_NO,@ClientId,@RcNumber,@BankName,@TagId,@Status,@FastagId,
                @LaneDirection,@TransactionDateTime,@SeqNo,@TollPlazaGeoCode,@TollPlazaName,@VehicleType,@UUSER,GETDATE(),@AED,@WSID,@LIP,@LID,@TRANSACTIONID)";

                // ? PARSE JSON
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
                    var model = new FasttagList_model
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


        // How to call any page 
        //_globalValidationdate.LogInsertUpdateDelete(destinationTable: "gate1", sourceTable: "gate1",  transactionType: "Transaction",
        //        codeVNo: "262700001", vtype: "INFU");

        //======================Check Modification Days For Edit==================
        public (int isAllowed, string message) CheckModificationDays(DateTime vDate)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            int allowed = 1;
            string? message = "";
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_checkModificationDays", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@vdate", vDate);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@userCode", gv.PubUserId);
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                allowed = reader["Not_allowed"] != DBNull.Value ? Convert.ToInt32(reader["Not_allowed"]) : 1;
                                message = reader["Message"]?.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                allowed = 1;
                message = ex.Message;
            }
            return (allowed, message ?? "");
        }
        //How to call
        //[HttpGet]
        //public JsonResult checkModificationDays(DateTime? vDate)
        //{
        //    if (!vDate.HasValue)
        //    {
        //        return Json(new { success = false, message = "Doc Date is empty!!" });
        //    }
        //    var (allowed, message) = _globalValidationdate.CheckModificationDays(vDate.Value);
        //    return Json(new { success = true, isAllowed = allowed, message = message });
        //}

        public byte[] ExportToExcel(string spName, string sheetName, Dictionary<string, object> parameters = null)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(spName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(
                                param.Key,
                                param.Value ?? DBNull.Value
                            );
                        }
                    }

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            using var workbook = new ClosedXML.Excel.XLWorkbook();

            var worksheet = workbook.Worksheets.Add(sheetName);

            worksheet.Cell(1, 1).InsertTable(dt);

            worksheet.Row(1).Style.Font.Bold = true;

            worksheet.Row(1).Style.Fill.BackgroundColor =
                ClosedXML.Excel.XLColor.LightBlue;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return stream.ToArray();
        }
    }
}


