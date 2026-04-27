using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Emit;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class AccountOutstandingMasterListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public AccountOutstandingMasterListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)         
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "A/c Outstanding Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/AccountOutstandingMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public async Task<JsonResult> GetAccOutstandingMast()
        {
            try
            {
                var usersessionDt = _globalValue.GetGlobalVariables();
                var AccOutstandDt = await _dbHelper.GetJsonDataAsync("select om.code, om.name, om.shortname, sg1.NAME as agent_code, isnull(om.agent_group, '') agentgroup, isnull(om.com_type, '') as comType, isnull(om.com_rate, 0) comRate,isnull(sg2.NAME, 0) actPayable, case when om.active=1 then 'Yes' else 'No' end as active from ACOS_MAST  om left join SUBGROUP_MAST sg1 on om.AGENT_CODE=sg1.CODE left join SUBGROUP_MAST sg2 on om.ACT_PAYABLE=sg2.CODE where om.comp_code ='" + _dbHelper.Xnull(usersessionDt.PubCompCode) + "'  order by NAME ");
                //var AccOutstandDt = await _dbHelper.GetJsonDataAsync("select om.code, om.name, om.shortname, sg1.NAME as agent_code, isnull(om.agent_group, '') agentgroup, isnull(om.com_type, '') as comType, isnull(om.com_rate, 0) comRate,isnull(sg2.NAME, 0) actPayable, case when om.active=1 then 'Yes' else 'No' end as active from ACOS_MAST  om left join SUBGROUP_MAST sg1 on om.AGENT_CODE=sg1.CODE left join SUBGROUP_MAST sg2 on om.ACT_PAYABLE=sg2.CODE  ");

                return Json(new { status = true, data = AccOutstandDt });
            }
            catch(Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }
        public IActionResult ExportAllDocs()
        {
            var list = new List<OutstandingExportDto>();
            var usersessionDt = _globalValue.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbcontext.GetErpConnection())
                {
                    string query = @"
                SELECT 
                    om.code, 
                    om.name, 
                    om.shortname, 
                    sg1.NAME AS agent_code, 
                    ISNULL(om.agent_group, '') AS agentgroup, 
                    ISNULL(om.com_type, '') AS comType, 
                    ISNULL(om.com_rate, 0) AS comRate, 
                    ISNULL(sg2.NAME, '') AS actPayable, 
                    CASE WHEN om.active = 1 THEN 'Yes' ELSE 'No' END AS active 
                FROM ACOS_MAST om
                LEFT JOIN SUBGROUP_MAST sg1 ON om.AGENT_CODE = sg1.CODE 
                LEFT JOIN SUBGROUP_MAST sg2 ON om.ACT_PAYABLE = sg2.CODE 
                WHERE om.comp_code = @comp_code";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@comp_code", usersessionDt.PubCompCode);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new OutstandingExportDto
                                {
                                    Code = reader["code"]?.ToString(),
                                    Name = reader["name"]?.ToString(),
                                    ShortName = reader["shortname"]?.ToString(),
                                    AgentCode = reader["agent_code"]?.ToString(),
                                    AgentGroup = reader["agentgroup"]?.ToString(),
                                    ComType = reader["comType"]?.ToString(),
                                    ComRate = Convert.ToDecimal(reader["comRate"]),
                                    ActPayable = reader["actPayable"]?.ToString(),
                                    Active = reader["active"]?.ToString()
                                });
                            }
                        }
                    }
                }

                return Json(list);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while exporting outstanding data.",
                    error = ex.Message
                });
            }
        }

        public JsonResult DocDetailsCode(string docCode)
        {
            List<ItemGroupDetailDto> docDetails = new List<ItemGroupDetailDto>();
            var usersessionDt = _globalValue.GetGlobalVariables();

            using (SqlConnection conn = _dbcontext.GetErpConnection())
            {
                string query = @"SELECT DISTINCT da.Code, um.USER_NAME as UUser, da.UDATE, ume.USER_NAME as EUSER, da.EDATE, 
          da.WSID, da.LIP, da.LID FROM ACOS_MAST da
          LEFT JOIN CONDATABASE..USER_MAST um ON da.UUSER = um.CODE
          LEFT JOIN CONDATABASE..USER_MAST ume ON da.EUSER = ume.CODE
          WHERE da.Code = @Code and da.COMP_CODE=@COMP_CODE";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", docCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
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
    public class OutstandingExportDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string AgentCode { get; set; }
        public string AgentGroup { get; set; }
        public string ComType { get; set; }
        public decimal ComRate { get; set; }
        public string ActPayable { get; set; }
        public string Active { get; set; }
    }

}
