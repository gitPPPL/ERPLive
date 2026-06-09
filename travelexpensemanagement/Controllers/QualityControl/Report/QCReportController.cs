using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.QualityControl.Report
{
    public class QCReportController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Report/QCReport/Index.cshtml");
        }
    }
}
