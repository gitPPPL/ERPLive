using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Production.BagsProcess
{
    public class BOMListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Production/BagsProcess/BOMList/Index.cshtml");
        }
    }
}
