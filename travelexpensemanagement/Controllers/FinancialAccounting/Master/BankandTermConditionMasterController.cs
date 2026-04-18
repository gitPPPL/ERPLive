using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class BankandTermConditionMasterController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/BankandTermConditionMaster/Index.cshtml");
        }
    }
}
