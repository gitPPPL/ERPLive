using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class ProductionIncentiveMasterListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Admin/Utilities/ProductionIncentiveMasterList/Index.cshtml");
        }
    }
}
