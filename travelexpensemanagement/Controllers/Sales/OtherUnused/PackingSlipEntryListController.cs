using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Repositories.Interfaces.Sales.OtherUnused;

namespace travelexpensemanagement.Controllers.Sales.OtherUnused
{
    [SessionAuthorize]
    public class PackingSlipEntryListController : Controller
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly GlobalExcelExport _excel;
        private readonly IPackingSlipEntryListRepository _repo;

        public PackingSlipEntryListController(GlobalVariableService globalVariableService, DbHelper dbHelper, GlobalExcelExport excel, 
            IPackingSlipEntryListRepository repo)
        {
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _excel = excel;
            _repo = repo;
        }

        public IActionResult Index()
        {
            return View("~/Views/Sales/OtherUnused/PackingSlipEntryList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetPackingSlipList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var result = _repo.GetPackingSlipList(searchTerm, pageNumber, pageSize);
                return Json(new { status = result.status, data = result.data, totalCount = result.totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int vNo, string docType)
        {
            var result = await _repo.Delete(vNo, docType);
            return Json(new { status = result.status, message = result.message });
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
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[Sp_PackingSlipEntry]", parameter);
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

                var fileBytes = _excel.ExportToExcel("Sp_PackingSlipEntry", "Packing Slip Entry", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"PackingslipEntry{DateTime.Now:ddMMyyyy}.xlsx"
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
