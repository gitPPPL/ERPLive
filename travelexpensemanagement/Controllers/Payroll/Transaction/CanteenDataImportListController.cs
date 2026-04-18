using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class CanteenDataImportListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Transaction/CanteenDataImportList/Index.cshtml");
        }
    }
}
