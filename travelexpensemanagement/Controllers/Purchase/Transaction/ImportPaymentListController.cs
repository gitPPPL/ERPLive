using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportPaymentListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportPaymentList/Index.cshtml");
        }
    }
}
