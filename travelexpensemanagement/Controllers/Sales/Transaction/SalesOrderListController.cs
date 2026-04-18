using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient; 

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesOrderListController : Controller
    {
       
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public SalesOrderListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Sales Order";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Sales/Transaction/SalesOrderList/Index.cshtml", model);            
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesOrderList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", UsersessionDt.PubCompCode },
                    {"@YEAR_CODE", UsersessionDt.PubFYearCode },
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE", "SORD"},
                    {"@Action", "SalesOrderList" }
                };

                var fullList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parameter);
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
        public async Task<IActionResult> DeleteSalesOrderEntry(string docid)
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
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {

                            string[] deleteQueries = {
                        "DELETE FROM ORDER1 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO",
                        "DELETE FROM ORDER2 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO",
                        "DELETE FROM ORDER3 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO"
                        };
                            foreach (var query in deleteQueries)
                            {
                                using (var cmd = new SqlCommand(query, con, transaction))
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
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesOrderEntryDetails(string docid)
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
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parameter);
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
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parameter);

                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }



        //[HttpGet]
        //public async Task<IActionResult> GetSalesOrderList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        //{
        //    try
        //    {
        //        var UsersessionDt = _globalValue.GetGlobalVariables();
        //        var parameter = new Dictionary<string, object>
        //        {
        //            {"@COMP_CODE", UsersessionDt.PubCompCode },
        //            {"@YEAR_CODE", UsersessionDt.PubFYearCode },
        //            {"@BRANCH_CODE", 1 },
        //            {"@Action", "SalesOrderList" }
        //        };

        //        var fullList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_SalesOrder]", parameter);
        //        if (!string.IsNullOrEmpty(searchTerm))
        //        {
        //            searchTerm = searchTerm.ToLower();
        //            fullList = fullList
        //                .Where(x =>
        //                {
        //                    var dict = (IDictionary<string, object>)x;
        //                    string[] searchableKeys = { "DOC_ID" };
        //                    return searchableKeys.Any(key =>
        //                        dict.ContainsKey(key) &&
        //                        dict[key]?.ToString().ToLower().Contains(searchTerm) == true
        //                    );
        //                })
        //                .ToList();
        //        }
        //        var totalCount = fullList.Count;
        //        var pagedList = fullList
        //            .Skip((pageNumber - 1) * pageSize)
        //            .Take(pageSize)
        //            .ToList();

        //        return Json(new { status = true, data = pagedList, totalCount });

        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = false, message = ex.Message });
        //    }
        //}

        //[HttpDelete]
        //public async Task<IActionResult> DeleteSalesOrderEntry(string docid)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(docid))
        //        {
        //            return Json(new { status = false, message = "Invalid ID" });
        //        }
        //        var userSession = _globalValue.GetGlobalVariables();
        //        string VType = docid.Substring(0, 4);
        //        string VNo = docid.Substring(4);
        //        using (var con = _dbcontext.GetErpConnection())
        //        {
        //            try
        //            {
        //                string query = "DELETE FROM PAY_INTRIM WHERE COMP_CODE=@COMP_CODE AND YEAR_CODE=@YEAR_CODE AND BRANCH_CODE=@BRANCH_CODE AND V_NO=@V_NO AND V_TYPE=@V_TYPE";
        //                using (var cmd = new SqlCommand(query, con))
        //                {
        //                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
        //                    cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
        //                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
        //                    cmd.Parameters.AddWithValue("@V_NO", VNo);
        //                    cmd.Parameters.AddWithValue("@V_TYPE", VType);
        //                    await con.OpenAsync();
        //                    await cmd.ExecuteNonQueryAsync();
        //                }
        //                return Json(new { status = true, data = "Data deleted successfully" });
        //            }
        //            catch (Exception ex)
        //            {
        //                return Json(new { status = false, message = $"Delete failed: {ex.Message}" });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = false, message = ex.Message });
        //    }
        //}

        //[HttpGet]
        //public async Task<IActionResult> GetSalesOrderEntryDetails(string docid)
        //{
        //    try
        //    {
        //        var usersession = _globalValue.GetGlobalVariables();
        //        if (string.IsNullOrEmpty(docid))
        //        {
        //            return Json(new { status = false, message = "Invalid ID" });
        //        }
        //        var vType = docid.Substring(0, 4);
        //        var vNo = docid.Substring(4);
        //        var parameter = new Dictionary<string, object>
        //        {
        //            {"@COMP_CODE", usersession.PubCompCode },
        //            {"@YEAR_CODE", usersession.PubFYearCode},
        //            {"@BRANCH_CODE", 1},
        //            {"@V_NO", vNo},
        //            {"@V_TYPE", vType},
        //            {"@Action", "EntryDetail" }
        //        };
        //        var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_SalesOrder]", parameter);
        //        return Json(new { status = true, data = entryDetailList });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = false, message = ex.Message });
        //    }
        //}

        //[HttpGet]
        //public async Task<IActionResult> ExportAllDocs()
        //{
        //    try
        //    {
        //        var usersession = _globalValue.GetGlobalVariables();
        //        var parameter = new Dictionary<string, object>
        //        {
        //            {"@COMP_CODE", usersession.PubCompCode },
        //            {"@YEAR_CODE", usersession.PubFYearCode},
        //            {"@BRANCH_CODE", 1},
        //            {"@Action", "Excel" }
        //        };
        //        var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_SalesOrder]", parameter);
        //        return Json(new { status = true, data = dataList });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = false, message = ex.Message });
        //    }
        //}



    }
}
