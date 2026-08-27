using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class TradingDirectPurchaseListController : Controller
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly GlobalExcelExport _excel;
        private readonly ITradingDirectPurchaseListRepository _tradingDirectPurchaseList;
        private readonly GlobalValidationdate _globalValidationdate;
        public TradingDirectPurchaseListController(GlobalVariableService globalVariableService,
            DbHelper dbHelper, GlobalExcelExport excel, ITradingDirectPurchaseListRepository tradingDirectPurchaseList,
            GlobalValidationdate globalValidationdate)
        {
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _excel = excel;
            _tradingDirectPurchaseList = tradingDirectPurchaseList;
            _globalValidationdate = globalValidationdate;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/TradingDirectPurchaseList/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetAllPurchaseBillPassEntry(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var result = _tradingDirectPurchaseList.GetAllPurchaseBillPassEntry(searchTerm, pageNumber, pageSize);
                if (result.data != null)
                {
                    return Json(new { success = result.status, purchaseBillDirect = result.data, totalCount = result.totalCount });
                }
                return Json(new { success = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching puchase bill pass", error = ex.Message });
            }

        }

        [HttpGet]
        public async Task<JsonResult> PBPEntryDetails(string vNo, string vType)
        {
            try
            {
                var usersession = _globalVariableService.GetGlobalVariables();
                if (string.IsNullOrEmpty(vNo))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@V_TYPE", vType},
                    {"@V_NO", vNo },
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseBillPassEntryDirect]", parameter);
                return Json(new { status = true, data = entryDetailList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@YEAR_CODE", gv.PubFYearCode },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode },
                    { "@Action", "Excel" },
                    { "@Doctype", "TradingPurchase" }
                };

                var fileBytes = _excel.ExportToExcel("sp_PurchaseBillPassEntryDirect", "Trading Direct Purchase", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"TradingDirectPurchase_{DateTime.Now:ddMMyyyy}.xlsx"
                );
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

        [HttpPost]
        public JsonResult Delete(int vNo, string docType)
        {
            if (vNo <= 0 || string.IsNullOrEmpty(docType))
            {
                return Json(new { status = false, message = "Invalid Id!" });
            }
            var result = _tradingDirectPurchaseList.DeletePurchaseBillPass(vNo, docType);
            return Json(new { status = result.status, message = result.message });
        }

        [HttpGet]
        public IActionResult GetPurchaseEditStatus(string vType, int vNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                var userlevel = gv.PubUserLevel;
                var result = _tradingDirectPurchaseList.GetPurchaseEditStatus(vType, vNo);
                return Json(new { success = true, data = result, userlevel });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult checkModificationDays(DateTime? vDate)
        {
            if (!vDate.HasValue)
            {
                return Json(new { success = false, message = "Doc Date is empty!!" });
            }
            var (allowed, message) = _globalValidationdate.CheckModificationDays(vDate.Value);
            return Json(new { success = true, isAllowed = allowed, message = message });
        }

        [HttpGet]
        public async Task<JsonResult> GetApprovalBody(string vType)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                string qry = $@"select 1 from DOC_APPROSTAGE where USER_CODE=@USER_CODE and DOC_CODE=@DOC_CODE and comp_code=@comp_code";
                var parameters = new Dictionary<string, object>{
                   {"@USER_CODE", gv.PubUserId },
                   {"@DOC_CODE", vType},
                   {"@comp_code", gv.PubCompCode},
                };
                var result = await _dbHelper.GetExecuteScalarAsync<int>(qry, parameters);
                bool isApprovalBody = result == 1;
                return Json(new { success = true, isApprovalBody });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetPurchaseDeleteStatus(string vType, int vNo)
        {
            try
            {
                var result = _tradingDirectPurchaseList.GetPurchaseDeleteStatus(vType, vNo);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
