using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class AccountRecoController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/AccountReco/Index.cshtml");
        }
    }
}
