using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class LoomFabricStrengthEntryListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Transaction/LoomFabricStrengthEntryList/Index.cshtml");
        }
    }
}
