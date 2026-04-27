using System.Dynamic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class AccountOutstandingMasterController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public AccountOutstandingMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/AccountOutstandingMaster/Index.cshtml");
        }
        [HttpGet]
        public async Task<JsonResult> GetACpayableName()
        {
            try
            {
                var usersessionDt = _globalValue.GetGlobalVariables();
                var AC_PayableNm = await _dbHelper.GetJsonDataAsync(" select code, name from  SUBGROUP_MAST where NATURE in ('cash', 'bank') and COMP_CODE='" + _dbHelper.Xnull(usersessionDt.PubCompCode) + "' order by name ");
                
                return Json(new { status = true, data = AC_PayableNm });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed" });
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetAgentName()
        {
            try
            {
                var usersessionDt = _globalValue.GetGlobalVariables();
                var AgentName = await _dbHelper.GetJsonDataAsync(" select code, name from  SUBGROUP_MAST where NATURE in ('broker') and COMP_CODE='" + _dbHelper.Xnull(usersessionDt.PubCompCode) + "' order by name ");
                //var AgentName = await _dbHelper.GetJsonDataAsync(" select code, name from  SUBGROUP_MAST where NATURE in ('broker') and COMP_CODE='5' order by name ");

                return Json(new { status = true, data = AgentName });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed" });
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
        public class AccountOutstandingMasterData
        {
            public int? Code { get; set; }
            public string? Name { get; set; }
            public string? ShortName { get; set; }
            public int? AgentCode { get; set; }
            public string? AgentGroup { get; set; }
            public string? ComType { get; set; }
            public decimal? ComRate { get; set; }
            public int? ActPayable { get; set; }
            public int? Active { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> SaveAccountOutstandingMaster([FromBody] AccountOutstandingMasterData inputData)
        {
            try
            {
                int exists = 0;
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM ACOS_MAST WHERE COMP_CODE = @COMP_CODE AND NAME = @NAME", con))
                    {
                        checkCmd.Parameters.AddWithValue("@COMP_CODE", _globalValue.GetGlobalVariables().PubCompCode);
                        checkCmd.Parameters.AddWithValue("@NAME", _dbHelper.Xnull(inputData.Name));

                        con.Open();
                        exists = (int)await checkCmd.ExecuteScalarAsync();
                        con.Close();
                    }
                    if (exists > 0)
                    {
                        return Json(new { status = false, message = "Name already exists!" });
                    }
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_OutstandingMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(inputData.Name));
                        cmd.Parameters.AddWithValue("@shortName", _dbHelper.Xnull(inputData.ShortName));
                        cmd.Parameters.AddWithValue("@agent_code", _dbHelper.Xnull(inputData.AgentCode));
                        cmd.Parameters.AddWithValue("@AGENT_GROUP", _dbHelper.Xnull(inputData.AgentGroup));
                        cmd.Parameters.AddWithValue("@COM_TYPE", _dbHelper.Xnull(inputData.ComType));
                        cmd.Parameters.AddWithValue("@COM_RATE", _dbHelper.Vnull(inputData.ComRate));
                        cmd.Parameters.AddWithValue("@ACT_PAYABLE", _dbHelper.Vnull(inputData.ActPayable));
                        cmd.Parameters.AddWithValue("@Active", _dbHelper.Xnull(inputData.Active));
                        cmd.Parameters.AddWithValue("@wsid", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubWorkStationID));
                        cmd.Parameters.AddWithValue("@lip", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubLocalId));

                        con.Open();
                        await cmd.ExecuteNonQueryAsync();
                        con.Close();
                    }
                }
                return Json(new { status = true, message = "Data saved successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data save failed: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAccountOutstandingMaster([FromBody] AccountOutstandingMasterData inputData)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_OutstandingMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
                        cmd.Parameters.AddWithValue("@code", _dbHelper.Xnull(inputData.Code));
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(inputData.Name));
                        cmd.Parameters.AddWithValue("@shortName", _dbHelper.Xnull(inputData.ShortName));
                        cmd.Parameters.AddWithValue("@agent_code", _dbHelper.Xnull(inputData.AgentCode));
                        cmd.Parameters.AddWithValue("@AGENT_GROUP", _dbHelper.Xnull(inputData.AgentGroup));
                        cmd.Parameters.AddWithValue("@COM_TYPE", _dbHelper.Xnull(inputData.ComType));
                        cmd.Parameters.AddWithValue("@COM_RATE", _dbHelper.Vnull(inputData.ComRate));
                        cmd.Parameters.AddWithValue("@ACT_PAYABLE", _dbHelper.Vnull(inputData.ActPayable));
                        cmd.Parameters.AddWithValue("@Active", _dbHelper.Xnull(inputData.Active));
                        cmd.Parameters.AddWithValue("@wsid", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubWorkStationID));
                        cmd.Parameters.AddWithValue("@lip", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubLocalId));
                        con.Open();
                        x = await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Json(new { status = true, message = "Data updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data update failed: " + ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAccountOutstandingMaster(string code)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_OutstandingMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "D");
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
                        cmd.Parameters.AddWithValue("@code", _dbHelper.Xnull(code));
                        con.Open();
                        x = await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Json(new { status = true, message = "Data deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data delete failed: " + ex.Message });
            }


        }

    }
}
