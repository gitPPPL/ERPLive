using System.Data;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class ImportExportExpenseMasterListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        public ImportExportExpenseMasterListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public IActionResult Index()
        {
            //return View("~/Views/FinancialAccounting/Master/ImportExportExpenseMasterList/Index.cshtml");
            return View("~/Views/FinancialAccounting/Master/ImportExportExpenseMasterList/Index.cshtml");
        }


        [HttpGet]
        public async Task<JsonResult> GetImportExportExpenseList()
        {
            try
            {
                var importExportExpenseList = await _dbHelper.GetJsonDataAsync(" select code, GROUP_CODE as groupCd, PARTY_CODE as partyCd, Name,Type from IMPORTVENDOR_MAST where COMP_CODE='" + _globalValue.GetGlobalVariables().PubCompCode + "' order by NAME  ");
                return Json(new { status = true, data = importExportExpenseList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        [HttpDelete]
        public async Task<IActionResult> deleteImportExportMast(int code)
        {
            try
            {
                int x = 0;
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_importExportExpenseMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "D");
                        cmd.Parameters.AddWithValue("@code", code);
                        cmd.Parameters.AddWithValue("@companyCd", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubCompCode));
                        await con.OpenAsync();
                        x = await cmd.ExecuteNonQueryAsync();
                    }
                }
                if (x > 0)
                    return Json(new { status = true, message = "Data deleted successfully" });
                else
                    return Json(new { status = false, message = "Data not deleted" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }

        }

    }
}
