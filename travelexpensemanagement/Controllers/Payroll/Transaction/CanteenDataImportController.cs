using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class CanteenDataImportController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Transaction/CanteenDataImport/Index.cshtml");
        }
    }
}
