using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class ExportTrackingReportController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/ExportTrackingReport/Index.cshtml");
        }
    }
}
