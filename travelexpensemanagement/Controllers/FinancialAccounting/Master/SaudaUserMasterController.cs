using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class SaudaUserMasterController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/SaudaUserMaster/Index.cshtml");
        }
    }
}
