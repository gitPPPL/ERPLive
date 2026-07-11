using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PrintingRequestionEntryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/PrintingRequestionEntry/Index.cshtml");
        }
    }
}
