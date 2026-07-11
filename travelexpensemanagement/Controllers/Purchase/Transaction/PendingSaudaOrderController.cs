using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PendingSaudaOrderController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/PendingSaudaOrder/Index.cshtml");
        }
    }
}
