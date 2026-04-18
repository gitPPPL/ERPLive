using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    public class CostMasterListController : Controller
    {
        public IActionResult Index()
        {
            //return View();
            return View("~/Views/Admin/Setup/CostMasterList/Index.cshtml");
        }
    }
}
