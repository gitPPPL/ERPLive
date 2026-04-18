using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class LcMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public LcMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "LC Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/LcMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetAllLcMasters(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var lcMasters = new List<object>(); // anonymous type or create a DTO if needed
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_LC_MAST", conn)) // Ensure this SP supports paging
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lcMasters.Add(new
                                {
                                    code = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    lcNo = reader["LC_NO"]?.ToString(),
                                    lcDate = reader["LC_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["LC_DATE"]).ToString("yyyy-MM-dd") : "",
                                    lcBankName = reader["LC_BANKNAME"]?.ToString(),
                                    bankAddressL1 = reader["LC_BANKADDL1"]?.ToString(),
                                    bankAddressL2 = reader["LC_BANKADDL2"]?.ToString(),
                                    bankAddressL3 = reader["LC_BANKADDL3"]?.ToString(),
                                    lcTerms = reader["LC_TERMS"]?.ToString()
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }

                return Json(new { success = true, lists = lcMasters, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching LC Master list", error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetLcMasterByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            LC_MAST lcMaster = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_LC_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                lcMaster = new LC_MAST
                                {
                                    CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    LC_NO = rdr["LC_NO"]?.ToString(),
                                    LC_DATE = rdr["LC_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["LC_DATE"]) : DateTime.MinValue,
                                    LC_BANKNAME = rdr["LC_BANKNAME"]?.ToString(),
                                    LC_BANKADDL1 = rdr["LC_BANKADDL1"]?.ToString(),
                                    LC_BANKADDL2 = rdr["LC_BANKADDL2"]?.ToString(),
                                    LC_BANKADDL3 = rdr["LC_BANKADDL3"]?.ToString(),
                                    LC_TERMS = rdr["LC_TERMS"]?.ToString(),
                                    // Optionally handle ACTIVE if you include it in your model
                                };
                            }
                        }
                    }
                }

                if (lcMaster == null)
                    return Json(new { success = false, message = "LC Master not found." });

                return Json(new { success = true, data = lcMaster });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching LC Master by code", error = ex.Message });
            }
        }
        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var docList = new List<LCExport>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_LC_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Export");
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@PageNumber", 1);
                    cmd.Parameters.AddWithValue("@PageSize", int.MaxValue);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            docList.Add(new LCExport
                            {
                                CODE = reader["CODE"]?.ToString(),
                                LC_NO = reader["LC_NO"]?.ToString(),
                                LC_DATE = reader["LC_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["LC_DATE"]) : (DateTime?)null,
                                LC_BANKNAME = reader["LC_BANKNAME"]?.ToString(),
                                LC_BANKADDL1 = reader["LC_BANKADDL1"]?.ToString(),
                                LC_BANKADDL2 = reader["LC_BANKADDL2"]?.ToString(),
                                LC_BANKADDL3 = reader["LC_BANKADDL3"]?.ToString(),
                                LC_TERMS = reader["LC_TERMS"]?.ToString(),
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
                using (SqlCommand cmd = new SqlCommand("sp_LC_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DocDetailID");
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", docCode);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detail = new ItemGroupDetailDto
                            {
                                Code = reader["CODE"]?.ToString(),
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
    public class LCExport
    {
        public string CODE { get; set; }
        public string LC_NO { get; set; }
        public DateTime? LC_DATE { get; set; }
        public string LC_BANKNAME { get; set; }
        public string LC_BANKADDL1 { get; set; }
        public string LC_BANKADDL2 { get; set; }
        public string LC_BANKADDL3 { get; set; }
        public string LC_TERMS { get; set; }
    }

}
