using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.ModuleService;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static travelexpensemanagement.Controllers.Master.ModuleMasterController;

namespace travelexpensemanagement.Controllers.Master
{
    [SessionAuthorize]
    public class ModuleMasterController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        string StrSystemIP = "", Strwsid = "", tablename, Vtype, description, Lip, Lid;
        int x, companyCd, Vno, StrUser;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public ModuleMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Module Structure";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/SystemInitilization/ModuleMaster/Index.cshtml", model);
        }

        public IActionResult ModuleMast()
        {
            //return View();
            return View("~/Views/Admin/SystemInitilization/ModuleMaster/ModuleMast.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> geModuleMaster()
        {
            try
            {
                const string query = @"
            SELECT  CODE,
                    NAME,
                    DISPLAY_NAME AS DisplayName,
                    POSITION_NO  AS PositionNo,
                    CASE WHEN ISNULL(Active, 0) = 1 THEN 'Yes' ELSE 'No' END AS Active
            FROM    MODULE_MAST
            ORDER BY DisplayName";

                DataTable dt = await _dbHelper.ExecuteQueryAsync(query);

                var result = dt.AsEnumerable()
                               .Select(row => new
                               {
                                   code = row.Field<int>("Code"),
                                   name = row.Field<string>("Name"),
                                   displayName = row.Field<string>("DisplayName"),
                                   positionNo = row.Field<int>("PositionNo"),  // <-- fixed mapping
                                   active = row.Field<string>("Active")
                               })
                               .ToList();

                return Json(new { data = result, status = true });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = $"Data load failed: {ex.Message}"
                });
            }
        }


        [HttpGet]
        public JsonResult getExitOrNot(string doctype)
        {
            try
            {
                bool isExist = false;

                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = @"
                       SELECT CASE 
                        WHEN EXISTS (
                            SELECT 1 
                            FROM MODULE_MAST 
                            WHERE UPPER(ISNULL(NAME, '')) = UPPER(@Inputdata)
                        ) 
                        THEN 1 ELSE 0 END";
                        cmd.Parameters.AddWithValue("@Inputdata", doctype);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        isExist = Convert.ToInt32(result) == 1;
                    }
                }

                return Json(new { status = true, exists = isExist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data check failed: " + ex.Message });
            }
        }

        public class ClsModuleMast
        {
            public int? code { get; set; }
            public string Name { get; set; }
            public string? displayname { get; set; }
            public int? positionNo { get; set; }
            public int? Active { get; set; }

        }


        [HttpPost]
        public JsonResult SaveModuleMastDt([FromBody] ClsModuleMast Moduledt)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    Strwsid = Environment.MachineName;
                    //StrSystemIP = _dbHelper.GetLocalIPAddress();
                    SqlConnection connew = _dbcontext.GetConDbConnection();
                    var sessionData = _globalValue.GetGlobalVariables();
                    StrUser = int.Parse(sessionData.PubUserId);
                    Strwsid = GetWindowsUser();
                    StrSystemIP = sessionData.PubLocalId;

                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_ModuleMast_AED", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Name", Moduledt.Name);
                        cmd.Parameters.AddWithValue("@displayName", Moduledt.displayname);
                        cmd.Parameters.AddWithValue("@position", Moduledt.positionNo);
                        cmd.Parameters.AddWithValue("@Active", Moduledt.Active);
                        cmd.Parameters.AddWithValue("@User", StrUser);
                        cmd.Parameters.AddWithValue("@Lip", StrSystemIP);
                        cmd.Parameters.AddWithValue("@Lid", Strwsid);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        x = cmd.ExecuteNonQuery();

                    }
                }

                return Json(new { status = true, message = "Data Save Successfully" });
            }
            catch (SqlException sqlex)
            {
                return Json(new { status = false, message = sqlex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<JsonResult> UpdateModuleDt([FromBody] ClsModuleMast Moduledt)
        {
            int x = 0;
            int companyCd = 0;
            int StrUser = 0;
            string Lip = string.Empty;
            string StrSystemIP = string.Empty;
            string Strwsid = string.Empty;
            string description = string.Empty;

            bool transactionSucceeded = false;
            DataTable dt = new DataTable();

            using (var con = _dbcontext.GetErpConnection())
            {
                dt = await _dbHelper.ExecuteQueryAsync("SELECT NAME, DISPLAY_NAME, POSITION_NO, ACTIVE FROM MODULE_MAST WHERE CODE = '" + Moduledt.code + "'");
                con.Open();
                var transaction = con.BeginTransaction();

                try
                {
                    var sessionUser = _globalValue.GetGlobalVariables();
                    companyCd = int.Parse(sessionUser.PubCompCode);
                    StrUser = int.Parse(sessionUser.PubUserId);
                    //Lip = _dbHelper.GetLocalIPAddress();
                    StrSystemIP = sessionUser.PubLocalId;
                    Strwsid = GetWindowsUser();

                    if (dt.Rows.Count > 0)
                    {
                        var oldName = dt.Rows[0]["NAME"]?.ToString();
                        var oldDisplayName = dt.Rows[0]["DISPLAY_NAME"]?.ToString();
                        var oldPositionNo = Convert.ToInt32(dt.Rows[0]["POSITION_NO"]);
                        var oldActive = Convert.ToInt32(dt.Rows[0]["ACTIVE"]);

                        var newName = Moduledt.Name?.ToString();
                        var newDisplayName = Moduledt.displayname?.ToString();
                        var newPositionNo = Convert.ToInt32(Moduledt.positionNo);
                        var newActive = Convert.ToInt32(Moduledt.Active);

                        var descriptionBuilder = new StringBuilder();

                        if (oldName != newName)
                            descriptionBuilder.AppendLine($"Name: {oldName} -> {newName}");

                        if (oldDisplayName != newDisplayName)
                            descriptionBuilder.AppendLine($"Display Name: {oldDisplayName} -> {newDisplayName}");

                        if (oldPositionNo != newPositionNo)
                            descriptionBuilder.AppendLine($"Position No: {oldPositionNo} -> {newPositionNo}");

                        if (oldActive != newActive)
                            descriptionBuilder.AppendLine($"Active: {oldActive} -> {newActive}");

                        description = descriptionBuilder.ToString();

                        if (!string.IsNullOrEmpty(description))
                        {
                            using (SqlCommand logCmd = new SqlCommand("sp_LogTable", con, transaction))
                            {
                                logCmd.CommandType = CommandType.StoredProcedure;
                                logCmd.Parameters.AddWithValue("@companyCd", companyCd);
                                logCmd.Parameters.AddWithValue("@tablename", "Module_MAST");
                                logCmd.Parameters.AddWithValue("@VNo", Moduledt.code);
                                logCmd.Parameters.AddWithValue("@description", description);
                                logCmd.Parameters.AddWithValue("@EUser", StrUser);
                                logCmd.Parameters.AddWithValue("@Lip", Lip);
                                logCmd.Parameters.AddWithValue("@Lid", Strwsid);

                                x = logCmd.ExecuteNonQuery();
                            }

                            if (x > 0)
                            {
                                using (SqlCommand updateCmd = new SqlCommand("sp_ModuleMast_AED", con, transaction))
                                {
                                    updateCmd.CommandType = CommandType.StoredProcedure;
                                    updateCmd.Parameters.AddWithValue("@code", Moduledt.code);
                                    updateCmd.Parameters.AddWithValue("@Name", Moduledt.Name);
                                    updateCmd.Parameters.AddWithValue("@displayName", Moduledt.displayname);
                                    updateCmd.Parameters.AddWithValue("@position", Moduledt.positionNo);
                                    updateCmd.Parameters.AddWithValue("@Active", Moduledt.Active);
                                    updateCmd.Parameters.AddWithValue("@User", StrUser);
                                    updateCmd.Parameters.AddWithValue("@Lip", StrSystemIP);
                                    updateCmd.Parameters.AddWithValue("@Lid", Strwsid);
                                    updateCmd.Parameters.AddWithValue("@AED", "E");

                                    x = updateCmd.ExecuteNonQuery();
                                }

                                transaction.Commit();
                                transactionSucceeded = true;
                            }
                        }
                    }

                    return Json(new { status = transactionSucceeded, message = transactionSucceeded ? "Data updated successfully" : "No changes found" });
                }
                catch (Exception ex)
                {
                    if (!transactionSucceeded)
                        transaction.Rollback();

                    return Json(new { status = false, message = "Error: " + ex.Message });
                }
            }
        }

        [HttpDelete]
        public async Task<JsonResult> DelModuleyDt(int code)
        {
            int x = 0;
            bool transactionSucceeded = false;
            DataTable dt = new DataTable();
            using (var con = _dbcontext.GetErpConnection())
            {
                SqlTransaction transaction = null;
                try
                {
                    con.Open(); // ✅ Make sure to open the connection

                    // Get existing module data
                    dt = await _dbHelper.ExecuteQueryAsync(
                        "SELECT NAME, DISPLAY_NAME, POSITION_NO, ACTIVE FROM MODULE_MAST WHERE CODE = " + code);

                    if (dt.Rows.Count > 0)
                    {
                        transaction = con.BeginTransaction(); // ✅ Begin transaction AFTER opening connection

                        var sessionUser = _globalValue.GetGlobalVariables();
                        var companyCd = sessionUser.PubCompCode;
                        var strUser = sessionUser.PubUserId;
                        string strwsid = GetWindowsUser();
                        string lip = sessionUser.PubLocalId;

                        var oldName = dt.Rows[0]["NAME"]?.ToString();
                        var oldDisplayName = dt.Rows[0]["DISPLAY_NAME"]?.ToString();
                        var oldPositionNo = Convert.ToInt32(dt.Rows[0]["POSITION_NO"]);
                        var oldActive = Convert.ToInt32(dt.Rows[0]["ACTIVE"]);

                        var descriptionBuilder = new StringBuilder();
                        descriptionBuilder.AppendLine($"Name={oldName}, DisplayName={oldDisplayName}, PositionNo={oldPositionNo}, Active={oldActive}");
                        string description = descriptionBuilder.ToString();

                        // Log audit
                        using (SqlCommand logCmd = new SqlCommand("sp_LogTable", con, transaction))
                        {
                            logCmd.CommandType = CommandType.StoredProcedure;
                            logCmd.Parameters.AddWithValue("@companyCd", companyCd);
                            logCmd.Parameters.AddWithValue("@tablename", "MODULE_MAST");
                            logCmd.Parameters.AddWithValue("@VNo", code);
                            logCmd.Parameters.AddWithValue("@description", description);
                            logCmd.Parameters.AddWithValue("@EUser", strUser);
                            logCmd.Parameters.AddWithValue("@Lip", lip);
                            logCmd.Parameters.AddWithValue("@Lid", strwsid);

                            x = logCmd.ExecuteNonQuery();
                        }

                        if (x > 0)
                        {
                            using (SqlCommand deleteCmd = new SqlCommand("sp_ModuleMast_AED", con, transaction))
                            {
                                deleteCmd.CommandType = CommandType.StoredProcedure;
                                deleteCmd.Parameters.AddWithValue("@code", code);
                                deleteCmd.Parameters.AddWithValue("@AED", "D");

                                x = deleteCmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            transactionSucceeded = true;
                        }
                        else
                        {
                            transaction.Rollback();
                        }
                    }

                    return Json(new { status = x > 0 });
                }
                catch (Exception ex)
                {
                    if (!transactionSucceeded && transaction != null)
                    {
                        try { transaction.Rollback(); } catch { }
                    }

                    return Json(new { status = false, message = ex.Message });
                }
            }
        }
        public static string GetWindowsUser()
        {
            string fullName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            return fullName.Contains("\\") ? fullName.Split('\\')[1] : fullName;
        }

        public IActionResult ExportAllDocs()
        {
            var compCode = _globalValue.GetGlobalVariables().PubCompCode;
            var docList = new List<Modulelist>();

            using (SqlConnection conn = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_ModuleMast_AED", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@AED", "Export");
                    //cmd.Parameters.AddWithValue("@PageNumber", 1);
                    //cmd.Parameters.AddWithValue("@PageSize", int.MaxValue);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            docList.Add(new Modulelist
                            {
                                Code = reader["Code"].Equals(DBNull.Value) ? 0 : Convert.ToInt32(reader["Code"]),
                                Name = reader["Name"]?.ToString(),
                                DISPLAY_NAME = reader["DISPLAY_NAME"]?.ToString(),
                                POSITION_NO = reader["POSITION_NO"]?.ToString(),
                                Status = reader["Status"]?.ToString(),
                                
                            });
                        }
                    }
                }
            }
            return Json(docList);
        }

        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalValue.GetGlobalVariables();
            List<ItemGroupDetailDto> docDetails = new List<ItemGroupDetailDto>();

            using (SqlConnection conn = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_ModuleMast_AED", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@AED", "DocDetailID");
                    //cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
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

    public class Modulelist
    {
        public int Code { get; set; }
        public string Name { get; set; }
        public string DISPLAY_NAME { get; set; }
        public string POSITION_NO { get; set; }
        public string Status { get; set; }
    }
}
