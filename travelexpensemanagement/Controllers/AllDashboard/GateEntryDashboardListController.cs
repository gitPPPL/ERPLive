using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.AllDashboard
{
    public class GateEntryDashboardListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/AllDashboard/GateEntryDashboardList/Index.cshtml");
        }
    }
}
