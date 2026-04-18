using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.ModuleService;


namespace travelexpensemanagement.Controllers
{
    public class LanguageMasterController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        string StrSystemName = "", StrSystemIP = "";
        public LanguageMasterController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Language Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Setup/LanguageMaster/Index.cshtml", model);
        }
        public IActionResult LanguageMaster()
        {
            //return View();
            return View("~/Views/Admin/Setup/LanguageMaster/LanguageMaster.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> getLanguageMaster()
        {
            try            
            {
                var LanguagedtList = new List<object>();
                DataTable dt = new DataTable();
               SqlConnection con = _dbcontext.GetErpConnection();
               string strqry = "select code ,name , SHORTNAME  , case when isnull(Active, 0)=1 then 'Yes' else 'No' end as Active from LANGUAGE_MAST order by NAME";
                dt =await _dbHelper.ExecuteQueryAsync(strqry);
                foreach (DataRow row in dt.Rows)
                {
                    LanguagedtList.Add(new { code = (Int32)row["code"] ,name = row["name"].ToString(), shortname = row["SHORTNAME"].ToString(), active = row["Active"].ToString() });
                }
                return Json(new { status = true, data= LanguagedtList });
            }
            catch(Exception ex)
            {
                return Json(new { status = true, message = "Daata Load failed" + ex.Message });
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
                            FROM LANGUAGE_MAST 
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
        [HttpPost]
        public JsonResult savelanguageMastDt([FromBody] ClsLanguage languagedt)
        {
            try
            {
                using(var con=_dbcontext.GetErpConnection())
                {
                    StrSystemName = Environment.MachineName;
                    //StrSystemIP = _dbHelper.GetLocalIPAddress();                    
                    SqlConnection connew = _dbcontext.GetConDbConnection();
                    var sessionData=_globalValue.GetGlobalVariables();
                    var StrUUser = sessionData.PubUserId;
                    StrSystemIP = sessionData.PubLocalId;
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_LanguageMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@name", languagedt.Name);
                        cmd.Parameters.AddWithValue("@shortnm", languagedt.ShortNm);
                        cmd.Parameters.AddWithValue("@Uuser", StrUUser);
                        cmd.Parameters.AddWithValue("@Lip", StrSystemIP);
                        cmd.Parameters.AddWithValue("@Lid", StrSystemName);
                        cmd.Parameters.AddWithValue("@active", languagedt.Active);
                        cmd.ExecuteNonQuery();
                        
                    }
                }

                return Json(new { status = true, message = "Data Save Successfully" });
            }
            catch (SqlException sqlex)
            {
              return Json(new { status = false, message=sqlex.Message});
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult UpdateLanguageDt([FromBody] ClsLanguage languagedt)
        {
            try
            {
                StrSystemName = Environment.MachineName;
                //StrSystemIP = _dbHelper.GetLocalIPAddress(); 
                var sessionData = _globalValue.GetGlobalVariables();
               var StrUUser = sessionData.PubUserId;
                StrSystemIP=sessionData.PubLocalId;
                using (var con = _dbcontext.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateLanguageMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@code", languagedt.code);
                        cmd.Parameters.AddWithValue("@name", languagedt.Name);
                        cmd.Parameters.AddWithValue("@shortnm", languagedt.ShortNm);
                        cmd.Parameters.AddWithValue("@Euser", StrUUser);
                        cmd.Parameters.AddWithValue("@Lip", StrSystemIP);
                        cmd.Parameters.AddWithValue("@Lid", StrSystemName);
                        cmd.Parameters.AddWithValue("@active", languagedt.Active);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { status = true, message = "Data Update Successfully" });
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
        [HttpDelete]
        public JsonResult DellanguageyDt(int code)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_DelLanguageMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@code", code);
                        int x = cmd.ExecuteNonQuery();
                        if (x > 0)
                            return Json(new { status = true });
                        else
                            return Json(new { status = false });

                    }
                }
            }
            catch { return Json(new { status = false }); }

        }
        public IActionResult ExportAllDocs()
        {
            var currencyList = new List<LanguageExportDto>();
            try
            {
                using (SqlConnection conn = _dbcontext.GetErpConnection())
                {
                    string query = "Select code, Name,SHORTNAME,CASE WHEN ACTIVE = 1 THEN 'Active' WHEN ACTIVE = 0 THEN 'Inactive' ELSE 'Unknown' END AS ACTIVE from LANGUAGE_MAST";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                currencyList.Add(new LanguageExportDto
                                {
                                    Code = reader["Code"]?.ToString(),
                                    Name = reader["Name"]?.ToString(),
                                    SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                    Active = reader["ACTIVE"]?.ToString(),
                                });
                            }
                        }
                    }
                }
                return Json(currencyList);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while exporting currency data.",
                    error = ex.Message
                });
            }
        }
        public JsonResult DocDetailsCode(string docCode)
        {
            List<DocDetailDto> docDetails = new List<DocDetailDto>();
            using (SqlConnection conn = _dbcontext.GetErpConnection())
            {
                string query = @"Select DISTINCT da.Code,um.USER_NAME as UUser,da.UDATE,ume.USER_NAME as EUSER,da.EDATE,da.WSID,da.LIP,da.LID 
                from LANGUAGE_MAST da  
                left Join CONDATABASE..USER_MAST um on da.UUSER= um.CODE   
                left Join CONDATABASE..USER_MAST ume on da.EUSER= ume.CODE  
                where  da.Code=@Code ";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", docCode);
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
    public class LanguageExportDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string SHORTNAME { get; set; }
        public string Active { get; set; } //
    }
    public class ClsLanguage
    {
        public int? code { get; set; }
        public string Name { get; set; }
        public string? ShortNm { get; set; }
        public int? Active { get; set; }

    }
}

