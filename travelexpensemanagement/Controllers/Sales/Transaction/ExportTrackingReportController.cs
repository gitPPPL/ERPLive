using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class ExportTrackingReportController : Controller
    {
        private readonly IExportTrackingReportRepository _repo;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalExcelExport _excel;
        public ExportTrackingReportController(IExportTrackingReportRepository repo, GlobalVariableService globalVariableService, GlobalExcelExport excel)
        {
            _repo = repo;
            _globalVariableService = globalVariableService;
            _excel = excel;
        }
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/ExportTrackingReport/Index.cshtml");
        }


        //================Export Tracking List==================
        [HttpGet]
        public async Task<object> GetReportData(DateTime FromDate, DateTime ToDate, string Status, int WithContainerDetails)
        {
            try
            {
                var result = await _repo.GetReportData(FromDate, ToDate, Status, WithContainerDetails);
                if (result.data != null)
                {
                    return new { success = true, data = result.data };
                }
                return new { success = result.status, message = result.message };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        //================EXCEL====================
        [HttpGet]
        public IActionResult ExportToExcel(DateTime FromDate, DateTime ToDate, string Status, int WithContainerDetails)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@CompCode", gv.PubCompCode },
                    { "@FromDate", FromDate },
                    { "@ToDate", ToDate },
                    { "@DocType", Status },
                    { "@WithContainer", WithContainerDetails },
                    { "@Action", "GetReportList" }
                };

                var fileBytes = _excel.ExportToExcel("sp_ExportTrackingReport", "Export Tracking Report", parameters);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ExportTrackingReport_{DateTime.Now:ddMMyyyy}.xlsx");
            }
            catch (Exception ex)
            {
                return Json(new {success = false, message = ex.Message});
            }
        }
    }
}
