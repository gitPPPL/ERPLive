using Microsoft.AspNetCore.Mvc;
using System.Data;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class QuotationRateApprovalListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public QuotationRateApprovalListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;

        }

        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Purchase Rate Approval";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Purchase/Transaction/QuotationRateApprovalList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetQuotationRateList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables(); 
                var parameter = new Dictionary<string, object> {
                    {"@COMP_CODE", UsersessionDt.PubCompCode },
                    {"@YEAR_CODE", UsersessionDt.PubFYearCode },
                    {"@BRANCH_CODE" , UsersessionDt.PubBranchCode},
                    {"@Action",  "List"}
                };
                var fullList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_QuotationRateApproval]", parameter);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "V_NO" };
                            return searchableKeys.Any(key =>
                                dict.ContainsKey(key) &&
                                dict[key]?.ToString().ToLower().Contains(searchTerm) == true
                            );
                        })
                        .ToList();
                }

                var totalCount = fullList.Count;
                var pagedList = fullList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                return Json(new { status = true, data = pagedList, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DelQuotationRateApprovalData(string docid)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    var UsersessionDt = _globalValue.GetGlobalVariables();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_QuotationRateApproval]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "Delete");
                        cmd.Parameters.AddWithValue("@COMP_CODE", UsersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", UsersessionDt.PubBranchCode);
                        cmd.Parameters.AddWithValue("@DOC_ID", docid);
                        var returnParam = new SqlParameter("@ResultVal", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.ReturnValue
                        };
                        cmd.Parameters.Add(returnParam);
                        var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 5000)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(errorParam);
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                        var errorMessageValue = errorParam?.Value.ToString();
                        var result = (int)returnParam.Value;

                        if (result > 0)
                        {
                            return Json(new { success = true, message = "data delete successfully" });
                        }
                        else
                        {
                            return Json(new { success = false, message = "data delete failed" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data delete failed" });
            }
        }

        //[HttpGet]
        //public async Task<IActionResult> GetpurchaseReceiptHistory(string itemcode)
        //{
        //    try
        //    {
        //        var UsersessionDt = _globalValue.GetGlobalVariables();
        //        var parameter = new Dictionary<string, object>
        //        {
        //            {"@COMP_CODE",  UsersessionDt.PubCompCode},
        //            {"@BRANCH_CODE", UsersessionDt.PubBranchCode },
        //            {"@ItemCode", itemcode  },
        //            {"@Action",  "PurchaseReceiptHistory"}
        //        };
        //        var purchaseReceiptHistoryQuery = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_QuotationRateApproval]", parameter);
        //        return Json(new { status = true, data = purchaseReceiptHistoryQuery });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = false, message = ex.Message });
        //    }

        //}

        //[HttpGet]
        //public IActionResult GetpurchaseReceiptHistory(int itemcode)
        //{
        //    var globalVaribale = _globalValue.GetGlobalVariables();

        //    try
        //    {
        //        List<object> data = new List<object>();

        //        using (SqlConnection con = _dbcontext.GetErpConnection())
        //        {
        //            using (SqlCommand cmd = new SqlCommand("sp_QuotationRateApproval", con))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("@Action", "PurchaseReceiptHistory");
        //                cmd.Parameters.AddWithValue("@COMP_CODE", globalVaribale.PubCompCode);
        //                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVaribale.PubBranchCode);
        //                cmd.Parameters.AddWithValue("@ItemCode",itemcode);

        //                con.Open();

        //                using (SqlDataReader reader = cmd.ExecuteReader())
        //                {
        //                    Console.WriteLine(reader.HasRows);
        //                    while (reader.Read())
        //                    {
        //                        data.Add(new
        //                        {
        //                            V_NO = reader["VNo"],
        //                            V_DATE = reader["Date"],
        //                            Supplier = reader["Supplier"],
        //                            ItemName = reader["ItemName"],
        //                            Make = reader["Make"],
        //                            Unit = reader["Unit"],
        //                            Qty = reader["Qty"],
        //                            Rate = reader["Rate"],
        //                            OthAmt = reader["OthAmt"],
        //                            CGSTPer = reader["CGSTPer"],
        //                            SGSTPer = reader["SGSTPer"],
        //                            IGSTPer = reader["IGSTPer"],
        //                            PackPer = reader["PackPer"],
        //                            DiscPer = reader["DiscPer"],
        //                            LDRate = reader["LDRate"],
        //                            Remarks = reader["Remarks"],
        //                            Status = reader["Status"]
        //                        });
        //                    }
        //                }
        //            }
        //        }

        //        return Json(new { status = true, data = data });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new
        //        {
        //            status = false,
        //            message = ex.Message,
        //            inner = ex.InnerException?.Message
        //        });
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> GetpurchaseReceiptHistory(string itemcode)
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();

                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", userSession.PubCompCode},
                    {"@BRANCH_CODE", userSession.PubBranchCode},
                    {"@ItemCode", itemcode},
                    {"@Action", "PurchaseReceiptHistory"}
                };

                var data = await _dbHelper.GetJsonFromProcedureAsync(
                    "[dbo].[sp_QuotationRateApproval]", parameter);

                return Json(new { status = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetQuotationApprovalData(string itemcode)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE",  UsersessionDt.PubCompCode},
                    {"@YEAR_CODE",  UsersessionDt.PubFYearCode},
                    {"@BRANCH_CODE", UsersessionDt.PubBranchCode },
                    {"@ItemCode", itemcode  },
                    {"@Action",  "QuotationApprovalHistory"}
                };
                var purchaseReceiptHistoryQuery = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_QuotationRateApproval]", parameter);
                return Json(new { status = true, data = purchaseReceiptHistoryQuery });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderData(string itemcode)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE",  UsersessionDt.PubCompCode},
                    {"@BRANCH_CODE", UsersessionDt.PubBranchCode },
                    {"@ItemCode", itemcode},
                    {"@Action",  "PurchaseOrderHistory"}
                };
                var purchaseReceiptHistoryQuery = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_QuotationRateApproval]", parameter);
                return Json(new { status = true, data = purchaseReceiptHistoryQuery });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderAprvlEntryDetails(string docid)
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
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@V_TYPE", docid.Substring(0, 4) },
                    {"@V_NO", docid.Substring(4) },
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_QuotationRateApproval]", parameter);
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
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@Action", "Excel" }
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_QuotationRateApproval]", parameter);

                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

    }
}
