using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text.Json;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Common.Globalvariable
{
    public class GlobalValidationdate
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly ModuleService.ModuleService _moduleService;

        public GlobalValidationdate(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
        }
        public async Task<ValidationResult> CheckValidDate( string tablename, DateTime vdate, string vtype, string vno)
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
                string imgQuery = "SELECT IMGDATABASE_NAME FROM COMP_MAST WHERE CODE = "+ global.PubCompCode +"";

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
                var getdata = _globalVariableService.GetGlobalVariables();

                string token = await AuthenticateEWayBillAsync();
                if (string.IsNullOrEmpty(token))
                    return new JsonResult(new { success = false, message = "Auth failed" });

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
                    return new JsonResult(new { success = false, message = content });

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (inoutdata != "IN")
                    return new JsonResult(new { success = true, message = "No IN data" });

                var dataArray = root.GetProperty("data");

                List<EwayBillData> list = new List<EwayBillData>();

                // ✅ LOAD PARTY DATA ONCE (NO LOOP DB CALL)
                Dictionary<string, int> partyLookup = new();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string query = @"SELECT BILL_GST, ISNULL(party_code,0) FROM PURCHASE1 WHERE comp_code = @compCode";

                    using SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@compCode", getdata.PubCompCode);

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
                        FORM_DATE = DateTime.TryParse(item.GetProperty("ewayBillDate").GetString(), out var fd) ? fd : null,
                        BILL_NO = item.GetProperty("docNo").GetString(),
                        BILL_DATE = DateTime.TryParse(item.GetProperty("docDate").GetString(), out var bd) ? bd : null,
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
            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                string vno = GetVNo("TRIN", "WAYBILL1");
                int srvno = Convert.ToInt32(vno);

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    foreach (var obj in list)
                    {
                        string sql = @"INSERT INTO waybill1
                (COMP_CODE,BRANCH_CODE,YEAR_CODE,V_TYPE,V_NO,DOC_ID,FORM_NO,FORM_DATE,PARTY_CODE,PARTY_GSTIN,
                OTHER_GSTIN,BILL_NO,BILL_DATE,ITEM_DESC,STATUS,UUSER,UDATE,GATE_TYPE)
                VALUES
                (@COMP_CODE,@BRANCH_CODE,@YEAR_CODE,@V_TYPE,@V_NO,@DOC_ID,@FORM_NO,@FORM_DATE,@PARTY_CODE,@PARTY_GSTIN,
                @OTHER_GSTIN,@BILL_NO,@BILL_DATE,@ITEM_DESC,@STATUS,@UUSER,@UDATE,'TEST')";

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
                        cmd.Parameters.AddWithValue("@ITEM_DESC", obj.ITEM_DESC ?? "");
                        cmd.Parameters.AddWithValue("@STATUS", obj.STATUS ?? 0);
                        cmd.Parameters.AddWithValue("@UUSER", getdata.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // ✅ PARALLEL API CALLS
                var tasks = list.Select(x => GetEWayBillDataOTHER(x.FORM_NO));
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
                var getdata = _globalVariableService.GetGlobalVariables();

                string apiUrl = $"https://api.mastergst.com/ewaybillapi/v1.03/ewayapi/getewaybill?email=it%40pashupatigrp.com&ewbNo={ewaybno}";

                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

                request.Headers.Add("ip_address", getdata.ip_address);
                request.Headers.Add("client_id", getdata.client_id);
                request.Headers.Add("client_secret", getdata.client_secret);
                request.Headers.Add("gstin", getdata.gstin);
                request.Headers.Add("auth_access_type", getdata.auth_access_type);

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
                    cmd.Parameters.Add("@EXPIRY_DATE", SqlDbType.SmallDateTime).Value = DateTime.TryParseExact(data.GetProperty("validUpto").GetString(),
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
                    cmd.Parameters.Add("@GR_DATE", SqlDbType.SmallDateTime).Value = DateTime.TryParseExact(grDate, "dd/MM/yyyy", CultureInfo.InvariantCulture,
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




        // How to call any page 
        //_globalValidationdate.LogInsertUpdateDelete(destinationTable: "gate1", sourceTable: "gate1",  transactionType: "Transaction",
        //        codeVNo: "262700001", vtype: "INFU");
    }
}