using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Purchase.EPR
{
    public class EPRReconciliationListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Purchase/EPR/EPRReconciliationList/Index.cshtml");
        }
    }
}
