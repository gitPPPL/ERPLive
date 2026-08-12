using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Purchase.EPR
{
    public class EPRReconciliationController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Purchase/EPR/EPRReconciliation/Index.cshtml");
        }
    }
}
