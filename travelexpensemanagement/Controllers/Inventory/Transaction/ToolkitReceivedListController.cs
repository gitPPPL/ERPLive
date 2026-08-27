using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class ToolkitReceivedListController : Controller
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalExcelExport _excel;
        private readonly DbHelper _dbHelper;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IToolkitIssueListRepository _repository;
        public ToolkitReceivedListController(GlobalVariableService globalVariableService, GlobalExcelExport excel, DbHelper dbHelper, 
            GlobalValidationdate globalValidationdate, IToolkitIssueListRepository repository)
        {
            _globalVariableService = globalVariableService;
            _excel = excel;
            _dbHelper = dbHelper;
            _globalValidationdate = globalValidationdate;
            _repository = repository;
        }

        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/ToolkitReceivedList/Index.cshtml");
        }

        private string docType = "TORC";

        [HttpGet]
        public IActionResult GetAllToolkitIssue(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var result = _repository.GetAllToolkitIssue(docType, searchTerm, pageNumber, pageSize);
                if (result.data == null)
                {
                    return Json(new { success = false, message = result.message });
                }
                return Json(new { success = true, data = result.data, totalcount = result.totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Delete(int vNo, string docType)
        {
            try
            {
                var result = _repository.Delete(vNo, docType);
                return Json(new { status = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, message = ex.Message });
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
                    { "@V_TYPE", docType },
                    { "@Action", "Excel" }
                };

                var fileBytes = _excel.ExportToExcel("sp_ToolKitIssueEntry", "Toolkit Received", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ToolkitReceived_{DateTime.Now:ddMMyyyy}.xlsx"
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
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_ToolKitIssueEntry]", parameter);
                return Json(new { status = true, data = entryDetailList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
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
    }
}
