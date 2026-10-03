using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    [SessionAuthorize]
    public class SalesReturnListController : Controller
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DbHelper _dbHelper;
        private readonly GlobalExcelExport _excel;
        private readonly ISalesReturnListRepository _repo;
        public SalesReturnListController(GlobalVariableService globalVariableService, GlobalValidationdate globalValidationdate,
            DbHelper dbHelper, GlobalExcelExport excel, ISalesReturnListRepository repo)
        {
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dbHelper = dbHelper;
            _excel = excel;
            _repo = repo;
        }
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesReturnList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetSalesReturnList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var result = _repo.GetSalesReturnList(searchTerm, pageNumber, pageSize);
                return Json(new { status = result.status, data = result.data, totalCount = result.totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetSalesEditStatus(string vType, int vNo)
        {
            try
            {
                var result = _repo.GetSalesEditStatus(vType, vNo);
                return Json(new { success = result.status, data = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetSalesDeleteStatus(string vType, int vNo)
        {
            try
            {
                var result = _repo.GetSalesDeleteStatus(vType, vNo);
                return Json(new { success = result.status, data = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteSales(string vType, int vNo)
        {
            var result = _repo.DeleteSales(vType, vNo);
            return Json(new { success = result.status, message = result.message });
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
        public async Task<IActionResult> GetSalesEntryEntryDetails(int vNo, string vType)
        {
            try
            {
                var usersession = _globalVariableService.GetGlobalVariables();
                if (string.IsNullOrEmpty(vType) || vNo <= 0)
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@V_NO", vNo},
                    {"@V_TYPE", vType},
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_SalesReturn]", parameter);
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
                    { "@Action", "Excel" }
                };

                var fileBytes = _excel.ExportToExcel("sp_SalesReturn", "Sales Return", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"SalesReturn{DateTime.Now:ddMMyyyy}.xlsx"
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
    }
}
