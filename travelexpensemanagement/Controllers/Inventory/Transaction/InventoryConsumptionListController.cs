using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class InventoryConsumptionListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/InventoryConsumptionList/Index.cshtml");
        }
    }
}
