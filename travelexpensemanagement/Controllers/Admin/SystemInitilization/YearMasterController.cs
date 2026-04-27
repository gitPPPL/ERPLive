using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Admin.SystemInitilization
{
    [SessionAuthorize]
    public class YearMasterController : Controller
    {
        private readonly GlobalVariableService _globalValue;
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        string StrSystemName = "", StrSystemIP = "";
        int StrUUser, StrEUser, x;
        public YearMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Posting Periods";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/SystemInitilization/YearMaster/Index.cshtml", model);
        }

        public IActionResult YearMast()
        {
            //return View();
            return View("~/Views/Admin/SystemInitilization/YearMaster/YearMast.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> getYearMaster()
        {
            try
            {
                var LanguagedtList = new List<object>();
                DataTable dt = new DataTable();
                SqlConnection con = _dbcontext.GetErpConnection();
                string strqry = " select code , CURR_YEAR as CurrentYear, PREV_YEAR as PreviousYear,  curr_pwd as currentPassword,PREV_PWD as prevPasswrd , format(START_DATE, 'dd-MMM-yyyy') as StartDt, format(END_DATE,  'dd-MMM-yyyy') as EndDt, format(TSTART_DATE,  'dd-MMM-yyyy') as TStartDt,  PREFIXYR as PrefixYear, CURR_DSN as CurrentDsn, PREV_DSN as PreviousDsn,CURR_SERVER as CurrentServer, PREV_SERVER as PreviousServer, CURR_USER as CurrentUser,PREV_USER as PreviousUser,  case when DATABASE_AVL='1' then 'YES' when DATABASE_AVL='0' then 'NO' else  DATABASE_AVL end as DatabaseAvl, case when STATUS='1' then 'UNLOCK' when STATUS='0' then 'LOCK' else  STATUS end as Status from YEAR_MAST order by code desc";
                dt =await _dbHelper.ExecuteQueryAsync(strqry);
                foreach (DataRow row in dt.Rows)
                {
                    LanguagedtList.Add( new {
                        code = (int)row["code"],
                        CurrentYear = row["CurrentYear"].ToString(),
                        PreviousYear = row["PreviousYear"].ToString(),
                        currentPassword = row["currentPassword"].ToString(),
                        prevPasswrd = row["prevPasswrd"].ToString(),
                        StartDt = row["StartDt"],
                        EndDt = row["EndDt"],
                        TStartDt =  row["TStartDt"],
                        PrefixYear = row["PrefixYear"].ToString(),
                        CurrentDsn = row["CurrentDsn"].ToString(),
                        PreviousDsn = row["PreviousDsn"],
                        CurrentServer = row["CurrentServer"].ToString(),
                        PreviousServer = row["PreviousServer"].ToString(),
                        CurrentUser = row["CurrentUser"].ToString(),
                        PreviousUser = row["PreviousUser"].ToString(),
                        DatabaseAvl = row["DatabaseAvl"].ToString(),
                        Status = row["Status"].ToString()
                    });
                }
                return Json(new { status = true, data = LanguagedtList });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, message = "Daata Load failed" + ex.Message });
            }
        }

        public class ClsYearMaster
        {
            public int? Code { get; set; }
            public string? CurrentYear { get; set; }
            public string? PreviousYear { get; set; }
            public DateTime? StartDt { get; set; }
            public DateTime? EndDt { get; set; }
            public DateTime? TStartDt { get; set; }
            public string? PrefixYear { get; set; }
            public string? CurrentDsn { get; set; }
            public string? PreviousDsn { get; set; }
            public string? CurrentServer { get; set; }
            public string? PreviousServer { get; set; }
            public string? CurrentUser { get; set; }
            public string? PreviousUser { get; set; }
            public string? CurrentPassword { get; set; }
            public string? PreviousPassword { get; set; }
            public string? DatabaseAvl { get; set; }
            public string? Status { get; set; }
        }

        [HttpPost]
        public JsonResult saveyearMastDt([FromBody] ClsYearMaster yeardt)
        {
            try
            {
                StrSystemName = Environment.MachineName;
            
                var sessionData = _globalValue.GetGlobalVariables();
                var StrUUser = sessionData.PubUserId;
                StrSystemIP = sessionData.PubLocalId;
                using ( var con = _dbcontext.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_YearMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@CurrentYear", _dbHelper.Xnull(yeardt.CurrentYear));
                        cmd.Parameters.AddWithValue("@PreviousYear", _dbHelper.Xnull(yeardt.PreviousYear));
                        cmd.Parameters.AddWithValue("@StartDt", Convert.ToDateTime(yeardt.StartDt));
                        cmd.Parameters.AddWithValue("@EndDt", Convert.ToDateTime(yeardt.EndDt));
                        cmd.Parameters.AddWithValue("@TStartDt", _dbHelper.Xnull(yeardt.TStartDt));
                        cmd.Parameters.AddWithValue("@PrefixYear", _dbHelper.Xnull(yeardt.PrefixYear));
                        cmd.Parameters.AddWithValue("@CurrentDsn", _dbHelper.Xnull(yeardt.CurrentDsn));
                        cmd.Parameters.AddWithValue("@PreviousDsn", _dbHelper.Xnull(yeardt.PreviousDsn));
                        cmd.Parameters.AddWithValue("@CurrentServer", _dbHelper.Xnull(yeardt.CurrentServer));
                        cmd.Parameters.AddWithValue("@PreviousServer", _dbHelper.Xnull(yeardt.PreviousServer));
                        cmd.Parameters.AddWithValue("@CurrentUser", _dbHelper.Xnull(yeardt.CurrentUser));
                        cmd.Parameters.AddWithValue("@PreviousUser", _dbHelper.Xnull(yeardt.PreviousUser));
                        cmd.Parameters.AddWithValue("@CurrentPsswrd", _dbHelper.Xnull(yeardt.CurrentPassword));
                        cmd.Parameters.AddWithValue("@PreviousPsswrd", _dbHelper.Xnull(yeardt.PreviousPassword));
                        cmd.Parameters.AddWithValue("@DatabaseAvl", _dbHelper.Xnull(yeardt.DatabaseAvl));
                        cmd.Parameters.AddWithValue("@Status", _dbHelper.Xnull(yeardt.Status));
                        cmd.Parameters.AddWithValue("@user", StrEUser);
                        cmd.Parameters.AddWithValue("@Lip", StrSystemIP);
                       x= cmd.ExecuteNonQuery();
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


        [HttpDelete]
        public JsonResult DelYearDt(int code)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_YearMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "D");
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

        [HttpPost]
        public JsonResult UpdatedYearMastDt([FromBody] ClsYearMaster yeardt)
        {
            try
            {
                StrSystemName = Environment.MachineName;              
                var sessionData = _globalValue.GetGlobalVariables();
               var StrEUser = sessionData.PubUserId;
                StrSystemIP = sessionData.PubLocalId;

                using (var con = _dbcontext.GetErpConnection())
                {                    
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_YearMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@code", yeardt.Code);
                        cmd.Parameters.AddWithValue("@CurrentYear", _dbHelper.Xnull(yeardt.CurrentYear));
                        cmd.Parameters.AddWithValue("@PreviousYear", _dbHelper.Xnull(yeardt.PreviousYear));
                        cmd.Parameters.AddWithValue("@StartDt", Convert.ToDateTime(yeardt.StartDt));
                        cmd.Parameters.AddWithValue("@EndDt", Convert.ToDateTime(yeardt.EndDt));
                        cmd.Parameters.AddWithValue("@TStartDt", _dbHelper.Xnull(yeardt.TStartDt));
                        cmd.Parameters.AddWithValue("@PrefixYear", _dbHelper.Xnull(yeardt.PrefixYear));
                        cmd.Parameters.AddWithValue("@CurrentDsn", _dbHelper.Xnull(yeardt.CurrentDsn));
                        cmd.Parameters.AddWithValue("@PreviousDsn", _dbHelper.Xnull(yeardt.PreviousDsn));
                        cmd.Parameters.AddWithValue("@CurrentServer", _dbHelper.Xnull(yeardt.CurrentServer));
                        cmd.Parameters.AddWithValue("@PreviousServer", _dbHelper.Xnull(yeardt.PreviousServer));
                        cmd.Parameters.AddWithValue("@CurrentUser", _dbHelper.Xnull(yeardt.CurrentUser));
                        cmd.Parameters.AddWithValue("@PreviousUser", _dbHelper.Xnull(yeardt.PreviousUser));
                        cmd.Parameters.AddWithValue("@CurrentPsswrd", _dbHelper.Xnull(yeardt.CurrentPassword));
                        cmd.Parameters.AddWithValue("@PreviousPsswrd", _dbHelper.Xnull(yeardt.PreviousPassword));
                        cmd.Parameters.AddWithValue("@DatabaseAvl", _dbHelper.Xnull(yeardt.DatabaseAvl));
                        cmd.Parameters.AddWithValue("@Status", _dbHelper.Xnull(yeardt.Status));
                        cmd.Parameters.AddWithValue("@user", StrEUser);
                        cmd.Parameters.AddWithValue("@Lip", StrSystemIP);                        
                          x=cmd.ExecuteNonQuery();

                        if (x>0)
                            return Json(new { status = true, message = "Data update Successfully" });
                        else
                            return Json(new { status = false, message = "Data update failed" });
                    }
                }

                
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

        public IActionResult ExportAllDocs()
        {
            var compCode = _globalValue.GetGlobalVariables().PubCompCode;
            var docList = new List<YearMasterExportModel>();

            using (SqlConnection conn = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_YearMast_AED", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@AED", "Export");

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            docList.Add(new YearMasterExportModel
                            {
                                Code = reader["Code"] != DBNull.Value ? Convert.ToInt32(reader["Code"]) : 0,
                                PREV_YEAR = reader["PREV_YEAR"]?.ToString(),
                                START_DATE = reader["START_DATE"]?.ToString(),
                                END_DATE = reader["END_DATE"]?.ToString(),
                                TSTART_DATE = reader["TSTART_DATE"]?.ToString(),
                                PREFIXYR = reader["PREFIXYR"]?.ToString(),
                                DATABASE_AVL = reader["DATABASE_AVL"]?.ToString(),
                                STATUS = reader["STATUS"]?.ToString()
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
                using (SqlCommand cmd = new SqlCommand("sp_YearMast_AED", conn))
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
    public class YearMasterExportModel
    {
        public int Code { get; set; }
        public string PREV_YEAR { get; set; }
        public string START_DATE { get; set; }  // in dd/MM/yyyy format
        public string END_DATE { get; set; }
        public string TSTART_DATE { get; set; }
        public string PREFIXYR { get; set; }
        public string DATABASE_AVL { get; set; }
        public string STATUS { get; set; }
    }

}
