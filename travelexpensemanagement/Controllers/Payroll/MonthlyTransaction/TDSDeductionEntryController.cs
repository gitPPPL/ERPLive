using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class TDSDeductionEntryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Payroll/MonthlyTransaction/TDSDeductionEntry/Index.cshtml");
        }
    }
}
