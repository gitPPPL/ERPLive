using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class InventoryOpeningEntryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/InventoryOpeningEntry/Index.cshtml");
        }
    }
}
