using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers
{
    [SessionAuthorize]
    public class CurrencyMasterController : Controller
    {
        private readonly GlobalVariableService _globalValue;
        private readonly DataBaseConnection _dbcontext;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        string StrSystemName = "", StrSystemIP = "";
   
        public CurrencyMasterController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService) 
        {
            _dbcontext = dbcontext;
            _dbHelper = dbHelper;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            //return View();
            ViewBag.CurrentMenu = "Currency Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Setup/CurrencyMaster/Index.cshtml", model);
        }
        public IActionResult CurrencyMast()
        {
            return View("~/Views/Admin/Setup/CurrencyMaster/CurrencyMast.cshtml");
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
                            FROM CURRENCY_MAST 
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
        [HttpGet]
        public async Task<JsonResult> getCurrencyMaster()
        {
            try
            
            {
                var CurrencyList = new List<object>();
                DataTable dt = new DataTable();
               SqlConnection con = _dbcontext.GetErpConnection();
               string strqry = "select CODE, name as Currency , SHORTNAME as shortName, CURR_CODE as CurrencyCode  , case when isnull(Active, 0)=1 then 'Yes' else 'No' end as Active  from CURRENCY_MAST order by Currency ";
               dt =await _dbHelper.ExecuteQueryAsync(strqry);
                foreach (DataRow row in dt.Rows)
                {
                    CurrencyList.Add(new {code= (Int32)row["CODE"], name = row["Currency"].ToString(), shortname = row["shortName"].ToString(), currencyCd = row["CurrencyCode"].ToString() ,active = row["Active"].ToString() });
                }
                return Json(new { status = true, data= CurrencyList });
            }
            catch(Exception ex)
            {
                return Json(new { status = true, message = "Data Load failed" + ex.Message });
            }
        }
        
        [HttpPost]
        public JsonResult saveCurrencyMastDt([FromBody] ClsCurrency Currencydt)
        {
           
            try
            {
                var sessionData = _globalValue.GetGlobalVariables();
               var StrUUser = (sessionData.PubUserId);              
                StrSystemName = Environment.MachineName;
                //StrSystemIP= _dbHelper.GetLocalIPAddress();
                StrSystemIP = sessionData.PubLocalId;
                using (var con=_dbcontext.GetErpConnection())
                {
                    con.Open();
                   
                    using (SqlCommand cmd = new SqlCommand("sp_CurrencyMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@name", Currencydt.Currencyname);
                        cmd.Parameters.AddWithValue("@shortnm", Currencydt.ShortNm);
                        cmd.Parameters.AddWithValue("@currencyCd", Currencydt.currencyCode);
                        cmd.Parameters.AddWithValue("@UUSER", StrUUser);
                        cmd.Parameters.AddWithValue("@Lip",StrSystemIP);
                        cmd.Parameters.AddWithValue("@Lid", StrSystemName);
                        cmd.Parameters.AddWithValue("@active", Currencydt.Active);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { status = true, message = "Data Save Successfully" });
            }
            catch (SqlException sqlex)
            {
              return Json(new {status=false, message=sqlex.Message});
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult UpdateCurrencyDt([FromBody] ClsCurrency Currencydt)
        {
            try
            {
                StrSystemName = Environment.MachineName;
                //StrSystemIP = _dbHelper.GetLocalIPAddress();               
                var sessionData = _globalValue.GetGlobalVariables();
                StrSystemIP = sessionData.PubLocalId;
                var StrEUser = (sessionData.PubUserId); 
                using (var con = _dbcontext.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateCurrencyMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@code", Currencydt.code);
                        cmd.Parameters.AddWithValue("@name", Currencydt.Currencyname);
                        cmd.Parameters.AddWithValue("@shortnm", Currencydt.ShortNm);
                        cmd.Parameters.AddWithValue("@currencyCd", Currencydt.currencyCode);
                        cmd.Parameters.AddWithValue("@Euser", StrEUser);
                        cmd.Parameters.AddWithValue("@Lip", StrSystemIP);
                        cmd.Parameters.AddWithValue("@Lid", StrSystemName);
                        cmd.Parameters.AddWithValue("@active", Currencydt.Active);
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
        [HttpPost]
        public JsonResult DelCurrencyDt(int code)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_DelCurrencyMast", con))
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
            var currencyList = new List<CurrencyExportDto>();
            try
            {
                using (SqlConnection conn = _dbcontext.GetErpConnection())
                {
                    string query = "SELECT Code, Name, SHORTNAME, CURR_CODE, ACTIVE FROM CURRENCY_MAST";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                currencyList.Add(new CurrencyExportDto
                                {
                                    Code = reader["Code"]?.ToString(),
                                    Name = reader["Name"]?.ToString(),
                                    ShortName = reader["SHORTNAME"]?.ToString(),
                                    CurrCode = reader["CURR_CODE"]?.ToString(),
                                    Active = reader["ACTIVE"] != DBNull.Value && Convert.ToBoolean(reader["ACTIVE"])
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
                string query = @"SELECT DISTINCT da.Code, um.USER_NAME as UUser, da.UDATE, ume.USER_NAME as EUSER, da.EDATE, 
                da.WSID, da.LIP, da.LID FROM CURRENCY_MAST da
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
    public class CurrencyExportDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string CurrCode { get; set; }
        public bool Active { get; set; }
    }
    public class ClsCurrency
    {
        public int code { get; set; }
        public string Currencyname { get; set; }
        public string ShortNm { get; set; }
        public string currencyCode { get; set; }
        public int Active { get; set; }

    }
}

