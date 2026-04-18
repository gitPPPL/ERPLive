using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Dynamic;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using travelexpensemanagement.Models.Admin.Setup;


namespace travelexpensemanagement.Controllers.Master
{   
    public class CityMasterController : Controller
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalVariableService _globalVariableService;
        int x;
        public CityMasterController(DataBaseConnection dbcontext, DbHelper.DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalVariableService globalVariableService)
        {
            _dbcontext = dbcontext;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalVariableService = globalVariableService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "City Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/CityMaster/Index.cshtml", model);
        }

        public IActionResult CityMast()
        {
            return View("~/Views/FinancialAccounting/Master/CityMaster/CityMast.cshtml");
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
                            FROM CITY_MAST 
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
        public async Task<IActionResult> GetState()
        {
            DataTable dtState = new DataTable();
            SqlConnection con = _dbcontext.GetErpConnection();
            var StateList = new List<object>();
            try
            {
                string strqry = "select code as StateId, NAME as StateName from dbo.STATE_MAST order by NAME  ";
                dtState =await _dbHelper.ExecuteQueryAsync(strqry);

                foreach (DataRow row in dtState.Rows)
                {
                    StateList.Add(new { id = row["StateId"].ToString(), State = row["StateName"].ToString() });
                }
                return Json(new { status = true, data = StateList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed : " + ex });
            }

        }

        [HttpGet]
        public async Task<IActionResult> getCityMastDt()
        {
            try
            {
                
                SqlConnection con = _dbcontext.GetErpConnection();
                string strqry = " select cm.code, cm.NAME as cityname,cm.SHORTNAME as shortname,isnull(cm.ZIPCODE, '') as zipcode,isnull(cm.STDCODE, '') as stdcode," +
                                " isnull(sm.NAME, '') as statename,isnull(countrym.name, '') as countryname , case when isnull(cm.ACTIVE, 0)=1 then 'Yes' else 'No' end as Active " +
                                " from CITY_MAST cm  left join COUNTRY_MAST countrym on cm.COUNTRY_CODE=countrym.CODE left join STATE_MAST sm on cm.STATE_CODE=sm.code order by cityname ";
              var  citylist =await _dbHelper.GetJsonDataAsync(strqry);
                return Json(new { status = true, data = citylist });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, message = "Data Load failed" + ex.Message });
            }
        }

        public class CityModel
        {
            public int? Code { get; set; }
            public string? CityName { get; set; }
            public string? ShortName { get; set; }
            public string? ZipCode { get; set; }
            public int? Stdcode { get; set; }
            public int? stateId { get; set; }
            public int? CountryId { get; set; }        
            public int? Active { get; set; }
            public string AED { get; set; }

        }
        

        [HttpPost]
        public JsonResult UpdateCityDt([FromBody] CityModel stateDt)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.[sp_CityMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@code", stateDt.Code);
                        cmd.Parameters.AddWithValue("@CityName", stateDt.CityName);
                        cmd.Parameters.AddWithValue("@shortName", _dbHelper.Xnull(stateDt.ShortName));
                        cmd.Parameters.AddWithValue("@ZipCode", _dbHelper.Xnull(stateDt.ZipCode));
                        cmd.Parameters.AddWithValue("@stdCode", _dbHelper.Xnull(stateDt.Stdcode));
                        cmd.Parameters.AddWithValue("@stateid", _dbHelper.Xnull(stateDt.stateId));
                        cmd.Parameters.AddWithValue("@countryid", _dbHelper.Xnull(stateDt.CountryId));                     
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
        public JsonResult saveCityMastDt([FromBody] CityModel stateDt)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.[sp_CityMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CityName", stateDt.CityName);
                        cmd.Parameters.AddWithValue("@shortName", _dbHelper.Xnull(stateDt.ShortName));
                        cmd.Parameters.AddWithValue("@ZipCode", _dbHelper.Xnull(stateDt.ZipCode));
                        cmd.Parameters.AddWithValue("@stdCode", _dbHelper.Xnull(stateDt.Stdcode));
                        cmd.Parameters.AddWithValue("@stateid", _dbHelper.Xnull(stateDt.Stdcode));
                        cmd.Parameters.AddWithValue("@countryid", _dbHelper.Xnull(stateDt.CountryId));
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
        public JsonResult DelCityDt(int code)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.[sp_CityMast_AED]", con))
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
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var cityList = new List<CityMasterExport>();

            using (SqlConnection conn = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_CityMast_AED", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@AED", "Export");

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cityList.Add(new CityMasterExport
                            {
                                Code = reader["code"]?.ToString(),
                                CityName = reader["cityname"]?.ToString(),
                                ShortName = reader["shortname"]?.ToString(),
                                ZipCode = reader["zipcode"]?.ToString(),
                                StdCode = reader["stdcode"]?.ToString(),
                                StateName = reader["statename"]?.ToString(),
                                CountryName = reader["countryname"]?.ToString(),
                                Active = reader["Active"]?.ToString(),
                            });
                        }
                    }
                }
            }
            return Json(cityList);
        }
        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<ItemGroupDetailDto> docDetails = new List<ItemGroupDetailDto>();

            using (SqlConnection conn = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_CityMast_AED", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@AED", "DocDetailID");
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

        public class CityMasterExport
        {
            public string Code { get; set; }
            public string CityName { get; set; }
            public string ShortName { get; set; }
            public string ZipCode { get; set; }
            public string StdCode { get; set; }
            public string StateName { get; set; }
            public string CountryName { get; set; }
            public string Active { get; set; }
        }

    }
}
