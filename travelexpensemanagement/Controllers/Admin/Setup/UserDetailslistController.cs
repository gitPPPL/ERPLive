using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class UserDetailslistController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalVariableService _globalVariableService;
        public UserDetailslistController(DataBaseConnection dbConnection, ModuleService.ModuleService moduleService, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _moduleService = moduleService;
            _globalVariableService = globalVariableService;
        }
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("USER_NAME") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.CurrentMenu = "User Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            //return View(model);
            return View("~/Views/Admin/Setup/UserDetailslist/Index.cshtml", model);
        }
        [HttpGet]
        public JsonResult GetUserDetailslist(string searchTerm = "", int page = 1, int pageSizeJourneyDetails = 10)
        {
            List<UserDetailslist> users = new List<UserDetailslist>();
            int totalCount = 0;

            try
            {
                using (SqlConnection con = _dbConnection.GetConDbConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetAllUsers", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Pass DBNull.Value if searchTerm is empty
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", page);
                        cmd.Parameters.AddWithValue("@PageSize", pageSizeJourneyDetails);

                        con.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                users.Add(new UserDetailslist
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    USER_NAME = reader["USER NAME"]?.ToString(),
                                    FULL_NAME = reader["FULL NAME"]?.ToString(),
                                    DESIGNATION = reader["DESIGNATION"]?.ToString(),
                                    DEPARTMENT = reader["DEPARTMENT"]?.ToString(),
                                    //DEPT_CODE = reader["DEPTT CODE"]?.ToString(),
                                    EMP_CODE = reader["EMP CODE"] != DBNull.Value ? Convert.ToInt32(reader["EMP CODE"]) : 0,
                                    //USER_LEVEL = reader["USER LEVEL"]?.ToString(),
                                    PCName1 = reader["PCName1"]?.ToString(),
                                    PCName2 = reader["PCName2"]?.ToString(),
                                    ACTIVE = reader["ACTIVE"]?.ToString(),
                                    ALLOW_DAYS = reader["ALLOW DAYS"] != DBNull.Value ? Convert.ToInt32(reader["ALLOW DAYS"]) : 0,
                                    PASSWORD_NEVER_EXPIRED = reader["PASSWORD NEVER EXPIRED"]?.ToString(),
                                    PASSWORD_CHANGE_ON_NEXT_LOGIN = reader["PASSWORD CHANGE ON NEXT LOGIN"]?.ToString()
                                });
                            }

                            // Read total count from next result set
                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }

                return Json(new { success = true, travelRequests = users, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GetDepartmentsddl()
        {
            List<object> departments = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code, Name  From DEPT_MAST";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    departments.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }
            }
            return Json(departments);
        }
        [HttpGet]
        public JsonResult GetDesignationddl()
        {
            List<object> designation = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code, Name From DESG_MAST";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    designation.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }
            }
            return Json(designation);
        }
        [HttpGet]
        public JsonResult GetDashboardNameddl()
        {
            List<object> designation = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code, DISPLAY_NAME From MENU_MAST where MODULE_CODE=17";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    designation.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["DISPLAY_NAME"].ToString()
                    });
                }
            }
            return Json(designation);
        }
        public JsonResult DeleteDocByCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbConnection.GetConDbConnection())
                {
                    string query = "DELETE FROM USER_MAST WHERE COMP_CODE = @COMP_CODE AND CODE = @Code";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", docCode);

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            return Json(new { success = false, message = "No record found to delete." });
                        }
                    }
                }
                return Json(new { success = true, message = "Record deleted successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception as needed
                return Json(new { success = false, message = "Error deleting record.", error = ex.Message });
            }
        }
        public IActionResult ExportAllDocs()
        {
            var userList = new List<UserExportDto>();
            try
            {
                using (SqlConnection conn = _dbConnection.GetConDbConnection())
                {
                    string query = @"SELECT Code, FULL_NAME, DESIGNATION, DEPARTMENT, EMP_CODE, 
                             PC_NAME, PC_NAME2, PC_NAME3, 
                             CASE WHEN ACTIVE = 1 THEN 'Active' 
                                  WHEN ACTIVE = 0 THEN 'Inactive' 
                                  ELSE 'Unknown' END AS Status 
                             FROM USER_MAST";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                userList.Add(new UserExportDto
                                {
                                    Code = reader["Code"]?.ToString(),
                                    FullName = reader["FULL_NAME"]?.ToString(),
                                    Designation = reader["DESIGNATION"]?.ToString(),
                                    Department = reader["DEPARTMENT"]?.ToString(),
                                    EmpCode = reader["EMP_CODE"]?.ToString(),
                                    PcName1 = reader["PC_NAME"]?.ToString(),
                                    PcName2 = reader["PC_NAME2"]?.ToString(),
                                    PcName3 = reader["PC_NAME3"]?.ToString(),
                                    Status = reader["Status"]?.ToString()
                                });
                            }
                        }
                    }
                }
                return Json(userList);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while exporting user data.",
                    error = ex.Message
                });
            }
        }
        public JsonResult DocDetailsCode(string docCode)
        {
            List<ItemGroupDetailDto> docDetails = new List<ItemGroupDetailDto>();
            using (SqlConnection conn = _dbConnection.GetConDbConnection())
            {
                string query = @"SELECT DISTINCT da.Code, um.USER_NAME as UUser, da.UDATE, ume.USER_NAME as EUSER, da.EDATE, 
          da.WSID, da.LIP, da.LID FROM USER_MAST da
          LEFT JOIN CONDATABASE..USER_MAST um ON da.UUSER = um.CODE
          LEFT JOIN CONDATABASE..USER_MAST ume ON da.EUSER = ume.CODE
          WHERE da.Code = @Code";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
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


}
