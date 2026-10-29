using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportTrackingEntryListController : Controller
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly GlobalExcelExport _excel;
        private readonly IImportTrackingEntryListRepository _ITListRepository;
        public ImportTrackingEntryListController(GlobalVariableService globalVariableService, DbHelper dbHelper, GlobalExcelExport excel, 
            IImportTrackingEntryListRepository ITListRepository)
        {
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _excel = excel;
            _ITListRepository = ITListRepository;
        }
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportTrackingEntryList/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetAllImportTrackingList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var result = _ITListRepository.GetAllImportTrackingList(searchTerm, pageNumber, pageSize);
                if (result.data != null)
                {
                    return Json(new { success = result.status, importList = result.data, totalCount = result.totalCount });
                }
                return Json(new { success = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching Import Tracking Entry List", error = ex.Message });
            }

        }

        [HttpGet]
        public async Task<JsonResult> ITEntryDetails(string vNo, string vType)
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
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@V_TYPE", vType},
                    {"@V_NO", vNo },
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_ImportTrackingEntry]", parameter);
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
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode },
                    { "@Action", "Excel" }
                };

                var fileBytes = _excel.ExportToExcel("sp_ImportTrackingEntry", "Import Tracking Entry", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ImportTrackingEntry_{DateTime.Now:ddMMyyyy}.xlsx"
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
            var result = _ITListRepository.DeleteImportTrackingEntry(vNo, docType);
            return Json(new { status = result.status, message = result.message });
        }

    }
}
