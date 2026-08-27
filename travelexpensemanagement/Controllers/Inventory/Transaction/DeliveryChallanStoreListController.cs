using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class DeliveryChallanStoreListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/DeliveryChallanStoreList/Index.cshtml");
        }
    }
}
