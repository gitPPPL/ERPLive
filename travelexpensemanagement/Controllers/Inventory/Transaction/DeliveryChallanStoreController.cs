using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class DeliveryChallanStoreController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/DeliveryChallanStore/Index.cshtml");
        }
    }
}
