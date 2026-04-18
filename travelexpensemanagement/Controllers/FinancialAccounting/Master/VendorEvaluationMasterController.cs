using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class VendorEvaluationMasterController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/VendorEvaluationMaster/Index.cshtml");
        }
    }
}
