using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    [SessionAuthorize]
    public class CreditLimitMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public CreditLimitMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
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
            ViewBag.CurrentMenu = "Credit Limit Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/CreditLimitMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public IActionResult GetCreditLimits(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
       {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var creditLimits = new List<CREDIT_LIMIT>(); 
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_CREDIT_LIMIT", conn)) 
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                CREDIT_LIMIT creditLimit = new CREDIT_LIMIT
                                {
                                    COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                    BRANCH_CODE = reader["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(reader["BRANCH_CODE"]) : 0,
                                    YEAR_CODE = reader["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(reader["YEAR_CODE"]) : 0,
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                    DOC_ID = reader["DOC_ID"]?.ToString(),
                                    V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                    PARTY_CODE = reader["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PARTY_CODE"]) : 0,
                                    GR_CODE = reader["GR_CODE"] != DBNull.Value ? Convert.ToInt32(reader["GR_CODE"]) : 0,
                                    CR_LIMIT = reader["CR_LIMIT"] != DBNull.Value ? Convert.ToDecimal(reader["CR_LIMIT"]) : 0,
                                    CR_DAYS = reader["CR_DAYS"] != DBNull.Value ? Convert.ToInt32(reader["CR_DAYS"]) : 0,
                                    //EFF_FROM = reader["EFF_FROM"] != DBNull.Value ? Convert.ToDateTime(reader["EFF_FROM"]) : DateTime.MinValue,
                                    EFF_FROM = reader["EFF_FROM"] != DBNull.Value ? Convert.ToDateTime(reader["EFF_FROM"]) : (DateTime?)null,
                                    REMARKS = reader["REMARKS"]?.ToString(),
                                    FAPROV_STATUS = reader["FAPROV_STATUS"]?.ToString(),
                                    FAPROV_REMARKS = reader["FAPROV_REMARKS"]?.ToString(),
                                    UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue,
                                    EUSER = reader["EUSER"] != DBNull.Value ? Convert.ToInt32(reader["EUSER"]) : 0,
                                    EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : DateTime.MinValue,
                                    AED = reader["AED"]?.ToString(),
                                    WSID = reader["WSID"]?.ToString(),
                                    LIP = reader["LIP"]?.ToString(),
                                    LID = reader["LID"]?.ToString(),
                                    OURCR_DAYS = reader["OURCR_DAYS"] != DBNull.Value ? Convert.ToInt32(reader["OURCR_DAYS"]) : 0
                                };

                                creditLimits.Add(creditLimit);
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching credit limits", error = ex.Message });
            }

            return Json(new { success = true, lists = creditLimits, totalCount });
        }

        [HttpGet]
        public IActionResult GetCreditLimitByCode(int code)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            CREDIT_LIMIT credit = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_CREDIT_LIMIT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                        con.Open();

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                credit = new CREDIT_LIMIT
                                {
                                    COMP_CODE = rdr["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COMP_CODE"]) : 0,
                                    BRANCH_CODE = rdr["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BRANCH_CODE"]) : 0,
                                    YEAR_CODE = rdr["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["YEAR_CODE"]) : 0,
                                    V_TYPE = rdr["V_TYPE"]?.ToString(),
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                                    DOC_ID = rdr["DOC_ID"]?.ToString(),
                                    V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                                    PARTY_CODE = rdr["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_CODE"]) : 0,
                                    GR_CODE = rdr["GR_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["GR_CODE"]) : 0,
                                    CR_LIMIT = rdr["CR_LIMIT"] != DBNull.Value ? Convert.ToDecimal(rdr["CR_LIMIT"]) : 0,
                                    CR_DAYS = rdr["CR_DAYS"] != DBNull.Value ? Convert.ToInt32(rdr["CR_DAYS"]) : 0,
                                    EFF_FROM = rdr["EFF_FROM"] != DBNull.Value ? Convert.ToDateTime(rdr["EFF_FROM"]) : DateTime.MinValue,
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    FAPROV_STATUS = rdr["FAPROV_STATUS"]?.ToString(),
                                    FAPROV_REMARKS = rdr["FAPROV_REMARKS"]?.ToString(),
                                    OURCR_DAYS = rdr["OURCR_DAYS"] != DBNull.Value ? Convert.ToInt32(rdr["OURCR_DAYS"]) : 0
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = credit });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching credit limit", error = ex.Message });
            }
        }


    }
}
 