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
    public class GodownMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public GodownMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "Godown Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/GodownMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public IActionResult GetGodowns(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var godowns = new List<GODOWN_MAST>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GODOWN_MAST", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value); // Optional if SP handles NULL correctly

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                godowns.Add(new GODOWN_MAST
                                {
                                    COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    COMP_NAME = reader["COMP_NAME"]?.ToString(),
                                    ADDRESS = reader["ADDRESS"]?.ToString(),
                                    ADDRESS2 = reader["ADDRESS2"]?.ToString(),
                                    CITY = reader["CITY"]?.ToString(),
                                    PINCODE = reader["PINCODE"]?.ToString(),
                                    STATE_CODE = reader["STATE_CODE"]?.ToString(),
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                    SNO = reader["SNO"] != DBNull.Value ? Convert.ToInt32(reader["SNO"]) : 0,
                                    UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue,
                                    EUSER = reader["EUSER"] != DBNull.Value ? Convert.ToInt32(reader["EUSER"]) : 0,
                                    EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : DateTime.MinValue,
                                    AED = reader["AED"]?.ToString(),
                                    WSID = reader["WSID"]?.ToString(),
                                    LIP = reader["LIP"]?.ToString(),
                                    LID = reader["LID"]?.ToString(),
                                    WB_YN = reader["WB_YN"]?.ToString(),
                                    ACTION = null // Set if needed or retrieved elsewhere
                                });

                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }

                return Json(new { success = true, lists = godowns, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching godowns", error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetGodownByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            GODOWN_MAST godown = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GODOWN_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                godown = new GODOWN_MAST
                                {
                                    COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    COMP_NAME = reader["COMP_NAME"]?.ToString(),
                                    ADDRESS = reader["ADDRESS"]?.ToString(),
                                    ADDRESS2 = reader["ADDRESS2"]?.ToString(),
                                    CITY = reader["CITY"]?.ToString(),
                                    PINCODE = reader["PINCODE"]?.ToString(),
                                    STATE_CODE = reader["STATE_CODE"]?.ToString(),
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                    SNO = reader["SNO"] != DBNull.Value ? Convert.ToInt32(reader["SNO"]) : 0,
                                    UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue,
                                    EUSER = reader["EUSER"] != DBNull.Value ? Convert.ToInt32(reader["EUSER"]) : 0,
                                    EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : DateTime.MinValue,
                                    AED = reader["AED"]?.ToString(),
                                    WSID = reader["WSID"]?.ToString(),
                                    LIP = reader["LIP"]?.ToString(),
                                    LID = reader["LID"]?.ToString(),
                                    WB_YN = reader["WB_YN"]?.ToString(),
                                    ACTION = null // Set if needed or retrieved elsewhere
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = godown });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching godown", error = ex.Message });
            }
        }

        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var docList = new List<GODOWNExport>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_GODOWN_MAST", conn))
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
                            docList.Add(new GODOWNExport
                            {
                                CODE = reader["CODE"]?.ToString(),
                                NAME = reader["NAME"]?.ToString(),
                                COMP_NAME = reader["COMP_NAME"]?.ToString(),
                                PINCODE = reader["PINCODE"]?.ToString(),
                                ADDRESS = reader["ADDRESS"]?.ToString(),
                                ADDRESS2 = reader["ADDRESS2"]?.ToString(),
                                CITY = reader["CITY"]?.ToString(),
                                STATE_CODE = reader["STATE_CODE"]?.ToString(),
                                ACTIVE = reader["ACTIVE"]?.ToString(),
                                WB_YN = reader["WB_YN"]?.ToString(),
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
                using (SqlCommand cmd = new SqlCommand("sp_GODOWN_MAST", conn))
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
}
 