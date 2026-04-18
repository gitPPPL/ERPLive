using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.ProductPortfolio
{
    public class LoaderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
