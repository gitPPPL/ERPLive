using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class ScrapReceivedListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/ScrapReceivedList/Index.cshtml");
        }
    }
}
