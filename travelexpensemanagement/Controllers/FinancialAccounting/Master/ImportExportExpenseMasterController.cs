using System.Data;
using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class ImportExportExpenseMasterController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public ImportExportExpenseMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public IActionResult Index()
        {
            //return View("~/Views/FinancialAccounting/Master/ImportExportExpenseMaster/Index.cshtml");
            return View("~/Views/FinancialAccounting/Master/ImportExportExpenseMaster/Index.cshtml");
        }

        [HttpGet]
        public async Task<JsonResult> GetAccGroupList()
        {
            try
            {
                var AccGroupList = await _dbHelper.GetJsonDataAsync(" select code, name from GR_MAST order by NAME ");
                return Json(new { status = true, data = AccGroupList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<JsonResult> GetAccNameList(int code)
        {
            try
            {
                var AccNameList = await _dbHelper.GetJsonDataAsync(" select code, name from SUBGROUP_MAST where code='" + code + "'  and COMP_CODE='" + _globalValue.GetGlobalVariables().PubCompCode + "' order by NAME ");
                return Json(new { status = true, data = AccNameList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult getExitOrNot(int groupCd, int AccountCd)
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
                            FROM importvendor_mast 
                            WHERE  comp_code=@companyCd and group_code=@groupCd and party_code=@partyCd
                        ) 
                        THEN 1 ELSE 0 END";
                        sqlcmd.Parameters.AddWithValue("@groupCd", groupCd);
                        sqlcmd.Parameters.AddWithValue("@partyCd", AccountCd);
                        sqlcmd.Parameters.AddWithValue("@companyCd", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubCompCode));
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

        public class ImpoprtExportMastModel
        {
            public int? code { get; set; }
            public int? Accountcode { get; set; }
            public int? group_code { get; set; }
            public string? AccountName { get; set; }
            public string? GroupName { get; set; }
        }


        [HttpPost]
        public async Task<IActionResult> saveImportExportMast([FromBody] ImpoprtExportMastModel model)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_importExportExpenseMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubCompCode));
                        cmd.Parameters.AddWithValue("@groupCd", _dbHelper.Xnull(model.group_code));
                        cmd.Parameters.AddWithValue("@partyCd", _dbHelper.Xnull(model.Accountcode));
                        cmd.Parameters.AddWithValue("@Accountname", _dbHelper.Xnull(model.AccountName));
                        cmd.Parameters.AddWithValue("@Groupname", _dbHelper.Xnull(model.GroupName));
                        await con.OpenAsync();
                        x = await cmd.ExecuteNonQueryAsync();
                    }
                }

                if (x > 0)
                    return Json(new { status = true, message = "Data save successfully" });
                else
                    return Json(new { status = false, message = "Data not save" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateImportExportMast([FromBody] ImpoprtExportMastModel model)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_importExportExpenseMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@code", _dbHelper.Xnull(model.code));
                        cmd.Parameters.AddWithValue("@companyCd", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubCompCode));
                        cmd.Parameters.AddWithValue("@groupCd", _dbHelper.Xnull(model.group_code));
                        cmd.Parameters.AddWithValue("@partyCd", _dbHelper.Xnull(model.Accountcode));
                        cmd.Parameters.AddWithValue("@Accountname", _dbHelper.Xnull(model.AccountName));
                        cmd.Parameters.AddWithValue("@Groupname", _dbHelper.Xnull(model.GroupName));
                        await con.OpenAsync();
                        x = await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { status = true, message = "Data updated successfully" });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }

        }

    }
}
