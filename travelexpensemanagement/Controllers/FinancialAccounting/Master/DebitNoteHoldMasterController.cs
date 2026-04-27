using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Dbconnection;
using System.Data;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class DebitNoteHoldMasterController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        public DebitNoteHoldMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/DebitNoteHoldMaster/Index.cshtml");
        }
        [HttpGet]
        public async Task<JsonResult> GetDebitNoteHoldList()
        {
            try
            {
                var DebitNoteHoldList = new List<object>();
                DataTable dt = new DataTable();
                dt = await _dbHelper.ExecuteQueryAsync("select code, (cast(CODE as varchar)+ ' ' + NAME) as AccName  from SUBGROUP_MAST where NATURE in ('Supplier', 'Customer') and COMP_CODE='"+ _globalValue.GetGlobalVariables().PubCompCode +"'  order by AccName ");
                foreach (DataRow row in dt.Rows)
                {
                    DebitNoteHoldList.Add(new
                    {
                        code = row["code"].ToString(),
                        name = row["AccName"].ToString()
                    });
                }
                return Json(new { status = true, data = DebitNoteHoldList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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
                            FROM DEBITNOTEHOLD_MAST 
                            WHERE  PARTY_CODE = @Inputdata and COMP_CODE=@companyCd
                        ) 
                        THEN 1 ELSE 0 END";
                        sqlcmd.Parameters.AddWithValue("@Inputdata", inputData);
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
        public class DebitNoteHoldModel
        {
            public int? partyCode { get; set; }
            public string? Name { get; set; }                    
        }
        [HttpPost]
        public async Task<JsonResult> SaveDebitNoteHold([FromBody] DebitNoteHoldModel model)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (var cmd = new SqlCommand("[dbo].[sp_DebitNoteHMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubCompCode));
                        cmd.Parameters.AddWithValue("@Partycode", _dbHelper.Xnull(model.partyCode));
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));                        
                        cmd.Parameters.AddWithValue("@wsid", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubWorkStationID));
                        cmd.Parameters.AddWithValue("@lip", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubLocalId));
                        con.Open();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Json(new { status = true, message = "Saved successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Save failed: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<JsonResult> UpdateDebitNoteHold([FromBody] DebitNoteHoldModel model)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (var cmd = new SqlCommand("[dbo].[sp_DebitNoteHMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubCompCode));
                        cmd.Parameters.AddWithValue("@Partycode", _dbHelper.Xnull(model.partyCode));
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@wsid", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubWorkStationID));
                        cmd.Parameters.AddWithValue("@lip", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubLocalId));
                        con.Open();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Json(new { status = true, message = "Updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Update failed: " + ex.Message });
            }
        }
 

    }

}
