using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;


namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class AvailableScrapStockListController : Controller
    {

        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public AvailableScrapStockListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Available Scrap Stock Entry";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Sales/Transaction/AvailableScrapStockList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailblScrapStkList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", UsersessionDt.PubCompCode },
                    {"@YEAR_CODE", UsersessionDt.PubFYearCode },
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE", "ALST"},
                    {"@Action", "AvailScrapStockList" }
                };

                var fullList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_AvailblScrapStk]", parameter);
                
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "DOC_ID" };
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
        public async Task<IActionResult> DeleteAvailblScrapStkEntry(string docid)
        {
            try
            {
                if (string.IsNullOrEmpty(docid))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }

                var userSession = _globalValue.GetGlobalVariables();
                string VType = docid.Substring(0, 4);
                string VNo = docid.Substring(4);

                string[] queries =
                {
                @"DELETE FROM AVAIL_SCRAPSTK2 
                  WHERE COMP_CODE=@COMP_CODE AND YEAR_CODE=@YEAR_CODE 
                    AND BRANCH_CODE=@BRANCH_CODE AND V_TYPE=@V_TYPE AND V_NO=@V_NO",

                @"DELETE FROM AVAIL_SCRAPSTK1 
                  WHERE COMP_CODE=@COMP_CODE AND YEAR_CODE=@YEAR_CODE 
                    AND BRANCH_CODE=@BRANCH_CODE AND V_TYPE=@V_TYPE AND V_NO=@V_NO"
                };

                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            foreach (var sqlQry in queries)
                            {
                                using (var cmd = new SqlCommand(sqlQry, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                    cmd.Parameters.AddWithValue("@V_TYPE", VType);
                                    cmd.Parameters.AddWithValue("@V_NO", VNo);
                                   
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                            transaction.Commit();
                            return Json(new { status = true, data = "Data deleted successfully" });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Json(new { status = false, message = $"Delete failed: {ex.Message}" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailblScrapStkEntryDetails(string docid)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                if (string.IsNullOrEmpty(docid))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE", docid.Substring(0, 4) },
                    {"@V_NO", docid.Substring(4) },
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_AvailblScrapStk]", parameter);
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
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE", 1},
                    {"@Action", "Excel" }
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_AvailblScrapStk]", parameter);

                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }



    }
}
