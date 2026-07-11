using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class StorePurchaseOrderStatusController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/StorePurchaseOrderStatus/Index.cshtml");
        }
    }
}
