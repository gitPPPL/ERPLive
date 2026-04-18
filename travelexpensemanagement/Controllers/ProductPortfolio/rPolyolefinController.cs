using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.ProductPortfolio
{
    public class rPolyolefinController : Controller
    {
        public IActionResult Index()
        {
            //return View("~/Views/rPolyolefin/Index.cshtml");
            return View("~/Views/ProductPortfolio/rPolyolefin/Index.cshtml");
        }
    }
}
