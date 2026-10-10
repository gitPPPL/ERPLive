using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportTrackingReportController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportTrackingReport/Index.cshtml");
        }
    }
}
