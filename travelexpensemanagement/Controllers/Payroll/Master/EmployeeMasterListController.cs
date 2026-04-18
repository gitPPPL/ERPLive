using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Asn1.X509;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Threading.Tasks;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.Admin.SystemInitilization;
using travelexpensemanagement.ModuleService;
using Dapper;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class EmployeeMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        int x;
        public EmployeeMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;

        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Employee Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Payroll/Master/EmployeeMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeMastData(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                string strqry = $@"
                select em.CODE, em.NAME, em.FATHER_NAME,isnull(cm.NAME, '') City, isnull(em.PADD1, '') Address ,isnull(dm.NAME, '') DEPT_CODE,isnull(dg.NAME, '')  DESG_CODE,isnull(em.AADAR, '') AADAR,
                isnull(format(em.JOIN_DATE, 'yyyy-MM-dd'), '') JOIN_DATE, isnull(format(em.RESIGN_DATE, 'yyyy-MM-dd'), '') RESIGN_DATE  ,em.ACTIVE
                from EMP_MAST em left join DEPT_MAST dm on em.DEPT_CODE = dm.CODE and em.COMP_CODE = dm.COMP_CODE left join DESG_MAST dg on em.DESG_CODE = dg.CODE and em.COMP_CODE = dg.COMP_CODE
                left join CITY_MAST cm on em.PCITY_CODE=cm.code 
                where em.COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} order by em.NAME ";
                var fullList = await _dbHelper.GetJsonDataAsync(strqry);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "NAME" };
                            return searchableKeys.Any(key =>
                                dict.ContainsKey(key) &&
                                dict[key]?.ToString().ToLower().Contains(searchTerm) == true
                            );
                        })
                        .ToList();
                }

                var totalCount = fullList.Count;
                var pagedList = fullList
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Json(new { status = true, data = pagedList, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        
        [HttpGet]
        public async Task<IActionResult> GetEmpDetail(int Code)
        {
            var compCode = _globalValue.GetGlobalVariables().PubCompCode;
            const string procName = "[dbo].[sp_EmployeeMast_AED]";

            try
            {
                using var conn = _dbcontext.GetErpConnection();
                await conn.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@AED", "EmpDetail");
                parameters.Add("@companyCd", compCode);
                parameters.Add("@CODE", Code);  
                parameters.Add("@ErrorMessage", dbType: DbType.String, size: 4000, direction: ParameterDirection.Output);
                parameters.Add("@ReturnVal", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                // Execute stored procedure and retrieve result set
                var list = (await conn.QueryAsync<dynamic>(
                    procName,
                    parameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                int resultCode = parameters.Get<int>("@ReturnVal");
                string errorMsg = parameters.Get<string>("@ErrorMessage");

                if (resultCode < 0)
                    return Json(new { status = false, message = errorMsg });

                return Json(new { status = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> ExportAllDocs()
        {
            var compCode = _globalValue.GetGlobalVariables().PubCompCode;
            const string procName = "[dbo].[sp_EmployeeMast_AED]";

            try
            {
                using var conn = _dbcontext.GetErpConnection();
                await conn.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@AED", "Excel");
                parameters.Add("@companyCd", compCode);
                parameters.Add("@CODE", dbType: DbType.Int32, direction: ParameterDirection.Input); // unused for Excel
                parameters.Add("@ErrorMessage", dbType: DbType.String, size: 4000, direction: ParameterDirection.Output);
                parameters.Add("@ReturnVal", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                // Execute stored procedure and retrieve result set
                var list = (await conn.QueryAsync<dynamic>(
                    procName,
                    parameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                int resultCode = parameters.Get<int>("@ReturnVal");
                string errorMsg = parameters.Get<string>("@ErrorMessage");

                if (resultCode < 0)
                    return Json(new { status = false, message = errorMsg });

                return Json(new { status = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

                
    }
}
