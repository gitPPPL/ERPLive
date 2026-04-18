using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class BSSchaduleMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection; 
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public BSSchaduleMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "Schedule Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Setup/BSSchaduleMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public IActionResult GetAllSchedules(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var scheduleList = new List<BS_SCH_MAST>();
            int totalCount = 0;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_BS_SCH_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    //cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    // Fill rest as DBNull
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@NAME", DBNull.Value);
                    cmd.Parameters.AddWithValue("@SORT_SR_NO", DBNull.Value);
                    cmd.Parameters.AddWithValue("@SCH_NO", DBNull.Value);
                    cmd.Parameters.AddWithValue("@ACTIVE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LID", DBNull.Value);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            scheduleList.Add(new BS_SCH_MAST
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                NAME = reader["NAME"]?.ToString(),
                                SORT_SR_NO = reader["SORT_SR_NO"] != DBNull.Value ? Convert.ToInt32(reader["SORT_SR_NO"]) : 0,
                                SCH_NO = reader["SCH_NO"]?.ToString(),
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

            return Json(new { schedules = scheduleList, totalCount });
        }

        [HttpGet]
        public JsonResult GetScheduleByCode(int code)
        {
            BS_SCH_MAST schedule = null;
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_BS_SCH_MAST", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    //cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@CODE", code);

                    // Others can be NULL
                    cmd.Parameters.AddWithValue("@NAME", DBNull.Value);
                    cmd.Parameters.AddWithValue("@SORT_SR_NO", DBNull.Value);
                    cmd.Parameters.AddWithValue("@SCH_NO", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@AED", DBNull.Value);
                    cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@ACTIVE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@SearchTerm", DBNull.Value);
                    cmd.Parameters.AddWithValue("@PageNumber", 1);
                    cmd.Parameters.AddWithValue("@PageSize", 1);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            schedule = new BS_SCH_MAST
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                NAME = reader["NAME"]?.ToString(),
                                SORT_SR_NO = reader["SORT_SR_NO"] != DBNull.Value ? Convert.ToInt32(reader["SORT_SR_NO"]) : 0,
                                SCH_NO = reader["SCH_NO"]?.ToString(),
                                ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                            };
                        }
                    }
                }
            }

            return Json(schedule);
        }
        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var docList = new List<BS_SCH_MAST>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_BS_SCH_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Export");
                    cmd.Parameters.AddWithValue("@PageNumber", 1);
                    cmd.Parameters.AddWithValue("@PageSize", int.MaxValue);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            docList.Add(new BS_SCH_MAST
                            {
                                CODE = Convert.ToInt32(reader["Code"]),
                                NAME = reader["Name"]?.ToString(),
                                SORT_SR_NO = Convert.ToInt32(reader["SORT_SR_NO"]),
                                ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
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
                using (SqlCommand cmd = new SqlCommand("sp_BS_SCH_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DocDetailID");
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
