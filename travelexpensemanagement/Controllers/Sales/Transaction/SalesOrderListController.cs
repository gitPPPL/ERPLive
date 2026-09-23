using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesOrderListController : Controller
    {
       
        private readonly DbHelper _dbHelper;
        private readonly GlobalVariableService _globalValue;
        private readonly GlobalExcelExport _excel;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly ISalesOrderListRepository _repo;
        public SalesOrderListController(DbHelper dbHelper, GlobalVariableService globalValue, GlobalExcelExport excel, 
            GlobalValidationdate globalValidationdate, ISalesOrderListRepository repo)
        {
            _dbHelper = dbHelper;
            _globalValue = globalValue;
            _excel = excel;
            _globalValidationdate = globalValidationdate;
            _repo = repo;
        }
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesOrderList/Index.cshtml");            
        }

        [HttpGet]
        public IActionResult GetSalesOrderList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var result = _repo.GetSalesOrderList(searchTerm, pageNumber, pageSize);
            if(result.data == null)
            {
                return Json(new { status = result.status, message = result.message });
            }
            return Json(new { status = true, data = result.data, totalCount = result.totalCount });
            
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSalesOrderEntry(int vNo, string docType)
        {
            var result = await _repo.DeleteSalesOrderEntry(vNo, docType);
            return Json(new { status = result.status, message = result.message });
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
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@DOC_ID", $"SORD{docid}" },
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_SalesOrder]", parameter);
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
                var gv = _globalValue.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@YEAR_CODE", gv.PubFYearCode },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode },
                    { "@V_TYPE", "SORD"},
                    { "@Action", "Excel" }
                };

                var fileBytes = _excel.ExportToExcel("sp_SalesOrder", "Sales Order", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"SalesOrder{DateTime.Now:ddMMyyyy}.xlsx"
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

        [HttpGet]
        public IActionResult GetSalesEditStatus(string vType, int vNo)
        {
            var result = _repo.GetSalesEditStatus(vType, vNo);
            if(result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }
        //===Check Modification Days==============
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
        public IActionResult CheckSaleInvoiceBeforeDelete(string vType, int vNo)
        {
            var result = _repo.CheckSaleInvoiceBeforeDelete(vType, vNo);

            if (!result.status)
                return Json(new { status = false, message = result.message });

            dynamic data = result.data;

            return Json(new
            {
                status = true,
                exists = data?.exists ?? false,
                billNo = data?.billNo,
                billDate = data?.billDate
            });
        }
    }
}
