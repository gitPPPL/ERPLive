using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.ProductPortfolio
{
    public class PackagingController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/ProductPortfolio/Packaging/Index.cshtml");
            //return View("~/Views/Packaging/Index.cshtml");
        }
    }
}
