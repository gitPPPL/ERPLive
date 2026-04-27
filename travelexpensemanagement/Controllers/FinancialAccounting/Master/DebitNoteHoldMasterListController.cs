using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.ModuleService;
using static travelexpensemanagement.Controllers.FinancialAccounting.Master.DebitNoteHoldMasterController;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class DebitNoteHoldMasterListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public DebitNoteHoldMasterListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Debit Note N/A Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/DebitNoteHoldMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<JsonResult> GetDebitNoteMast()
        {
            try
            {
                var DebitNoteDt = new List<object>();
                DataTable dt = new DataTable();
                dt = await _dbHelper.ExecuteQueryAsync("select PARTY_CODE as code, NAME from DEBITNOTEHOLD_MAST where COMP_CODE='"+ _globalValue.GetGlobalVariables().PubCompCode +"' order by NAME ");
                foreach (DataRow row in dt.Rows)
                {
                    DebitNoteDt.Add(new
                    {
                        code = row["code"].ToString(),
                        name = row["NAME"].ToString()
                    });
                }
                return Json(new { status = true, data = DebitNoteDt });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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
        [HttpDelete]
        public async Task<JsonResult> DeleteDebitNoteHold(int code)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (var cmd = new SqlCommand("[dbo].[sp_DebitNoteHMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "D");
                        cmd.Parameters.AddWithValue("@companyCd", _dbHelper.Xnull(_globalValue.GetGlobalVariables().PubCompCode));
                        cmd.Parameters.AddWithValue("@Partycode", _dbHelper.Xnull(code));                        
                        con.Open();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Json(new { status = true, message = "Delete successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Delete failed: " + ex.Message });
            }
        }


        public IActionResult ExportAllDocs()
        {
            var compCode = _globalValue.GetGlobalVariables().PubCompCode;
            var docList = new List<DebitNoteExport>();

            using (SqlConnection conn = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_DebitNoteHMast_AED", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@AED", "Export");
                    cmd.Parameters.AddWithValue("@companyCd", compCode);
                    //cmd.Parameters.AddWithValue("@PageNumber", 1);
                    //cmd.Parameters.AddWithValue("@PageSize", int.MaxValue);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            docList.Add(new DebitNoteExport
                            {
                                Code = reader["Code"]?.ToString(),
                                NAME = reader["NAME"]?.ToString()
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
                using (SqlCommand cmd = new SqlCommand("sp_DebitNoteHMast_AED", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@AED", "DocDetailID");
                    cmd.Parameters.AddWithValue("@companyCd", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@Partycode", docCode);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detail = new ItemGroupDetailDto
                            {
                                Code = reader["PARTY_CODE"]?.ToString(),
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
    public class DebitNoteExport
    {
        public string Code { get; set; }
        public string NAME { get; set; }
    }
}
