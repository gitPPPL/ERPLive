using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class EInvoiceUtilityRepository : IEInvoiceUtilityRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public EInvoiceUtilityRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        private string _authToken = string.Empty;

        //==========================Get EInvoice List==========================
        public RepositoryResponseList<EInvoiceUtilityModel> GetEInvoiceList(string vType)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                var list = new List<EInvoiceUtilityModel>();

                string sql;

                if (vType == "DNSL" || vType == "CRSL")
                {
                    sql = @"SELECT a.V_Type, a.V_No, a.V_Date, CAST(a.Namount AS numeric(20,2)) AS Namount, ISNULL(a.FAPROV_STATUS,'') AS FAPROV_STATUS,
                           b.name AS Bill_Name, a.Bill_GST, a.Bill_Pincode, a.Ship_Name, a.Ship_GST, a.Ship_Pincode,
                           ISNULL(a.IRN,'') AS IRN, IIF(ISNULL(a.IRN,'')<>'','Generated','') AS EStatus,
                           IIF(ISNULL(a.EWAYBILL_NO,'')<>'','Generated','') AS EWB,
                           IIF(a.Status=1,'OK',IIF(a.Status=2,'Cancelled','')) AS CStatus,
                           a.Tran_type AS TranType
                    FROM DC_NOTE1 a
                    LEFT JOIN subgroup_mast b ON a.DRCR_CODE = b.code AND a.comp_code = b.comp_code
                    WHERE a.v_type = @V_TYPE AND a.comp_code = @COMP_CODE AND a.branch_code = @BRANCH_CODE AND a.year_code = @YEAR_CODE
                    ORDER BY a.V_No DESC";
                }
                else if (vType == "SAGT" || vType == "SASI" || vType == "SAST" || vType == "SART" || vType == "ESAG" || vType == "SATD")
                {
                    sql = @"SELECT V_Type, V_No, V_Date, CAST(Namount AS numeric(20,2)) AS Namount, ISNULL(FAPROV_STATUS,'') AS FAPROV_STATUS,
                           Bill_Name, Bill_GST, Bill_Pincode, Ship_Name, Ship_GST, Ship_Pincode, ISNULL(IRN,'') AS IRN,
                           IIF(ISNULL(IRN,'')<>'','Generated','') AS EStatus,
                           IIF(ISNULL(EWAYBILL_NO,'')<>'','Generated','') AS EWB,
                           IIF(Status=1,'OK',IIF(Status=2,'Cancelled','')) AS CStatus,
                           Tran_type AS TranType
                    FROM Sale1
                    WHERE faprov_status = 'Approved' AND v_type = @V_TYPE AND comp_code = @COMP_CODE AND branch_code = @BRANCH_CODE AND year_code = @YEAR_CODE 
                    ORDER BY V_No DESC";
                }
                else
                {
                    return new RepositoryResponseList<EInvoiceUtilityModel> { status = true, data = list };
                }

                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new EInvoiceUtilityModel
                    {
                        VType = reader["V_Type"]?.ToString(),
                        VNo = Convert.ToInt64(reader["V_No"]),
                        VDate = reader["V_Date"] == DBNull.Value ? "" : Convert.ToDateTime(reader["V_Date"]).ToString("dd/MM/yyyy"),
                        NetAmount = reader["Namount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Namount"]),
                        ApprovalStatus = reader["FAPROV_STATUS"]?.ToString(),
                        BillName = reader["Bill_Name"]?.ToString(),
                        BillGst = reader["Bill_GST"]?.ToString(),
                        BillPincode = reader["Bill_Pincode"]?.ToString(),
                        ShipName = reader["Ship_Name"]?.ToString(),
                        ShipGst = reader["Ship_GST"]?.ToString(),
                        ShipPincode = reader["Ship_Pincode"]?.ToString(),
                        Irn = reader["IRN"]?.ToString(),
                        EInvoiceStatus = reader["EStatus"]?.ToString(),
                        EwbStatus = reader["EWB"]?.ToString(),
                        CancelStatus = reader["CStatus"]?.ToString(),
                        TranType = reader["TranType"]?.ToString()
                    });
                }

                return new RepositoryResponseList<EInvoiceUtilityModel> { status = true, data = list };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<EInvoiceUtilityModel> { status = false, message = ex.Message };
            }
        }

        //==========================Get EInvoice Status==========================
        public async Task<RepositoryResponseData<EInvoiceStatusModel>> GetInvoiceStatus(string vType, long vNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                string[] saleTypes = { "SAGT", "SASI", "SAST", "SART", "ESAG", "SATD" };
                string table = saleTypes.Contains(vType) ? "Sale1" : "DC_Note1";

                string sql = $@"SELECT ISNULL(FAPROV_STATUS,'') AS FAPROV_STATUS,
                               ISNULL(STATUS,0)         AS STATUS,
                               ISNULL(IRN,'')           AS IRN,
                               ISNULL(EWAYBILL_NO,'')   AS EWAYBILL_NO
                        FROM {table}
                        WHERE v_type = @V_TYPE AND v_no = @V_NO
                          AND comp_code = @COMP_CODE
                          AND branch_code = @BRANCH_CODE
                          AND year_code = @YEAR_CODE";

                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return new RepositoryResponseData<EInvoiceStatusModel> { status = false, message = "Invoice not found." };

                var model = new EInvoiceStatusModel
                {
                    ApprovalStatus = reader["FAPROV_STATUS"]?.ToString() ?? "",
                    IsCancelled = Convert.ToInt32(reader["STATUS"]) == 2,
                    Irn = reader["IRN"]?.ToString() ?? "",
                    EwaybillNo = reader["EWAYBILL_NO"]?.ToString() ?? ""
                };
                model.IsEInvoiceGenerated = model.Irn != "";
                model.IsEwbGenerated = model.EwaybillNo != "";

                return new RepositoryResponseData<EInvoiceStatusModel> { status = true, data = model };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<EInvoiceStatusModel> { status = false, message = ex.Message };
            }
        }

        //==========================Get Signed JSON==========================
        public async Task<RepositoryResponseData<string>> GetSignedJson(string vType, long vNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                string[] saleTypes = { "SAGT", "SASI", "SAST", "SART", "ESAG", "SATD" };
                string table = saleTypes.Contains(vType) ? "Sale1" : "DC_Note1";

                string sql = $@"SELECT ISNULL(SIGNED_JSON,'') AS SIGNED_JSON
                        FROM {table}
                        WHERE v_type = @V_TYPE AND v_no = @V_NO
                          AND comp_code = @COMP_CODE
                          AND branch_code = @BRANCH_CODE
                          AND year_code = @YEAR_CODE";

                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                var result = await cmd.ExecuteScalarAsync();
                string signedJson = result == null || result == DBNull.Value ? "" : result.ToString();

                return new RepositoryResponseData<string> { status = true, data = signedJson };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<string> { status = false, message = ex.Message };
            }
        }

        //==========================Get EWayBill No==========================
        public async Task<RepositoryResponseData<string>> GetEWayBillNo(string vType, long vNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                string sql = @"Select isnull(EWAYBILL_NO,'') from Sale1 where V_TYPE=@V_TYPE and V_NO=@V_NO and Comp_Code=@COMP_CODE and 
                                Branch_code=@BRANCH_CODE and Year_Code=@YEAR_CODE";

                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                var result = await cmd.ExecuteScalarAsync();
                string WaybillNo = result == null || result == DBNull.Value ? "" : result.ToString();

                return new RepositoryResponseData<string> { status = true, data = WaybillNo };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<string> { status = false, message = ex.Message };
            }
        }

        //==========================Get IRN==========================
        public async Task<RepositoryResponseData<string>> GetIRN(string vType, long vNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                string sql = @"Select isnull(IRN,'') from QrImage_Path where V_TYPE=@V_TYPE and V_NO=@V_NO and Comp_Code=@Comp_Code and Branch_code=@Branch_code
                                and Year_Code=@Year_Code";

                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                var result = await cmd.ExecuteScalarAsync();
                string irn = result == null || result == DBNull.Value ? "" : result.ToString();

                return new RepositoryResponseData<string> { status = true, data = irn };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<string> { status = false, message = ex.Message };
            }
        }

        //=============================API Credentials========================
        public async Task<RepositoryResponseData<ApiModeModel>> GetApiMode()
        {
            try
            {
                var c = await GetCreds();
                var data = new ApiModeModel
                {
                    isLive = c.IsLive,
                    gstin = c.Gstin
                };
                return new RepositoryResponseData<ApiModeModel> { status = true, data = data };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<ApiModeModel> { status = false, message = ex.Message };
            }
        }
        private async Task<Creds> GetCreds()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var gs = await _globalVariableService.LoadGeneralSetting();

            var query = $@"SELECT EINV_LIVECONFIG, EINV_LIP, EINV_LUSERNAME, EINV_LPASS, EINV_LCLIENTID, EINV_LCLIENTSID, EINV_LGSTIN,
                                EINV_TIP, EINV_TUSERNAME, EINV_TPASS, EINV_TCLIENTID, EINV_TCLIENTSID, EINV_TGSTIN
                         FROM SYS_SERVICE WHERE COMP_CODE=@COMP_CODE";

            using (var con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (!await dr.ReadAsync())
                            throw new Exception("E-Invoice API configuration not found.");

                        bool isLive = Convert.ToInt32(dr["EINV_LIVECONFIG"]) == 1;

                        var c = new Creds
                        {
                            IsLive = isLive
                        };

                        if (isLive)
                        {
                            c.Ip = Convert.ToString(dr["EINV_LIP"]);
                            c.Username = Convert.ToString(dr["EINV_LUSERNAME"]);
                            c.Password = Convert.ToString(dr["EINV_LPASS"]);
                            c.ClientId = Convert.ToString(dr["EINV_LCLIENTID"]);
                            c.ClientSid = Convert.ToString(dr["EINV_LCLIENTSID"]);
                            c.Gstin = Convert.ToString(dr["EINV_LGSTIN"]);
                        }
                        else
                        {
                            c.Ip = Convert.ToString(dr["EINV_TIP"]);
                            c.Username = Convert.ToString(dr["EINV_TUSERNAME"]);
                            c.Password = Convert.ToString(dr["EINV_TPASS"]);
                            c.ClientId = Convert.ToString(dr["EINV_TCLIENTID"]);
                            c.ClientSid = Convert.ToString(dr["EINV_TCLIENTSID"]);
                            c.Gstin = Convert.ToString(dr["EINV_TGSTIN"]);
                        }

                        c.SellerGSTIN = gs.PubEinvGSTIN;

                        if (isLive)
                        {
                            if (gv.PubCompCode == "7")
                            {
                                c.SellerPIN = "501503";
                                c.SellerLoc = "Ranga Reddy District";
                                c.SellerMob = "9392918844";
                            }
                            else if (gv.PubCompCode == "5")
                            {
                                c.SellerPIN = "303008";
                                c.SellerLoc = "Gidani";
                                c.SellerMob = "9634094222";
                            }
                            else if (gv.PubCompCode == "8")
                            {
                                c.SellerPIN = "301707";
                                c.SellerLoc = "Khushkhera";
                                c.SellerMob = "7082030401";
                            }
                            else
                            {
                                c.SellerPIN = "244713";
                                c.SellerLoc = "Kashipur";
                                c.SellerMob = "9634090052";
                            }
                        }
                        else
                        {
                            c.SellerPIN = "560007";
                            c.SellerLoc = "Kashipur";
                            c.SellerMob = "9999999999";
                        }

                        if (!string.IsNullOrWhiteSpace(c.SellerGSTIN) &&
                            c.SellerGSTIN.Length >= 2)
                        {
                            c.SellerSTCD = c.SellerGSTIN.Substring(0, 2);
                        }

                        return c;

                    }
                }
            }

        }

        //==============================E-Invoice Authentication==========================
        private async Task<MasterGstAuthResult> GetAuthentication()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                var gs = await _globalVariableService.LoadGeneralSetting();

                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.mastergst.com/einvoice/authenticate?email=it%40pashupatigrp.com");

                request.Headers.Add("username", gs.PubEinvUName);
                request.Headers.Add("password", gs.PubEinvPass);
                request.Headers.Add("ip_address", gs.PubEinvIP);
                request.Headers.Add("client_id", gs.PubEinvCID);
                request.Headers.Add("client_secret", gs.PubEinvCSID);
                request.Headers.Add("gstin", gs.PubEinvGSTIN);
                request.Headers.Add("auth_access_type", "read");

                request.Content = new StringContent("", Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new MasterGstAuthResult
                    {
                        success = false,
                        message = $"HTTP Error: {(int)response.StatusCode}",
                        response = responseContent
                    };
                }


                var result = JsonConvert.DeserializeObject<MasterGstAuthResponse>(responseContent);

                if (result == null || !string.Equals(result.status_cd, "sucess", StringComparison.OrdinalIgnoreCase))
                {
                    return new MasterGstAuthResult
                    {
                        success = false,
                        message = result?.status_desc ?? "Authentication failed.",
                        response = responseContent
                    };
                }


                _authToken = result.data?.AuthToken;

                return new MasterGstAuthResult
                {
                    success = true,
                    authToken = result.data?.AuthToken,
                    tokenExpiry = result.data?.TokenExpiry,
                    sek = result.data?.Sek,
                    clientId = result.data?.ClientId
                };
            }
            catch (Exception ex)
            {
                return new MasterGstAuthResult { success = false, message = ex.Message };
            }
        }

        //==========================Create JSON==========================
        private async Task<EInvoiceJsonResult> CreateJSON(string vType, int vNo, bool cessNAValue = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vType) || vNo <= 0)
                    return new EInvoiceJsonResult { success = false, message = "Voucher No. not selected." };

                var gv = _globalVariableService.GetGlobalVariables();
                var c = await GetCreds();

                string invType = "";

                if (vType == "SAGT" || vType == "SASI" || vType == "SAST" ||
                    vType == "ESAG" || vType == "SATD")
                    invType = "INV";
                else if (vType == "SART" || vType == "CRSL")
                    invType = "CRN";
                else if (vType == "DNSL")
                    invType = "DBN";

                if (string.IsNullOrEmpty(invType))
                    return new EInvoiceJsonResult { success = false, message = "Invalid voucher type." };

                DataTable dt = new();

                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    string sql;

                    if (vType == "SAGT" || vType == "SASI" || vType == "SAST" || vType == "SART" || vType == "ESAG" || vType == "SATD")
                    {
                        sql = @"SELECT *,b.name City,c.name ShipCity,
                        s1.Aliasname EXPBILLNAME,s2.Aliasname EXPSHIPNAME
                        FROM Sale1 a
                        LEFT JOIN Subgroup_mast s1 ON a.Bill_Code=s1.code
                            AND a.Comp_code=s1.Comp_code
                        LEFT JOIN Subgroup_mast s2 ON a.Ship_Code=s2.code
                            AND a.Comp_code=s2.Comp_code
                        LEFT JOIN CITY_MAST b ON a.bill_city=b.code
                        LEFT JOIN CITY_MAST c ON a.SHIP_CITY=c.code
                        WHERE v_type=@v_type AND v_no=@v_no
                        AND a.comp_code=@comp_code
                        AND a.branch_code=@branch_code
                        AND a.year_code=@year_code";
                    }
                    else
                    {
                        sql = @"SELECT *,c.state_code StCode,c.name City,d.name ShipCity,
                        b.code Bill_code,b.name Bill_Name
                        FROM DC_NOTE1 a
                        LEFT JOIN subgroup_mast b ON a.DRCR_CODE=b.code
                            AND a.comp_code=b.comp_code
                        LEFT JOIN CITY_MAST c ON a.bill_city=c.code
                        LEFT JOIN CITY_MAST d ON a.SHIP_CITY=d.code
                        WHERE v_type=@v_type AND v_no=@v_no
                        AND a.comp_code=@comp_code
                        AND branch_code=@branch_code
                        AND year_code=@year_code
                        ORDER BY V_No DESC";
                    }

                    using var cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@v_type", vType);
                    cmd.Parameters.AddWithValue("@v_no", vNo);
                    cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@branch_code", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@year_code", gv.PubFYearCode);

                    using var da = new SqlDataAdapter(cmd);
                    da.Fill(dt);
                }

                if (dt.Rows.Count == 0)
                    return new EInvoiceJsonResult { success = false, message = "Voucher not found." };

                DataRow h = dt.Rows[0];

                string billPin = Convert.ToString(h["BILL_PINCODE"]);
                string shipPin = Convert.ToString(h["SHIP_PINCODE"]);
                string supplyType = Convert.ToString(h["SUPPLY_TYPE"]);
                string billGst = Convert.ToString(h["BILL_GST"]).Trim();
                string shipGst = Convert.ToString(h["SHIP_GST"]).Trim();

                // ---------------------------------------------------------
                // VALIDATIONS
                // ---------------------------------------------------------

                if (ToInt(billPin) == 0)
                {
                    string name;

                    if (vType == "DNSL" || vType == "CRSL")
                        name = Convert.ToString(h["BILL_NAME1"]);
                    else if (billPin == "999999" &&
                             (supplyType == "EXPWOP" || supplyType == "EXPWP"))
                        name = Convert.ToString(h["EXPBILLNAME"]);
                    else
                        name = Convert.ToString(h["BILL_NAME"]);

                    return new EInvoiceJsonResult { success = false, message = "Invalid Pin code of Party : " + name };
                }

                if (ToInt(shipPin) == 0)
                {
                    string name;

                    if (shipPin == "999999" && (supplyType == "EXPWOP" || supplyType == "EXPWP"))
                        name = Convert.ToString(h["EXPSHIPNAME"]);
                    else
                        name = Convert.ToString(h["BILL_NAME"]);

                    return new EInvoiceJsonResult { success = false, message = "Invalid Pin code of Consignee : " + name };
                }

                if (billGst.Length == 0)
                {
                    string name;

                    if (billPin == "999999" && (supplyType == "EXPWOP" || supplyType == "EXPWP"))
                        name = Convert.ToString(h["EXPBILLNAME"]);
                    else
                        name = Convert.ToString(h["BILL_NAME"]);

                    return new EInvoiceJsonResult { success = false, message = "Bill GST No. Required of Party : " + name };
                }

                if (shipGst.Length == 0)
                {
                    string name;

                    if (shipPin == "999999" && (supplyType == "EXPWOP" || supplyType == "EXPWP"))
                        name = Convert.ToString(h["EXPSHIPNAME"]);
                    else
                        name = Convert.ToString(h["Ship_NAME"]);

                    return new EInvoiceJsonResult { success = false, message = "Ship GST No. Required of Shipping Party : " + name };
                }

                string docNo = vType + vNo;

                string compName = Convert.ToString(gv.CompanyName);
                string compAdd1 = Convert.ToString(gv.Address1);
                string compAdd2 = Convert.ToString(gv.Address2);

                // ---------------------------------------------------------
                // STRING BUILDER - SAME AS VB
                // ---------------------------------------------------------

                StringBuilder builder = new();

                builder.Append("{" + Environment.NewLine);
                builder.Append("\"Version\": \"1.1\"," + Environment.NewLine);

                // ---------------------------------------------------------
                // TranDtls
                // ---------------------------------------------------------

                if (billPin == "999999" && supplyType == "EXPWOP")
                {
                    builder.Append("\"TranDtls\": {\"TaxSch\": \"GST\",\"SupTyp\": \"EXPWOP\",\"RegRev\": \"N\",\"EcmGstin\": null,\"IgstOnIntra\": \"N\"}," + Environment.NewLine);
                }
                else if (billPin == "999999" && supplyType == "EXPWP")
                {
                    builder.Append("\"TranDtls\": {\"TaxSch\": \"GST\",\"SupTyp\": \"EXPWP\",\"RegRev\": \"N\",\"EcmGstin\": null,\"IgstOnIntra\": \"N\"}," + Environment.NewLine);
                }
                else if (supplyType == "SEZWOP")
                {
                    builder.Append("\"TranDtls\": {\"TaxSch\": \"GST\",\"SupTyp\": \"SEZWOP\",\"RegRev\": \"N\",\"EcmGstin\": null,\"IgstOnIntra\": \"N\"}," + Environment.NewLine);
                }
                else
                {
                    builder.Append("\"TranDtls\": {\"TaxSch\": \"GST\",\"SupTyp\": \"B2B\",\"RegRev\": \"N\",\"EcmGstin\": null,\"IgstOnIntra\": \"N\"}," + Environment.NewLine);
                }

                if (docNo.Length != 13)
                    return new EInvoiceJsonResult { success = false, message = "Invalid Invoice no., Please check it." };

                // ---------------------------------------------------------
                // DocDtls
                // ---------------------------------------------------------

                builder.Append("\"DocDtls\": {");
                builder.Append("\"Typ\": \"" + invType + "\",");
                builder.Append("\"No\": \"" + docNo + "\",");
                builder.Append("\"Dt\": \"" + Convert.ToDateTime(h["V_DATE"]).ToString("dd/MM/yyyy") + "\"}," + Environment.NewLine);

                // ---------------------------------------------------------
                // SellerDtls
                // ---------------------------------------------------------

                builder.Append("\"SellerDtls\": {" + Environment.NewLine);
                builder.Append("\"Gstin\": \"" + c.SellerGSTIN.Trim() + "\"," + Environment.NewLine);

                // Name
                string error = CheckData(compName);
                if (error != null)
                    return new EInvoiceJsonResult { success = false, message = error };

                builder.Append("\"LglNm\": \"" + compName + "\"," + Environment.NewLine);
                builder.Append("\"TrdNm\": \"" + compName + "\"," + Environment.NewLine);

                //Add1
                if (string.IsNullOrWhiteSpace(compAdd1))
                    return new EInvoiceJsonResult { success = false, message = "Seller Address Line-1 missing, Please fill it." };

                error = CheckData(compAdd1);
                if (error != null)
                    return new EInvoiceJsonResult { success = false, message = error };

                builder.Append("\"Addr1\": \"" + compAdd1 + "\"," + Environment.NewLine);

                //Add2
                if (string.IsNullOrWhiteSpace(compAdd2))
                    return new EInvoiceJsonResult { success = false, message = "Seller Address Line-2 missing, Please fill it." };

                error = CheckData(compAdd2);
                if (error != null)
                    return new EInvoiceJsonResult { success = false, message = error };

                builder.Append("\"Addr2\": \"" + compAdd2 + "\"," + Environment.NewLine);

                //Other
                builder.Append("\"Loc\": \"" + c.SellerLoc + "\"," + Environment.NewLine);
                builder.Append("\"Pin\": " + c.SellerPIN + "," + Environment.NewLine);
                builder.Append("\"Stcd\": \"" + c.SellerSTCD + "\"," + Environment.NewLine);
                builder.Append("\"Ph\": \"" + c.SellerMob + "\"," + Environment.NewLine);
                builder.Append("\"Em\": null" + Environment.NewLine);
                builder.Append("}," + Environment.NewLine);

                // ---------------------------------------------------------
                // BuyerDtls
                // ---------------------------------------------------------

                builder.Append("\"BuyerDtls\": {" + Environment.NewLine);
                builder.Append("\"Gstin\": \"" + billGst + "\"," + Environment.NewLine);

                //Name
                string buyerName;

                if (vType == "DNSL" || vType == "CRSL")
                {
                    buyerName = Convert.ToString(h["BILL_NAME1"]);
                }
                else if (billPin == "999999" && (supplyType == "EXPWOP" || supplyType == "EXPWP"))
                {
                    if (vType == "ESAG")
                        buyerName = Convert.ToString(h["BILL_NAME"]);
                    else
                        buyerName = Convert.ToString(h["EXPBILLNAME"]);
                }
                else
                {
                    buyerName = Convert.ToString(h["BILL_NAME"]);
                }

                error = CheckData(buyerName);
                if (error != null)
                    return new EInvoiceJsonResult { success = false, message = error };

                builder.Append("\"LglNm\": \"" + buyerName + "\"," + Environment.NewLine);
                builder.Append("\"TrdNm\": \"" + buyerName + "\"," + Environment.NewLine);

                //POS
                string pos;

                if (billPin == "999999")
                    pos = "96";
                else
                    pos = await GetGstCode(h["BILL_CITY"], gv);

                if (!int.TryParse(pos, out int posNo) || posNo <= 0 || pos.Length < 1 || pos.Length > 2)
                {
                    return new EInvoiceJsonResult { success = false, message = "Invalid POS." };
                }

                builder.Append("\"POS\": \"" + pos + "\"," + Environment.NewLine);

                //Address
                string billAdd1 = Convert.ToString(h["BILL_ADD1"]);
                string billAdd2 = Convert.ToString(h["BILL_ADD2"]);
                string billAdd3 = Convert.ToString(h["BILL_ADD3"]);

                //Address Line-1
                if (billAdd1.Trim() == "")
                {
                    return new EInvoiceJsonResult { success = false, message = "Buyer Address Line-1 missing, Please fill it." };
                }

                error = CheckData(billAdd1);
                if (error != null)
                    return new EInvoiceJsonResult { success = false, message = error };

                builder.Append("\"Addr1\": \"" + billAdd1 + "\"," + Environment.NewLine);

                //Address Line-2 and Line-3
                if (billAdd2.Trim() == "" && billAdd3.Trim() == "")
                {
                    return new EInvoiceJsonResult { success = false, message = "Buyer Address Line-2 missing, Please fill it." };
                }

                error = CheckData(billAdd2);
                if (error != null)
                    return new EInvoiceJsonResult { success = false, message = error };

                error = CheckData(billAdd3);
                if (error != null)
                    return new EInvoiceJsonResult { success = false, message = error };

                builder.Append("\"Addr2\": \"" + billAdd2 + " " + billAdd3 + "\"," + Environment.NewLine);

                //Other
                builder.Append("\"Loc\": \"" + Convert.ToString(h["City"]) + "\"," + Environment.NewLine);
                builder.Append("\"Pin\": " + ToInt(billPin) + "," + Environment.NewLine);

                if (billPin == "999999")
                {
                    builder.Append("\"Stcd\": \"96\"," + Environment.NewLine);
                }
                else
                {
                    string stCode = await GetGstCode(h["BILL_CITY"], gv);
                    builder.Append("\"Stcd\": \"" + stCode.Substring(0, 2) + "\"," + Environment.NewLine);
                }

                builder.Append("\"Ph\": null," + Environment.NewLine);
                builder.Append("\"Em\": null" + Environment.NewLine);
                builder.Append("}," + Environment.NewLine);

                // ---------------------------------------------------------
                // ShipDtls / DispDtls
                // ---------------------------------------------------------

                string tranType = Convert.ToString(h["TRAN_TYPE"]);

                if (tranType == "Regular")
                {
                }
                else if (tranType == "Bill To - Ship To")
                {
                    builder.Append("\"ShipDtls\": {" + Environment.NewLine);
                    builder.Append("\"Gstin\": \"" + shipGst + "\"," + Environment.NewLine);

                    //Name
                    string shipName;

                    if (shipPin == "999999" && (supplyType == "EXPWOP" || supplyType == "EXPWP"))
                    {
                        if (vType == "ESAG")
                            shipName = Convert.ToString(h["SHIP_NAME"]);
                        else
                            shipName = Convert.ToString(h["EXPSHIPNAME"]);
                    }
                    else
                    {
                        shipName = Convert.ToString(h["SHIP_NAME"]);
                    }

                    error = CheckData(shipName);
                    if (error != null)
                        return new EInvoiceJsonResult { success = false, message = error };

                    builder.Append("\"LglNm\": \"" + shipName + "\"," + Environment.NewLine);
                    builder.Append("\"TrdNm\": \"" + shipName + "\"," + Environment.NewLine);

                    //Address
                    string shipAdd1 = Convert.ToString(h["SHIP_ADD1"]);
                    string shipAdd2 = Convert.ToString(h["SHIP_ADD2"]);
                    string shipAdd3 = Convert.ToString(h["SHIP_ADD3"]);

                    //Address Line-1
                    if (shipAdd1.Trim() == "")
                    {
                        return new EInvoiceJsonResult { success = false, message = "Shipping Address Line-1 missing, Please fill it." };
                    }

                    error = CheckData(shipAdd1);
                    if (error != null)
                        return new EInvoiceJsonResult { success = false, message = error };

                    builder.Append("\"Addr1\": \"C/o " + Convert.ToString(h["SHIP_NAME"]) + ", " + shipAdd1 + "\"," + Environment.NewLine);

                    //Address Line-2 and Line-3 
                    if (shipAdd2.Trim() == "" && shipAdd3.Trim() == "")
                    {
                        return new EInvoiceJsonResult { success = false, message = "Shipping Address Line-2 missing, Please fill it." };
                    }

                    error = CheckData(shipAdd2);
                    if (error != null)
                        return new EInvoiceJsonResult { success = false, message = error };

                    error = CheckData(shipAdd3);
                    if (error != null)
                        return new EInvoiceJsonResult { success = false, message = error };

                    builder.Append("\"Addr2\": \"" + shipAdd2 + " " + shipAdd3 + " GSTIN:" + shipGst + "\"," + Environment.NewLine);

                    //Other
                    builder.Append("\"Loc\": \"" + Convert.ToString(h["ShipCity"]) + "\"," + Environment.NewLine);
                    builder.Append("\"Pin\": " + ToInt(shipPin) + "," + Environment.NewLine);

                    if (shipPin == "999999")
                    {
                        builder.Append("\"Stcd\": \"96\"" + Environment.NewLine);
                    }
                    else
                    {
                        string stCode = await GetGstCode(h["SHIP_CITY"], gv);
                        builder.Append("\"Stcd\": \"" + stCode.Substring(0, 2) + "\"" + Environment.NewLine);
                    }

                    builder.Append("}," + Environment.NewLine);
                }
                else if (tranType == "Bill From")
                {
                    if (vType == "DNSL" || vType == "CRSL")
                    {
                        builder.Append("\"DispDtls\": {" + Environment.NewLine);

                        error = CheckData(compName);
                        if (error != null)
                            return new EInvoiceJsonResult { success = false, message = error };

                        builder.Append("\"Nm\": \"" + gv.CompanyName + "\"," + Environment.NewLine);

                        error = CheckData(compAdd1);
                        if (error != null)
                            return new EInvoiceJsonResult { success = false, message = error };

                        builder.Append("\"Addr1\": \"" + gv.Address1 + "\"," + Environment.NewLine);

                        error = CheckData(compAdd2);
                        if (error != null)
                            return new EInvoiceJsonResult { success = false, message = error };

                        builder.Append("\"Addr2\": \"" + gv.Address2 + "\"," + Environment.NewLine);

                        builder.Append("\"Loc\": \"" + c.SellerLoc + "\"," + Environment.NewLine);
                        builder.Append("\"Pin\": " + c.SellerPIN + "," + Environment.NewLine);
                        builder.Append("\"Stcd\": \"" + c.SellerSTCD + "\"" + Environment.NewLine);
                        builder.Append("}," + Environment.NewLine);
                    }
                    else
                    {
                        builder.Append("\"DispDtls\": {" + Environment.NewLine);

                        if (ToInt(dt.Rows[0]["GODOWN_CODE"]) > 1)
                        {
                            string godownSql = @"SELECT * FROM GODOWN_MAST WHERE CODE=@code AND COMP_CODE=@COMP_CODE";

                            using var con = _dbConnection.GetErpConnection();
                            con.Open();
                            using var cmdGodown = new SqlCommand(godownSql, con);
                            cmdGodown.Parameters.AddWithValue("@CODE", ToInt(dt.Rows[0]["GODOWN_CODE"]));
                            cmdGodown.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                            using var daGodown = new SqlDataAdapter(cmdGodown);
                            var dtGWD = new DataTable();
                            daGodown.Fill(dtGWD);

                            if (dtGWD.Rows.Count > 0)
                            {
                                string value = Convert.ToString(dtGWD.Rows[0]["Comp_Name"]) ?? "";
                                error = CheckData(value);
                                if (error != null)
                                    return new EInvoiceJsonResult { success = false, message = error };
                                builder.Append("\"Nm\": \"" + value + "\"," + Environment.NewLine);

                                value = Convert.ToString(dtGWD.Rows[0]["ADDRESS"]) ?? "";
                                error = CheckData(value);
                                if (error != null)
                                    return new EInvoiceJsonResult { success = false, message = error };
                                builder.Append("\"Addr1\": \"" + value + "\"," + Environment.NewLine);

                                value = Convert.ToString(dtGWD.Rows[0]["ADDRESS2"]) ?? "";
                                error = CheckData(value);
                                if (error != null)
                                    return new EInvoiceJsonResult { success = false, message = error };
                                builder.Append("\"Addr2\": \"" + value + "\"," + Environment.NewLine);

                                value = Convert.ToString(dtGWD.Rows[0]["CITY"]) ?? "";
                                error = CheckData(value);
                                if (error != null)
                                    return new EInvoiceJsonResult { success = false, message = error };
                                builder.Append("\"Loc\": \"" + value + "\"," + Environment.NewLine);

                                value = Convert.ToString(dtGWD.Rows[0]["PINCODE"]) ?? "";
                                error = CheckData(value);
                                if (error != null)
                                    return new EInvoiceJsonResult { success = false, message = error };
                                builder.Append("\"Pin\": " + value + "," + Environment.NewLine);

                                value = Convert.ToString(dtGWD.Rows[0]["STATE_CODE"]) ?? "";
                                error = CheckData(value);
                                if (error != null)
                                    return new EInvoiceJsonResult { success = false, message = error };
                                builder.Append("\"Stcd\": \"" + value + "\"" + Environment.NewLine);
                            }
                        }
                        else
                        {
                            if (CheckData(gv.CompanyName) != null)
                                return new EInvoiceJsonResult { success = false, message = CheckData(gv.CompanyName) };

                            builder.Append("\"Nm\": \"" + gv.CompanyName + "\"," + Environment.NewLine);

                            if (CheckData(gv.Address1) != null)
                                return new EInvoiceJsonResult { success = false, message = CheckData(gv.Address1) };

                            builder.Append("\"Addr1\": \"" + gv.Address1 + "\"," + Environment.NewLine);

                            if (CheckData(gv.Address2) != null)
                                return new EInvoiceJsonResult { success = false, message = CheckData(gv.Address2) };

                            builder.Append("\"Addr2\": \"" + gv.Address2 + "\"," + Environment.NewLine);

                            builder.Append("\"Loc\": \"" + c.SellerLoc + "\"," + Environment.NewLine);
                            builder.Append("\"Pin\": " + c.SellerPIN + "," + Environment.NewLine);
                            builder.Append("\"Stcd\": \"" + c.SellerSTCD + "\"" + Environment.NewLine);
                        }

                        builder.Append("}," + Environment.NewLine);
                    }
                }
                else
                {
                    if (vType == "DNSL" || vType == "CRSL")
                    {
                        builder.Append("\"DispDtls\": {" + Environment.NewLine);

                        error = CheckData(gv.CompanyName);
                        if (error != null)
                            return new EInvoiceJsonResult { success = false, message = error };
                        builder.Append("\"Nm\": \"" + gv.CompanyName + "\"," + Environment.NewLine);

                        error = CheckData(gv.Address1);
                        if (error != null)
                            return new EInvoiceJsonResult { success = false, message = error };
                        builder.Append("\"Addr1\": \"" + gv.Address1 + "\"," + Environment.NewLine);

                        error = CheckData(gv.Address2);
                        if (error != null)
                            return new EInvoiceJsonResult { success = false, message = error };
                        builder.Append("\"Addr2\": \"" + gv.Address2 + "\"," + Environment.NewLine);

                        builder.Append("\"Loc\": \"" + c.SellerLoc + "\"," + Environment.NewLine);
                        builder.Append("\"Pin\": " + c.SellerPIN + "," + Environment.NewLine);
                        builder.Append("\"Stcd\": \"" + c.SellerSTCD + "\"" + Environment.NewLine);
                        builder.Append("}," + Environment.NewLine);
                    }
                    else
                    {
                        builder.Append("\"DispDtls\": {" + Environment.NewLine);

                        if (ToInt(dt.Rows[0]["GODOWN_CODE"]) > 1)
                        {
                            using var con = _dbConnection.GetErpConnection();
                            con.Open();
                            string godownSql = @"SELECT * FROM GODOWN_MAST WHERE CODE=@CODE AND COMP_CODE=@COMP_CODE";

                            using var cmdGodown = new SqlCommand(godownSql, con);
                            cmdGodown.Parameters.AddWithValue("@CODE", ToInt(dt.Rows[0]["GODOWN_CODE"]));
                            cmdGodown.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                            using var daGodown = new SqlDataAdapter(cmdGodown);
                            var dtGWD = new DataTable();
                            daGodown.Fill(dtGWD);

                            if (dtGWD.Rows.Count > 0)
                            {
                                string value = Convert.ToString(dtGWD.Rows[0]["Comp_Name"]) ?? "";
                                error = CheckData(value);
                                if (error != null)
                                    return new EInvoiceJsonResult { success = false, message = error };
                                builder.Append("\"Nm\": \"" + value + "\"," + Environment.NewLine);

                                value = Convert.ToString(dtGWD.Rows[0]["ADDRESS"]) ?? "";
                                error = CheckData(value);
                                if (error != null)
                                    return new EInvoiceJsonResult { success = false, message = error };
                                builder.Append("\"Addr1\": \"" + value + "\"," + Environment.NewLine);

                                value = Convert.ToString(dtGWD.Rows[0]["ADDRESS2"]) ?? "";
                                error = CheckData(value);
                                if (error != null)
                                    return new EInvoiceJsonResult { success = false, message = error };
                                builder.Append("\"Addr2\": \"" + value + "\"," + Environment.NewLine);

                                value = Convert.ToString(dtGWD.Rows[0]["CITY"]) ?? "";
                                error = CheckData(value);
                                if (error != null)
                                    return new EInvoiceJsonResult { success = false, message = error };
                                builder.Append("\"Loc\": \"" + value + "\"," + Environment.NewLine);

                                value = Convert.ToString(dtGWD.Rows[0]["PINCODE"]) ?? "";
                                error = CheckData(value);
                                if (error != null)
                                    return new EInvoiceJsonResult { success = false, message = error };
                                builder.Append("\"Pin\": " + value + "," + Environment.NewLine);

                                value = Convert.ToString(dtGWD.Rows[0]["STATE_CODE"]) ?? "";
                                error = CheckData(value);
                                if (error != null)
                                    return new EInvoiceJsonResult { success = false, message = error };
                                builder.Append("\"Stcd\": \"" + value + "\"" + Environment.NewLine);
                            }
                        }
                        else
                        {
                            error = CheckData(gv.CompanyName);
                            if (error != null)
                                return new EInvoiceJsonResult { success = false, message = error };
                            builder.Append("\"Nm\": \"" + gv.CompanyName + "\"," + Environment.NewLine);

                            error = CheckData(gv.Address1);
                            if (error != null)
                                return new EInvoiceJsonResult { success = false, message = error };
                            builder.Append("\"Addr1\": \"" + gv.Address1 + "\"," + Environment.NewLine);

                            error = CheckData(gv.Address1);
                            if (error != null)
                                return new EInvoiceJsonResult { success = false, message = error };
                            builder.Append("\"Addr2\": \"" + gv.Address2 + "\"," + Environment.NewLine);

                            builder.Append("\"Loc\": \"" + c.SellerLoc + "\"," + Environment.NewLine);
                            builder.Append("\"Pin\": " + c.SellerPIN + "," + Environment.NewLine);
                            builder.Append("\"Stcd\": \"" + c.SellerSTCD + "\"" + Environment.NewLine);
                        }

                        builder.Append("}," + Environment.NewLine);
                    }

                    // ShipDtls
                    builder.Append("\"ShipDtls\": {" + Environment.NewLine);
                    builder.Append("\"Gstin\": \"" + shipGst + "\"," + Environment.NewLine);

                    string shipName = "";

                    if (shipPin == "999999" && (supplyType == "EXPWOP" || supplyType == "EXPWP"))
                    {
                        if (vType == "ESAG")
                        {
                            shipName = Convert.ToString(dt.Rows[0]["SHIP_NAME"]) ?? "";

                            error = CheckData(shipName);
                            if (error != null)
                                return new EInvoiceJsonResult { success = false, message = error };
                            builder.Append("\"LglNm\": \"" + shipName + "\"," + Environment.NewLine);

                            error = CheckData(shipName);
                            if (error != null)
                                return new EInvoiceJsonResult { success = false, message = error };
                            builder.Append("\"TrdNm\": \"" + shipName + "\"," + Environment.NewLine);
                        }
                        else
                        {
                            shipName = Convert.ToString(dt.Rows[0]["EXPSHIPNAME"]) ?? "";

                            error = CheckData(shipName);
                            if (error != null)
                                return new EInvoiceJsonResult { success = false, message = error };
                            builder.Append("\"LglNm\": \"" + shipName + "\"," + Environment.NewLine);

                            error = CheckData(shipName);
                            if (error != null)
                                return new EInvoiceJsonResult { success = false, message = error };
                            builder.Append("\"TrdNm\": \"" + shipName + "\"," + Environment.NewLine);
                        }
                    }
                    else
                    {
                        shipName = Convert.ToString(dt.Rows[0]["SHIP_NAME"]) ?? "";

                        error = CheckData(shipName);
                        if (error != null)
                            return new EInvoiceJsonResult { success = false, message = error };
                        builder.Append("\"LglNm\": \"" + shipName + "\"," + Environment.NewLine);

                        error = CheckData(shipName);
                        if (error != null)
                            return new EInvoiceJsonResult { success = false, message = error };
                        builder.Append("\"TrdNm\": \"" + shipName + "\"," + Environment.NewLine);
                    }

                    string shipAdd1 = Convert.ToString(dt.Rows[0]["SHIP_ADD1"]) ?? "";
                    if (shipAdd1.Trim() == "")
                    {
                        return new EInvoiceJsonResult { success = false, message = "Shipping Address Line-1 missing, Please fill it." };
                    }
                    else
                    {
                        error = CheckData(shipAdd1);
                        if (error != null)
                            return new EInvoiceJsonResult { success = false, message = error };

                        builder.Append("\"Addr1\": \"" + shipAdd1 + "\"," + Environment.NewLine);
                    }

                    string shipAdd2 = Convert.ToString(dt.Rows[0]["SHIP_ADD2"]) ?? "";
                    string shipAdd3 = Convert.ToString(dt.Rows[0]["SHIP_ADD3"]) ?? "";

                    if (shipAdd2.Trim() == "" && shipAdd3.Trim() == "")
                    {
                        return new EInvoiceJsonResult { success = false, message = "Shipping Address Line-2 missing, Please fill it." };
                    }
                    else
                    {
                        error = CheckData(shipAdd2);
                        if (error != null)
                            return new EInvoiceJsonResult { success = false, message = error };

                        error = CheckData(shipAdd3);
                        if (error != null)
                            return new EInvoiceJsonResult { success = false, message = error };

                        builder.Append("\"Addr2\": \"" + shipAdd2 + " " + shipAdd3 + "\"," + Environment.NewLine);
                    }

                    builder.Append("\"Loc\": \"" + Convert.ToString(dt.Rows[0]["ShipCity"]) + "\"," + Environment.NewLine);
                    builder.Append("\"Pin\": " + ToInt(dt.Rows[0]["SHIP_PINCODE"]) + "," + Environment.NewLine);

                    if (Convert.ToString(dt.Rows[0]["BILL_PINCODE"]) == "999999")
                    {
                        builder.Append("\"Stcd\": \"96\"" + Environment.NewLine);
                    }
                    else
                    {
                        builder.Append("\"Stcd\": \"" + shipGst.Substring(0, 2) + "\"" + Environment.NewLine);
                    }

                    builder.Append("}," + Environment.NewLine);
                }

                // ---------------------------------------------------------
                // ItemList
                // ---------------------------------------------------------

                builder.Append("\"ItemList\": [" + Environment.NewLine);

                DataTable dt1 = new();

                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    string sql;

                    if (vType == "SAGT" || vType == "SASI" || vType == "SAST" || vType == "SART" || vType == "ESAG" || vType == "SATD")
                    {
                        sql = @"SELECT * FROM Sale2 a
                        LEFT JOIN Item_mast b ON a.Item_code=b.code
                        AND a.comp_code=b.comp_code
                        WHERE a.v_type=@v_type
                        AND a.v_no=@v_no
                        AND a.comp_code=@comp_code
                        AND a.branch_code=@branch_code
                        AND a.year_code=@year_code";
                    }
                    else
                    {
                        sql = @"SELECT * FROM DC_Note2 a
                        LEFT JOIN Item_mast b ON a.Item_code=b.code
                        AND a.comp_code=b.comp_code
                        WHERE a.v_type=@v_type
                        AND a.v_no=@v_no
                        AND a.comp_code=@comp_code
                        AND a.branch_code=@branch_code
                        AND a.year_code=@year_code";
                    }

                    using var cmd = new SqlCommand(sql, con);

                    cmd.Parameters.AddWithValue("@v_type", vType);
                    cmd.Parameters.AddWithValue("@v_no", vNo);
                    cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@branch_code", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@year_code", gv.PubFYearCode);

                    using var da = new SqlDataAdapter(cmd);
                    da.Fill(dt1);
                }

                for (int i = 0; i < dt1.Rows.Count; i++)
                {
                    DataRow r = dt1.Rows[i];

                    builder.Append("{" + Environment.NewLine);
                    builder.Append("\"SlNo\": \"" + (i + 1) + "\"," + Environment.NewLine);

                    string itemName;

                    if (gv.PubCompCode.ToString() == "2" || gv.PubCompCode.ToString() == "5")
                    {
                        string itemGroupPrint = await GetScalar(@"SELECT ISNULL(PRINT_NAME,'') FROM ITEM_GROUP WHERE code=@code AND COMP_CODE=@compCode", r["GROUP_CODE"], gv);

                        if (!string.IsNullOrEmpty(itemGroupPrint))
                            itemName = itemGroupPrint;
                        else if (!string.IsNullOrEmpty(Convert.ToString(r["PRINT_NAME"])))
                            itemName = Convert.ToString(r["PRINT_NAME"]);
                        else
                            itemName = Convert.ToString(r["ITEM_NAME"]);
                    }
                    else if (!string.IsNullOrEmpty(Convert.ToString(r["PRINT_NAME"])))
                    {
                        itemName = Convert.ToString(r["PRINT_NAME"]);
                    }
                    else
                    {
                        itemName = Convert.ToString(r["ITEM_NAME"]);
                    }

                    itemName = itemName.Replace("\"", " ");

                    builder.Append("\"PrdDesc\": \"" + itemName + "\"," + Environment.NewLine);

                    bool isService;

                    if (vType == "SAGT" || vType == "SASI" || vType == "SAST" || vType == "SART" || vType == "ESAG" || vType == "SATD")
                    {
                        isService = vType == "SASI" || vType == "SAST";
                    }
                    else
                    {
                        string docType = Convert.ToString(h["DOC_TYPE"]);
                        isService = vType == "SASI" || vType == "SAST" || docType == "SASI" || docType == "SAST";
                    }

                    builder.Append("\"IsServc\": \"" + (isService ? "Y" : "N") + "\"," + Environment.NewLine);
                    builder.Append("\"HsnCd\": \"" + Convert.ToString(r["HSN_CODE"]) + "\"," + Environment.NewLine);
                    builder.Append("\"Barcde\": null," + Environment.NewLine);
                    builder.Append("\"Qty\": " + Math.Round(ToDecimal(r["QTY"]), 3).ToString("0.###", CultureInfo.InvariantCulture) + "," + Environment.NewLine);
                    builder.Append("\"FreeQty\": 0," + Environment.NewLine);

                    string unit = Convert.ToString(r["Unit_Name"]);

                    if (unit.Trim().Length <= 2)
                    {
                        return new EInvoiceJsonResult { success = false, message = "Unit must be 3 to 8 digit long, Please correct it first." };
                    }

                    error = CheckData(unit);
                    if (error != null)
                        return new EInvoiceJsonResult { success = false, message = error };

                    builder.Append("\"Unit\": \"" + unit + "\"," + Environment.NewLine);
                    builder.Append("\"UnitPrice\": " + ToDecimal(r["Rate"]).ToString("0.000") + "," + Environment.NewLine);

                    decimal amount = ToDecimal(r["Amount"]);
                    decimal packAmt = ToDecimal(r["Pack_Amt"]);
                    decimal discAmt = ToDecimal(r["Disc_Amt"]);

                    builder.Append("\"TotAmt\": " + (amount + packAmt).ToString("0.00") + "," + Environment.NewLine);
                    builder.Append("\"Discount\": " + discAmt.ToString("0.00") + "," + Environment.NewLine);
                    builder.Append("\"PreTaxVal\": 0," + Environment.NewLine);
                    builder.Append("\"AssAmt\": " + (amount + packAmt - discAmt).ToString("0.00") + "," + Environment.NewLine);

                    decimal cgstPer = ToDecimal(r["CGST_PER"]);
                    decimal sgstPer = ToDecimal(r["SGST_PER"]);
                    decimal igstPer = ToDecimal(r["IGST_PER"]);

                    decimal taxRate;

                    if ((billPin == "999999" || supplyType == "EXPWOP") && cgstPer + sgstPer + igstPer == 0)
                    {
                        taxRate = await GetDecimalScalar(@"SELECT IGST_PER FROM ITEM_MAST WHERE Code=@code AND Comp_code=@compCode", r["Item_code"], gv);
                    }
                    else
                    {
                        taxRate = cgstPer + sgstPer + igstPer;
                    }

                    builder.Append("\"GstRt\": " + taxRate.ToString("0.##") + "," + Environment.NewLine);
                    builder.Append("\"IgstAmt\": " + ToDecimal(r["IGST_AMT"]).ToString("0.00") + "," + Environment.NewLine);
                    builder.Append("\"CgstAmt\": " + ToDecimal(r["CGST_AMT"]).ToString("0.00") + "," + Environment.NewLine);
                    builder.Append("\"SgstAmt\": " + ToDecimal(r["SGST_AMT"]).ToString("0.00") + "," + Environment.NewLine);

                    if (cessNAValue)
                    {
                        builder.Append("\"CesRt\": 0," + Environment.NewLine);
                        builder.Append("\"CesAmt\": 0," + Environment.NewLine);
                        builder.Append("\"CesNonAdvlAmt\": " + ToDecimal(r["CESS_AMT"]).ToString("0.00") + "," + Environment.NewLine);
                    }
                    else
                    {
                        builder.Append("\"CesRt\": " + ToDecimal(r["CESS_PER"]).ToString("0.##") + "," + Environment.NewLine);
                        builder.Append("\"CesAmt\": " + ToDecimal(r["CESS_AMT"]).ToString("0.00") + "," + Environment.NewLine);
                        builder.Append("\"CesNonAdvlAmt\": 0," + Environment.NewLine);
                    }

                    builder.Append("\"StateCesRt\": 0," + Environment.NewLine);
                    builder.Append("\"StateCesAmt\": 0," + Environment.NewLine);
                    builder.Append("\"StateCesNonAdvlAmt\": 0," + Environment.NewLine);
                    builder.Append("\"OthChrg\": 0," + Environment.NewLine);

                    decimal totItemAmt = amount + ToDecimal(r["IGST_AMT"]) + ToDecimal(r["CGST_AMT"]) + ToDecimal(r["SGST_AMT"]) + ToDecimal(r["CESS_AMT"]) + packAmt - discAmt;

                    builder.Append("\"TotItemVal\": " + totItemAmt.ToString("0.00") + "," + Environment.NewLine);
                    builder.Append("\"OrdLineRef\": \"240622722\"," + Environment.NewLine);
                    builder.Append("\"OrgCntry\": \"IN\"," + Environment.NewLine);
                    builder.Append("\"PrdSlNo\": \"" + Convert.ToString(r["ITEM_CODE"]) + Convert.ToString(r["SNO"]) + "\"," + Environment.NewLine);

                    builder.Append("\"BchDtls\": {" + Environment.NewLine);
                    builder.Append("\"Nm\": \"000\"," + Environment.NewLine);
                    builder.Append("\"ExpDt\": null," + Environment.NewLine);
                    builder.Append("\"WrDt\": null" + Environment.NewLine);
                    builder.Append("}," + Environment.NewLine);

                    builder.Append("\"AttribDtls\": [{" + Environment.NewLine);
                    builder.Append("\"Nm\": null," + Environment.NewLine);
                    builder.Append("\"Val\": null" + Environment.NewLine);
                    builder.Append("}]" + Environment.NewLine);

                    builder.Append("}");

                    if (i < dt1.Rows.Count - 1)
                        builder.Append(",");

                    builder.Append("" + Environment.NewLine);
                }

                builder.Append("]," + Environment.NewLine);

                // ---------------------------------------------------------
                // ValDtls
                // ---------------------------------------------------------

                decimal assVal = ToDecimal(h["AMOUNT"]) + ToDecimal(h["PACK_AMT"]) - ToDecimal(h["DISC_AMT"]);

                builder.Append("\"ValDtls\": {" + Environment.NewLine);
                builder.Append("\"AssVal\": " + assVal.ToString("0.00") + "," + Environment.NewLine);
                builder.Append("\"CgstVal\": " + ToDecimal(h["CGST_AMT"]).ToString("0.00") + "," + Environment.NewLine);
                builder.Append("\"SgstVal\": " + ToDecimal(h["SGST_AMT"]).ToString("0.00") + "," + Environment.NewLine);
                builder.Append("\"IgstVal\": " + ToDecimal(h["IGST_AMT"]).ToString("0.00") + "," + Environment.NewLine);
                builder.Append("\"CesVal\": " + ToDecimal(h["CESS_AMT"]).ToString("0.00") + "," + Environment.NewLine);
                builder.Append("\"StCesVal\": 0," + Environment.NewLine);
                builder.Append("\"Discount\": 0," + Environment.NewLine);
                builder.Append("\"OthChrg\": " + ToDecimal(h["TCS_AMT"]).ToString("0.00") + "," + Environment.NewLine);

                if (vType == "SAGT" || vType == "SASI" || vType == "SAST" || vType == "SART" || vType == "ESAG" || vType == "SATD")
                {
                    builder.Append("\"RndOffAmt\": " + ToDecimal(h["ROUND_OFF"]).ToString("0.00") + "," + Environment.NewLine);
                }
                else
                {
                    builder.Append("\"RndOffAmt\": " + ToDecimal(h["ROUNDOFF"]).ToString("0.00") + "," + Environment.NewLine);
                }

                builder.Append("\"TotInvVal\": " + ToDecimal(h["NAMOUNT"]).ToString("0.00") + "," + Environment.NewLine);
                builder.Append("\"TotInvValFc\": 0" + Environment.NewLine);
                builder.Append("}," + Environment.NewLine);

                // ---------------------------------------------------------
                // PayDtls
                // ---------------------------------------------------------

                builder.Append("\"PayDtls\": {" + Environment.NewLine);
                builder.Append("\"Nm\": null," + Environment.NewLine);
                builder.Append("\"AccDet\": null," + Environment.NewLine);
                builder.Append("\"Mode\": null," + Environment.NewLine);
                builder.Append("\"FinInsBr\": null," + Environment.NewLine);
                builder.Append("\"PayTerm\": null," + Environment.NewLine);
                builder.Append("\"PayInstr\": null," + Environment.NewLine);
                builder.Append("\"CrTrn\": null," + Environment.NewLine);
                builder.Append("\"DirDr\": null," + Environment.NewLine);
                builder.Append("\"CrDay\": null," + Environment.NewLine);
                builder.Append("\"PaidAmt\": null," + Environment.NewLine);
                builder.Append("\"PaymtDue\": null" + Environment.NewLine);
                builder.Append("}," + Environment.NewLine);

                // ---------------------------------------------------------
                // RefDtls
                // ---------------------------------------------------------

                string invoiceDate = Convert.ToDateTime(h["V_DATE"]).ToString("dd/MM/yyyy");

                builder.Append("\"RefDtls\": {" + Environment.NewLine);
                builder.Append("\"InvRm\": \"TTT\"," + Environment.NewLine);

                builder.Append("\"DocPerdDtls\": {" + Environment.NewLine);
                builder.Append("\"InvStDt\": \"" + invoiceDate + "\"," + Environment.NewLine);
                builder.Append("\"InvEndDt\": \"" + invoiceDate + "\"" + Environment.NewLine);
                builder.Append("}," + Environment.NewLine);

                builder.Append("\"PrecDocDtls\": [{" + Environment.NewLine);
                builder.Append("\"InvNo\": \"" + docNo + "\"," + Environment.NewLine);
                builder.Append("\"InvDt\": \"" + invoiceDate + "\"," + Environment.NewLine);
                builder.Append("\"OthRefNo\": null" + Environment.NewLine);
                builder.Append("}]," + Environment.NewLine);

                builder.Append("\"ContrDtls\": [{" + Environment.NewLine);
                builder.Append("\"RecAdvRefr\": null," + Environment.NewLine);
                builder.Append("\"RecAdvDt\": null," + Environment.NewLine);
                builder.Append("\"TendRefr\": null," + Environment.NewLine);
                builder.Append("\"ContrRefr\": null," + Environment.NewLine);
                builder.Append("\"ExtRefr\": null," + Environment.NewLine);
                builder.Append("\"ProjRefr\": null," + Environment.NewLine);
                builder.Append("\"PORefr\": null," + Environment.NewLine);
                builder.Append("\"PORefDt\": null" + Environment.NewLine);
                builder.Append("}]" + Environment.NewLine);
                builder.Append("}," + Environment.NewLine);

                // ---------------------------------------------------------
                // AddlDocDtls
                // ---------------------------------------------------------

                builder.Append("\"AddlDocDtls\": [{" + Environment.NewLine);
                builder.Append("\"Url\": null," + Environment.NewLine);
                builder.Append("\"Docs\": null," + Environment.NewLine);
                builder.Append("\"Info\": null" + Environment.NewLine);
                builder.Append("}]," + Environment.NewLine);

                // ---------------------------------------------------------
                // ExpDtls
                // ---------------------------------------------------------

                builder.Append("\"ExpDtls\": {" + Environment.NewLine);

                if (vType == "SAGT" || vType == "SASI" || vType == "SAST" || vType == "SART" || vType == "ESAG" || vType == "SATD")
                {
                    string sbNo = Convert.ToString(h["SB_NO"]);
                    string sbDate = Convert.ToString(h["SB_DATE"]);

                    if (sbNo.Trim() != "")
                        builder.Append("\"ShipBNo\": \"" + sbNo + "\"," + Environment.NewLine);
                    else
                        builder.Append("\"ShipBNo\": null," + Environment.NewLine);

                    if (sbDate.Trim() != "")
                    {
                        builder.Append("\"ShipBDt\": \"" + Convert.ToDateTime(h["SB_DATE"]).ToString("dd/MM/yyyy") + "\"," + Environment.NewLine);
                    }
                    else
                    {
                        builder.Append("\"ShipBDt\": null," + Environment.NewLine);
                    }

                    if (sbDate.Trim() != "")
                        builder.Append("\"Port\": \"" + Convert.ToString(h["PORT_CODE"]) + "\"," + Environment.NewLine);
                    else
                        builder.Append("\"Port\": null," + Environment.NewLine);
                }
                else
                {
                    builder.Append("\"ShipBNo\": null," + Environment.NewLine);
                    builder.Append("\"ShipBDt\": null," + Environment.NewLine);
                    builder.Append("\"Port\": null," + Environment.NewLine);
                }

                builder.Append("\"RefClm\": null," + Environment.NewLine);
                builder.Append("\"ForCur\": null," + Environment.NewLine);
                builder.Append("\"CntCode\": null," + Environment.NewLine);
                builder.Append("\"ExpDuty\": null" + Environment.NewLine);
                builder.Append("}," + Environment.NewLine);

                // ---------------------------------------------------------
                // EwbDtls
                // ---------------------------------------------------------

                builder.Append("\"EwbDtls\": {" + Environment.NewLine);
                builder.Append("\"TransId\": null," + Environment.NewLine);
                builder.Append("\"TransName\": null," + Environment.NewLine);
                builder.Append("\"Distance\": 500," + Environment.NewLine);
                builder.Append("\"TransDocNo\": null," + Environment.NewLine);
                builder.Append("\"TransDocDt\": null," + Environment.NewLine);
                builder.Append("\"VehNo\": null," + Environment.NewLine);
                builder.Append("\"VehType\": null," + Environment.NewLine);
                builder.Append("\"TransMode\": null" + Environment.NewLine);
                builder.Append("}" + Environment.NewLine);

                builder.Append("}");

                string jsonText = builder.ToString();
                return new EInvoiceJsonResult { success = true, jsonText = jsonText };

            }
            catch (Exception ex)
            {
                return new EInvoiceJsonResult { success = false, message = ex.Message };
            }
        }

        private static int ToInt(object value)
        {
            return int.TryParse(Convert.ToString(value), out var result) ? result : 0;
        }
        private static decimal ToDecimal(object value)
        {
            return decimal.TryParse(Convert.ToString(value), out var result) ? result : 0;
        }
        private async Task<string> GetGstCode(object cityCode, dynamic gv)
        {
            return await GetScalar(
                @"SELECT GST_CODE FROM STATE_MAST WHERE CODE=(SELECT TOP 1 STATE_CODE FROM CITY_MAST WHERE CODE=@cityCode)",
                cityCode, gv);
        }
        private async Task<string> GetScalar(string sql, object value, dynamic gv)
        {
            using var con = _dbConnection.GetErpConnection();
            await con.OpenAsync();

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@code", value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cityCode", value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@compCode", gv.PubCompCode);

            return Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
        }
        private static string? CheckData(string data)
        {
            if (!string.IsNullOrEmpty(data) && data.Contains("\""))
                return "Remove Double Quotes from :" + Environment.NewLine + data;

            return null;
        }
        private async Task<decimal> GetDecimalScalar(string sql, object value, dynamic gv)
        {
            string result = await GetScalar(sql, value, gv);
            return decimal.TryParse(result, out var d) ? d : 0;
        }

        //==========================Generate IRN==========================
        private async Task<IrnResult> GenerateIRN(string vType, int vNo, string authToken, string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                    return new IrnResult { Message = "JSON Text Empty." };

                var gv = _globalVariableService.GetGlobalVariables();
                var gs = await _globalVariableService.LoadGeneralSetting();

                // ---------------- API call ----------------
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

                var request = new HttpRequestMessage(HttpMethod.Post,
                    "https://api.mastergst.com/einvoice/type/GENERATE/version/V1_03?email=it%40pashupatigrp.com");

                request.Headers.Add("ip_address", gs.PubEinvIP);
                request.Headers.Add("client_id", gs.PubEinvCID);
                request.Headers.Add("client_secret", gs.PubEinvCSID);
                request.Headers.Add("username", gs.PubEinvUName);
                request.Headers.Add("auth-token", authToken);
                request.Headers.Add("gstin", gs.PubEinvGSTIN);
                request.Headers.Add("auth_access_type", "read");
                request.Content = new StringContent(json.Trim(), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseContent))
                    return new IrnResult { Message = "Signed QR not find in IRN generated. Please regenerate IRN." };

                // ---------------- Parse response ----------------
                JObject res;
                try
                {
                    res = JObject.Parse(responseContent);
                }
                catch
                {
                    return new IrnResult
                    {
                        Message = "Invalid response from API: " + responseContent,
                        SignedJson = responseContent
                    };
                }

                string statusCd = Convert.ToString(res["status_cd"]) ?? "";

                if (statusCd != "1" && !string.Equals(statusCd, "Sucess", StringComparison.OrdinalIgnoreCase))
                {
                    string err = Convert.ToString(res["status_desc"]);
                    if (string.IsNullOrWhiteSpace(err)) err = responseContent;

                    return new IrnResult
                    {
                        Message = "Please correct following error:" + Environment.NewLine + err,
                        SignedJson = responseContent
                    };
                }

                var data = res["data"] as JObject;
                string irn = Convert.ToString(data?["Irn"]) ?? "";
                string signedQr = Convert.ToString(data?["SignedQRCode"]) ?? "";

                if (irn == "" || signedQr == "")
                {
                    return new IrnResult
                    {
                        Message = "Signed QR not find in IRN generated. Please regenerate IRN.",
                        SignedJson = responseContent
                    };
                }

                // ---------------- Save to DB ----------------
                string[] saleTypes = { "SAGT", "SASI", "SAST", "SART", "ESAG", "SATD" };
                string table = saleTypes.Contains(vType) ? "Sale1" : "DC_Note1";

                using var con = _dbConnection.GetErpConnection();
                await con.OpenAsync();
                using var tran = con.BeginTransaction();

                try
                {
                    // remove old QR record
                    using (var cmd = new SqlCommand(@"DELETE FROM QrImage_Path WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE
                                              AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con, tran))
                    {
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // update invoice / note
                    using (var cmd = new SqlCommand($@"UPDATE {table} SET IRN=@IRN, SIGNED_JSON=@SIGNED_JSON, SIGNED_QR=@SIGNED_QR, EINVOICE_FLG=1
                                               WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE
                                               AND YEAR_CODE=@YEAR_CODE", con, tran))
                    {
                        cmd.Parameters.AddWithValue("@IRN", irn);
                        cmd.Parameters.AddWithValue("@SIGNED_JSON", responseContent);
                        cmd.Parameters.AddWithValue("@SIGNED_QR", signedQr);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // insert QR record
                    using (var cmd = new SqlCommand(@"INSERT INTO QrImage_Path (COMP_CODE,BRANCH_CODE,YEAR_CODE,V_TYPE,V_NO,IRN,UUSER,UDATE,AED,WSID,LIP,LID)
                                              VALUES (@COMP_CODE,@BRANCH_CODE,@YEAR_CODE,@V_TYPE,@V_NO,@IRN,@UUSER,
                                              FORMAT(GETDATE(),'yyyy-MM-dd HH:mm'),'A',@WSID,@LIP,@LID)", con, tran))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@IRN", irn);
                        cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    tran.Commit();
                }
                catch (Exception dbEx)
                {
                    try { tran.Rollback(); } catch { }

                    return new IrnResult
                    {
                        Irn = irn,
                        SignedQr = signedQr,
                        SignedJson = responseContent,
                        Message = "IRN " + irn + " was generated at the portal but saving failed: " + dbEx.Message
                    };
                }

                return new IrnResult
                {
                    Success = true,
                    Message = "IRN Generated.",
                    Irn = irn,
                    SignedQr = signedQr,
                    SignedJson = responseContent
                };
            }
            catch (Exception ex)
            {
                return new IrnResult { Message = ex.Message };
            }
        }

        //==========================Create QR Image==========================
        private async Task<IrnResult> CreateQRImage(string vType, int vNo, string signedQr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(signedQr))
                    return new IrnResult { Message = "Signed QR not found. Please regenerate IRN." };

                var gv = _globalVariableService.GetGlobalVariables();

                // Build PNG bytes (ECC level Q, 5 px per module, same as VB)
                byte[] contents;
                using (var qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(signedQr, QRCodeGenerator.ECCLevel.Q))
                {
                    var qrCode = new PngByteQRCode(qrCodeData);
                    contents = qrCode.GetGraphic(5);
                }

                // Save to QrImage_Path
                using var con = _dbConnection.GetErpConnection();
                await con.OpenAsync();

                using var cmd = new SqlCommand(@"UPDATE QrImage_Path SET QR_IMAGE=@QR_IMAGE
                                         WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE
                                         AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con);

                cmd.Parameters.Add("@QR_IMAGE", SqlDbType.VarBinary, -1).Value = contents;
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                int rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    return new IrnResult { Message = "QR record not found in QrImage_Path." };

                return new IrnResult { Success = true, Message = "QR Image Created", SignedQr = signedQr };
            }
            catch (Exception ex)
            {
                return new IrnResult { Message = ex.Message };
            }
        }

        //===========================E-Invoice Button Click===========================
        public async Task<EInvoiceResultModel> EInvoice(int vNo, string vType, bool cessNAValue)
        {
            try
            {
                if (vNo <= 0 || string.IsNullOrWhiteSpace(vType))
                    return new EInvoiceResultModel { status = false, message = "Please select Voucher No. to generate EInvoice." };

                var gv = _globalVariableService.GetGlobalVariables();

                string[] saleTypes = { "SAGT", "SASI", "SAST", "SART", "ESAG", "SATD" };
                string table = saleTypes.Contains(vType) ? "Sale1" : "DC_Note1";

                // 1. Create JSON 
                var jsonResult = await CreateJSON(vType, vNo, cessNAValue);

                if (!jsonResult.success)
                    return new EInvoiceResultModel { status = false, message = jsonResult.message };

                string jsonText = jsonResult.jsonText ?? "";


                try { using (JsonDocument.Parse(jsonText)) { } }
                catch (System.Text.Json.JsonException jex)
                {
                    return new EInvoiceResultModel
                    {
                        status = false,
                        message = "Generated JSON is invalid: " + jex.Message,
                        requestJson = jsonText
                    };
                }

                // 2. Authentication
                var authValue = await GetAuthentication();

                if (!authValue.success)
                    return new EInvoiceResultModel
                    {
                        status = false,
                        message = authValue.message ?? "Authentication failed.",
                        requestJson = jsonText
                    };

                string authToken = authValue.authToken ?? "";

                if (string.IsNullOrWhiteSpace(authToken))
                    return new EInvoiceResultModel
                    {
                        status = false,
                        message = "Authentication token not received.",
                        requestJson = jsonText
                    };

                // 3. Generate IRN
                var irn = await GenerateIRN(vType, vNo, authToken, jsonText);

                if (!irn.Success)
                    return new EInvoiceResultModel
                    {
                        status = false,
                        message = irn.Message,
                        requestJson = jsonText,
                        irnGenerated = irn.Irn != "",
                        irn = irn.Irn,
                        signedJson = irn.SignedJson
                    };

                // 4. Create QR Image
                var qr = await CreateQRImage(vType, vNo, irn.SignedQr);

                if (!qr.Success)
                    return new EInvoiceResultModel
                    {
                        status = false,
                        irnGenerated = true,
                        irn = irn.Irn,
                        requestJson = jsonText,
                        signedJson = irn.SignedJson,
                        message = "IRN generated, but QR image failed: " + qr.Message
                    };

                // 5. Re-check IRN in DB 
                string savedIrn;
                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using var cmd = new SqlCommand($@"SELECT ISNULL(IRN,'') FROM {table}
                                              WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE
                                              AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    savedIrn = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
                }


                // 6. Response for JS
                return new EInvoiceResultModel
                {
                    status = true,
                    message = "E-Invoice generated successfully.",
                    irn = irn.Irn,
                    requestJson = jsonText,
                    signedJson = irn.SignedJson,
                    einvoiceGenerated = savedIrn != "",
                    authenticated = authValue.success,
                    qrGenerated = true
                };
            }
            catch (Exception ex)
            {
                return new EInvoiceResultModel { status = false, message = ex.Message };
            }
        }

        //===========================Generate IRN Button Click===========================
        public async Task<IrnResult> GenerateIRNFromJson(int vNo, string vType, string jsonText)
        {
            try
            {
                if (vNo <= 0 || string.IsNullOrWhiteSpace(vType))
                    return new IrnResult { Success = false, Message = "Please select Voucher No. to generate EInvoice." };

                if (string.IsNullOrWhiteSpace(jsonText))
                    return new IrnResult { Success = false, Message = "JSON Text Empty." };

                var gv = _globalVariableService.GetGlobalVariables();

                string[] saleTypes = { "SAGT", "SASI", "SAST", "SART", "ESAG", "SATD" };
                string table = saleTypes.Contains(vType) ? "Sale1" : "DC_Note1";

                // 1. JSON Validation
                JObject req;
                try
                {
                    req = JObject.Parse(jsonText);
                }
                catch (JsonReaderException jex)
                {
                    return new IrnResult { Success = false, Message = "JSON is invalid: " + jex.Message };
                }

                string docNoInJson = Convert.ToString(req["DocDtls"]?["No"]) ?? "";
                if (!string.Equals(docNoInJson, vType + vNo, StringComparison.OrdinalIgnoreCase))
                    return new IrnResult
                    {
                        Success = false,
                        Message = "JSON Doc No (" + docNoInJson + ") does not match selected voucher (" + vType + vNo + ")."
                    };

                // 3. Authentication
                var authValue = await GetAuthentication();

                if (!authValue.success)
                    return new IrnResult
                    {
                        Success = false,
                        Message = authValue.message ?? "Authentication failed.",
                    };

                string authToken = authValue.authToken ?? "";

                if (string.IsNullOrWhiteSpace(authToken))
                    return new IrnResult
                    {
                        Success = false,
                        Message = "Authentication token not received.",
                    };

                // 4. Generate IRN 
                var irn = await GenerateIRN(vType, vNo, authToken, jsonText);

                if (!irn.Success)
                    return new IrnResult
                    {
                        Success = false,
                        Message = irn.Message,
                        IrnGenerated = irn.Irn != "",
                        Irn = irn.Irn,
                        SignedJson = irn.SignedJson,
                        Authenticated = authValue.success
                    };

                // 5. Create QR Image
                var qr = await CreateQRImage(vType, vNo, irn.SignedQr);

                if (!qr.Success)
                    return new IrnResult
                    {
                        Success = false,
                        IrnGenerated = true,
                        Irn = irn.Irn,
                        SignedJson = irn.SignedJson,
                        Message = "IRN generated, but QR image failed: " + qr.Message,
                        Authenticated = authValue.success,
                        QrGenerated = false
                    };

                // 6. Re-check IRN 
                string savedIrn;
                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using var cmd = new SqlCommand($@"SELECT ISNULL(IRN,'') FROM {table}
                                              WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE
                                              AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    savedIrn = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
                }

                return new IrnResult
                {
                    Success = true,
                    Message = "IRN Generated.",
                    Irn = irn.Irn,
                    SignedJson = irn.SignedJson,
                    EinvoiceGenerated = savedIrn != "",
                    Authenticated = authValue.success,
                    QrGenerated = true
                };
            }
            catch (Exception ex)
            {
                return new IrnResult { Success = false, Message = ex.Message };
            }
        }

        //===========================Create JSON only===========================
        public async Task<RepositoryResponseData<string>> CreateJsonOnly(int vNo, string vType, bool cessNAValue)
        {
            try
            {
                if (vNo <= 0 || string.IsNullOrWhiteSpace(vType))
                    return new RepositoryResponseData<string> { status = false, message = "Voucher No. not selected." };

                var gv = _globalVariableService.GetGlobalVariables();

                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using var cmd = new SqlCommand(@"SELECT TOP 1 1 FROM Sale2
                                                    WHERE Tax_Code IN (SELECT code FROM TAX_MAST WHERE TAX_TYPE='GST' AND T_TYPE NOT IN ('Import'))
                                                    AND CGST_AMT+SGST_AMT+IGST_AMT=0
                                                    AND V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE
                                                    AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                    if (await cmd.ExecuteScalarAsync() != null)
                        return new RepositoryResponseData<string> { status = false, message = "ERROR! Please check Tax not calculated in invoice." };
                }

                var jsonResult = await CreateJSON(vType, vNo, cessNAValue);

                if (!jsonResult.success)
                    return new RepositoryResponseData<string> { status = false, message = jsonResult.message ?? "Unable to create JSON." };


                return new RepositoryResponseData<string> { status = true, message = "JSON Created.", data = jsonResult.jsonText };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<string> { status = false, message = ex.Message };
            }
        }

        //===========================Create QR from Signed JSON===========================
        public async Task<RepositoryResponseData<string>> CreateQrFromSignedJson(int vNo, string vType, string signedJson)
        {
            try
            {
                if (vNo <= 0 || string.IsNullOrWhiteSpace(vType))
                    return new RepositoryResponseData<string> { status = false, message = "Please select Voucher No. for which QR Code need to be generate." };

                if (string.IsNullOrWhiteSpace(signedJson))
                    return new RepositoryResponseData<string> { status = false, message = "Signed JSON is empty." };

                var gv = _globalVariableService.GetGlobalVariables();

                string[] saleTypes = { "SAGT", "SASI", "SAST", "SART", "ESAG", "SATD" };
                string table = saleTypes.Contains(vType) ? "Sale1" : "DC_Note1";

                // 1. Signed JSON to IRN and SignedQRCode
                JObject root;
                try
                {
                    root = JObject.Parse(signedJson);
                }
                catch
                {
                    return new RepositoryResponseData<string> { status = false, message = "Signed JSON is invalid." };
                }

                string statusCd = Convert.ToString(root["status_cd"]) ?? "";
                if (statusCd != "" && statusCd != "1" && !string.Equals(statusCd, "Sucess", StringComparison.OrdinalIgnoreCase))
                {
                    string err = Convert.ToString(root["status_desc"]);
                    if (string.IsNullOrWhiteSpace(err)) err = signedJson;
                    return new RepositoryResponseData<string> { status = false, message = "Error found in Signed JSON :" + Environment.NewLine + err };
                }

                var data = root["data"] as JObject ?? root;
                string irn = Convert.ToString(data["Irn"]) ?? "";
                string signedQr = Convert.ToString(data["SignedQRCode"]) ?? "";

                if (irn == "" || signedQr == "")
                    return new RepositoryResponseData<string> { status = false, message = "Irn / SignedQRCode not found in Signed JSON." };

                // 3. DB update 
                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using var tran = con.BeginTransaction();

                    try
                    {
                        using (var cmd = new SqlCommand($@"UPDATE {table}
                    SET IRN=@IRN, SIGNED_JSON=@SIGNED_JSON, SIGNED_QR=@SIGNED_QR, EINVOICE_FLG=1
                    WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE
                    AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con, tran))
                        {
                            cmd.Parameters.AddWithValue("@IRN", irn);
                            cmd.Parameters.AddWithValue("@SIGNED_JSON", signedJson);
                            cmd.Parameters.AddWithValue("@SIGNED_QR", signedQr);
                            cmd.Parameters.AddWithValue("@V_TYPE", vType);
                            cmd.Parameters.AddWithValue("@V_NO", vNo);
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        using (var cmd = new SqlCommand(@"DELETE FROM QrImage_Path
                    WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE
                    AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con, tran))
                        {
                            cmd.Parameters.AddWithValue("@V_TYPE", vType);
                            cmd.Parameters.AddWithValue("@V_NO", vNo);
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        using (var cmd = new SqlCommand(@"INSERT INTO QrImage_Path
                    (COMP_CODE,BRANCH_CODE,YEAR_CODE,V_TYPE,V_NO,IRN,UUSER,UDATE,AED,WSID,LIP,LID)
                    VALUES (@COMP_CODE,@BRANCH_CODE,@YEAR_CODE,@V_TYPE,@V_NO,@IRN,@UUSER,
                    FORMAT(GETDATE(),'yyyy-MM-dd HH:mm'),'A',@WSID,@LIP,@LID)", con, tran))
                        {
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", vType);
                            cmd.Parameters.AddWithValue("@V_NO", vNo);
                            cmd.Parameters.AddWithValue("@IRN", irn);
                            cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                            cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        tran.Commit();
                    }
                    catch
                    {
                        try { tran.Rollback(); } catch { }
                        throw;
                    }
                }

                // 4. Create QR image banao and save in QR_IMAGE 
                var qr = await CreateQRImage(vType, vNo, signedQr);

                if (!qr.Success)
                    return new RepositoryResponseData<string> { status = false, message = "IRN saved, but QR image failed: " + qr.Message };

                return new RepositoryResponseData<string> { status = true, message = "QR Image Created", data = irn };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<string> { status = false, message = ex.Message };
            }
        }

        //===========================Create E-Waybill JSON================================
        private async Task<RepositoryResponseData<string>> CreateEWBJSON(string vType, int vNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vType) || vNo <= 0)
                    return new() { status = false, message = "Voucher No. not selected." };

                var gv = _globalVariableService.GetGlobalVariables();
                string docNo = vType + vNo;

                DataTable dt = new();
                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using var cmd = new SqlCommand(@"SELECT * FROM Sale1 WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE
                                             AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                    using var da = new SqlDataAdapter(cmd);
                    da.Fill(dt);
                }

                if (dt.Rows.Count == 0)
                    return new() { status = false, message = "Voucher not found." };

                string irnNo = Convert.ToString(dt.Rows[0]["IRN"]) ?? "";
                if (irnNo == "")
                    return new() { status = false, message = "IRN not found of Invoice no=" + docNo + ". Please generate IRN first." };

                StringBuilder builder = new();
                string tptName = "", tptGstin = "";

                if (dt.Rows.Count > 0)
                {
                    DataRow h = dt.Rows[0];

                    builder.Append("{" + Environment.NewLine);
                    builder.Append("\"Irn\": \"" + irnNo + "\"," + Environment.NewLine);

                    if (Convert.ToString(h["V_TYPE"]) == "SAGT")
                    {
                        // ---------Transport detail for EWaybill----------------------
                        if (ToInt(h["Transport_code"]) == 0)
                        {
                            tptGstin = "";
                            if (Convert.ToString(h["Transport_Name"]) != "")
                                tptName = Convert.ToString(h["Transport_Name"]);
                        }
                        else
                        {
                            int partyCode;

                            using (var con = _dbConnection.GetErpConnection())
                            {
                                await con.OpenAsync();
                                using var cmd = new SqlCommand(@"SELECT Party_code, Name FROM TRANSPORT_MAST WHERE COMP_CODE=@COMP_CODE AND CODE=@CODE", con);
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", ToInt(h["Transport_code"]));

                                using var rd = await cmd.ExecuteReaderAsync();
                                if (!await rd.ReadAsync())
                                    return new() { status = false, message = "Transport not found in Invoice no=" + docNo + "." };

                                partyCode = ToInt(rd["Party_code"]);
                                tptName = Convert.ToString(rd["Name"]) ?? "";
                            }

                            tptGstin = await GetScalar(@"SELECT ISNULL(GSTIN,'') FROM Subgroup_mast WHERE Code=@code AND Comp_code=@compCode", partyCode, gv);
                        }

                        if (tptGstin.Trim() != "" && tptGstin.Trim().Length < 15)
                            return new() { status = false, message = "Incorrect Transport GST No." };

                        if (tptName.Trim() == "")
                            return new() { status = false, message = "Please enter Transport Name in Invoice no=" + docNo + "." };

                        string tptDistance = Convert.ToString(h["TPT_DISTANCE"]) ?? "";

                        if (ToDecimal(tptDistance) == 0)
                            return new() { status = false, message = "Please enter distance in Invoice no=" + docNo + "." };

                        string tptGRNo = (Convert.ToString(h["GR_NO"]) ?? "").Trim();
                        string tptGRDate = "";
                        if (Convert.ToString(h["GR_DATE"]) != "")
                            tptGRDate = Convert.ToDateTime(h["GR_DATE"]).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

                        string tptVehicleNo = Convert.ToString(h["VEHICLE_NO"]) ?? "";
                        if (tptVehicleNo.Trim() == "")
                            return new() { status = false, message = "Please enter Vehicel No. in Invoice no=" + docNo + "." };

                        string tptVehType = "R";   // O-ODC, R-Regular

                        string tptTransMode = Convert.ToString(h["TPT_MODE"]) ?? "";
                        if (tptTransMode.Trim() == "")
                            return new() { status = false, message = "Please enter 'Mode of Transport' in Invoice no=" + docNo + "." };

                        // ---------JSON build----------------------
                        if (tptDistance != "")
                            builder.Append("\"Distance\": " + ToDecimal(tptDistance).ToString("0.##", CultureInfo.InvariantCulture) + "," + Environment.NewLine);

                        if (tptTransMode != "")
                            builder.Append("\"TransMode\": \"" + tptTransMode + "\"," + Environment.NewLine);
                        else
                            builder.Append("\"TransMode\": null," + Environment.NewLine);

                        if (tptGstin != "")
                            builder.Append("\"TransId\": \"" + tptGstin + "\"," + Environment.NewLine);
                        else
                            builder.Append("\"TransId\": null," + Environment.NewLine);

                        string error = CheckData(tptName);
                        if (error != null)
                            return new() { status = false, message = error };
                        else if (tptName != "")
                            builder.Append("\"TransName\": \"" + tptName + "\"," + Environment.NewLine);
                        else
                            builder.Append("\"TransName\": null," + Environment.NewLine);

                        if (tptGRDate != "")
                            builder.Append("\"TransDocDt\": \"" + tptGRDate + "\"," + Environment.NewLine);
                        else
                            builder.Append("\"TransDocDt\": null," + Environment.NewLine);

                        if (tptGRNo != "")
                            builder.Append("\"TransDocNo\": \"" + tptGRNo + "\"," + Environment.NewLine);
                        else
                            builder.Append("\"TransDocNo\": null," + Environment.NewLine);

                        if (tptVehicleNo != "")
                            builder.Append("\"VehNo\": \"" + tptVehicleNo + "\"," + Environment.NewLine);
                        else
                            builder.Append("\"VehNo\": null," + Environment.NewLine);

                        if (tptVehType != "")
                            builder.Append("\"VehType\": \"" + tptVehType + "\"" + Environment.NewLine);
                        else
                            builder.Append("\"VehType\": null" + Environment.NewLine);

                        builder.Append("}");
                    }
                }

                return new() { status = true, data = builder.ToString() };
            }
            catch (Exception ex)
            {
                return new() { status = false, message = ex.Message };
            }
        }

        //===========================Create EWB JSON button===========================
        public async Task<RepositoryResponseData<string>> CreateEwbJsonOnly(int vNo, string vType)
        {
            try
            {
                var res = await CreateEWBJSON(vType, vNo);

                if (!res.status)
                    return new RepositoryResponseData<string>
                    {
                        status = false,
                        message = res.message ?? "Unable to create E-Waybill JSON."
                    };

                return new RepositoryResponseData<string> { status = true, data = Convert.ToString(res.data) };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<string> { status = false, message = ex.Message };
            }
        }

        //===========================Generate EWayBill===========================
        private async Task<EwayBillResultModel> GenerateEwayBill(int vNo, string vType, string jsonText, bool useEwbJson)
        {
            if (vNo <= 0 || string.IsNullOrWhiteSpace(vType))
                return new EwayBillResultModel { success = false, message = "Invalid voucher details." };

            if (string.Equals(vType, "SAGT", StringComparison.OrdinalIgnoreCase))
            {

                if (string.IsNullOrWhiteSpace(_authToken))
                    return new EwayBillResultModel { success = false, message = "Please get Authentication first" };

                try
                {
                    var gv = _globalVariableService.GetGlobalVariables();
                    var gs = await _globalVariableService.LoadGeneralSetting();

                    if (!useEwbJson)
                    {
                        var jr = await CreateEWBJSON(vType, vNo);
                        if (!useEwbJson)
                        {
                            var result = await CreateEWBJSON(vType, vNo);

                            if (!result.status)
                                return new EwayBillResultModel
                                {
                                    success = false,
                                    message = result.message ?? "Unable to create E-Waybill JSON."
                                };

                            jsonText = result.data ?? "";
                        }

                        if (string.IsNullOrWhiteSpace(jsonText))
                            return new EwayBillResultModel
                            {
                                success = false,
                                message = "EWaybill JSON is empty."
                            };

                        jsonText = jsonText.Trim();
                    }
                    try { JObject.Parse(jsonText); }
                    catch (JsonReaderException jex)
                    {
                        return new EwayBillResultModel { success = false, message = "EWaybill JSON is invalid: " + jex.Message, requestJson = jsonText };
                    }

                    // ---------------- API call ----------------
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(100) };
                    var request = new HttpRequestMessage(HttpMethod.Post,
                        "https://api.mastergst.com/einvoice/type/GENERATE_EWAYBILL/version/V1_03?email=it%40pashupatigrp.com");

                    request.Headers.Add("ip_address", gs.PubEinvIP);
                    request.Headers.Add("client_id", gs.PubEinvCID);
                    request.Headers.Add("client_secret", gs.PubEinvCSID);
                    request.Headers.Add("username", gs.PubEinvUName);
                    request.Headers.Add("auth-token", _authToken);
                    request.Headers.Add("gstin", gs.PubEinvGSTIN);
                    request.Headers.Add("auth_access_type", "read");
                    request.Content = new StringContent(jsonText, Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(request);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(responseContent))
                        return new EwayBillResultModel { success = false, message = "EWaybill Not Generated.", requestJson = jsonText };

                    // ---------------- Parse response ----------------
                    JObject res;
                    try { res = JObject.Parse(responseContent); }
                    catch
                    {
                        return new EwayBillResultModel
                        {
                            success = false,
                            message = "Invalid response from API: " + responseContent,
                            requestJson = jsonText,
                            signedJson = responseContent
                        };
                    }

                    string statusCd = Convert.ToString(res["status_cd"]) ?? "";

                    if (statusCd != "1" && !string.Equals(statusCd, "Sucess", StringComparison.OrdinalIgnoreCase))
                    {
                        string err = Convert.ToString(res["status_desc"]);
                        if (string.IsNullOrWhiteSpace(err)) err = responseContent;

                        return new EwayBillResultModel
                        {
                            success = false,
                            message = "EWaybill Not Generated:" + Environment.NewLine + err,
                            requestJson = jsonText,
                            signedJson = responseContent
                        };
                    }

                    var data = res["data"] as JObject ?? res;

                    string ewaybillNo = Convert.ToString(data["EwbNo"]) ?? "";
                    DateTime? ewbDate = ParseIrpDate(data["EwbDt"]);
                    DateTime? ewbValidDate = ParseIrpDate(data["EwbValidTill"]);

                    if (ewaybillNo == "")
                        return new EwayBillResultModel
                        {
                            success = false,
                            message = "EWaybill Not Generated.",
                            requestJson = jsonText,
                            signedJson = responseContent
                        };

                    // ---------------- Save to DB ----------------
                    try
                    {
                        using var con = _dbConnection.GetErpConnection();
                        await con.OpenAsync();
                        using var cmd = new SqlCommand(@"UPDATE Sale1 SET EWAYBILL_FLG=1, EWAYBILL_NO=@EWAYBILL_NO, EWAYBILL_DATE=@EWAYBILL_DATE,
                                              EWAYBILL_VALIDDATE=@EWAYBILL_VALIDDATE, EWAYBILL_JSON=@EWAYBILL_JSON
                                              WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE
                                              AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con);

                        cmd.Parameters.AddWithValue("@EWAYBILL_NO", ewaybillNo);
                        cmd.Parameters.AddWithValue("@EWAYBILL_DATE", (object?)ewbDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EWAYBILL_VALIDDATE", (object?)ewbValidDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EWAYBILL_JSON", responseContent);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception dbEx)
                    {
                        return new EwayBillResultModel
                        {
                            success = false,
                            ewbGenerated = true,
                            ewbNo = ewaybillNo,
                            requestJson = jsonText,
                            signedJson = responseContent,
                            message = "EWaybill " + ewaybillNo + " was generated at the portal but saving failed: " + dbEx.Message
                        };
                    }

                    return new EwayBillResultModel
                    {
                        success = true,
                        message = "EWaybill Generated.",
                        ewbNo = ewaybillNo,
                        ewbDate = ewbDate?.ToString("dd/MM/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture) ?? "",
                        validUpto = ewbValidDate?.ToString("dd/MM/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture) ?? "",
                        requestJson = jsonText,
                        signedJson = responseContent
                    };
                }
                catch (TaskCanceledException)
                {
                    return new EwayBillResultModel { success = false, message = "E-WayBill API request timed out." };
                }
                catch (HttpRequestException ex)
                {
                    return new EwayBillResultModel { success = false, message = "Unable to connect to E-WayBill API: " + ex.Message };
                }
                catch (Exception ex)
                {
                    return new EwayBillResultModel { success = false, message = ex.Message };
                }
            }
            return new EwayBillResultModel { success = false, message = "E-Waybill can be generated only for SAGT vouchers." };
        }

        private static DateTime? ParseIrpDate(JToken? t)
        {
            if (t == null || t.Type == JTokenType.Null) return null;
            if (t.Type == JTokenType.Date) return t.Value<DateTime>();

            return DateTime.TryParse(Convert.ToString(t), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
        }

        //===========================Generate EWayBill button===========================
        public async Task<RepositoryResponseData<EwayBillResultModel>> EWayBill(int vNo, string vType, bool useEwbJson, string jsonText, bool cbEwayBill,
            bool takeConfirmation = false)
        {
            if (vNo <= 0 || string.IsNullOrWhiteSpace(vType))
                return new RepositoryResponseData<EwayBillResultModel> { status = false, message = "Invalid voucher details." };

            var gv = _globalVariableService.GetGlobalVariables();
            var gs = await _globalVariableService.LoadGeneralSetting();

            string docNo = vType + vNo;

            try
            {
                decimal nAmount = 0;
                if (cbEwayBill)
                {
                    if (!takeConfirmation)
                    {
                        string qry = $@"Select NAMOUNT from Sale1 where v_type=@v_type and v_no=@v_no and comp_code=@comp_code and branch_code=@branch_code 
                                    and year_code=@year_code";
                        using var con = _dbConnection.GetErpConnection();
                        con.Open();

                        using var cmd = new SqlCommand(qry, con);

                        cmd.Parameters.AddWithValue("@v_type", vType);
                        cmd.Parameters.AddWithValue("@v_no", vNo);
                        cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@branch_code", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@year_code", gv.PubFYearCode);

                        var result = cmd.ExecuteScalar();

                        if (result != null)
                            nAmount = Convert.ToDecimal(result);

                        if (nAmount < gs.PubDefEWaybillAmt)
                            return new RepositoryResponseData<EwayBillResultModel>
                            {
                                status = false,
                                message = $@"Total invoice Amount is Rs. {nAmount} which is less than Rs. {gs.PubDefEWaybillAmt} in 
                                            Invoice no = {docNo}.{Environment.NewLine} Do you want to create EWayBill?",
                                data = new EwayBillResultModel { continueEwaybillQuestion = true }
                            };
                    }

                    // 2. Authentication
                    var authValue = await GetAuthentication();

                    if (!authValue.success)
                        return new()
                        {
                            status = false,
                            message = authValue.message ?? "Authentication failed.",
                            data = new EwayBillResultModel { requestJson = jsonText }
                        };

                    if (string.IsNullOrWhiteSpace(authValue.authToken))
                        return new()
                        {
                            status = false,
                            message = "Authentication token not received.",
                            data = new EwayBillResultModel { requestJson = jsonText }
                        };

                    // 3. Generate E-Way Bill
                    var ewb = await GenerateEwayBill(vNo, vType, jsonText, useEwbJson);

                    return new()
                    {
                        status = ewb.success,
                        message = ewb.message,
                        data = ewb
                    };

                }
                return new() { status = false, message = "Check E-WayBill option!" };
            }
            catch (Exception ex)
            {
                return new() { status = false, message = ex.Message };
            }
        }

        //==============================E-Waybill Authentication==========================
        private async Task<MasterGstAuthResult> GetAuthenticationEWAYBILL()
        {
            try
            {
                var gs = await _globalVariableService.LoadGeneralSetting();

                string url = "https://api.mastergst.com/ewaybillapi/v1.03/authenticate?email=it%40pashupatigrp.com" +
                             "&username=" + Uri.EscapeDataString(gs.PubEinvUName ?? "") +
                             "&password=" + Uri.EscapeDataString(gs.PubEinvPass ?? "");

                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                request.Headers.Add("ip_address", gs.PubEinvIP);
                request.Headers.Add("client_id", gs.PubEWayBillCID);
                request.Headers.Add("client_secret", gs.PubEWayBillCSID);
                request.Headers.Add("gstin", gs.PubEinvGSTIN);
                request.Headers.Add("auth_access_type", "read");

                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                JObject res;
                try { res = JObject.Parse(responseContent); }
                catch
                {
                    return new MasterGstAuthResult
                    {
                        success = false,
                        message = $"HTTP {(int)response.StatusCode}: " + responseContent
                    };
                }

                string statusCd = Convert.ToString(res["status_cd"]) ?? "";

                if (statusCd != "1")
                {
                    string err = Convert.ToString(res["status_desc"]);
                    if (string.IsNullOrWhiteSpace(err)) err = responseContent;
                    return new MasterGstAuthResult { success = false, message = err, response = responseContent };
                }

                return new MasterGstAuthResult { success = true };
            }
            catch (Exception ex)
            {
                return new MasterGstAuthResult { success = false, message = ex.Message };
            }
        }

        //===========================Cancel IRN===========================
        public async Task<RepositoryResponseData<CancelResultModel>> CancelIRN(int vNo, string vType, string irn, bool confirmed)
        {
            RepositoryResponseData<CancelResultModel> Fail(string msg, CancelResultModel? d = null)
                => new() { status = false, message = msg, data = d };

            try
            {
                if (vNo <= 0 || string.IsNullOrWhiteSpace(vType))
                    return Fail("Please select Voucher No. for which IRN to be cancel.");

                var gv = _globalVariableService.GetGlobalVariables();
                var gs = await _globalVariableService.LoadGeneralSetting();

                string[] saleTypes = { "SAGT", "SASI", "SAST", "SART", "ESAG", "SATD" };
                bool isSale = saleTypes.Contains(vType);

                string where = @" WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE";

                // 1. CheckCanCelApproval
                int approvals;
                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using var cmd = new SqlCommand(@"SELECT COUNT(*) FROM EINVOICE_CANAPP WHERE REF_TYPE=@V_TYPE AND REF_NO=@V_NO AND COMP_CODE=@COMP_CODE
                                             AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    approvals = ToInt(await cmd.ExecuteScalarAsync());
                }

                if (approvals <= 0)
                    return Fail("Please obtain the first approval for cancellation. Once the cancellation request is approved, the system should allow the cancellation to proceed.");

                // 2. Cancel IRN confirmation
                if (!confirmed)
                    return Fail("Do you want to cancel IRN of this Invoice ?", new CancelResultModel { needConfirm = true });

                // 3. Authenticate
                var auth = await GetAuthentication();
                if (!auth.success)
                    return Fail(auth.message ?? "Authentication failed.");

                if (string.IsNullOrWhiteSpace(_authToken))
                    return Fail("Authentication token not received.");

                // 4. Invalid IRN check
                string savedIrn;
                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using var cmd = new SqlCommand("SELECT ISNULL(IRN,'') FROM QrImage_Path" + where, con);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    savedIrn = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
                }

                irn = (irn ?? "").Trim();
                if (savedIrn != irn)
                    return Fail("Invalid IRN.");

                // 5. IRP cancel call
                var body = new JObject
                {
                    ["Irn"] = irn,
                    ["CnlRsn"] = "1",
                    ["CnlRem"] = "Wrong entry"
                };

                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                var request = new HttpRequestMessage(HttpMethod.Post,
                    "https://api.mastergst.com/einvoice/type/CANCEL/version/V1_03?email=it%40pashupatigrp.com");

                request.Headers.Add("ip_address", gs.PubEinvIP);
                request.Headers.Add("client_id", gs.PubEinvCID);
                request.Headers.Add("client_secret", gs.PubEinvCSID);
                request.Headers.Add("username", gs.PubEinvUName);
                request.Headers.Add("auth-token", _authToken);
                request.Headers.Add("gstin", gs.PubEinvGSTIN);
                request.Headers.Add("auth_access_type", "read");
                request.Content = new StringContent(body.ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                JObject res;
                try { res = JObject.Parse(responseContent); }
                catch
                {
                    return Fail("Invalid response from API: " + responseContent);
                }

                string statusCd = Convert.ToString(res["status_cd"]) ?? "";

                if (statusCd != "1" && !string.Equals(statusCd, "Sucess", StringComparison.OrdinalIgnoreCase))
                {
                    string err = Convert.ToString(res["status_desc"]);
                    if (string.IsNullOrWhiteSpace(err)) err = responseContent;
                    return Fail(err);
                }

                // 6. DB updates
                try
                {
                    using var con = _dbConnection.GetErpConnection();
                    await con.OpenAsync();
                    using var tran = con.BeginTransaction();

                    async Task Exec(string sql, int? doNo = null)
                    {
                        using var c = new SqlCommand(sql, con, tran);
                        c.Parameters.AddWithValue("@V_TYPE", vType);
                        c.Parameters.AddWithValue("@V_NO", vNo);
                        c.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        c.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        c.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        if (doNo != null) c.Parameters.AddWithValue("@DO_NO", doNo.Value);
                        await c.ExecuteNonQueryAsync();
                    }

                    try
                    {
                        if (isSale)
                        {
                            await Exec("UPDATE Sale1 SET STATUS=2, EINVOICE_FLG=NULL, IRN=NULL, EWAYBILL_NO=NULL" + where);
                            await Exec("UPDATE Sale2 SET STATUS=2" + where);
                            await Exec(@"UPDATE DO1 SET STATUS=2 WHERE REF_TYPE=@V_TYPE AND REF_NO=@V_NO AND COMP_CODE=@COMP_CODE
                                 AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE");

                            object? doObj;
                            using (var c = new SqlCommand(@"SELECT TOP 1 V_NO FROM DO1 WHERE REF_TYPE=@V_TYPE AND REF_NO=@V_NO
                                                    AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con, tran))
                            {
                                c.Parameters.AddWithValue("@V_TYPE", vType);
                                c.Parameters.AddWithValue("@V_NO", vNo);
                                c.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                c.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                c.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                doObj = await c.ExecuteScalarAsync();
                            }

                            if (doObj != null && doObj != DBNull.Value)
                            {
                                await Exec(@"UPDATE DO2 SET STATUS=2 WHERE V_TYPE='DOGT' AND V_NO=@DO_NO AND COMP_CODE=@COMP_CODE
                                     AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", ToInt(doObj));
                            }
                        }
                        else
                        {
                            await Exec("UPDATE DC_Note1 SET STATUS=2, EINVOICE_FLG=NULL, IRN=NULL, EWAYBILL_NO=NULL" + where);
                            await Exec("UPDATE DC_Note2 SET STATUS=2" + where);
                        }

                        await Exec("DELETE FROM LEDGER2" + where);
                        await Exec("DELETE FROM LEDGER_OS" + where);

                        tran.Commit();
                    }
                    catch
                    {
                        try { tran.Rollback(); } catch { }
                        throw;
                    }
                }
                catch (Exception dbEx)
                {
                    return Fail("IRN was cancelled at the portal but saving failed: " + dbEx.Message,
                                new CancelResultModel { irnCancelled = true });
                }

                return new()
                {
                    status = true,
                    message = "Request for IRN Cancellation successfully processed of Invoice No.= " + vType + vNo
                };
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        //===========================Cancel E-Waybill===========================
        public async Task<RepositoryResponseData<CancelResultModel>> CancelEwayBill(int vNo, string vType, string ewbNo)
        {
            RepositoryResponseData<CancelResultModel> Fail(string msg, CancelResultModel? d = null)
                => new() { status = false, message = msg, data = d };

            try
            {
                ewbNo = (ewbNo ?? "").Trim();
                if (!long.TryParse(ewbNo, out long ewbNumber) || ewbNumber <= 0)
                    return Fail("Please enter valid Ewaybill No.");

                var gv = _globalVariableService.GetGlobalVariables();
                var gs = await _globalVariableService.LoadGeneralSetting();

                // 1. Authentication
                var auth = await GetAuthenticationEWAYBILL();
                if (!auth.success)
                    return Fail(auth.message ?? "Authentication failed.");

                // 2. canewb call
                var body = new JObject
                {
                    ["ewbNo"] = ewbNumber,
                    ["cancelRsnCode"] = 2,
                    ["cancelRmrk"] = "Order Cancelled"
                };

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(100) };
                var request = new HttpRequestMessage(HttpMethod.Post,
                    "https://api.mastergst.com/ewaybillapi/v1.03/ewayapi/canewb?email=it%40pashupatigrp.com");

                request.Headers.Add("ip_address", gs.PubEinvIP);
                request.Headers.Add("client_id", gs.PubEWayBillCID);
                request.Headers.Add("client_secret", gs.PubEWayBillCSID);
                request.Headers.Add("username", gs.PubEinvUName);
                request.Headers.TryAddWithoutValidation("auth-token", _authToken ?? "");
                request.Headers.Add("gstin", gs.PubEinvGSTIN);
                request.Headers.Add("auth_access_type", "read");
                request.Content = new StringContent(body.ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                JObject res;
                try { res = JObject.Parse(responseContent); }
                catch
                {
                    return Fail("Invalid response from API: " + responseContent);
                }

                string statusCd = Convert.ToString(res["status_cd"]) ?? "";

                if (statusCd != "1" &&
                    !string.Equals(statusCd, "Sucess", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(statusCd, "Success", StringComparison.OrdinalIgnoreCase))
                {
                    string err = Convert.ToString(res["status_desc"]);
                    if (string.IsNullOrWhiteSpace(err)) err = responseContent;
                    return Fail(err);
                }

                // 3. Update DB
                string[] saleTypes = { "SAGT", "SASI", "SAST", "SART", "ESAG", "SATD" };

                if (saleTypes.Contains(vType))
                {
                    try
                    {
                        using var con = _dbConnection.GetErpConnection();
                        await con.OpenAsync();
                        using var cmd = new SqlCommand(@"UPDATE Sale1 SET EWAYBILL_FLG=0, EWAYBILL_NO=NULL
                                                 WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE
                                                 AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception dbEx)
                    {
                        return Fail("Ewaybill was cancelled at the portal but saving failed: " + dbEx.Message,
                                    new CancelResultModel { ewbCancelled = true });
                    }
                }

                return new()
                {
                    status = true,
                    message = "Request for Ewaybill Cancellation successfully processed of Ewaybill No.= " + ewbNo
                };
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        //===========================Authenticate button===========================
        public async Task<RepositoryResponseData<string>> Authenticate()
        {
            var auth = await GetAuthentication();

            return new()
            {
                status = auth.success,
                message = auth.success ? "Authenticated." : (auth.message ?? "Authentication failed.")
            };
        }
    }
}
