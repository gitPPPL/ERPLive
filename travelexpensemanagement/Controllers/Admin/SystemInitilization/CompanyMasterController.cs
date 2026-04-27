using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.Design;
using System.Data;
using System.Data.Common;
using System.Diagnostics.Metrics;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.SystemInitilization;
using travelexpensemanagement.ModuleService;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace travelexpensemanagement.Controllers.Master
{
    [SessionAuthorize]
    public class CompanyMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public CompanyMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        //VIEW FOR LISTING...........
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Company Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); 

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            //return View(model);
            return View("~/Views/Admin/SystemInitilization/CompanyMaster/Index.cshtml", model);
        }

        public IActionResult AddOrEditForm(int? code)
        {
            ViewBag.CompanyId = code;
            return View("~/Views/Admin/SystemInitilization/CompanyMaster/AddOrEditForm.cshtml");
        }
        // ACTIONs TO GET DATA FROM TABLE
        [HttpGet]
        public IActionResult GetAllCompanies(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var companyList = new List<COMP_MAST>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_CompanyMaster", conn)) // Make sure to create this stored procedure
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value);


                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                companyList.Add(new COMP_MAST
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString() ?? string.Empty,
                                    PINCODE = reader["PINCODE"]?.ToString() ?? string.Empty,
                                    PAN = reader["PAN"]?.ToString() ?? string.Empty,
                                    GSTIN = reader["GSTIN"]?.ToString() ?? string.Empty,
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                    CITY_CODE = reader["CITY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["CITY_CODE"]) : 0
                                    // Add other fields as needed
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = Convert.ToInt32(reader["TotalCount"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "An error occurred while processing your request.", message = ex.Message });
            }

            return Json(new { companies = companyList, totalCount });
        }
        [HttpGet]
        public JsonResult GetCompanyById(int RowId)
        {
            COMP_MAST company = null;
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_CompanyMaster", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@CODE", RowId);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            company = new COMP_MAST
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                NAME = reader["NAME"]?.ToString(),
                                ADD1 = reader["ADD1"]?.ToString(),
                                ADD2 = reader["ADD2"]?.ToString(),
                                ADD3 = reader["ADD3"]?.ToString(),
                                CITY_CODE = reader["CITY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["CITY_CODE"]) : 0,
                                PINCODE = reader["PINCODE"]?.ToString(),
                                STATE_CODE = reader["STATE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["STATE_CODE"]) : 0,
                                COUNTRY_CODE = reader["COUNTRY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COUNTRY_CODE"]) : 0,
                                REGADD1 = reader["REGADD1"]?.ToString(),
                                REGADD2 = reader["REGADD2"]?.ToString(),
                                CINNO = reader["CINNO"]?.ToString(),
                                PHONE = reader["PHONE"]?.ToString(),
                                FAX = reader["FAX"]?.ToString(),
                                EMAIL = reader["EMAIL"]?.ToString(),
                                WEBSITE = reader["WEBSITE"]?.ToString(),
                                PAN = reader["PAN"]?.ToString(),
                                GSTIN = reader["GSTIN"]?.ToString(),
                                IEC = reader["IEC"]?.ToString(),
                                EXCISE = reader["EXCISE"]?.ToString(),
                                SERVICETAX = reader["SERVICETAX"]?.ToString(),
                                STORE_PHONE = reader["STORE_PHONE"]?.ToString(),
                                STORE_EMAIL = reader["STORE_EMAIL"]?.ToString(),
                                CURRENCY_CODE = reader["CURRENCY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["CURRENCY_CODE"]) : 0,
                                LANGUAGE_CODE = reader["LANGUAGE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["LANGUAGE_CODE"]) : 0,
                                BANK_CODE = reader["BANK_CODE"] != DBNull.Value ? Convert.ToInt32(reader["BANK_CODE"]) : 0,
                                BANK_ADD1 = reader["BANK_ADD1"]?.ToString(),
                                BANK_ADD2 = reader["BANK_ADD2"]?.ToString(),
                                BANK_IFSC = reader["BANK_IFSC"]?.ToString(),
                                BANK_AC = reader["BANK_AC"]?.ToString(),
                                VALUATION_METHOD = reader["VALUATION_METHOD"]?.ToString(),
                                DEPRECIATION_METHOD = reader["DEPRECIATION_METHOD"]?.ToString(),
                                BUSINESS_TYPE = reader["BUSINESS_TYPE"]?.ToString(),
                                COMPANY_TYPE = reader["COMPANY_TYPE"]?.ToString(),
                                ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                SERVER_IP = reader["SERVER_IP"]?.ToString(),
                                DATABASE_NAME = reader["DATABASE_NAME"]?.ToString(),
                                MSMENO = reader["MSMENO"]?.ToString(),
                                LUTNO = reader["LUTNO"]?.ToString()
                            };

                            // Handle image logo
                            if (reader["COMP_LOGO"] != DBNull.Value)
                            {
                                byte[] logoBytes = (byte[])reader["COMP_LOGO"];
                                company.COMP_LOGO = Convert.ToBase64String(logoBytes);
                            }

                            if (reader["logo"] != DBNull.Value)
                            {
                                company.logo = (byte[])reader["logo"];
                            }
                        }
                        else
                        {
                            Console.WriteLine("No data found for the given company code.");
                        }
                    }
                }
            }
            return Json(company);
        }
        //Dropdown Loaders.............
        public JsonResult GetCountryList()
        {
            var countries = new List<object>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE, NAME FROM COUNTRY_MAST";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    countries.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }

            }
            return Json(countries);
        }
        public JsonResult GetStateList()
        {
            var state = new List<object>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE, NAME FROM STATE_MAST";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    state.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }

            }
            return Json(state);
        }
        public JsonResult GetCityList()
        {
            var city = new List<object>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE, NAME FROM CITY_MAST";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    city.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }

            }
            return Json(city);
        }
        public JsonResult GetLanguageList()
        {
            var city = new List<object>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE, NAME FROM LANGUAGE_MAST";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    city.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }

            }
            return Json(city);
        }
        public JsonResult GetCurrencyList()
        {
            var city = new List<object>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE, NAME FROM CURRENCY_MAST";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    city.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }

            }
            return Json(city);
        }
        public JsonResult GetBankList()
        {
            var city = new List<object>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE, NAME FROM BANK_MAST";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    city.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }

            }
            return Json(city);
        }
        public JsonResult GetStateCountryByCity(string cityCode)
        {
            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = @"SELECT s.CODE AS StateCode, s.NAME AS StateName, c.CODE AS CountryCode, c.NAME AS CountryName
            FROM CITY_MAST cm JOIN STATE_MAST s ON cm.STATE_CODE = s.CODE JOIN COUNTRY_MAST c ON cm.COUNTRY_CODE = c.CODE
            WHERE cm.CODE = @CityCode";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CityCode", cityCode);
                conn.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var result = new
                    {
                        stateCode = reader["StateCode"].ToString(),
                        stateName = reader["StateName"].ToString(),
                        countryCode = reader["CountryCode"].ToString(),
                        countryName = reader["CountryName"].ToString()
                    };
                    return Json(result);
                }
            }

            return Json(null);
        }

        [HttpPost]
        public IActionResult SaveCompany([FromBody] COMP_MAST company)
        {
            string action = company.ACTION == "INSERT" ? "INSERT" : "UPDATE";

            // Check for duplicate name before insert
            if (action == "INSERT" && IsDuplicateCompanyName(company.NAME))
            {
                return Json(new { success = false, message = "Company name already exists." });
            }

            var result = SaveOrUpdateCompany(company, action);

            TempData["Message"] = result;
            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }

        [HttpPost]
        public string SaveOrUpdateCompany(COMP_MAST company, string action)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_CompanyMaster", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var globalVar = _globalVariableService.GetGlobalVariables();

                        cmd.Parameters.Add("@Action", SqlDbType.NVarChar).Value = action;
                        cmd.Parameters.Add("@CODE", SqlDbType.Int).Value = company.CODE;
                        cmd.Parameters.Add("@NAME", SqlDbType.NVarChar, 200).Value = company.NAME ?? "";
                        cmd.Parameters.Add("@ADD1", SqlDbType.NVarChar, 200).Value = company.ADD1 ?? "";
                        cmd.Parameters.Add("@ADD2", SqlDbType.NVarChar, 200).Value = company.ADD2 ?? "";
                        cmd.Parameters.Add("@ADD3", SqlDbType.NVarChar, 200).Value = company.ADD3 ?? "";
                        cmd.Parameters.Add("@COUNTRY_CODE", SqlDbType.Int).Value = company.COUNTRY_CODE;
                        cmd.Parameters.Add("@STATE_CODE", SqlDbType.Int).Value = company.STATE_CODE;
                        cmd.Parameters.Add("@CITY_CODE", SqlDbType.Int).Value = company.CITY_CODE;
                        cmd.Parameters.Add("@PINCODE", SqlDbType.NVarChar, 20).Value = company.PINCODE ?? "";
                        cmd.Parameters.Add("@REGADD1", SqlDbType.NVarChar, 200).Value = company.REGADD1 ?? "";
                        cmd.Parameters.Add("@REGADD2", SqlDbType.NVarChar, 200).Value = company.REGADD2 ?? "";
                        cmd.Parameters.Add("@PHONE", SqlDbType.NVarChar, 20).Value = company.PHONE ?? "";
                        cmd.Parameters.Add("@FAX", SqlDbType.NVarChar, 20).Value = company.FAX ?? "";
                        cmd.Parameters.Add("@EMAIL", SqlDbType.NVarChar, 100).Value = company.EMAIL ?? "";
                        cmd.Parameters.Add("@WEBSITE", SqlDbType.NVarChar, 100).Value = company.WEBSITE ?? "";
                        cmd.Parameters.Add("@PAN", SqlDbType.NVarChar, 10).Value = company.PAN ?? "";
                        cmd.Parameters.Add("@GSTIN", SqlDbType.NVarChar, 15).Value = company.GSTIN ?? "";
                        cmd.Parameters.Add("@CINNO", SqlDbType.NVarChar, 21).Value = company.CINNO ?? "";
                        cmd.Parameters.Add("@IEC", SqlDbType.NVarChar, 10).Value = company.IEC ?? "";
                        cmd.Parameters.Add("@EXCISE", SqlDbType.NVarChar, 15).Value = company.EXCISE ?? "";
                        cmd.Parameters.Add("@SERVICETAX", SqlDbType.NVarChar, 15).Value = company.SERVICETAX ?? "";
                        cmd.Parameters.Add("@STORE_PHONE", SqlDbType.NVarChar, 20).Value = company.STORE_PHONE ?? "";
                        cmd.Parameters.Add("@STORE_EMAIL", SqlDbType.NVarChar, 100).Value = company.STORE_EMAIL ?? "";
                        cmd.Parameters.Add("@BANK_CODE", SqlDbType.Int, 20).Value = company.BANK_CODE ?? 0;
                        cmd.Parameters.Add("@BANK_IFSC", SqlDbType.NVarChar, 15).Value = company.BANK_IFSC ?? "";
                        cmd.Parameters.Add("@BANK_AC", SqlDbType.NVarChar, 30).Value = company.BANK_AC ?? "";
                        cmd.Parameters.Add("@VALUATION_METHOD", SqlDbType.NVarChar, 20).Value = company.VALUATION_METHOD ?? "";
                        cmd.Parameters.Add("@DEPRECIATION_METHOD", SqlDbType.NVarChar, 20).Value = company.DEPRECIATION_METHOD ?? "";
                        cmd.Parameters.Add("@BUSINESS_TYPE", SqlDbType.NVarChar, 50).Value = company.BUSINESS_TYPE ?? "";
                        cmd.Parameters.Add("@BANK_ADD1", SqlDbType.NVarChar, 200).Value = company.BANK_ADD1 ?? "";
                        cmd.Parameters.Add("@BANK_ADD2", SqlDbType.NVarChar, 200).Value = company.BANK_ADD2 ?? "";
                        cmd.Parameters.Add("@CURRENCY_CODE", SqlDbType.NVarChar, 10).Value = (object?)company.CURRENCY_CODE ?? DBNull.Value;
                        cmd.Parameters.Add("@LANGUAGE_CODE", SqlDbType.NVarChar, 10).Value = (object?)company.LANGUAGE_CODE ?? DBNull.Value;
                        cmd.Parameters.Add("@COMP_LOGO", SqlDbType.VarBinary).Value = string.IsNullOrEmpty(company.COMP_LOGO_BASE64) ? DBNull.Value :
                            Convert.FromBase64String(company.COMP_LOGO_BASE64);

                        // Audit fields
                        cmd.Parameters.Add("@ACTIVE", SqlDbType.Int).Value = company.ACTIVE;
                        cmd.Parameters.Add("@UUSER", SqlDbType.Int).Value = globalVar.PubUserId;
                        cmd.Parameters.Add("@UDATE", SqlDbType.SmallDateTime).Value = (object?)company.UDATE ?? DateTime.Now;
                        cmd.Parameters.Add("@EUSER", SqlDbType.Int).Value = globalVar.PubUserId;
                        cmd.Parameters.Add("@EDATE", SqlDbType.SmallDateTime).Value = (object?)company.EDATE ?? DateTime.Now;
                        cmd.Parameters.Add("@AED", SqlDbType.NVarChar, 1).Value = company.AED ?? "A";
                        cmd.Parameters.Add("@WSID", SqlDbType.NVarChar, 100).Value = globalVar.PubWorkStationID ?? "";
                        cmd.Parameters.Add("@LIP", SqlDbType.NVarChar, 100).Value = globalVar.PubLocalId;
                        cmd.Parameters.Add("@LID", SqlDbType.NVarChar, 100).Value = Environment.MachineName ?? "";

                        con.Open();
                        cmd.ExecuteNonQuery();
                        return "Success";
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                return $"SQL Error: {sqlEx.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        public JsonResult DeleteCompanyByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_CompanyMaster", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Record deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting record.", error = ex.Message });
            }
        }

        private bool IsDuplicateCompanyName(string companyName)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM COMP_MAST WHERE NAME = @Name", con))
                {
                    cmd.Parameters.AddWithValue("@Name", companyName ?? "");

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        public IActionResult ExportAllCompanyData()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var companyList = new List<CompanyExportModel>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_CompanyMaster", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Export");
                    //cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@PageNumber", 1);
                    cmd.Parameters.AddWithValue("@PageSize", int.MaxValue);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            companyList.Add(new CompanyExportModel
                            {
                                Code = reader["Code"]?.ToString(),
                                Name = reader["Name"]?.ToString(),
                                City = reader["City"]?.ToString(),
                                PINCODE = reader["PINCODE"]?.ToString(),
                                PAN = reader["PAN"]?.ToString(),
                                GSTIN = reader["GSTIN"]?.ToString(),
                                Status = reader["Status"]?.ToString()
                            });
                        }
                    }
                }
            }

            return Json(companyList);
        }

        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<DocDetailDto> docDetails = new List<DocDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_CompanyMaster", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DocDetailID");
                    cmd.Parameters.AddWithValue("@Code", docCode);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            docDetails.Add(new DocDetailDto
                            {
                                Code = reader["Code"]?.ToString(),
                                UUser = reader["UUser"]?.ToString(),
                                UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : (DateTime?)null,
                                EUSER = reader["EUSER"]?.ToString(),
                                EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : (DateTime?)null,
                                WSID = reader["WSID"]?.ToString(),
                                LIP = reader["LIP"]?.ToString(),
                                LID = reader["LID"]?.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { success = true, data = docDetails });
        }



    }
}
