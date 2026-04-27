using iTextSharp.text.pdf.parser.clipper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class PostingParameterListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public PostingParameterListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Posting Parameter";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Utilities/PostingParameterList/Index.cshtml", model);
        }
         
        [HttpGet]
        public async Task<IActionResult> GetPostingParameterList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", UsersessionDt.PubCompCode },
                    {"@Action", "PostingParameterEntryList" }
                };

                var fullList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PostingParameterEntry]", parameter);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "Doctype" };
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
        public async Task<IActionResult> DeletePostingParameterEntry(string Vtype, string docType)
        {
            try
            {
                
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;                
                using (var con = _dbcontext.GetErpConnection())
                {
                    try
                    {
                        int x = 0;
                        string query = "DELETE FROM POSTING_MAST WHERE COMP_CODE=@COMP_CODE and BRANCH_CODE=@BRANCH_CODE AND DOC_TYPE=@DOC_TYPE and V_TYPE=@V_TYPE  ";
                        using (var cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@COMP_CODE", companyCd);
                            cmd.Parameters.AddWithValue("@DOC_TYPE", docType);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                            cmd.Parameters.AddWithValue("@V_TYPE", Vtype);

                            await con.OpenAsync();
                              x= await cmd.ExecuteNonQueryAsync();
                        }
                        if(x>0)
                          return Json(new { status = true, data = "Data deleted successfully" });
                        else
                            return Json(new { status = false, message = "Data Delete failed" });

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
        public async Task<IActionResult> GetPostingParameterEntryDetails(string VType)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                if (string.IsNullOrEmpty(VType))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE",  VType},
                    {"@POST_TYPE", ""},
                    {"@FORM_CODE", ""},
                    {"@Action", "EntryDetail" }
                };
 
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PostingParameterEntry]", parameter);
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
                    {"@Action", "Excel" }
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PostingParameterEntry]", parameter);

                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }



    }
}
