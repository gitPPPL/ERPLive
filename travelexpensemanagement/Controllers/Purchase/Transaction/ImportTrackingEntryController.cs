using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportTrackingEntryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportTrackingEntry/Index.cshtml");
        }
    }
}
