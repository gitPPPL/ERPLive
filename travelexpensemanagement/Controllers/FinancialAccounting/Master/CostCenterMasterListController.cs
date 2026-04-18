using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Controllers.Globalvariable;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class CostCenterMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public CostCenterMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/CostCenterMasterList/Index.cshtml");
        }

        [HttpGet]
        public async Task<JsonResult> GetCostCenterList()
        {
            try
            {
                var costcenterList = await _dbHelper.GetJsonDataAsync("select code,Name,COSTCODE as CostCd,case when ACTIVE=1 then 'Yes' else 'No' end as Active  from COSTCENTER_MAST  where COMP_CODE='"+ _globalValue.GetGlobalVariables().PubCompCode +"' order by NAME ");
                return Json(new { status = true, data = costcenterList });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, message = "Data load failed" });
            }
        }

        [HttpDelete]
        public async Task<JsonResult> DeleteCostCenter(int code)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_CostCenterMast_AED]", con))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "D");
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
                        cmd.Parameters.AddWithValue("@code", _dbHelper.Xnull(code));                        
                        con.Open();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Json(new { status = true, message = "Data save successfully " });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data save failed: " });
            }
        }

    }
}
