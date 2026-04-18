using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class VendorEvaluationMasterListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/VendorEvaluationMasterList/Index.cshtml");
        }
    }
}
