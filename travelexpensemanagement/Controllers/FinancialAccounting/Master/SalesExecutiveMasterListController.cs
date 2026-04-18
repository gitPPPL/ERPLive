using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using static travelexpensemanagement.Controllers.FinancialAccounting.Master.SalesExecutiveMasterController;
using Microsoft.Data.SqlClient;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class SalesExecutiveMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;        
        int x ;

        public SalesExecutiveMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/SalesExecutiveMasterList/Index.cshtml");
            //return View("~/Views/FincialAccounting/Master/SalesExecutiveMasterList/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetSaleExecutiveMast()
        {
            try
            {
                var salesExecutiveMast = await _dbHelper.GetJsonDataAsync("select VNO as Sno,code, NAME from dbo.SALESEXECUTIVE_MAST where COMP_CODE='" + _globalValue.GetGlobalVariables().PubCompCode + "' ");
                //var salesExecutiveMast = await _dbHelper.GetJsonDataAsync("select VNO as Sno,code, NAME from dbo.SALESEXECUTIVE_MAST where COMP_CODE='5' ");
                return Json(new {status=true, data= salesExecutiveMast });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });

            }

        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSalesExecutiveMast(int sno, int code)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.[sp_SalesExecutiveMast_AED]", con))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "D");
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
                        cmd.Parameters.AddWithValue("@Vno", _dbHelper.Xnull(sno));
                        cmd.Parameters.AddWithValue("@code", _dbHelper.Xnull(code));              
                        con.Open();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Json(new { status = true, message = "Data delete successfully " });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data delete failed: " });
            }
        }
    }
}
