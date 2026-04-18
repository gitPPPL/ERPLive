using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
 
namespace travelexpensemanagement.Controllers.Admin.Setup
{ 
    public class ApprovalStageMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public ApprovalStageMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "Approval Stages";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Setup/ApprovalStageMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public IActionResult GetAllDocs(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var docList = new List<DOC_APPROSTAGE>();
            int totalCount = 0;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_DOC_APPROSTAGE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@SEARCHDOC", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    // Other optional parameters as NULL
                    cmd.Parameters.AddWithValue("@DOC_CODE", DBNull.Value);
                    //cmd.Parameters.AddWithValue("@DOC_NAME", DBNull.Value);
                    //cmd.Parameters.AddWithValue("@USER_NO", DBNull.Value);
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
                            docList.Add(new DOC_APPROSTAGE
                            {
                                DOC_CODE = reader["DOC_CODE"]?.ToString(),
                                DOC_NAME = reader["DOC_NAME"]?.ToString(),
                                USER_NO = reader["USER_NO"] != DBNull.Value ? Convert.ToInt32(reader["USER_NO"]) : 0,
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = Convert.ToInt32(reader["TotalCount"]);
                        }
                    }
                }
            }

            return Json(new { items = docList, totalCount });
        }

        public JsonResult GetDocByCode(string docCode)
        {
            List<DOC_APPROSTAGE> docList = new List<DOC_APPROSTAGE>();
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_DOC_APPROSTAGE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@DOC_CODE", docCode);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var doc = new DOC_APPROSTAGE
                            {
                                DOC_CODE = reader["DOC_CODE"]?.ToString(),
                                USER_CODE = reader["USER_CODE"] != DBNull.Value ? Convert.ToInt32(reader["USER_CODE"]) : 0,
                                DESIGNATION = reader["DESIGNATION"]?.ToString(),
                                DEPARTMENT = reader["DEPARTMENT"]?.ToString(),
                                APPROV_USER = reader["APPROV_USER"]?.ToString(),
                                FLAG_A = reader["FLAG_A"]?.ToString(),
                                FLAG_B = reader["FLAG_B"]?.ToString(),
                                FLAG_C = reader["FLAG_C"]?.ToString(),
                                FLAG_D = reader["FLAG_D"]?.ToString(),
                                FLAG_E = reader["FLAG_E"]?.ToString(),
                                SRNO = reader["SRNO"] != DBNull.Value ? Convert.ToInt32(reader["SRNO"]) : 0,
                                ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                            };

                            docList.Add(doc);
                        }
                    }
                }
            }
            return Json(docList);
        }

        public JsonResult DeleteDocByCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_DOC_APPROSTAGE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DELETE");
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@DOC_CODE", docCode);
                    cmd.Parameters.AddWithValue("@USER_CODE", globalVar.PubUserId);

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
            var docList = new List<DOC_APPROSTAGE>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_DOC_APPROSTAGE", conn))
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
                            docList.Add(new DOC_APPROSTAGE
                            {
                                DOC_CODE = reader["DOC_CODE"]?.ToString(),
                                DOC_NAME = reader["DOC_NAME"]?.ToString(),
                                USER_NO = reader["USER_NO"] != DBNull.Value ? Convert.ToInt32(reader["USER_NO"]) : 0,
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
            List<DocDetailDto> docDetails = new List<DocDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_DOC_APPROSTAGE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DocDetailID");
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@DOC_CODE", docCode);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detail = new DocDetailDto
                            {
                                DOC_CODE = reader["DOC_CODE"]?.ToString(),
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
