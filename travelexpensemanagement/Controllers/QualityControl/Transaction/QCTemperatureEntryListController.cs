using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class QCTemperatureEntryListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Transaction/QCTemperatureEntryList/Index.cshtml");
        }
    }
}
