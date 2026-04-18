using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class hrmsjoiningController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Payroll/HRMS/hrmsjoining/Index.cshtml");
        }
    }
}
