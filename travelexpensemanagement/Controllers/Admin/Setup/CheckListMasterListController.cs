using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class CheckListMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public CheckListMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "Check List Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Setup/CheckListMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetAllCheckLists(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var checkListItems = new List<CHECK_LIST>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_CheckList", conn)) // Your actual stored procedure name
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        // Optional dummy parameters for compatibility
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@NATURE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@CHECKLIST_NAME", DBNull.Value);
                        cmd.Parameters.AddWithValue("@TASK_NAME", DBNull.Value);
                        cmd.Parameters.AddWithValue("@RESPONSIBLE_USER", DBNull.Value);
                        cmd.Parameters.AddWithValue("@APPROVAL_USER", DBNull.Value);
                        cmd.Parameters.AddWithValue("@DUE_DATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@FREQUENCY_CODE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@ALERT_DAYS", DBNull.Value);
                        cmd.Parameters.AddWithValue("@ALERT_DAYS2", DBNull.Value);
                        cmd.Parameters.AddWithValue("@STATUS", DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARKS", DBNull.Value);
                        //cmd.Parameters.AddWithValue("@ACTIVE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                        cmd.Parameters.AddWithValue("@UDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AED", DBNull.Value);
                        cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                        cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                        cmd.Parameters.AddWithValue("@LID", DBNull.Value);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                checkListItems.Add(new CHECK_LIST
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NATURE = reader["NATURE"]?.ToString(),
                                    CHECKLIST_NAME = reader["CHECKLIST_NAME"]?.ToString(),
                                    TASK_NAME = reader["TASK_NAME"]?.ToString(),
                                    RESPONSIBLE_USER = reader["RESPONSIBLE_USER"] != DBNull.Value ? Convert.ToInt32(reader["RESPONSIBLE_USER"]) : (int?)null,
                                    APPROVAL_USER = reader["APPROVAL_USER"] != DBNull.Value ? Convert.ToInt32(reader["APPROVAL_USER"]) : (int?)null,
                                    DUE_DATE = reader["DUE_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["DUE_DATE"]) : (DateTime?)null,
                                    FREQUENCY_CODE = reader["FREQUENCY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["FREQUENCY_CODE"]) : (int?)null,
                                    FREQUENCY = reader["FREQUENCY"]?.ToString(),
                                    REMARKS = reader["REMARKS"]?.ToString(),
                                    STATUS = reader["STATUS"]?.ToString(),
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
                return Json(new { success = false, message = "Error fetching checklists", error = ex.Message });
            }

            return Json(new { success = true, lists = checkListItems, totalCount });
        }

        public CHECK_LIST GetCheckListByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            CHECK_LIST checklist = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_CheckList", con)) // Update SP name if different
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
                            checklist = new CHECK_LIST
                            {
                                CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                COMP_CODE = rdr["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COMP_CODE"]) : 0,
                                NATURE = rdr["NATURE"]?.ToString(),
                                CHECKLIST_NAME = rdr["CHECKLIST_NAME"]?.ToString(),
                                TASK_NAME = rdr["TASK_NAME"]?.ToString(),
                                RESPONSIBLE_USER = rdr["RESPONSIBLE_USER"] != DBNull.Value ? Convert.ToInt32(rdr["RESPONSIBLE_USER"]) : (int?)null,
                                APPROVAL_USER = rdr["APPROVAL_USER"] != DBNull.Value ? Convert.ToInt32(rdr["APPROVAL_USER"]) : (int?)null,
                                DUE_DATE = rdr["DUE_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["DUE_DATE"]) : (DateTime?)null,
                                FREQUENCY_CODE = rdr["FREQUENCY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["FREQUENCY_CODE"]) : (int?)null,
                                FREQUENCY = rdr["FREQUENCY"]?.ToString(),
                                ALERT_DAYS = rdr["ALERT_DAYS"] != DBNull.Value ? Convert.ToInt32(rdr["ALERT_DAYS"]) : 0,
                                ALERT_DAYS2 = rdr["ALERT_DAYS2"] != DBNull.Value ? Convert.ToInt32(rdr["ALERT_DAYS2"]) : 0,
                                STATUS = rdr["STATUS"]?.ToString(),
                                REMARKS = rdr["REMARKS"]?.ToString(),
                                //ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0
                            };
                        }
                    }
                }
            }

            return checklist;
        }
        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var Checklist = new List<ChecklistExportDTO>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_CheckList", conn))
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
                            Checklist.Add(new ChecklistExportDTO
                            {
                                CODE = reader["Code"] != DBNull.Value ? Convert.ToInt32(reader["Code"]) : 0,
                                NATURE = reader["NATURE"]?.ToString(),
                                CHECKLIST_NAME = reader["CHECKLIST_NAME"]?.ToString(),
                                TASK_NAME = reader["TASK_NAME"]?.ToString(),
                                RESPONSIBLE_USER = reader["RESPONSIBLE_USER"]?.ToString(),
                                APPROVAL_USER = reader["APPROVAL_USER"]?.ToString(),
                                FREQUENCY = reader["FREQUENCY"]?.ToString(),
                                DUE_DATE = reader["DUE_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["DUE_DATE"]) : (DateTime?)null,
                                REMARKS = reader["REMARKS"]?.ToString(),
                                STATUS = reader["STATUS"]?.ToString(),
                            });
                        }
                    }
                }
            }

            return Json(Checklist);
        }
        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var CHECKLISTDetails = new List<ChecklistExportDocdetails>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_CheckList", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DocDetailID");
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                    if (int.TryParse(docCode, out int code))
                    {
                        cmd.Parameters.AddWithValue("@CODE", code);
                    }
                    else
                    {
                        return Json(new { success = false, message = "Invalid document code." });
                    }
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CHECKLISTDetails.Add(new ChecklistExportDocdetails
                            {
                                CODE = reader["Code"] != DBNull.Value ? Convert.ToInt32(reader["Code"]) : 0,
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

            return Json(new { success = true, data = CHECKLISTDetails });
        }
    }
}
