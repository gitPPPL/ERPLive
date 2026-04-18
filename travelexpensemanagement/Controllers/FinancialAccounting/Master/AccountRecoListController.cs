using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class AccountRecoListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/AccountRecoList/Index.cshtml");
        }
    }
}
