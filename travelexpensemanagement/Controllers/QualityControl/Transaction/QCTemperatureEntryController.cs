using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class QCTemperatureEntryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Transaction/QCTemperatureEntry/Index.cshtml");
        }
    }
}
