using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class InventoryTransferRequestController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/InventoryTransferRequest/Index.cshtml");
        }
    }
}
