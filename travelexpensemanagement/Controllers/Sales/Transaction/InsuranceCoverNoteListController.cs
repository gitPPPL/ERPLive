using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient; 

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class InsuranceCoverNoteListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public InsuranceCoverNoteListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Insurance Cover Note";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Sales/Transaction/InsuranceCoverNoteList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetInsuCoverNoteList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", UsersessionDt.PubCompCode },
                    {"@YEAR_CODE", UsersessionDt.PubFYearCode },
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE", "INSU"},
                    {"@Action", "SalesOrderList" }
                };

                var fullList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_Sale3]", parameter);
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
        public async Task<IActionResult> DeleteInsuCoverNoteEntry(string docid)
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

                using (var con = _dbcontext.GetErpConnection())
                {      
                  string deleteQueries = "DELETE FROM sale3 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO";                                                   
                  using (var cmd = new SqlCommand(deleteQueries, con))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                    cmd.Parameters.AddWithValue("@V_TYPE", VType);
                                    cmd.Parameters.AddWithValue("@V_NO", VNo);
                                    await con.OpenAsync();
                                    await cmd.ExecuteNonQueryAsync();
                  }  
                  return Json(new { status = true, data = "Data deleted successfully" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInsuCoverNoteEntryDetails(string docid)
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
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_Sale3]", parameter);
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
                    {"@V_TYPE", "INSU"},
                    {"@Action", "Excel" }
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_Sale3]", parameter);

                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }



    }
}
