using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Emit;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.Financial_Accounting.Master
{
    public class BusinessPartnerMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public BusinessPartnerMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
        ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/BusinessPartnerMaster/Index.cshtml");
        }
        [HttpGet]
      
        public JsonResult GetNextAccountID()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            int nextCode = 1;
            string compInit = "";
            string fullAccountId = "";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string sql = @"SELECT ISNULL(MAX(CODE), 0) + 1 FROM SUBGROUP_MAST WHERE COMP_CODE = @CompCode";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    object result = cmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out int code))
                        nextCode = code;
                }
            }
            using (SqlConnection con2 = _dbConnection.GetConDbConnection())
            {
                con2.Open();
                string compInitSql = @"SELECT COMP_INIT FROM COMP_MAST WHERE CODE = @CompCode";
                using (SqlCommand cmd2 = new SqlCommand(compInitSql, con2))
                {
                    cmd2.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    object initResult = cmd2.ExecuteScalar();

                    if (initResult != null)
                        compInit = initResult.ToString();
                }
            }
            fullAccountId = $"{compInit}{nextCode}";
            return Json(new
            {
                NextCode = nextCode,
                CompInit = compInit,
                FullAccountID = fullAccountId
            });
        }

        [HttpGet]
        public JsonResult GetddlGroupName()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT CODE, NAME FROM MGROUP_MAST WHERE comp_code = {globalVar.PubCompCode} AND ISNULL(NATURE, '') NOT IN ('CASH', 'BANK', 'OTHERS')  ORDER BY NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetNatureBanding(string code)
        {
            if (string.IsNullOrEmpty(code))
                return Json(new { success = false, message = "Invalid code" });
            string query = $@" Select code, NATURE From MGROUP_MAST where CODE= {code}";
            var GetNature = _dropdownService.GetDropdownList(query);
            return Json(GetNature);
        }
        [HttpGet]
        public JsonResult GetLanguageBanding()
        {
            string query = $@"SELECT CODE, NAME FROM LANGUAGE_MAST Order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetCurrencyBanding()
        {
            string query = $@"select CODE,shortname NAME from CURRENCY_MAST Order by code";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlMainAC()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" select CODE,NAME from subgroup_mast where comp_code = {globalVar.PubCompCode} and Active=1 Order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlAgentName()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" select a.CODE,a.NAME from subgroup_mast a 
            left join mgroup_mast b on a.group_code=b.code and a.comp_code=b.comp_code
            where a.comp_code = {globalVar.PubCompCode} and b.nature='Broker' Order by a.name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlOSGroup()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT CODE, NAME FROM ACOS_MAST where comp_code = {globalVar.PubCompCode} and Active=1 Order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlDistrictGroup()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT CODE, NAME FROM DISC_MAST where comp_code = {globalVar.PubCompCode} group by code,name Order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlPaymentTerms()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT CODE, NAME FROM Payterm_mast where comp_code = {globalVar.PubCompCode} Order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlBankName()
        {
            string query = $@" SELECT CODE, NAME FROM Bank_MAST Where ACTIVE=1  Order by NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlBankCountry()
        {
            string query = $@" SELECT CODE, NAME FROM Country_MAST Where ACTIVE=1  Order by NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlTitle()
        {
            string query = $@" SELECT CODE, NAME FROM CPTITLE_MAST Order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlCity()
        {
            string query = $@" Select a.CODE, a.NAME from CITY_MAST a left join STATE_MAST b on a.STATE_CODE=b.CODE 
            left join COUNTRY_MAST c on a.COUNTRY_CODE=c.CODE where a.ACTIVE=1 and b.ACTIVE=1 and c.ACTIVE=1 Order by a.NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult Getddlstate()
        {
            string query = $@" select a.CODE, a.NAME from STATE_MAST a left join COUNTRY_MAST b on a.COUNTRY_CODE=b.CODE where a.ACTIVE=1 and b.ACTIVE=1  Order by a.NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlCountry()
        {
            string query = $@" SELECT CODE, NAME FROM Country_MAST Where ACTIVE=1  Order by NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlBankNameBD()
        {
            string query = $@" SELECT CODE, NAME FROM Bank_MAST Where ACTIVE=1  Order by NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlBankCountryBD()
        {
            string query = $@" SELECT CODE, NAME FROM Country_MAST Where ACTIVE=1  Order by NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        //add new function to get legal name by gstin start
        public IActionResult GetPaymentTerms(int id)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            int dayInt = 0;
            string sql = @"SELECT DAY_INT FROM Payterm_mast WHERE COMP_CODE = @CompCode AND Code = @Code";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@Code", id);

                    con.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null && int.TryParse(result.ToString(), out int code))
                    {
                        dayInt = code;
                    }
                }
            }
            return Json(new { Days = dayInt });
        }
        public async Task<string> GetLegalNameByGSTIN(string gstno)
        {
            try
            {
                string url = $"https://api.mastergst.com/public/search?email=it%40pashupatigrp.com&gstin={gstno.Trim()}";
                string PubGSTAPICID = "GSP9962544f-10d6-4666-87d0-3d74bf082076";   
                string PubGSTAPICSID = "GSP188864f9-cab2-418f-875d-2f1d76f8b009";  
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("client_id", PubGSTAPICID);
                    client.DefaultRequestHeaders.Add("client_secret", PubGSTAPICSID);
                    client.DefaultRequestHeaders.Add("auth_access_type", "read");

                    var response = await client.GetAsync(url);
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch
            {
                return string.Empty;
            }
        }
        //add new function to get legal name by gstin start
        public IActionResult GetCityID(int id)
        {
            string sqlquery = "Select state_code, Country_code, Zipcode from CITY_MAST where code=@Code";
            CityDetailsModel cityDetails = new CityDetailsModel();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(sqlquery, con))
                {
                    cmd.Parameters.AddWithValue("Code", id);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            cityDetails.StateCode = reader["state_code"].ToString();
                            cityDetails.CountryCode = reader["Country_code"].ToString();
                            cityDetails.ZipCode = reader["Zipcode"].ToString();
                        }
                    }
                }
            }
            return Json(cityDetails); 
        }
        [HttpPost]
        public IActionResult SaveAllData([FromBody] BusinessPartnerWrapper data)
        {
            if (data == null)
                return BadRequest("Invalid data.");

            var general = data.General;
            var contacts = data.Contacts;
            var addresses = data.Addresses;
            var banks = data.Banks;
            var others = data.Others;
            var compCode = _globalVariableService.GetGlobalVariables();

            decimal TryParseDecimal(string input, int precisionLimit)
            {
                if (decimal.TryParse(input, out decimal result))
                {
                    if (precisionLimit == 5 && (result < -999.99m || result > 999.99m))
                        return 0;
                    return result;
                }
                return 0;
            }
            int ParseInt(string input) => int.TryParse(input, out int result) ? result : 0;
            DateTime? ParseDate(string input) => DateTime.TryParse(input, out DateTime result) ? result : (DateTime?)null;

            using (SqlConnection conn = _dbConnection.GetErpConnection() as SqlConnection)
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. General Info
                        using (SqlCommand cmd = new SqlCommand("sp_InsertBusinessPartner", conn, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                            cmd.Parameters.AddWithValue("@GROUP_CODE", ParseInt(general.GROUPNAME));
                            cmd.Parameters.AddWithValue("@NATURE", general.NATURE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@NAME", general.NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHORTNAME", general.SHORTNAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LANGUAGE_CODE", general.LANGUAGE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CURRENCY_CODE", general.CURRENCY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ALIASNAME", general.ALIAS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PHONE", general.PHONE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@FAX", general.FAX ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MOBILE", general.MOBILE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SMS", general.SMS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@WEBSITE", general.WEBSITE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@EMAIL", general.EMAILID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PARTY_TYPE", general.PARTYTYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@GST_TYPE", general.GSTTYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MSME_YN", general.MSMEYN ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MSME_NO", general.MSMENO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MSME_TYPE", general.MSMETYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TCS_APPLY", general.TCSAPPLICABLE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TDS_206APPLY", general.TDSAPPLICABLE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CONTACT_PERSON", general.CONTACTPERSON ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MAIN_CODE", ParseInt(general.MAINAC));
                            cmd.Parameters.AddWithValue("@AGENT_CODE", ParseInt(general.AGENTNAME));
                            cmd.Parameters.AddWithValue("@OS_CODE", ParseInt(general.OSGROUP));
                            cmd.Parameters.AddWithValue("@DISC_PER", TryParseDecimal(general.DISCOUNT, 5));
                            cmd.Parameters.AddWithValue("@DISCGRP_CODE", ParseInt(general.DISTRICTGROUP));
                            cmd.Parameters.AddWithValue("@PAYTERM_CODE", ParseInt(general.PAYMENTTERM));
                            cmd.Parameters.AddWithValue("@CREDIT_DAYS", ParseInt(general.CRDAYS));
                            cmd.Parameters.AddWithValue("@CREDIT_LIMIT", TryParseDecimal(general.CRLIMIT, 14));
                            cmd.Parameters.AddWithValue("@LASTCREDIT_LIMIT", TryParseDecimal(general.LASTCRLIMIT, 14));
                            cmd.Parameters.AddWithValue("@LEGAL_NAME", general.LEGALNAME ?? (object)DBNull.Value);
                            // Bank
                            //cmd.Parameters.AddWithValue("@BANK_COUNTRY", ParseInt(general.BANKCOUNTRY) ?? 0);
                            cmd.Parameters.Add("@BANK_COUNTRY", SqlDbType.Int).Value = string.IsNullOrEmpty(general.BANKCOUNTRY)? 0: int.Parse(general.BANKCOUNTRY);

                            //cmd.Parameters.AddWithValue("@BANK_CODE", general.BANK_CODE ?? 0);
                            cmd.Parameters.AddWithValue("@BANK_CODE", DBNull.Value);
                            cmd.Parameters.AddWithValue("@BANK_NAME", DBNull.Value);

                            cmd.Parameters.AddWithValue("@BANK_BRANCH", general.BANKBRANCH ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IFSC_CODE", general.IFSCCODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PAY_TYPE", general.PAYTYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@AC_NO", general.ACNO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@REMARKS", general.REMARKS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@AADHAR", general.AADHAR ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CPCBNO", general.CPCBNo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ENTITY_TYPE", general.ENTITYTYPE ?? (object)DBNull.Value);

                            // Audit fields
                            cmd.Parameters.AddWithValue("@UUSER", compCode.PubUserId);
                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                            cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                            cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                            cmd.Parameters.AddWithValue("@AED", "A");
                            cmd.Parameters.AddWithValue("@WSID", compCode.PubWorkStationID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LIP", compCode.PubLocalId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmd.Parameters.AddWithValue("@Action", "Insert");

                            cmd.ExecuteNonQuery();
                        }
                        // 2. Contacts
                        foreach (var contact in contacts)
                        {
                            using (SqlCommand cmd = new SqlCommand("sp_InsertSubGroupContact", conn, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CONTACT_ID", 0);
                                cmd.Parameters.AddWithValue("@NAME", contact.ContactPerson ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TITLE", contact.Title ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DESIGNATION", contact.Designation ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PHONE", contact.Phone ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@MOBILE", contact.Mobile ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SMS", contact.SMS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FAX", contact.Fax ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EMAIL", contact.Email ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DOB", contact.DOB ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DOM", contact.DOM ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GENDER", contact.Gender ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PORTAL_ID", contact.PortalID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PORTAL_PASSWORD", contact.PortalPassword ?? (object)DBNull.Value);

                                cmd.Parameters.AddWithValue("@UUSER", compCode.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", compCode.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", compCode.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@Action", "Insert");

                                cmd.ExecuteNonQuery();  
                            }
                        }
                        // 3. Addresses
                        if (addresses != null && addresses.All(a => a.IsChecked == 0) && addresses.Count > 0)
                        {
                            addresses[0].IsChecked = 1;
                        }
                        foreach (var address in addresses)
                        {
                            using (SqlCommand cmd = new SqlCommand("Sp_InsertSubgroupAddress", conn, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ADDRESS_ID", DBNull.Value);
                                cmd.Parameters.AddWithValue("@ADDRESS_TYPE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@ADD1", address.address1 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ADD2", address.address2 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ADD3", address.address3 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CITY_CODE", address.CityValue);
                                cmd.Parameters.AddWithValue("@STATE_CODE", address.StateValue);
                                cmd.Parameters.AddWithValue("@COUNTRY_CODE", address.CountryValue);
                                cmd.Parameters.AddWithValue("@PINCODE", address.pincode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DISTANCE", ParseInt(address.distance));
                                cmd.Parameters.AddWithValue("@GSTIN", address.gstNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PAN", address.panNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DECL_NO", address.declNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DECL_DATE", ParseDate(address.declDate));
                                cmd.Parameters.AddWithValue("@IS_DEFAULT", address.IsChecked);
                                cmd.Parameters.AddWithValue("@DEFAULT_ID", DBNull.Value);
                                cmd.Parameters.AddWithValue("@UUSER", compCode.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", compCode.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", compCode.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@TAN_NO", address.tanNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_TYPE", address.billType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GST_CERDATE", ParseDate(address.gstCertDt));
                                cmd.Parameters.AddWithValue("@LEAD_DAYS", ParseInt(address.leadDays));
                                cmd.Parameters.AddWithValue("@ALEGAL_NAME", address.legalName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Action", "Insert");
                                cmd.ExecuteNonQuery();
                            }
                        }
                        // 4. Attachments (Others)
                        foreach (var other in others)
                        {
                            using (SqlCommand cmd = new SqlCommand("Sp_InsertSubgroupAttach", conn, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ATTACH_ID", 1);
                                cmd.Parameters.AddWithValue("@FILENAME", other.PAN ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FILEPATH", "/attachments/pan/" + (other.PAN ?? ""));
                                cmd.Parameters.AddWithValue("@ATTACH_DATE", DateTime.Now);

                                cmd.Parameters.AddWithValue("@UUSER", compCode.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", compCode.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", compCode.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                //cmd.Parameters.AddWithValue("@Action", "Insert");
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 5. Attachments (Others)
                        foreach (var bank in banks)
                        {
                            using (SqlCommand cmd = new SqlCommand("Sp_InsertSubgroupBank", conn, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@IS_DEFAULT", 1); // empty string "" not needed
                                cmd.Parameters.AddWithValue("@BD_CODE", bank.BankCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BD_NAME", bank.BankName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BD_COUNTRYCODE", bank.CountryCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BD_IFSCCODE", bank.IFSC ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BD_ACTNO", bank.acNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BD_BRANCH", bank.Branch ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BD_PAYTYPE", bank.pay ?? (object)DBNull.Value);

                                // System fields
                                cmd.Parameters.AddWithValue("@UUSER", compCode.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", compCode.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", compCode.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                // Attachment
                                cmd.Parameters.AddWithValue("@FILENAME", bank.FileName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FILEPATH", DBNull.Value); // if not used
                                cmd.Parameters.AddWithValue("@ATTACH_DATE", DateTime.TryParse(bank.attachDate, out DateTime dt) ? dt : (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Action", "Insert");

                                cmd.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                        return Ok(new { success = true, message = "Data saved successfully." });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, $"Error occurred: {ex.Message}");
                    }
                }
            }
        }
        [HttpPost]
        public IActionResult UpdateAllData([FromBody] BusinessPartnerWrapperUpdate data)
        {
            if (data == null)
                return BadRequest("Invalid data.");
            var general = data.General;
            var contacts = data.Contacts;
            var addresses = data.Addresses;
            var banks = data.Banks;
            var others = data.Others;
            var compCode = _globalVariableService.GetGlobalVariables();

            decimal TryParseDecimal(string input, int precisionLimit)
            {
                if (decimal.TryParse(input, out decimal result))
                {
                    if (precisionLimit == 5 && (result < -999.99m || result > 999.99m))
                        return 0;
                    return result;
                }
                return 0;
            }
            int ParseInt(string input) => int.TryParse(input, out int result) ? result : 0;
            DateTime? ParseDate(string input) => DateTime.TryParse(input, out DateTime result) ? result : (DateTime?)null;

            using (SqlConnection conn = _dbConnection.GetErpConnection() as SqlConnection)
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. General Info
                        using (SqlCommand cmd = new SqlCommand("sp_InsertBusinessPartner", conn, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                            cmd.Parameters.AddWithValue("@GROUP_CODE", ParseInt(general.GROUPNAME));
                            cmd.Parameters.AddWithValue("@NATURE", general.NATURE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@NAME", general.NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHORTNAME", general.SHORTNAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LANGUAGE_CODE", general.LANGUAGE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CURRENCY_CODE", general.CURRENCY ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ALIASNAME", general.ALIAS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PHONE", general.PHONE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@FAX", general.FAX ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MOBILE", general.MOBILE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SMS", general.SMS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@WEBSITE", general.WEBSITE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@EMAIL", general.EMAILID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PARTY_TYPE", general.PARTYTYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@GST_TYPE", general.GSTTYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MSME_YN", general.MSMEYN ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MSME_NO", general.MSMENO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MSME_TYPE", general.MSMETYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TCS_APPLY", general.TCSAPPLICABLE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TDS_206APPLY", general.TDSAPPLICABLE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CONTACT_PERSON", general.CONTACTPERSON ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MAIN_CODE", ParseInt(general.MAINAC));
                            cmd.Parameters.AddWithValue("@AGENT_CODE", ParseInt(general.AGENTNAME));
                            cmd.Parameters.AddWithValue("@OS_CODE", ParseInt(general.OSGROUP));
                            cmd.Parameters.AddWithValue("@DISC_PER", TryParseDecimal(general.DISCOUNT, 5));
                            cmd.Parameters.AddWithValue("@DISCGRP_CODE", ParseInt(general.DISTRICTGROUP));
                            cmd.Parameters.AddWithValue("@PAYTERM_CODE", ParseInt(general.PAYMENTTERM));
                            cmd.Parameters.AddWithValue("@CREDIT_DAYS", ParseInt(general.CRDAYS));
                            cmd.Parameters.AddWithValue("@CREDIT_LIMIT", TryParseDecimal(general.CRLIMIT, 14));
                            cmd.Parameters.AddWithValue("@LASTCREDIT_LIMIT", TryParseDecimal(general.LASTCRLIMIT, 14));
                            cmd.Parameters.AddWithValue("@LEGAL_NAME", general.LEGALNAME ?? (object)DBNull.Value);

                            // Bank
                            cmd.Parameters.Add("@BANK_COUNTRY", SqlDbType.Int).Value = string.IsNullOrEmpty(general.BANKCOUNTRY) ? 0 : int.Parse(general.BANKCOUNTRY);
                            cmd.Parameters.AddWithValue("@BANK_CODE", DBNull.Value);
                            cmd.Parameters.AddWithValue("@BANK_NAME", DBNull.Value);

                            cmd.Parameters.AddWithValue("@BANK_BRANCH", general.BANKBRANCH ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IFSC_CODE", general.IFSCCODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PAY_TYPE", general.PAYTYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@AC_NO", general.ACNO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@REMARKS", general.REMARKS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@AADHAR", general.AADHAR ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CPCBNO", general.CPCBNo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ENTITY_TYPE", general.ENTITYTYPE ?? (object)DBNull.Value);

                            // Audit fields
                            cmd.Parameters.AddWithValue("@UUSER", compCode.PubUserId);
                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                            cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                            cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                            cmd.Parameters.AddWithValue("@AED", "A");
                            cmd.Parameters.AddWithValue("@WSID", compCode.PubWorkStationID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LIP", compCode.PubLocalId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmd.Parameters.AddWithValue("@Action", "Update");

                            cmd.ExecuteNonQuery();
                        }

                        // 2. Contacts

                        using (SqlCommand deleteContacts = new SqlCommand("DELETE FROM SUBGROUP_CONTACT WHERE COMP_CODE = @COMP_CODE AND CODE = @CODE", conn, tran))
                        {
                            deleteContacts.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                            deleteContacts.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                            deleteContacts.ExecuteNonQuery();
                        }
                        foreach (var contact in contacts)
                        {
                            using (SqlCommand cmd = new SqlCommand("sp_InsertSubGroupContact", conn, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CONTACT_ID", 0);
                                cmd.Parameters.AddWithValue("@NAME", contact.ContactPerson ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TITLE", contact.Title ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DESIGNATION", contact.Designation ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PHONE", contact.Phone ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@MOBILE", contact.Mobile ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SMS", contact.SMS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FAX", contact.Fax ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EMAIL", contact.Email ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DOB", contact.DOB ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DOM", contact.DOM ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GENDER", contact.Gender ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PORTAL_ID", contact.PortalID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PORTAL_PASSWORD", contact.PortalPassword ?? (object)DBNull.Value);

                                cmd.Parameters.AddWithValue("@UUSER", compCode.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", compCode.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", compCode.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@Action", "Update");


                                cmd.ExecuteNonQuery();
                            }
                        }
                        // 3. Addresses
                        if (addresses != null && addresses.All(a => a.IsChecked == 0) && addresses.Count > 0)
                        {
                            // Set the first address as default
                            addresses[0].IsChecked = 1;
                        }
                        using (SqlCommand deleteAddresses = new SqlCommand("DELETE FROM SUBGROUP_ADDRESS WHERE COMP_CODE = @COMP_CODE AND CODE = @CODE", conn, tran))
                        {
                            deleteAddresses.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                            deleteAddresses.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                            deleteAddresses.ExecuteNonQuery();
                        }

                        foreach (var address in addresses)      
                        {
                            using (SqlCommand cmd = new SqlCommand("Sp_InsertSubgroupAddress", conn, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ADDRESS_ID", DBNull.Value);
                                cmd.Parameters.AddWithValue("@ADDRESS_TYPE", DBNull.Value);

                                cmd.Parameters.AddWithValue("@ADD1", address.address1 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ADD2", address.address2 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ADD3", address.address3 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CITY_CODE", address.CityValue);
                                cmd.Parameters.AddWithValue("@STATE_CODE", address.StateValue);
                                cmd.Parameters.AddWithValue("@COUNTRY_CODE", address.CountryValue);
                                cmd.Parameters.AddWithValue("@PINCODE", address.pincode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DISTANCE", ParseInt(address.distance));
                                cmd.Parameters.AddWithValue("@GSTIN", address.gstNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PAN", address.panNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DECL_NO", address.declNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DECL_DATE", ParseDate(address.declDate));
                                cmd.Parameters.AddWithValue("@IS_DEFAULT", address.IsChecked);
                                cmd.Parameters.AddWithValue("@DEFAULT_ID", DBNull.Value);
                                cmd.Parameters.AddWithValue("@UUSER", compCode.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", compCode.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", compCode.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@TAN_NO", address.tanNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_TYPE", address.billType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GST_CERDATE", ParseDate(address.gstCertDt));
                                cmd.Parameters.AddWithValue("@LEAD_DAYS", ParseInt(address.leadDays));
                                cmd.Parameters.AddWithValue("@ALEGAL_NAME", address.legalName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Action", "Update");
                                cmd.ExecuteNonQuery();
                            }
                        }

                        using (SqlCommand deleteother = new SqlCommand("DELETE FROM SUBGROUP_ATTACH WHERE COMP_CODE = @COMP_CODE AND CODE = @CODE", conn, tran))
                        {
                            deleteother.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                            deleteother.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                            deleteother.ExecuteNonQuery();
                        }

                        //// 4. Attachments (Others)
                        foreach (var other in others)
                        {
                            using (SqlCommand cmd = new SqlCommand("Sp_InsertSubgroupAttach", conn, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ATTACH_ID", 1);
                                cmd.Parameters.AddWithValue("@FILENAME", other.PAN ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FILEPATH", "/attachments/pan/" + (other.PAN ?? ""));
                                cmd.Parameters.AddWithValue("@ATTACH_DATE", DateTime.Now);

                                cmd.Parameters.AddWithValue("@UUSER", compCode.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", compCode.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", compCode.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                //cmd.Parameters.AddWithValue("@Action", "Insert");
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 5. Attachments (Others)
                        using (SqlCommand deleteBanks = new SqlCommand("DELETE FROM SUBGROUP_BANK WHERE COMP_CODE = @COMP_CODE AND CODE = @CODE", conn, tran))
                        {
                            deleteBanks.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                            deleteBanks.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                            deleteBanks.ExecuteNonQuery();
                        }
                        foreach (var bank in banks)
                        {
                            using (SqlCommand cmd = new SqlCommand("Sp_InsertSubgroupBank", conn, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@COMP_CODE", compCode.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", general.ACCODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@IS_DEFAULT", 1);
                                cmd.Parameters.AddWithValue("@BD_CODE", bank.BankCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BD_NAME", bank.BankName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BD_COUNTRYCODE", bank.CountryCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BD_IFSCCODE", bank.IFSC ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BD_ACTNO", bank.acNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BD_BRANCH", bank.Branch ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BD_PAYTYPE", bank.pay ?? (object)DBNull.Value);

                                // System fields
                                cmd.Parameters.AddWithValue("@UUSER", compCode.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", compCode.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", compCode.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                // Attachment
                                cmd.Parameters.AddWithValue("@FILENAME", bank.FileName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FILEPATH", DBNull.Value);
                                cmd.Parameters.AddWithValue("@ATTACH_DATE", DateTime.TryParse(bank.attachDate, out DateTime dt) ? dt : (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Action", "Update");
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                        return Ok(new { success = true, message = "Data Update successfully." });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, $"Error occurred: {ex.Message}");
                    }
                }
            }
        }
        [HttpPost]
        public IActionResult GetBusinessPartnerMaster([FromBody] CodeRequest request)
        {
            BusinessPartnerWrapper wrapper = new BusinessPartnerWrapper
            {
                General = new GeneralDetailsModel(),
                Contacts = new List<ContactDetailsModel>(),
                Addresses = new List<AddressModel>(),
                Banks = new List<BankDetailModel>(),
                Others = new List<OtherDetailModel>()
            };

            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetBusinessPartnerDetails", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CODE", request.code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        // General Details
                        if (rdr.Read())
                        {
                            wrapper.General = new GeneralDetailsModel
                            {
                                ACCODE = rdr["CODE"]?.ToString(),
                                NAME = rdr["NAME"]?.ToString(),
                                NATURE = rdr["NATURE"]?.ToString(),
                                SHORTNAME = rdr["SHORTNAME"]?.ToString(),
                                ALIAS = rdr["ALIASNAME"]?.ToString(),
                                LEGALNAME = rdr["LEGAL_NAME"]?.ToString(),
                                GROUPNAME = rdr["GROUP_CODE"]?.ToString(),

                                PHONE = rdr["PHONE"]?.ToString(),
                                FAX = rdr["FAX"]?.ToString(),
                                MOBILE = rdr["MOBILE"]?.ToString(),
                                SMS = rdr["SMS"]?.ToString(),
                                WEBSITE = rdr["WEBSITE"]?.ToString(),
                                EMAILID = rdr["EMAIL"]?.ToString(),

                                LANGUAGE = rdr["CURRENCY_CODE"]?.ToString(),
                                CURRENCY = rdr["LANGUAGE_CODE"]?.ToString(),

                                PARTYTYPE = rdr["PARTY_TYPE"]?.ToString(),
                                GSTTYPE = rdr["GST_TYPE"]?.ToString(),
                                MSMEYN = rdr["MSME_YN"]?.ToString(),
                                MSMENO = rdr["MSME_NO"]?.ToString(),
                                MSMETYPE = rdr["MSME_TYPE"]?.ToString(),
                                TCSAPPLICABLE = rdr["TCS_APPLY"]?.ToString(),
                                TDSAPPLICABLE = rdr["TDS_206APPLY"]?.ToString(),
                                CONTACTPERSON = rdr["CONTACT_PERSON"]?.ToString(),
                                MAINAC = rdr["MAIN_CODE"]?.ToString(),
                                AGENTNAME = rdr["AGENT_CODE"]?.ToString(),
                                OSGROUP = rdr["OS_CODE"]?.ToString(),
                                DISCOUNT = rdr["DISC_PER"]?.ToString(),
                                DISTRICTGROUP = rdr["DISCGRP_CODE"]?.ToString(),
                                PAYMENTTERM = rdr["PAYTERM_CODE"]?.ToString(),
                                CRDAYS = rdr["CREDIT_DAYS"]?.ToString(),
                                CRLIMIT = rdr["CREDIT_LIMIT"]?.ToString(),
                                LASTCRLIMIT = rdr["LASTCREDIT_LIMIT"]?.ToString(),
                                BANKCOUNTRY = rdr["BANK_COUNTRY"]?.ToString(),
                                BANK_CODE = rdr["BANK_CODE"]?.ToString(),
                                BANKNAME = rdr["BANK_NAME"]?.ToString(),
                                BANKBRANCH = rdr["BANK_BRANCH"]?.ToString(),
                                IFSCCODE = rdr["IFSC_CODE"]?.ToString(),
                                PAYTYPE = rdr["PAY_TYPE"]?.ToString(),
                                ACNO = rdr["AC_NO"]?.ToString(),
                                REMARKS = rdr["REMARKS"]?.ToString(),
                                AADHAR = rdr["AADHAR"]?.ToString(),
                                CPCBNo = rdr["CPCBNO"]?.ToString(),
                                ENTITYTYPE = rdr["ENTITY_TYPE"]?.ToString()
                            };
                        }
                        // Contact Details
                        if (rdr.NextResult())
                        {
                            while (rdr.Read())
                            {
                                wrapper.Contacts.Add(new ContactDetailsModel
                                {
                                    Title = rdr["TITLE"] != DBNull.Value ? rdr["TITLE"].ToString() : null,
                                    ContactPerson = rdr["NAME"] != DBNull.Value ? rdr["NAME"].ToString() : null,
                                    Designation = rdr["DESIGNATION"] != DBNull.Value ? rdr["DESIGNATION"].ToString() : null,
                                    Phone = rdr["PHONE"] != DBNull.Value ? rdr["PHONE"].ToString() : null,
                                    Mobile = rdr["MOBILE"] != DBNull.Value ? rdr["MOBILE"].ToString() : null,
                                    SMS = rdr["SMS"] != DBNull.Value ? rdr["SMS"].ToString() : null,
                                    Email = rdr["EMAIL"] != DBNull.Value ? rdr["EMAIL"].ToString() : null,
                                    Fax = rdr["FAX"] != DBNull.Value ? rdr["FAX"].ToString() : null,
                                    DOB = rdr["DOB"] != DBNull.Value ? Convert.ToDateTime(rdr["DOB"]) : (DateTime?)null,
                                    DOM = rdr["DOM"] != DBNull.Value ? Convert.ToDateTime(rdr["DOM"]) : (DateTime?)null,
                                    Gender = rdr["GENDER"] != DBNull.Value ? rdr["GENDER"].ToString() : null,
                                    PortalID = rdr["PORTAL_ID"] != DBNull.Value ? rdr["PORTAL_ID"].ToString() : null,
                                    PortalPassword = rdr["PORTAL_PASSWORD"] != DBNull.Value ? rdr["PORTAL_PASSWORD"].ToString() : null
                                });
                            }
                        }
                        // Address Details
                        if (rdr.NextResult())
                        {
                            while (rdr.Read())
                            {
                                wrapper.Addresses.Add(new AddressModel
                                {
                                    address1 = rdr["ADD1"]?.ToString(),
                                    address2 = rdr["ADD2"]?.ToString(),
                                    address3 = rdr["ADD3"]?.ToString(),
                                    city = rdr["CITY_CODE"]?.ToString(),
                                    state = rdr["STATE_CODE"]?.ToString(),
                                    country = rdr["COUNTRY_CODE"]?.ToString(),
                                    CityValue = rdr["CityValue"] != DBNull.Value ? Convert.ToInt32(rdr["CityValue"]) : 0,
                                    StateValue = rdr["StateValue"] != DBNull.Value ? Convert.ToInt32(rdr["StateValue"]) : 0,
                                    CountryValue = rdr["CountryValue"] != DBNull.Value ? Convert.ToInt32(rdr["CountryValue"]) : 0,

                                    pincode = rdr["PINCODE"]?.ToString(),
                                    gstNo = rdr["GSTIN"]?.ToString(),
                                    panNo = rdr["PAN"]?.ToString(),
                                    tanNo = rdr["TAN_NO"]?.ToString(),
                                    gstCertDt = rdr["GST_CERDATE"]?.ToString(),
                                    declNo = rdr["DECL_NO"]?.ToString(),
                                    declDate = rdr["DECL_DATE"]?.ToString(),
                                    distance = rdr["DISTANCE"]?.ToString(),
                                    billType = rdr["BILL_TYPE"]?.ToString(),
                                    leadDays = rdr["LEAD_DAYS"]?.ToString(),
                                    legalName = rdr["ALEGAL_NAME"]?.ToString(),
                                    IsChecked = rdr["IS_DEFAULT"] != DBNull.Value ? Convert.ToInt32(rdr["IS_DEFAULT"]) : 0
                                });
                            }
                        }
                        // Bank Details
                        if (rdr.NextResult())
                        {
                            while (rdr.Read())
                            {
                                wrapper.Banks.Add(new BankDetailModel
                                {
                                    BankCode = rdr["BD_CODE"]?.ToString(),
                                    BankName = rdr["BD_NAME"]?.ToString(),
                                    CountryCode = rdr["BD_COUNTRYCODE"]?.ToString(),
                                    Country = rdr["BD_COUNTRYCODE"]?.ToString(),
                                    IFSC = rdr["BD_IFSCCODE"]?.ToString(),
                                    acNo = rdr["BD_ACTNO"]?.ToString(),
                                    Branch = rdr["BD_BRANCH"]?.ToString(),
                                    pay = rdr["BD_PAYTYPE"]?.ToString(),
                                    FileName = rdr["FILENAME"]?.ToString(),
                                    attachDate = rdr["ATTACH_DATE"]?.ToString(),
                                    BD_Name = rdr["BD_Name"]?.ToString()
                                });
                            }
                        }
                        // Other Details
                        if (rdr.NextResult())
                        {
                            while (rdr.Read())
                            {
                                wrapper.Others.Add(new OtherDetailModel
                                {
                                    FileName = rdr["FILENAME"]?.ToString(),
                                    FilePath = rdr["FILEPATH"]?.ToString(),
                                    AttachDate = rdr["ATTACH_DATE"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            return Json(wrapper);
        }
        // Modal poup save Start Block
        [HttpGet]
        public JsonResult GetTableColumns(string tableName)
        {
            List<object> cols = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                SqlCommand cmd = new SqlCommand("sp_GetLanguageAndCurrency", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TableName", tableName);

                SqlDataReader rd = cmd.ExecuteReader();

                // Read column names dynamically from query result
                var schema = rd.GetSchemaTable();

                foreach (DataRow row in schema.Rows)
                {
                    string colName = row["ColumnName"].ToString();

                    cols.Add(new
                    {
                        ColumnName = colName,
                        Label = colName,   
                        Value = ""
                    });
                }

                rd.Close();
            }

            return Json(cols);
        }
        [HttpPost]
        public JsonResult SaveDynamicRecord([FromBody] DynamicSaveRequest request)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                var whereParts = new List<string>();
                var insertCols = new List<string>();
                var insertParams = new List<string>();
                var g = _globalVariableService.GetGlobalVariables();

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                foreach (var item in request.Values)
                {
                    string col = item.ColumnName.ToUpper();
                    string param = "@" + col;

                    whereParts.Add($"{col} = {param}");
                    insertCols.Add(col);
                    insertParams.Add(param);

                    cmd.Parameters.AddWithValue(param,
                        string.IsNullOrWhiteSpace(item.Value)
                            ? (object)DBNull.Value
                            : item.Value);
                }
                bool hasCodeColumn = false;
                using (var colCmd = new SqlCommand(@"SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tbl AND COLUMN_NAME = 'CODE'", con))
                {
                    colCmd.Parameters.AddWithValue("@tbl", request.TableName);
                    hasCodeColumn = Convert.ToInt32(colCmd.ExecuteScalar()) > 0;
                }
                if (hasCodeColumn &&
                    !insertCols.Contains("CODE")) 
                {
                    using (var maxCmd = new SqlCommand($"SELECT ISNULL(MAX(CODE),0) + 1 FROM {request.TableName}", con))
                    {
                        int nextCode = Convert.ToInt32(maxCmd.ExecuteScalar());
                        insertCols.Add("CODE");
                        insertParams.Add("@CODE");
                        cmd.Parameters.AddWithValue("@CODE", nextCode);
                        whereParts.Add("CODE = @CODE");
                    }
                }
                string checkQuery = $"SELECT COUNT(1) FROM {request.TableName} " + $"WHERE {string.Join(" AND ", whereParts)}";
                cmd.CommandText = checkQuery;
                int exists = Convert.ToInt32(cmd.ExecuteScalar());
                if (exists > 0)
                {
                    return Json(new
                    {
                        status = false,
                        message = "Record already exists"
                    });
                }
                insertCols.AddRange(new[]
                {
                    "UUSER","UDATE","EUSER","EDATE", "AED","WSID","LIP","LID","ACTIVE"
                });
                insertParams.AddRange(new[]
                {
                    "@UUSER","@UDATE","@EUSER","@EDATE","@AED","@WSID","@LIP","@LID","@ACTIVE"
                });
                cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                cmd.Parameters.AddWithValue("@AED", "A");
                cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@LIP", g.PubLocalId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                cmd.Parameters.AddWithValue("@ACTIVE", 1);

                string insertQuery = $"INSERT INTO {request.TableName} " + $"({string.Join(",", insertCols)}) " + $"VALUES ({string.Join(",", insertParams)})";
                cmd.CommandText = insertQuery;
                cmd.ExecuteNonQuery();

                return Json(new
                {
                    status = true,
                    message = "Inserted successfully"
                });
            }
        }
        public class DynamicSaveRequest
        {
            public string TableName { get; set; }
            public List<DynamicColumn> Values { get; set; }
        }
        public class DynamicColumn
        {
            public string ColumnName { get; set; }
            public string Value { get; set; }
        }
        // Modal poup save End Block
        public class CodeRequest
        {
            public int code { get; set; }
        }
        private int? ParseInt(string val)
        {
            if (int.TryParse(val, out int result))
                return result;
            return null;
        }
        private DateTime? ParseDate(string val)
        {
            if (DateTime.TryParse(val, out DateTime result))
                return result;
            return null;
        }

    }
}
