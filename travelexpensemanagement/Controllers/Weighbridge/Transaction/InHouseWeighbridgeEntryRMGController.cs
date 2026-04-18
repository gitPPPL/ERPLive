using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Weighbridge.Transaction
{
    public class InHouseWeighbridgeEntryRMGController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Weighbridge/Transaction/InHouseWeighbridgeEntryRMG/Index.cshtml");
        }
    }
}
