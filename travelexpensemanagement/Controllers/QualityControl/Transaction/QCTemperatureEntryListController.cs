using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Dynamic;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    [SessionAuthorize]
    public class QCTemperatureEntryListController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public QCTemperatureEntryListController(DataBaseConnection dbcontext, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "QC Temprature Entry";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/QualityControl/Transaction/QCTemperatureEntryList/Index.cshtml", model);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetQcTempratureList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var dataList = new List<dynamic>();
                int totalCount = 0;

                using (SqlConnection conn = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_GetQcTempratureEntry]", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add Parameters
                        cmd.Parameters.AddWithValue("@COMP_CODE", UsersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", UsersessionDt.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", UsersessionDt.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", "TAPE");
                        cmd.Parameters.AddWithValue("@Action", "QcTempratureEntryList");
                        cmd.Parameters.AddWithValue("@SearchTerm", (object)searchTerm ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        await conn.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            // --- RESULT SET 1: QCTempEntryList ---
                            while (await reader.ReadAsync())
                            {
                                var row = new ExpandoObject() as IDictionary<string, object>;
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row.Add(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i));
                                }
                                dataList.Add(row);
                            }

                            // --- RESULT SET 2: TotalCount ---
                            if (await reader.NextResultAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    totalCount = Convert.ToInt32(reader["TotalCount"]);
                                }
                            }
                        }
                    }
                }
                return Json(new { success = true, data = dataList, totalCount });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQcTempratureEntry(string docId)
        {
            try
            {
                if (string.IsNullOrEmpty(docId))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }

                var userSession = _globalValue.GetGlobalVariables();
                string VType = docId.Substring(0, 4);
                string VNo = docId.Substring(4);

                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {

                            string[] deleteQueries = {
                        "DELETE FROM TAPE_QUALITY1 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO",
                        "DELETE FROM TAPE_QUALITY2 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO"

                        };

                            foreach (var query in deleteQueries)
                            {
                                using (var cmd = new SqlCommand(query, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@V_TYPE", VType);
                                    cmd.Parameters.AddWithValue("@V_NO", VNo);

                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            return Json(new { success = true, data = "Data deleted successfully" });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Json(new { success = false, message = $"Delete failed: {ex.Message}" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetQcTempratureEntryDetails(string docid)
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
                    {"@BRANCH_CODE",  usersession.PubBranchCode},
                    {"@V_TYPE", "TAPE" },
                    {"@V_NO", docid.Substring(4) },
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetQcTempratureEntry]", parameter);
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
                    {"@V_TYPE",  "TAPE"},
                    {"@Action", "Excel" }
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetQcTempratureEntry]", parameter);

                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


    }
}
  