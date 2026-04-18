using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class BankandTermConditionMasterListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/BankandTermConditionMasterList/Index.cshtml");
        }
    }
}
