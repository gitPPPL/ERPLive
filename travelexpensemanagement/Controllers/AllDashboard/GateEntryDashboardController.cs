using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.AllDashboard
{
    public class GateEntryDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/AllDashboard/GateEntryDashboard/Index.cshtml");
        }
    }
}
