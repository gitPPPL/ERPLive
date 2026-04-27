using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.ModuleService;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
namespace travelexpensemanagement.Controllers.Admin.SystemInitilization
{
    [SessionAuthorize]
    public class EmailSettingList : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalVariableService _globalVariableService;

        public EmailSettingList(DataBaseConnection dbConnection, DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalVariableService = globalVariableService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Email Setting";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/SystemInitilization/EmailSettingList/Index.cshtml", model);
        }
        [HttpGet]
        public JsonResult GetPagedEmailSettings(int page = 1, int pageSize = 10)
        {
            try
            {
                List<object> emailSettingList = new List<object>();
                int totalCount = 0;
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    // Count total distinct records
                    string countQuery = @"SELECT COUNT(*) FROM (SELECT COMP_CODE, CODE, USER_ID, WEBPASSWORD, SMTP_SERVER, SMTP_PORT, SMTP_USSL FROM EMAIL_SETTING1
                    GROUP BY COMP_CODE, CODE, USER_ID, WEBPASSWORD, SMTP_SERVER, SMTP_PORT, SMTP_USSL) AS DistinctData";

                    using (SqlCommand countCmd = new SqlCommand(countQuery, con))
                    {
                        totalCount = (int)countCmd.ExecuteScalar();
                    }

                    // Paginated distinct result
                    string query = $@"
                SELECT * FROM (SELECT COMP_CODE, CODE, USER_ID, WEBPASSWORD, SMTP_SERVER, SMTP_PORT, SMTP_USSL, ROW_NUMBER() OVER (ORDER BY USER_ID) AS RowNum
                    FROM EMAIL_SETTING1 GROUP BY COMP_CODE, CODE, USER_ID, WEBPASSWORD, SMTP_SERVER, SMTP_PORT, SMTP_USSL) AS Result
                WHERE RowNum BETWEEN {(page - 1) * pageSize + 1} AND {page * pageSize}
                ORDER BY RowNum";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                emailSettingList.Add(new
                                {
                                    COMPCODE = reader["COMP_CODE"]?.ToString(),
                                    CODEID = reader["CODE"]?.ToString(),
                                    userID = reader["USER_ID"]?.ToString(),
                                    webPassword = reader["WEBPASSWORD"]?.ToString(),
                                    smtpServer = reader["SMTP_SERVER"]?.ToString(),
                                    smtpPort = reader["SMTP_PORT"]?.ToString(),
                                    smtpUssl = reader["SMTP_USSL"]?.ToString()
                                });
                            }
                        }
                    }
                }

                return Json(new { success = true, emailSettingList, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        public JsonResult DeleteDocByCode(int docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "DELETE FROM EMAIL_SETTING1 WHERE COMP_CODE = @CompCode AND CODE = @Code";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@Code", docCode);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = "Email setting deleted successfully." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "No record found to delete." });
                    }
                }
            }
        }

        public IActionResult ExportAllDocs()
        {
            var emailSettingsList = new List<EmailSettingExportDto>();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    string query = @"SELECT * FROM (SELECT CODE, USER_ID, WEBPASSWORD, SMTP_SERVER, SMTP_PORT, SMTP_USSL, ROW_NUMBER() OVER (ORDER BY USER_ID) AS RowNum
                    FROM EMAIL_SETTING1 GROUP BY COMP_CODE, CODE, USER_ID, WEBPASSWORD, SMTP_SERVER, SMTP_PORT, SMTP_USSL) AS Result ORDER BY RowNum";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                emailSettingsList.Add(new EmailSettingExportDto
                                {
                                    Code = reader["CODE"]?.ToString(),
                                    UserId = reader["USER_ID"]?.ToString(),
                                    WebPassword = reader["WEBPASSWORD"]?.ToString(),
                                    SmtpServer = reader["SMTP_SERVER"]?.ToString(),
                                    SmtpPort = reader["SMTP_PORT"]?.ToString(),
                                    SmtpUssl = reader["SMTP_USSL"]?.ToString(),
                                    RowNum = reader["RowNum"] != DBNull.Value ? Convert.ToInt32(reader["RowNum"]) : 0
                                });
                            }
                        }
                    }
                }
                return Json(new
                {
                    success = true,
                    data = emailSettingsList
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while exporting email settings.",
                    error = ex.Message
                });
            }
        }

        public JsonResult DocDetailsCode(string docCode)
        {
            List<DocDetailDto> docDetails = new List<DocDetailDto>();
            var globalVar = _globalVariableService.GetGlobalVariables();
            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = @"Select DISTINCT da.Code,um.USER_NAME as UUser,da.UDATE,ume.USER_NAME as EUSER,da.EDATE,da.WSID,da.LIP,da.LID 
                from EMAIL_SETTING1 da left Join CONDATABASE..USER_MAST um on da.UUSER= um.CODE left Join CONDATABASE..USER_MAST ume on da.EUSER= ume.CODE  
                where da.COMP_CODE=@COMP_CODE and da.Code=@Code  ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", docCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detail = new DocDetailDto
                            {
                                DOC_CODE = reader["Code"]?.ToString(),
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
    public class EmailSettingExportDto
    {
        public string Code { get; set; }
        public string UserId { get; set; }
        public string WebPassword { get; set; }
        public string SmtpServer { get; set; }
        public string SmtpPort { get; set; }  // Changed to string to handle any value
        public string SmtpUssl { get; set; }  // Changed to string to handle any value
        public int RowNum { get; set; }
    }






}
