using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class VehicleReceivingEntryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/VehicleReceivingEntry/Index.cshtml");
        }
    }
}
