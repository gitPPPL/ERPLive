using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class SaudaUserMasterListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/SaudaUserMasterList/Index.cshtml");
        }
    }
}
