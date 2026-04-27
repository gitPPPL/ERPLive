using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Admin.SystemInitilization
{
    [SessionAuthorize]
    public class DocumentNumberingListController : Controller
    {
        private readonly GlobalVariableService _globalValue;
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public DocumentNumberingListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Document Numbering";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Admin/SystemInitilization/DocumentNumberingList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> getDocumenNumberDt()
        {
            try
            {


                var con = _dbcontext.GetErpConnection();
                var sessionData = _globalValue.GetGlobalVariables();
                var compCode = sessionData.PubCompCode;
                var docNumlist = await _dbHelper.GetJsonDataAsync("select distinct (cast(dn.COMP_CODE as varchar)+cast(dn.YEAR_CODE as varchar)+ cast(dn.V_TYPE as varchar)) as code, dn.COMP_CODE as companyCd, ym.PREFIXYR as yearcode,dn.YEAR_CODE as YearCd, dn.V_TYPE vType, dn.PREFIX as Prefix, dn.FROM_NO as FromNo, dn.TO_NO as ToNo, dn.DOCTYPE from DOC_NUMBER dn left join YEAR_MAST ym on dn.YEAR_CODE=ym.CODE  WHERE COMP_CODE='" + compCode + "' order by yearcode desc, DOCTYPE asc ");
                return Json(new { status = true, data = docNumlist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" + ex.Message });

            }
        }
        [HttpDelete]
        public JsonResult DelDocNumberDt(string code)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_DelDocNumberMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@code", code);
                        int x = cmd.ExecuteNonQuery();
                        if (x > 0)
                            return Json(new { status = true });
                        else
                            return Json(new { status = false });

                    }
                }
            }
            catch { return Json(new { status = false }); }

        }

    }
}
