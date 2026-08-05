using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PendingSaudaOrderListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/PendingSaudaOrderList/Index.cshtml");
        }
    }
}
