using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class InventoryConsumptionEntryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/InventoryConsumptionEntry/Index.cshtml");
        }
    }
}
