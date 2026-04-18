using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace travelexpensemanagement.Controllers.ProductPortfolio
{
    public class rPETController : Controller
    {
        public IActionResult Index()
        {
            //return View("~/Views/rPET/Index.cshtml");
            return View("~/Views/ProductPortfolio/rPET/Index.cshtml");
        }
    }
}
