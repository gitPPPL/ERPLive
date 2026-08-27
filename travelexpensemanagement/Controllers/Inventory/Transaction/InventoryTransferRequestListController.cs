using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class InventoryTransferRequestListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/InventoryTransferRequestList/Index.cshtml");
        }
    }
}
