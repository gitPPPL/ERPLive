using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class WinderMasterController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/WinderMaster/Index.cshtml");
        }
    }
}
