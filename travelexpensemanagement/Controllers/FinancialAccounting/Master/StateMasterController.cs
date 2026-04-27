using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Security.Cryptography.X509Certificates;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using static travelexpensemanagement.Controllers.Master.CountryMasterController;


namespace travelexpensemanagement.Controllers.Master
{
    public class StateMasterController : Controller
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly DbHelper _dbHelper;
        private readonly GlobalVariableService _globalVariableService;
        int x;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public StateMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalVariableService globalVariableService)
        {
            _dbcontext = dbcontext;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalVariableService = globalVariableService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "State Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); 

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/StateMaster/Index.cshtml", model);
        }

        public IActionResult StateMast()
        {
            return View("~/Views/FinancialAccounting/Master/StateMaster/StateMast.cshtml");
            //return View("~/Views/FincialAccounting/Master/StateMaster/StateMast.cshtml");
        }

        [HttpGet]
        public JsonResult getExitOrNot(string inputData)
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
                            FROM STATE_MAST 
                            WHERE UPPER(ISNULL(NAME, '')) = UPPER(@Inputdata)
                        ) 
                        THEN 1 ELSE 0 END";
                        cmd.Parameters.AddWithValue("@Inputdata", inputData);
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
        public async Task<JsonResult> GetCountryName()
        {
            DataTable dtState = new DataTable();
            SqlConnection con = _dbcontext.GetErpConnection();
            var countryList = new List<object>();
            try
            {
                string strqry = "select code countrycode, name as countryname from dbo.COUNTRY_MAST order by NAME ";
                dtState =await _dbHelper.ExecuteQueryAsync(strqry);

                foreach (DataRow row in dtState.Rows)
                {
                    countryList.Add(new { id = row["countrycode"].ToString(), country = row["countryname"].ToString() });
                }
                return Json(new { status = true, data = countryList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed : " + ex });
            }

        }

        [HttpGet]
        public async Task<JsonResult> getStateMastDt()
        {
            try
            {
              
                SqlConnection con = _dbcontext.GetErpConnection();
                string strqry = "  select STATE_MAST.code,STATE_MAST.NAME as StateName, SHORT_NAME as shortName,GST_CODE as GstCode,E_CODE as Ecode, isnull(country.NAME, '') as countryName, UNION_TERITORY as UnionTeritory, STATE_TYPE as statetype, case when isnull(STATE_MAST.ACTIVE, 0)=1 then 'Yes' else 'No' end as Active from STATE_MAST  left join COUNTRY_MAST country on STATE_MAST.COUNTRY_CODE=country.CODE order by StateName";
               var ListCountry = await _dbHelper.GetJsonDataAsync(strqry);
                return Json(new { status = true, data = ListCountry });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, message = "Data Load failed" + ex.Message });
            }
        }

        public class StateModel
        {           
            public int? Code { get; set; }

            public string? StateName { get; set; }

            public string? ShortName { get; set; }

            public string? GstCode { get; set; }

            public string? Ecode { get; set; }

            public int? CountryId { get; set; }

            public string? UnionTerritory { get; set; }

            public string? StateType { get; set; }

            public int? Active { get; set; }
            public string AED { get; set; }

        }
 
        [HttpPost]
        public JsonResult UpdateStateDt([FromBody] StateModel stateDt)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.[sp_StateMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@code", stateDt.Code);
                        cmd.Parameters.AddWithValue("@StateName", stateDt.StateName);
                        cmd.Parameters.AddWithValue("@shortName", _dbHelper.Xnull(stateDt.ShortName));
                        cmd.Parameters.AddWithValue("@GstCode", _dbHelper.Xnull(stateDt.GstCode));
                        cmd.Parameters.AddWithValue("@Ecode", _dbHelper.Xnull(stateDt.Ecode));
                        cmd.Parameters.AddWithValue("@countryid", _dbHelper.Xnull(stateDt.CountryId));
                        cmd.Parameters.AddWithValue("@UnionTeritory", _dbHelper.Xnull(stateDt.UnionTerritory));
                        cmd.Parameters.AddWithValue("@statetype", _dbHelper.Xnull(stateDt.StateType));
                        cmd.Parameters.AddWithValue("@active", stateDt.Active);
                        cmd.Parameters.AddWithValue("@AED", "E");
                        con.Open();
                        x = cmd.ExecuteNonQuery();
                    }
                }
                if (x > 0)
                    return Json(new { status = true, message = "Data update Successfully" });
                else
                    return Json(new { status = false, message = "Data update failed" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }

        }

        [HttpPost]
        public JsonResult saveStateMastDt([FromBody] StateModel stateDt)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.[sp_StateMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;                     
                        cmd.Parameters.AddWithValue("@StateName", stateDt.StateName);
                        cmd.Parameters.AddWithValue("@shortName", _dbHelper.Xnull(stateDt.ShortName));
                        cmd.Parameters.AddWithValue("@GstCode", _dbHelper.Xnull(stateDt.GstCode));
                        cmd.Parameters.AddWithValue("@Ecode", _dbHelper.Xnull(stateDt.Ecode));
                        cmd.Parameters.AddWithValue("@countryid", stateDt.CountryId);
                        cmd.Parameters.AddWithValue("@UnionTeritory", _dbHelper.Xnull(stateDt.UnionTerritory));
                        cmd.Parameters.AddWithValue("@statetype", _dbHelper.Xnull(stateDt.StateType));
                        cmd.Parameters.AddWithValue("@active", stateDt.Active);                     
                        cmd.Parameters.AddWithValue("@AED", "A");
                        con.Open();
                        x = cmd.ExecuteNonQuery();
                    }
                }
                if (x > 0)
                    return Json(new { status = true, message = "Data save Successfully" });
                else
                    return Json(new { status = false, message = "Data save failed" });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });

            }

        }

        [HttpDelete]
        public JsonResult DelStateDt(int code)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.[sp_StateMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@code", code);
                        cmd.Parameters.AddWithValue("@AED", "D");
                        con.Open();
                        x = cmd.ExecuteNonQuery();

                    }
                }
                if (x > 0)
                    return Json(new { status = true, message = "Data delete Successfully" });
                else
                    return Json(new { status = false, message = "Data delete failed" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        public IActionResult ExportAllDocs()
        {
            var docList = new List<StateExport>();

            using (SqlConnection conn = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_StateMast_AED", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@AED", "Export");
                  
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            docList.Add(new StateExport
                            {
                                Code = reader["code"] as int? ?? 0,
                                StateName = reader["StateName"]?.ToString(),
                                ShortName = reader["shortName"]?.ToString(),
                                GstCode = reader["GstCode"]?.ToString(),
                                Ecode = reader["Ecode"]?.ToString(),
                                CountryName = reader["countryName"]?.ToString(),
                                UnionTeritory = reader["UnionTeritory"]?.ToString(),
                                Statetype = reader["statetype"]?.ToString(),
                                Active = reader["Active"]?.ToString()
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

            using (SqlConnection conn = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_StateMast_AED", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@AED", "DocDetailID");
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
    public class StateExport
    {
        public int? Code { get; set; }
        public string? StateName { get; set; }
        public string? ShortName { get; set; }
        public string? GstCode { get; set; }
        public string? Ecode { get; set; }
        public string? CountryName { get; set; }
        public string? UnionTeritory { get; set; }
        public string? Statetype { get; set; }
        public string? Active { get; set; }
    }



}
