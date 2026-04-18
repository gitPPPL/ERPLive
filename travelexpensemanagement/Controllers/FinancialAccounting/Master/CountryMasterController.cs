using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.ModuleService;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static travelexpensemanagement.Controllers.Master.CountryMasterController;


namespace travelexpensemanagement.Controllers.Master
{
    public class CountryMasterController : Controller
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;


        private readonly GlobalVariableService _globalVarservice;
        int x, Vno;
        string tablename, Vtype, description, Lip, Lid;
        public CountryMasterController(DataBaseConnection dbcontext, DbHelper.DbHelper dbHelper, GlobalVariableService globalVarservice, ModuleService.ModuleService moduleService)
        {
            _dbcontext = dbcontext;
            _dbHelper = dbHelper;
            _globalVarservice = globalVarservice;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Country Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/CountryMaster/Index.cshtml", model);
        }
        public IActionResult CountryMast()
        {
            return View("~/Views/FinancialAccounting/Master/CountryMaster/CountryMast.cshtml");
            //return View("~/Views/FincialAccounting/Master/CountryMaster/CountryMast.cshtml");
        }

        public class CountryData
        {
            public int? code { get; set; }
            public string name { get; set; }
            public int Active { get; set; }
        }


        [HttpGet]
        public async Task<JsonResult> getCountryMaster()
        {
            try
            {
                var CurrencyList = new List<object>();
                DataTable dt = new DataTable();
                SqlConnection con = _dbcontext.GetErpConnection();
                string strqry = " select code, name,case when isnull(ACTIVE, 0)=1 then 'Yes' else 'No' end as  active from COUNTRY_MAST order by name ";
                dt = await _dbHelper.ExecuteQueryAsync(strqry);
                foreach (DataRow row in dt.Rows)
                {
                    CurrencyList.Add(new { code = (Int32)row["code"], name = row["name"].ToString(), active = row["active"].ToString() });
                }
                return Json(new { status = true, data = CurrencyList });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, message = "Data Load failed" + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult getExitOrNot(string inputData)
        {
            try
            {
                bool isExist = false;

                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand sqlcmd = new SqlCommand())
                    {
                        sqlcmd.Connection = con;
                        sqlcmd.CommandText = @"
                        SELECT CASE 
                        WHEN EXISTS (
                            SELECT 1 
                            FROM COUNTRY_MAST 
                            WHERE UPPER(ISNULL(NAME, '')) = UPPER(@Inputdata)
                        ) 
                        THEN 1 ELSE 0 END";
                        sqlcmd.Parameters.AddWithValue("@Inputdata", inputData);
                        con.Open();
                        var result = sqlcmd.ExecuteScalar();
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
        public async Task<JsonResult> UpdateCountryDt([FromBody] CountryData countrydt)
        {
            x = 0;
            bool transactionSucceeded = false;
            DataTable dt = new DataTable();
            using (var con = _dbcontext.GetErpConnection())
            {
                dt =await _dbHelper.ExecuteQueryAsync("SELECT NAME, ACTIVE FROM COUNTRY_MAST WHERE CODE ='" + _dbHelper.Xnull(countrydt.code) + "' ");
                con.Open();
                var transaction = con.BeginTransaction();
                try
                {
                    var sessionUser = _globalVarservice.GetGlobalVariables();
                  var  companyCd = sessionUser.PubCompCode;
                  var  Euser = sessionUser.PubUserId;
                    //Lip = _dbHelper.GetLocalIPAddress();
                    Lip = sessionUser.PubLocalId;
                    if (dt.Rows.Count > 0)
                    {
                        var oldName = dt.Rows[0]["NAME"]?.ToString();
                        var oldActive = Convert.ToInt32(dt.Rows[0]["ACTIVE"]);
                        var newName = countrydt.name?.ToString();
                        var newActive = Convert.ToInt32(countrydt.Active);

                        var descriptionBuilder = new StringBuilder();

                        if (oldName != newName)
                            descriptionBuilder.AppendLine($"Name={oldName} -> {newName} , ");

                        if (oldActive != newActive)
                            descriptionBuilder.AppendLine($"Active={oldActive} -> {newActive}");

                        description = descriptionBuilder.ToString();

                        if (!string.IsNullOrEmpty(description))
                        {
                            using (SqlCommand logCmd = new SqlCommand("sp_LogTable", con, transaction))
                            {
                                logCmd.CommandType = CommandType.StoredProcedure;
                                logCmd.Parameters.AddWithValue("@companyCd", companyCd);
                                logCmd.Parameters.AddWithValue("@tablename", "COUNTRY_MAST");
                                logCmd.Parameters.AddWithValue("@VNo", countrydt.code);
                                logCmd.Parameters.AddWithValue("@description", description);
                                logCmd.Parameters.AddWithValue("@EUser", Euser);
                                logCmd.Parameters.AddWithValue("@Lip", Lip);
                                logCmd.Parameters.AddWithValue("@Lid", "admin");
                                x = logCmd.ExecuteNonQuery();
                            }
                            if (x > 0)
                            {
                                using (SqlCommand updateCmd = new SqlCommand("sp_CountryMast_AED", con, transaction))
                                {
                                    updateCmd.CommandType = CommandType.StoredProcedure;
                                    updateCmd.Parameters.AddWithValue("@code", countrydt.code);
                                    updateCmd.Parameters.AddWithValue("@name", countrydt.name);
                                    updateCmd.Parameters.AddWithValue("@active", countrydt.Active);
                                    updateCmd.Parameters.AddWithValue("@AED", "E");
                                    updateCmd.ExecuteNonQuery();
                                    transaction.Commit();
                                    transactionSucceeded = true;
                                }

                            }


                        }
                    }

                    return Json(new { status = true, message = "Data updated successfully" });
                }
                catch (Exception ex)
                {
                    if (!transactionSucceeded)
                        transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }


        [HttpPost]
        public JsonResult saveCountryMastDt([FromBody] CountryData countryDt)
        {

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand sqlcmd = new SqlCommand("sp_CountryMast_AED", con))
                    {
                        sqlcmd.CommandType = CommandType.StoredProcedure;
                        sqlcmd.Parameters.AddWithValue("@name", countryDt.name);
                        sqlcmd.Parameters.AddWithValue("@active", countryDt.Active);
                        sqlcmd.Parameters.AddWithValue("@AED", "A");
                        con.Open();
                        x = sqlcmd.ExecuteNonQuery();
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
        public async Task<JsonResult> DelCountryDt(int code)
        {
            bool transactionSucceeded = false;
            x = 0;
            DataTable dt = new DataTable();

            using (var con = _dbcontext.GetErpConnection())
            {
                dt =await _dbHelper.ExecuteQueryAsync("SELECT NAME, ACTIVE FROM COUNTRY_MAST WHERE CODE ='" + _dbHelper.Xnull(code) + "' ");
                con.Open();
                var transaction = con.BeginTransaction();

                var sessionUser = _globalVarservice.GetGlobalVariables();
               var companyCd = sessionUser.PubCompCode;
               var Euser = sessionUser.PubUserId;
                //Lip = _dbHelper.GetLocalIPAddress();
                Lip = sessionUser.PubLocalId;
                try
                {
                    if (dt.Rows.Count > 0)
                    {
                        var oldName = dt.Rows[0]["NAME"]?.ToString();
                        var oldActive = Convert.ToInt32(dt.Rows[0]["ACTIVE"]);

                        var descriptionBuilder = new StringBuilder();
                        descriptionBuilder.AppendLine($"Name= {oldName} , Active={oldActive} -> {oldActive} ");

                        description = descriptionBuilder.ToString();

                        if (!string.IsNullOrEmpty(description))
                        {
                            using (SqlCommand logCmd = new SqlCommand("sp_LogTable", con, transaction))
                            {
                                logCmd.CommandType = CommandType.StoredProcedure;
                                logCmd.Parameters.AddWithValue("@companyCd", companyCd);
                                logCmd.Parameters.AddWithValue("@tablename", "COUNTRY_MAST");
                                logCmd.Parameters.AddWithValue("@VNo", code);
                                logCmd.Parameters.AddWithValue("@description", description);
                                logCmd.Parameters.AddWithValue("@EUser", Euser);
                                logCmd.Parameters.AddWithValue("@Lip", Lip);
                                logCmd.Parameters.AddWithValue("@Lid", "admin");
                                x = logCmd.ExecuteNonQuery();
                            }
                            if (x > 0)
                            {
                                using (SqlCommand updateCmd = new SqlCommand("sp_CountryMast_AED", con, transaction))
                                {
                                    updateCmd.CommandType = CommandType.StoredProcedure;
                                    updateCmd.Parameters.AddWithValue("@code", code);
                                    updateCmd.Parameters.AddWithValue("@AED", "D");
                                    updateCmd.ExecuteNonQuery();                        
                                    transaction.Commit();
                                    transactionSucceeded = true;

                                }

                            }

                        }                     
                    }
                    return Json(new { status = true, message = "Data delete Successfully" });
                }
                catch (Exception ex)
                {
                    if (!transactionSucceeded)
                        transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }

            }

        }

        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVarservice.GetGlobalVariables().PubCompCode;
            var countryList = new List<CountryModel>();

            using (SqlConnection conn = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_CountryMast_AED", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@AED", "Export");
    
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            countryList.Add(new CountryModel
                            {
                                Code = reader["Code"]?.ToString(),
                                Name = reader["Name"]?.ToString(),
                                Status = reader["STATUS"]?.ToString()
                            });
                        }
                    }
                }
            }
            return Json(countryList);
        }


        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVarservice.GetGlobalVariables();
            List<ItemGroupDetailDto> docDetails = new List<ItemGroupDetailDto>();

            using (SqlConnection conn = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_CountryMast_AED", conn))
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
    public class CountryModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
    }

}
