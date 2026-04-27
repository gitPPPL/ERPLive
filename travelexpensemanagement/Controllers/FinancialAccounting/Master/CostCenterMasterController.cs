using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class CostCenterMasterController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;

        public CostCenterMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/CostCenterMaster/Index.cshtml");
        }

        [HttpGet]
        public JsonResult getExitOrNot(string name)
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
                            FROM dbo.COSTCENTER_MAST 
                             WHERE comp_code = @companyCd and name = @name 
                        ) 
                        THEN 1 ELSE 0 END";
                        cmd.Parameters.AddWithValue("@companyCd", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubCompCode));
                        cmd.Parameters.AddWithValue("@name", name);
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

        public class CostCenterModel
        {           
            public int? Code { get; set; }
            public string? Name { get; set; }
            public string? CostCode { get; set; }
            public int active { get; set; }
        }     

        [HttpPost]
        public async Task<IActionResult> SaveSalesExecutiveMast([FromBody] CostCenterModel model)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_CostCenterMast_AED]", con))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);                        
                        cmd.Parameters.AddWithValue("@name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@costCd", _dbHelper.Xnull(model.CostCode));
                        cmd.Parameters.AddWithValue("@active", _dbHelper.Xnull(model.active));
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


        [HttpPost]
        public async Task<IActionResult> EditSalesExecutiveMast([FromBody] CostCenterModel model)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_CostCenterMast_AED]", con))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");                        
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
                        cmd.Parameters.AddWithValue("@code", _dbHelper.Xnull(model.Code));
                        cmd.Parameters.AddWithValue("@name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@costCd", _dbHelper.Xnull(model.CostCode));
                        cmd.Parameters.AddWithValue("@active", _dbHelper.Xnull(model.active));
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
