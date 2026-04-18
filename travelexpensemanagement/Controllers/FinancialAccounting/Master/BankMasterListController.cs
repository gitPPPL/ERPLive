using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    [SessionAuthorize]
    public class BankMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public BankMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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

            ViewBag.CurrentMenu = "Bank Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/BankMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public IActionResult GetBanks(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var banks = new List<BANK_MAST>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_BANK_MAST", conn)) // Make sure SP exists
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
                                banks.Add(new BANK_MAST
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                    TYPE = reader["TYPE"]?.ToString(),
                                    REPL_CODE = reader["REPL_CODE"] != DBNull.Value ? Convert.ToInt32(reader["REPL_CODE"]) : 0,
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                                });
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
                return Json(new { success = false, message = "Error fetching banks", error = ex.Message });
            }

            return Json(new { success = true, lists = banks, totalCount });
        }

        [HttpGet]
        public IActionResult GetBankByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            BANK_MAST bank = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_BANK_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);

                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                bank = new BANK_MAST
                                {
                                    CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    NAME = rdr["NAME"]?.ToString(),
                                    SHORTNAME = rdr["SHORTNAME"]?.ToString(),
                                    TYPE = rdr["TYPE"]?.ToString(),
                                    REPL_CODE = rdr["REPL_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["REPL_CODE"]) : 0,
                                    ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0,
                                    UUSER = rdr["UUSER"] != DBNull.Value ? Convert.ToInt32(rdr["UUSER"]) : 0,
                                    UDATE = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : DateTime.MinValue,
                                    EUSER = rdr["EUSER"] != DBNull.Value ? Convert.ToInt32(rdr["EUSER"]) : 0,
                                    EDATE = rdr["EDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EDATE"]) : DateTime.MinValue,
                                    AED = rdr["AED"]?.ToString(),
                                    WSID = rdr["WSID"]?.ToString(),
                                    LIP = rdr["LIP"]?.ToString(),
                                    LID = rdr["LID"]?.ToString()
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = bank });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching bank", error = ex.Message });
            }
        }


        public JsonResult DeleteDocByCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_BANK_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DELETE");
                    cmd.Parameters.AddWithValue("@CODE", docCode);

                    conn.Open();
                    cmd.ExecuteNonQuery(); // Execute the DELETE command
                }
            }
            return Json(new { success = true, message = "Record deleted successfully." });
        }

        // Downlod File in Excel
        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var docList = new List<BankMasterExport>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_BANK_MAST", conn))
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
                            docList.Add(new BankMasterExport
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                NAME = reader["NAME"]?.ToString(),
                                SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                TYPE = reader["TYPE"]?.ToString(),
                                STATUS = reader["STATUS"]?.ToString()
                                
                            });
                        }
                    }
                }
            }
            return Json(docList);
        }
        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<ItemGroupDetailDto> docDetails = new List<ItemGroupDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_BANK_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DocDetailID");
                    cmd.Parameters.AddWithValue("@Code", docCode);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detail = new ItemGroupDetailDto
                            {
                                Code = reader["Code"]?.ToString(),
                                UUser = reader["UUser"]?.ToString(),
                                UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : (DateTime?)null,
                                EUSER = reader["EUSER"]?.ToString(),
                                EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : (DateTime?)null,
                                WSID = reader["WSID"]?.ToString(),
                                LIP = reader["LIP"]?.ToString(),
                                LID = reader["LID"]?.ToString()
                            };
                            docDetails.Add(detail);
                        }
                    }
                }
            }

            return Json(new { success = true, data = docDetails });
        }


    }

    public class BankMasterExport
    {
        public int? CODE { get; set; }
        public string NAME { get; set; }
        public string? SHORTNAME { get; set; }
        public string? TYPE { get; set; }
        public string? STATUS { get; set; }
       
    }
}
