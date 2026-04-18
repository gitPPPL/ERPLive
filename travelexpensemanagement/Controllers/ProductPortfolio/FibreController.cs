using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.ProductPortfolio
{
    public class FibreController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/ProductPortfolio/Fibre/Index.cshtml");
            //return View("~/Views/Fibre/Index.cshtml");
        }
    }
}
