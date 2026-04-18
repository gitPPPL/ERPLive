using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class TDSDeductionListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public TDSDeductionListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "TDS Deduction Entry";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Payroll/MonthlyTransaction/TDSDeductionList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetTDSDeductionEntryList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", UsersessionDt.PubCompCode },
                    {"@YEAR_CODE", UsersessionDt.PubFYearCode },
                    {"@BRANCH_CODE", 1 },
                    {"@Action", "TDSDeductionEntryList" }
                };

                var fullList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_TDSDeductionEntry]", parameter);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "EMP_CODE" };
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

        [HttpDelete]
        public async Task<IActionResult> DeleteTDSDeductionEntryEntry(string empCode)
        {
            try
            {
                if (string.IsNullOrEmpty(empCode))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var userSession = _globalValue.GetGlobalVariables();
 
                using (var con = _dbcontext.GetErpConnection())
                {
                    try
                    {
                        string query = "DELETE FROM PAY_TDS1 WHERE COMP_CODE=@COMP_CODE AND YEAR_CODE=@YEAR_CODE AND BRANCH_CODE=@BRANCH_CODE AND EMP_CODE=@EMP_CODE";
                        using (var cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                            cmd.Parameters.AddWithValue("@EMP_CODE", empCode);
                   
                            await con.OpenAsync();
                            await cmd.ExecuteNonQueryAsync();
                        }
                        return Json(new { status = true, data = "Data deleted successfully" });
                    }
                    catch (Exception ex)
                    {
                        return Json(new { status = false, message = $"Delete failed: {ex.Message}" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTDSDeductionEntryEntryDetails(string empCode)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                if (string.IsNullOrEmpty(empCode))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
               
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", 1},
                    {"@EMP_CODE", empCode},                   
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_TDSDeductionEntry]", parameter);
                return Json(new { status = true, data = entryDetailList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportAllDocs()
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", 1},
                    {"@Action", "Excel" }
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_TDSDeductionEntry]", parameter);

                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


    }
}
