using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class LoomFabricStrengthEntryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Transaction/LoomFabricStrengthEntry/Index.cshtml");
        }
    }
}
