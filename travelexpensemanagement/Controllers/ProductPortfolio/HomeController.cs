using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;


namespace travelexpensemanagement.Controllers.ProductPortfolio
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
           
            return View("~/Views/ProductPortfolio/Home/Index.cshtml");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        
    }
}
