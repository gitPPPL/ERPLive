using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class ProductionIncentiveMasterController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Admin/Utilities/ProductionIncentiveMaster/Index.cshtml");
        }
    }
}
