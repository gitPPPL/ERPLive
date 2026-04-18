using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class WinderMasterListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/WinderMasterList/Index.cshtml");
        }
    }
}
