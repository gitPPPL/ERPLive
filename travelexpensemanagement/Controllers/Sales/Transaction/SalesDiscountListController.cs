using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.ModuleService;
using Microsoft.Data.SqlClient;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesDiscountListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public SalesDiscountListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Missed Punch Entry";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Sales/Transaction/SalesDiscountList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> SalesDiscountMastList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", UsersessionDt.PubCompCode },                   
                    {"@V_TYPE", "SALE" },
                    {"@Action", "DisctMastList" }
                };

                var fullList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_DiscMast]", parameter);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "CODE" };
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
        public async Task<IActionResult> DeleteSalesDiscMastEntry(string Code)
        {
            try
            {
                if (string.IsNullOrEmpty(Code))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var userSession = _globalValue.GetGlobalVariables();                
                using (var con = _dbcontext.GetErpConnection())
                {
                    try
                    {
                        int x = 0;
                        string query = "DELETE FROM DISC_MAST WHERE COMP_CODE = @COMP_CODE AND V_TYPE = @V_TYPE AND CODE = @CODE";
                        using (var cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", "SALE");
                            cmd.Parameters.AddWithValue("@CODE", Code);
                            await con.OpenAsync();
                            x=await cmd.ExecuteNonQueryAsync();
                        }
                        if (x > 0)
                            return Json(new { status = true, data = "Data deleted successfully" });
                        else
                            return Json(new { status = false, message = "Delete failed" });
                       
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
        public async Task<IActionResult> SalesDiscountMastEntryDetails(string Code)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                if (string.IsNullOrEmpty(Code))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },                     
                    {"@V_TYPE", "SALE" },
                    {"@V_NO", Code},
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_DiscMast]", parameter);
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
                    {"@V_TYPE",  "SALE"},
                    {"@Action", "Excel" }
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_DiscMast]", parameter);

                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }



    }
}
