using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Dynamic;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class QuotationRateApprovalListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IQuotationRateApprovalListRepository _quotationRateApprovalListRepository;

        public QuotationRateApprovalListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, IQuotationRateApprovalListRepository quotationRateApprovalListRepository)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _quotationRateApprovalListRepository = quotationRateApprovalListRepository;
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

        //[HttpGet]
        //public async Task<IActionResult> GetQuotationRateList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        //{
        //    try
        //    {
        //        var UsersessionDt = _globalValue.GetGlobalVariables(); 
        //        var parameter = new Dictionary<string, object> {
        //            {"@COMP_CODE", UsersessionDt.PubCompCode },
        //            {"@YEAR_CODE", UsersessionDt.PubFYearCode },
        //            {"@BRANCH_CODE" , UsersessionDt.PubBranchCode},
        //            {"@Action",  "List"}
        //        };
        //        var fullList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_QuotationRateApproval]", parameter);
        //        if (!string.IsNullOrEmpty(searchTerm))
        //        {
        //            searchTerm = searchTerm.ToLower();
        //            fullList = fullList
        //                .Where(x =>
        //                {
        //                    var dict = (IDictionary<string, object>)x;
        //                    string[] searchableKeys = { "V_NO" };
        //                    return searchableKeys.Any(key =>
        //                        dict.ContainsKey(key) &&
        //                        dict[key]?.ToString().ToLower().Contains(searchTerm) == true
        //                    );
        //                })
        //                .ToList();
        //        }

        //        var totalCount = fullList.Count;
        //        var pagedList = fullList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        //        return Json(new { status = true, data = pagedList, totalCount });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = false, message = ex.Message });
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> GetQuotationRateList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var fullList = await _quotationRateApprovalListRepository.GetQuotationRateListAsync();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();

                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;

                            string[] searchableKeys = { "V_NO" };

                            return searchableKeys.Any(key =>
                                dict.ContainsKey(key) &&
                                dict[key]?.ToString()?.ToLower().Contains(searchTerm) == true);
                        })
                        .ToList();
                }

                var totalCount = fullList.Count;

                var pagedList = fullList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                return Json(new
                {
                    status = true,
                    data = pagedList,
                    totalCount
                });
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

        [HttpPost]
        public async Task<IActionResult> DelQuotationRateApprovalData(string docid)
        {
            try
            {
                var result = await _quotationRateApprovalListRepository.DeleteQuotationRateApprovalAsync(docid);

                if (result > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Data deleted successfully."
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "Data delete failed."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetpurchaseReceiptHistory(string itemcode)
        {
            try
            {
                var data = await _quotationRateApprovalListRepository
                    .GetPurchaseReceiptHistoryAsync(itemcode);

                return Json(new
                {
                    status = true,
                    data = data
                });
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
        public async Task<IActionResult> GetQuotationApprovalData(string itemcode)
        {
            try
            {
                var data = await _quotationRateApprovalListRepository
                    .GetQuotationApprovalHistoryAsync(itemcode);

                return Json(new
                {
                    status = true,
                    data = data
                });
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
        public async Task<IActionResult> GetPurchaseOrderData(string itemcode)
        {
            try
            {
                var data = await _quotationRateApprovalListRepository
                    .GetPurchaseOrderHistoryAsync(itemcode);

                return Json(new
                {
                    status = true,
                    data = data
                });
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
        public async Task<IActionResult> GetPurchaseOrderAprvlEntryDetails(string docid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(docid))
                {
                    return Json(new
                    {
                        status = false,
                        message = "Invalid ID"
                    });
                }

                var data = await _quotationRateApprovalListRepository
                    .GetPurchaseOrderApprovalEntryDetailsAsync(docid);

                return Json(new
                {
                    status = true,
                    data = data
                });
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
        public async Task<IActionResult> ExportAllDocs()
        {
            try
            {
                var data = await _quotationRateApprovalListRepository
                    .ExportAllDocumentsAsync();

                return Json(new
                {
                    status = true,
                    data = data
                });
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

    }
}
